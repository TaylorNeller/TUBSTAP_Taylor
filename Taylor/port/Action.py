class Action:
    # Action types
    ACTIONTYPE_SURRENDER = 0     # Surrender
    ACTIONTYPE_MOVEONLY = 1      # Move only
    ACTIONTYPE_MOVEANDATTACK = 2 # Move and attack
    ACTIONTYPE_TURNEND = 3       # End turn

    def __init__(self, action_type=-1, team_color=None, operation_unit_id=-1, target_unit_id=-1,
                 from_x_pos=-1, from_y_pos=-1, dest_x_pos=-1, dest_y_pos=-1, attack_x_pos=-1, attack_y_pos=-1):
        # The following five are basically specified by the user
        self.action_type = action_type      # Action type
        self.destination_x_pos = dest_x_pos # Destination position
        self.destination_y_pos = dest_y_pos
        self.operation_unit_id = operation_unit_id # Acting unit
        self.target_unit_id = target_unit_id       # Target unit

        # The following five are automatically set
        self.team_color = team_color  # Team color of the acting unit
        self.attack_x_pos = attack_x_pos # Position of the target unit
        self.attack_y_pos = attack_y_pos
        self.from_x_pos = from_x_pos   # Previous position of the acting unit
        self.from_y_pos = from_y_pos

        # Extension variables, feel free to use them
        self.X_attack_damage = 0  # Damage dealt
        self.X_counter_damage = 0 # Counter-attack damage
        self.X_action_evaluation_value = 0 # Action evaluation value

    @staticmethod
    def create_move_only_action(op_unit, dest_x_pos, dest_y_pos):
        """Static function to generate and return a move-only action"""
        action = Action()
        action.operation_unit_id = op_unit.get_ID()
        action.team_color = op_unit.get_team_color()
        action.destination_x_pos = dest_x_pos
        action.destination_y_pos = dest_y_pos
        action.from_x_pos = op_unit.get_x_pos()
        action.from_y_pos = op_unit.get_y_pos()
        action.action_type = Action.ACTIONTYPE_MOVEONLY
        return action

    @staticmethod
    def create_attack_action(op_unit, dest_x_pos, dest_y_pos, target_unit):
        """Static function to generate and return an action with an attack"""
        action = Action()
        action.operation_unit_id = op_unit.get_ID()
        action.team_color = op_unit.get_team_color()
        action.target_unit_id = target_unit.get_ID()
        action.destination_x_pos = dest_x_pos
        action.destination_y_pos = dest_y_pos
        action.from_x_pos = op_unit.get_x_pos()
        action.from_y_pos = op_unit.get_y_pos()
        action.action_type = Action.ACTIONTYPE_MOVEANDATTACK
        action.attack_x_pos = target_unit.get_x_pos()
        action.attack_y_pos = target_unit.get_y_pos()
        return action

    @staticmethod
    def create_turn_end_action():
        """Action type TURNEND (optional)
        Generating an empty Action object and directly returning it is considered as a turn end"""
        action = Action()
        action.action_type = Action.ACTIONTYPE_TURNEND
        return action

    @staticmethod
    def resignation():
        """Generate a resignation"""
        action = Action()
        action.action_type = Action.ACTIONTYPE_SURRENDER
        return action

    def to_one_line_string(self):
        """A slightly more readable one-line string. Used for logging."""
        return f"ID:{self.operation_unit_id} ({self.from_x_pos},{self.from_y_pos})" + \
               f" -> ({self.destination_x_pos},{self.destination_y_pos})" + \
               f" target={self.target_unit_id} ({self.attack_x_pos},{self.attack_y_pos})"

    def to_sgf_string(self):
        """Used by SGFManager to save the action history"""
        return f"{self.operation_unit_id},{self.destination_x_pos},{self.destination_y_pos}," + \
               f"{self.from_x_pos},{self.from_y_pos},{self.target_unit_id},{self.attack_x_pos}," + \
               f"{self.attack_y_pos},{self.action_type},{self.X_attack_damage},{self.X_counter_damage}"

    def __str__(self):
        if self.action_type == Action.ACTIONTYPE_MOVEONLY:
            return f"{self.from_x_pos}:{self.from_y_pos}:{self.destination_x_pos}:{self.destination_y_pos}:{self.destination_x_pos}:{self.destination_y_pos}"
        elif self.action_type == Action.ACTIONTYPE_MOVEANDATTACK:
            return f"{self.from_x_pos}:{self.from_y_pos}:{self.destination_x_pos}:{self.destination_y_pos}:{self.attack_x_pos}:{self.attack_y_pos}"
        elif self.action_type == Action.ACTIONTYPE_TURNEND:
            return "TURNEND"
        elif self.action_type == Action.ACTIONTYPE_SURRENDER:
            return "SURRENDER"
        else:
            return "UNKNOWN"

    def to_string(self):
        """Describe the action details in a string"""
        return f"from->({self.from_x_pos},{self.from_y_pos}) " + \
               f"to->({self.destination_x_pos},{self.destination_y_pos}) " + \
               f"attack->({self.attack_x_pos},{self.attack_y_pos})\n"

    @staticmethod
    def parse(action_string):
        """Receive the action history of one unit from the game record as a string, generate an Action, and return it"""
        values = action_string.split(',')
        action = Action()
        action.operation_unit_id = int(values[0])
        action.destination_x_pos = int(values[1])
        action.destination_y_pos = int(values[2])
        action.from_x_pos = int(values[3])
        action.from_y_pos = int(values[4])
        action.target_unit_id = int(values[5])
        action.attack_x_pos = int(values[6])
        action.attack_y_pos = int(values[7])
        action.action_type = int(values[8])
        action.X_attack_damage = int(values[9])
        action.X_counter_damage = int(values[10])
        return action

    def create_deep_clone(self):
        """Generate a deep clone (not strictly deep)"""
        copied = Action(self.action_type, self.team_color, self.operation_unit_id, self.target_unit_id,
                        self.from_x_pos, self.from_y_pos, self.destination_x_pos, self.destination_y_pos,
                        self.attack_x_pos, self.attack_y_pos)
        copied.X_action_evaluation_value = self.X_action_evaluation_value
        copied.X_attack_damage = self.X_attack_damage
        copied.X_counter_damage = self.X_counter_damage
        return copied