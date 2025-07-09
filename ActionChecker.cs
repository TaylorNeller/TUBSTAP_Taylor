using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars {
    // その行動が可能かどうかを判定するチェックする
    // 主にシステム側で使うもので，ユーザが中身を知る必要はない
    // version 0.104 より，ユーザの利用も考え，dialogを出さないモードを追加．
    class ActionChecker {
        // どうして合法手でないのか．
        public static string lastError = "";

        // その行動が可能かどうかを判定するチェックする．従来版で，ダイアログを出す．
        public static bool isTheActionLegalMove(Action act, Map map) {
            return isTheActionLegalMove(act, map, true);
        }

        // その行動が可能かどうかを判定するチェックする．ダイアログを出さず，チェックだけできる．
        public static bool isTheActionLegalMove_Silent(Action act, Map map) {
            return isTheActionLegalMove(act, map, false);
        }

        // メッセージ．
        private static void log(bool showDialog, string message) {
            lastError = message;
            if (showDialog == true) {
                Logger.showDialogMessage(message);
            }
        }

        // 本体．ダイアログの有無を設定できる．        
        public static bool isTheActionLegalMove(Action act, Map map, bool showDialog) {
            lastError = "";

            // 行動タイプが選択されていない場合
            if (act.actionType == -1) {
                log(showDialog, "ActionChecker: アクションタイプが設定されていません．");
                return false;
            }

            // ターンエンドはＯＫ
            if (act.actionType == Action.ACTIONTYPE_TURNEND || act.actionType == Action.ACTIONTYPE_SURRENDER) {
                return true;
            }

            Unit[] units = map.getUnits();//マップユニット配列

            // 操作するユニットを選択しているかチェック
            if (act.operationUnitId < 0 && act.operationUnitId > units.Length - 1) {
                log(showDialog, "ActionChecker: 操作ユニットのIDが範囲外です");
                return false;
            }

            Unit opUnit = map.getUnit(act.operationUnitId);// 操作したユニット

            // 操作ユニットの有無を確認
            if (units[act.operationUnitId] == null) {
                log(showDialog, "ActionChecker: 存在しないユニットを操作しようとしています．");
                return false;
            }

            // 敵ユニットを移動させていないかのチェック
            if (units[act.operationUnitId].getTeamColor() != act.teamColor) {
                log(showDialog, "ActionChecker: 敵ユニットを操作しようとしています．");
                return false; // version 0.104で追加．バグ．
            }

            // 行動済みユニットを動かしていないかのチェック
            if (units[act.operationUnitId].isActionFinished() == true) {
                log(showDialog, "ActionChecker: 行動済みユニットを操作しようとしてます．");
                return false; // version 0.104で追加．バグ．
            }

            // 移動先をチェック
            Unit destU = map.getUnit(act.destinationXpos, act.destinationYpos);
            if (destU != null && destU.getID() != opUnit.getID()) {
                log(showDialog, "ActionChecker: 移動先には既に別ユニットがあります．"); 
                return false;
            } else if (act.destinationXpos < 1 || act.destinationXpos > map.getXsize() - 2) {
                log(showDialog, "ActionChecker: 移動先Ｘ座標がマップ範囲外です．"); 
                return false;
            } else if (act.destinationYpos < 1 || act.destinationYpos > map.getYsize() - 2) {
                log(showDialog, "ActionChecker: 移動先Ｙ座標がマップ範囲外です．"); 
                return false;
            }

            bool[,] movableCell = RangeController.getReachableCellsMatrix(opUnit, map);// 移動可能範囲のmatrix

            if (!movableCell[opUnit.getXpos(), opUnit.getYpos()]) {
                log(showDialog, "ActionChecker: 移動可能範囲外にユニットを移動させようとしています．"); 
                return false;
            }

            if (act.actionType == Action.ACTIONTYPE_MOVEANDATTACK) {
                // 攻撃相手をチェック
                if (act.targetUnitId == -1) {
                    log(showDialog, "ActionChecker: 攻撃対象ユニットが選択されていません"); 
                    return false;
                } else if (units[act.targetUnitId] == null) {
                    log(showDialog, "ActionChecker: 存在しないユニットを攻撃対象にしようとしています．"); 
                    return false;
                } else if (opUnit.getTeamColor() == units[act.targetUnitId].getTeamColor()) {
                    log(showDialog, "ActionChecker: 同じチームのユニットを攻撃しようとしています．"); 
                    return false;
                }

                bool[,] attackAbleCell = RangeController.getAttackableCellsMatrix(opUnit, map);// 攻撃可能範囲
                // 射程外の敵を攻撃しようとしていた場合
                if (!attackAbleCell[units[act.targetUnitId].getXpos(), units[act.targetUnitId].getYpos()]) {
                    log(showDialog, "ActionChecker: 攻撃範囲外の敵を攻撃しようとしています");
                    return false;
                }
            }

            return true;
        }

        // operationUnitがtargetUnitに攻撃可能かどうか．ダメージ効果0の場合にはfalseが返る
        private static bool isEffective(Unit operationUnit, Unit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        }
    }
}
