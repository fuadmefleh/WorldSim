using System;
using System.Collections.Generic;
using System.Text;
using WorldSimLib.AI;
using WorldSimLib.DataObjects;

namespace WorldSimLib
{
    public class Factory : GameAgent
    {
        public Factory(string name, AgentType aType) : base(name)
        {

        }

        public Dictionary<GamePop, int> Workers { get; set; } = new Dictionary<GamePop, int>();


    }
}
