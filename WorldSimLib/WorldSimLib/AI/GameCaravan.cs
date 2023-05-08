﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.AI
{
    public class GameCaravan : GameAgent
    {
        public HexTile Location;
        public Dictionary<GamePop, int> Populations = new Dictionary<GamePop, int>();
        public GameCaravan(string name) : base(name) { }
    }
}
