using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace SimpleWars {
	public partial class Settings : Form {

		//設定類
		public static int ConnectionPort = 7777;
		public static String ConnectionIP = "25.64.182.163";
		public static int LimitTime = 10;
		public static int TOTALWAIT = 0;

		// netlog を表示するかどうか
		public static bool showingNetLog = false;

		// ailog を表示するかどうか
		public static bool showingAILog = false;

		// Start-as-server を表示するかどうか
		public static bool enableStartAsServer = false;

		// ユーザの名前
		public static String userName = "name";

		// AI 同士の戦いを繰り返すかどうか
		public static bool repeatAIBattles = false;

        // Serverとして起動する場合のIPを自身で決めるか
        public static bool useServerIP = false;

		public Settings() {
			InitializeComponent();
		}

		private void button_Cancel_Click(object sender, EventArgs e) {
			Logger.addLogMessage("Settings change canceled.",0);
			this.Close();
		}

		// Settingsの中身を読んで画面に現在の値を表示
		private void Form_Settings_Shown(object sender, EventArgs e) {
			comboBox_ip.Text = Settings.ConnectionIP;
			comboBox_port.Text = Settings.ConnectionPort.ToString();
			comboBox_limittime.Text = Settings.LimitTime.ToString();
			comboBox_wait.Text = Settings.TOTALWAIT.ToString();
			checkBox_showingNetLog.Checked = Settings.showingNetLog;
			textBox_username.Text = Settings.userName;

			if (File.Exists("TUBSTAP_settings.txt") == true) {
				bt_load.Enabled = true;
			} else {
				bt_load.Enabled = false;
			}
		}

		private void button_OK_Click(object sender, EventArgs e) {
			Settings.ConnectionIP = comboBox_ip.Text;
			Settings.ConnectionPort = Int32.Parse(comboBox_port.Text);
			Settings.LimitTime = Int32.Parse(comboBox_limittime.Text);
			Settings.TOTALWAIT = Int32.Parse(comboBox_wait.Text);
			Settings.showingNetLog = checkBox_showingNetLog.Checked;
			Settings.userName = textBox_username.Text;
            Settings.useServerIP = checkBox2.Checked;
			Logger.addLogMessage("Settnigs changed",0);
			this.Close();
		}

		private void textBox_username_KeyUp(object sender, KeyEventArgs e) {
		}

		private String boolToString(bool b) {
			if (b == true) {
				return "true";
			} else {
				return "false";
			}
		}

		private bool StringToBool(String s) {
			if (s.Equals("true") == true) {
				return true;
			} else {
				return false;
			}
		}

		// 設定をファイルに書き込む．
		private void bt_save_Click(object sender, EventArgs e) {
			using (StreamWriter sw = new StreamWriter("TUBSTAP_settings.txt")) {
				sw.WriteLine("ConnectionIP " + comboBox_ip.Text);
				sw.WriteLine("ConnectionPort " + comboBox_port.Text);
				sw.WriteLine("LimitTime " + comboBox_limittime.Text);
				sw.WriteLine("TOTALWAIT " + comboBox_wait.Text);
				sw.WriteLine("showingNetLog " + boolToString(checkBox_showingNetLog.Checked));
				sw.WriteLine("userName " + textBox_username.Text);
			}
			bt_load.Enabled = true;
			Logger.addLogMessage("Settings saved to TUBSTAP_settngs.txt", 0);
		}

		// 設定をファイルから読み込む．一応，順番や書式の乱れを許す．
		private void bt_load_Click(object sender, EventArgs e) {
			if (File.Exists("TUBSTAP_settings.txt") == false) {
				MessageBox.Show("TUBSTAP_settings.txt doesn't exist.");
				return;
			}

			using (StreamReader sr = new StreamReader("TUBSTAP_settings.txt")) {
				String line;
				while ((line = sr.ReadLine()) != null) {
					if (line.Length < 3) continue;
					if (line.IndexOf(" ") == -1) continue;
					String[] words = line.Split(' ');
					if (words[0].Equals("ConnectionIP") == true) comboBox_ip.Text = words[1];
					if (words[0].Equals("ConnectionPort") == true) comboBox_port.Text = words[1];
					if (words[0].Equals("LimitTime") == true) comboBox_limittime.Text = words[1];
					if (words[0].Equals("TOTALWAIT") == true) comboBox_wait.Text = words[1];
					if (words[0].Equals("showingNetLog") == true) checkBox_showingNetLog.Checked = StringToBool(words[1]);
				}
				Logger.addLogMessage("Settings loaded from TUBSTAP_settings.txt", 0);
			}

		}

		private void Settings_Load(object sender, EventArgs e) {

		}

	}
}
