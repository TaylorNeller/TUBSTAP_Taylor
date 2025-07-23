using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// Assume these will be provided or implemented later
// (In Python, we had references to `ConstsData`, `port.GameCLI`, `MapUtils`, etc.)
static class ConstsData
{
    // Example placeholder: index into unitNames to match the original Python code
    public static List<string> unitNames = new List<string>() {
        "attacker", "fighter", "antiair", "infantry", "panzer", "cannon"
    };
}
static class MapUtils
{
    // Placeholder if needed for local search or debugging map states.
    public static string map_to_string(int[][] mapMatrix, List<int[]> units)
    {
        return "Map + Units debug string";
    }
}

public static class MapGen
{
    // Mirror the Python static fields with default values
    public static string terrain_type = "random";    // "plains" or "random"
    public static string unit_dist     = "unit-list";// "no-cannon", "ground-melee", "inf-tank", "unit-list"
    // public static List<string> unit_list = new List<string> { "infantry", "antiair", "panzer", "cannon", "infantry", "antiair", "panzer", "cannon" };
    public static List<string> unit_list = new List<string> { "infantry", "infantry", "panzer", "panzer", "cannon", "antiair", "fighter", "attacker", "infantry", "infantry", "panzer", "panzer", "cannon", "antiair", "fighter", "attacker" };
    public static int n_red = 8;
    public static int n_blue = 8;
    public static int seed   = -1;
    public static int map_x  = 12;
    public static int map_y  = 12;
    public static bool map_padded = true;
    public static bool CORNER_BIAS = true;          // Bias unit placement to corners
    public static int n_iters = 50;
    public static int turn_limit = 30;        // Also determines starting player in some usage
    public static bool local_search_flag = false;
    public static bool starting_exhaust = false;
    public static int player1 = 2;           // for local search usage
    public static int player2 = 2;           // for local search usage
    public static double fta_counter = 1.1;  // First turn advantage counter (1.1 - 4u)

    /// <summary>
    /// Generates a map file (or returns its string) that mirrors the logic from the original Python code.
    /// </summary>
    /// <param name="filePath">
    /// If non-null, writes to the file. Otherwise returns the map string.
    /// </param>
    public static string GenerateMapFile(string filePath = null)
    {
        int width  = map_x;
        int height = map_y;

        // If user set seed != -1, fix the seed
        Random rnd = (seed != -1) ? new Random(seed) : new Random();

        // 1) Generate terrain
        int[][] terrain = new int[height][];
        for (int i = 0; i < height; i++)
            terrain[i] = new int[width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (map_padded && (x == 0 || y == 0 || x == width - 1 || y == height - 1))
                {
                    // Impassable boundary
                    terrain[y][x] = 0;
                }
                else
                {
                    if (terrain_type == "random")
                    {
                        // random terrain
                        int indicator = rnd.Next(0, 20);
                        if      (indicator == 0) terrain[y][x] = 4; // mountain
                        else if (indicator == 1) terrain[y][x] = 2; // sea
                        else
                        {
                            // plains/forest/road distribution
                            int n = 1 + (rnd.Next(0, 3) * 2); 
                            if (n == 6) n = 5; // convert index 6 to 5 if needed
                            terrain[y][x] = n; 
                        }
                    }
                    else
                    {
                        // "plains" terrain
                        terrain[y][x] = 1;
                    }
                }
            }
        }

        // 2) Generate units
        // AtFAIPC in Python code => attacker, fighter, ...
        // We'll just store them in a list, each: [x, y, typeIndex, team, hp, isExhausted].
        List<int[]> allUnits = new List<int[]>();

        int exhaustedRed = 0;
        if (starting_exhaust && n_red > 0) {
            exhaustedRed = rnd.Next(1, Math.Max(1, n_red / 2));
        }

        // Some sets from the Python code:
        List<int> ground_melee = new List<int> { 
            ConstsData.unitNames.IndexOf("infantry"), 
            ConstsData.unitNames.IndexOf("panzer"), 
            ConstsData.unitNames.IndexOf("antiair") 
        };
        List<int> air = new List<int> { 
            ConstsData.unitNames.IndexOf("fighter"), 
            ConstsData.unitNames.IndexOf("attacker") 
        };

