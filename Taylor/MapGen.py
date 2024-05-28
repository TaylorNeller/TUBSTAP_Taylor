import random

class MapGen:

    terrain_type = "plains"
    # unit_dist = "random"
    n_red = 3
    n_blue = 3

    def generate_map_file(file_path, width, height, padded=True):
        n_iters = 50

        # generate terrain
        terrain = [[0]*width for _ in range(height)]
        
        if MapGen.terrain_type == "random":
            for y in range(height):
                for x in range(width):
                    if padded and (x == 0 or y == 0 or x == width - 1 or y == height - 1):
                        terrain[y][x] = 0
                    else:
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
        else:
            for y in range(height):
                for x in range(width):
                    if padded and (x == 0 or y == 0 or x == width - 1 or y == height - 1):
                        terrain[y][x] = 0
                    else:
                        terrain[y][x] = 1

        # generate units
        unit_names = ["attacker", "fighter", "antiair", "infantry", "panzer", "cannon"]
        all_units = []
        exhausted_red = random.randint(1, MapGen.n_red // 2)
        
        for i in range(MapGen.n_red + MapGen.n_blue):
            while True:
                x = random.randint(0, width - 1)
                y = random.randint(0, height - 1)
                type_index = random.randint(0, 4) if random.randint(0, 4) == 0 else random.randint(2, len(unit_names) - 2)
                team = 0 if i < MapGen.n_red else 1
                hp = 10
                is_exhausted = 0
                if team == 0 and exhausted_red > 0:
                    exhausted_red -= 1
                    is_exhausted = 1
                
                new_unit = [x, y, type_index, team, hp, is_exhausted]

                if any(new_unit[0] == u[0] and new_unit[1] == u[1] for u in all_units):
                    continue

                if terrain[y][x] == 0 or \
                (terrain[y][x] == 2 and not (type_index == 0 or type_index == 1)) or \
                (terrain[y][x] == 4 and not (type_index == 0 or type_index == 1 or type_index == 3)):
                    continue
                
                all_units.append(new_unit)
                break

        unit_values = [11, 9, 8, 1, 7, 6]
        
        team_values = [0, 0]
        for unit in all_units:
            team_values[unit[3]] += unit_values[unit[2]] * unit[4] * 0.1

        target_percent = 0.8
        target_value = min(target_percent * team_values[0], target_percent * team_values[1])

        for team in range(2):
            iter_count = 0
            while abs(team_values[team] - target_value) > 0.05 * target_value and iter_count < n_iters:
                unit_index = random.randint(0, MapGen.n_red - 1) if team == 0 else random.randint(0, MapGen.n_blue - 1) + MapGen.n_red
                if team_values[team] > target_value:
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

        # write to file
        with open(file_path, 'w') as writer:
            writer.write(f"SIZEX[{width}];SIZEY[{height}];\nTURNLIMIT[29];HPTHRESHOLD[10];\nUNITNUMRED[{MapGen.n_red}];UNITNUMBLUE[{MapGen.n_blue}];\nAUTHOR[Taylor];\n")

            for y in range(height):
                writer.write("MAP[")
                writer.write(",".join(map(str, terrain[y])))
                writer.write("];\n")

            for unit in all_units:
                writer.write("UNIT[")
                writer.write(",".join([str(unit[0]), str(unit[1]), unit_names[unit[2]], str(unit[3]), str(unit[4]), str(unit[5])]))
                writer.write("];\n")




# main
if __name__ == "__main__":
    # Generate 10 map files
    for i in range(10):
        MapGen.generate_map_file(f"bin/Release/autobattle/genmap{i}.tbsmap", 8, 8)