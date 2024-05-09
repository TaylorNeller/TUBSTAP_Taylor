import Action

class RangeController:
    # Constants representing up, down, left, right
    DX = [+1, 0, -1, 0]
    DY = [0, -1, 0, +1]

    @staticmethod
    def get_reachable_cells_matrix(op_unit, map):
        """Function that returns a bool matrix of the movable range of opUnit.
        Enemy units cannot be passed through, but allied units can (but cannot be the final destination)."""
        unit_spec = op_unit.get_spec()  # Spec of the unit to be moved
        unit_step = unit_spec.get_unit_step()  # Number of steps
        unit_color = op_unit.get_team_color()  # Team of the unit to be moved

        reachable = [[False] * map.get_y_size() for _ in range(map.get_x_size())]  # Reachable or not. This is returned.

        pos_counts = [0] * (unit_step + 1)  # Number of movable positions with remaining movement power as index.
        pos_x = [[0] * 100 for _ in range(unit_step + 1)]  # Movable positions with remaining movement power as index.
        pos_y = [[0] * 100 for _ in range(unit_step + 1)]  # 100 is an arbitrary number, should be made a constant.

        # The current position is a movable position with remaining movement power unitStep and is also ultimately movable.
        pos_counts[unit_step] = 1
        pos_x[unit_step][0] = op_unit.get_x_pos()
        pos_y[unit_step][0] = op_unit.get_y_pos()
        reachable[op_unit.get_x_pos()][op_unit.get_y_pos()] = True

        # From movable positions with remaining movement power restStep, look at their up, down, left, right,
        # and add them to the list if they can be moved to.
        for rest_step in range(unit_step, 0, -1):  # Current remaining movement cost
            for i in range(pos_counts[rest_step]):
                x = pos_x[rest_step][i]  # Position to focus on
                y = pos_y[rest_step][i]

                # Look at the up, down, left, right of this (x, y)
                for r in range(4):
                    new_x = x + RangeController.DX[r]  # Up, down, left, right
                    new_y = y + RangeController.DY[r]

                    new_rest = rest_step - unit_spec.get_move_cost(map.get_field_type(new_x, new_y))  # Remaining movement cost after moving
                    if new_rest < 0:
                        continue  # Hit the surroundings or not enough movement power → Cannot enter

                    u = map.get_unit_at(new_x, new_y)
                    if u is not None:
                        if u.get_team_color() != unit_color:
                            continue  # Hit an enemy unit → Cannot enter

                    # If it's not a position that has already been marked as movable, add it to the movable positions.
                    if not reachable[new_x][new_y]:
                        pos_x[new_rest][pos_counts[new_rest]] = new_x
                        pos_y[new_rest][pos_counts[new_rest]] = new_y
                        pos_counts[new_rest] += 1
                        reachable[new_x][new_y] = True

        # Positions occupied by allied units are ultimately not movable positions.
        for u in map.get_units_list(unit_color, True, True, False):
            reachable[u.get_x_pos()][u.get_y_pos()] = False

        # It's up to you how to consider your own position, but if you want to make it movable:
        reachable[op_unit.get_x_pos()][op_unit.get_y_pos()] = True

        return reachable

    @staticmethod
    def get_attack_action_list(op_unit, map):
        """Returns a list of opUnit's attack actions."""
        actions = []

        if op_unit.get_spec().is_direct_attack_type():  # If opUnit is a melee attack type
            op_units_movable_range = RangeController.get_reachable_cells_matrix(op_unit, map)  # opUnit's movable range array

            # Check if enemy units are adjacent to the movable range
            for en_unit in map.get_units_list(op_unit.get_team_color(), False, False, True):
                pos_x = en_unit.get_x_pos()  # Position of the enemy unit
                pos_y = en_unit.get_y_pos()

                for i in range(4):  # Look at the up, down, left, right of the enemy unit
                    check_x = pos_x + RangeController.DX[i]
                    check_y = pos_y + RangeController.DY[i]

                    if op_units_movable_range[check_x][check_y]:  # It means you can come to the position checkX, checkY and attack
                        actions.append(Action.create_attack_action(op_unit, check_x, check_y, en_unit))
        else:  # If it's an indirect attack type
            u_spec = op_unit.get_spec()
            # Check if enemy units are within the attackable range
            for en_unit in map.get_units_list(op_unit.get_team_color(), False, False, True):
                pos_x = en_unit.get_x_pos()  # Position of the enemy unit
                pos_y = en_unit.get_y_pos()
                dist = abs(pos_x - op_unit.get_x_pos()) + abs(pos_y - op_unit.get_y_pos())  # Distance between the enemy unit and yourself
                if u_spec.get_unit_min_attack_range() <= dist <= u_spec.get_unit_max_attack_range():
                    actions.append(Action.create_attack_action(op_unit, op_unit.get_x_pos(), op_unit.get_y_pos(), en_unit))  # Add to the list if it's within the attackable range

        return actions

    @staticmethod
    def get_attackable_cells_matrix(op_unit, map):
        """Returns the attackable range of opUnit as a bool[,] matrix.
        True is assigned to the attackable positions."""
        attackable = [[False] * map.get_y_size() for _ in range(map.get_x_size())]  # Attackable range matrix

        if op_unit.get_spec().is_direct_attack_type():  # If opUnit is a melee attack type
            movable_cells_matrix = RangeController.get_reachable_cells_matrix(op_unit, map)  # opUnit's movable range
            cells = [[False] * map.get_y_size() for _ in range(map.get_x_size())]

            for x in range(1, map.get_x_size() - 1):
                for y in range(1, map.get_y_size() - 1):
                    if map.get_unit_at(x, y) is not None and \
                            map.get_unit_at(x, y).get_team_color() == op_unit.get_team_color():
                        continue

                    # Look at the up, down, left, right positions of x, y, and positions where true is assigned are attackable range
                    for i in range(4):  # Look at the up, down, left, right of the enemy unit
                        check_x = x + RangeController.DX[i]
                        check_y = y + RangeController.DY[i]

                        if movable_cells_matrix[check_x][check_y]:
                            cells[x][y] = True  # Change to true as attackable range

            attackable = cells
        else:  # If it's an indirect attack type
            u_spec = op_unit.get_spec()

            for x in range(1, map.get_x_size() - 1):
                for y in range(1, map.get_y_size() - 1):
                    dist = abs(x - op_unit.get_x_pos()) + abs(y - op_unit.get_y_pos())  # Distance between each (x, y) coordinate and yourself
                    if u_spec.get_unit_min_attack_range() <= dist <= u_spec.get_unit_max_attack_range():  # If it's within the attackable range
                        attackable[x][y] = True  # Positions within the attackable range are set to true

        return attackable

    @staticmethod
    def is_effective(operation_unit, target_unit):
        """Check if operationUnit can attack targetUnit. Returns false if the damage effect is 0."""
        if operation_unit.get_spec().get_unit_atk_power(target_unit.get_type_of_unit()) != 0:
            return True
        return False