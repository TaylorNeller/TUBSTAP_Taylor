using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars
{
    // 新規にAIを作成したい場合は，このインターフェースを
    // 実装する必要がある
    interface Player {
        //AIの行動作成関数
        /// <summary>
        /// AIの行動を作成する関数
        /// </summary>
        /// <param name="map">現在のMAP状況</param>
        /// <param name="teamColor">操作するチーム</param>
        /// <param name="turnStart">ターンの開始直後であるか</param>
        /// <param name="gameStart">ゲームの開始直後であるか</param>
        /// <returns>AIの行動（1ユニット分）</returns>
		Action makeAction(Map map, int teamColor, bool isTheBeggingOfTurn, bool isTheFirstTurnOfGame);

        // １行で，名前を返す関数
        string getName();

        // 改行を含んでも良い，パラメータ等の情報を返す関数
        string showParameters();

    }
}
