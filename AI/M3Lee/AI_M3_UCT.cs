using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using SimpleWars.AI;

namespace SimpleWars {

	/// <summary>
	/// M3Leeにて使われているサブAI
	/// 単純なUCTとしても動作します．
	/// </summary>
	class AI_M3_UCT : Player {
		//ふぃーるど
		int fTotalPlayout ;
		int fAllUnitPlayout = 3000; //多くすると強くなります．
        int fSearchDepth = 0;
        int fUserNum = 0;
		int fAIteam;

		//すとっぷうぉっち
		Stopwatch sw = new Stopwatch();

		// AIの表示名を返す（必須）
		public string getName() {
			return "AI_M3_UCT";
		}

		// パラメータ等の情報を返す（必須だが，空でも良い）
		public string showParameters() {
			return "";
		}

        //ユーザーログ変数　プロパティ
        public int userNumOfCombatLog {
            get { return fUserNum; }
            set { fUserNum = value; }
        }

		// 1ユニットの行動を決定する
        public Action makeAction(Map map, int teamColor, bool isTheBeggingOfTurn, bool isTheFirstTurnOfGame) {
			fAIteam = teamColor;

			if (isTheBeggingOfTurn) {
				//fAllPlayOutの設定
				fTotalPlayout = fAllUnitPlayout / map.getUnitsList(fAIteam, false, true, false).Count;
			}

            M3_Node aRootNode = new M3_Node();
            Action aBestAction = new Action();
            Map copyMap = new Map();
            aRootNode.fMap = map.createDeepClone();
			aRootNode.fPlayerColor = fAIteam;
			List<Action> ActionList = AiTools.getAllAttackActions(fAIteam, map);
			ActionList.AddRange(AiTools.getAllMoveActions(fAIteam, map));
			
			//ルートノードからUCTを行う
            aBestAction = UCT(aRootNode);

			return aBestAction;
		}


		#region UCT系めそっど

		//UCT本体
        private Action UCT(M3_Node aRootNode) {
            M3_Node[] aNodes = new M3_Node[fTotalPlayout];
			//この配列はプレイアウト時に使うもの．配列の長さが探索した深さに連動している．
            aNodes[0] = aRootNode;
			//PorogressiveWiding用
			List<Action> AttackActionList = AiTools.getAllAttackActions(aRootNode.fPlayerColor, aRootNode.fMap);
			List<Action> MoveActionList = AiTools.getAllMoveActions(aRootNode.fPlayerColor, aRootNode.fMap);

            //規定回数分プレイアウトを回す
			for (int i = 1; i <= fTotalPlayout+1; i++) {
				progressiveWiding(aRootNode, i, AttackActionList, MoveActionList);
				onePlayOut(aNodes);
			}

            //最大の勝率を出力
            double maxWinCnt = -10000;
            M3_Node maxWinNode = new M3_Node();
            foreach (M3_Node node in aRootNode.fChildNodes) {
                if (node.fWinCnt > maxWinCnt) {
					maxWinCnt = node.fWinCnt;
                    maxWinNode = node;
                }
            }

            Logger.addLogMessage("■採用手のパラメータ■" + "\r\n" +
                                 "WinCnt:" + maxWinNode.fWinCnt.ToString() + "\r\n" +
                                 "POCnt:" + maxWinNode.fCnt.ToString() + "\r\n" +
                                 "WinRate:" + (maxWinNode.fWinCnt / maxWinNode.fCnt).ToString() + "\r\n\r\n", fAIteam);

            return maxWinNode.fAction;
        }

		private void progressiveWiding(M3_Node aRootNode, int aTotalPlayOut, List<Action> aAtAcList, List<Action> aMoAcList) {
			//ルートノードにつける子ノードの数を設定
			int aNumOfChild = (int)(Math.Log(aTotalPlayOut / 40.0) / Math.Log(1.4) + 2);
			if (aTotalPlayOut >= 3000) aNumOfChild = (int)(Math.Log((aTotalPlayOut + 2000.0) / 45.0) / Math.Log(1.2) - 11);
			if (aNumOfChild < 1) aNumOfChild = 2;
			if (aTotalPlayOut == 1) aNumOfChild = 1;
			//copyMap用
			Map copyMap= new Map();
			Random aRand = RandomT.GetThreadRandom();
			int aTmp;
			//本来の子ノードとの差を計算して，子ノードの数を増やす
			if (aRootNode.fChildNodes.Count < aNumOfChild) {
				while(aNumOfChild!=aRootNode.fChildNodes.Count) {
					if (aAtAcList.Count != 0) {
						aTmp = aRand.Next(aAtAcList.Count);
						copyMap = aRootNode.fMap.createDeepClone();
						aRootNode.fChildNodes.Add(new M3_Node());
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fAction = aAtAcList[aTmp];
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fMap = copyMap;
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fMap.executeAction(aAtAcList[aTmp]);
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fPlayerColor = aRootNode.fPlayerColor;

						aAtAcList.RemoveAt(aTmp);
					} else {
						if (aMoAcList.Count == 0) break;
						aTmp = aRand.Next(aMoAcList.Count);
						copyMap = aRootNode.fMap.createDeepClone();
						aRootNode.fChildNodes.Add(new M3_Node());
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fAction = aMoAcList[aTmp];
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fMap = copyMap;
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fMap.executeAction(aMoAcList[aTmp]);
						aRootNode.fChildNodes[aRootNode.fChildNodes.Count - 1].fPlayerColor = aRootNode.fPlayerColor;

						aMoAcList.RemoveAt(aTmp);
					}
				}
			}
		}

