class Player:
    """
    If you want to create a new AI, you need to implement this interface.
    """

    def make_action(self, map, team_color, is_the_beginning_of_turn, is_the_first_turn_of_game):
        """
        Function to create the AI's action.
        :param map: Current MAP situation
        :param team_color: Team to operate
        :param is_the_beginning_of_turn: Whether it's the beginning of the turn
        :param is_the_first_turn_of_game: Whether it's the first turn of the game
        :return: AI's action (for 1 unit)
        """
        pass

    def get_name(self):
        """
        Function to return the name in one line.
        :return: Name of the AI
        """
        pass

    def show_parameters(self):
        """
        Function to return parameter information, which can include line breaks.
        :return: Parameter information
        """
        pass