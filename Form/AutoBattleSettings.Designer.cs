namespace SimpleWars {
    partial class AutoBattleSettings {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
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
			this.Bt_OK = new System.Windows.Forms.Button();
			this.Bt_Cancel = new System.Windows.Forms.Button();
			this.CoB_numBattles = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.ChB_move = new System.Windows.Forms.CheckBox();
			this.ChB_decreaseHP = new System.Windows.Forms.CheckBox();
			this.ChB_changePos = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// Bt_OK
			// 
			this.Bt_OK.Location = new System.Drawing.Point(151, 191);
			this.Bt_OK.Name = "Bt_OK";
			this.Bt_OK.Size = new System.Drawing.Size(75, 23);
			this.Bt_OK.TabIndex = 0;
			this.Bt_OK.Text = "開始";
			this.Bt_OK.UseVisualStyleBackColor = true;
			this.Bt_OK.Click += new System.EventHandler(this.Bt_OK_Click);
			// 
			// Bt_Cancel
			// 
			this.Bt_Cancel.Location = new System.Drawing.Point(249, 191);
			this.Bt_Cancel.Name = "Bt_Cancel";
			this.Bt_Cancel.Size = new System.Drawing.Size(75, 23);
			this.Bt_Cancel.TabIndex = 1;
			this.Bt_Cancel.Text = "戻る";
			this.Bt_Cancel.UseVisualStyleBackColor = true;
			this.Bt_Cancel.Click += new System.EventHandler(this.Bt_Cancel_Click);
			// 
			// CoB_numBattles
			// 
			this.CoB_numBattles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.CoB_numBattles.FormattingEnabled = true;
			this.CoB_numBattles.Items.AddRange(new object[] {
            "2",
            "6",
            "10",
            "20",
            "30",
            "50",
            "70",
            "100",
            "150",
            "200",
            "300",
            "500",
            "1000",
            "1500",
            "2000",
            "3000"});
			this.CoB_numBattles.Location = new System.Drawing.Point(119, 26);
			this.CoB_numBattles.Name = "CoB_numBattles";
			this.CoB_numBattles.Size = new System.Drawing.Size(121, 20);
			this.CoB_numBattles.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 29);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(101, 12);
			this.label1.TabIndex = 3;
			this.label1.Text = "１マップあたり対戦数";
			// 
			// ChB_move
			// 
			this.ChB_move.AutoSize = true;
			this.ChB_move.Location = new System.Drawing.Point(14, 65);
			this.ChB_move.Name = "ChB_move";
			this.ChB_move.Size = new System.Drawing.Size(232, 16);
			this.ChB_move.TabIndex = 4;
			this.ChB_move.Text = "ゲームごとに初期位置をランダムで少しずらす";
			this.ChB_move.UseVisualStyleBackColor = true;
			// 
			// ChB_decreaseHP
			// 
			this.ChB_decreaseHP.AutoSize = true;
			this.ChB_decreaseHP.Location = new System.Drawing.Point(14, 97);
			this.ChB_decreaseHP.Name = "ChB_decreaseHP";
			this.ChB_decreaseHP.Size = new System.Drawing.Size(201, 16);
			this.ChB_decreaseHP.TabIndex = 5;
			this.ChB_decreaseHP.Text = "ゲームごとにHPをランダムで少し減らす";
			this.ChB_decreaseHP.UseVisualStyleBackColor = true;
			// 
			// ChB_changePos
			// 
			this.ChB_changePos.AutoSize = true;
			this.ChB_changePos.Checked = true;
			this.ChB_changePos.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ChB_changePos.Location = new System.Drawing.Point(14, 131);
			this.ChB_changePos.Name = "ChB_changePos";
			this.ChB_changePos.Size = new System.Drawing.Size(204, 16);
			this.ChB_changePos.TabIndex = 6;
			this.ChB_changePos.Text = "対戦数の半数を先手後手入れ替える";
			this.ChB_changePos.UseVisualStyleBackColor = true;
			// 
			// AutoBattleSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(347, 226);
			this.Controls.Add(this.ChB_changePos);
			this.Controls.Add(this.ChB_decreaseHP);
			this.Controls.Add(this.ChB_move);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.CoB_numBattles);
			this.Controls.Add(this.Bt_Cancel);
			this.Controls.Add(this.Bt_OK);
			this.Name = "AutoBattleSettings";
			this.Text = "自動対戦設定";
			this.Shown += new System.EventHandler(this.AutoBattleSettings_Shown);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Bt_OK;
        private System.Windows.Forms.Button Bt_Cancel;
        private System.Windows.Forms.ComboBox CoB_numBattles;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox ChB_move;
        private System.Windows.Forms.CheckBox ChB_decreaseHP;
        private System.Windows.Forms.CheckBox ChB_changePos;
    }
}