using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace WorldSimAPI
{
    public class GamePopCenterContentQueryMsg
    {
        public Vector2 startPos;
        public Vector2 endPos;
    }

    public class GamePopCenterContentReplyMsg
    {
        public Vector2 startPos;
        public Vector2 endPos;

        public List<GamePopCenterContentMsg> gamePopCenters;

        public GamePopCenterContentReplyMsg()
        {
            this.gamePopCenters = new List<GamePopCenterContentMsg>();
        }

        public GamePopCenterContentReplyMsg(Vector2 startPos, Vector2 endPos)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.gamePopCenters = new List<GamePopCenterContentMsg>();
        }
    }

    public class GamePopCenterContentMsg
    {
        public string name;
        public int xPos;
        public int yPos;
        public SettlementType settlementType;
        public List<FactoryContentMsg> factoryData;
        public MarketContentMsg marketContent;
        public float wealth;

        public List<GamePopContentMsg> gamePops;

        public GamePopCenterContentMsg(int x, int y)
        {
            xPos = x;
            yPos = y;
            gamePops = new List<GamePopContentMsg>();
            factoryData = new List<FactoryContentMsg>();
        }

        public GamePopCenterContentMsg()
        {
            gamePops = new List<GamePopContentMsg>();
            factoryData = new List<FactoryContentMsg>();
        }
    }

    public class GamePopContentMsg
    {
        public string Name { get; set; }

        public string Culture { get; set; }

        public string Religion { get; set; }

        public int EducationLevel { get; set; }

        public string Occupation { get; set; }

        public int Quantity { get; set; }

        public float Wealth { get; set; }

        public float OverallHappiness { get; set; }
    }
}