		//一回のプレイアウト
		private void onePlayOut(M3_Node[] aNode) {
			int d = 0;
			//ここのaNodeはあくまでも深く探索しているだけ（縦型）
			while (aNode[d].fChildNodes.Count() > 0) {
				//UCBを使って次に探索する枝を選定
				aNode[d + 1] = decideByUCB(aNode[d]);
				d++;
			}
			//基準に見合う枝が存在していないのならば一度終局までシミュレーションを行う
			double result = getSimuResult(aNode[d]);
			if(result !=3) aNode[d].fWinCnt += result;

            if (fSearchDepth<d) {
                fSearchDepth = d;
            }
			upDateData(aNode, result,d);
		}

		//UCBによるノード選択
		private M3_Node decideByUCB(M3_Node aP_node) {
			int aSum = 0;
			double aMax_UCB = -100;
			M3_Node returnNode = new M3_Node();
			foreach (M3_Node node in aP_node.fChildNodes) {
				aSum += node.fCnt;
			}
			foreach (M3_Node node in aP_node.fChildNodes) {
				if (node.fCnt == 0) node.fUCB = 1000;
				else {
					if (aP_node.fPlayerColor == node.fPlayerColor) {
						node.fUCB = node.fWinCnt / node.fCnt + Math.Sqrt(2 * Math.Log(aSum) / node.fCnt);
					} else {
						node.fUCB = (1 - node.fWinCnt / node.fCnt) + Math.Sqrt(2 * Math.Log(aSum) / node.fCnt);
					}
				}

				if (aMax_UCB < node.fUCB ) {
					returnNode = node;
					aMax_UCB = node.fUCB;
				}
			}
			return returnNode;
		}

		//ランダムシミュレーションの結果を出す
        //シミュレーションするノードに対して子ノードをつける
        //勝敗は現時点のノードから見てのものとなる
        private double getSimuResult(M3_Node aNode) {
            Map copyMap = aNode.fMap.createDeepClone();
			if (evaluateMAP(aNode.fMap, 0) != -1) {
				aNode.fCnt++;
				return evaluateMAP(aNode.fMap, aNode.fPlayerColor);
			}

			List<Action> MoveActionList;
			List<Action> AttackActionList;
			int nexTeamCol;

			//候補手を列挙する前に次の一手でチームカラーが変わっていないかチェックする
			if (copyMap.getUnitsList(aNode.fPlayerColor, false, true, false).Count == 0) {//変わっている場合
				copyMap.resetActionFinish();
				copyMap.incTurnCount();
				nexTeamCol = (aNode.fPlayerColor + 1) % 2;
				MoveActionList = AiTools.getAllMoveActions((aNode.fPlayerColor + 1) % 2, copyMap);
				AttackActionList = AiTools.getAllAttackActions((aNode.fPlayerColor + 1) % 2, copyMap);
			} else {//変わっていない場合
				nexTeamCol = aNode.fPlayerColor;
				MoveActionList = AiTools.getAllMoveActions(aNode.fPlayerColor, copyMap);
				AttackActionList = AiTools.getAllAttackActions(aNode.fPlayerColor, copyMap);
			}

            //一度もシミュレートされていないノード場合に子ノードの候補を列挙する
            if (aNode.fChildNodes.Count() == 0) {
				//枝をつける
                for (int i = 0; i < AttackActionList.Count; i++) {
                    aNode.fChildNodes.Add(new M3_Node());
                    aNode.fChildNodes[i].fAction = AttackActionList[i];
					aNode.fChildNodes[i].fMap = copyMap.createDeepClone();
					aNode.fChildNodes[i].fMap.executeAction(AttackActionList[i]);
					aNode.fChildNodes[i].fPlayerColor = nexTeamCol;
                }
                for (int i = AttackActionList.Count; i < MoveActionList.Count; i++) {
                    aNode.fChildNodes.Add(new M3_Node());
                    aNode.fChildNodes[i].fAction = MoveActionList[i];
					aNode.fChildNodes[i].fMap = copyMap.createDeepClone();
					aNode.fChildNodes[i].fMap.executeAction(MoveActionList[i]);
					aNode.fChildNodes[i].fPlayerColor = nexTeamCol;
                }
				
            }

            Random aRand = RandomT.GetThreadRandom();
            int tempNodeNum = aRand.Next(aNode.fChildNodes.Count);
			int nowColor;
            copyMap = aNode.fChildNodes[tempNodeNum].fMap.createDeepClone();
			
			//一回行動した後に色が変わっているかチェックする．
			if (copyMap.getUnitsList(aNode.fPlayerColor, false, true, false).Count == 0) {
				copyMap.resetActionFinish();
				copyMap.incTurnCount();
				nowColor = (aNode.fChildNodes[tempNodeNum].fPlayerColor+1)%2;
			}
			nowColor = aNode.fChildNodes[tempNodeNum].fPlayerColor;


			//ランダムに選んだ次局面を終局まで進める
            for (int k = copyMap.getTurnCount(); k < copyMap.getTurnLimit() && evaluateMAP(copyMap,0) == -1; k++) {
				doOneTurnAction(copyMap, nowColor++ % 2);
            }

            //ランダムシミュレーションに使用した子ノードの勝利数更新
            aNode.fChildNodes[tempNodeNum].fCnt++;
			aNode.fChildNodes[tempNodeNum].fWinCnt = evaluateMAP(copyMap, aNode.fChildNodes[tempNodeNum].fPlayerColor);

            return evaluateMAP(copyMap, aNode.fPlayerColor);
        }

