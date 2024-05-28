from UnitData import *

# recursively search for moves
# input format: 
# unit format: Unit class
# map format: m x m
# output format: m x m x 3
# 1st layer: can move to
# 2nd layer: can attack
# 3rd layer: damage to defender
def gen_cnn_input(unit, units, map):
    # setup output matrices
    can_move_to = [[-1 for i in range(len(map))] for j in range(len(map))]
    can_attack = [[False for i in range(len(map))] for j in range(len(map))]
    damage = [[0 for i in range(len(map))] for j in range(len(map))]

    # setup team matrix
    team_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]
    for u in units:
        team_matrix[u.x][u.y] = u.team

    search_moves(unit.x, unit.y, unit, map, can_move_to, team_matrix, unit.get_movement_capacity())

    # set can move layer to False if -1 and True if any other number
    for i in range(len(map)):
        for j in range(len(map)):
            if can_move_to[i][j] == -1:
                can_move_to[i][j] = False
            else:
                can_move_to[i][j] = True

    # remove allied units from can move to layer
    for u in units:
        if u.team == unit.team and u != unit:
            can_move_to[u.x][u.y] = False

    # for each enemy unit, see if it is next to a movable location by checking adjacent squares
    # if so, update movable location in attack layer, and update damage layer
    for u in units:
        if u.team != unit.team:
            for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
                x = u.x + dx
                y = u.y + dy
                if x >= 0 and x < len(map) and y >= 0 and y < len(map) and can_move_to[x][y]:
                    can_attack[u.x][u.y] = True
                    damage[u.x][u.y] = unit.calc_dmg(u, map[u.x][u.y])
    return [can_move_to, can_attack, damage]


def search_moves(x,y,unit,map,output,team_matrix,movement):
    if output[x][y] >= movement or movement < 0 or team_matrix[x][y] == (1-unit.team):
        return
    output[x][y] = movement
    for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
        newx = x+dx
        newy = y+dy
        if newx >= 0 and newx < len(map) and newy >= 0 and newy < len(map):
            search_moves(x+dx, y+dy, unit, map, output, team_matrix, movement-unit.move_cost(map[newx][newy]))

# ouput format: n x n x ?
# original
# 1st layer: hp
# 2nd layer: type (encoded with team)
# 3rd layer: mobility
# 4th layer: moved or not
# 5th layer: manhattan distance
# proposed
# hp, type, moved, mobility encoded on diagonal?
# 6th layer: unobstructed distance
# 7th layer: damage to enemy

  
# max_units is the maximum number of units on the map
# indexes adjacency matrix with unit order in units
def create_gcn_adjacency(units, map, max_units):
    # make distance matrix with manhattan distances between units
    man_distance = [[0 for i in range(max_units)] for j in range(max_units)]
    for i in range(len(units)):
        for j in range(len(units)):
            man_distance[i][j] = abs(units[i].x - units[j].x) + abs(units[i].y - units[j].y)

    # setup unobstructed distance matrix, where (i,j) is the distance from unit i to unit j
    unobstructed_distance = [[0 for i in range(max_units)] for j in range(max_units)]
    # for each unit, find valid moves and attacks
    for unit, index in zip(units, range(len(units))):
        # setup output matrix
        dist_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]

        # setup team matrix
        team_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]
        for u in units:
            team_matrix[u.x][u.y] = u.team
        
        search_moves2(unit.x, unit.y, unit, map, dist_matrix, team_matrix, 0)

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
    can_access = [[False for i in range(max_units)] for j in range(max_units)]
    for i in range(len(units)):
        for j in range(len(units)):
            if unobstructed_distance[i][j] <= units[i].get_movement_capacity() and units[i].team != units[j].team:
                can_access[i][j] = True

    # make matrices hp, type, mobility, and moved where (i,j) is the hp, type, and mobility of unit j if unit i can access unit j
    hp = [[0 for i in range(max_units)] for j in range(max_units)]
    type = [[0 for i in range(max_units)] for j in range(max_units)]
    mobility = [[0 for i in range(max_units)] for j in range(max_units)]
    moved = [[0 for i in range(max_units)] for j in range(max_units)]
    for i in range(len(units)):
        for j in range(len(units)):
            if can_access[i][j]:
                hp[i][j] = units[i].hp
                type[i][j] = units[i].type if units[i].team == 0 else -1 * units[i].type # -type if blue
                mobility[i][j] = units[i].get_movement_capacity()
                if (units[i].team == 0): # red team
                    moved[i][j] = 1 if units[i].moved else 0

    # make damage matrix, where (i,j) is the damage unit i does to unit j
    damage = [[0 for i in range(max_units)] for j in range(max_units)]
    for i in range(len(units)):
        for j in range(len(units)):
            if units[i].team != units[j].team:
                damage[i][j] = units[i].calc_dmg(units[j], map[units[j].x][units[j].y])

    # combine all 7 layers into one output
    output = [hp, type, mobility, moved, man_distance, unobstructed_distance, damage]
    return output
    
