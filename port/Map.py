import Consts
import Action
import Unit

class Map:
    def __init__(self, map_file_name=None):
        self.x_size = None
        self.y_size = None
        self.map_field_type = None
        self.map_unit = None
        self.max_unit_num = [0, 0]
        self.units = None
        self.num_of_alive_units = [0, 0]
        self.turn_count = None
        self.turn_limit = None
        self.draw_hp_threshold = None
        self.map_file_name = map_file_name
        self.reverse = False

        if map_file_name is not None:
            self.load_map_file(map_file_name)

    def get_x_size(self):
        return self.x_size

    def get_y_size(self):
        return self.y_size

    def get_units(self):
        """Get the array containing all units of all teams"""
        return self.units

    def get_unit(self, unit_id):
        """Get the unit specified by the argument (Id), returning None if out of range (version 0.104 and later)"""
        if unit_id < 0 or unit_id >= len(self.units):
            return None
        else:
            return self.units[unit_id]

    def get_unit_at(self, x, y):
        """Get the unit at a specific position"""
        return self.map_unit[x][y]

    def get_map_unit(self):
        """Get the entire map-to-unit matrix"""
        return self.map_unit

    def get_field_type_array(self):
        """Get the entire terrain matrix"""
        return self.map_field_type

    def get_field_type(self, x, y):
        """Get the terrain at a specific position"""
        return self.map_field_type[x][y]

    def get_turn_count(self):
        return self.turn_count

    def get_turn_limit(self):
        return self.turn_limit

    def get_draw_hp_threshold(self):
        return self.draw_hp_threshold

    def get_num_of_alive_color_units(self, team_color):
        """Return the number of remaining units of the specified team"""
        return self.num_of_alive_units[team_color]

    def get_field_defensive_effect(self, x, y):
        """Get the defensive effect of the field"""
        return Consts.FIELD_DEFENSE[self.map_field_type[x][y]]

    def is_all_unit_action_finished(self, team_color):
        """Check if all units of the specified team have finished their actions"""
        return len(self.get_units_list(team_color, False, True, False)) == 0

    def get_units_list(self, team_color, including_my_action_finished_units, including_my_movable_units, including_opponent_units):
        """
        Return a list of units corresponding to the specified 'including' conditions.
        If duplicates occur, they will be merged into one.
        """
        units_list = []

        for unit in self.units:
            if unit is None:
                continue
            if including_my_action_finished_units and unit.is_action_finished() and unit.get_team_color() == team_color:
                units_list.append(unit)  # Units that have finished their actions
                continue
            if including_my_movable_units and not unit.is_action_finished() and unit.get_team_color() == team_color:
                units_list.append(unit)  # Units that have not finished their actions
                continue
            if including_opponent_units and unit.get_team_color() != team_color:
                units_list.append(unit)  # Units of the opposing team
                continue

        return units_list

    def execute_action(self, action):
        """Apply an action to the map, which can be used for tree search, etc."""
        op_unit = self.get_unit(action.operation_unit_id)  # Unit that performed the action
        
        if action.action_type == Action.ACTIONTYPE_MOVEONLY:
            # Change the unit's position
            self.change_unit_location(action.destination_x_pos, action.destination_y_pos, op_unit)
            op_unit.set_action_finished(True)  # Set the action finished flag
        elif action.action_type == Action.ACTIONTYPE_MOVEANDATTACK:
            target_unit = self.get_unit(action.target_unit_id)  # Target unit of the attack

            # First, change the unit's position
            self.change_unit_location(action.destination_x_pos, action.destination_y_pos, op_unit)
            damages = DamageCalculator.calculate_damages(self, action)  # Attack and counter-attack damages

            target_unit.reduce_HP(damages[0])  # Reduce the HP of the target unit
            if target_unit.get_HP() == 0:
                self.delete_unit(target_unit)  # If the HP becomes 0 or less, delete the unit

            op_unit.reduce_HP(damages[1])  # Counter-attack processing
            if op_unit.get_HP() == 0:
                self.delete_unit(op_unit)  # If destroyed by counter-attack, delete the unit
                return

            op_unit.set_action_finished(True)  # Set the action finished flag
        elif action.action_type == Action.ACTIONTYPE_TURNEND:
            movable_units = self.get_units_list(action.team_color, False, True, False)  # List of units that have not acted

            for unit in movable_units:
                unit.set_action_finished(True)  # Set the action finished flag

    def load_map_file(self, file_name):
        """Load the map at the start of the game"""
        # First, read everything at once
        with open(file_name, 'r') as file:
            full_line = file.read()

        # Split by semicolon
        lines = full_line.split(';')
        size_x = size_y = unit_num_r = unit_num_b = turn_limit = hp_ts = ""
        for line in lines:
            one = SGFManager.search_one(line, "SIZEX")
            if one != "":
                size_x = one

            one = SGFManager.search_one(line, "SIZEY")
            if one != "":
                size_y = one

            one = SGFManager.search_one(line, "UNITNUMRED")
            if one != "":
                unit_num_r = one

            one = SGFManager.search_one(line, "UNITNUMBLUE")
            if one != "":
                unit_num_b = one

            one = SGFManager.search_one(line, "TURNLIMIT")
            if one != "":
                turn_limit = one

            one = SGFManager.search_one(line, "HPTHRESHOLD")
            if one != "":
                hp_ts = one

        # Load map size and unit count
        self.x_size = int(size_x)
        self.y_size = int(size_y)
        self.max_unit_num[0] = int(unit_num_r)
        self.max_unit_num[1] = int(unit_num_b)
        self.draw_hp_threshold = int(hp_ts)
        self.turn_limit = int(turn_limit)

        # Initialization
        self.map_field_type = [[0] * self.y_size for _ in range(self.x_size)]
        self.map_unit = [[None] * self.y_size for _ in range(self.x_size)]
        self.units = [None] * (self.max_unit_num[0] + self.max_unit_num[1])

        # Load map placement (MS = MapSet)
        st_list = []

        for line in lines:
            ms_line = SGFManager.search_one(line, "MAP")
            if ms_line == "":
                continue

            ones = ms_line.split(',')

            if len(ones) > self.x_size:
                Logger.show_dialog_message("Map file X size exceeded.")

            st_list.append(ones)

        if len(st_list) > self.y_size:
            Logger.show_dialog_message("Map file Y size exceeded.")

        # Store the terrain type at the corresponding position in the field type array
        for y in range(len(st_list)):
            ones = st_list[y]
            for x in range(len(ones)):
                self.map_field_type[x][y] = int(ones[x])

        self.load_units(lines)  # Initialize unit positions

    def load_units(self, lines):
        """Load unit positions. Called when the map is generated."""
        # Initialize map placement
        self.map_unit = [[None] * self.y_size for _ in range(self.x_size)]
        self.units = [None] * (self.max_unit_num[0] + self.max_unit_num[1])

        # Initialize unit count
        self.num_of_alive_units[0] = 0
        self.num_of_alive_units[1] = 0

        # Load unit placement (US = UnitSet)
        for line in lines:
            us_line = SGFManager.search_one(line, "UNIT")
            if us_line == "":
                continue

            ones = us_line.split(',')
            x = int(ones[0])
            y = int(ones[1])
            team = int(ones[3])
            HP = int(ones[4])
            action_finished = int(ones[5])
            if self.reverse:
                team = (team + 1) % 2

            self.add_unit(x, y, ones[2], team, HP, action_finished)

    def send_map_file(self):
        """Communication-oriented map data sending function"""
        # First, read everything at once
        with open(self.map_file_name, 'r') as file:
            full_line = file.read()

        return full_line

    def receive_map_file(self, full_line):
        """Communication-oriented map data receiving function"""
        # Split by semicolon
        lines = full_line.split(';')
        size_x = size_y = unit_num_r = unit_num_b = turn_limit = hp_ts = ""
        for line in lines:
            one = SGFManager.search_one(line, "SIZEX")
            if one != "":
                size_x = one

            one = SGFManager.search_one(line, "SIZEY")
            if one != "":
                size_y = one

            one = SGFManager.search_one(line, "UNITNUMRED")
            if one != "":
                unit_num_r = one

            one = SGFManager.search_one(line, "UNITNUMBLUE")
            if one != "":
                unit_num_b = one

            one = SGFManager.search_one(line, "TURNLIMIT")
            if one != "":
                turn_limit = one

            one = SGFManager.search_one(line, "HPTHRESHOLD")
            if one != "":
                hp_ts = one

        # Load map size and unit count
        self.x_size = int(size_x)
        self.y_size = int(size_y)
        self.max_unit_num[0] = int(unit_num_r)
        self.max_unit_num[1] = int(unit_num_b)
        self.draw_hp_threshold = int(hp_ts)
        self.turn_limit = int(turn_limit)

        # Initialization
        self.map_field_type = [[0] * self.y_size for _ in range(self.x_size)]
        self.map_unit = [[None] * self.y_size for _ in range(self.x_size)]
        self.units = [None] * (self.max_unit_num[0] + self.max_unit_num[1])

        # Load map placement (MS = MapSet)
        st_list = []

        for line in lines:
            ms_line = SGFManager.search_one(line, "MAP")
            if ms_line == "":
                continue

            ones = ms_line.split(',')

            if len(ones) > self.x_size:
                Logger.show_dialog_message("Map file X size exceeded.")

            st_list.append(ones)

        if len(st_list) > self.y_size:
            Logger.show_dialog_message("Map file Y size exceeded.")

        # Store the terrain type at the corresponding position in the field type array
        for y in range(len(st_list)):
            ones = st_list[y]
            for x in range(len(ones)):
                self.map_field_type[x][y] = int(ones[x])

        self.load_units(lines)  # Initialize unit positions

    def act_control(self, action):
        """Internally process the act. It can be fast-forwarded but not rewound."""
        if action.action_type == Action.ACTIONTYPE_MOVEONLY:
            self.change_unit_location(action.destination_x_pos, action.destination_y_pos, self.units[action.operation_unit_id])
            self.units[action.operation_unit_id].set_action_finished(True)
        elif action.action_type == Action.ACTIONTYPE_MOVEANDATTACK:
            self.change_unit_location(action.destination_x_pos, action.destination_y_pos, self.units[action.operation_unit_id])
            self.units[action.operation_unit_id].reduce_HP(action.X_counter_damage)
            self.units[action.target_unit_id].reduce_HP(action.X_attack_damage)
            self.units[action.operation_unit_id].set_action_finished(True)

            if self.units[action.operation_unit_id].get_HP() <= 0:
                self.delete_unit(self.units[action.operation_unit_id])
            if self.units[action.target_unit_id].get_HP() <= 0:
                self.delete_unit(self.units[action.target_unit_id])

    def add_unit(self, x, y, name, team, HP, action_finished):
        """Method for generating units"""
        max_units_cnt = sum(self.num_of_alive_units)  # Maximum number of units, corresponding to the ID of each unit

        self.map_unit[x][y] = Unit(x, y, max_units_cnt, team, HP, action_finished, Spec.get_spec(name))
        self.units[max_units_cnt] = self.map_unit[x][y]

        if team == Consts.RED_TEAM:
            self.num_of_alive_units[0] += 1
        elif team == Consts.BLUE_TEAM:
            self.num_of_alive_units[1] += 1

    def reset_action_finish(self):
        """Reset the actionFinish flag of all Units to false.
        Make them in a non-action state. Used for reproducing the game record."""
        self.enable_units_action(Consts.BLUE_TEAM)
        self.enable_units_action(Consts.RED_TEAM)

    def set_turn_count(self, turn):
        self.turn_count = turn

    def inc_turn_count(self):
        self.turn_count += 1

    def delete_unit(self, dead_unit):
        """Delete a destroyed unit"""
        if dead_unit.get_team_color() == Consts.RED_TEAM:
            self.num_of_alive_units[0] -= 1
        elif dead_unit.get_team_color() == Consts.BLUE_TEAM:
            self.num_of_alive_units[1] -= 1

        self.map_unit[dead_unit.get_x_pos()][dead_unit.get_y_pos()] = None