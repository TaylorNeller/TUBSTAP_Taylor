from Action import Action
from RangeController import RangeController
from DamageCalculator import DamageCalculator

class MTools: 
    """Tools used in Monte Carlo Tree Search (MCTS)."""

    @staticmethod
    def get_all_attack_actions(team_color, map_):
        """
        Get a list of all possible attack actions for movable units of a team.
        """
        attack_actions = []

        # List of movable units of the team
        my_movable_units = map_.get_units_list(team_color, False, True, False)

        for my_unit in my_movable_units:
            # Attack actions for the unit
            attack_actions_of_one = RangeController.get_attack_action_list(my_unit, map_)

            # Include in the overall list
            attack_actions.extend(attack_actions_of_one)

        return attack_actions

    @staticmethod
    def get_all_move_actions(team_color, map_):
        """
        Get a list of all possible move actions for movable units of a team.
        """
        move_actions = []

        # List of movable units of the team
        my_movable_units = map_.get_units_list(team_color, False, True, False)

        # Compile a list of movable positions for each unit
        for my_unit in my_movable_units:
            reachable = RangeController.get_reachable_cells_matrix(my_unit, map_)
            for i in range(len(reachable)):
                for j in range(len(reachable[0])):
                    if reachable[i][j]:
                        move_actions.append(Action.create_move_only_action(my_unit, i, j))

        return move_actions

    @staticmethod
    def is_effective(operation_unit, target_unit):
        """
        Check if operation_unit can attack target_unit effectively (non-zero damage).
        """
        if operation_unit.get_spec().get_unit_atk_power(target_unit.get_type_of_unit()) != 0:
            return True
        return False

    @staticmethod
    def get_unit_actions(unit, map_):
        """
        Get a list of all possible actions for a given unit.
        """
        unit_actions = []

        # Get attack actions
        attack_actions = RangeController.get_attack_action_list(unit, map_)

        for attack in attack_actions:
            attack_damages = DamageCalculator.calculate_damages(map_, attack)  # Calculate attack and counter-attack damages

            if attack_damages[0] != 0:
                unit_actions.append(attack)

        # Get move actions
        movable = RangeController.get_reachable_cells_matrix(unit, map_)

        for x in range(1, map_.get_x_size() - 1):
            for y in range(1, map_.get_y_size() - 1):
                if movable[x][y]:
                    # Uncomment the following block if additional checks are needed
                    """
                    exist_flag = False

                    for enemy_unit in map_.get_units_list(unit.get_team_color(), False, False, True):
                        pos_x = enemy_unit.get_x_pos()  # Enemy unit position
                        pos_y = enemy_unit.get_y_pos()

                        DX = [1, 0, -1, 0]
                        DY = [0, -1, 0, 1]

                        for i in range(4):
                            if x + DX[i] == pos_x and y + DY[i] == pos_y:
                                exist_flag = True
                                break

                    if not exist_flag:
                    """
                    unit_actions.append(Action.create_move_only_action(unit, x, y))

        return unit_actions

    @staticmethod
    def get_all_attack_units(team_color, map_):
        """
        Get a list of all units that can perform an attack action.
        """
        attack_units = []

        # List of movable units of the team
        my_movable_units = map_.get_units_list(team_color, False, True, False)

        for my_unit in my_movable_units:
            # Attack actions for the unit
            attack_actions_of_one = RangeController.get_attack_action_list(my_unit, map_)

            if attack_actions_of_one:
                attack_units.append(my_unit)

        return attack_units

    @staticmethod
    def get_all_actions(team_color, map_):
        """
        Generate all legal actions in the current state.
        """
        all_actions = []

        all_actions.extend(MTools.get_all_attack_actions(team_color, map_))
        all_actions.extend(MTools.get_all_move_actions(team_color, map_))

        return all_actions

    @staticmethod
    def get_near_units_matrix(op_unit, map_):
        """
        Get a matrix of units within the movement range of op_unit.
        Units not in movement range or cells without units are represented as None.
        """
        DX = [1, 0, -1, 0]
        DY = [0, -1, 0, 1]

        unit_spec = op_unit.get_spec()  # Spec of the unit to move
        unit_step = unit_spec.get_unit_step()  # Number of steps the unit can move
        unit_color = op_unit.get_team_color()  # Team color of the unit

        x_size = map_.get_x_size()
        y_size = map_.get_y_size()
        reachable = [[False for _ in range(y_size)] for _ in range(x_size)]
        near_units = [[None for _ in range(y_size)] for _ in range(x_size)]  # Matrix to return

        pos_counts = [0] * (unit_step + 1)  # Number of reachable positions with remaining steps
        pos_x = [[0] * 100 for _ in range(unit_step + 1)]  # Positions' x-coordinates
        pos_y = [[0] * 100 for _ in range(unit_step + 1)]  # Positions' y-coordinates

        # The current location is reachable with remaining steps equal to unit_step
        pos_counts[unit_step] = 1
        pos_x[unit_step][0] = op_unit.get_x_pos()
        pos_y[unit_step][0] = op_unit.get_y_pos()
        reachable[op_unit.get_x_pos()][op_unit.get_y_pos()] = True

        # Explore reachable positions based on remaining steps
        for rest_step in range(unit_step, 0, -1):
            for i in range(pos_counts[rest_step]):
                x = pos_x[rest_step][i]
                y = pos_y[rest_step][i]

                # Check adjacent cells
                for r in range(4):
                    new_x = x + DX[r]
                    new_y = y + DY[r]

                    # Check if within map bounds
                    if new_x < 0 or new_x >= x_size or new_y < 0 or new_y >= y_size:
                        continue

                    move_cost = unit_spec.get_move_cost(map_.get_field_type(new_x, new_y))
                    new_rest = rest_step - move_cost
                    if new_rest < 0:
                        continue  # Cannot enter due to insufficient movement points

                    unit_ = map_.get_unit(new_x, new_y)
                    if unit_:
                        if near_units[new_x][new_y] is None:
                            near_units[new_x][new_y] = unit_

                        if unit_.get_team_color() != unit_color:
                            continue  # Cannot enter cell occupied by enemy unit

                    if not reachable[new_x][new_y]:
                        pos_x[new_rest][pos_counts[new_rest]] = new_x
                        pos_y[new_rest][pos_counts[new_rest]] = new_y
                        pos_counts[new_rest] += 1
                        reachable[new_x][new_y] = True

        # Cells occupied by friendly units are not reachable
        for unit_ in map_.get_units_list(unit_color, True, True, True):
            reachable[unit_.get_x_pos()][unit_.get_y_pos()] = False

        # Consider the unit's current position as reachable
        reachable[op_unit.get_x_pos()][op_unit.get_y_pos()] = True

        # The current position should not have a near unit
        near_units[op_unit.get_x_pos()][op_unit.get_y_pos()] = None

        return near_units
