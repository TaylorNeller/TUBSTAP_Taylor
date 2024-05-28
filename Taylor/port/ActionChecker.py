from Action import Action
from Logger import Logger
from RangeController import RangeController

class ActionChecker:
    # The reason why the move is illegal
    last_error = ""

    @staticmethod
    def is_the_action_legal_move(act, map):
        """
        Check if the action is possible. Traditional version that displays a dialog.
        """
        return ActionChecker.is_the_action_legal_move_internal(act, map, True)

    @staticmethod
    def is_the_action_legal_move_silent(act, map):
        """
        Check if the action is possible. Can only check without displaying a dialog.
        """
        return ActionChecker.is_the_action_legal_move_internal(act, map, False)

    @staticmethod
    def log(show_dialog, message):
        """
        Display a message.
        """
        ActionChecker.last_error = message
        if show_dialog:
            Logger.show_dialog_message(message)

    @staticmethod
    def is_the_action_legal_move_internal(act, map, show_dialog):
        """
        The main body. Can set whether to display a dialog or not.
        """
        ActionChecker.last_error = ""

        # If no action type is selected
        if act.action_type == -1:
            ActionChecker.log(show_dialog, "ActionChecker: Action type is not set.")
            return False

        # Turn end is OK
        if act.action_type == Action.ACTIONTYPE_TURNEND or act.action_type == Action.ACTIONTYPE_SURRENDER:
            return True

        units = map.get_units()  # Map unit array

        # Check if the unit to operate is selected
        if act.operation_unit_id < 0 or act.operation_unit_id > len(units) - 1:
            ActionChecker.log(show_dialog, "ActionChecker: Operation unit ID is out of range")
            return False

        op_unit = map.get_unit(act.operation_unit_id)  # Operated unit

        # Check the existence of the operation unit
        if units[act.operation_unit_id] is None:
            ActionChecker.log(show_dialog, "ActionChecker: Attempting to operate a non-existent unit.")
            return False

        # Check if the enemy unit is not being moved
        if units[act.operation_unit_id].get_team_color() != act.team_color:
            ActionChecker.log(show_dialog, "ActionChecker: Attempting to operate an enemy unit.")
            return False  # Added in version 0.104. Bug.

        # Check if the unit has already taken action
        if units[act.operation_unit_id].is_action_finished():
            ActionChecker.log(show_dialog, "ActionChecker: Attempting to operate a unit that has already taken action.")
            return False  # Added in version 0.104. Bug.

        # Check the destination
        dest_unit = map.get_unit_at(act.destination_x_pos, act.destination_y_pos)
        if dest_unit is not None and dest_unit.get_ID() != op_unit.get_ID():
            ActionChecker.log(show_dialog, "ActionChecker: There is already another unit at the destination.")
            return False
        elif act.destination_x_pos < 1 or act.destination_x_pos > map.get_x_size() - 2:
            ActionChecker.log(show_dialog, "ActionChecker: Destination X coordinate is outside the map range.")
            return False
        elif act.destination_y_pos < 1 or act.destination_y_pos > map.get_y_size() - 2:
            ActionChecker.log(show_dialog, "ActionChecker: Destination Y coordinate is outside the map range.")
            return False

        movable_cell = RangeController.get_reachable_cells_matrix(op_unit, map)  # Matrix of movable range

        if not movable_cell[op_unit.get_x_pos()][op_unit.get_y_pos()]:
            ActionChecker.log(show_dialog, "ActionChecker: Attempting to move a unit outside the movable range.")
            return False

        if act.action_type == Action.ACTIONTYPE_MOVEANDATTACK:
            # Check the attack target
            if act.target_unit_id == -1:
                ActionChecker.log(show_dialog, "ActionChecker: No target unit is selected")
                return False
            elif units[act.target_unit_id] is None:
                ActionChecker.log(show_dialog, "ActionChecker: Attempting to target a non-existent unit.")
                return False
            elif op_unit.get_team_color() == units[act.target_unit_id].get_team_color():
                ActionChecker.log(show_dialog, "ActionChecker: Attempting to attack a unit of the same team.")
                return False

            attackable_cell = RangeController.get_attackable_cells_matrix(op_unit, map)  # Attackable range
            # If attempting to attack an enemy outside the range
            if not attackable_cell[units[act.target_unit_id].get_x_pos()][units[act.target_unit_id].get_y_pos()]:
                ActionChecker.log(show_dialog, "ActionChecker: Attempting to attack an enemy outside the attack range")
                return False

        return True

    @staticmethod
    def is_effective(operation_unit, target_unit):
        """
        Check if operationUnit can attack targetUnit. Returns false if the damage effect is 0.
        """
        if operation_unit.get_spec().get_unit_atk_power(target_unit.get_type_of_unit()) != 0:
            return True
        return False