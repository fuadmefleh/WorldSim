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
    public class GameAgent
    {
        [JsonProperty]
        public string Name { get; set; }
  
        public GameAgentWallet Wallet { get; set; } = new GameAgentWallet();

        [JsonProperty]
        public Inventory Inventory { get; set; } = new Inventory();

        public Dictionary<uint, GameAgentWallet> WealthAtTurn { get; set; } = new Dictionary<uint, GameAgentWallet>();



        protected List<Offer> OffersFromLastTurn = new List<Offer>();

        protected Dictionary<string, Vector2> _priceBeliefs = null;       

        #region Settings for Price Beliefs

        protected const float FullProcessedPriceDistanceMultiplier = 0.9f;
        protected const float LowerPriceBeliefMultiplier = 0.9f;
        protected const float RaisePriceBeliefMultiplier = 1.1f;
        protected const float LearningRate = 1.75f;
        protected const float OverpaymentPriceDistanceMultiplier = 0.5f;

        #endregion

        protected GameData GameData { get { return GameOracle.Instance.GameData; } }

        public GameAgent(string name)
        {
            Name = name;
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

        public GameAgentWallet ProfitOverTurns(uint turnNumber, uint lookback)
        {
            uint turnToStartAt = Math.Max(1, turnNumber - lookback);
            GameAgentWallet retVal = new GameAgentWallet();

            if (turnToStartAt == turnNumber)
                return retVal;

            if (!WealthAtTurn.ContainsKey(turnToStartAt))
                return retVal;

            return WealthAtTurn[turnNumber] - WealthAtTurn[turnToStartAt];
        }

        public void EndTurn(uint turnNumber)
        {
            
        }

    }
}