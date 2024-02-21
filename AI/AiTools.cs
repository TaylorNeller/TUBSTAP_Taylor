using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleWars {
    /// <summary>
    /// システムは使わない，思考ルーチンを作るのにあるとうれしいような関数たちです．
	/// 拡充予定です
    /// </summary>
    class AiTools {

        // 攻撃行動可能な自ユニット・敵ユニット・攻撃位置のリストを ActionのListとして取得する
        public static List<Action> getAllAttackActions(int teamcolor, Map map) {
            List<Action> atkActions = new List<Action>();

            // 行動可能な自ユニットリスト
            List<Unit> myMovableUnits = map.getUnitsList(teamcolor, false, true, false);

            foreach (Unit myUnit in myMovableUnits) {
                // movableUnitsの攻撃行動リスト
                List<Action> atkActionsOfOne = RangeController.getAttackActionList(myUnit, map);

                // 全体に含める
                atkActions.AddRange(atkActionsOfOne);
            }

            return atkActions;
        }

		/// <summary>
		/// 可能な行動全てを取得する
		/// </summary>
        public static List<Action> getAllMoveActions(int teamcolor, Map map) {
            List<Action> moveActions = new List<Action>();

            // 行動可能な自ユニットリスト
            List<Unit> myMovableUnits = map.getUnitsList(teamcolor, false, true, false);

            //行動可能なユニットの移動可能箇所をリストにまとめる
            foreach (Unit myUnit in myMovableUnits) {
                bool[,] reachable = RangeController.getReachableCellsMatrix(myUnit, map);
                for (int i = 0; i < reachable.GetLength(0); i++) {
                    for (int j = 0; j < reachable.GetLength(1); j++) {
                        if (reachable[i, j]) {
                            moveActions.Add(Action.createMoveOnlyAction(myUnit, i, j));
                        }
                    }
                }
            }

            return moveActions;
        }

        // operationUnitがtargetUnitに攻撃可能かどうか．ダメージ効果0の場合にはfalseが返る
        public static bool isEffective(Unit operationUnit, Unit targetUnit) {
            if (operationUnit.getSpec().getUnitAtkPower(targetUnit.getTypeOfUnit()) != 0) { return true; }
            return false;
        }

        /// <summary>
        /// 現在の状態における全合法手を生成する
        /// </summary>
        public static List<Action> getAllActions(int teamColor, Map map) {
            List<Action> allActions = new List<Action>();

            allActions.AddRange(getAllAttackActions(teamColor, map));
            allActions.AddRange(getAllMoveActions(teamColor, map));

            return allActions;
        }


		//動いていないユニットの数を返す
		public static int getNumOfDontMovedUnit(int teamColor, Map map) {
			return map.getUnitsList(teamColor, false, true, false).Count;
		}

		//指定した危険度以下の行動を返す
		//危険度マップに関してはcalcRiskMapを参照
		public static List<Action> getDeffensiveActions(int teamcolor, Map map, int[,] riskOfMap, int LowThreshold, int HighThreshold) {
			List<Action> moveActions = new List<Action>();

			// 行動可能な自ユニットリスト
			List<Unit> myMovableUnits = map.getUnitsList(teamcolor, false, true, false);

			//行動可能なユニットの移動可能箇所をリストにまとめる
			foreach (Unit myUnit in myMovableUnits) {
				bool[,] reachable = RangeController.getReachableCellsMatrix(myUnit, map);
				for (int i = 1; i < reachable.GetLength(0); i++) {
					for (int j = 1; j < reachable.GetLength(1); j++) {
						if (reachable[i, j] && (riskOfMap[i, j] <= HighThreshold && riskOfMap[i, j] >= LowThreshold)) {
							moveActions.Add(Action.createMoveOnlyAction(myUnit, i, j));
						}
					}
				}
			}

			return moveActions;
		}

		/// <summary>
		/// 敵ユニットからの利き（攻撃範囲）に入ってる場所に，危険度を書き込む
		/// 危険度は敵何ユニットから攻撃される可能性があるかを示している，高いほどヤバイ（最大で4方向+敵の自走砲からの攻撃数となる）
		/// ※注意：計算方法がユニットの行動順序を考慮しない為，若干間違ってるときがある．　
		/// </summary>
		/// <param name="teamColor"></param>
		/// <param name="map"></param>
		/// <returns></returns>
		public static int[,] calcRiskMap(int teamColor, Map map) {
			int[] dx = new int[4] { +1, 0, -1, 0 };
			int[] dy = new int[4] { 0, -1, 0, +1 };

			List<Unit> enUnits = map.getUnitsList(teamColor, false, false, true);

			bool[, , ,] ifMap = new bool[map.getXsize(), map.getYsize(), enUnits.Count(), 5];
			int[,] riskOfMap = new int[map.getXsize(), map.getYsize()];

			int i = 0;

			foreach (Unit u in enUnits) {
				//直接攻撃タイプのユニットの場合は，
				if (u.getSpec().isDirectAttackType() == true) {
					bool[,] reachAbleCells = RangeController.getReachableCellsMatrix(u, map);

					for (int x = 1; x < map.getXsize() - 1; x++) {
						for (int y = 1; y < map.getYsize() - 1; y++) {
							//対応するセルの上下左右の場所をチェックする．
							for (int r = 0; r < 4; r++) {
								//上↓左右どこかが移動範囲ならば，(x,y)は攻撃可能な場所
								if (reachAbleCells[x + dx[r], y + dy[r]] == true) {
									ifMap[x, y, i, r] = true;
								}
							}
						}
					}
					//間接攻撃ユニットの場合
				} else {
					bool[,] attackAbleCells = RangeController.getAttackableCellsMatrix(u, map);

					for (int x = 1; x < map.getXsize() - 1; x++) {
						for (int y = 1; y < map.getYsize() - 1; y++) {
							//(x,y)が攻撃可能な場所の場合
							if (attackAbleCells[x, y] == true) {
								ifMap[x, y, i, 4] = true;
							}
						}
					}
				}
				i++;
			}

			for (int x = 1; x < map.getXsize() - 1; x++) {
				for (int y = 1; y < map.getYsize() - 1; y++) {
					//初期化
					bool[] TorF_Unit = new bool[enUnits.Count];
					bool[] TorF_Direction = new bool[4];
					int RiskOfUnit = 0;
					int RiskOfLine = 0;
					int RiskOfIndirection = 0;

					//対応するセルを攻撃可能なユニット数をチェックする．
					for (int k = 0; k < enUnits.Count; k++) {
						TorF_Unit[k] = ifMap[x, y, k, 0] || ifMap[x, y, k, 1] || ifMap[x, y, k, 2] || ifMap[x, y, k, 3];
						if (TorF_Unit[k]) RiskOfUnit++;
						if (ifMap[x, y, k, 4]) RiskOfIndirection++;
					}

					//対応するセルを攻撃可能な方向をチェックする．
					for (int r = 0; r < 4; r++) {
						for (int k = 0; k < enUnits.Count; k++) {
							TorF_Direction[r] |= ifMap[x, y, k, r];
						}
						if (TorF_Direction[r]) RiskOfLine++;
					}

					if (RiskOfLine > RiskOfUnit) {
						riskOfMap[x, y] = RiskOfUnit + RiskOfIndirection;
					} else {
						riskOfMap[x, y] = RiskOfLine + RiskOfIndirection;
					}

				}
			}

			return riskOfMap;
		}


		/// <summary>
		/// 敵ユニットからの利き（攻撃範囲）に入ってる場所に，危険度配列に書き込む
		/// </summary>
		/// <param name="teamColor"></param>
		/// <param name="map"></param>
		/// <returns></returns>
		public static int[,] calcIFMap(int teamColor, Map map) {
			int[] dx = new int[4] { +1, 0, -1, 0 };
			int[] dy = new int[4] { 0, -1, 0, +1 };

			int[,] ifMap = new int[map.getXsize(), map.getYsize()];
			List<Unit> enUnits = map.getUnitsList(teamColor, false, false, true);

			foreach (Unit u in enUnits) {
				//直接攻撃タイプのユニットの場合は，
				if (u.getSpec().isDirectAttackType() == true) {
					bool[,] reachAbleCells = RangeController.getReachableCellsMatrix(u, map);

					for (int x = 1; x < map.getXsize() - 1; x++) {
						for (int y = 1; y < map.getYsize() - 1; y++) {
							//対応するセルの上下左右の場所をチェックする．
							for (int r = 0; r < 4; r++) {
								//上↓左右どこかが移動範囲ならば，(x,y)は攻撃可能な場所
								if (reachAbleCells[x + dx[r], y + dy[r]] == true) {
									ifMap[x, y] += 1;
								}
							}
						}
					}
					//間接攻撃ユニットの場合
				} else {
					bool[,] attackAbleCells = RangeController.getAttackableCellsMatrix(u, map);

					for (int x = 1; x < map.getXsize() - 1; x++) {
						for (int y = 1; y < map.getYsize() - 1; y++) {
							//(x,y)が攻撃可能な場所の場合
							if (attackAbleCells[x, y] == true) {
								ifMap[x, y] += 1;

							}
						}
					}
				}
			}
			return ifMap;
		}
    }
}
