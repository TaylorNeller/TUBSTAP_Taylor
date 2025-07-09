import time
import math
import random

from MGameTree import MGameTree
from MTools import MTools
from Player import Player
from AiTools import AiTools


class AI_M_UCT(Player):
    # Parameters
    MAX_SIM = 200  # Number of simulations per action
    SIM_THRESHOLD = 10  # Threshold for expanding child nodes during tree search
    UCB_CONST = 0.15  # Constant for UCB value calculation

    # Debug variables
    max_depth = 0
    total_simulations = 0
    last_id = 0
    movable_unit_num = 0

    # Stopwatch-related
    LIMIT_TIME = 9700  # Time per turn in milliseconds

    def __init__(self):
        self.stopwatch = time.time()
        self.time_left = AI_M_UCT.LIMIT_TIME

    def get_name(self):
        return "M-UCT (Rule-Based Rollout)"

    def show_parameters(self):
        return ""

    def make_action(self, map_, team_color, turn_start, game_start):
        # Create root node
        root = self.make_root(map_, team_color)
        AI_M_UCT.total_simulations = 0

        for _ in range(AI_M_UCT.MAX_SIM):
            self.search(root, team_color)
            AI_M_UCT.total_simulations += 1

        return self.max_rate_action(root)

    @staticmethod
    def make_root(map_, team_color):
        root = MGameTree()
        root.board = map_.create_deep_clone()

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

        return root

    def search(self, node, team_color):
        if AI_M_UCT.total_simulations == 0:
            AI_M_UCT.max_depth = 0

        if node.depth > AI_M_UCT.max_depth:
            AI_M_UCT.max_depth = node.depth

        enemy_color = self.get_enemy_color(team_color)

        # 1) If no children, nothing to search
        if len(node.next) == 0:
            return

        # 2) Select child with the best UCT
        max_id, max_ucb = 0, -1
        for j, child in enumerate(node.next):
            ucb_value = self.evaluate_uct(child)
            if ucb_value == 100:
                max_ucb = ucb_value
                max_id = j
                break
            if ucb_value > max_ucb:
                max_ucb = ucb_value
                max_id = j

        selected_child = node.next[max_id]

        # 3) Expand if needed
        if selected_child.simnum > AI_M_UCT.SIM_THRESHOLD:
            if len(selected_child.next) == 0:
                # Expand
                if len(selected_child.board.get_units_list(team_color, False, True, False)) > 0:
                    self.develop(selected_child, team_color)
                else:
                    self.develop(selected_child, enemy_color)

            # If expansion yielded children, recurse down
            if len(selected_child.next) > 0:
                self.search(selected_child, team_color)
                selected_child.simnum += 1

                # update stats
                best_child = max(selected_child.next, key=lambda x: x.last_score) if selected_child.next else None
                if best_child:
                    selected_child.last_score = best_child.last_score
                    selected_child.total_score += selected_child.last_score
                    selected_child.housyuu = selected_child.total_score / selected_child.simnum
                AI_M_UCT.last_id = max_id

        else:
            # 4) Simulate using rule-based rollout
            result_map = self.rule_based_simulation(selected_child.board, team_color)
            selected_child.simnum += 1
            selected_child.last_score = self.evaluate_state_value(result_map, team_color)
            selected_child.total_score += selected_child.last_score
            selected_child.housyuu = selected_child.total_score / selected_child.simnum
            AI_M_UCT.last_id = max_id

    @staticmethod
    def max_rate_action(root):
        max_rate = -1
        return_id = 0
        for i, child in enumerate(root.next):
            if child.housyuu > max_rate:
                max_rate = child.housyuu
                return_id = i
        return root.next[return_id].act

    @staticmethod
    def develop(node, team_color):
        all_actions = []
        all_units = node.board.get_units_list(team_color, False, True, False)

        for unit in all_units:
            unit_actions = MTools.get_unit_actions(unit, node.board)
            all_actions.extend(unit_actions)

        for action in all_actions:
            child = MGameTree()
            child.board = node.board.create_deep_clone()

            if len(node.board.get_units_list(team_color, True, False, False)) == 0:
                child.board.inc_turn_count()

            child.act = action
            child.depth = node.depth + 1
            child.board.execute_action(action)
            node.next.append(child)

    @staticmethod
    def evaluate_uct(node):
        # If we never tried this child, return a large constant
        if node.simnum == 0:
            return 100
        # UCT formula
        return node.housyuu + AI_M_UCT.UCB_CONST * math.sqrt(
            math.log(AI_M_UCT.total_simulations) / node.simnum
        )

    def rule_based_simulation(self, map_, team_color):
        """
        A simple rule-based simulation:
          1. For each side (team_color, then enemy_color):
             - Repeatedly pick all units that can still act.
             - For each unit, do either an attack (if any) or a simple move.
          2. Stop if turn limit is reached or if one side has no units.
        """

        sim_map = map_.create_deep_clone()
        enemy_color = self.get_enemy_color(team_color)

        while sim_map.get_turn_count() < sim_map.get_turn_limit():
            # --- Team turn ---
            self.do_simple_turn(sim_map, team_color)

            # Check end conditions
            if sim_map.get_turn_count() >= sim_map.get_turn_limit():
                break
            if len(sim_map.get_units_list(enemy_color, True, True, False)) == 0 or \
               len(sim_map.get_units_list(team_color, True, True, False)) == 0:
                break

            # --- Enemy turn ---
            self.do_simple_turn(sim_map, enemy_color)

            # Check end conditions
            if sim_map.get_turn_count() >= sim_map.get_turn_limit():
                break
            if len(sim_map.get_units_list(enemy_color, True, True, False)) == 0 or \
               len(sim_map.get_units_list(team_color, True, True, False)) == 0:
                break

            # Increase turn count for both sides
            # (You might be using a per-side or per-both-sides increment; adapt to your game’s rules.)
            sim_map.inc_turn_count()
            sim_map.inc_turn_count()

        return sim_map

    def do_simple_turn(self, sim_map, active_color):
        """
        Execute a single 'turn' for the given color with a simplistic heuristic:
         - For each unit that hasn't acted:
           1) If an attack is available, pick the first one.
           2) Otherwise, pick the first move action if available.
        """

        units = sim_map.get_units_list(active_color, False, True, False)
        for unit in units:
            # gather possible attack actions
            attack_actions = AiTools.get_unit_attack_actions(unit, sim_map)
            if attack_actions:
                # Just pick the first available attack
                sim_map.execute_action(attack_actions[0])
            else:
                # If no attack, gather move actions
                move_actions = AiTools.get_unit_move_actions(unit, sim_map)
                if move_actions:
                    # Pick the first move
                    sim_map.execute_action(move_actions[0])

        # Re-enable for next turn
        sim_map.enable_units_action(active_color)

    @staticmethod
    def evaluate_state_value(map_, team_color):
        enemy_color = AI_M_UCT.get_enemy_color(team_color)

        my_team_units = map_.get_units_list(team_color, True, True, False)
        enemy_team_units = map_.get_units_list(enemy_color, True, True, False)

        # If we lost all units, worst outcome
        if len(my_team_units) == 0:
            return 0
        # If enemy lost all units, best outcome
        if len(enemy_team_units) == 0:
            return 1

        # Otherwise, compare HP
        my_total_hp = sum(unit.get_HP() for unit in my_team_units)
        enemy_total_hp = sum(unit.get_HP() for unit in enemy_team_units)

        if my_total_hp - enemy_total_hp >= map_.get_draw_hp_threshold():
            return 1
        elif abs(my_total_hp - enemy_total_hp) < map_.get_draw_hp_threshold():
            return 0.5
        else:
            return 0

    @staticmethod
    def get_enemy_color(team_color):
        return 0 if team_color == 1 else 1
