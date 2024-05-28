class Spec:
    SPECTYPENUM = 6  # Number of unit types (spec count)

    def __init__(self, type_index, unit_step, min_range, max_range, can_direct_attack):
        self.type_index = type_index
        self.step = unit_step
        self.min_range = min_range
        self.max_range = max_range
        self.direct_attack = can_direct_attack

    # Set the status for each unit type
    # Arguments (type index, movement power, minimum attack range, maximum attack range, direct attack enabled flag)
    spec_f_fighter = None
    spec_a_attacker = None
    spec_t_panzer = None
    spec_u_cannon = None
    spec_r_antiAir = None
    spec_i_infantry = None

    # Status storage location corresponding to type_index below
    specs = [spec_f_fighter, spec_a_attacker, spec_t_panzer, spec_u_cannon, spec_r_antiAir, spec_i_infantry]

    # Unit names (strings) corresponding to type_index
    spec_names = ["fighter", "attacker", "panzer", "cannon", "antiair", "infantry"]

    # Short version of unit names
    spec_marks = ["F", "A", "P", "U", "R", "I"]

    # Is it an air unit (not affected by terrain effects)
    air_unit = [True, True, False, False, False, False]

    # Damage compatibility for each unit, higher values mean easier to deal damage F A P U R I order
    atk_power_array = [
        [55, 65, 0, 0, 0, 0],    # of F
        [0, 0, 105, 105, 85, 115],  # of A
        [0, 0, 55, 70, 75, 75],   # of P
        [0, 0, 60, 75, 65, 90],   # of U
        [70, 70, 15, 50, 45, 105],  # of R
        [0, 0, 5, 10, 3, 55]      # of I
    ]

    # Movement cost for each unit, lower values mean easier to move NOENTRY, Plains, Sea, Forest, Mountain, Road order
    move_cost = [
        [99, 1, 1, 1, 1, 1, 1],    # F
        [99, 1, 1, 1, 1, 1, 1],    # A
        [99, 1, 99, 2, 99, 1, 1],  # P
        [99, 1, 99, 2, 99, 1, 1],  # U
        [99, 1, 99, 2, 99, 1, 1],  # R
        [99, 1, 99, 1, 2, 1, 1]    # I
    ]

    def get_unit_atk_power(self, target_unit_type):
        """Get the attack power"""
        return self.atk_power_array[self.type_index][target_unit_type]

    def get_move_cost(self, map_field_type):
        """Get the movement cost for each field"""
        return self.move_cost[self.type_index][map_field_type]

    def get_unit_step(self):
        """Get the unit's movement power"""
        return self.step

    def get_unit_type(self):
        """Return the unit's type index"""
        return self.type_index

    def get_unit_min_attack_range(self):
        """Get the minimum value of the unit's attack range"""
        return self.min_range

    def get_unit_max_attack_range(self):
        """Get the maximum value of the unit's attack range"""
        return self.max_range

    def is_direct_attack_type(self):
        """Return the unit's attack type, True for melee, False for ranged"""
        return self.direct_attack

    def is_air_unit(self):
        """Check if the unit type does not receive terrain effects"""
        return self.air_unit[self.type_index]

    def get_spec_name(self):
        """Return the name (string) from the unit's type index, mainly used for display purposes"""
        return self.spec_names[self.type_index]

    def get_spec_mark(self):
        """Return the short name (string) from the unit's type index, mainly used for display purposes"""
        return self.spec_marks[self.type_index]

    @staticmethod
    def get_spec(unit_name):
        """Return the spec of the unit from its name (string)"""
        for i in range(len(Spec.spec_names)):
            if Spec.spec_names[i] == unit_name:
                return Spec.specs[i]  # Spec of the unit whose name (string) matches

        print("SPEC: get_spec: The unit name is inaccurate")
        return None


# Initialize the spec objects
Spec.spec_f_fighter = Spec(0, 9, 0, 1, True)
Spec.spec_a_attacker = Spec(1, 7, 0, 1, True)
Spec.spec_t_panzer = Spec(2, 6, 0, 1, True)
Spec.spec_u_cannon = Spec(3, 5, 2, 3, False)
Spec.spec_r_antiAir = Spec(4, 6, 0, 1, True)
Spec.spec_i_infantry = Spec(5, 3, 0, 1, True)

# Update the specs list with the initialized objects
Spec.specs = [Spec.spec_f_fighter, Spec.spec_a_attacker, Spec.spec_t_panzer, Spec.spec_u_cannon, Spec.spec_r_antiAir, Spec.spec_i_infantry]