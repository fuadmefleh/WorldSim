using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace WorldSimAPI.ContentMsg
{
    public class TurnNumberContentMsg
    {
        public int TurnNumber { get; set; }
        public List<CaravanContentMsg> Caravans { get; set; } = new List<CaravanContentMsg>();
        public List<GamePopCenterContentMsg> PopulationCenters { get; set; } = new List<GamePopCenterContentMsg>();
    }

    public class TurnNumberQueryMsg
    {
        public bool UpdateTurn { get; set; }

        public TurnNumberQueryMsg(bool updateTurn)
        { 
            UpdateTurn = updateTurn;
        }
    }

    public class TurnNumberReplyMsg
    {
        public TurnNumberContentMsg Content { get; set; }

        public TurnNumberReplyMsg(TurnNumberContentMsg content)
        {
            Content = content;
        }
    }
}
