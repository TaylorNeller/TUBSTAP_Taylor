namespace SimpleWars {
	partial class Settings {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.button_OK = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_wait = new System.Windows.Forms.ComboBox();
            this.comboBox_limittime = new System.Windows.Forms.ComboBox();
            this.comboBox_port = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBox_ip = new System.Windows.Forms.ComboBox();
            this.checkBox_showingNetLog = new System.Windows.Forms.CheckBox();
            this.textBox_username = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.bt_save = new System.Windows.Forms.Button();
            this.bt_load = new System.Windows.Forms.Button();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Animation Wait";
            // 
            // button_OK
            // 
            this.button_OK.Location = new System.Drawing.Point(257, 285);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(56, 24);
            this.button_OK.TabIndex = 1;
            this.button_OK.Text = "OK";
            this.button_OK.UseVisualStyleBackColor = true;
            this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.Location = new System.Drawing.Point(440, 285);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(56, 24);
            this.button_Cancel.TabIndex = 2;
            this.button_Cancel.Text = "Cancel";
            this.button_Cancel.UseVisualStyleBackColor = true;
            this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(140, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Seconds given per a move";
            // 
            // comboBox_wait
            // 
            this.comboBox_wait.FormattingEnabled = true;
            this.comboBox_wait.Items.AddRange(new object[] {
            "0",
            "1",
            "5",
            "10",
            "15",
            "20",
            "30"});
            this.comboBox_wait.Location = new System.Drawing.Point(158, 6);
            this.comboBox_wait.Name = "comboBox_wait";
            this.comboBox_wait.Size = new System.Drawing.Size(121, 20);
            this.comboBox_wait.TabIndex = 4;
            // 
            // comboBox_limittime
            // 
            this.comboBox_limittime.FormattingEnabled = true;
            this.comboBox_limittime.Items.AddRange(new object[] {
            "3",
            "10",
            "60",
            "600"});
            this.comboBox_limittime.Location = new System.Drawing.Point(158, 32);
            this.comboBox_limittime.Name = "comboBox_limittime";
            this.comboBox_limittime.Size = new System.Drawing.Size(121, 20);
            this.comboBox_limittime.TabIndex = 5;
            // 
            // comboBox_port
            // 
            this.comboBox_port.FormattingEnabled = true;
            this.comboBox_port.Items.AddRange(new object[] {
            "6741",
            "6742",
            "6743",
            "6744",
            "6745",
            "6746",
            "6747",
            "6748",
            "6749",
            "5961",
            "5962",
            "5963",
            "5964",
            "5965",
            "5966",
            "5967",
            "5968",
            "5969"});
            this.comboBox_port.Location = new System.Drawing.Point(158, 140);
            this.comboBox_port.MaxDropDownItems = 20;
            this.comboBox_port.Name = "comboBox_port";
            this.comboBox_port.Size = new System.Drawing.Size(121, 20);
            this.comboBox_port.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 143);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "Connection Port";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 115);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "Server IP";
            // 
            // comboBox_ip
            // 
            this.comboBox_ip.FormattingEnabled = true;
            this.comboBox_ip.Items.AddRange(new object[] {
            "127.0.0.1",
            "59.106.25.16"});
            this.comboBox_ip.Location = new System.Drawing.Point(158, 112);
            this.comboBox_ip.Name = "comboBox_ip";
            this.comboBox_ip.Size = new System.Drawing.Size(121, 20);
            this.comboBox_ip.TabIndex = 9;
            // 
            // checkBox_showingNetLog
            // 
            this.checkBox_showingNetLog.AutoSize = true;
            this.checkBox_showingNetLog.Location = new System.Drawing.Point(14, 289);
            this.checkBox_showingNetLog.Name = "checkBox_showingNetLog";
            this.checkBox_showingNetLog.Size = new System.Drawing.Size(95, 16);
            this.checkBox_showingNetLog.TabIndex = 11;
            this.checkBox_showingNetLog.Text = "Show Net Log";
            this.checkBox_showingNetLog.UseVisualStyleBackColor = true;
            // 
            // textBox_username
            // 
            this.textBox_username.Location = new System.Drawing.Point(158, 166);
            this.textBox_username.Name = "textBox_username";
            this.textBox_username.Size = new System.Drawing.Size(121, 19);
            this.textBox_username.TabIndex = 13;
            this.textBox_username.Text = "User";
            this.textBox_username.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBox_username_KeyUp);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 169);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "User Name";
            // 
            // bt_save
            // 
            this.bt_save.Location = new System.Drawing.Point(318, 285);
            this.bt_save.Name = "bt_save";
            this.bt_save.Size = new System.Drawing.Size(56, 24);
            this.bt_save.TabIndex = 17;
            this.bt_save.Text = "Save";
            this.bt_save.UseVisualStyleBackColor = true;
            this.bt_save.Click += new System.EventHandler(this.bt_save_Click);
            // 
            // bt_load
            // 
            this.bt_load.Location = new System.Drawing.Point(379, 285);
            this.bt_load.Name = "bt_load";
            this.bt_load.Size = new System.Drawing.Size(56, 24);
            this.bt_load.TabIndex = 18;
            this.bt_load.Text = "Load";
            this.bt_load.UseVisualStyleBackColor = true;
            this.bt_load.Click += new System.EventHandler(this.bt_load_Click);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(14, 267);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(176, 16);
            this.checkBox2.TabIndex = 19;
            this.checkBox2.Text = "Start as server with server IP";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 317);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.bt_load);
            this.Controls.Add(this.bt_save);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox_username);
            this.Controls.Add(this.checkBox_showingNetLog);
            this.Controls.Add(this.comboBox_ip);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBox_port);
            this.Controls.Add(this.comboBox_limittime);
            this.Controls.Add(this.comboBox_wait);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.label1);
            this.Name = "Settings";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            this.Shown += new System.EventHandler(this.Form_Settings_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBox_wait;
		private System.Windows.Forms.ComboBox comboBox_limittime;
		private System.Windows.Forms.ComboBox comboBox_port;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox comboBox_ip;
		private System.Windows.Forms.CheckBox checkBox_showingNetLog;
		private System.Windows.Forms.TextBox textBox_username;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Button bt_save;
		private System.Windows.Forms.Button bt_load;
        private System.Windows.Forms.CheckBox checkBox2;
	}
}