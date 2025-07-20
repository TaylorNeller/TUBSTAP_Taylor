using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    // Playerの種類を登録するためのクラス
    // 新しい思考ルーチンを追加した場合はここを編集してください．
    class PlayerList {

        // プレイヤーオブジェクトのリスト．ここに必要なクラスを登録してください
        public static Player[] sRegisteredPlayerList = new Player[] {
            new HumanPlayer(),           // これは変更しないでください
            new NetworkPlayer(),         // これは変更しないでください
			new AI_M_UCT(),
            new AI_Sample_MaxActEval(),
            new AI_RHEA(),
            new AI_RHCP(),
            new AI_TBETS(),
            new AI_EMCTS(),
            new AI_EMCTS_FH(),
            // new AI_SatTS_D2(),
            new AI_M_UCT_PW()
        };

        // デフォルトで対戦させたい 2プレイヤーのインデックスをここに登録する
        public static readonly int[] sDefaultPlayerIndex = new int[2] { 0, 2 };

        // 以降はユーザがいじらなくてもよい部分 -------------

        // playerの種類
        public const int HUMAN_PLAYER = 0;
        public const int NETWORK_PLAYER = 1;

        // indexに指定されたPlayerオブジェクトを返す
        public static Player getPlayer(int playerIndex) {
            return sRegisteredPlayerList[playerIndex];
        }

        // コンボボックスのプレイヤーリストを初期化する
        public static void initializePlayerListComboBox(ComboBox cb_red, ComboBox cb_blue) {
            cb_red.Items.Clear();
            cb_blue.Items.Clear();

            foreach (Player p in sRegisteredPlayerList) {
                cb_red.Items.Add(p.getName());
                cb_blue.Items.Add(p.getName());
            }

            cb_red.SelectedIndex = sDefaultPlayerIndex[Consts.RED_TEAM];
            cb_blue.SelectedIndex = sDefaultPlayerIndex[Consts.BLUE_TEAM];
        }
    }
}
