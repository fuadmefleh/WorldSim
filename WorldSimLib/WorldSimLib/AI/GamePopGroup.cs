using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.AI
{
    public class GamePopGroup : GameAgent
    {
        public HexTile Location;
        public Dictionary<GamePop, int> Populations = new Dictionary<GamePop, int>();

        public GamePopGroup(string name) : base(name) { }
    }
}