# max_units is the maximum number of units on the map
# indexes adjacency matrix with unit order in units
def create_gcn_input(units, map, max_units):
    # make distance matrix with manhattan distances between units
    man_distance = [[0 for i in range(max_units)] for j in range(max_units)]
    for i in range(len(units)):
        for j in range(len(units)):
            man_distance[i][j] = abs(units[i].x - units[j].x) + abs(units[i].y - units[j].y)

    # setup unobstructed distance matrix, where (i,j) is the distance from unit i to unit j
    # IMPORTANT: this distance is the distance to the tile of an allied unit, and the distance to the nearest tile of an enemy unit
    unobstructed_distance = [[0 for i in range(max_units)] for j in range(max_units)]
    # for each unit, find valid moves and attacks
    for unit, index in zip(units, range(len(units))):
        # setup output matrix
        dist_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]

        # setup team matrix
        team_matrix = [[-1 for i in range(len(map))] for j in range(len(map))]
        for u in units:
            team_matrix[u.x][u.y] = u.team
        
        search_moves2(unit.x, unit.y, unit, map, dist_matrix, team_matrix, 0)

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

    # make damage matrix, where (i,j) is the damage unit i does to unit j
    damage = [[0 for i in range(max_units)] for j in range(max_units)]
    for i in range(len(units)):
        for j in range(len(units)):
            if units[i].team != units[j].team:
                damage[i][j] = units[i].calc_dmg(units[j], map[units[j].x][units[j].y])

    output = (can_access, feature_matrix, [man_distance, unobstructed_distance, damage])
    return output

# searches by increasing movement instead of decreasing
def search_moves2(x,y,unit,map,searched,team_matrix,movement, max_movement=100):
    if (searched[x][y] != -1 and searched[x][y] <= movement) or movement > max_movement or team_matrix[x][y] == (1-unit.team):
        return
    searched[x][y] = movement
    for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
        newx = x+dx
        newy = y+dy
        if newx >= 0 and newx < len(map) and newy >= 0 and newy < len(map):
            search_moves2(x+dx, y+dy, unit, map, searched, team_matrix, movement+unit.move_cost(map[newx][newy]))

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

# if main, run test
if __name__ == "__main__":
    # make 8x8 map full of plains
    map = [[1 for i in range(8)] for j in range(8)]
    # make 6 infantry
    units = [UnitData(4,5,3,0,6,False), UnitData(3,4,3,0,7,True), UnitData(3,6,3,0,5,False), UnitData(3,5,3,1,4,False), UnitData(5,5,3,1,6,False), UnitData(3,7,3,1,8,False)]
    
    # test search_moves

    # test gen_cnn_input
    cnn_input = gen_cnn_input(units[5], units, map)
    # print_matrix(cnn_input[0])
    # print_matrix(cnn_input[1])
    # print_matrix(cnn_input[2])

    # test create_gcn_input
    gcn_input = create_gcn_input(units, map, 6)
    for matrix in gcn_input:
        print_matrix(matrix)