using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    public partial class BattleResult : Form {

		public BattleResult() {
            InitializeComponent();
        }

        private static bool IsOKButtonPushed;
		private static int MyUnitHP;
        private static int MyUnitType;
	    private static int EnUnitHP;
        private static int EnUnitType;
        private static int MyTeam;
		private static int[] Damages;



        // 設定画面を出し，ＯＫが押されるまで待ち，設定を返す．
        public static bool setBattleResult(int myUnitHP, int enUnitHP, int myUnitType, int enUnitType, int[] damages, int myTeam) {
			BattleResult form = new BattleResult();
            form.StartPosition = FormStartPosition.Manual;
            form.Left = MainForm.ActiveForm.Left+(MainForm.ActiveForm.Width-form.Width)/2;
            form.Top = MainForm.ActiveForm.Top + (MainForm.ActiveForm.Height - form.Height) / 2;
			MyUnitHP = myUnitHP;
            MyUnitType = myUnitType;
			EnUnitHP = enUnitHP;
            EnUnitType = enUnitType;
            MyTeam = myTeam;
			Damages = damages;
            form.ShowDialog();
            return IsOKButtonPushed;
        }

        // この場合のみ情報を保存して，元の関数は trueを返す
        private void Bt_OK_Click(object sender, EventArgs e) {
            IsOKButtonPushed = true;
            this.Close();
        }

        // キャンセル．
        private void Bt_Cancel_Click(object sender, EventArgs e) {
            this.Close();
        }
        // 毎回表示のたびに呼ばれる．
        private void BattleResult_Shown(object sender, EventArgs e) {
            // デフォルトでは，キャンセル扱い．
            IsOKButtonPushed = false;

            //表示する画像の選択
            Pb_MyUnit.Image = DrawManager.fUnitBmps[MyTeam, MyUnitType];
            Pb_EnUnit.Image = DrawManager.fUnitBmps[(MyTeam + 1) % 2, EnUnitType];
			// 対戦結果表示
			//自
            Lbl_MyHP.Text = MyUnitHP.ToString() + " → " + (MyUnitHP - Damages[1]).ToString();
			//敵
			Lbl_EnHP.Text = EnUnitHP.ToString() + " → " + (EnUnitHP-Damages[0]).ToString();
        }

    }
}
