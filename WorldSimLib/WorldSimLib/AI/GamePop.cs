using System;
using System.Collections.Generic;
using System.Text;
using WorldSimLib.DataObjects;

namespace WorldSimLib.AI
{
    public class GamePop : GameAgent
    {

        /// <summary>
        /// Name of the game population, this is usually a combination of other pop values
        /// </summary>
        public string Culture { get; set; }

        public string Religion { get; set; }

        /// <summary>
        /// Average education level. Max is 99 and min is 1.  This determines how educated/intelligent a group is.
        /// </summary>
        public int EducationLevel { get; set; }

        public string Occupation { get; set; }

        public List<PopNeed> Needs { get; set; }

        public Dictionary<GamePopCenter, int> Locations { get; set; }


        public GamePop(string name) : base(name)
        {
            Locations = new Dictionary<GamePopCenter, int>();
            Needs = new List<PopNeed>();
        }

        public void EndTurn(uint turnNumber, GamePopCenter center)
        {
            int quantityToProcess = Locations[center];

            foreach( var need in Needs )
            {
                if( Wealth > need.WealthLevel)
                {
                    foreach( var itemNeed in need.PopItemNeeds )
                    {
                        Offer newOffer = new Offer();
                        newOffer.owner = this;
                        newOffer.qty = itemNeed.Qty * quantityToProcess;
                        newOffer.IsMarketOrder = true;

                        center.MarketPlace.PlaceOffer(newOffer);
                    }

                }
            }
        }
    }
}
