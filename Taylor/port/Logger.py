import os
import AutoBattleSettings
from Consts import Consts
from Action import Action

# from Settings
class Logger:

    verbose = False

    @staticmethod
    def add_log_message(message, team_color):
        Logger.log(message)
        SGFManager.store_comment(message)

    @staticmethod
    def log_comment_to_sgf(message):
        """
        Add log only to game record
        :param message: String to display
        """
        SGFManager.store_comment(message)

    @staticmethod
    def log(message, team_color=0):
        """
        Display string in text box
        :param message: String to display
        """
        if Logger.verbose:
            print(message)

    # @staticmethod
    # def netlog(message, team_color):
    #     """
    #     Log output for network battle
    #     :param message: String to display
    #     """
    #     if Settings.showing_net_log:
    #         if team_color == 0:
    #             Logger.s_blue_text_box.insert(tk.END, message)
    #         else:
    #             Logger.s_red_text_box.insert(tk.END, message)

    @staticmethod
    def clear_text_box():
        pass
    
    @staticmethod
    def clear_blue_text_box():
        pass

    @staticmethod
    def clear_red_text_box():
        pass

    @staticmethod
    def clear_box_and_print_log(message):
        Logger.log(message)

    @staticmethod
    def show_dialog_message(message):
        Logger.log(message)

    @staticmethod
    def show_unit_status(message):
        Logger.log(message)

    @staticmethod
    def show_game_result(team_color):
        Logger.log(f"{Consts.TEAM_NAMES[team_color]} wins.")

    @staticmethod
    def show_auto_battle_result(file_name, map_name, red_player_name, blue_player_name, battle_cnt, win_cnt_of_red,
                                win_cnt_of_blue, draw_cnt, over_turn_cnt):
        """
        Display the result of auto battle in the text box. Can also output to an external csv file.
        """
        result_str = f"\nNumber of battles: {battle_cnt}\n"
        result_str += f"Red victories: {win_cnt_of_red[0]}\n"
        result_str += f"Blue victories: {win_cnt_of_blue[0]}\n"
        result_str += f"Draws: {draw_cnt}\n"
        result_str += f"Turn limit exceeded: {over_turn_cnt}\n"
        result_str += f"Red victories by turn limit: {win_cnt_of_red[1]}\n"
        result_str += f"Blue victories by turn limit: {win_cnt_of_blue[1]}\n"
        Logger.log(result_str)

        write_title_flag = False
        if not os.path.exists(file_name):
            write_title_flag = True

        # Use this when you want to output to an external file
        with open(file_name, 'a') as file:
            if write_title_flag:
                file.write("MapName,RedPlayerName,BluePlayerName,BattleCnt,WinCntOfRed,WinCntOfBlue,DrawCnt,OverTrunCnt,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos2\n")
            file.write(f"{map_name},{red_player_name},{blue_player_name},{battle_cnt},"
                       f"{win_cnt_of_red[0]},{win_cnt_of_blue[0]},{draw_cnt},{over_turn_cnt},"
                       f"{win_cnt_of_red[1]},{win_cnt_of_blue[1]},"
                       f"{AutoBattleSettings.IsHPRandomlyDecreased},{AutoBattleSettings.IsPositionRandomlyMoved}\n")

    @staticmethod
    def show_auto_battle_result_2(file_name, map_name, red_player_name, blue_player_name, battle_cnt,
                                  win_cnt_of_red, win_cnt_of_blue, draw_cnt, over_turn_cnt,
                                  win_cnt_of_red_latter, win_cnt_of_blue_latter, draw_cnt_latter, over_turn_cnt_latter):
        """
        ※※ Used when the player order is reversed in the first and second halves ※※
        Display the result of auto battle in the text box. Can also output to an external csv file.
        """
        result_str = f"\nTotal battles: {battle_cnt}\n"
        result_str += f"[First half] ({battle_cnt // 2}/{battle_cnt} battles)\n"
        result_str += f"Red victories: {win_cnt_of_red[0]}\n"
        result_str += f"Blue victories: {win_cnt_of_blue[0]}\n"
        result_str += f"Draws: {draw_cnt}\n"
        result_str += f"Turn limit exceeded: {over_turn_cnt}\n"
        result_str += f"Red victories by turn limit: {win_cnt_of_red[1]}\n"
        result_str += f"Blue victories by turn limit: {win_cnt_of_blue[1]}\n"
        result_str += f"[Second half] ({(battle_cnt + 1) // 2}/{battle_cnt} battles)\n"
        result_str += f"Red victories: {win_cnt_of_red_latter[0]}\n"
        result_str += f"Blue victories: {win_cnt_of_blue_latter[0]}\n"
        result_str += f"Draws: {draw_cnt_latter}\n"
        result_str += f"Turn limit exceeded: {over_turn_cnt_latter}\n"
        result_str += f"Red victories by turn limit: {win_cnt_of_red_latter[1]}\n"
        result_str += f"Blue victories by turn limit: {win_cnt_of_blue_latter[1]}\n"
        Logger.log(result_str)

        write_title_flag = False
        if not os.path.exists(file_name):
            write_title_flag = True

        # Use this when you want to output to an external file
        with open(file_name, 'a') as file:
            if write_title_flag:
                file.write("MapName,RedPlayerName,BluePlayerName,BattleCnt,"
                           "WinCntOfRed,WinCntOfBlue,"
                           "DrawCnt,OverTrunCnt,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos1\n")
            # Record of the first half before the player order is reversed
            file.write(f"{map_name},{red_player_name},{blue_player_name},{battle_cnt // 2},"
                       f"{win_cnt_of_red[0]},{win_cnt_of_blue[0]},"
                       f"{draw_cnt},{over_turn_cnt},"
                       f"{win_cnt_of_red[1]},{win_cnt_of_blue[1]},"
                       f"{AutoBattleSettings.IsHPRandomlyDecreased},{AutoBattleSettings.IsPositionRandomlyMoved}\n")
            # Record of the second half after the player order is reversed
            file.write(f"{map_name},{red_player_name},{blue_player_name},{(battle_cnt + 1) // 2},"
                       f"{win_cnt_of_red_latter[0]},{win_cnt_of_blue_latter[0]},"
                       f"{draw_cnt_latter},{over_turn_cnt_latter},"
                       f"{win_cnt_of_red_latter[1]},{win_cnt_of_blue_latter[1]},"
                       f"{AutoBattleSettings.IsHPRandomlyDecreased},{AutoBattleSettings.IsPositionRandomlyMoved}\n")

    @staticmethod
    def save_combat_log(file_name, win_team, resignation_flag, turn_of_game_end,
                        attack_cnt_0_to_5_r, attack_cnt_0_to_5_b, attack_cnt_5_to_10_r, attack_cnt_5_to_10_b,
                        blue_unit, red_unit,
                        blue_f, blue_a, blue_p, blue_u, blue_r, blue_i,
                        red_f, red_a, red_p, red_u, red_r, red_i):
        write_title_flag = False
        if not os.path.exists(file_name):
            write_title_flag = True

        # Use this when you want to output to an external file
        with open(file_name, 'a') as file:
            if write_title_flag:
                file.write("WinTeam,Resing,EndTurn,AttackCntBlue_0_5,AttackCntRed_0_5,attackCntBlue_6_11,attackCntRed_6_11,AllUnitHP_Blue,AllUnitHP_Red,"
                           "F_UnitHP_Blue,A_UnitHP_Blue,P_UnitHP_Blue,U_UnitHP_Blue,R_UnitHP_Blue,I_UnitHP_Blue,"
                           "F_UnitHP_Red,A_UnitHP_Red,P_UnitHP_Red,U_UnitHP_Red,R_UnitHP_Red,I_UnitHP_Red\n")
            file.write(f"{win_team},{resignation_flag > -1},{turn_of_game_end},{attack_cnt_0_to_5_b},{attack_cnt_0_to_5_r},{attack_cnt_5_to_10_b},{attack_cnt_5_to_10_r},{blue_unit},{red_unit},"
                       f"{blue_f},{blue_a},{blue_p},{blue_u},{blue_r},{blue_i},"
                       f"{red_f},{red_a},{red_p},{red_u},{red_r},{red_i}\n")

    # @staticmethod
    # def show_array_contents(array):
    #     """
    #     Used for debugging 2D arrays
    #     """
    #     result_str = ""
    #     for row in array:
    #         for item in row:
    #             result_str += f"{item:3}"
    #         result_str += "\n"
    #     messagebox.showinfo("Array Contents", result_str)

    # have persistant file open so no need to reopen it after each function call
    active_file = None

    @staticmethod
    def log_battle_result_file(file_name, map_name, win_cnt_of_red, win_cnt_of_blue, draw_cnt, first_move, debug_str):
        write_title_flag = False
        if not os.path.exists(file_name):
            write_title_flag = True
            # make file
            Logger.active_file = open(file_name, 'w')
            Logger.active_file.write("MapName,WinCntOfRed,WinCntOfBlue,DrawCnt,FirstMove\n")
        Logger.active_file.write(f"{map_name},{win_cnt_of_red[0]},{win_cnt_of_blue[0]},{draw_cnt},{first_move}\n")

    @staticmethod
    def print_battle_result(file_name, map_name, red_player_name, blue_player_name, battle_cnt,
                                                  win_cnt_of_red, win_cnt_of_blue, draw_cnt, over_turn_cnt,
                                                  win_cnt_of_red_latter, win_cnt_of_blue_latter, draw_cnt_latter,
                                                  over_turn_cnt_latter, first_move, debug_str):
        write_title_flag = False
        if not os.path.exists(file_name):
            write_title_flag = True

        # Use this when you want to output to an external file
        with open(file_name, 'a') as file:
            if write_title_flag:
                file.write("MapName,RedPlayerName,BluePlayerName,BattleCnt,"
                           "WinCntOfRed,WinCntOfBlue,"
                           "DrawCnt,OverTrunCnt,FirstMove,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos1\n")
            # Record of the first half before the player order is reversed
            file.write(f"{map_name},{red_player_name},{blue_player_name},{battle_cnt // 2},"
                       f"{win_cnt_of_red[0]},{win_cnt_of_blue[0]},"
                       f"{draw_cnt},{over_turn_cnt},{first_move},"
                       f"{win_cnt_of_red[1]},{win_cnt_of_blue[1]},"
                       f"{AutoBattleSettings.IsHPRandomlyDecreased},{AutoBattleSettings.IsPositionRandomlyMoved}\n")
            # Record of the second half after the player order is reversed
            file.write(f"{map_name},{red_player_name},{blue_player_name},{(battle_cnt + 1) // 2},"
                       f"{win_cnt_of_red_latter[0]},{win_cnt_of_blue_latter[0]},"
                       f"{draw_cnt_latter},{over_turn_cnt_latter},{first_move},"
                       f"{win_cnt_of_red_latter[1]},{win_cnt_of_blue_latter[1]},"
                       f"{AutoBattleSettings.IsHPRandomlyDecreased},{AutoBattleSettings.IsPositionRandomlyMoved}\n")
    
    @staticmethod
    def flush():
        Logger.active_file.flush()
            






