        // We want n_red + n_blue total units
        for (int i = 0; i < (n_red + n_blue); i++)
        {
            while (true)
            {
                int team = (i < n_red) ? 0 : 1;

                int x = 0;
                int y = 0;
                if (CORNER_BIAS)
                {
                    x = (int)(rnd.NextDouble() * rnd.NextDouble() * width);
                    y = (int)(rnd.NextDouble() * rnd.NextDouble() * height);
                    if (team == 1)
                    {
                        x = width - 1 - x;
                        y = height - 1 - y;
                    }
                }
                else
                {
                    x = rnd.Next(0, width);
                    y = rnd.Next(0, height);
                }

                int hp   = 10;
                int isExhausted = 0;
                if (team == 0 && exhaustedRed > 0)
                {
                    exhaustedRed--;
                    isExhausted = 1;
                }

                // pick the unit type
                int typeIndex;
                if (unit_dist == "no-cannon")
                {
                    // 20% chance air, else ground
                    if (rnd.Next(0, 5) == 0) 
                        typeIndex = air[rnd.Next(0, air.Count)];
                    else
                        typeIndex = ground_melee[rnd.Next(0, ground_melee.Count)];
                }
                else if (unit_dist == "ground-melee")
                {
                    typeIndex = ground_melee[rnd.Next(0, ground_melee.Count)];
                }
                else if (unit_dist == "inf-tank")
                {
                    // 1/3 chance panzer, else infantry
                    if (rnd.Next(0, 3) == 0) typeIndex = ConstsData.unitNames.IndexOf("panzer");
                    else                     typeIndex = ConstsData.unitNames.IndexOf("infantry");
                }
                else if (unit_dist == "unit-list")
                {
                    // pick from user-supplied list
                    string unitName = unit_list[i];
                    typeIndex = ConstsData.unitNames.IndexOf(unitName);
                    // if the Python code sets HP=5 for panzer
                    if (unitName == "panzer")
                    {
                        hp = 5;
                    }
                }
                else
                {
                    // completely random among all known unitNames
                    typeIndex = rnd.Next(0, ConstsData.unitNames.Count);
                }

                int[] newUnit = new int[] { x, y, typeIndex, team, hp, isExhausted };

                // 2a) Check if same location as an existing unit
                bool overlap = allUnits.Any(u => (u[0] == x && u[1] == y));
                if (overlap) continue;

                // 2b) Check if terrain is impassable
                // terrain[y][x] == 0 => impassable
                // terrain[y][x] == 2 => sea, only air allowed
                // terrain[y][x] == 4 => mountain, only air or "cannon" (index 5) allowed
                //   But the Python code specifically let cannon = index 5 stand on mountain as well.
                //   So: (typeIndex in air) or typeIndex == 5 => okay on mountain
                int terrainCell = terrain[y][x];
                // "air" means typeIndex is in the `air` list
                bool isAir = air.Contains(typeIndex);
                bool isCannon = (typeIndex == ConstsData.unitNames.IndexOf("cannon"));

                if (terrainCell == 0)
                {
                    // no go
                    continue;
                }
                else if (terrainCell == 2 && !isAir)
                {
                    // sea => only air
                    continue;
                }
                else if (terrainCell == 4 && !(isAir || isCannon))
                {
                    // mountain => only air or cannon
                    continue;
                }

                // If we reach here, we accept the new unit
                allUnits.Add(newUnit);
                break;
            }
        }

