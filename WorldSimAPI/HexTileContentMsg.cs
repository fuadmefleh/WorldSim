using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimAPI
{

     public class HexTileContentQueryMsg
    {
        public int xPos;
        public int yPos;

        public HexTileContentQueryMsg(int x, int y)
        {
            xPos = x;
            yPos = y;
        }
    }

    public class HexTileContentMsg
    {
        public int xPos;
        public int yPos;        
        public HeightType tileType;
        public BiomeType biomeType;
        public HeatType heatType;
        public MoistureType moistureType;
        public bool hasRiver;
        public bool hasGamePop;

        public HexTileContentMsg(int x, int y, HeightType tType, HeatType heatTypeVal)
        {
            xPos = x;
            yPos = y;

            tileType = tType;
            heatType = heatTypeVal;
        }
    }

    public class HexTileDetailedContentMsg
    {
        public int xPos;
        public int yPos;
        public HeatType heatType;
        public BiomeType biomeType;
        public HeightType tileType;
        public MoistureType moistureType;
        public bool hasRiver;
        public List<(Direction, Direction)> riverDirections;
        public float humidity;
        public float temperature;
        public float elevation;

        public HexTileDetailedContentMsg(int x, int y, HeightType tType, HeatType heatTypeVal)
        {
            xPos = x;
            yPos = y;

            tileType = tType;
            heatType = heatTypeVal;
        }
    }

}