		//ランダムシミュレーションの結果を行進する
		private void upDateData(M3_Node[] aNode, double result,int depth) {


			for (int d = depth - 1; d >= 0; d--) {
				aNode[d].fCnt++;
				if (result == 3) continue;
				else {
					if (aNode[d].fPlayerColor != aNode[d + 1].fPlayerColor) {
						result = 1 - result;
					}
					aNode[d].fWinCnt += result;
				}
			}
		}

		#endregion

		#region 他のモンテカルロと共通

		//完全ランダムなワンターンアクション
		private List<Action> makeOneTurnRandomAction(Map aMap, int teamColor) {
			//ランダム行動の作成
			List<Action> aActions = new List<Action>();
			Action aOneAction = new Action();
			List<Action> fullActions = new List<Action>();
            Random fRand = RandomT.GetThreadRandom();

			while ((fullActions = AiTools.getAllMoveActions(teamColor, aMap)).Count != 0) {
				fullActions.AddRange(AiTools.getAllAttackActions(teamColor, aMap));
				int tmp = fRand.Next(fullActions.Count);
				aOneAction = fullActions[tmp];
				aMap.executeAction(fullActions[tmp]);
				aActions.Add(aOneAction);
			}

            aMap.resetActionFinish();
            aMap.incTurnCount();
			return aActions;
		}

        //攻撃優先なランダムワンターンアクション
        private List<Action> makeOneTurnAttackAction(Map aMap ,int teamColor) {
            //ランダム行動の作成
            List<Action> aActions = new List<Action>();
            Action aOneAction = new Action();
            List<Action> fullActions = new List<Action>();
            Random fRand = RandomT.GetThreadRandom();

            while ((fullActions = AiTools.getAllAttackActions(teamColor, aMap)).Count != 0) {
                int tmp = fRand.Next(fullActions.Count);
                aOneAction = fullActions[tmp];
                aMap.executeAction(fullActions[tmp]);
                aActions.Add(aOneAction);
            }

            while ((fullActions = AiTools.getAllMoveActions(teamColor, aMap)).Count != 0) {
                int tmp = fRand.Next(fullActions.Count);
                aOneAction = fullActions[tmp];
                aMap.executeAction(fullActions[tmp]);
                aActions.Add(aOneAction);
            }

            aMap.resetActionFinish();
            aMap.incTurnCount();
            return aActions;
        }

