namespace SimpleWars {
	partial class BattleCheat {
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
			this.Lbl_MyHP = new System.Windows.Forms.Label();
			this.Lbl_EnHP = new System.Windows.Forms.Label();
			this.Tb_RedUnitHP = new System.Windows.Forms.TextBox();
			this.Tb_BlueUnitHP = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// Bt_OK
			// 
			this.Bt_OK.Location = new System.Drawing.Point(53, 60);
			this.Bt_OK.Name = "Bt_OK";
			this.Bt_OK.Size = new System.Drawing.Size(75, 23);
			this.Bt_OK.TabIndex = 0;
			this.Bt_OK.Text = "OK";
			this.Bt_OK.UseVisualStyleBackColor = true;
			this.Bt_OK.Click += new System.EventHandler(this.Bt_OK_Click);
			// 
			// Lbl_MyHP
			// 
			this.Lbl_MyHP.AutoSize = true;
			this.Lbl_MyHP.Location = new System.Drawing.Point(19, 34);
			this.Lbl_MyHP.Name = "Lbl_MyHP";
			this.Lbl_MyHP.Size = new System.Drawing.Size(61, 12);
			this.Lbl_MyHP.TabIndex = 5;
			this.Lbl_MyHP.Text = "RedUnitHP";
			// 
			// Lbl_EnHP
			// 
			this.Lbl_EnHP.AutoSize = true;
			this.Lbl_EnHP.Location = new System.Drawing.Point(103, 34);
			this.Lbl_EnHP.Name = "Lbl_EnHP";
			this.Lbl_EnHP.Size = new System.Drawing.Size(64, 12);
			this.Lbl_EnHP.TabIndex = 6;
			this.Lbl_EnHP.Text = "BlueUnitHP";
			// 
			// Tb_RedUnitHP
			// 
			this.Tb_RedUnitHP.Location = new System.Drawing.Point(28, 12);
			this.Tb_RedUnitHP.Name = "Tb_RedUnitHP";
			this.Tb_RedUnitHP.Size = new System.Drawing.Size(41, 19);
			this.Tb_RedUnitHP.TabIndex = 7;
			// 
			// Tb_BlueUnitHP
			// 
			this.Tb_BlueUnitHP.Location = new System.Drawing.Point(111, 12);
			this.Tb_BlueUnitHP.Name = "Tb_BlueUnitHP";
			this.Tb_BlueUnitHP.Size = new System.Drawing.Size(41, 19);
			this.Tb_BlueUnitHP.TabIndex = 8;
			// 
			// CheatBattle
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(177, 90);
			this.Controls.Add(this.Tb_BlueUnitHP);
			this.Controls.Add(this.Tb_RedUnitHP);
			this.Controls.Add(this.Lbl_EnHP);
			this.Controls.Add(this.Lbl_MyHP);
			this.Controls.Add(this.Bt_OK);
			this.Name = "CheatBattle";
			this.Text = "残りHP入力";
			this.Shown += new System.EventHandler(this.SetDamage_Shown);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.Button Bt_OK;
		private System.Windows.Forms.Label Lbl_MyHP;
		private System.Windows.Forms.Label Lbl_EnHP;
		private System.Windows.Forms.TextBox Tb_RedUnitHP;
		private System.Windows.Forms.TextBox Tb_BlueUnitHP;
    }
}