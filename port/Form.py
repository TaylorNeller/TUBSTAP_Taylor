# contains one function get_player(n) which returns the player int mapped to n, held in the variable players 
class Form:

    # constructor inits players to [0,0]
    def __init__(self, player1=0, player2=0):
        self.players = [player1, player2]

    # get_player returns the player int mapped to n
    def get_player(self, n):
        return self.players[n]