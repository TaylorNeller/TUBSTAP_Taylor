import random

from Action import Action
from AiTools import AiTools
from DamageCalculator import DamageCalculator
from Logger import Logger
from Map import Map
from Player import Player
from RangeController import RangeController
from Unit import Unit

class AI_Sample_MaxActEval(Player):
    # Index for storing the maximum threat of enemy units in Unit object's arbitrary use int[]
    XINDEX_MAXTHREAT = 0

    # Parameters
    DETAILED_LOG = False                  # Whether to display detailed logs
    CALC_THREAT_ONLYTO_MOVEDUNITS = True  # Whether to calculate enemy unit's threat only for moved friendly units
    MINIMUM_EFFICIENCY_FOR_ATTACK = -20   # If the evaluation value of an attack is below this, do not attack (can be negative due to receiving counterattacks, etc.)
    BASE_VALUE_OF_MYUNIT = 10             # This + HP is the value of own units
    BASE_VALUE_OF_ENEMY = 10              # This + threat is the value of enemy units

    def __init__(self):
        super().__init__()
        self.rand = random.Random()
        self.evaluate_map = None

    def get_name(self):
        """Returns the display name of the AI (required public function)"""
        return "Sample_MaxActionEvalFunc"

    def show_parameters(self):
        """Returns information about parameters, etc."""
        return f"DETAILED_LOG = {self.DETAILED_LOG}\r\n" + \
               f"CALC_THREAT_ONLYTO_MOVEDUNITS = {self.CALC_THREAT_ONLYTO_MOVEDUNITS}\r\n" + \
               f"MINIMUM_EFFICIENCY_FOR_ATTACK = {self.MINIMUM_EFFICIENCY_FOR_ATTACK}\r\n" + \
               f"BASE_VALUE myunits/enemy = {self.BASE_VALUE_OF_MYUNIT} / {self.BASE_VALUE_OF_ENEMY}"

    def make_action(self, map: Map, team_color: int, turn_start: bool, game_start: bool) -> Action:
        """Determines the action of 1 unit (required public function)"""
        if turn_start:
            self.evaluate_map = [[""]*map.get_y_size() for _ in range(map.get_x_size())]
            Logger.add_log_message(map.to_string(), team_color)

        enemies = map.get_units_list(team_color, False, False, True)  # List of all enemies

        # Procedure 1: Calculate and store the maximum attack power from each enemy unit to own units (only moved units)
        Logger.add_log_message("\r\nThinking start:\r\n Step 1: Calculating threats of enemies\r\n", team_color)
        for enemy_unit in enemies:
            damage = self.estimate_max_atk_damage(enemy_unit, map)
            Logger.add_log_message(f"   max damage from {enemy_unit.to_short_string()} = {damage}\r\n", team_color)

            # Initialize the arbitrary use int of Unit object and store the threat
            enemy_unit.init_x_ints(1)
            enemy_unit.set_x_int(self.XINDEX_MAXTHREAT, damage)

        # Procedure 2: If there are own units that can attack, select the action with the maximum evaluation value
        Logger.add_log_message(" Step 2: Attack\r\n", team_color)
        atk_actions = AiTools.get_all_attack_actions(team_color, map)  # List of all attack actions

        max_atk_act_value = self.MINIMUM_EFFICIENCY_FOR_ATTACK  # Do not attack if below this
        max_action = None

        for act in atk_actions:
            # Calculate the evaluation value of the attack action
            act_value = self.evaluate_attack_action_value(act, map, team_color)

            if act_value > max_atk_act_value:
                max_atk_act_value = act_value  # Replace with the maximum evaluation value
                max_action = act

        if max_action is not None:
            Logger.add_log_message(f" Selected = {max_action.to_one_line_string()} Value={max_atk_act_value}\r\n", team_color)

            # Update the evaluation value map
            # Since evaluation values only exist for attack actions, do not display evaluations for move actions
            # Display the evaluation value at the time of attack on the attacking unit
            self.evaluate_map[max_action.destination_x_pos][max_action.destination_y_pos] = str(max_atk_act_value)
            # Display process
            # DrawManager.draw_string_on_map(self.evaluate_map)

            return max_action

        # Procedure 3: If there are no good attack actions, randomly select a unit and move it
        # Either approach the enemy or stay in advantageous terrain. Does not consider moves to protect allies
        Logger.add_log_message("   No effective attack.\r\n Step 3: Move.\r\n", team_color)

        my_units = map.get_units_list(team_color, False, True, False)  # Own units that have not acted
        op_unit = my_units[self.rand.randint(0, len(my_units)-1)]  # Randomly select one

        Logger.add_log_message(f"   Unit {op_unit.to_short_string()} is randomly selected to move.\r\n", team_color)

        # The movable range of that unit
        movable = RangeController.get_reachable_cells_matrix(op_unit, map)

        # Select the best among them considering terrain, proximity to advantageous units, etc.
        max_score = float("-inf")
        max_x = -1
        max_y = -1

        matrix_log = "  "

        for x in range(1, map.get_x_size() - 1):
            for y in range(1, map.get_y_size() - 1):
                if not movable[x][y]:
                    matrix_log += " ---"
                    continue  # Skip places that cannot be moved to

                defense = map.get_field_defensive_effect(x, y)  # Defense power
                if op_unit.get_spec().is_air_unit():
                    defense = 0

                local_max_score = float("-inf")  # Best at that cell (for logging)
                for enemy_unit in enemies:
                    dist = abs(x - enemy_unit.get_x_pos()) + abs(y - enemy_unit.get_y_pos())  # Manhattan distance
                    effect = op_unit.get_spec().get_unit_atk_power(enemy_unit.get_spec().get_unit_type())  # Attack power from opUnit to enemyU

                    # If there is a good opponent, approach. Otherwise, prioritize defense using a function
                    score = defense * 5 + effect // (dist + 5)

                    if self.DETAILED_LOG:
                        Logger.add_log_message(f"  -> ({x},{y}) vs {enemy_unit.to_short_string()} score={score}\r\n", team_color)

                    if score > max_score:
                        max_score = score
                        max_x = x
                        max_y = y
                    if score > local_max_score:
                        local_max_score = score
                matrix_log += f"{local_max_score:4}"
            matrix_log += "\r\n  "

        Logger.add_log_message(matrix_log, team_color)
        Logger.add_log_message(f"   Selected = {op_unit.to_short_string()} -> ({max_x},{max_y})\r\n", team_color)
        return Action.create_move_only_action(op_unit, max_x, max_y)  # Create and return an action that only moves

    @staticmethod
    def evaluate_attack_action_value(act: Action, map: Map, team_color: int) -> int:
        """
        Action evaluation function. Receives an attack action and returns its desirability.
        The desirability is defined as: damage dealt × enemy's value - received counterattack × own value
        """
        my_unit = map.get_unit(act.operation_unit_id)  # Attacking unit
        en_unit = map.get_unit(act.target_unit_id)  # Defending unit
        if my_unit is None or en_unit is None:
            raise ValueError("ActionEvaluator_Sample: evaluate_attack_action_value: ERROR_unit_is_null")

        attack_damages = DamageCalculator.calculate_damages(map, act)  # Calculate attack and counterattack damage

        # If damage is 0, do not attack
        if attack_damages[0] == 0:
            return float("-inf")

        # Own unit value = 10 + unit's remaining HP
        my_value = AI_Sample_MaxActEval.BASE_VALUE_OF_MYUNIT + my_unit.get_HP()

        # Enemy unit value = 10 + threat
        en_value = AI_Sample_MaxActEval.BASE_VALUE_OF_ENEMY + en_unit.get_x_int(AI_Sample_MaxActEval.XINDEX_MAXTHREAT)

        # Desirability of the action (can be negative)
        act_value = (attack_damages[0] * en_value) - (attack_damages[1] * my_value)

        if AI_Sample_MaxActEval.DETAILED_LOG:
            Logger.add_log_message(f"   total={act_value}, dm={attack_damages[0]} enval={en_value} rvdm={attack_damages[1]} myval={my_value} act={act.to_one_line_string()}\r\n", team_color)
        else:
            Logger.add_log_message(f"   score={act_value} {my_unit.to_short_string()}  -> {en_unit.to_short_string()}\r\n", team_color)

        return act_value

    def estimate_max_atk_damage(self, atk_unit: Unit, map: Map) -> int:
        """
        Calculate the threat of atkUnit. The maximum damage that can be dealt to moved units.
        Could include unmoved units or incorporate unit values (e.g., infantry is low).
        """
        max_dm = 0
        attack_actions = RangeController.get_attack_action_list(atk_unit, map)  # All attack actions of atkUnit

        for act in attack_actions:
            my_unit = map.get_unit(act.target_unit_id)
            if self.CALC_THREAT_ONLYTO_MOVEDUNITS and not my_unit.is_action_finished():
                continue  # Do not target unmoved units
            dm = DamageCalculator.calculate_damages_unit_map(atk_unit, my_unit, map)  # Calculate damage (ignore counterattack)
            if dm[0] > max_dm:
                max_dm = dm[0]

        return max_dm