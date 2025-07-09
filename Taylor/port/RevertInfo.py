class RevertInfo:
    """
    Holds all information needed to undo a single action.
    """
    def __init__(self):
        # You will store any info you need to undo the action here
        self.old_positions = []   # For move actions
        self.old_action_finished = []  # For flipping action-finished flags
        self.dead_units = []      # For any unit that died (so we can revive it)
        self.damage_done = []     # HP lost, so we can restore it
        self.turn_increased = False  # If we ended the turn, we might have to decrement turn_count, etc.
