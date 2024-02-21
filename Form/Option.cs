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
    public partial class Option : Form {
        public Option() {
            InitializeComponent();
        }

        public static bool IsCombatResultShowed = true;
		public static bool IsCheatUsed = false;

        private static bool IsOKButtonPushed;

        // 設定画面を出し，ＯＫが押されるまで待ち，設定を返す．
        public static bool setOption() {
            Option form = new Option();
            form.ShowDialog();

            return IsOKButtonPushed;
        }

        // この場合のみ情報を保存して，元の関数は trueを返す
        private void Bt_OK_Click(object sender, EventArgs e) {

            IsCombatResultShowed = ChB_CombatResult.Checked;
			IsCheatUsed = ChB_BattleCheat.Checked;

            IsOKButtonPushed = true;
            this.Close();
        }

        // キャンセル．
        private void Bt_Cancel_Click(object sender, EventArgs e) {
            this.Close();
        }

        // 毎回表示のたびに呼ばれる．
        private void Option_Shown(object sender, EventArgs e) {
            // 現在の設定を画面に反映させる
            ChB_CombatResult.Checked = IsCombatResultShowed;
			ChB_BattleCheat.Checked=IsCheatUsed;

            // デフォルトでは，キャンセル扱い．
            IsOKButtonPushed = false;
        }
    }
}
