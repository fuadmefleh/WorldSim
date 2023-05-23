using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.AI
{
    public class GameSettler : GameAgent
    {
        public HexTile Location;
        public Dictionary<GamePop, int> Populations = new Dictionary<GamePop, int>();

        public GameSettler(string name) : base(name)
        {
           
        }

    }
}
