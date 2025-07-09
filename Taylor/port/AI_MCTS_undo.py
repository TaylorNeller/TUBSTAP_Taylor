import time
import math
import random

from Player import Player
from MGameTree import MGameTree
from MTools import MTools
from Action import Action
from Consts import Consts
from DamageCalculator import DamageCalculator

class AI_M_UCT(Player):
    # Parameters
    MAX_SIM = 200       # Number of simulations per action
    SIM_THRESHOLD = 10  # Threshold for expanding child nodes
    UCB_CONST = 0.15    # UCB exploration constant

    # Debug variables
    max_depth = 0
    total_simulations = 0
    last_id = 0

    # Time limit (example)
    LIMIT_TIME = 9700  # Time per turn in milliseconds

    def __init__(self):
        self.stopwatch = time.time()
        self.time_left = AI_M_UCT.LIMIT_TIME

    def get_name(self):
        return "M-UCT"

    def show_parameters(self):
        return ""

    def make_action(self, map_, team_color, turn_start, game_start):
        """
        The main entry point for deciding which action to make.
        """
        # 1. Create the root node
        root = MGameTree()
        root.act = None
        root.depth = 0
        root.simnum = 0
        root.total_score = 0.0
        root.last_score = 0.0
        root.housyuu = 0.0
        root.next = []

        # 2. Gather all possible actions from the current map/team in-place
        all_actions = []
        all_units = map_.get_units_list(team_color, False, True, False)  # all movable units
        for unit in all_units:
            unit_actions = MTools.get_unit_actions(unit, map_)
            all_actions.extend(unit_actions)

        # 3. Create one child node per action (no deep clone, just store the action)
        for action in all_actions:
            child = MGameTree()
            child.act = action
            child.depth = 1
            root.next.append(child)

        # 4. Run simulations
        AI_M_UCT.total_simulations = 0
        for _ in range(AI_M_UCT.MAX_SIM):
            self.search(root, map_, team_color)
            AI_M_UCT.total_simulations += 1

        # 5. Select the best action
        return self.max_rate_action(root)

    def search(self, node, map_, team_color):
        """
        MCTS search step: selection -> expansion -> simulation/rollout -> backprop.
        node: current tree node
        map_: current board state (in place)
        team_color: whose move it is from node's perspective
        """
        if AI_M_UCT.total_simulations == 0:
            AI_M_UCT.max_depth = 0
        if node.depth > AI_M_UCT.max_depth:
            AI_M_UCT.max_depth = node.depth

        # If no children, no moves available
        if len(node.next) == 0:
            return

        # Selection: pick child with max UCT
        max_id, max_ucb = 0, -99999
        for j, child in enumerate(node.next):
            tmp_ucb = self.evaluate_uct(child)
            if tmp_ucb == 100:
                # Found unvisited child, pick it immediately
                max_ucb = tmp_ucb
                max_id = j
                break
            if tmp_ucb > max_ucb:
                max_ucb = tmp_ucb
                max_id = j

        # Expand if needed
        selected_child = node.next[max_id]
        if selected_child.simnum > AI_M_UCT.SIM_THRESHOLD and len(selected_child.next) == 0:
            # Expand
            # We must figure out who is to move in that child's position
            # The child action is from 'team_color', after applying it, the next mover is either 'team_color' or the enemy
            # depending on whether the same side can still move or not.
            undo_log = map_.execute_action_inplace(selected_child.act, team_color)
            next_color = self.determine_next_color(map_, team_color)

            # Now gather all possible actions for the next_color
            actions = []
            units = map_.get_units_list(next_color, False, True, False)
            for u in units:
                actions.extend(MTools.get_unit_actions(u, map_))

            # Create children
            for act in actions:
                c = MGameTree()
                c.act = act
                c.depth = selected_child.depth + 1
                selected_child.next.append(c)

            # Revert the action
            map_.undo_action_inplace(undo_log)

        # Now either we descend or we rollout
        if len(selected_child.next) > 0:
            # We have children, so descend deeper
            undo_log = map_.execute_action_inplace(selected_child.act, team_color)
            next_color = self.determine_next_color(map_, team_color)
            self.search(selected_child, map_, next_color)
            # Backprop
            selected_child.simnum += 1
            # We pick the best child's last_score to feed back
            if selected_child.next:
                best_child = max(selected_child.next, key=lambda x: x.last_score)
                selected_child.last_score = best_child.last_score
                selected_child.total_score += selected_child.last_score
                selected_child.housyuu = selected_child.total_score / selected_child.simnum
            map_.undo_action_inplace(undo_log)
            AI_M_UCT.last_id = max_id
        else:
            # Rollout
            undo_log = map_.execute_action_inplace(selected_child.act, team_color)
            next_color = self.determine_next_color(map_, team_color)
            value = self.random_simulation(map_, next_color, team_color)  # random rollout
            # Backprop
            selected_child.simnum += 1
            selected_child.last_score = value
            selected_child.total_score += value
            selected_child.housyuu = selected_child.total_score / selected_child.simnum
            map_.undo_action_inplace(undo_log)
            AI_M_UCT.last_id = max_id

    def random_simulation(self, map_, sim_color, my_team_color):
        """
        Execute a random playout starting from the current in-place map_ state,
        with sim_color to move first. Return the final state value for 'my_team_color'.
        We apply random moves in-place, record them, then revert them after simulation.
        """
        # We track all undo logs for the entire rollout and revert them at the end.
        # For performance reasons, you might want a single list. However, here is a 
        # straightforward approach: we do the random rollout, store each step's undo, 
        # then revert in reverse order.

        rollout_undos = []
        rollout_actions = []
        max_steps = 50  # limit the length of random playout (to avoid huge expansions)

        step_count = 0
        while step_count < max_steps and not self.is_terminal(map_):
            # Gather all possible moves for sim_color
            units = map_.get_units_list(sim_color, False, True, False)
            if len(units) == 0:
                # No moves for sim_color, so we switch to the other side
                sim_color = self.get_enemy_color(sim_color)
                map_.enable_units_action(sim_color)
                continue

            # Randomly pick a unit, then randomly pick from its possible actions
            rand_unit = random.choice(units)
            possible_acts = MTools.get_unit_actions(rand_unit, map_)
            if len(possible_acts) == 0:
                # No action for this unit. Mark it done, continue
                rand_unit.set_action_finished(True)
                # If that's the last unit, we switch color
                if len(map_.get_units_list(sim_color, False, True, False)) == 0:
                    sim_color = self.get_enemy_color(sim_color)
                    map_.enable_units_action(sim_color)
                continue

            chosen_act = random.choice(possible_acts)
            undo_log = map_.execute_action_inplace(chosen_act, sim_color)
            rollout_undos.append(undo_log)
            rollout_actions.append(chosen_act)
            # Switch color if no more moves remain
            if len(map_.get_units_list(sim_color, False, True, False)) == 0:
                sim_color = self.get_enemy_color(sim_color)
                map_.enable_units_action(sim_color)
            step_count += 1

            if self.is_terminal(map_):
                break

        # Evaluate final board from perspective of my_team_color
        final_value = self.evaluate_state_value(map_, my_team_color)

        # Undo everything
        while rollout_undos:
            log_ = rollout_undos.pop()
            map_.undo_action_inplace(log_)

        return final_value

    def is_terminal(self, map_):
        """
        A condition for ending a rollout early: if either side is wiped out or turn limit is reached, etc.
        """
        if map_.get_turn_count() >= map_.get_turn_limit():
            return True
        # If either side has no living units left:
        red_count = map_.get_num_of_alive_color_units(Consts.RED_TEAM)
        blue_count = map_.get_num_of_alive_color_units(Consts.BLUE_TEAM)
        if red_count == 0 or blue_count == 0:
            return True
        return False

    def determine_next_color(self, map_, current_color):
        """
        After current_color performed one action, check if that color still has units that can act.
        If not, enable the other color and return it.
        """
        remain = map_.get_units_list(current_color, False, True, False)
        if len(remain) == 0:
            # all done, switch color
            next_color = self.get_enemy_color(current_color)
            map_.enable_units_action(next_color)
            return next_color
        return current_color

    @staticmethod
    def max_rate_action(root):
        """
        Among root's children, pick the action with the highest average return (housyuu).
        """
        max_rate = -9999
        return_id = 0
        for i, node in enumerate(root.next):
            if node.housyuu > max_rate:
                max_rate = node.housyuu
                return_id = i
        return root.next[return_id].act

    @staticmethod
    def evaluate_uct(node):
        """
        Standard UCB1 formula + hack for unvisited nodes
        """
        if node.simnum == 0:
            return 100  # prioritize unvisited nodes
        else:
            return node.housyuu + AI_M_UCT.UCB_CONST * math.sqrt(
                math.log(AI_M_UCT.total_simulations) / node.simnum
            )

    @staticmethod
    def evaluate_state_value(map_, team_color):
        """
        Returns a float in [0,1].
         - 1 means 'team_color' is effectively winning or has won
         - 0 means 'team_color' lost
         - 0.5 is a draw, etc.
        """
        enemy_color = AI_M_UCT.get_enemy_color(team_color)

        my_team_units = map_.get_units_list(team_color, True, True, False)
        enemy_team_units = map_.get_units_list(enemy_color, True, True, False)

        # If my side is gone, value=0
        if len(my_team_units) == 0:
            return 0
        # If enemy side is gone, value=1
        if len(enemy_team_units) == 0:
            return 1

        # Compare total HP with threshold
        my_total_hp = sum(u.get_HP() for u in my_team_units)
        enemy_total_hp = sum(u.get_HP() for u in enemy_team_units)
        diff = my_total_hp - enemy_total_hp

        # Example logic: if you lead by more than draw_hp_threshold => 1, losing by more => 0, else 0.5
        if diff >= map_.get_draw_hp_threshold():
            return 1
        elif abs(diff) < map_.get_draw_hp_threshold():
            return 0.5
        else:
            return 0

    @staticmethod
    def get_enemy_color(team_color):
        return Consts.BLUE_TEAM if team_color == Consts.RED_TEAM else Consts.RED_TEAM