        // 3) Balance unit values, if local_search_flag == false
        if (!local_search_flag)
        {
            // approximate unit values from the Python snippet
            // attacker=9, fighter=11, antiair=6, panzer=7, artillery=8, infantry=1
            // The python code had them in order: [9, 11, 6, 7, 8, 1]
            // But that was apparently: [attacker, fighter, antiair, infantry, panzer, cannon].
            // We'll keep the same array:
            double[] unitValues = new double[] { 
                9.0,   // attacker
                11.0,  // fighter
                6.0,   // antiair
                7.0,   // infantry
                8.0,   // panzer
                1.0    // cannon
            };

            // Sum up team values
            double[] teamValues = new double[]{ 0.0, 0.0 };
            foreach (int[] unit in allUnits)
            {
                int tIndex = unit[2];   // type index
                int tTeam  = unit[3];   // 0 or 1
                int tHP    = unit[4];
                double val = unitValues[tIndex] * tHP * 0.1;
                teamValues[tTeam] += val;
            }

            // target_value = 0.8 * min( teamValues[0], teamValues[1] )
            double target_percent = 0.8;
            double targetVal = Math.Min(
                target_percent * teamValues[0], 
                target_percent * teamValues[1]
            );

            // scale second team’s target by the first-turn-advantage factor
            double[] teamTargets = new double[2];
            teamTargets[0] = targetVal;
            teamTargets[1] = targetVal * fta_counter; 

            // Adjust HP on each side to get close to the target
            for (int t = 0; t < 2; t++)
            {
                int iterCount = 0;
                while (Math.Abs(teamValues[t] - teamTargets[t]) > 0.05 * teamTargets[t]
                       && iterCount < n_iters)
                {
                    // pick a random unit from that team
                    int unitIndex = (t == 0)
                        ? rnd.Next(0, n_red)
                        : rnd.Next(0, n_blue) + n_red;

                    int[] thisUnit = allUnits[unitIndex];
                    // if team value is too high, reduce HP. If too low, increase HP.
                    if (teamValues[t] > teamTargets[t])
                    {
                        if (thisUnit[4] > 1)  // hp
                        {
                            thisUnit[4] -= 1;
                            teamValues[t] -= (unitValues[thisUnit[2]] * 0.1);
                        }
                    }
                    else
                    {
                        if (thisUnit[4] < 10)
                        {
                            thisUnit[4] += 1;
                            teamValues[t] += (unitValues[thisUnit[2]] * 0.1);
                        }
                    }
                    iterCount++;
                }
            }
        }

        // 4) Convert each unit’s typeIndex to a string
        foreach (var unit in allUnits)
        {
            // replace integer with actual string name in final output
            // We'll do that at final string formatting time.
        }

        // 5) Build the .tbsmap final string
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append($"SIZEX[{width}];SIZEY[{height}];\n");
        sb.Append($"TURNLIMIT[{turn_limit}];HPTHRESHOLD[10];\n");
        sb.Append($"UNITNUMRED[{n_red}];UNITNUMBLUE[{n_blue}];\n");
        sb.Append("AUTHOR[Taylor];\n");

        // 5a) The terrain lines
        for (int y = 0; y < height; y++)
        {
            sb.Append("MAP[");
            for (int x = 0; x < width; x++)
            {
                if (x > 0) sb.Append(",");
                sb.Append(terrain[y][x]);
            }
            sb.Append("];\n");
        }

        // 5b) The UNIT lines
        foreach (var unit in allUnits)
        {
            // unit = [x, y, typeIndex, team, hp, isExhausted]
            int x = unit[0];
            int y = unit[1];
            int typeIndex = unit[2];
            int team = unit[3];
            int hp   = unit[4];
            int ex   = unit[5]; // isExhausted

            string typeStr = ConstsData.unitNames[typeIndex];
            sb.Append($"UNIT[{x},{y},{typeStr},{team},{hp},{ex}];\n");
        }

        string mapStr = sb.ToString();

        // 6) localSearch if flagged
        if (local_search_flag)
        {
            DateTime startTime = DateTime.Now;
            // mapStr = LocalSearch(mapStr, null);
            double secs = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Local search took {secs} seconds");
        }