class SGFManager:
    str_buffer = ""  # Buffer to accumulate comments
    action_list_of_the_turn = []  # List to store actions in one turn
    action_lists = []  # List of action lists, should be in a separate class
    comment_list = []  # List of comments to be saved in the game record

    def __init__(self):
        self.player_names = ["", ""]  # Player names, required for saving game records
        self.file_name = ""  # Loaded game record file name
        self.limit_time = 0  # Game end time limit, may not be necessary
        self.date_time = None  # Current time, date, and elapsed time since the game started
        self.team_color_of_action_list = []  # Array to record the team color of the action list (for each turn)
        self.map = None  # Map for game record playback purpose
        self.initialize_map = None  # Initial state of the map, saved here at the start of the game
        self.temp_map_file = ""  # Temporary map file, selected at the start of the game
        self.step_cnt = 0  # Counter used when replaying the game record from the beginning
        self.turn_cnt = 0  # Counter used when replaying the game record from the beginning
        self.all_step_log = None  # List of all actions from the beginning when replaying the game record

    def set_player_name(self, player1, player2):
        """Set the player names (currently not used)"""
        self.player_names[0] = player1
        self.player_names[1] = player2

    def set_date_time(self, d_time):
        """Set the DateTime when the game starts"""
        self.date_time = d_time

    def set_limit_time(self, t):
        self.limit_time = t

    def set_initialize_map(self, a_map):
        """Set the initial state of the map
        Called from GameManager's initGame"""
        self.initialize_map = a_map.create_deep_clone()

    def set_map(self, map):
        self.map = map

    def set_file_name(self, f_name):
        self.file_name = f_name

    def set_temp_map(self, map_file):
        self.temp_map_file = map_file

    def clear_action_lists(self):
        self.action_list_of_the_turn = []
        self.action_lists = []
        self.comment_list = []

    def get_update(self):
        """Get the current action list"""
        return self.action_list_of_the_turn

    def get_update_log(self):
        """Get the log of action lists"""
        return self.action_lists

    def get_initialize_map(self):
        return self.initialize_map

    def get_player_name(self, index):
        if index == 0:
            return self.player_names[0]
        elif index == 1:
            return self.player_names[1]
        else:
            Logger.show_dialog_message("Invalid player index.")
            return None

    def save_autobattle_file(self):
        map_file = self.to_string_map_file(self.initialize_map)

    def save_action_lists_to_file(self):
        with open(self.file_name, "w") as sw:
            with open(self.temp_map_file, "r", encoding="shift_jis") as sr:
                for line in sr:
                    sw.write(line)

                # get datetime
                dt = self.date_time.strftime("%Y-%m-%d %H:%M:%S")
                sw.write(f"PLAYEDDATE[{dt}];\n")
                sw.write(f"PLAYEDRULE[{Consts.RULE_SET_VERSION}];\n")
                sw.write(f"PLAYERRED[{self.player_names[0]}]PLAYERBLUE[{self.player_names[1]}];\n")

                self.save_action_lists_to_file_helper(sw)

    def save_action_lists_to_file_helper(self, sw):
        for i in range(len(self.action_lists)):
            team_color = self.team_color_of_action_list[i]
            li_update = self.action_lists[i]
            num_of_actions = len(li_update)

            sw.write(f"TURNINFO[{i},{team_color},{num_of_actions}];\n")

            for j in range(len(li_update)):
                sw.write(f"ACTION[{li_update[j].to_sgf_string()}];\n")

                for cSt in self.comment_list:
                    if cSt.turn_cnt == i and cSt.step_cnt == j:
                        sw.write(f"COM[{cSt.comment}{self.get_all_step_log_cnt(cSt)}];\n")

        if len(self.action_list_of_the_turn) != 0:
            sw.write(f"TURNINFO[{len(self.action_lists)},{self.action_list_of_the_turn[0].team_color},{len(self.action_list_of_the_turn)}];\n")

            for j in range(len(self.action_list_of_the_turn)):
                sw.write(f"ACTION[{self.action_list_of_the_turn[j].to_sgf_string()}];\n")

                for cSt in self.comment_list:
                    if cSt.turn_cnt == len(self.action_lists) and cSt.step_cnt == j:
                        sw.write(f"COM[{cSt.comment}{self.get_all_step_log_cnt(cSt)}];\n")

    def get_all_step_log_cnt(self, cSt):
        cnt = 0

        for i in range(cSt.turn_cnt):
            cnt += len(self.action_lists[i])

        cnt += cSt.step_cnt

        return cnt

    def load_map_state_from_sgf(self, file_name):
        """Load the map state from a file (sgf, tbsmap)
        However, since the loaded intermediate state is treated as the initial state, the action log is deleted
        This means only UNDO up to the loaded point can be used"""
        self.load_action_from_file(file_name)
        li_act = self.action_lists[-1]
        self.go_back_next_turn(len(self.action_lists) - 1)

        for i in range(len(li_act)):
            self.map.act_control(li_act[i])

        self.delete_log()
        self.delete_unit_action()

    def load_map_state(self):
        """Internal function of LoadMapStateFromSgf, used for replay
        Create an intermediate state of the map"""
        self.turn_cnt = len(self.action_lists) - 1
        if self.turn_cnt < 0:
            return
        self.replay_next_turn()

    def load_action_from_file(self, file_name):
        """Load the action log from a file"""
        with open(file_name, "r") as sr:
            full_line = sr.read()

        self.all_step_log = []
        lines = full_line.split(';')
        self.comment_list = []

        for i in range(len(lines)):
            one = self.search_one(lines[i], "TURNINFO")
            if one != "":
                ones = one.split(',')
                act_list_of_turn = self.search_step(i, lines, int(ones[1]), int(ones[2]))
                self.action_lists.append(act_list_of_turn)

            one_comment = self.search_one(lines[i], "MAPCOM")
            if one_comment != "":
                continue

            one_comment = self.search_one(lines[i], "COM")

            if one_comment != "":
                ones = one_comment.split(',')
                step_cnt = int(ones[-1])

                for j in range(len(ones) - 1):
                    self.comment_list.append(CommentStruct(0, step_cnt, ones[j]))

    def search_step(self, s_index, lines, team_color, num_of_actions):
        """Read steps (unit actions) from the game record"""
        self.delete_unit_action()
        act_list_of_turn = []
        cmt_cnt_in_turn = 0

        k = s_index + 1
        while True:
            one = self.search_one(lines[k], "TURNINFO")
            com_one = self.search_one(lines[k], "COM")

            if com_one != "":
                cmt_cnt_in_turn += 1
            k += 1
            if one != "" or k >= len(lines):
                break

        for i in range(s_index + 1, num_of_actions + cmt_cnt_in_turn + s_index + 1):
            one = self.search_one(lines[i], "ACTION")

            if one == "":
                continue

            a = Action.Parse(one)
            a.team_color = team_color
            self.all_step_log.append(a)
            act_list_of_turn.append(a)

        return act_list_of_turn

    def undo(self, real_map):
        """Go back to the previous turn (like 'undo' in Shogi and other games)
        Called from GameManager
        The Map argument is the Map being manipulated by HumanPlayer in GameManager"""
        if len(self.action_lists) <= 1 or len(self.action_lists) == 0:
            Logger.show_dialog_message("Cannot go back any further")
            return None

        undo_map = real_map
        undo_map.set_turn_count(0)

        self.initialize_map.create_deep_clone(undo_map)

        for i in range(len(self.action_lists) - 2):
            self.action_list_of_the_turn = self.action_lists[i]

            for j in range(len(self.action_list_of_the_turn)):
                undo_map.act_control(self.action_list_of_the_turn[j])
            undo_map.inc_turn_count()
            undo_map.reset_action_finish()

        if len(self.action_lists) == 2:
            self.delete_log()
            return undo_map

        self.action_lists.pop(len(self.action_lists) - 2)
        self.action_lists.pop(len(self.action_lists) - 1)
        self.delete_unit_action()

        return undo_map

    def undo_one_action(self, real_map):
        """Go back to the state before completing one action (one unit's action)
        Called from GameManager
        The Map argument is the Map being manipulated by HumanPlayer in GameManager"""
        undo_map = real_map

        if len(self.action_list_of_the_turn) == 0:
            Logger.show_dialog_message("Cannot go back any further")
            return None

        self.initialize_map.create_deep_clone(undo_map)

        for i in range(len(self.action_lists)):
            update = self.action_lists[i]

            for j in range(len(update)):
                undo_map.act_control(update[j])
            undo_map.inc_turn_count()
            undo_map.reset_action_finish()

        for j in range(len(self.action_list_of_the_turn) - 1):
            undo_map.act_control(self.action_list_of_the_turn[j])

        self.action_list_of_the_turn.pop()

        return undo_map

    def replay_next_step(self):
        """Move to the next step during replay"""
        if self.step_cnt > len(self.all_step_log) - 1:
            return
        self.initialize_map.create_deep_clone(self.map)
        self.show_comment(self.step_cnt)
        self.step_cnt += 1
        self.go_back_next_step(self.step_cnt, self.all_step_log)

    def replay_back_step(self):
        """Move to the previous step during replay"""
        if self.step_cnt < 1:
            return
        self.initialize_map.create_deep_clone(self.map)
        self.show_comment(self.step_cnt)
        self.step_cnt -= 1
        self.go_back_next_step(self.step_cnt, self.all_step_log)

    def go_back_next_step(self, s_index, act_log):
        """Replay function for game record, replay the unit action log from the initial state up to sIndex"""
        color_of_control_unit = act_log[0].team_color
        self.map.set_turn_count(0)
        self.turn_cnt = 0

        for i in range(s_index):
            if color_of_control_unit != act_log[i].team_color:
                color_of_control_unit = act_log[i].team_color
                self.map.reset_action_finish()
                self.map.inc_turn_count()
                self.turn_cnt += 1

            self.map.act_control(act_log[i])

    def show_comment(self, step_cnt):
        """Display comments saved in the game record"""
        for i in range(len(self.comment_list)):
            cSt = self.comment_list[i]

            if cSt.step_cnt == step_cnt:
                Logger.log(cSt.comment + "\r\n", 0)

    def replay_next_turn(self):
        """Move to the next turn during replay"""
        if self.turn_cnt > len(self.action_lists) - 1:
            return
        if self.turn_cnt < 0:
            self.turn_cnt = 0
        self.initialize_map.create_deep_clone(self.map)
        self.turn_cnt += 1
        self.go_back_next_turn(self.turn_cnt)

    def replay_back_turn(self):
        """Move to the previous turn during replay"""
        if self.turn_cnt < 1:
            return
        if self.turn_cnt > len(self.action_lists) - 1:
            self.turn_cnt = len(self.action_lists) - 1
        self.initialize_map.create_deep_clone(self.map)
        self.turn_cnt -= 1
        self.go_back_next_turn(self.turn_cnt)

    def go_back_next_turn(self, s_index_turn):
        """Replay function for game record, replay the unit action log from the initial state up to sIndexTurn"""
        self.map.set_turn_count(0)
        self.step_cnt = 0
        for i in range(s_index_turn):
            action_list_of_the_turn = self.action_lists[i]
            for j in range(len(action_list_of_the_turn)):
                self.map.act_control(action_list_of_the_turn[j])
                self.step_cnt += 1
            self.map.inc_turn_count()
            self.map.reset_action_finish()

    def add_unit_action(self, act):
        """Add one unit's action
        When a unit's action is determined in GameManager,
        save that action"""
        self.action_list_of_the_turn.append(act)

    def add_log_of_one_turn(self, team_color):
        """Add the set of actions in one turn to the List
        When one turn's actions are determined in GameManager,
        store each unit's action list further in a list"""
        self.action_lists.append(self.action_list_of_the_turn)
        self.action_list_of_the_turn = []
        self.team_color_of_action_list.append(team_color)

    def delete_unit_action(self):
        """Discard the current update"""
        self.action_list_of_the_turn = []

    def delete_log(self):
        """Discard the log"""
        self.action_lists = []

    def to_string_map_file(self, map):
        """Convert the map class argument to a string-type map file"""
        str_map = ""
        red_team_units = map.get_units_list(Consts.RED_TEAM, True, True, False)
        blue_team_units = map.get_units_list(Consts.BLUE_TEAM, True, True, False)

        str_map += f"SZX[{map.get_x_size()}]SZY[{map.get_y_size()}]"
        str_map += f"UN0[{len(red_team_units)}]UN1[{len(blue_team_units)}]"
        str_map += f"TLM[{map.get_turn_limit()}]HPTH[{map.get_draw_hp_threshold()}];\n"

        for y in range(map.get_y_size()):
            str_map += "MS["
            for x in range(map.get_x_size() - 1):
                str_map += f"{map.get_field_type(x, y)},"
            str_map += f"{map.get_field_type(map.get_x_size() - 1, y)}];\n"

        units = map.get_units()
        for unit in units:
            if unit is None:
                continue
            str_map += f"US[{unit.get_x_pos()},{unit.get_y_pos()},{unit.get_name()},"
            str_map += f"{unit.get_team_color()},{unit.get_HP()},{unit.is_action_finished()}];\n"

        # get datetime
        dt = self.date_time.strftime("%Y-%m-%d %H:%M:%S")
        str_map += f"DT[{dt}];\n"
        str_map += f"RU[{Consts.RULE_SET_VERSION}];\n"
        str_map += f"PR[{self.player_names[0]}]PB[{self.player_names[1]}];\n"

        return str_map

    @staticmethod
    def search_one(src, tag):
        """Search for the content (e.g., User) from the tag (e.g., PB[User])"""
        idx = src.find(tag + "[")
        if idx == -1:
            return ""
        idx_to = idx + len(tag) + 1
        idx = idx_to
        while True:
            if idx_to == len(src):
                return ""
            if src[idx_to] == "]":
                return src[idx:idx_to]
            idx_to += 1

    @staticmethod
    def record_comment():
        """Leave a comment at the specified position in the game record"""
        if SGFManager.str_buffer is None or SGFManager.str_buffer == "":
            return
        c_struct = CommentStruct(len(SGFManager.action_lists), len(SGFManager.action_list_of_the_turn),
                                 SGFManager.str_buffer)
        SGFManager.comment_list.append(c_struct)
        SGFManager.str_buffer = ""

    @staticmethod
    def store_comment(str_comment):
        """Save the log (comment) by appending"""
        str_comment += ","
        SGFManager.str_buffer += str_comment

    @staticmethod
    def show_map_info(map_name):
        """Display the map author's name and comments"""
        Logger.clear_text_box()

        author = ""
        comment = ""
        one = ""

        with open(map_name, "r", encoding="shift_jis") as sr:
            full_line = sr.read()

        lines = full_line.split(';')

        for line in lines:
            one = SGFManager.search_one(line, "AUTHOR")
            if one != "":
                author = one

            one = SGFManager.search_one(line, "MAPCOM")
            if one != "":
                comment = one

        Logger.log(f"Map Author Name [{author}]\r\n", 0)
        Logger.log(f"Comment\r\n[{comment}]\r\n", 0)


class CommentStruct:
    def __init__(self, t_cnt, s_cnt, comment):
        self.turn_cnt = t_cnt  # Total number of turns up to that point
        self.step_cnt = s_cnt  # Total step count in that turn
        self.comment = comment  # Comment to be left in the game record