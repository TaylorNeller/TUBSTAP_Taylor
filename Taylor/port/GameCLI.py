from PlayerList import PlayerList
import sys
import os
from GameManager import GameManager
from Logger import SGFManager
from Logger import Logger
from AutoBattleSettings import AutoBattleSettings
from Form import Form

def run_autobattle(user1=0, user2=0, map="AutoBattles", output="AutoBattleResult.csv", games=2):
    # Specify the number of battles, default to 2
    AutoBattleSettings.NumberOfGamesPerMap = games

    if map == "AutoBattles":
        map_files = get_files_most_deep("./autobattle/", "*.tbsmap")
        if len(map_files) == 0:
            print("No tbsmap files found in the autobattle directory.")
            return

        for map_file in map_files:
            # print("Running AutoBattle on map: " + map_file)
            form = Form(user1, user2)
            game_manager = GameManager(form, map_file)
            sgf_manager = SGFManager()
            
            game_manager.set_auto_battle_result_file_name(output)
            game_manager.set_sgf_manager(sgf_manager)
            game_manager.enable_auto_battle()
            game_manager.init_game()
    else:
        form = Form(user1, user2)
        game_manager = GameManager(form, map_file)
        sgf_manager = SGFManager()
        
        game_manager.set_auto_battle_result_file_name(output)
        game_manager.set_sgf_manager(sgf_manager)
        game_manager.enable_auto_battle()
        game_manager.init_game()
    Logger.flush()

def command_line_interface():
    # Get command line arguments, ignoring the first one (executable path)
    args = sys.argv[1:]

    if len(args) > 3:
        # Use a dictionary to allow arbitrary order of arguments
        arg_dict = {}
        for arg in args:
            pair = arg.split('=')
            arg_dict[pair[0]] = pair[1]

        # Specify RED TEAM AI, default to SampleAI if not found
        red_team_index = 0
        for i in range(1, len(PlayerList.registered_player_list)):
            if arg_dict.get("user1") == PlayerList.get_player(i).get_name():
                red_team_index = i

        # Specify BLUE TEAM AI, default to SampleAI if not found
        blue_team_index = 0
        for i in range(1, len(PlayerList.registered_player_list)):
            if arg_dict.get("user2") == PlayerList.get_player(i).get_name():
                blue_team_index = i

        # Specify map file name, default to "AutoBattles"
        map_file_name = "AutoBattles"
        if "map" in arg_dict:
            map_file_name = "./map/" + arg_dict["map"]

        # Specify output file name, default to "AutoBattleResultXXX.csv" where XXX is the date
        result_file_name = "AutoBattleResult.csv"
        if "output" in arg_dict:
            result_file_name = arg_dict["output"]

        if PlayerList.get_player(red_team_index).get_name() == "HumanPlayer":
            print("Please select an AI for the Red Team Player.")
            return

        if PlayerList.get_player(blue_team_index).get_name() == "HumanPlayer":
            print("Please select an AI for the Blue Team Player.")
            return
        
        games = 2
        if "games" in arg_dict:
            games = int(arg_dict["games"])
        
        run_autobattle(red_team_index, blue_team_index, map_file_name, result_file_name, games)
    else:
        run_autobattle()
        # if map_file_name == "AutoBattles":
        #     map_files = get_files_most_deep("./autobattle/", "*.tbsmap")
        #     if len(map_files) == 0:
        #         print("No tbsmap files found in the autobattle directory.")
        #         return

        #     for map_file in map_files:
        #         print("Running AutoBattle on map: " + map_file)
        #         form = Form(red_team_index, blue_team_index)
        #         game_manager = GameManager(form, map_file)
        #         sgf_manager = SGFManager()
                
        #         # Specify the number of battles, default to 100
        #         if "games" in arg_dict:
        #             AutoBattleSettings.NumberOfGamesPerMap = int(arg_dict["games"])

        #         game_manager.set_auto_battle_result_file_name(result_file_name)
        #         game_manager.set_sgf_manager(sgf_manager)
        #         game_manager.enable_auto_battle()
        #         game_manager.init_game()
        # else:
        #     form = Form(red_team_index, blue_team_index)
        #     game_manager = GameManager(form, map_file_name)
            
        #     # Specify the number of battles, default to 100
        #     if "games" in arg_dict:
        #         AutoBattleSettings.NumberOfGamesPerMap = int(arg_dict["games"])

        #     sgf_manager = SGFManager()
        #     game_manager.set_auto_battle_result_file_name(result_file_name)
        #     game_manager.set_sgf_manager(sgf_manager)
        #     game_manager.enable_auto_battle()
        #     game_manager.init_game()

def get_files_most_deep(root_path, pattern):
    file_paths = []
    
    # Search for all files in the specified directory that match the pattern
    for file_path in os.listdir(root_path):
        if os.path.isfile(os.path.join(root_path, file_path)) and file_path.endswith(pattern[1:]):
            file_paths.append(os.path.join(root_path, file_path))
    
    return file_paths

if __name__ == "__main__":
    command_line_interface()