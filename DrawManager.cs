using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimpleWars {
    // 地形やユニットの描画処理を担当するクラス
    // ユーザーは特に意識する必要のないクラス
    class DrawManager {
        public const int BMP_SIZE = 48; // マスのサイズ

        #region ビットマップのフィールド
        public static Bitmap[,] fUnitBmps;// 色，種類ごとの，ユニットのBitMap
        Bitmap[] fFieldBmps;// 地形ごとのBitMap
        Bitmap[] fNumberBmps;// 残りHP表示用フォント
        Bitmap[] fCharacterBmps;// ユニットの種類表示用フォント

        // Endマークと，ユニットの移動可能範囲を表示するビットマップ
        Bitmap fEndMarkBMP;
        Bitmap fRedWindowBMP;
        Bitmap fBlueWindowBMP;

        //フィルター用
        System.Drawing.Imaging.ImageAttributes fIA_05Alpha;
        System.Drawing.Imaging.ImageAttributes fIA_Dark;
        #endregion

        // 描画用コントローラ
        Graphics fGraphics;     // 描画ハンドラ (fPictureBoxから取得）
        static MainForm fForm;         // 親フォーム，Refresh用
        PictureBox fPictureBox; // 描画対象

        // テンポラリな情報群
        Map fMap;
        private bool[,] fCanBeAttacked; // 対応するインデックスのフラグが立っていると選択可能域に赤ワクを表示する
        private bool[,] fRedWindowed;   // 対応する位置がtrueならば，赤ワクを表示する
        static private string[,] fAiSetString;  // AI用の配列　好きな文字列を表示する
        Font font = new Font("Times New Roman", 10, FontStyle.Bold);
        SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        
        public DrawManager(MainForm form, PictureBox pb) {
            fForm = form;
            this.fPictureBox = pb;
            fGraphics = Graphics.FromImage(pb.Image);

            // 地形とユニットのBitMap
            fUnitBmps = new Bitmap[2, Spec.SPECTYPENUM];
            fFieldBmps = new Bitmap[Consts.FIELDTYPENUM];
            fNumberBmps = new Bitmap[11];
            fCharacterBmps = new Bitmap[Spec.SPECTYPENUM];

            #region 各種Bitmap を imgディレクトリから読み込む部分
            // REDチームのBitMap
            fUnitBmps[0, 0] = new Bitmap("./img/Red_fighter.png");
            fUnitBmps[0, 1] = new Bitmap("./img/Red_attacker.png");
            fUnitBmps[0, 2] = new Bitmap("./img/Red_panzer.png");
            fUnitBmps[0, 3] = new Bitmap("./img/Red_cannon.png");
            fUnitBmps[0, 4] = new Bitmap("./img/Red_antiair.png");
            fUnitBmps[0, 5] = new Bitmap("./img/Red_infantry.png");

            // BLUEチームのBitMap
            fUnitBmps[1, 0] = new Bitmap("./img/Blue_fighter.png");
            fUnitBmps[1, 1] = new Bitmap("./img/Blue_attacker.png");
            fUnitBmps[1, 2] = new Bitmap("./img/Blue_panzer.png");
            fUnitBmps[1, 3] = new Bitmap("./img/Blue_cannon.png");
            fUnitBmps[1, 4] = new Bitmap("./img/Blue_antiair.png");
            fUnitBmps[1, 5] = new Bitmap("./img/Blue_infantry.png");

            //ユニット記号BitMap
            fCharacterBmps[0] = new Bitmap("./img/Char_F.png");
            fCharacterBmps[1] = new Bitmap("./img/Char_A.png");
            fCharacterBmps[2] = new Bitmap("./img/Char_P.png");
            fCharacterBmps[3] = new Bitmap("./img/Char_U.png");
            fCharacterBmps[4] = new Bitmap("./img/Char_R.png");
            fCharacterBmps[5] = new Bitmap("./img/Char_I.png");

            //HPフォントBitMap
            fNumberBmps[0] = new Bitmap("./img/Number_0.png");
            fNumberBmps[1] = new Bitmap("./img/Number_1.png");
            fNumberBmps[2] = new Bitmap("./img/Number_2.png");
            fNumberBmps[3] = new Bitmap("./img/Number_3.png");
            fNumberBmps[4] = new Bitmap("./img/Number_4.png");
            fNumberBmps[5] = new Bitmap("./img/Number_5.png");
            fNumberBmps[6] = new Bitmap("./img/Number_6.png");
            fNumberBmps[7] = new Bitmap("./img/Number_7.png");
            fNumberBmps[8] = new Bitmap("./img/Number_8.png");
            fNumberBmps[9] = new Bitmap("./img/Number_9.png");
            fNumberBmps[10] = new Bitmap("./img/Number_10.png");

            // 地形表示用のBitMap
            fFieldBmps[0] = new Bitmap("./img/Field_noentry.png");
            fFieldBmps[1] = new Bitmap("./img/Field_plain.png");
            fFieldBmps[2] = new Bitmap("./img/Field_sea.png");
            fFieldBmps[3] = new Bitmap("./img/Field_wood.png");
            fFieldBmps[4] = new Bitmap("./img/Field_mountain.png");
            fFieldBmps[5] = new Bitmap("./img/Field_road.png");
			fFieldBmps[6] = new Bitmap("./img/Field_fortress.png");

            // 行動終了マークと，移動可能範囲などを表示するワク
            fEndMarkBMP = new Bitmap("./img/Mark_end.png");
            fRedWindowBMP = new Bitmap("./img/Mark_redselect.png");
            fBlueWindowBMP = new Bitmap("./img/Mark_blueselect.png");

            //フィルター用変数設定
            fIA_05Alpha = new System.Drawing.Imaging.ImageAttributes();
            fIA_Dark = new System.Drawing.Imaging.ImageAttributes();
            System.Drawing.Imaging.ColorMatrix fCM = new System.Drawing.Imaging.ColorMatrix();
            System.Drawing.Imaging.ColorMatrix fCM2 = new System.Drawing.Imaging.ColorMatrix();
            fCM.Matrix00 = 1;
            fCM.Matrix11 = 1;
            fCM.Matrix22 = 1;
            fCM.Matrix33 = 0.3F;
            fCM.Matrix44 = 1;
            fCM2.Matrix00 = 0.7F;
            fCM2.Matrix11 = 0.7F;
            fCM2.Matrix22 = 0.7F;
            fCM2.Matrix33 = 1;
            fCM2.Matrix44 = 1;
            fIA_05Alpha.SetColorMatrix(fCM);
            fIA_Dark.SetColorMatrix(fCM2);
            #endregion
        }

        public void setMap(Map map) {
            this.fMap = map;
            fCanBeAttacked = new bool[fMap.getXsize(), fMap.getYsize()];// 青いワクを表示するフラグの入った配列
            fRedWindowed = new bool[fMap.getXsize(), fMap.getYsize()]; // 赤いワクを表示するフラグの入った配列
            fAiSetString = new string[fMap.getXsize(), fMap.getYsize()];
        }

        // マップの情報からマスとユニットを描画
        public void drawMap() {
            if (this.fMap == null) {
                //Logger.showDialogMessage("DrawManagerにマップがセットされていないのに，描画しようとしてしています．．");
                return;
            }

            // 1マス描画する
            for (int x = 1; x < fMap.getXsize() - 1; x++) {
                for (int y = 1; y < fMap.getYsize() - 1; y++) {
                    drawCell(x, y);
                }
            }
        }

        // 1マスだけ描画したいときに使用する
        public void drawCell(int x, int y) {
            Unit u = fMap.getUnit(x, y);
            int fieldType = fMap.getFieldType(x, y);

            // 地形の描画
            fGraphics.DrawImage(fFieldBmps[fieldType], BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE);

            if (u != null) {
                // ユニットを描画する
                if (u.isActionFinished()) {
                    //行動終了ユニットの描画
                    fGraphics.DrawImage(fUnitBmps[u.getTeamColor(), u.getTypeOfUnit()], new Rectangle(BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE), 0, 0, BMP_SIZE, BMP_SIZE, GraphicsUnit.Pixel, fIA_Dark);
                    fGraphics.DrawImage(fEndMarkBMP, BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE);
                }
                else {
                    //行動前ユニットの描画
                    fGraphics.DrawImage(fUnitBmps[u.getTeamColor(), u.getTypeOfUnit()], BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE);
                }

                // ユニットのHPを表示する
                fGraphics.DrawImage(fNumberBmps[(int)u.getHP()], BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE);

                // ユニットの頭文字を表示する
                fGraphics.DrawImage(fCharacterBmps[u.getSpec().getUnitType()], BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE);
            }

            // ユニットの選択可能フラグが立っていれば赤ワクを描画する
            if (fCanBeAttacked != null && fCanBeAttacked[x, y] == true) {
                fGraphics.DrawImage(fRedWindowBMP, new Rectangle(BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE), 0, 0, BMP_SIZE, BMP_SIZE, GraphicsUnit.Pixel,fIA_05Alpha);
            }

            // 移動可能範囲に青ワクを表示する
            if (fRedWindowed != null && fRedWindowed[x, y] == true) {
                fGraphics.DrawImage(fBlueWindowBMP, new Rectangle(BMP_SIZE * (x - 1), BMP_SIZE * (y - 1), BMP_SIZE, BMP_SIZE), 0, 0, BMP_SIZE, BMP_SIZE, GraphicsUnit.Pixel, fIA_05Alpha);
            }

            if (fAiSetString!=null && fAiSetString[x, y] != null) {
                fGraphics.DrawString(fAiSetString[x, y], font, SystemBrushes.WindowText, BMP_SIZE * (x - 1)+1, BMP_SIZE * (y - 1)+1);
                fGraphics.DrawString(fAiSetString[x, y], font, brush, BMP_SIZE * (x - 1), BMP_SIZE * (y - 1));
            }
        }
        
        // マップの再描画
        public void reDrawMap(Map aMap) {
            this.fMap = aMap;
            drawMap();
            fForm.Refresh();
        }

        // マップの再描画、
        public void reDrawMap(Map map, bool[,] selectRange) {
            this.fMap = map;
			setMap(fMap);
            drawMap();
            fForm.Refresh();
        }

        // 引数x,yに指定された場所にユニットがいればその移動可能範囲を表示する
        public void showUnitMovablePos(bool[,] range, Map map) {
            // Logger.showArrayContents(aMap.selectRange);

            for (int x = 0; x < map.getXsize(); x++) {
                for (int y = 0; y < map.getYsize(); y++) {
                    if (range[x, y]) {
                        fRedWindowed[x, y] = true;// 移動可能範囲を青く描画するフラグをたてる
                    }
                }
            }

            reDrawMap(map);
            initBoolArray();
        }

        // 攻撃可能ユニットを表示する(選択可能にする）
        public void showAttackTargetUnits(Map map, List<Unit> canAttackUnits) {
            int x, y;
            for (int i = 0; i < canAttackUnits.Count; i++) {
                x = canAttackUnits[i].getXpos();
                y = canAttackUnits[i].getYpos();
                fCanBeAttacked[x, y] = true;// 攻撃可能場所として，赤ワクを描画するフラグをたてる
            }

            reDrawMap(map);
            initBoolArray();
        }

        // boolAryの初期化
        public void initBoolArray() {
            int x, y;
            for (x = 0; x < fMap.getXsize(); x++) {
                for (y = 0; y < fMap.getYsize(); y++) {
                    fCanBeAttacked[x, y] = false;
                    fRedWindowed[x, y] = false;
                }
            }
        }

        //フォームに表示しているマップの初期化
        public void clearMapImage() {
            fPictureBox.Image = new Bitmap(fPictureBox.Width, fPictureBox.Height);
            fGraphics = Graphics.FromImage(fPictureBox.Image);
        }

        public static void drawStringOnMap(string[,] value) {

            fAiSetString = value;
            fForm.Refresh();
        
        }
    }
}
