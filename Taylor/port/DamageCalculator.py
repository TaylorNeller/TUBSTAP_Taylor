from Action import Action
from Logger import Logger


class DamageCalculator:
    """
    Class that contains functions to calculate damage between units.
    Returns an array of attack damage and counter-attack damage.
    It has a variety of argument combinations to calculate damage.
    """

    @staticmethod
    def calculate_damages(map, action):
        """
        Calculate attack damage from Map and Action.
        """
        if action.action_type == Action.ACTIONTYPE_MOVEONLY:
            Logger.show_dialog_message("DamageCalculator: calculate_damages(1): Damage is being calculated for a non-attack action type.")
            return [0, 0]
        else:
            atk_unit = map.get_unit(action.operation_unit_id)
            target_unit = map.get_unit(action.target_unit_id)
            return DamageCalculator.calculate_damages_unit_map_pos(atk_unit, target_unit, map, action.destination_x_pos, action.destination_y_pos)

    @staticmethod
    def calculate_damages_unit_map(atk_unit, target_unit, map):
        """
        Calculate attack damage and counter-attack damage.
        Version with Unit and Map as arguments.
        Note: Damage is calculated based on the current position of the attacking unit (non-standard function).
        """
        return DamageCalculator.calculate_damages_unit_map_pos(atk_unit, target_unit, map, atk_unit.get_x_pos(), atk_unit.get_y_pos())

    @staticmethod
    def calculate_damages_unit_map_pos(atk_unit, target_unit, map, atk_x_pos, atk_y_pos):
        """
        Calculate attack damage and counter-attack damage.
        Version with Unit, Map, and attack position as arguments (standard).
        """
        atk_stars = map.get_field_defensive_effect(atk_x_pos, atk_y_pos)
        target_stars = map.get_field_defensive_effect(target_unit.get_x_pos(), target_unit.get_y_pos())
        return DamageCalculator.calculate_damages_unit_stars(atk_unit, target_unit, atk_stars, target_stars)

    @staticmethod
    def calculate_damages_unit_stars(atk_unit, target_unit, atk_stars, target_stars):
        """
        Calculate attack damage and counter-attack damage.
        Version with Unit and number of stars as arguments.
        """
        return DamageCalculator.calculate_damages_spec_hp_stars(atk_unit.get_spec(), atk_unit.get_HP(), target_unit.get_spec(), target_unit.get_HP(), atk_stars, target_stars)

    @staticmethod
    def calculate_damages_spec_hp_stars(atk_spec, atk_hp, target_spec, target_hp, atk_stars, target_stars):
        """
        Calculate attack damage and counter-attack damage.
        Version with Spec, HP, and number of stars as arguments.
        """
        damages = [0, 0]
        damages[0] = DamageCalculator.calculate_damage(atk_spec, atk_hp, target_spec, target_hp, target_stars)

        # Calculation of counter-attack damage. However:
        # - There is no counter-attack for ranged attacks.
        # - Ranged attack units cannot counter-attack.
        # - There is no counter-attack if the unit is destroyed.
        if target_hp - damages[0] > 0 and atk_spec.is_direct_attack_type() and target_spec.is_direct_attack_type():
            damages[1] = DamageCalculator.calculate_damage(target_spec, target_hp - damages[0], atk_spec, atk_hp, atk_stars)

        return damages

    @staticmethod
    def calculate_damage(atk_spec, atk_hp, target_spec, target_hp, target_stars):
        """
        Calculate only attack damage.
        """
        # If the target unit is an air unit, ignore the defensive effect of the terrain and set it to 0.
        if target_spec.is_air_unit():
            target_stars = 0

        # Compatibility
        atk_power = atk_spec.get_unit_atk_power(target_spec.get_unit_type())

        # Damage calculation formula
        raw_damage = ((atk_power * atk_hp) + 70) // (100 + (target_stars * target_hp))
        if raw_damage > target_hp:
            raw_damage = target_hp

        return raw_damage