class Consts:
    unitNames = ["attacker", "fighter", "antiair", "infantry", "panzer", "cannon"]

    dmg_matrix = [  [0, 0, 85, 115, 105, 105],
                    [65, 55, 0, 0, 0, 0],
                    [70, 70, 45, 105, 15, 50],
                    [0, 0, 3, 55, 5, 10],
                    [0, 0, 75, 75, 55, 70],
                    [0, 0, 65, 90, 60, 75]]

    # 0 = barrier, 1 = plain, 2 = sea, 3 = forest, 4 = mountain, 5 = road
    move_matrix = [ [10, 1, 1, 1, 1, 1],
                    [10, 1, 1, 1, 1, 1],
                    [10, 10, 1, 2, 1, 10],
                    [10, 1, 1, 1, 2, 10],
                    [10, 10, 1, 2, 1, 10],
                    [10, 10, 1, 2, 1, 10]]

    def_matrix = [ [0, 0, 0, 0, 0, 0],
                    [0, 0 ,0, 0, 0, 0],
                    [0, .1, 0, .3, .4, 0],
                    [0, .1, 0, .3, .4, 0],
                    [0, .1, 0, .3, .4, 0],
                    [0, .1, 0, .3, .4, 0],]

    movement_arr = [7,9,6,3,6,5]

class Unit:

    def __init__(self, x, y, type, team, hp, moved):
        self.x = x
        self.y = y
        self.type = type
        self.team = team
        self.hp = hp
        self.moved = moved
    
    def __str__(self):
        return "Unit: " + Consts.unitNames[self.type] + " at (" + str(self.x) + ", " + str(self.y) + ") with " + str(self.hp) + " hp. Team " + str(self.team) + ". Moved: " + str(self.moved) + "."
    
    def get_name(self):
        return Consts.unitNames[self.type]
    
    def get_movement_capacity(self):
        return Consts.movement_arr[self.type]
    
    def move_cost(self, terrain):
        return Consts.move_matrix[self.type][terrain]

    def calc_dmg(self, defender, terrain):
        # taken from calculation in DamageCalculator.cs:
        #      int rawDamage = ((atkPower * atkHP) + 70) / (100 + (targetStars * targetHP));
        dmg = int((Consts.dmg_matrix[self.type][defender.type] * self.hp + 70) / (100+10*Consts.def_matrix[defender.type][terrain] * defender.hp))
        if dmg > defender.hp:
            dmg = defender.hp
        return dmg

