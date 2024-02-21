namespace SimpleWars
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fILEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadreplayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.メニューToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GameStartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.gameEndToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resingnetionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoBattleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.eXITToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.uNDOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.oneActionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.oneTurnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.settingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.startAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			this.comboBox_REDTEAM = new System.Windows.Forms.ComboBox();
			this.comboBox_BLUETEAM = new System.Windows.Forms.ComboBox();
			this.label_RedTeam = new System.Windows.Forms.Label();
			this.label_BlueTeam = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.bt_replayBackStep = new System.Windows.Forms.Button();
			this.bt_replayNextStep = new System.Windows.Forms.Button();
			this.bt_replayNextTurn = new System.Windows.Forms.Button();
			this.bt_replayBackTurn = new System.Windows.Forms.Button();
			this.bt_TurnEnd = new System.Windows.Forms.Button();
			this.comboBox_MapFile = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.BlueLog = new System.Windows.Forms.TextBox();
			this.bt_logboxclear = new System.Windows.Forms.Button();
			this.RedLog = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.UnitID = new System.Windows.Forms.Label();
			this.menuStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fILEToolStripMenuItem,
            this.メニューToolStripMenuItem,
            this.uNDOToolStripMenuItem,
            this.settingToolStripMenuItem,
            this.optionToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.startAsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(1044, 24);
			this.menuStrip1.TabIndex = 14;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fILEToolStripMenuItem
			// 
			this.fILEToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem,
            this.loadToolStripMenuItem,
            this.loadreplayToolStripMenuItem});
			this.fILEToolStripMenuItem.Name = "fILEToolStripMenuItem";
			this.fILEToolStripMenuItem.Size = new System.Drawing.Size(36, 20);
			this.fILEToolStripMenuItem.Text = "File";
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Enabled = false;
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
			this.saveToolStripMenuItem.Text = "Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// loadToolStripMenuItem
			// 
			this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
			this.loadToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
			this.loadToolStripMenuItem.Text = "Load (save file)";
			this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
			// 
			// loadreplayToolStripMenuItem
			// 
			this.loadreplayToolStripMenuItem.Name = "loadreplayToolStripMenuItem";
			this.loadreplayToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
			this.loadreplayToolStripMenuItem.Text = "Load (replay)";
			this.loadreplayToolStripMenuItem.Click += new System.EventHandler(this.loadreplayToolStripMenuItem_Click);
			// 
			// メニューToolStripMenuItem
			// 
			this.メニューToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GameStartToolStripMenuItem,
            this.gameEndToolStripMenuItem,
            this.resingnetionToolStripMenuItem,
            this.autoBattleToolStripMenuItem,
            this.eXITToolStripMenuItem});
			this.メニューToolStripMenuItem.Name = "メニューToolStripMenuItem";
			this.メニューToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.メニューToolStripMenuItem.Text = "Menu";
			// 
			// GameStartToolStripMenuItem
			// 
			this.GameStartToolStripMenuItem.Name = "GameStartToolStripMenuItem";
			this.GameStartToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.GameStartToolStripMenuItem.Text = "Game Start";
			this.GameStartToolStripMenuItem.Click += new System.EventHandler(this.GameStartToolStripMenuItem_Click);
			// 
			// gameEndToolStripMenuItem
			// 
			this.gameEndToolStripMenuItem.Enabled = false;
			this.gameEndToolStripMenuItem.Name = "gameEndToolStripMenuItem";
			this.gameEndToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.gameEndToolStripMenuItem.Text = "Game End";
			this.gameEndToolStripMenuItem.Click += new System.EventHandler(this.gameEndToolStripMenuItem_Click);
			// 
			// resingnetionToolStripMenuItem
			// 
			this.resingnetionToolStripMenuItem.Enabled = false;
			this.resingnetionToolStripMenuItem.Name = "resingnetionToolStripMenuItem";
			this.resingnetionToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.resingnetionToolStripMenuItem.Text = "Resingnetion";
			this.resingnetionToolStripMenuItem.Click += new System.EventHandler(this.resingnetionToolStripMenuItem_Click);
			// 
			// autoBattleToolStripMenuItem
			// 
			this.autoBattleToolStripMenuItem.Name = "autoBattleToolStripMenuItem";
			this.autoBattleToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.autoBattleToolStripMenuItem.Text = "Auto Battle";
			this.autoBattleToolStripMenuItem.Click += new System.EventHandler(this.autoBattleToolStripMenuItem_Click);
			// 
			// eXITToolStripMenuItem
			// 
			this.eXITToolStripMenuItem.Name = "eXITToolStripMenuItem";
			this.eXITToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
			this.eXITToolStripMenuItem.Text = "Exit";
			this.eXITToolStripMenuItem.Click += new System.EventHandler(this.eXITToolStripMenuItem_Click);
			// 
			// uNDOToolStripMenuItem
			// 
			this.uNDOToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.oneActionToolStripMenuItem,
            this.oneTurnToolStripMenuItem});
			this.uNDOToolStripMenuItem.Name = "uNDOToolStripMenuItem";
			this.uNDOToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
			this.uNDOToolStripMenuItem.Text = "Undo";
			// 
			// oneActionToolStripMenuItem
			// 
			this.oneActionToolStripMenuItem.Enabled = false;
			this.oneActionToolStripMenuItem.Name = "oneActionToolStripMenuItem";
			this.oneActionToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
			this.oneActionToolStripMenuItem.Text = "One Action";
			this.oneActionToolStripMenuItem.Click += new System.EventHandler(this.oneActionToolStripMenuItem_Click);
			// 
			// oneTurnToolStripMenuItem
			// 
			this.oneTurnToolStripMenuItem.Enabled = false;
			this.oneTurnToolStripMenuItem.Name = "oneTurnToolStripMenuItem";
			this.oneTurnToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
			this.oneTurnToolStripMenuItem.Text = "One Turn";
			this.oneTurnToolStripMenuItem.Click += new System.EventHandler(this.oneTurnToolStripMenuItem_Click);
			// 
			// settingToolStripMenuItem
			// 
			this.settingToolStripMenuItem.Name = "settingToolStripMenuItem";
			this.settingToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
			this.settingToolStripMenuItem.Text = "Setting";
			this.settingToolStripMenuItem.Click += new System.EventHandler(this.settingToolStripMenuItem_Click);
			// 
			// optionToolStripMenuItem
			// 
			this.optionToolStripMenuItem.Name = "optionToolStripMenuItem";
			this.optionToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
			this.optionToolStripMenuItem.Text = "Option";
			this.optionToolStripMenuItem.Click += new System.EventHandler(this.optionToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
			this.helpToolStripMenuItem.Text = "About";
			this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
			// 
			// startAsToolStripMenuItem
			// 
			this.startAsToolStripMenuItem.Name = "startAsToolStripMenuItem";
			this.startAsToolStripMenuItem.Size = new System.Drawing.Size(98, 20);
			this.startAsToolStripMenuItem.Text = "Start-as-server";
			this.startAsToolStripMenuItem.Click += new System.EventHandler(this.startAsToolStripMenuItem_Click);
			// 
			// pictureBox
			// 
			this.pictureBox.Location = new System.Drawing.Point(22, 83);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(540, 401);
			this.pictureBox.TabIndex = 0;
			this.pictureBox.TabStop = false;
			// 
			// comboBox_REDTEAM
			// 
			this.comboBox_REDTEAM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_REDTEAM.FormattingEnabled = true;
			this.comboBox_REDTEAM.Items.AddRange(new object[] {
            "HumanPlayer",
            "AI_HeuristhicC",
            "ACT_ALLCOMBINATION"});
			this.comboBox_REDTEAM.Location = new System.Drawing.Point(141, 33);
			this.comboBox_REDTEAM.Name = "comboBox_REDTEAM";
			this.comboBox_REDTEAM.Size = new System.Drawing.Size(166, 20);
			this.comboBox_REDTEAM.TabIndex = 15;
			// 
			// comboBox_BLUETEAM
			// 
			this.comboBox_BLUETEAM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_BLUETEAM.FormattingEnabled = true;
			this.comboBox_BLUETEAM.Items.AddRange(new object[] {
            "HumanPlayer",
            "AI_HeuristhicC",
            "ACT_ALLCOMBINATION"});
			this.comboBox_BLUETEAM.Location = new System.Drawing.Point(396, 34);
			this.comboBox_BLUETEAM.Name = "comboBox_BLUETEAM";
			this.comboBox_BLUETEAM.Size = new System.Drawing.Size(166, 20);
			this.comboBox_BLUETEAM.TabIndex = 16;
			// 
			// label_RedTeam
			// 
			this.label_RedTeam.AutoSize = true;
			this.label_RedTeam.Location = new System.Drawing.Point(80, 34);
			this.label_RedTeam.Name = "label_RedTeam";
			this.label_RedTeam.Size = new System.Drawing.Size(61, 12);
			this.label_RedTeam.TabIndex = 17;
			this.label_RedTeam.Text = "Red Player";
			// 
			// label_BlueTeam
			// 
			this.label_BlueTeam.AutoSize = true;
			this.label_BlueTeam.Location = new System.Drawing.Point(331, 34);
			this.label_BlueTeam.Name = "label_BlueTeam";
			this.label_BlueTeam.Size = new System.Drawing.Size(64, 12);
			this.label_BlueTeam.TabIndex = 18;
			this.label_BlueTeam.Text = "Blue Player";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(587, 68);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(47, 12);
			this.label1.TabIndex = 23;
			this.label1.Text = "Red Log";
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// openFileDialog
			// 
			this.openFileDialog.FileName = "openFileDialog";
			// 
			// bt_replayBackStep
			// 
			this.bt_replayBackStep.Enabled = false;
			this.bt_replayBackStep.Location = new System.Drawing.Point(496, 495);
			this.bt_replayBackStep.Name = "bt_replayBackStep";
			this.bt_replayBackStep.Size = new System.Drawing.Size(20, 20);
			this.bt_replayBackStep.TabIndex = 24;
			this.bt_replayBackStep.Text = "<";
			this.bt_replayBackStep.UseVisualStyleBackColor = true;
			this.bt_replayBackStep.Click += new System.EventHandler(this.bt_replayBackStep_Click);
			// 
			// bt_replayNextStep
			// 
			this.bt_replayNextStep.Enabled = false;
			this.bt_replayNextStep.Location = new System.Drawing.Point(522, 495);
			this.bt_replayNextStep.Name = "bt_replayNextStep";
			this.bt_replayNextStep.Size = new System.Drawing.Size(20, 20);
			this.bt_replayNextStep.TabIndex = 25;
			this.bt_replayNextStep.Text = ">";
			this.bt_replayNextStep.UseVisualStyleBackColor = true;
			this.bt_replayNextStep.Click += new System.EventHandler(this.bt_replayNextStep_Click);
			// 
			// bt_replayNextTurn
			// 
			this.bt_replayNextTurn.Enabled = false;
			this.bt_replayNextTurn.Location = new System.Drawing.Point(548, 495);
			this.bt_replayNextTurn.Name = "bt_replayNextTurn";
			this.bt_replayNextTurn.Size = new System.Drawing.Size(25, 20);
			this.bt_replayNextTurn.TabIndex = 26;
			this.bt_replayNextTurn.Text = ">>";
			this.bt_replayNextTurn.UseVisualStyleBackColor = true;
			this.bt_replayNextTurn.Click += new System.EventHandler(this.bt_replayNextTurn_Click);
			// 
			// bt_replayBackTurn
			// 
			this.bt_replayBackTurn.Enabled = false;
			this.bt_replayBackTurn.Location = new System.Drawing.Point(465, 495);
			this.bt_replayBackTurn.Name = "bt_replayBackTurn";
			this.bt_replayBackTurn.Size = new System.Drawing.Size(25, 20);
			this.bt_replayBackTurn.TabIndex = 27;
			this.bt_replayBackTurn.Text = "<<";
			this.bt_replayBackTurn.UseVisualStyleBackColor = true;
			this.bt_replayBackTurn.Click += new System.EventHandler(this.bt_replayBackTurn_Click);
			// 
			// bt_TurnEnd
			// 
			this.bt_TurnEnd.Enabled = false;
			this.bt_TurnEnd.Location = new System.Drawing.Point(583, 495);
			this.bt_TurnEnd.Name = "bt_TurnEnd";
			this.bt_TurnEnd.Size = new System.Drawing.Size(75, 20);
			this.bt_TurnEnd.TabIndex = 33;
			this.bt_TurnEnd.Text = "Turn End";
			this.bt_TurnEnd.UseVisualStyleBackColor = true;
			this.bt_TurnEnd.Click += new System.EventHandler(this.bt_TurnEnd_Click);
			// 
			// comboBox_MapFile
			// 
			this.comboBox_MapFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox_MapFile.FormattingEnabled = true;
			this.comboBox_MapFile.Location = new System.Drawing.Point(635, 34);
			this.comboBox_MapFile.Name = "comboBox_MapFile";
			this.comboBox_MapFile.Size = new System.Drawing.Size(151, 20);
			this.comboBox_MapFile.TabIndex = 34;
			this.comboBox_MapFile.SelectedIndexChanged += new System.EventHandler(this.comboBox_MapFile_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(581, 35);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(55, 12);
			this.label4.TabIndex = 35;
			this.label4.Text = "Map File :";
			// 
			// BlueLog
			// 
			this.BlueLog.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.BlueLog.Location = new System.Drawing.Point(581, 83);
			this.BlueLog.Multiline = true;
			this.BlueLog.Name = "BlueLog";
			this.BlueLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.BlueLog.Size = new System.Drawing.Size(451, 187);
			this.BlueLog.TabIndex = 36;
			// 
			// bt_logboxclear
			// 
			this.bt_logboxclear.Location = new System.Drawing.Point(1014, 65);
			this.bt_logboxclear.Margin = new System.Windows.Forms.Padding(2);
			this.bt_logboxclear.Name = "bt_logboxclear";
			this.bt_logboxclear.Size = new System.Drawing.Size(18, 18);
			this.bt_logboxclear.TabIndex = 37;
			this.bt_logboxclear.Text = "×";
			this.bt_logboxclear.UseVisualStyleBackColor = true;
			this.bt_logboxclear.Click += new System.EventHandler(this.bt_logboxclear_Click);
			// 
			// RedLog
			// 
			this.RedLog.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.RedLog.Location = new System.Drawing.Point(581, 297);
			this.RedLog.Multiline = true;
			this.RedLog.Name = "RedLog";
			this.RedLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.RedLog.Size = new System.Drawing.Size(451, 187);
			this.RedLog.TabIndex = 38;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(1014, 279);
			this.button1.Margin = new System.Windows.Forms.Padding(2);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(18, 18);
			this.button1.TabIndex = 39;
			this.button1.Text = "×";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(586, 282);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(50, 12);
			this.label2.TabIndex = 40;
			this.label2.Text = "Blue Log";
			// 
			// UnitID
			// 
			this.UnitID.AutoSize = true;
			this.UnitID.Location = new System.Drawing.Point(20, 499);
			this.UnitID.Name = "UnitID";
			this.UnitID.Size = new System.Drawing.Size(0, 12);
			this.UnitID.TabIndex = 41;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1044, 525);
			this.Controls.Add(this.UnitID);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.RedLog);
			this.Controls.Add(this.bt_logboxclear);
			this.Controls.Add(this.BlueLog);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.comboBox_MapFile);
			this.Controls.Add(this.bt_TurnEnd);
			this.Controls.Add(this.bt_replayBackTurn);
			this.Controls.Add(this.bt_replayNextTurn);
			this.Controls.Add(this.bt_replayNextStep);
			this.Controls.Add(this.bt_replayBackStep);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label_BlueTeam);
			this.Controls.Add(this.label_RedTeam);
			this.Controls.Add(this.comboBox_BLUETEAM);
			this.Controls.Add(this.comboBox_REDTEAM);
			this.Controls.Add(this.pictureBox);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "TUrn Based STrategy Academic Package  version 0.108 2016-03-25";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem メニューToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GameStartToolStripMenuItem;
        public System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.ComboBox comboBox_REDTEAM;
        private System.Windows.Forms.ComboBox comboBox_BLUETEAM;
        private System.Windows.Forms.ToolStripMenuItem eXITToolStripMenuItem;
        private System.Windows.Forms.Label label_RedTeam;
		private System.Windows.Forms.Label label_BlueTeam;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem uNDOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem oneActionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem oneTurnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fILEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button bt_replayBackStep;
        private System.Windows.Forms.Button bt_replayNextStep;
        private System.Windows.Forms.ToolStripMenuItem loadreplayToolStripMenuItem;
        private System.Windows.Forms.Button bt_replayNextTurn;
        private System.Windows.Forms.Button bt_replayBackTurn;
        private System.Windows.Forms.ToolStripMenuItem autoBattleToolStripMenuItem;
		private System.Windows.Forms.Button bt_TurnEnd;
		private System.Windows.Forms.ComboBox comboBox_MapFile;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox BlueLog;
		private System.Windows.Forms.ToolStripMenuItem gameEndToolStripMenuItem;
        private System.Windows.Forms.Button bt_logboxclear;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resingnetionToolStripMenuItem;
		private System.Windows.Forms.TextBox RedLog;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label UnitID;
		private System.Windows.Forms.ToolStripMenuItem optionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem settingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem startAsToolStripMenuItem;
    }
}

