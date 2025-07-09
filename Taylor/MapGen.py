import random
from UnitData import *
import sys
import os
import MapUtils
import time

# Add the port directory to the Python path
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), 'port')))
import port.GameCLI as GameCLI

class MapGen:

    terrain_type = "plains" # plains, random
    unit_dist = "unit-list" # no-cannon, ground-melee, inf-tank, unit list
    unit_list = ["infantry", "infantry", "panzer", "infantry", "infantry", "panzer"]
    n_red = 3
    n_blue = 3
    seed = -1
    map_x = 8
    map_y = 8
    map_padded = True
    n_iters = 50
    turn_limit = 30 # also currently determines starting player
    local_search_flag = False
    starting_exhaust = False
    player1 = 0
    player2 = 0

    # First turn advantage counter
    # this means that blue team (second player) will have fta_counter times the value of red team on average
    fta_counter = 1.1 #1.06 is best fta_counter for plains_inf-tank with SimpleAI

    @staticmethod
    def generate_map_file(file_path=None):
        width = MapGen.map_x
        height = MapGen.map_y

        # set seed
        if MapGen.seed != -1:
            random.seed(MapGen.seed)


        # generate terrain
        terrain = [[0]*width for _ in range(height)]
        
        for y in range(height):
            for x in range(width):
                if MapGen.map_padded and (x == 0 or y == 0 or x == width - 1 or y == height - 1):
                    terrain[y][x] = 0
                else:
                    if MapGen.terrain_type == "random": #random terrain
                        indicator = random.randint(0, 9)
                        if indicator == 0:
                            terrain[y][x] = 4
                        elif indicator == 1:
                            terrain[y][x] = 2
                        else:
                            n = 1 + random.randint(0, 2) * 2
                            if n == 6:
                                n = 5
                            terrain[y][x] = n
                    else: #plains terrain
                        terrain[y][x] = 1

        # generate units
        # AtFAIPC
        # new order FAtPCAI
        all_units = []
        exhausted_red = random.randint(1, MapGen.n_red // 2) if MapGen.starting_exhaust else 0
        ground_melee = [ConstsData.INFANTRY, ConstsData.PANZER, ConstsData.ANTIAIR]
        air = [ConstsData.FIGHTER, ConstsData.ATTACKER]
        
        for i in range(MapGen.n_red + MapGen.n_blue):
            while True:
                x = random.randint(0, width - 1)
                y = random.randint(0, height - 1)

                
                team = 0 if i < MapGen.n_red else 1
                hp = 10
                is_exhausted = 0
                if team == 0 and exhausted_red > 0:
                    exhausted_red -= 1
                    is_exhausted = 1
                
                if MapGen.unit_dist == "no-cannon":
                    type_index = random.choice(air) if random.randint(0,4) else random.choice(ground_melee)
                elif MapGen.unit_dist == "ground-melee":
                    type_index = random.choice(ground_melee)
                elif MapGen.unit_dist == "inf-tank":
                    type_index = ConstsData.PANZER if random.randint(0,2) == 0 else ConstsData.INFANTRY
                elif MapGen.unit_dist == "unit-list":
                    type_index = ConstsData.unitNames.index(MapGen.unit_list[i])
                    if MapGen.unit_list[i] == "panzer":
                        hp = 5
                else:
                    type_index = random.randint(0, len(ConstsData.unitNames) - 1)

                new_unit = [x, y, type_index, team, hp, is_exhausted]

                # continue if unit on preexisting unit location
                if any(new_unit[0] == u[0] and new_unit[1] == u[1] for u in all_units):
                    continue
                
                # continue if unit generated on impassable terrain
                if terrain[y][x] == 0 or \
                (terrain[y][x] == 2 and not (type_index in air)) or \
                (terrain[y][x] == 4 and not (type_index in air or type_index == 5)):
                    continue
                
                all_units.append(new_unit)
                break

        # balance unit value on each team
        if not MapGen.local_search_flag:
            unit_values = [9, 11, 6, 7, 8, 1]        
            team_values = [0, 0]
            for unit in all_units:
                team_values[unit[3]] += unit_values[unit[2]] * unit[4] * 0.1

            target_percent = 0.8
            target_value = min(target_percent * team_values[0], target_percent * team_values[1])
            team_targets = [target_value, target_value * MapGen.fta_counter]

            for team in range(2):
                iter_count = 0
                while abs(team_values[team] - team_targets[team]) > 0.05 * team_targets[team] and iter_count < MapGen.n_iters:
                    unit_index = random.randint(0, MapGen.n_red - 1) if team == 0 else random.randint(0, MapGen.n_blue - 1) + MapGen.n_red
                    if team_values[team] > team_targets[team]:
                        if all_units[unit_index][4] > 1:
                            all_units[unit_index][4] -= 1
                            team_values[team] -= unit_values[all_units[unit_index][2]] * 0.1
                    else:
                        if all_units[unit_index][4] < 10:
                            all_units[unit_index][4] += 1
                            team_values[team] += unit_values[all_units[unit_index][2]] * 0.1
                    iter_count += 1

        # print(team_values[0])
        # print(team_values[1])

        # convert units types to strings
        for unit in all_units:
            unit[2] = ConstsData.unitNames[unit[2]]

        map_str = f"SIZEX[{width}];SIZEY[{height}];\nTURNLIMIT[{MapGen.turn_limit}];HPTHRESHOLD[10];\nUNITNUMRED[{MapGen.n_red}];UNITNUMBLUE[{MapGen.n_blue}];\nAUTHOR[Taylor];\n" + \
                "".join([f"MAP[{','.join(map(str, terrain[y]))}];\n" for y in range(height)]) + \
                "".join([f"UNIT[{unit[0]},{unit[1]},{str(unit[2])},{unit[3]},{unit[4]},{unit[5]}];\n" for unit in all_units])

        if MapGen.local_search_flag:
            # time to run local search
            stopwatch = time.time()
            map_str = MapGen.local_search(map_str)
            print("Local search took ", time.time() - stopwatch, " seconds")



        if file_path is not None:
            # write to file
            with open(file_path, 'w') as writer:
                writer.write(map_str)
        else:
            # return string version
            return map_str
        
    @staticmethod
    def generate_n_maps_str(n):
        # concat n map files with separator 'MAPEND;
        return "MAPEND;\n".join([MapGen.generate_map_file(8, 8) for _ in range(n)])

    @staticmethod
    def local_search(map_str, prev_winner=None):
        # print("Local search with prev_winner ", prev_winner)
        # print(map_str)
        states, result = GameCLI.run_autobattle(user1=MapGen.player1, user2=MapGen.player2, map='random_map', games=2, raw_map_str=map_str)
        if result == 0:
            # for state in states:
            #     map_matrix, unit_list, move = state
            #     # gcn_input = MapUtils.create_gcn_input_new(unit_list, map_matrix, DataManager.num_nodes)
            #     print(MapUtils.map_to_string(map_matrix, unit_list))
            return map_str
        if prev_winner is None or result == prev_winner:
            # extract all units from map_str
            units = []
            for line in map_str.split('\n'):
                if line.startswith("UNIT["):
                    unit = line.split('[')[1].split(']')[0].split(',')
                    units.append([int(unit[0]), int(unit[1]), unit[2], int(unit[3]), int(unit[4]), int(unit[5])])
            # get winner units > 1 hp
            winner_units = [unit for unit in units if unit[3] == (0 if result == 1 else 1) and unit[4] > 1]
            unit = random.choice(winner_units)
            # decrement hp in the map_str
            old_str = f"UNIT[{unit[0]},{unit[1]},{str(unit[2])},{unit[3]},{unit[4]},{unit[5]}];"
            new_str = f"UNIT[{unit[0]},{unit[1]},{str(unit[2])},{unit[3]},{unit[4] - 1},{unit[5]}];"
            map_str = map_str.replace(old_str, new_str)

            return MapGen.local_search(map_str, result)
        else:
            return map_str

        






# main
if __name__ == "__main__":
    # Generate 10 map files
    for i in range(10):
        MapGen.generate_map_file(file_path=f"bin/Release/autobattle/genmap{i}.tbsmap", width=8, height=8)