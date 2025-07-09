class ConstsData:


    unitNames = ["fighter", "attacker", "panzer", "cannon", "antiair", "infantry"]
    unit_initials = ["F", "A", "P", "U", "R", "I"]
    FIGHTER = unitNames.index("fighter")
    ATTACKER = unitNames.index("attacker")
    PANZER = unitNames.index("panzer")
    CANNON = unitNames.index("cannon")
    ANTIAIR = unitNames.index("antiair")
    INFANTRY = unitNames.index("infantry")
    
    dmg_matrix = [  [55, 65, 0, 0, 0, 0],
                    [0, 0, 105, 105, 85, 115],
                    [0, 0, 55, 70, 75, 75],
                    [0, 0, 60, 75, 65, 90],
                    [70, 70, 15, 50, 45, 105],
                    [0, 0, 5, 10, 3, 55]]

    # 0 = barrier, 1 = plain, 2 = sea, 3 = forest, 4 = mountain, 5 = road
    # move_matrix = [ [10, 1, 1, 1, 1, 1],
    #                 [10, 1, 1, 1, 1, 1],
    #                 [10, 10, 1, 2, 1, 10],
    #                 [10, 1, 1, 1, 2, 10],
    #                 [10, 10, 1, 2, 1, 10],
    #                 [10, 10, 1, 2, 1, 10]]
    # Movement cost for each unit, lower values mean easier to move NOENTRY, Plains, Sea, Forest, Mountain, Road order
    move_cost = [
        [99, 1, 1, 1, 1, 1],    # F
        [99, 1, 1, 1, 1, 1],    # A
        [99, 1, 99, 2, 99, 1],  # P
        [99, 1, 99, 2, 99, 1],  # U
        [99, 1, 99, 2, 99, 1],  # R
        [99, 1, 99, 1, 2, 1]    # I
    ]
    def_matrix = [ [0, 0, 0, 0, 0, 0],
                    [0, 0 ,0, 0, 0, 0],
                    [0, .1, 0, .3, .4, 0],
                    [0, .1, 0, .3, .4, 0],
                    [0, .1, 0, .3, .4, 0],
                    [0, .1, 0, .3, .4, 0],]

    movement_arr = [9,7,6,5,6,3]


class UnitData:

    def __init__(self, x, y, type, team, hp, moved):
        self.x = x
        self.y = y
        self.type = type
        self.team = team
        self.hp = hp
        self.moved = moved
    
    def __str__(self):
        return "Unit: " + ConstsData.unitNames[self.type] + " at (" + str(self.x) + ", " + str(self.y) + ") with " + str(self.hp) + " hp. Team " + str(self.team) + ". Moved: " + str(self.moved) + "."
    
    def get_name(self):
        return ConstsData.unitNames[self.type]
    
    def get_movement_capacity(self):
        return ConstsData.movement_arr[self.type]
    
    def move_cost(self, terrain):
        return ConstsData.move_cost[self.type][terrain]

    def calc_dmg(self, defender, terrain, atk_terrain=None):
        # taken from calculation in DamageCalculator.cs:
        #      int rawDamage = ((atkPower * atkHP) + 70) / (100 + (targetStars * targetHP));
        dmg = int((ConstsData.dmg_matrix[self.type][defender.type] * self.hp + 70) / (100+10*ConstsData.def_matrix[defender.type][terrain] * defender.hp))
        if dmg > defender.hp:
            dmg = defender.hp
        if atk_terrain is not None:
            # calculate counterattack
            counter_dmg = -1
            if dmg == defender.hp:
                counter_dmg = int((ConstsData.dmg_matrix[defender.type][self.type] * defender.hp - dmg + 70) / (100+10*ConstsData.def_matrix[self.type][atk_terrain] * self.hp))
                if counter_dmg > self.hp:
                    counter_dmg = self.hp
            return dmg, counter_dmg
        return dmg
