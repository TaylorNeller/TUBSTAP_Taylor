from networkx import adjacency_matrix
from UnitData import *
from UnitDistanceCalculator import UnitDistanceCalculator

class MapUtils:

    calculator = UnitDistanceCalculator()

    @staticmethod
    def map_to_string(map_matrix, unit_list):
        x_size = len(map_matrix[0])
        y_size = len(map_matrix)
        map_unit = [[None for i in range(x_size)] for j in range(y_size)]
        for unit in unit_list:
            map_unit[unit.y][unit.x] = unit

        map_str = "_"
        for t in range(y_size):
            map_str += "_____"
        map_str += "\r\n"

        for y in range(y_size):
            map_str += "|"
            for x in range(x_size):
                if map_matrix[y][x] == 0:
                    map_str += "----|"
                    continue
                elif map_unit[y][x] is None:
                    map_str += "    |"
                    continue
                if map_unit[y][x].team == 0:
                    map_str += "R"
                else:
                    map_str += "B"
                map_str += ConstsData.unit_initials[map_unit[y][x].type]
                map_str += str(map_unit[y][x].hp).zfill(2)
                map_str += "|"

            map_str += "\r\n"
            map_str += "|"
            for x in range(y_size):
                map_str += "____|"
            map_str += "\r\n"

        return map_str
    
    @staticmethod
    def map_str_from_data(data_row):
        # takes data rows
        # assumes full 10 input exists
        # sets turn limit 30
        turn_limit = 30
        # sets hp threshold 10
        hp_threshold = 10

        map_mat = data_row[5]
        width = len(map_mat[0])
        height = len(map_mat)

        units = []

        # using np 2D matrix
        type_mat = data_row[6].tolist()
        team_mat = data_row[7].tolist()
        hp_mat = data_row[8].tolist()
        moved_mat = data_row[9].tolist()

        for r in range(len(type_mat)):
            for c in range(len(type_mat[0])):
                if type_mat[r][c] != 0:
                    units.append([c, r, type_mat[r][c], team_mat[r][c], hp_mat[r][c], moved_mat[r][c]])

        # find number of red and blue units
        n_red = 0
        n_blue = 0
        for unit in units:
            if unit[3] == 0:
                n_red += 1
            else:
                n_blue += 1
        
        map_str = f"SIZEX[{width}];SIZEY[{height}];\nTURNLIMIT[{turn_limit}];HPTHRESHOLD[{hp_threshold}];\nUNITNUMRED[{n_red}];UNITNUMBLUE[{n_blue}];\nAUTHOR[Taylor];\n" + \
                "".join([f"MAP[{','.join(map(str, map_mat[y]))}];\n" for y in range(height)]) + \
                "".join([f"UNIT[{unit[0]},{unit[1]},{str(unit[2])},{unit[3]},{unit[4]},{unit[5]}];\n" for unit in units])
        
        return map_str

        


    # flatten units into parallel matrices
    @staticmethod
    def create_cnn_input(units, map):
        x = len(map)
        y = len(map[0])
        # Initialize matrices type, team, hp, moved (size of map)
        type_matrix = [[0 for i in range(x)] for j in range(y)]
        team_matrix = [[0 for i in range(x)] for j in range(y)]
        hp_matrix = [[0 for i in range(x)] for j in range(y)]
        moved_matrix = [[0 for i in range(x)] for j in range(y)]
        for unit in units:
            type_matrix[unit.x][unit.y] = unit.type
            team_matrix[unit.x][unit.y] = unit.team
            hp_matrix[unit.x][unit.y] = unit.hp
            moved_matrix[unit.x][unit.y] = unit.moved

        return (map, type_matrix, team_matrix, hp_matrix, moved_matrix)












    @staticmethod
    def create_data_matrices(units, map, max_units, fast=True, mask=False):
        if fast:
            gcn_input = MapUtils.create_gcn_input_fast(units, map, max_units)
        else:
            gcn_input = MapUtils.calculator.create_gcn_input(units, map, max_units)
        
        # take the first matrix (adjacency matrix) and remove the upper left and lower right corners
        if mask:
            adjacency_matrix = gcn_input[0]
            n = len(adjacency_matrix)
            h = n // 2
            for i in range(n):
                for j in range(n):
                    if (i < h and j < h) or (i >= h and j >= h):
                        adjacency_matrix[i][j] = 0


        # gcn_input = dummy_gcn_input(units, map, max_units)
        cnn_input  = MapUtils.create_cnn_input(units, map)    
        return gcn_input + cnn_input







    @staticmethod
    def dummy_gcn_input(units, map, max_units):
        can_access = [[0 for i in range(max_units)] for j in range(max_units)]
        feature_matrix = [[0 for i in range(4)] for j in range(max_units)]
        manhattan_matrix = [[0 for i in range(max_units)] for j in range(max_units)]
        unobstructed_matrix = [[0 for i in range(max_units)] for j in range(max_units)]
        damage_matrix = [[0 for i in range(max_units)] for j in range(max_units)]
        return (can_access, feature_matrix, manhattan_matrix, unobstructed_matrix, damage_matrix)

    # max_units is the maximum number of units on the map
    # indexes adjacency matrix with unit order in units
    @staticmethod
    def create_gcn_input_new(units, map, max_units):
        # make distance matrix with manhattan distances between units
        man_distance = [[0 for i in range(max_units)] for j in range(max_units)]
        for i in range(len(units)):
            for j in range(len(units)):
                man_distance[i][j] = abs(units[i].x - units[j].x) + abs(units[i].y - units[j].y)

        # setup unobstructed distance matrix, where (i,j) is the distance from unit i to unit j
        # IMPORTANT: this distance is the distance to the tile of an allied unit, and the distance to the nearest tile adjacent to an enemy unit
        unobstructed_distance = [[0 for i in range(max_units)] for j in range(max_units)]
        # for each unit, find valid moves and attacks
        for unit, index in zip(units, range(len(units))):
            # setup output matrix
            dist_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]

            # setup team matrix
            team_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]
            for u in units:
                team_matrix[u.x][u.y] = u.team
            
            MapUtils.search_moves2(unit.x, unit.y, unit, map, dist_matrix, team_matrix, 0)

            # print("dist_matrix for unit " + str(index) + " at (" + str(unit.x) + ", " + str(unit.y) + "):")
            # print_matrix(dist_matrix)
            # fill row of unobstructed distance matrix
            # start with allied units, then for each enemy unit, see if it is adjacent to a movable location
            # if so, update unobstructed distance
            for i in range(len(units)):
                if units[i].team == unit.team:
                    unobstructed_distance[index][i] = dist_matrix[units[i].x][units[i].y]
                else:
                    closest = 99
                    for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
                        x = units[i].x + dx
                        y = units[i].y + dy
                        if x >= 0 and x < len(map) and y >= 0 and y < len(map) and dist_matrix[x][y] >= 0 and dist_matrix[x][y] < closest:
                            closest = dist_matrix[x][y]
                    unobstructed_distance[index][i] = closest
            
        # make can access matrix, where (i,j) is true if unobstructed distance <= movement and unit i and j are on different teams
        can_access = [[0 for i in range(max_units)] for j in range(max_units)]
        for i in range(len(units)):
            for j in range(len(units)):
                if unobstructed_distance[i][j] <= units[i].get_movement_capacity() and units[i].team != units[j].team:
                    can_access[i][j] = 1
        
        # make single matrix for hp, type, mobility, and moved, where the rows are the units and the columns are the features (hp, type,...)
        feature_matrix = [[0 for i in range(4)] for j in range(max_units)]
        for i in range(len(units)):
            feature_matrix[i][0] = units[i].hp
            feature_matrix[i][1] = units[i].type if units[i].team == 0 else -1 * units[i].type # -type if blue
            feature_matrix[i][2] = units[i].get_movement_capacity()
            if (units[i].team == 0): # red team
                feature_matrix[i][3] = 1 if units[i].moved else 0
            else:
                feature_matrix[i][3] = 1

        # make damage matrix, where (i,j) is the damage unit i does to unit j
        damage = [[0 for i in range(max_units)] for j in range(max_units)]
        for i in range(len(units)):
            for j in range(len(units)):
                if units[i].team != units[j].team:
                    damage[i][j] = units[i].calc_dmg(units[j], map[units[j].x][units[j].y])

        output = (can_access, feature_matrix, man_distance, unobstructed_distance, damage)
        return output

    # searches by increasing movement instead of decreasing
    @staticmethod
    def search_moves2(x,y,unit,map,searched,team_matrix,movement, max_movement=99):
        if (searched[x][y] != -1 and searched[x][y] <= movement) or movement >= max_movement or team_matrix[x][y] == (1-unit.team):
            return
        searched[x][y] = movement
        for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
            newx = x+dx
            newy = y+dy
            if newx >= 0 and newx < len(map) and newy >= 0 and newy < len(map):
                # print(unit.move_cost(map[newx][newy]))
                MapUtils.search_moves2(x+dx, y+dy, unit, map, searched, team_matrix, movement+unit.move_cost(map[newx][newy]))

    @staticmethod
    def create_gcn_input_fast(units, map, max_units):
        # make distance matrix with manhattan distances between units
        man_distance = [[0 for i in range(max_units)] for j in range(max_units)]
        for i in range(len(units)):
            for j in range(len(units)):
                man_distance[i][j] = abs(units[i].x - units[j].x) + abs(units[i].y - units[j].y)
            
        # make can access matrix, where (i,j) is true if unobstructed distance <= movement and unit i and j are on different teams
        can_access = [[0 for i in range(max_units)] for j in range(max_units)]
        for i in range(len(units)):
            for j in range(len(units)):
                if man_distance[i][j] <= units[i].get_movement_capacity()+1 and units[i].team != units[j].team:
                    can_access[i][j] = 1
        
        # make single matrix for hp, type, mobility, and moved, where the rows are the units and the columns are the features (hp, type,...)
        feature_matrix = [[0 for i in range(4)] for j in range(max_units)]
        for i in range(len(units)):
            feature_matrix[i][0] = units[i].hp
            feature_matrix[i][1] = units[i].type if units[i].team == 0 else -1 * units[i].type # -type if blue
            feature_matrix[i][2] = units[i].get_movement_capacity()
            if (units[i].team == 0): # red team
                feature_matrix[i][3] = 1 if units[i].moved else 0
            else:
                feature_matrix[i][3] = 1

        # make damage matrix, where (i,j) is the damage unit i does to unit j
        damage = [[0 for i in range(max_units)] for j in range(max_units)]
        counterattack = [[0 for i in range(max_units)] for j in range(max_units)]
        for i in range(len(units)):
            for j in range(len(units)):
                if units[i].team != units[j].team:
                    dmg, counter = units[i].calc_dmg(units[j], map[units[j].x][units[j].y], map[units[i].x][units[i].y])
                    damage[i][j] = dmg
                    counterattack[i][j] = counter


        output = (can_access, feature_matrix, man_distance, counterattack, damage)
        return output

    # add padding to make all cells 3 characters wide
    # format [x, x, ..., x] where x is the same width each time
    def print_matrix(matrix):
        for i in range(len(matrix)):
            print("[", end="")
            for j in range(len(matrix[i])):
                value = matrix[i][j]
                if isinstance(value, bool):
                    value = "T" if value else "F"
                print(str(value).rjust(3), end=",")
            print("]")
        print()
