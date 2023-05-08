using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using WorldSimLib.AI;
using WorldSimLib.DataObjects;
using WorldSimLib.Utils;

namespace WorldSimLib
{
    public class GameAgentWallet
    {
        float amount;

        public GameAgentWallet(float amount)
        {
            Amount = amount;
        }

        public float Amount
        {
            get { return amount; }
            set { amount = value; }
        }

        public override string ToString()
        {
            string retStr = "Wallet: \n";

            retStr += Amount.ToString("##.##") + "\n";

            return retStr;
        }
    }
    public class GameAgent
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public virtual float Wealth {
            get
            {
                return this.Wallet.Amount;
            }
            set
            {
                this.Wallet.Amount = value;
            }
        }        
        public GameAgentWallet Wallet { get; set; } = new GameAgentWallet(0);

        [JsonProperty]
        public Inventory Inventory { get; set; } = new Inventory();


        protected List<Offer> OffersFromLastTurn = new List<Offer>();

        protected Dictionary<string, Vector2> _priceBeliefs = null;       

        #region Settings for Price Beliefs

        protected const float FullProcessedPriceDistanceMultiplier = 0.9f;
        protected const float LowerPriceBeliefMultiplier = 0.9f;
        protected const float RaisePriceBeliefMultiplier = 1.1f;
        protected const float LearningRate = 1.1f;
        protected const float OverpaymentPriceDistanceMultiplier = 0.5f;

        #endregion


        public GameAgent(string name)
        {
            Name = name;
            Wealth = 0;
        }


        protected virtual void AdjustPriceBeliefs(uint turnNumber, GamePopCenter center)
        {
            float historicalPriceWeight = 0.5f;

            foreach (Offer offer in OffersFromLastTurn)
            {
                string itemName = offer.itemName;
                var currentPriceBelief = _priceBeliefs[itemName];

                float marketPrice = center.MarketPlace.GetAveragePrice(itemName);

                var priceDistance = currentPriceBelief.Y - currentPriceBelief.X;
                var middlePrice = (currentPriceBelief.Y + currentPriceBelief.X) / 2;

                // Incorporate historical price data
                middlePrice = (historicalPriceWeight * marketPrice) + ((1 - historicalPriceWeight) * middlePrice);

                if (offer.IsProcessed)
                {
                    priceDistance *= FullProcessedPriceDistanceMultiplier;
                    priceDistance /= 2;

                    _priceBeliefs[itemName] = new Vector2(middlePrice - priceDistance, middlePrice + priceDistance);
                }
                else if (offer.qty != offer.origQty)
                {
                    float ratio = (float)(offer.origQty - offer.qty) / offer.origQty;

                    priceDistance *= (FullProcessedPriceDistanceMultiplier + (1 - FullProcessedPriceDistanceMultiplier) * ratio);
                    priceDistance /= 2;

                    _priceBeliefs[itemName] = new Vector2(middlePrice - priceDistance, middlePrice + priceDistance);
                }
                else
                {
                    float multiplier = offer.offerType == OfferType.Sell ? LowerPriceBeliefMultiplier : RaisePriceBeliefMultiplier;

                    // Apply a learning rate to control the pace of belief updates
                    Vector2 adjustment = (multiplier - 1) * LearningRate * currentPriceBelief;
                    _priceBeliefs[itemName] += adjustment;

                    // If offer type gets too high, lower it to near the marketplace average
                    if (offer.offerType == OfferType.Buy)
                    {
                        float avgSellPrice = center.MarketPlace.GetAverageSellPrice(itemName);

                        if (_priceBeliefs[itemName].X > avgSellPrice * 2f)
                        {
                            _priceBeliefs[itemName] = new Vector2(avgSellPrice * 0.8f, avgSellPrice * 1.2f);
                        }
                    }
                }
            }

            OffersFromLastTurn.Clear();
        }


        public void EndTurn(uint turnNumber)
        {
            
        }

    }
}