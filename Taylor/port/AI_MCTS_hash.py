import time
import math
import random

from MGameTree import MGameTree
from MTools import MTools

from Player import Player

class AI_M_UCT(Player):
    # Parameters
    MAX_SIM = 200      # Number of simulations per action
    SIM_THRESHOLD = 10 # Threshold for expanding child nodes during tree search
    UCB_CONST = 0.15   # Constant for UCB value calculation
    LIMIT_TIME = 9700  # Time per turn in milliseconds

    # Debug variables
    max_depth = 0
    total_simulations = 0
    last_id = 0
    movable_unit_num = 0

    # --- NEW: Our global transposition table ---
    # Maps a state_hash -> MGameTree node
    # The node keeps track of simnum, total_score, children, etc.
    transposition_table = {}

    def __init__(self):
        self.stopwatch = time.time()
        self.time_left = AI_M_UCT.LIMIT_TIME

    def get_name(self):
        return "M-UCT with Hash"

    def show_parameters(self):
        return ""

    def make_action(self, map_, team_color, turn_start, game_start):
        # Create root node (or reuse if seen)
        root = self.make_root(map_, team_color)

        # Reset debug counters
        AI_M_UCT.total_simulations = 0
        AI_M_UCT.max_depth = 0

        # Core MCTS iteration
        for i in range(AI_M_UCT.MAX_SIM):
            self.search(root, team_color)
            AI_M_UCT.total_simulations += 1

        return self.max_rate_action(root)

    def make_root(self, map_, team_color):
        """
        Construct or retrieve the root node for the current map/team_color.
        """
        # Compute the hash of the current board
        state_key = self.get_state_hash(map_)

        # If we've seen this state before, reuse its node:
        if state_key in AI_M_UCT.transposition_table:
            root = AI_M_UCT.transposition_table[state_key]
            return root

        # Otherwise, create a brand-new node
        root = MGameTree()
        root.board = map_.create_deep_clone()

        # Expand immediate children (all possible next actions)
        all_actions = []
        all_units = map_.get_units_list(team_color, False, True, False)
        for unit in all_units:
            unit_actions = MTools.get_unit_actions(unit, map_)
            all_actions.extend(unit_actions)

        for action in all_actions:
            child = MGameTree()
            child.board = map_.create_deep_clone()
            child.act = action
            child.depth = 1
            child.board.execute_action(action)
            root.next.append(child)

        # Store in transposition table
        AI_M_UCT.transposition_table[state_key] = root
        return root

    def search(self, node, team_color):
        """
        Perform one UCT search iteration starting at 'node'.
        """
        # Track max depth seen
        if node.depth > AI_M_UCT.max_depth:
            AI_M_UCT.max_depth = node.depth

        enemy_color = AI_M_UCT.get_enemy_color(team_color)

        # If node is terminal (no children), nothing to explore
        if len(node.next) == 0:
            return

        # 1. Select the best child by UCT
        max_id = 0
        max_ucb = -1
        for j, child in enumerate(node.next):
            tmp_ucb = self.evaluate_uct(child)
            if tmp_ucb == 100:  # Unvisited child
                max_ucb = tmp_ucb
                max_id = j
                break
            if tmp_ucb > max_ucb:
                max_ucb = tmp_ucb
                max_id = j

        best_child = node.next[max_id]

        # 2. Expand if child is heavily simulated (>= threshold)
        #    i.e. if child has enough visits, we expand or go deeper.
        if best_child.simnum > AI_M_UCT.SIM_THRESHOLD:
            # If not already expanded, do so
            if len(best_child.next) == 0:
                # Expand from best_child
                # Who moves next? If no units from team_color can move, it's enemy_color's turn
                if len(best_child.board.get_units_list(team_color, False, True, False)) > 0:
                    self.develop(best_child, team_color)
                else:
                    self.develop(best_child, enemy_color)

            # If expansion produced new children, keep searching deeper
            if len(best_child.next) > 0:
                self.search(best_child, team_color)

                # Update best_child stats
                best_child.simnum += 1
                # We pick the best child from best_child’s children for backprop
                deeper_best = max(best_child.next, key=lambda x: x.last_score) if best_child.next else None
                if deeper_best:
                    best_child.last_score = deeper_best.last_score
                    best_child.total_score += best_child.last_score
                    best_child.housyuu = best_child.total_score / best_child.simnum

                AI_M_UCT.last_id = max_id

        else:
            # 3. Otherwise, do a random playout
            result_state = self.random_simulation(best_child.board, team_color)

            best_child.simnum += 1
            score = self.evaluate_state_value(result_state, team_color)
            best_child.last_score = score
            best_child.total_score += score
            best_child.housyuu = best_child.total_score / best_child.simnum

            AI_M_UCT.last_id = max_id

    def develop(self, node, team_color):
        """
        Expand `node` by generating all child states from current board `node.board`
        for the specified team_color. We also store newly expanded children into the
        transposition table if not already present.
        """
        all_actions = []
        all_units = node.board.get_units_list(team_color, False, True, False)

        for unit in all_units:
            unit_actions = MTools.get_unit_actions(unit, node.board)
            all_actions.extend(unit_actions)

        for action in all_actions:
            # Check if this action leads to a known state
            child_board = node.board.create_deep_clone()

            # If no units can move, we might need to increment turn_count,
            # but that might be handled in your environment. We'll keep your logic:
            if len(node.board.get_units_list(team_color, True, False, False)) == 0:
                child_board.inc_turn_count()

            child_board.execute_action(action)

            # Compute the state hash & see if it already exists
            state_key = self.get_state_hash(child_board)
            if state_key in AI_M_UCT.transposition_table:
                # Reuse the existing node
                child_node = AI_M_UCT.transposition_table[state_key]
            else:
                # Create a brand new node
                child_node = MGameTree()
                child_node.board = child_board
                child_node.depth = node.depth + 1
                # Insert into transposition table
                AI_M_UCT.transposition_table[state_key] = child_node

            child_node.act = action
            node.next.append(child_node)

    def random_simulation(self, map_, team_color):
        """
        Rollout or playout policy. Random moves until game ends or turn_limit reached.
        """
        enemy_color = AI_M_UCT.get_enemy_color(team_color)
        sim_map = map_.create_deep_clone()
        rand_gen = random.Random()

        while sim_map.get_turn_count() < sim_map.get_turn_limit():
            # 1. Team_color moves
            sim_units = sim_map.get_units_list(team_color, False, True, False)
            while len(sim_units) > 0:
                # With probability 0.8, move; with 0.2, do an attack if possible.
                if rand_gen.random() >= 0.2 and len(MTools.get_all_attack_actions(team_color, sim_map)) > 0:
                    sim_actions = MTools.get_all_attack_actions(team_color, sim_map)
                    sim_action = random.choice(sim_actions)
                else:
                    sim_unit = random.choice(sim_units)
                    sim_actions = MTools.get_unit_actions(sim_unit, sim_map)
                    sim_action = random.choice(sim_actions)

                # Remove from sim_units so we don't move it again
                # in the same 'turn'
                possibly_stale_unit = sim_map.get_unit(sim_action.operation_unit_id)
                if possibly_stale_unit in sim_units:
                    sim_units.remove(possibly_stale_unit)

                sim_map.execute_action(sim_action)

            sim_map.enable_units_action(team_color)
            sim_map.inc_turn_count()
            if sim_map.get_turn_count() >= sim_map.get_turn_limit():
                break

            # 2. enemy_color moves
            enemies = sim_map.get_units_list(enemy_color, False, True, False)
            while len(enemies) > 0:
                if rand_gen.random() >= 0.2 and len(MTools.get_all_attack_actions(enemy_color, sim_map)) > 0:
                    enemy_actions = MTools.get_all_attack_actions(enemy_color, sim_map)
                    enemy_action = random.choice(enemy_actions)
                else:
                    enemy_unit = random.choice(enemies)
                    enemy_actions = MTools.get_unit_actions(enemy_unit, sim_map)
                    enemy_action = random.choice(enemy_actions)

                possibly_stale_unit = sim_map.get_unit(enemy_action.operation_unit_id)
                if possibly_stale_unit in enemies:
                    enemies.remove(possibly_stale_unit)

                sim_map.execute_action(enemy_action)

            sim_map.enable_units_action(enemy_color)
            sim_map.inc_turn_count()
            if sim_map.get_turn_count() >= sim_map.get_turn_limit():
                break

            # Check if either side is wiped out
            if (len(sim_map.get_units_list(team_color, True, True, False)) == 0 or
                len(sim_map.get_units_list(team_color, False, False, True)) == 0):
                break

        return sim_map

    def max_rate_action(self, root):
        """
        Choose the child whose average reward (housyuu) is highest.
        """
        max_rate = 0
        return_id = 0

        for i, return_node in enumerate(root.next):
            if return_node.housyuu > max_rate:
                max_rate = return_node.housyuu
                return_id = i

        return root.next[return_id].act

    def evaluate_uct(self, node):
        """
        Classic UCB formula.
        If node is unvisited (simnum=0), return a big sentinel (e.g., 100).
        """
        if node.simnum == 0:
            return 100.0
        return (node.housyuu
                + AI_M_UCT.UCB_CONST * math.sqrt(math.log(AI_M_UCT.total_simulations) / node.simnum))

    def evaluate_state_value(self, map_, team_color):
        """
        Evaluate a game state from the perspective of `team_color`.
        Returns a float reward in [0,1], e.g. 1 if winning, 0 if losing, 0.5 for draw-ish.
        """
        enemy_color = AI_M_UCT.get_enemy_color(team_color)

        my_team_units = map_.get_units_list(team_color, True, True, False)
        enemy_team_units = map_.get_units_list(enemy_color, True, True, False)

        # If we are fully destroyed, reward = 0
        if len(my_team_units) == 0:
            return 0.0
        # If enemy is destroyed, reward = 1
        if len(enemy_team_units) == 0:
            return 1.0

        my_total_hp = sum(unit.get_HP() for unit in my_team_units)
        enemy_total_hp = sum(unit.get_HP() for unit in enemy_team_units)

        # If your HP advantage is >= threshold, treat as winning
        if my_total_hp - enemy_total_hp >= map_.get_draw_hp_threshold():
            return 1.0
        # If difference is smaller than threshold, treat as partial draw
        elif abs(my_total_hp - enemy_total_hp) < map_.get_draw_hp_threshold():
            return 0.5
        else:
            return 0.0

    @staticmethod
    def get_enemy_color(team_color):
        return 0 if team_color == 1 else 1

    # ---------------------------------------------------------------------
    # NEW: A simple state hashing method
    # ---------------------------------------------------------------------
    def get_state_hash(self, map_):
        """
        Returns a string or integer that (mostly) uniquely identifies the map state.
        We'll do a straightforward approach:
          - Gather info about each unit: (team, type, x, y, HP, actionFinished)
          - Sort by unit ID (or positions) to get a canonical ordering
          - Include turn_count if relevant
        For large boards or if performance is critical, a Zobrist hash is recommended.
        """

        # You can keep it minimal or as robust as you need
        # Example: create a list of tuples
        data = []
        for unit in map_.get_units():
            if unit is None:
                continue
            # You might also incorporate the unit's base type or Mark
            data.append((
                unit.get_team_color(),
                unit.get_name(),          # or the numeric spec ID
                unit.get_x_pos(),
                unit.get_y_pos(),
                unit.get_HP(),
                unit.is_action_finished()
            ))

        # Sort the list so that different permutations of the same positions produce the same key
        data.sort(key=lambda x: (x[0], x[1], x[2], x[3]))
        turn_cnt = map_.get_turn_count()

        # Then turn that list + turn count into a hashable string
        # Alternatively, you could do a tuple of (turn_cnt, tuple_of_data) and rely on Python's built-in hashing.
        return str(turn_cnt) + "|" + str(data)
