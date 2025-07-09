import copy
import random

from Action import Action
from ActionChecker import ActionChecker
from AutoBattleSettings import AutoBattleSettings
from Consts import Consts
from DamageCalculator import DamageCalculator
from Logger import Logger
from Logger import SGFManager
from Map import Map
from PlayerList import PlayerList


class GameManager:
    def __init__(self, form, map_file_name, raw_map_str=None):
        self.sgf_active = False
        self.map_file_name = map_file_name
        self.raw_map_str = raw_map_str
        # print(f'ginit {raw_map_str is None}')
        self.map = Map(self.map_file_name, raw_map_str=raw_map_str)
        self.phase = Consts.RED_TEAM
        self.form = form
        self.draw_manager = None
        self.human_player = None
        self.sgf_manager = None
        self.players = [None] * 2
        self.vs_server = None
        self.resignation_flag = -1
        self.net_color = -1
        self.auto_battle_flag = False
        self.network_battle_flag = False
        self.game_end_flag = False
        self.auto_battle_result_file_name = None
        self.auto_battle_combat_log_file_name = None
        self.log_flag = False

        # Fields for statistics in auto-battle mode
        self.battle_cnt_of_now = 0
        self.draw_game_cnt = 0
        self.over_max_turn_cnt = 0
        self.win_cnt_of_red = [0] * 2
        self.win_cnt_of_blue = [0] * 2
        self.draw_game_cnt_latter_half = 0
        self.over_max_turn_cnt_latter_half = 0
        self.win_cnt_of_red_latter_half = [0] * 2
        self.win_cnt_of_blue_latter_half = [0] * 2

        # Fields for statistics in auto-battle mode 2
        self.win_team = 0
        self.attack_cnt_0to5_red = 0
        self.attack_cnt_0to5_blue = 0
        self.attack_cnt_6to11_red = 0
        self.attack_cnt_6to11_blue = 0
        self.remaining_blue_num_of_all = 0
        self.remaining_red_num_of_all = 0
        self.remaining_blue_num_of_p = 0
        self.remaining_blue_num_of_i = 0
        self.remaining_blue_num_of_u = 0
        self.remaining_blue_num_of_f = 0
        self.remaining_blue_num_of_a = 0
        self.remaining_blue_num_of_r = 0
        self.remaining_red_num_of_p = 0
        self.remaining_red_num_of_i = 0
        self.remaining_red_num_of_u = 0
        self.remaining_red_num_of_f = 0
        self.remaining_red_num_of_a = 0
        self.remaining_red_num_of_r = 0
        self.first_move = None
        self.debug_string = "<none>"

    def set_sgf_manager(self, sgf_manager):
        self.sgf_manager = sgf_manager
    
    def toggle_sgf(self):
        self.sgf_active = not self.sgf_active

    def set_map(self, map):
        self.map = map

    def set_human_player(self, human_player):
        self.human_player = human_player

    def set_draw_manager(self, draw_manager):
        self.draw_manager = draw_manager
        draw_manager.set_map(self.map)

    def set_map_file_name(self, map_file_name):
        self.map_file_name = map_file_name

    def set_auto_battle_result_file_name(self, save_file_name):
        self.auto_battle_result_file_name = save_file_name

    def set_auto_battle_combat_log_file_name(self, save_file_name):
        self.auto_battle_combat_log_file_name = save_file_name

    def set_game_end_flag(self, ended):
        self.game_end_flag = True

    def get_turn_count(self):
        return self.map.get_turn_count()

    def init_game(self, log_flag=False):
        self.log_flag = log_flag
        self.players[0] = PlayerList.get_player(self.form.get_player(Consts.RED_TEAM))
        self.players[1] = PlayerList.get_player(self.form.get_player(Consts.BLUE_TEAM))

        if self.auto_battle_flag:
            self.map = Map(self.map_file_name, raw_map_str=self.raw_map_str)
            # self.make_random_noise()
            self.map.set_turn_count(0)

        self.first_move = None
        self.debug_string = "<none>"

        self.phase = self.map.get_turn_count() % 2
        self.sgf_manager.set_initialize_map(self.map)
        self.sgf_manager.set_player_name(self.players[0].get_name(), self.players[1].get_name())
        # self.draw_manager.redraw_map(self.map)
        
        return self.execute_game()

    def enable_auto_battle(self):
        self.auto_battle_flag = True

    def new_game(self):
        if (self.auto_battle_flag and self.battle_cnt_of_now >= AutoBattleSettings.NumberOfGamesPerMap // 2 and
            AutoBattleSettings.IsPosChange):
            self.map = Map(self.map_file_name, True, raw_map_str=self.raw_map_str)
            self.phase = Consts.BLUE_TEAM
        else:
            self.map = Map(self.map_file_name, raw_map_str=self.raw_map_str)
            self.phase = Consts.RED_TEAM

        self.make_random_noise()
        self.map.set_turn_count(0)

        self.sgf_manager.set_initialize_map(self.map)
        self.sgf_manager.set_player_name(self.players[0].get_name(), self.players[1].get_name())
        # self.draw_manager.redraw_map(self.map)
        self.resignation_flag = -1

        # Initialize statistics fields
        self.attack_cnt_0to5_red = 0
        self.attack_cnt_0to5_blue = 0
        self.attack_cnt_6to11_red = 0
        self.attack_cnt_6to11_blue = 0
        self.remaining_blue_num_of_p = 0
        self.remaining_blue_num_of_i = 0
        self.remaining_blue_num_of_u = 0
        self.remaining_blue_num_of_f = 0
        self.remaining_blue_num_of_a = 0
        self.remaining_blue_num_of_r = 0
        self.remaining_red_num_of_p = 0
        self.remaining_red_num_of_i = 0
        self.remaining_red_num_of_u = 0
        self.remaining_red_num_of_f = 0
        self.remaining_red_num_of_a = 0
        self.remaining_red_num_of_r = 0
        self.remaining_red_num_of_all = 0
        self.remaining_blue_num_of_all = 0

    def execute_game(self):
        while self.battle_cnt_of_now < AutoBattleSettings.NumberOfGamesPerMap:
            self.inquire_ai(self.players[self.phase], self.phase)

            self.change_phase()

            if (self.is_all_unit_dead() or
                self.map.get_turn_count() == self.map.get_turn_limit() or
                self.resignation_flag >= 0):
                self.game_end_phase()
                self.new_game()
            if self.battle_cnt_of_now == AutoBattleSettings.NumberOfGamesPerMap // 2:
                if self.log_flag:
                    Logger.log_file(f"{self.map_file_name},{self.win_cnt_of_red[0]},{self.win_cnt_of_blue[0]},{self.draw_game_cnt},{self.first_move}\n")
                # return f"{self.map_file_name},{self.win_cnt_of_red[0]},{self.win_cnt_of_blue[0]},{self.draw_game_cnt},{self.first_move}\n"
                return Logger.return_game_log(self.win_cnt_of_red[0], self.win_cnt_of_blue[0], self.draw_game_cnt)


    def is_all_unit_dead(self):
        if self.map.get_num_of_alive_color_units(Consts.RED_TEAM) == 0:
            return True
        if self.map.get_num_of_alive_color_units(Consts.BLUE_TEAM) == 0:
            return True
        return False

    def change_phase(self):
        self.map.enable_units_action(self.phase)
        # self.draw_manager.redraw_map(self.map)
        if self.sgf_active:
            self.sgf_manager.add_log_of_one_turn(self.phase)
        self.map.inc_turn_count()
        self.phase = (self.phase + 1) % 2

    def inquire_ai(self, ai_player, team_color):
        copy_map = None
        turn_end_flag = False
        turn_start_flag = False
        game_start_flag = False
        num_of_color_units = len(self.map.get_units_list(self.phase, False, True, False))

        for i in range(num_of_color_units):
            if i == 0:
                turn_start_flag = True
            else:
                turn_start_flag = False
            if turn_start_flag and (self.map.get_turn_count() == 1 or self.map.get_turn_count() == 0):
                game_start_flag = True
            else:
                game_start_flag = False

            if self.is_all_unit_dead():
                # self.draw_manager.redraw_map(self.map)
                return

            copy_map = self.map.create_deep_clone()

            act = None

            if not turn_end_flag:
                # Let the AI generate an action (generate an action for 1 unit)
                act = self.players[self.phase].make_action(copy_map, self.phase, turn_start_flag, game_start_flag)
                if not ActionChecker.is_the_action_legal_move(act, self.map):
                    print(self.map.map_to_string())
                    print(self.map.raw_map)
                    print(f"Action: {act.to_string()}")
                    print(f"Color: {team_color}")
                    raise Exception("Illegal move")
                
                # print(self.map.to_string())
                # print(f"Action: {act.to_string()}")

                if act.action_type == Action.ACTIONTYPE_TURNEND:
                    turn_end_flag = True

                if act.action_type == Action.ACTIONTYPE_SURRENDER:
                    turn_end_flag = True
                    self.resignation_flag = team_color

            if turn_end_flag:
                unit = self.map.get_units_list(self.phase, False, True, False)[0]
                act = Action.create_move_only_action(unit, unit.get_x_pos(), unit.get_y_pos())

            if self.first_move is None:
                self.first_move = act
            Logger.add_turn_record(self.map, act, self.phase)

            if self.sgf_active:
                SGFManager.record_comment()

            # Apply the action to the map
            self.map.change_unit_location(act.destination_x_pos, act.destination_y_pos, self.map.get_unit(act.operation_unit_id))
            self.map.get_unit(act.operation_unit_id).set_action_finished(True)
            # self.draw_manager.redraw_map(self.map)
            # If the action is an attack type, perform battle
            if act.target_unit_id != -1:
                self.battle_phase(act)
            # self.draw_manager.redraw_map(self.map)

            if self.sgf_active:
                self.sgf_manager.add_unit_action(act)


    def game_end_phase(self):
        # Collect the ratio of remaining units
        units = self.map.get_units()
        red_team_units = self.map.get_units_list(Consts.RED_TEAM, True, True, False)
        blue_team_units = self.map.get_units_list(Consts.BLUE_TEAM, True, True, False)

        for unit in red_team_units:
            self.remaining_red_num_of_all += unit.get_HP()
        for unit in blue_team_units:
            self.remaining_blue_num_of_all += unit.get_HP()

        for unit in units:
            if unit is None:
                continue
            if unit.get_team_color() == 0:
                unit_type = unit.get_type_of_unit()
                if unit_type == 0:
                    self.remaining_red_num_of_f += unit.get_HP()
                elif unit_type == 1:
                    self.remaining_red_num_of_a += unit.get_HP()
                elif unit_type == 2:
                    self.remaining_red_num_of_p += unit.get_HP()
                elif unit_type == 3:
                    self.remaining_red_num_of_u += unit.get_HP()
                elif unit_type == 4:
                    self.remaining_red_num_of_r += unit.get_HP()
                elif unit_type == 5:
                    self.remaining_red_num_of_i += unit.get_HP()
            else:
                unit_type = unit.get_type_of_unit()
                if unit_type == 0:
                    self.remaining_blue_num_of_f += unit.get_HP()
                elif unit_type == 1:
                    self.remaining_blue_num_of_a += unit.get_HP()
                elif unit_type == 2:
                    self.remaining_blue_num_of_p += unit.get_HP()
                elif unit_type == 3:
                    self.remaining_blue_num_of_u += unit.get_HP()
                elif unit_type == 4:
                    self.remaining_blue_num_of_r += unit.get_HP()
                elif unit_type == 5:
                    self.remaining_blue_num_of_i += unit.get_HP()

        # End due to annihilation
        if self.map.get_num_of_alive_color_units(Consts.RED_TEAM) == 0:
            Logger.log(Consts.TEAM_NAMES[Consts.RED_TEAM] + " has been annihilated.\r\n", 0)
            Logger.show_game_result(Consts.BLUE_TEAM)
            if self.auto_battle_flag:
                self.win_cnt_of_blue[0] += 1
            if self.auto_battle_flag:
                self.battle_cnt_of_now += 1
            self.win_team = Consts.BLUE_TEAM
            return
        elif self.map.get_num_of_alive_color_units(Consts.BLUE_TEAM) == 0:
            Logger.log(Consts.TEAM_NAMES[Consts.BLUE_TEAM] + " has been annihilated.\r\n", 0)
            Logger.show_game_result(Consts.RED_TEAM)
            if self.auto_battle_flag:
                self.win_cnt_of_red[0] += 1
            if self.auto_battle_flag:
                self.battle_cnt_of_now += 1
            self.win_team = Consts.RED_TEAM
            return

        # End due to resignation
        if self.resignation_flag == Consts.RED_TEAM:
            Logger.log(Consts.TEAM_NAMES[Consts.RED_TEAM] + " has resigned.\r\n", 0)
            Logger.show_game_result(Consts.BLUE_TEAM)
            if self.auto_battle_flag:
                self.win_cnt_of_blue[0] += 1
            if self.auto_battle_flag:
                self.battle_cnt_of_now += 1
            self.win_team = Consts.BLUE_TEAM
            return
        if self.resignation_flag == Consts.BLUE_TEAM:
            Logger.log(Consts.TEAM_NAMES[Consts.BLUE_TEAM] + " has resigned.\r\n", 0)
            Logger.show_game_result(Consts.RED_TEAM)
            if self.auto_battle_flag:
                self.win_cnt_of_red[0] += 1
            if self.auto_battle_flag:
                self.battle_cnt_of_now += 1
            self.win_team = Consts.RED_TEAM
            return

        # In case the total number of turns has exceeded
        if self.map.get_turn_count() == self.map.get_turn_limit():
            self.over_max_turn_cnt += 1
            Logger.log("The maximum number of turns has been exceeded. Victory will be determined based on the remaining units.\r\n", 0)
            win_team_color = self.judge_by_remain_hp()
            self.win_team = win_team_color

            if win_team_color == Consts.RED_TEAM:
                Logger.show_game_result(Consts.RED_TEAM)
                self.win_cnt_of_red[1] += 1
                self.win_cnt_of_red[0] += 1
            elif win_team_color == Consts.BLUE_TEAM:
                Logger.show_game_result(Consts.BLUE_TEAM)
                self.win_cnt_of_blue[1] += 1
                self.win_cnt_of_blue[0] += 1
            elif win_team_color == -1:
                self.draw_game_cnt += 1

            if self.auto_battle_flag:
                self.battle_cnt_of_now += 1
            return

    def judge_by_remain_hp(self):
        win_team_color = -1
        sum_of_red_team_hp = 0
        sum_of_blue_team_hp = 0
        red_team_units = self.map.get_units_list(Consts.RED_TEAM, True, True, False)
        blue_team_units = self.map.get_units_list(Consts.BLUE_TEAM, True, True, False)

        for unit in red_team_units:
            sum_of_red_team_hp += unit.get_HP()

        for unit in blue_team_units:
            sum_of_blue_team_hp += unit.get_HP()

        if sum_of_red_team_hp > sum_of_blue_team_hp + self.map.get_draw_hp_threshold():
            win_team_color = Consts.RED_TEAM
        elif sum_of_blue_team_hp > sum_of_red_team_hp + self.map.get_draw_hp_threshold():
            win_team_color = Consts.BLUE_TEAM

        return win_team_color

    def battle_phase(self, act):
        if act.action_type == Action.ACTIONTYPE_MOVEANDATTACK:
            units = self.map.get_units()
            self.battle(units[act.operation_unit_id], units[act.target_unit_id], act)

    def battle(self, attack_unit, target_unit, act):
        attack_damages = DamageCalculator.calculate_damages_unit_map(attack_unit, target_unit, self.map)
        counter_damage = 0
        target_unit.reduce_HP(attack_damages[0])
        act.X_attack_damage = attack_damages[0]

        if self.map.get_turn_count() < 6:
            if attack_unit.get_team_color() == 0:
                self.attack_cnt_0to5_red += 1
            else:
                self.attack_cnt_0to5_blue += 1
        if 5 < self.map.get_turn_count() < 12:
            if attack_unit.get_team_color() == 0:
                self.attack_cnt_6to11_red += 1
            else:
                self.attack_cnt_6to11_blue += 1

        if target_unit.get_HP() == 0:
            counter_damage = 0
            act.X_counter_damage = counter_damage
            self.unit_delete_process(target_unit)
        elif target_unit.get_HP() > 0 and attack_unit.get_spec().is_direct_attack_type() and target_unit.get_spec().is_direct_attack_type():
            counter_damage = attack_damages[1]
            act.X_counter_damage = counter_damage
            attack_unit.reduce_HP(counter_damage)

            if attack_unit.get_HP() == 0:
                self.unit_delete_process(attack_unit)
        else:
            act.X_counter_damage = 0

    def unit_delete_process(self, dead_unit):
        self.map.delete_unit(dead_unit)
        my_text = dead_unit.get_name() + str(dead_unit.get_ID()) + " has been destroyed\r\n"
        my_text += "\n"

    def undo_one_action(self):
        undo_map = self.sgf_manager.undo_one_action(self.map)
        if undo_map is None:
            return
        self.set_map(undo_map)
        # self.draw_manager.set_map(undo_map)
        self.human_player.init_action()
        self.human_player.init_mouse_state()
        # self.draw_manager.redraw_map(self.map)

    def undo(self):
        undo_map = self.sgf_manager.undo(self.map)
        if undo_map is None:
            return
        self.set_map(undo_map)
        # self.draw_manager.set_map(undo_map)
        self.human_player.init_action()
        self.human_player.init_mouse_state()
        # self.draw_manager.redraw_map(self.map)

    def make_random_noise(self):
        units = self.map.get_units()
        r = random.Random()

        for unit in units:
            if unit is None:
                continue

            if AutoBattleSettings.IsPositionRandomlyMoved:
                rn = r.randint(0, 3)
                x_pos = unit.get_x_pos()
                y_pos = unit.get_y_pos()

                if rn == 0 and self.is_unit_can_move(x_pos - 1, y_pos, unit):
                    self.map.change_unit_location(x_pos - 1, y_pos, unit)
                elif rn == 1 and self.is_unit_can_move(x_pos + 1, y_pos, unit):
                    self.map.change_unit_location(x_pos + 1, y_pos, unit)
                elif rn == 2 and self.is_unit_can_move(x_pos, y_pos - 1, unit):
                    self.map.change_unit_location(x_pos, y_pos - 1, unit)
                elif rn == 3 and self.is_unit_can_move(x_pos, y_pos + 1, unit):
                    self.map.change_unit_location(x_pos, y_pos + 1, unit)

            if AutoBattleSettings.IsHPRandomlyDecreased:
                rn = r.randint(0, 1)

                if rn == 0 and unit.get_HP() <= 9:
                    unit.raise_HP(1)
                elif rn == 1 and unit.get_HP() >= 2:
                    unit.reduce_HP(1)

    def is_unit_can_move(self, x_pos, y_pos, select_unit):
        if self.map.get_unit_at(x_pos, y_pos) is not None:
            return False

        if select_unit.get_spec().get_move_cost(self.map.get_field_type(x_pos, y_pos)) == 99:
            return False

        return True

    def set_vs_server(self, server):
        self.vs_server = server

    def increment_draw_cnt(self):
        if self.map.reverse:
            self.draw_game_cnt_latter_half += 1
        else:
            self.draw_game_cnt += 1

    def increment_over_max_turn_cnt(self):
        if self.map.reverse:
            self.over_max_turn_cnt_latter_half += 1
        else:
            self.over_max_turn_cnt += 1

    def increment_win_cnt_of_red(self, index):
        if self.map.reverse:
            self.win_cnt_of_red_latter_half[index] += 1
        else:
            self.win_cnt_of_red[index] += 1

    def increment_win_cnt_of_blue(self, index):
        if self.map.reverse:
            self.win_cnt_of_blue_latter_half[index] += 1
        else:
            self.win_cnt_of_blue[index] += 1