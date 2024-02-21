using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    // version 0.104で追加．自動対戦系の設定３つを保持し，それを変更するダイアログを出せる．
    public partial class AutoBattleSettings : Form {
        public AutoBattleSettings() {
            InitializeComponent();
        }

        public static int NumberOfGamesPerMap = 100;
        public static bool IsPositionRandomlyMoved = false;
        public static bool IsHPRandomlyDecreased = false;
		public static bool IsPosChange = true;

        private static bool IsOKButtonPushed;

        // 設定画面を出し，ＯＫが押されるまで待ち，設定を返す．
        public static bool setAutoBattleSettings() {
            AutoBattleSettings form = new AutoBattleSettings();
            form.ShowDialog();

            return IsOKButtonPushed;
        }

        // この場合のみ情報を保存して，元の関数は trueを返す
        private void Bt_OK_Click(object sender, EventArgs e) {
            NumberOfGamesPerMap = Int32.Parse(CoB_numBattles.Text);

            IsPositionRandomlyMoved = ChB_move.Checked;
            IsHPRandomlyDecreased = ChB_decreaseHP.Checked;
			IsPosChange = ChB_changePos.Checked;

            IsOKButtonPushed = true;
            this.Close();
        }

        // キャンセル．
        private void Bt_Cancel_Click(object sender, EventArgs e) {
            this.Close();
        }

        // 毎回表示のたびに呼ばれる．
        private void AutoBattleSettings_Shown(object sender, EventArgs e) {
            // 現在の設定を画面に反映させる
            for (int i = 0; i < CoB_numBattles.Items.Count; i++) {
                if (Int32.Parse((string)CoB_numBattles.Items[i]) == NumberOfGamesPerMap) CoB_numBattles.SelectedIndex = i;
            }
            ChB_move.Checked = IsPositionRandomlyMoved;
            ChB_decreaseHP.Checked = IsHPRandomlyDecreased;
			ChB_changePos.Checked = IsPosChange;

            // デフォルトでは，キャンセル扱い．
            IsOKButtonPushed = false;
        }
    }
}
