using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    // 定数はここに
    static class Consts {
 
        // 地形の種類数
        public const int FIELDTYPENUM = 7;

        // 地形の防御効果を保持する配列（禁止区域，草原，海，森，山, 道路, 城）の順，
        public static readonly int[] sFieldDefense = { 0, 1, 0, 3, 4, 0, 4};

        // チームを表す定数
        public const int RED_TEAM = 0;
        public const int BLUE_TEAM = 1;
        public static readonly string[] TEAM_NAMES = new string[2]{"Red", "Blue"};

        public const string RULE_SET_VERSION = "rule_set_version_0100";
    }
}
