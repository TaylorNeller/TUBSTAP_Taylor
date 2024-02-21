using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleWars.AI {
	/// <summary>
	/// M3Lee内で使用されているゲームツリーノード
	/// </summary>
    class M3_Node {
        public Map fMap;
        public Action fAction;
		public int fPlayerColor;
        public List<M3_Node> fChildNodes;
        public double fUCB;
        public int fCnt;
		public double fWinCnt;

        public M3_Node() {
            fMap = new Map();
            fAction = new Action();
            fChildNodes = new List<M3_Node>();
            fUCB = 0;
            fCnt = 0;
            fWinCnt= 0;
        }
    }
}