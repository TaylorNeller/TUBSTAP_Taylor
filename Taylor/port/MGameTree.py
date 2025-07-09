from Action import Action
from Map import Map

class MGameTree:
    def __init__(self):
        # self.board = Map() 

        self.depth = 0
        self.simnum = 0
        self.last_score = 0.0
        self.total_score = 0.0
        self.housyuu = 0.0  # Presumably some kind of reward or score
        self.act = Action()  # Assumed to be an object similar to the C# 'Action'
        self.next = []  # List of MGameTree objects (child nodes)