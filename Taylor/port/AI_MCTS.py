from MGameTree import MGameTree
from MTools import MTools

from Player import Player
import time
import math
import random


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

    # Stopwatch-related variables
    LIMIT_TIME = 9700  # Time per turn in milliseconds

    def __init__(self):
        self.stopwatch = time.time()
        self.time_left = AI_M_UCT.LIMIT_TIME

    def get_name(self):
        return "M-UCT"

    def show_parameters(self):
        return ""

    def make_action(self, map_, team_color, turn_start, game_start):

        # print('Starting action decision process')

        # Create root node
        root = self.make_root(map_, team_color)

        AI_M_UCT.total_simulations = 0

        for i in range(AI_M_UCT.MAX_SIM):
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

        enemy_color = AI_M_UCT.get_enemy_color(team_color)
        max_id = 0
        max_ucb = -1

        for j, child in enumerate(node.next):
            tmp_ucb = self.evaluate_uct(child)

            if tmp_ucb == 100:
                max_ucb = tmp_ucb
                max_id = j
                break

            if tmp_ucb > max_ucb:
                max_ucb = tmp_ucb
                max_id = j

        if len(node.next) == 0:
            return

        if node.next[max_id].simnum > AI_M_UCT.SIM_THRESHOLD:
            if len(node.next[max_id].next) == 0:
                if len(node.next[max_id].board.get_units_list(team_color, False, True, False)) > 0:
                    self.develop(node.next[max_id], team_color)
                else:
                    self.develop(node.next[max_id], enemy_color)

            # If development created new nodes, continue search
            if len(node.next[max_id].next) > 0:
                child_node = node.next[max_id]
                self.search(child_node, team_color)

                # Update statistics
                child_node.simnum += 1
                
                # Instead of using last_id, find the best child
                best_child = max(child_node.next, key=lambda x: x.last_score) if child_node.next else None
                if best_child:
                    child_node.last_score = best_child.last_score
                    child_node.total_score += child_node.last_score
                    child_node.housyuu = child_node.total_score / child_node.simnum
                
                AI_M_UCT.last_id = max_id
        else:
            result = self.random_simulation(node.next[max_id].board, team_color)

            node.next[max_id].simnum += 1
            node.next[max_id].last_score = self.evaluate_state_value(result, team_color)
            node.next[max_id].total_score += node.next[max_id].last_score
            node.next[max_id].housyuu = node.next[max_id].total_score / node.next[max_id].simnum

            AI_M_UCT.last_id = max_id


    @staticmethod
    def max_rate_action(root):
        max_rate = 0
        return_id = 0

        for i, return_node in enumerate(root.next):
            if return_node.housyuu > max_rate:
                max_rate = return_node.housyuu
                return_id = i

        # print(root.board.to_string())

        return root.next[return_id].act

    def random_simulation(self, map_, team_color):
        enemy_color = AI_M_UCT.get_enemy_color(team_color)
        sim_map = map_.create_deep_clone()
        rand_gen = random.Random()

        while sim_map.get_turn_count() < sim_map.get_turn_limit():
            sim_units = sim_map.get_units_list(team_color, False, True, False)

            while len(sim_units) > 0:
                if rand_gen.random() >= 0.2 and len(MTools.get_all_attack_actions(team_color, sim_map)) > 0:
                    sim_actions = MTools.get_all_attack_actions(team_color, sim_map)
                    sim_action = random.choice(sim_actions)
                else:
                    sim_unit = random.choice(sim_units)
                    sim_actions = MTools.get_unit_actions(sim_unit, sim_map)
                    sim_action = random.choice(sim_actions)

                sim_units.remove(sim_map.get_unit(sim_action.operation_unit_id))
                sim_map.execute_action(sim_action)

            sim_map.enable_units_action(team_color)
            sim_map.inc_turn_count()

            if sim_map.get_turn_count() >= sim_map.get_turn_limit():
                break

            enemies = sim_map.get_units_list(enemy_color, False, True, False)

            while len(enemies) > 0:
                if rand_gen.random() >= 0.2 and len(MTools.get_all_attack_actions(enemy_color, sim_map)) > 0:
                    enemy_actions = MTools.get_all_attack_actions(enemy_color, sim_map)
                    enemy_action = random.choice(enemy_actions)
                else:
                    enemy_unit = random.choice(enemies)
                    enemy_actions = MTools.get_unit_actions(enemy_unit, sim_map)
                    enemy_action = random.choice(enemy_actions)

                enemies.remove(sim_map.get_unit(enemy_action.operation_unit_id))
                sim_map.execute_action(enemy_action)

            sim_map.enable_units_action(enemy_color)
            sim_map.inc_turn_count()

            if sim_map.get_turn_count() >= sim_map.get_turn_limit():
                break

            if len(sim_map.get_units_list(team_color, True, True, False)) == 0 or \
               len(sim_map.get_units_list(team_color, False, False, True)) == 0:
                break

        return sim_map

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
        if node.simnum == 0:
            return 100
        else:
            return node.housyuu + AI_M_UCT.UCB_CONST * math.sqrt(math.log(AI_M_UCT.total_simulations) / node.simnum)

    @staticmethod
    def evaluate_state_value(map_, team_color):
        enemy_color = AI_M_UCT.get_enemy_color(team_color)

        my_team_units = map_.get_units_list(team_color, True, True, False)
        enemy_team_units = map_.get_units_list(enemy_color, True, True, False)

        if len(my_team_units) == 0:
            return 0
        if len(enemy_team_units) == 0:
            return 1

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
