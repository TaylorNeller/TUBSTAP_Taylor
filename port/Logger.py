class Logger:
    s_blue_text_box = None
    s_red_text_box = None
    s_owner_form = None
    s_unit_id_label = None

    @staticmethod
    def set_log_box(owner_form, tbx_blue, tbx_red, lbl):
        """
        Setter
        :param owner_form: Form to display
        :param tbx_blue: Blue text box
        :param tbx_red: Red text box
        """
        Logger.s_owner_form = owner_form
        Logger.s_blue_text_box = tbx_blue
        Logger.s_red_text_box = tbx_red
        Logger.s_unit_id_label = lbl

    @staticmethod
    def add_log_message(message, team_color):
        """
        Add log to text box and game record
        :param message: String to display
        """
        if team_color == 0:
            Logger.s_blue_text_box.insert(tk.END, message)
        else:
            Logger.s_red_text_box.insert(tk.END, message)
        SGFManager.store_comment(message)

    @staticmethod
    def log_comment_to_sgf(message):
        """
        Add log only to game record
        :param message: String to display
        """
        SGFManager.store_comment(message)

    @staticmethod
    def log(message, team_color):
        """
        Display string in text box
        :param message: String to display
        """
        if team_color == 0:
            Logger.s_blue_text_box.insert(tk.END, message)
        else:
            Logger.s_red_text_box.insert(tk.END, message)

    @staticmethod
    def netlog(message, team_color):
        """
        Log output for network battle
        :param message: String to display
        """
        if Settings.showing_net_log:
            if team_color == 0:
                Logger.s_blue_text_box.insert(tk.END, message)
            else:
                Logger.s_red_text_box.insert(tk.END, message)

    @staticmethod
    def clear_text_box():
        Logger.s_blue_text_box.delete('1.0', tk.END)
        Logger.s_red_text_box.delete('1.0', tk.END)

    @staticmethod
    def clear_blue_text_box():
        """
        Clear the contents of the blue text box
        """
        Logger.s_blue_text_box.delete('1.0', tk.END)

    @staticmethod
    def clear_red_text_box():
        """
        Clear the contents of the red text box
        """
        Logger.s_red_text_box.delete('1.0', tk.END)

    @staticmethod
    def clear_box_and_print_log(message):
        """
        Clear the contents of the text box and then output the log
        :param message: String to display
        """
        Logger.s_blue_text_box.delete('1.0', tk.END)
        Logger.s_blue_text_box.insert(tk.END, message)

    @staticmethod
    def show_dialog_message(message):
        """
        Display a message in a dialog
        :param message: String to display
        """
        messagebox.showinfo("Message", message)

    @staticmethod
    def show_unit_status(message):
        """
        Display the log during game progress
        """
        Logger.s_unit_id_label.config(text=message)

    @staticmethod
    def show_game_result(team_color):
        """
        Display the game result in the text box
        """
        Logger.s_blue_text_box.insert(tk.END, f"{Consts.TEAM_NAMES[team_color]} wins.")

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
        Logger.s_blue_text_box.insert(tk.END, result_str)

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
        Logger.s_blue_text_box.insert(tk.END, result_str)

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

    @staticmethod
    def show_array_contents(array):
        """
        Used for debugging 2D arrays
        """
        result_str = ""
        for row in array:
            for item in row:
                result_str += f"{item:3}"
            result_str += "\n"
        messagebox.showinfo("Array Contents", result_str)

    @staticmethod
    def show_auto_battle_result_with_first_move(file_name, map_name, red_player_name, blue_player_name, battle_cnt,
                                                win_cnt_of_red, win_cnt_of_blue, draw_cnt, over_turn_cnt, first_move, debug_str):
        write_title_flag = False
        if not os.path.exists(file_name):
            write_title_flag = True

        # Use this when you want to output to an external file
        with open(file_name, 'a') as file:
            if write_title_flag:
                file.write("MapName,WinCntOfRed,WinCntOfBlue,DrawCnt,FirstMove\n")
            file.write(f"{map_name},{win_cnt_of_red[0]},{win_cnt_of_blue[0]},{draw_cnt},{first_move}\n")

    @staticmethod
    def show_auto_battle_result_with_first_move_2(file_name, map_name, red_player_name, blue_player_name, battle_cnt,
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
                       f"{AutoBattleSettings.IsHPRandomlyDecreased},{AutoBattle