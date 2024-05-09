import Consts
from Player import Player
from HumanPlayer import HumanPlayer
from NetworkPlayer import NetworkPlayer
from AI_Sample_MaxActEval import AI_Sample_MaxActEval
from AI_MSSystem import AI_MSSystem
from AI_M_UCT import AI_M_UCT
from AI_M3Lee_hull import AI_M3Lee_hull
from AI_SatD2 import AI_SatD2

class PlayerList:
    """
    Class for registering player types.
    Please edit this when you add a new thinking routine.
    """

    # List of player objects. Please register the necessary classes here.
    registered_player_list = [
        HumanPlayer(),  # Don't change this
        NetworkPlayer(),  # Don't change this
        AI_Sample_MaxActEval(),
        AI_MSSystem(),
        AI_M_UCT(),
        AI_M3Lee_hull(),
        AI_SatD2()
    ]

    # Register the indices of the 2 players you want to compete by default here.
    default_player_index = [0, 2]

    # Constants for player types
    HUMAN_PLAYER = 0
    NETWORK_PLAYER = 1

    @staticmethod
    def get_player(player_index):
        """
        Return the player object specified by the index.
        :param player_index: Index of the player
        :return: Player object
        """
        return PlayerList.registered_player_list[player_index]

    @staticmethod
    def initialize_player_list_combo_box(cb_red, cb_blue):
        """
        Initialize the player list combo box.
        :param cb_red: Combo box for the red team
        :param cb_blue: Combo box for the blue team
        """
        cb_red.clear()
        cb_blue.clear()
        for player in PlayerList.registered_player_list:
            cb_red.addItem(player.get_name())
            cb_blue.addItem(player.get_name())
        cb_red.setCurrentIndex(PlayerList.default_player_index[Consts.RED_TEAM])
        cb_blue.setCurrentIndex(PlayerList.default_player_index[Consts.BLUE_TEAM])