        //攻撃よりな選択をする（本当の意味で）ランダムワンターンアクション
        private List<Action> makeOneTurnAttackAndMoveAction(Map aMap, int teamColor) {
            //ランダム行動の作成
            List<Action> aActions = new List<Action>();
            Action aOneAction = new Action();
            List<Action> fullActions = new List<Action>();
            Random fRand = RandomT.GetThreadRandom();

            while (AiTools.getAllMoveActions(teamColor, aMap).Count != 0 || AiTools.getAllAttackActions(teamColor, aMap).Count != 0) {
                if (fRand.Next(10) > 2 && AiTools.getAllAttackActions(teamColor, aMap).Count != 0) {
                    fullActions = AiTools.getAllAttackActions(teamColor, aMap);
                    int tmp = fRand.Next(fullActions.Count);
                    aOneAction = fullActions[tmp];
                    aMap.executeAction(fullActions[tmp]);
                    aActions.Add(aOneAction);
                }
                else {
                    fullActions = AiTools.getAllMoveActions(teamColor, aMap);
                    int tmp = fRand.Next(fullActions.Count);
                    aOneAction = fullActions[tmp];
                    aMap.executeAction(fullActions[tmp]);
                    aActions.Add(aOneAction);
                }
            }

            aMap.resetActionFinish();
            aMap.incTurnCount();
            return aActions;
        }

		//完全ランダムなワンターンアクション
		private void doOneTurnRandomAction(Map aMap, int teamColor) {
			//ランダム行動の作成
			while (doRandomAction(aMap, teamColor)) ;
            aMap.resetActionFinish();
            aMap.incTurnCount();
		}

		//全行動からランダムに行動を選択
		private bool doRandomAction(Map aMap, int teamColor) {
			List<Action> tmp = AiTools.getAllAttackActions(teamColor, aMap);
			List<Action> fullActions = AiTools.getAllMoveActions(teamColor, aMap);
            Random fRand = RandomT.GetThreadRandom();
			fullActions.AddRange(tmp);

			if (fullActions.Count == 0) return false;

			int temp = fRand.Next(fullActions.Count);
			aMap.executeAction(fullActions[temp]);
			return true;
		}

		//全ての行動をランダムに決定する(ただし攻撃できるユニットは絶対する)
		private void doOneTurnAction(Map aMap, int teamColor) {
			//攻撃行動の作成
			while (doRandomAtkAction(aMap, teamColor)) ;
			//移動行動の作成
			while (doRandomMoveAction(aMap, teamColor)) ;
            aMap.resetActionFinish();
            aMap.incTurnCount();
		}

		//全攻撃行動からランダムに行動選択
		private bool doRandomAtkAction(Map aMap, int teamColor) {
			List<Action> atkActions = AiTools.getAllAttackActions(teamColor, aMap);
            Random fRand = RandomT.GetThreadRandom();
			if (atkActions.Count == 0) return false;
			int tmp = fRand.Next(atkActions.Count);
			aMap.executeAction(atkActions[tmp]);
			return true;
		}

		//全移動行動からランダムに行動選択
		private bool doRandomMoveAction(Map aMap, int teamColor) {
			List<Action> moveActions = AiTools.getAllMoveActions(teamColor, aMap);
            Random fRand = RandomT.GetThreadRandom();
			if (moveActions.Count == 0) return false;
			int tmp = fRand.Next(moveActions.Count);
			aMap.executeAction(moveActions[tmp]);
			return true;
		}

		//TeamColorを反転する関数
		//なんとなく作った　
		//あった　ような　気がしなくもない　ような　気がしなくもない
		private int returnTeamColor(int teamColor) {
			if (teamColor == 0) return 1;
			else return 0;
		}

		#endregion

		/// <summary>
		/// 評価関数 -1:試合中　1:勝ち　0:負け 0.5:引き分け
		/// 入力されたマップの操作チームからみた勝敗
		/// </summary>
		/// <param name="map">マップ</param>
		/// <param name="teamColor">チームカラー</param>
		/// <returns></returns>
		private double evaluateMAP(Map map, int TeamColor) {

			int EnemyTeam = (TeamColor + 1) % 2;

			if (map.getTurnLimit() == map.getTurnCount()) {
				if (map.getDrawHPThreshold() > Math.Abs(map.getNumOfAliveColorUnits(TeamColor) - map.getNumOfAliveColorUnits(EnemyTeam))) return 0.5; //本来ならここは0.5
				else {
					if (map.getNumOfAliveColorUnits(TeamColor) - map.getNumOfAliveColorUnits(EnemyTeam) > map.getDrawHPThreshold())
						return 1;
					else return 0;
				}
			}

			if (map.getNumOfAliveColorUnits(Consts.BLUE_TEAM) == 0 && map.getNumOfAliveColorUnits(Consts.RED_TEAM) == 0) return 0;
			if (map.getNumOfAliveColorUnits(EnemyTeam) == 0) 
				return 1;
			if (map.getNumOfAliveColorUnits(TeamColor) == 0) 
				return 0;

			return -1;
		}
	}
}
