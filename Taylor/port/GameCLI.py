from PlayerList import PlayerList
import sys
import os
from GameManager import GameManager
from Logger import SGFManager
from Logger import Logger
from AutoBattleSettings import AutoBattleSettings
from Form import Form

def run_autobattle(user1=0, user2=0, map="AutoBattles", output="AutoBattleResult.csv", games=2, raw_map_str=None):
    # Specify the number of battles, default to 2
    AutoBattleSettings.NumberOfGamesPerMap = games

    if raw_map_str is None and map == "AutoBattles":
        map_files = get_files_most_deep("./autobattle/", "*.tbsmap")
        if len(map_files) == 0:
            print("No tbsmap files found in the autobattle directory.")
            return

        Logger.log_file("MapName,WinCntOfRed,WinCntOfBlue,DrawCnt,FirstMove\n")
        for map_file in map_files:
            # print("Running AutoBattle on map: " + map_file)
            form = Form(user1, user2)
            game_manager = GameManager(form, map_file)
            sgf_manager = SGFManager()
            
            game_manager.set_auto_battle_result_file_name(output)
            game_manager.set_sgf_manager(sgf_manager)
            game_manager.enable_auto_battle()
            game_manager.init_game(log_flag=True)
        Logger.flush_file_log(output)
    else:
        form = Form(user1, user2)
        if raw_map_str is None:
            game_manager = GameManager(form, map)
        else:
            # print("Running AutoBattle on raw map string: " + raw_map_str)
            game_manager = GameManager(form, None, raw_map_str) # sets file to None
        sgf_manager = SGFManager()
        
        game_manager.set_auto_battle_result_file_name(output)
        game_manager.set_sgf_manager(sgf_manager)
        game_manager.enable_auto_battle()

        # returns string to be placed in file
        return game_manager.init_game()
    
def run_battles(user1=0, user2=0, data_loader=None):
    games = data_loader.get_enum()
    
def get_files_most_deep(root_path, pattern):
    file_paths = []
    
    # Search for all files in the specified directory that match the pattern
    for file_path in os.listdir(root_path):
        if os.path.isfile(os.path.join(root_path, file_path)) and file_path.endswith(pattern[1:]):
            file_paths.append(os.path.join(root_path, file_path))
    
    return file_paths
