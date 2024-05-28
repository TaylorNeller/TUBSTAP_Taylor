from Action import Action
from RangeController import RangeController

class AiTools:
    @staticmethod
    def get_all_attack_actions(teamcolor, map):
        """
        Get a list of all possible attack actions as a list of Action objects.
        """
        atk_actions = []

        # List of movable own units
        my_movable_units = map.get_units_list(teamcolor, False, True, False)

        for my_unit in my_movable_units:
            # List of attack actions for movable units
            atk_actions_of_one = RangeController.get_attack_action_list(my_unit, map)

            # Add to the overall list
            atk_actions.extend(atk_actions_of_one)

        return atk_actions

    @staticmethod
    def get_all_move_actions(teamcolor, map):
        """
        Get all possible move actions.
        """
        move_actions = []

        # List of movable own units
        my_movable_units = map.get_units_list(teamcolor, False, True, False)

        # Gather the movable positions of movable units into a list
        for my_unit in my_movable_units:
            reachable = RangeController.get_reachable_cells_matrix(my_unit, map)
            for i in range(len(reachable)):
                for j in range(len(reachable[0])):
                    if reachable[i][j]:
                        move_actions.append(Action.create_move_only_action(my_unit, i, j))

        return move_actions

    @staticmethod
    def is_effective(operation_unit, target_unit):
        """
        Check if operationUnit can attack targetUnit. Returns False if the damage effect is 0.
        """
        if operation_unit.get_spec().get_unit_atk_power(target_unit.get_type_of_unit()) != 0:
            return True
        return False

    @staticmethod
    def get_all_actions(team_color, map):
        """
        Generate all legal moves in the current state.
        """
        all_actions = []

        all_actions.extend(AiTools.get_all_attack_actions(team_color, map))
        all_actions.extend(AiTools.get_all_move_actions(team_color, map))

        return all_actions

    @staticmethod
    def get_num_of_dont_moved_unit(team_color, map):
        """
        Return the number of units that have not moved.
        """
        return len(map.get_units_list(team_color, False, True, False))

    @staticmethod
    def get_deffensive_actions(teamcolor, map, risk_of_map, low_threshold, high_threshold):
        """
        Return actions with the specified risk level or below.
        For the risk map, refer to calc_risk_map.
        """
        move_actions = []

        # List of movable own units
        my_movable_units = map.get_units_list(teamcolor, False, True, False)

        # Gather the movable positions of movable units into a list
        for my_unit in my_movable_units:
            reachable = RangeController.get_reachable_cells_matrix(my_unit, map)
            for i in range(1, len(reachable)):
                for j in range(1, len(reachable[0])):
                    if reachable[i][j] and low_threshold <= risk_of_map[i][j] <= high_threshold:
                        move_actions.append(Action.create_move_only_action(my_unit, i, j))

        return move_actions

    @staticmethod
    def calc_risk_map(team_color, map):
        """
        Write the risk level to places within the range of enemy units' attacks.
        The risk level indicates the number of enemy units that can potentially attack that position.
        Higher values indicate higher risk (maximum is the number of attacks from 4 directions + the number of enemy self-propelled artillery).
        Note: The calculation does not consider the order of unit actions, so it may be slightly inaccurate at times.
        """
        dx = [+1, 0, -1, 0]
        dy = [0, -1, 0, +1]

        en_units = map.get_units_list(team_color, False, False, True)

        if_map = [[[False] * 5 for _ in range(map.get_y_size())] for _ in range(map.get_x_size()) for _ in range(len(en_units))]
        risk_of_map = [[0] * map.get_y_size() for _ in range(map.get_x_size())]

        i = 0

        for u in en_units:
            # For direct attack type units
            if u.get_spec().is_direct_attack_type():
                reachable_cells = RangeController.get_reachable_cells_matrix(u, map)

                for x in range(1, map.get_x_size() - 1):
                    for y in range(1, map.get_y_size() - 1):
                        # Check the positions above, below, left, and right of the corresponding cell
                        for r in range(4):
                            # If any of the above, below, left, or right positions are within the movable range, (x, y) is an attackable position
                            if reachable_cells[x + dx[r]][y + dy[r]]:
                                if_map[x][y][i][r] = True
            # For indirect attack units
            else:
                attackable_cells = RangeController.get_attackable_cells_matrix(u, map)

                for x in range(1, map.get_x_size() - 1):
                    for y in range(1, map.get_y_size() - 1):
                        # If (x, y) is an attackable position
                        if attackable_cells[x][y]:
                            if_map[x][y][i][4] = True
            i += 1

        for x in range(1, map.get_x_size() - 1):
            for y in range(1, map.get_y_size() - 1):
                # Initialization
                tor_f_unit = [False] * len(en_units)
                tor_f_direction = [False] * 4
                risk_of_unit = 0
                risk_of_line = 0
                risk_of_indirection = 0

                # Check the number of units that can attack the corresponding cell
                for k in range(len(en_units)):
                    tor_f_unit[k] = if_map[x][y][k][0] or if_map[x][y][k][1] or if_map[x][y][k][2] or if_map[x][y][k][3]
                    if tor_f_unit[k]:
                        risk_of_unit += 1
                    if if_map[x][y][k][4]:
                        risk_of_indirection += 1

                # Check the directions from which the corresponding cell can be attacked
                for r in range(4):
                    for k in range(len(en_units)):
                        tor_f_direction[r] |= if_map[x][y][k][r]
                    if tor_f_direction[r]:
                        risk_of_line += 1

                if risk_of_line > risk_of_unit:
                    risk_of_map[x][y] = risk_of_unit + risk_of_indirection
                else:
                    risk_of_map[x][y] = risk_of_line + risk_of_indirection

        return risk_of_map

    @staticmethod
    def calc_if_map(team_color, map):
        """
        Write the risk level to places within the range of enemy units' attacks in the risk level array.
        """
        dx = [+1, 0, -1, 0]
        dy = [0, -1, 0, +1]

        if_map = [[0] * map.get_y_size() for _ in range(map.get_x_size())]
        en_units = map.get_units_list(team_color, False, False, True)

        for u in en_units:
            # For direct attack type units
            if u.get_spec().is_direct_attack_type():
                reachable_cells = RangeController.get_reachable_cells_matrix(u, map)

                for x in range(1, map.get_x_size() - 1):
                    for y in range(1, map.get_y_size() - 1):
                        # Check the positions above, below, left, and right of the corresponding cell
                        for r in range(4):
                            # If any of the above, below, left, or right positions are within the movable range, (x, y) is an attackable position
                            if reachable_cells[x + dx[r]][y + dy[r]]:
                                if_map[x][y] += 1
            # For indirect attack units
            else:
                attackable_cells = RangeController.get_attackable_cells_matrix(u, map)

                for x in range(1, map.get_x_size() - 1):
                    for y in range(1, map.get_y_size() - 1):
                        # If (x, y) is an attackable position
                        if attackable_cells[x][y]:
                            if_map[x][y] += 1

        return if_map