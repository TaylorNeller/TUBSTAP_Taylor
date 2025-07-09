class Consts:
    FIELD_BARRIER = 0
    # Number of terrain types
    FIELDTYPENUM = 7

    # Array to hold the defense effect of each terrain type
    # (prohibited area, grassland, sea, forest, mountain, road, castle) in order
    FIELD_DEFENSE = [0, 1, 0, 3, 4, 0, 4]

    # Constants representing teams
    RED_TEAM = 0
    BLUE_TEAM = 1

    # Array to hold the team names
    TEAM_NAMES = ["Red", "Blue"]

    # Rule set version
    RULE_SET_VERSION = "rule_set_version_0100"