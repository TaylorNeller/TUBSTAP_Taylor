import random

from Action import Action
from Map import Map
from Player import Player
from Consts import Consts

class HumanPlayer(Player):

    def __init__(self):
        super().__init__()
        self.rand = random.Random()
        self.evaluate_map = None

    def get_name(self):
        return "Human"

    def show_parameters(self):
        return 

    def make_action(self, map_: Map, team_color: int, turn_start: bool, game_start: bool) -> Action:
        """Determines the action of 1 unit (required public function)"""
        
        print('You are team: ', 'Red (0)' if team_color == Consts.RED_TEAM else 'Blue (1)')

        # print board
        print(map_.map_to_string())

        print('Enter the six space-separated integers that denote your action: ')
        # get input (integer array)
        action = list(map(int, input().split()))
        print(action)
        if len(action) == 0:
            return Action.create_turn_end_action()  
        elif len(action) == 1:
            if action[0] == 0:
                print(map_.get_raw_map())
            elif action[0] == 1:
                possible_actions = self.get_valid_actions_for_state(map_, team_color)
                for act in possible_actions:
                    print(act.from_x_pos, act.from_y_pos, act.destination_x_pos, act.destination_y_pos, act.attack_x_pos, act.attack_y_pos) 
        elif len(action) == 2:
            action = [action[0], action[1], action[0], action[1], -1, -1]

        if len(action) == 6:
            possible_actions = self.get_valid_actions_for_state(map_, team_color)
            for act in possible_actions:
                if act.from_x_pos == action[0] \
                    and act.from_y_pos == action[1] \
                    and act.destination_x_pos == action[2] \
                    and act.destination_y_pos == action[3] \
                    and act.attack_x_pos == action[4] \
                    and act.attack_y_pos == action[5]:
                    return act
            print('Invalid action')
        
        return self.make_action(map_, team_color, turn_start, game_start)
