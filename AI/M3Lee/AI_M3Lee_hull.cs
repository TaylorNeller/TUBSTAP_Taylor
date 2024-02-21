using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace SimpleWars {
	/// <summary>
	/// 2016年GAT杯の準優勝AIです．
	/// </summary>
	
 
	//専用乱数器
	//スレッド毎に異なるseed値を持たせる為に使用
	public static class RandomT {
		private static int seed = Environment.TickCount;

		private static ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() =>
			new Random(Interlocked.Increment(ref seed))
		);

		public static Random GetThreadRandom() {
			return randomWrapper.Value;
		}
	}

	/// <summary>
	/// M3Leeの本体
	/// DLMC（防御担当）とUCT（攻撃担当）の2種類が動作し，良い手を作ったほうを採用する手法です．
	/// AIを5つ搭載しT35に生まれ変わる．
	/// </summary>
    class AI_M3Lee_hull : Player {
        //フィールド
        //DLMC
        int fAIteam;
        const int fSampleAction = 150; //チェックする行動の数を表しています．多いほど強くなります．
        int fPlayOut = 50;             //行動の評価回数を表しています．多いほど強くなりますが，最大でも100くらいで性能は頭打ちになります．
        int fDepth = 2;				   //評価する際の打ち切りの深さを表します．多くしても強くはなりません．
        List<Action> fTthisTurnActions = new List<Action>();
        //UCT
		//UCT部分のパラメータに関してはM3_UCTで直接操作する必要あり
		AI_M3_UCT UCT_PW = new AI_M3_UCT();
        Map c_map_for_UCT;
        List<Action> fUCTActions = new List<Action>();
		//DLMC
		Map c_map_for_DLMC;
		List<Action> fDLMCActions = new List<Action>();

        //StopWatch
        Stopwatch sw = new Stopwatch();

        // AIの表示名を返す（必須）
        public string getName() {
            return "M3Lee";
        }

        // パラメータ等の情報を返す
        // 空リターン
        public string showParameters() {
            return "";
        }

		//ユーザーログ変数　プロパティ
		public int userNumOfCombatLog {
			get { return 0; }
			set { }
		}

        // 1ユニットの行動を決定する
        public Action makeAction(Map map, int teamColor, bool isTheBeggingOfTurn, bool isTheFirstTurnOfGame) {
            fAIteam = teamColor;

            //ターンの最初に全ての行動を決定する
			if (isTheBeggingOfTurn) {
				//すとっぷうぉっち開始
				sw.Reset();
				sw.Start();
				Logger.addLogMessage("思考開始\r\n", teamColor);

				//初期化
				c_map_for_UCT = map.createDeepClone();
				c_map_for_DLMC = map.createDeepClone();
				fUCTActions = new List<Action>();

				//UCT_PWによる順列生成
				while (true) {
					Action UCT_Action = new Action();
					UCT_Action = UCT_PW.makeAction(c_map_for_UCT, teamColor, isTheBeggingOfTurn, isTheFirstTurnOfGame);
					fUCTActions.Add(UCT_Action);
					c_map_for_UCT.executeAction(UCT_Action);
					if (c_map_for_UCT.getUnitsList(teamColor, false, true, false).Count == 0) break;
					isTheBeggingOfTurn = false;
				}
				c_map_for_UCT.resetActionFinish();
				c_map_for_UCT.incTurnCount();

				//UCTの出した手を評価
				Parallel.For(0, fPlayOut, i => {
					Map copyCopyMap = c_map_for_UCT.createDeepClone();
					int phase = fAIteam;
					for (int k = copyCopyMap.getTurnCount(); k < copyCopyMap.getTurnLimit() && evaluateMAP_binary(copyCopyMap, 0) == -1; k++) {
						doRandomAttackPriorityActionOfOneTurn(copyCopyMap, ++phase % 2);
					}
					fUCTActions[0].X_actionEvaluationValue += evaluateMAP_binary(copyCopyMap, fAIteam);
				});


				//DLMCで全行動を作成
				fDLMCActions = makeAllAction(map, teamColor);
				for (int i = 0; i < fDLMCActions.Count; i++) {
					c_map_for_DLMC.executeAction(fDLMCActions[i]);
				}
				c_map_for_DLMC.resetActionFinish();
				c_map_for_DLMC.incTurnCount();
				fDLMCActions[0].X_actionEvaluationValue = 0;

				Parallel.For(0, fPlayOut, i => {
					Map copyCopyMap = c_map_for_DLMC.createDeepClone();
					int phase = fAIteam;
					for (int k = copyCopyMap.getTurnCount(); k < copyCopyMap.getTurnLimit() && evaluateMAP_binary(copyCopyMap, 0) == -1; k++) {
						doRandomAttackPriorityActionOfOneTurn(copyCopyMap, ++phase % 2);
					}
					fDLMCActions[0].X_actionEvaluationValue += evaluateMAP_binary(copyCopyMap, fAIteam);
				});

				if (fDLMCActions[0].X_actionEvaluationValue > fUCTActions[0].X_actionEvaluationValue) {
					fTthisTurnActions = fDLMCActions;
					Logger.addLogMessage("DLMC採用\r\n", fAIteam);
				} else {
					fTthisTurnActions = fUCTActions;
					Logger.addLogMessage("UCT採用\r\n", fAIteam);
				}

				//すとっぷうぉっち終了
				sw.Stop();
				Logger.addLogMessage("思考終了\r\n", fAIteam);
				Logger.addLogMessage("思考時間：" + sw.Elapsed.ToString() + "秒\r\n", fAIteam);


				//1手目の行動出力
				Action temp = fTthisTurnActions[0];
				fTthisTurnActions.Remove(fTthisTurnActions[0]);
				Logger.addLogMessage(temp.toString(), teamColor);
				return temp;
			} else {
				//2手目以降の行動出力
				Action temp = fTthisTurnActions[0];
				fTthisTurnActions.Remove(fTthisTurnActions[0]);
				Logger.addLogMessage(temp.toString(), teamColor);
				return temp;
			}
        }


		//1ターンの行動を全て決定する
		private List<Action> makeAllAction(Map aMap, int teamColor) {
			int OriginalColor = teamColor;
			//BestScoreを最低値に変更
			double aBestScore = -100000;
			List<List<Action>> aTempActions = new List<List<Action>>();
			List<Action> aBestActions = new List<Action>();

			//評価すべき行動の組み合わせを生成，その後行動の組み合わせをモンテカルロ法により評価
			for (int j = 0; j < fSampleAction; j++) {
				//ここで貰ったマップは原本なのでコピー
				Map copyMap = aMap.createDeepClone();

				//現状態から次状態への候補手を適当につくる
				aTempActions.Add(makeSampleOneTurnAction(copyMap, fAIteam));

				//勝利確定の場合はそのまま返す
				if (WinOrLose(copyMap)) {
					sw.Stop();
					//Logger.addLogMessage("思考時間：" + sw.Elapsed.ToString() + "秒\r\n", teamColor);
					return aTempActions[j];
				}

				//三手先まで進め，評価関数にかける
				//ランダムシミュレーション部分をパラレル処理
				//攻撃優先のシミュレーションを7割，完全ランダムなシミュレーションを3割実行
				Parallel.For(0, fPlayOut, i => {
					Map copyCopyMap = copyMap.createDeepClone();
					int phase = fAIteam;
					for (int k = 0; k < fDepth; k++) {
						doRandomAttackPriorityActionOfOneTurn(copyCopyMap, ++phase % 2);
					}
					aTempActions[j][0].X_actionEvaluationValue += evaluateMAP(copyCopyMap, fAIteam);
				});
			}
			sw.Stop();
			//Logger.addLogMessage("思考時間：" + sw.Elapsed.ToString() + "秒\r\n", teamColor);

			foreach (List<Action> OneTurnActions in aTempActions) {
				if (OneTurnActions[0].X_actionEvaluationValue > aBestScore) {
					aBestScore = OneTurnActions[0].X_actionEvaluationValue;
					aBestActions = OneTurnActions;
					//Logger.addLogMessage(aBestScore.ToString() + "\r\n", teamColor);
				}
			}
			return aBestActions;
		}



		//評価するサンプル行動の組み合わせを生成（1ターン内全ての行動）
		//防御よりの行動を選択する
		private List<Action> makeSampleOneTurnAction(Map aMap, int teamColor) {
			//ランダム行動の作成
			List<Action> aActions = new List<Action>();
			Action aOneAction = new Action();
			List<Action> fullActions = new List<Action>();
			// 乱数生成器
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

		//引数のマップに対してランダムな行動(1ターン分)を適用
		private void doRandomActionOfOneTurn(Map aMap, int teamColor) {
			//ランダム行動の作成
			while (doRandomActionOfOneUnit(aMap, teamColor)) ;
			aMap.resetActionFinish();
			aMap.incTurnCount();
		}

		//引数のマップに対してランダムな行動(1ユニット分)を適用
		private bool doRandomActionOfOneUnit(Map aMap, int teamColor) {
			List<Action> tmp = AiTools.getAllAttackActions(teamColor, aMap);
			List<Action> fullActions = AiTools.getAllMoveActions(teamColor, aMap);
			// 乱数生成器
			Random fRand = RandomT.GetThreadRandom();

			fullActions.AddRange(tmp);

			if (fullActions.Count == 0) return false;

			int temp = fRand.Next(fullActions.Count);
			aMap.executeAction(fullActions[temp]);
			return true;
		}


		//引数のマップに対してランダムに攻撃優先な行動(1ターン分)を適用
		private void doRandomAttackPriorityActionOfOneTurn(Map aMap, int teamColor) {
			//攻撃行動の作成
			while (doRandomAtkActionOfOneUnit(aMap, teamColor)) ;
			//移動行動の作成
			while (doRandomMoveActionOfOneUnit(aMap, teamColor)) ;
			aMap.resetActionFinish();
			aMap.incTurnCount();
		}

		//引数のマップに対してランダムに攻撃行動(1ユニット分)を適用
		private bool doRandomAtkActionOfOneUnit(Map aMap, int teamColor) {
			List<Action> atkActions = AiTools.getAllAttackActions(teamColor, aMap);
			// 乱数生成器
			Random fRand = RandomT.GetThreadRandom();

			if (atkActions.Count == 0) return false;
			int tmp = fRand.Next(atkActions.Count);
			aMap.executeAction(atkActions[tmp]);
			return true;
		}

		//引数のマップに対してランダムに移動行動(1ユニット分)を適用
		private bool doRandomMoveActionOfOneUnit(Map aMap, int teamColor) {
			List<Action> moveActions = AiTools.getAllMoveActions(teamColor, aMap);
			Random fRand = RandomT.GetThreadRandom();

			if (moveActions.Count == 0) return false;
			int tmp = fRand.Next(moveActions.Count);
			aMap.executeAction(moveActions[tmp]);
			return true;
		}

		//相手のチームカラーを返す関数
		private int returnOpormentTeamColor(int teamColor) {
			if (teamColor == 0) return 1;
			else return 0;
		}

		//敵ユニットと自軍ユニットの総HPの差から現状を評価する．ただし歩兵は0.2倍掛けとする．
		private double evaluateMAP(Map map, int teamColor) {
			List<Unit> enUnits = map.getUnitsList(teamColor, false, false, true);
			List<Unit> myUnits = map.getUnitsList(teamColor, true, true, false);


			double[] UnitsCost = { 2, 2, 1.5, 1, 0.7, 0.2 };
			double MyUnitsEvaluate = 0, EnUnitsEvaluate = 0;

			foreach (Unit unit in enUnits) {
				EnUnitsEvaluate += unit.getHP() * UnitsCost[unit.getTypeOfUnit()];
			}
			foreach (Unit unit in myUnits) {
				MyUnitsEvaluate += unit.getHP() * UnitsCost[unit.getTypeOfUnit()];
			}

			int bonus = 1;
			if (MyUnitsEvaluate == 0 || EnUnitsEvaluate == 0) bonus = 2;

			return (MyUnitsEvaluate - EnUnitsEvaluate) * bonus;
		}

		/// <summary>
		/// 評価関数 -1:試合中　1:勝ち　0:負け 0.5:引き分け
		/// 入力されたマップの操作チームからみた勝敗
		/// </summary>
		/// <param name="map">マップ</param>
		/// <param name="teamColor">チームカラー</param>
		/// <returns></returns>
		private double evaluateMAP_binary(Map map, int TeamColor) {

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

		private bool WinOrLose(Map map) {
			if (map.getNumOfAliveColorUnits(Consts.RED_TEAM) == 0 && map.getNumOfAliveColorUnits(Consts.BLUE_TEAM) == 0) return false;
			if (map.getNumOfAliveColorUnits(returnOpormentTeamColor(fAIteam)) == 0) return true;
			if (map.getNumOfAliveColorUnits(fAIteam) == 0) return false;

			return false;
		}
	}
}
