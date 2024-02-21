using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    public partial class BattleCheat : Form {

		public BattleCheat() {
            InitializeComponent();
        }

        private static bool IsOKButtonPushed;
		public static int RedUnitHP;
        public static int BlueUnitHP;


        // 設定画面を出し，ＯＫが押されるまで待ち，設定を返す．
		public static bool SetBattleResult() {
			BattleCheat form = new BattleCheat();
            form.StartPosition = FormStartPosition.Manual;
            form.Left = MainForm.ActiveForm.Left+(MainForm.ActiveForm.Width-form.Width)/2;
            form.Top = MainForm.ActiveForm.Top + (MainForm.ActiveForm.Height - form.Height) / 2;
            form.ShowDialog();
            return IsOKButtonPushed;
        }

        // この場合のみ情報を保存して，元の関数は trueを返す
        private void Bt_OK_Click(object sender, EventArgs e) {
			if (Tb_RedUnitHP.Text == "" || Tb_BlueUnitHP.Text == "") return;

            IsOKButtonPushed = true;
			RedUnitHP = Convert.ToInt32(Tb_RedUnitHP.Text);
			BlueUnitHP = Convert.ToInt32(Tb_BlueUnitHP.Text);
            this.Close();
        }

        // キャンセル．
        private void Bt_Cancel_Click(object sender, EventArgs e) {
            this.Close();
        }
        // 毎回表示のたびに呼ばれる．
		private void SetDamage_Shown(object sender, EventArgs e) {
            // デフォルトでは，キャンセル扱い．
            IsOKButtonPushed = false;
        }

		private void Tb_RedUnitHP_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
			if (e.KeyChar < '0' || '9' < e.KeyChar) {
				//押されたキーが 0～9でない場合は、イベントをキャンセルする
				e.Handled = true;
			}
		}

		private void Tb_BlueUnitHP_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
			if (e.KeyChar < '0' || '9' < e.KeyChar) {
				//押されたキーが 0～9でない場合は、イベントをキャンセルする
				e.Handled = true;
			}
		}

		protected override void WndProc(ref Message m) {
			const int WM_SYSCOMMAND = 0x112;
			const long SC_CLOSE = 0xF060L;

			if (m.Msg == WM_SYSCOMMAND &&
				(m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE) {
				return;
			}

			base.WndProc(ref m);
		}
    }
}
