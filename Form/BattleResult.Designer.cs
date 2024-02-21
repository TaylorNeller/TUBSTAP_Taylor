namespace SimpleWars {
    partial class BattleResult {
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
            this.Lbl_MyHP = new System.Windows.Forms.Label();
            this.Lbl_EnHP = new System.Windows.Forms.Label();
            this.Pb_MyUnit = new System.Windows.Forms.PictureBox();
            this.Pb_EnUnit = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.Pb_MyUnit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Pb_EnUnit)).BeginInit();
            this.SuspendLayout();
            // 
            // Bt_OK
            // 
            this.Bt_OK.Location = new System.Drawing.Point(12, 100);
            this.Bt_OK.Name = "Bt_OK";
            this.Bt_OK.Size = new System.Drawing.Size(75, 23);
            this.Bt_OK.TabIndex = 0;
            this.Bt_OK.Text = "攻撃";
            this.Bt_OK.UseVisualStyleBackColor = true;
            this.Bt_OK.Click += new System.EventHandler(this.Bt_OK_Click);
            // 
            // Bt_Cancel
            // 
            this.Bt_Cancel.Location = new System.Drawing.Point(107, 100);
            this.Bt_Cancel.Name = "Bt_Cancel";
            this.Bt_Cancel.Size = new System.Drawing.Size(75, 23);
            this.Bt_Cancel.TabIndex = 1;
            this.Bt_Cancel.Text = "やめる";
            this.Bt_Cancel.UseVisualStyleBackColor = true;
            this.Bt_Cancel.Click += new System.EventHandler(this.Bt_Cancel_Click);
            // 
            // Lbl_MyHP
            // 
            this.Lbl_MyHP.AutoSize = true;
            this.Lbl_MyHP.Location = new System.Drawing.Point(35, 74);
            this.Lbl_MyHP.Name = "Lbl_MyHP";
            this.Lbl_MyHP.Size = new System.Drawing.Size(25, 12);
            this.Lbl_MyHP.TabIndex = 5;
            this.Lbl_MyHP.Text = "test";
            // 
            // Lbl_EnHP
            // 
            this.Lbl_EnHP.AutoSize = true;
            this.Lbl_EnHP.Location = new System.Drawing.Point(127, 74);
            this.Lbl_EnHP.Name = "Lbl_EnHP";
            this.Lbl_EnHP.Size = new System.Drawing.Size(25, 12);
            this.Lbl_EnHP.TabIndex = 6;
            this.Lbl_EnHP.Text = "test";
            // 
            // Pb_MyUnit
            // 
            this.Pb_MyUnit.Location = new System.Drawing.Point(24, 12);
            this.Pb_MyUnit.Name = "Pb_MyUnit";
            this.Pb_MyUnit.Size = new System.Drawing.Size(48, 48);
            this.Pb_MyUnit.TabIndex = 7;
            this.Pb_MyUnit.TabStop = false;
            // 
            // Pb_EnUnit
            // 
            this.Pb_EnUnit.Location = new System.Drawing.Point(114, 12);
            this.Pb_EnUnit.Name = "Pb_EnUnit";
            this.Pb_EnUnit.Size = new System.Drawing.Size(48, 48);
            this.Pb_EnUnit.TabIndex = 8;
            this.Pb_EnUnit.TabStop = false;
            // 
            // BattleResult
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(194, 128);
            this.Controls.Add(this.Pb_EnUnit);
            this.Controls.Add(this.Pb_MyUnit);
            this.Controls.Add(this.Lbl_EnHP);
            this.Controls.Add(this.Lbl_MyHP);
            this.Controls.Add(this.Bt_Cancel);
            this.Controls.Add(this.Bt_OK);
            this.Name = "BattleResult";
            this.Text = "戦闘結果";
            this.Shown += new System.EventHandler(this.BattleResult_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.Pb_MyUnit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Pb_EnUnit)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Bt_OK;
        private System.Windows.Forms.Button Bt_Cancel;
		private System.Windows.Forms.Label Lbl_MyHP;
		private System.Windows.Forms.Label Lbl_EnHP;
		private System.Windows.Forms.PictureBox Pb_MyUnit;
		private System.Windows.Forms.PictureBox Pb_EnUnit;
    }
}