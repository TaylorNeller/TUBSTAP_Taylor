using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWars
{
	/// <summary>
	/// M_UCTに使用されているゲームツリー
	/// </summary>
    class M_GameTree
    {
        public Map board;
        public int depth;
        public int simnum;
        public double lastscore;
        public double totalscore;
        public double housyuu;
        public Action act;
        public List<M_GameTree> next;

        public M_GameTree()
        {
            board = new Map();
            depth = 0;
            simnum = 0;
            lastscore = 0;
            totalscore = 0;
            housyuu = 0;
            act = new Action();
            next = new List<M_GameTree>();
        }

    }
}
