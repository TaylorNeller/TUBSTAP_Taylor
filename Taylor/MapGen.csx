using System;
using System.IO;

// MapGen.generateMapFile("Taylor/generated_maps/genmap1.tbsmap", 8, 8);
for (int i = 0; i < 10; i++) {
    MapGen.generateMapFile($"bin/Release/autobattle/genmap{i}.tbsmap", 8, 8);
}

class MapGen {
    public static void generateMapFile(string filePath, int width, int height, bool padded = true) {
        Random random = new Random();
        int nRed = 6;  
        int nBlue = 6;
        int nIters = 50;

        // generate terrain
        int[][] terrain = new int[height][];
        for (int i = 0; i < height; i++) {
            terrain[i] = new int[width];
        }

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (padded && (x == 0 || y == 0 || x == width - 1 || y == height - 1)) {
                    terrain[y][x] = 0;
                }
                else {
                    // give 10% chance of mountain and 10% chance of sea
                    int indicator = random.Next(0, 10);
                    if (indicator == 0) {
                        terrain[y][x] = 4;
                    }
                    else if (indicator == 1) {
                        terrain[y][x] = 2;
                    }
                    else {
                        int n = 1+random.Next(0, 3)*2;
                        if (n == 6) {
                            n = 5;
                        }
                        terrain[y][x] = n;
                    }
                }
            }
        }

        // generate units
        // unit array structure: [[x,y,type,team,hp,isExhausted],...]
        string[] unitNames = new string[] {"attacker", "fighter", "antiair", "infantry", "panzer", "cannon"};
        int[][] allUnits = new int[nRed+nBlue][];
        int exhaustedRed = random.Next(1, nRed/2);
        Console.WriteLine("n "+exhaustedRed);
        for (int i = 0; i < nRed+nBlue; i++) {
            int x = random.Next(0, width);
            int y = random.Next(0, height);
            // give only 20% chance of attacker/fighter
            // DOES NOT GENERATE CANNON - NOT SUPPORTED
            int type = random.Next(0, 5) == 0 ? random.Next(0, 2) : random.Next(2, unitNames.Length-1); 
            int team = 0;
            if (i >= nRed) {
                team = 1;
            }
            int hp = 10;
            int isExhausted = 0;
            if (team == 0 && exhaustedRed > 0) {
                exhaustedRed--;
                isExhausted = 1;
                Console.WriteLine("exhausted "+exhaustedRed);
            }
            allUnits[i] = new int[] {x, y, type, team, hp, isExhausted};

            //check if unit is on top of another unit
            for (int j = 0; j < i; j++) {
                if (allUnits[i][0] == allUnits[j][0] && allUnits[i][1] == allUnits[j][1]) {
                    i--;
                    break;
                }
            }

            //check if unit is on top of impassable terrain
            // attacker/fighter are air, non-air cannot be on sea (2)
            // non-air/non-infantry cannot be no mountain (4)
            if (terrain[y][x] == 0 || 
                (terrain[y][x] == 2 && !(type == 0 || type == 1)) || 
                (terrain[y][x] == 4 && !(type == 0 || type == 1 || type == 3))) {
                i--;
            }
        }

        // approximate unit values: 
        // Dictionary<string, int> unitValues = new Dictionary<string, int>()
        // {
        //     { "attacker", 11 },
        //     { "fighter", 9 },
        //     { "antiair", 8 },
        //     { "infantry", 1 },
        //     { "panzer", 7 },
        //     { "cannon", 6 }
        // };
        int[] unitValues = new int[] {11, 9, 8, 1, 7, 6};
        
        // stochastically balance teams by modifying hp (target 80% lower team full unit value)
        double targetPercent = .8;
        double targetValue = 0;
        double[] teamValues = [0,0];

        for (int i = 0; i < nRed+nBlue; i++) {
            teamValues[allUnits[i][3]] += unitValues[allUnits[i][2]]*allUnits[i][4]*.1;
        }

        targetValue = targetPercent * teamValues[0];
        if (targetValue > targetPercent * teamValues[1]) {
            targetValue = targetPercent * teamValues[1];
        }

        // stochastically balance both teams until within 5% of target (or 100 iterations)
        for (int team = 0; team <= 1; team++) {
            int iter = 0;
            while (Math.Abs(teamValues[team] - targetValue) > .05*targetValue && iter < nIters) {
                int unit = random.Next(0, nRed);
                if (team == 1) {
                    unit = random.Next(0, nBlue)+nRed;
                }
                if (teamValues[team] > targetValue) {
                    if (allUnits[unit][4] > 1) {
                        allUnits[unit][4]--;
                        teamValues[team] -= unitValues[allUnits[unit][2]]*.1;
                    }
                }
                else {
                    if (allUnits[unit][4] < 10) {
                        allUnits[unit][4]++;
                        teamValues[team] += unitValues[allUnits[unit][2]]*.1;
                    }
                }
                iter++;
            }   
        }
        

        Console.WriteLine(teamValues[0]);
        Console.WriteLine(teamValues[1]);

        // print everything
        using (StreamWriter writer = new StreamWriter(filePath)) {
            //print header
            writer.Write($"SIZEX[{width}];SIZEY[{height}];\nTURNLIMIT[29];HPTHRESHOLD[10];\nUNITNUMRED[{nRed}];UNITNUMBLUE[{nBlue}];\nAUTHOR[Taylor];\n");

            // print terrain
            for (int y = 0; y < height; y++) {
                writer.Write("MAP[");
                for (int x = 0; x < width; x++) {
                    if (x != 0) {
                        writer.Write(",");
                    }
                    writer.Write(terrain[y][x]);
                }
                writer.Write("];");
                writer.WriteLine();
            }

            // print units
            for (int i = 0; i < nRed+nBlue; i++) {
                writer.Write("UNIT[");
                for (int j = 0; j < allUnits[i].Length; j++) {
                    if (j != 0) {
                        writer.Write(",");
                    }
                    if (j == 2) {
                        writer.Write(unitNames[allUnits[i][j]]);
                    }
                    else {
                        writer.Write(allUnits[i][j]);
                    }
                }
                writer.Write("];");
                writer.WriteLine();
            }
        }


    }
}