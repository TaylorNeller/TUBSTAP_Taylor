from Consts import Consts

class Unit:
    def __init__(self, x=None, y=None, id=None, team=None, HP=None, action_finished=None, spec=None):
        if x is not None:
            self.x_pos = x
            self.y_pos = y
            self.id = id
            self.HP = HP
            self.team_color = team
            self.spec = spec
            self.action_finished = action_finished == 1
            self.x_ints = []
        else:
            self.x_pos = None
            self.y_pos = None
            self.HP = None
            self.team_color = None
            self.id = None
            self.action_finished = None
            self.spec = None
            self.x_ints = None

    def get_HP(self):
        """Get the hit points"""
        return self.HP

    def is_dead(self):
        """Check if the unit is destroyed (HP is 0 or less)"""
        return self.HP == 0


    def get_x_pos(self):
        """Get the X position on the map"""
        return self.x_pos

    def get_y_pos(self):
        """Get the Y position on the map"""
        return self.y_pos

    def get_type_of_unit(self):
        """Get the type of the unit"""
        return self.spec.get_unit_type()

    def get_ID(self):
        """Get the ID of the unit"""
        return self.id

    def get_team_color(self):
        """Get the team color of the unit"""
        return self.team_color

    def get_name(self):
        """Get the name of the unit"""
        return self.spec.get_spec_name()

    def get_mark(self):
        """Get the mark of the unit"""
        return self.spec.get_spec_mark()

    def get_spec(self):
        """Get the spec of the unit"""
        return self.spec

    def get_x_ints(self):
        """Get the X_ints array"""
        return self.x_ints

    def get_x_int(self, idx):
        """Get the value at the specified index of X_ints"""
        return self.x_ints[idx]

    def is_action_finished(self):
        """Check if the action is finished"""
        return self.action_finished

    def set_HP(self, hp):
        """Set the hit points"""
        self.HP = hp

    def set_x_pos(self, x):
        """Set the X position on the map"""
        self.x_pos = x

    def set_y_pos(self, y):
        """Set the Y position on the map"""
        self.y_pos = y

    def set_pos(self, x, y):
        """Set the position on the map"""
        self.x_pos = x
        self.y_pos = y

    def set_x_int(self, idx, value):
        """Set the value at the specified index of X_ints"""
        self.x_ints[idx] = value

    def set_x_ints(self, arg_ints):
        """Set the X_ints array"""
        self.x_ints = arg_ints

    def init_x_ints(self, length):
        """Initialize the X_ints array with the specified length"""
        self.x_ints = [0] * length

    def set_action_finished(self, action_finish_flag):
        """Set the action finished flag"""
        self.action_finished = action_finish_flag

    def raise_HP(self, value):
        """Increase the hit points by the specified value"""
        self.HP += value

    def reduce_HP(self, value):
        """Decrease the hit points by the specified value, setting it to 0 if it becomes 0 or less"""
        self.HP = max(0, self.HP - value)

    def to_string(self):
        """Convert the unit information to a string for display purposes"""
        str_info = f"ID:{self.id}  "
        str_info += f"HP:{self.HP}  "
        str_info += f"Pos:({self.x_pos}, {self.y_pos})  "
        str_info += f"Type:{self.spec.get_spec_name()}  "
        if self.team_color == Consts.RED_TEAM:
            str_info += "Team:RED"
        elif self.team_color == Consts.BLUE_TEAM:
            str_info += "Team:BLUE"
        return str_info

    def to_short_string(self):
        """Convert the unit information to a short string for logging purposes"""
        return f"{self.spec.get_spec_mark()}{self.HP}({self.x_pos}, {self.y_pos})"

    def create_deep_clone(self):
        """Create a deep clone of the unit, with all variables including x_ints list"""
        clone = Unit(self.x_pos, self.y_pos, self.id, self.team_color, self.HP, 1 if self.action_finished else 0, self.spec)
        clone.set_x_ints(self.x_ints.copy())
        return clone