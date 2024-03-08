import os
import sys
from Unit import *

game_results = {} # formatted {map_id: result}
game_maps = {} # formatted {map_id: [map_matrix, unit_list]}

def read_map(filename):
    # find map id (# after genmap)
    map_id = int(filename.split('genmap')[1].split('.')[0])

    # Load the file   
    with open(filename, 'r') as file:
        data = file.read()
    # read MAP[x,x,x,...,x]; lines into map matrix
    # read UNIT[x,y,typestr,team,hp,moved]; lines into Unit list
    map_matrix = []
    unit_list = []
    for line in data.split('\n'):
        if line.startswith('MAP['):
            map_matrix.append([int(x) for x in line[4:-2].split(',')])
        if line.startswith('UNIT['):
            # read unit data, converting unit type to integer based on index in unitNames
            unit_data = line[5:-2].split(',')
            unit_data[2] = Consts.unitNames.index(unit_data[2])
            unit_list.append(Unit(*[int(x) for x in unit_data]))
    
    game_maps[map_id] = [map_matrix, unit_list]

def read_all_maps(directory):
    # Get all files in directory
    files = os.listdir(directory)
    # Filter out only .tbsmap files
    maps = [file for file in files if file.endswith('.tbsmap')]
    # Read each map
    for map in maps:
        # read map (directory + map file name)
        read_map(directory + '/' + map)

def annotate_battle_results(filename):
    # Load the file
    with open(filename, 'r') as file:
        data = file.read()
    # drop header from data
    data = data.split('\n', 1)[1]
    # line format:
    # MapName,RedPlayerName,BluePlayerName,BattleCnt,WinCntOfRed,WinCntOfBlue,DrawCnt,OverTrunCnt,JudgeWinCntOfRed,JudgeWinCntOfBlue,NoiseOfHP,NoiseOfPos
    # ./autobattle/genmap0.tbsmap,Sample_MaxActionEvalFunc,Sample_MaxActionEvalFunc,1,1,0,0,1,1,0,False,False
    # ./autobattle/genmap0.tbsmap,Sample_MaxActionEvalFunc,Sample_MaxActionEvalFunc,1,0,1,0,1,0,1,False,False
    # if red wins then blue, first player (red) advantage, if the opposite then blue advantage, if draw then no advantage, if both red or both blue then discard data
    # process 2 lines at a time
    for line1, line2 in zip(data.split('\n')[::2], data.split('\n')[1::2]):
        line1 = line1.split(',')
        line2 = line2.split(',')
        # get just number from map file
        map_id = int(line1[0].split('genmap')[1].split('.')[0])
        # if red wins then blue, first player (red) advantage, if the opposite then blue advantage, if draw then no advantage, if both red or both blue then discard data
        if line1[4] == '1' and line1[5] == '0' and line2[4] == '0' and line2[5] == '1':
            game_results[map_id] = 1
        elif line1[4] == '0' and line1[5] == '1' and line2[4] == '1' and line2[5] == '0':
            game_results[map_id] = -1
        elif line1[4] == '0' and line1[5] == '0' and line2[4] == '0' and line2[5] == '0':
            game_results[map_id] = 0
        else:
            game_results[map_id] = 2
        # print(map_id, game_results[map_id])


if __name__ == '__main__':
    # read all maps (default directory ./bin/Release/autobattle)
    read_all_maps('../bin/Release/autobattle')
    # annotate results from given file in arguments and print results
    annotate_battle_results(sys.argv[1])
    print(game_results)
    # print(game_maps)