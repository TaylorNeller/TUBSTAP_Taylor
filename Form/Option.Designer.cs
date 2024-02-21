namespace SimpleWars {
	partial class Option {
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
			this.ChB_CombatResult = new System.Windows.Forms.CheckBox();
			this.ChB_BattleCheat = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// Bt_OK
			// 
			this.Bt_OK.Location = new System.Drawing.Point(134, 191);
			this.Bt_OK.Name = "Bt_OK";
			this.Bt_OK.Size = new System.Drawing.Size(75, 23);
			this.Bt_OK.TabIndex = 0;
			this.Bt_OK.Text = "OK";
			this.Bt_OK.UseVisualStyleBackColor = true;
			this.Bt_OK.Click += new System.EventHandler(this.Bt_OK_Click);
			// 
			// ChB_CombatResult
			// 
			this.ChB_CombatResult.AutoSize = true;
			this.ChB_CombatResult.Location = new System.Drawing.Point(14, 12);
			this.ChB_CombatResult.Name = "ChB_CombatResult";
			this.ChB_CombatResult.Size = new System.Drawing.Size(145, 16);
			this.ChB_CombatResult.TabIndex = 4;
			this.ChB_CombatResult.Text = "戦闘前に結果を表示する";
			this.ChB_CombatResult.UseVisualStyleBackColor = true;
			// 
			// ChB_BattleCheat
			// 
			this.ChB_BattleCheat.AutoSize = true;
			this.ChB_BattleCheat.Location = new System.Drawing.Point(14, 34);
			this.ChB_BattleCheat.Name = "ChB_BattleCheat";
			this.ChB_BattleCheat.Size = new System.Drawing.Size(124, 16);
			this.ChB_BattleCheat.TabIndex = 5;
			this.ChB_BattleCheat.Text = "戦闘結果を操作する";
			this.ChB_BattleCheat.UseVisualStyleBackColor = true;
			// 
			// Option
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(347, 226);
			this.Controls.Add(this.ChB_BattleCheat);
			this.Controls.Add(this.ChB_CombatResult);
			this.Controls.Add(this.Bt_OK);
			this.Name = "Option";
			this.Text = "Option";
			this.Shown += new System.EventHandler(this.Option_Shown);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.Button Bt_OK;
		private System.Windows.Forms.CheckBox ChB_CombatResult;
		private System.Windows.Forms.CheckBox ChB_BattleCheat;
    }
}