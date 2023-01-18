using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI;

namespace WorldSimLib.AI
{
    public class GamePopCenter
    {
        public HexTile Location { get; set; }
        public List<GamePop> Populations { get; set; }

        public SettlementType SettlementLevel { get; set; }

        public MarketPlace MarketPlace { get; set; }

        public List<Factory> Factorys { get; set; }

        public GamePopCenter(HexTile location, List<GamePop> populations, SettlementType settlementLevel)
        {
            Location = location;
            Populations = populations;
            SettlementLevel = settlementLevel;
        }

        public GamePopCenter()
        {
            Populations = new List<GamePop>();
            MarketPlace = new MarketPlace();
        }

        public void EndTurn(uint turnNumber)
        {
            foreach (var population in Populations)
            {
                population.EndTurn(turnNumber, this);
            }
        }
    }
}
