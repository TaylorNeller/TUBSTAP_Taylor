import os
import sys
from UnitData import *
import MapUtils

game_maps = {} # formatted {map_id: [map_matrix, unit_list]}
game_move = {} # formatted {map_id: [result, move]}
# game_data = {} # formatted {map_id: [map_matrix, unit_list, result, move]}

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
            unit_data[2] = ConstsData.unitNames.index(unit_data[2])
            unit_list.append(UnitData(*[int(x) for x in unit_data]))
    
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
    # line format (header and example data row):
    # MapName,WinCntOfRed,WinCntOfBlue,DrawCnt,FirstMove
    # ./autobattle/genmap0.tbsmap,1,0,0,2:1:2:1:1:1
    with open(filename, 'r') as file:
        # drop header from data
        next(file)

        for line in file:
            data = line.strip().split(',')
            map_name = data[0]
            win_cnt_red = int(data[1])
            win_cnt_blue = int(data[2])
            # draw_cnt = int(data[3])
            move = data[4].split(':')
            
            map_id = int(map_name.split('genmap')[1].split('.')[0])
            
            result = 0
            if win_cnt_red > win_cnt_blue:
                result = 1
            elif win_cnt_blue > win_cnt_red:
                result = -1
            
            # find direction of attack in move
            dirs = [(0,0), (1,0), (0,1), (-1,0), (0,-1)]
            dx = int(move[4]) - int(move[2])
            dy = int(move[5]) - int(move[3])
            if (dx,dy) not in dirs:
                print('map_id =', map_id, 'dx =', dx, 'dy =', dy, 'dirs =', dirs, 'move =', move)
            dir = dirs.index((dx,dy))

            move = [int(move[0]), int(move[1]), int(move[2]), int(move[3]), dir]
            
            game_move[map_id] = [result, move]


def load_gcn_matrices(data_csv):
    # Load the data from the file
    if not os.path.exists(data_csv):
        print('File not found:', data_csv)
        return None, None

    data = []
    labels = []
    # for each map, generate input data row for data and labels
    for map_id in game_maps:
        map_matrix = game_maps[map_id][0]
        unit_list = game_maps[map_id][1]
        result = game_move[map_id][0]
        move = game_move[map_id][1]

        # generate input data row for data and labels
        input_data = MapUtils.create_gcn_input(unit_list, map_matrix, 6*2) # max units on map = 12
        data.append(input_data)
        labels.append(result)

    # rename autobattle_name file to autobattle_name_processed, e.g. file.csv -> file_processed.csv
    # os.rename(autobattle_name, autobattle_name.split('.')[0] + '_processed.csv')
    return data, labels



if __name__ == '__main__':
    # read all maps (default directory ./bin/Release/autobattle)
    read_all_maps('../bin/Release/autobattle')
    # annotate results from given file in arguments and print results
    annotate_battle_results(sys.argv[1])
    print(game_move)
    # print(game_maps)