        // If filePath is provided, write to file; else just return the string
        if (!string.IsNullOrEmpty(filePath))
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(mapStr);
            }
            return null; // or return filePath
        }
        else
        {
            return mapStr;
        }
    }

    /// <summary>
    /// Generate multiple maps as a single string, concatenated by "MAPEND;\n" as in the Python code.
    /// </summary>
    public static string GenerateNMapsStr(int n)
    {
        // This duplicates the idea of the python function `generate_n_maps_str`.
        // You can adapt as needed or remove if not used.
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            if (i > 0) sb.Append("MAPEND;\n");
            // Temporarily override map_x,map_y if needed or just use them
            sb.Append(GenerateMapFile());
        }
        return sb.ToString();
    }

    // /// <summary>
    // /// Local search logic referencing GameCLI.run_autobattle
    // /// </summary>
    // private static string LocalSearch(string mapStr, int? prev_winner)
    // {
    //     // In Python: states, result = GameCLI.run_autobattle(...)
    //     var (states, result) = GameCLI.run_autobattle(user1: player1,
    //                                                  user2: player2,
    //                                                  map: "random_map",
    //                                                  games: 2,
    //                                                  raw_map_str: mapStr);

    //     // If result==0 => no winner (draw), just return
    //     if (result == 0) return mapStr;

    //     // If we haven't got a prev_winner or if it's the same winner, we try to degrade a random winner-unit
    //     if (!prev_winner.HasValue || result == prev_winner.Value)
    //     {
    //         // Extract all UNIT lines from mapStr
    //         var lines = mapStr.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
    //         List<int[]> units = new List<int[]>();
    //         // We'll parse them as [x, y, typeIndexString, team, hp, isExhausted]
    //         // but store them as int[] with typeIndexString in a separate string for reassembling
    //         List<string> typeStrings = new List<string>();

    //         foreach (var line in lines)
    //         {
    //             if (line.StartsWith("UNIT["))
    //             {
    //                 // e.g. UNIT[x,y,typeName,team,hp,isExhausted];
    //                 // remove "UNIT[" and "];"
    //                 string content = line.Substring(5, line.Length - 7);
    //                 // x,y,type,team,hp,ex
    //                 var parts = content.Split(',');
    //                 if (parts.Length == 6)
    //                 {
    //                     int px = Int32.Parse(parts[0]);
    //                     int py = Int32.Parse(parts[1]);
    //                     string pTypeStr = parts[2];
    //                     int pTeam = Int32.Parse(parts[3]);
    //                     int pHP   = Int32.Parse(parts[4]);
    //                     int pEx   = Int32.Parse(parts[5]);
    //                     units.Add(new int[] { px, py, pTeam, pHP, pEx });
    //                     typeStrings.Add(pTypeStr);
    //                 }
    //             }
    //         }

    //         // find all “winner units” = those with the same team as result, hp>1
    //         // result is 0 or 1, so winner = result
    //         var winnerUnitsIndices = new List<int>();
    //         for (int i = 0; i < units.Count; i++)
    //         {
    //             var u = units[i];
    //             if (u[2] == result && u[3] > 1)
    //             {
    //                 winnerUnitsIndices.Add(i);
    //             }
    //         }

    //         if (winnerUnitsIndices.Count > 0)
    //         {
    //             int idx = winnerUnitsIndices[new Random().Next(0, winnerUnitsIndices.Count)];
    //             // degrade that unit by 1 hp
    //             units[idx][3] = units[idx][3] - 1; // hp--

    //             // Rebuild mapStr with updated hp
    //             // We only modify the selected unit line
    //             // More simply, we can rebuild the entire string
    //             // but let's just do a naive approach:

    //             System.Text.StringBuilder newSb = new System.Text.StringBuilder();
    //             foreach (var line in lines)
    //             {
    //                 if (!line.StartsWith("UNIT["))
    //                 {
    //                     newSb.AppendLine(line);
    //                 }
    //             }
    //             // Now re-append all units with updated data
    //             for (int i = 0; i < units.Count; i++)
    //             {
    //                 var u = units[i];
    //                 // x,y,typeStr,team,hp,isEx
    //                 newSb.Append($"UNIT[{u[0]},{u[1]},{typeStrings[i]},{u[2]},{u[3]},{u[4]}];\n");
    //             }
    //             mapStr = newSb.ToString();
    //         }

    //         // Recursively call local_search
    //         return LocalSearch(mapStr, result);
    //     }
    //     else
    //     {
    //         // The winner changed => we stop
    //         return mapStr;
    //     }
    // }
}


for (int i = 0; i < 100; i++)
{
    // The static fields in MapGen can be adjusted before each call if needed
    // For example:
    // MapGen.map_x = 8; 
    // MapGen.map_y = 8;
    // MapGen.terrain_type = "plains";
    // etc.

    string filePath = $"c:/Users/Taylor/OneDrive/Documents/thesis/TUBSTAP_src_ver0108/bin/Release/autobattle/genmap{i}.tbsmap";
    MapGen.GenerateMapFile(filePath);
    // Console.WriteLine($"Generated {filePath}");
}
