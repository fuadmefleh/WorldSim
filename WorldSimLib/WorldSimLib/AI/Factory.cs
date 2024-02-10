using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using WorldSimAPI;
using WorldSimLib.DataObjects;
using WorldSimLib.Utils;

namespace WorldSimLib.AI
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Factory : GameAgent
    {
        public Factory(string name, AgentType aType) : base(name)
        {
            agentType = aType;            
        }

        public AgentType agentType;

        
        public Dictionary<GamePop, int> Workers { get; set; } = new Dictionary<GamePop, int>();

        public float Wage { get; set; }

        public int Level { get; set; } = 1;

        int turnsSinceLastWorkerAdjustment = 0;
        int turnsSinceLastRaise = 0;
        int turnsSinceLastProduction = 0;
        int turnsSinceLastSale = 0;

        void Init(GamePopCenter center)
        {
            _priceBeliefs = new Dictionary<string, Vector2>();

            var listOfItems = GameOracle.Instance.GameData.Items;

            Wage = StaticRandom.Instance.Range(0.05f, 0.25f);

            foreach (Item item in listOfItems)
            {
                float price = center.MarketPlace.GetAveragePrice(item.Name);

                _priceBeliefs.Add(item.Name, new Vector2(Wage * 0.65f, Wage * 1.65f));
            }
        }

        public int TotalWorkers
        {
            get
            {
                return Workers.Values.Sum();
            }
        }

        public int WorkersNeeded
        {
            get {
                return WorkersRequired - TotalWorkers;
            }
        }

        public int WorkersRequired
        {
            get
            {
                return agentType.RequiredWorkers * Level;
            }
        }

        public void FireAllWorkers(GamePopCenter center)
        {
            foreach (var worker in Workers)
            {
                worker.Key.FireWorkers( center, worker.Value );
            }
            Workers.Clear();
            turnsSinceLastWorkerAdjustment = 0;
        }


        public void EndTurn(uint turnNumber, GamePopCenter center)
        {
            if (_priceBeliefs == null)
                Init(center);

            WealthAtTurn[turnNumber] = new GameAgentWallet(Wallet);

            // Increment turnsSinceLastSale if no sales occurred in the last turn
            bool anySales = OffersFromLastTurn.Any(offer => offer.offerType == OfferType.Sell && offer.IsProcessed);
            if (!anySales)
            {
                turnsSinceLastSale++;
            }
            else
            {
                turnsSinceLastSale = 0;
            }

            AdjustPriceBeliefs(turnNumber, center);

            PayWages(turnNumber, center);

            if (TotalWorkers == 0)
            {
                turnsSinceLastWorkerAdjustment++;
            }
            else
            {
                turnsSinceLastWorkerAdjustment = 0;
            }

            turnsSinceLastRaise++;

            if (turnsSinceLastRaise > 5 && turnNumber > 5 && ProfitOverTurns(turnNumber, 5).GetAmount(center.LocalCurrency) > 0)
            {
                AdjustWage(center);
                turnsSinceLastRaise = 0;
            }

            if (turnsSinceLastWorkerAdjustment >= 3)
            {
                AdjustWage(center);
                turnsSinceLastWorkerAdjustment = 0;
            }

            ProcessRecipes(turnNumber, center);

            SellGoods(turnNumber, center);

            BuyGoods(turnNumber, center);

            // Check if the factory didn't produce any goods in this turn
            if (turnsSinceLastProduction > 0)
            {
                // Add the cost of wages to the existing inventory of output
                foreach (Recipe recipe in agentType.Recipes)
                {
                    foreach (var output in recipe.Outputs)
                    {
                        var itemName = output.ItemName;
                        Inventory.IncreaseItemCost(itemName, TotalWages());
                    }
                }
            }
        }


        protected override void AdjustPriceBeliefs(uint turnNumber, GamePopCenter center)
        {
            float historicalPriceWeight = 0.3f;

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
                    float clearingPriceWeight = 0.8f; // Adjust this weight to give more influence to the clearing price

                    // Use the clearing price to update the price belief
                    middlePrice = (clearingPriceWeight * offer.ClearingPrice) + ((1 - clearingPriceWeight) * middlePrice);
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
                }

                _priceBeliefs[itemName] = new Vector2(Math.Max(0, _priceBeliefs[itemName].X), Math.Max(0, _priceBeliefs[itemName].Y));
            }

            OffersFromLastTurn.Clear();
        }


        public void AdjustWage(GamePopCenter center, float desiredProfitMargin = 1.2f)
        {          
            // Get the list of all other factories
            var otherFactories = center.Factorys.Where(agent => agent != this).ToList();

            if (otherFactories.Count == 0)
                return;

            // Calculate the average wage of other factories
            float avgWage = otherFactories.Average(factory => factory.Wage);

            // Calculate the total cost of production including the current wage
            float totalCost = 0;
            foreach (var recipe in agentType.Recipes)
            {
                totalCost += CalculateProductionPurchaseCost(recipe, center);
            }

            // Calculate the new wage
            //float newWage = (avgWage * desiredProfitMargin * totalCost) / (totalCost - avgWage * Workers.Values.Sum());
                        
            float newWage = Wage * 1.05f;

            Console.WriteLine($"Updating wage {newWage} from {Wage}");

            // Set the new wage
            Wage = newWage;
        }


        void PayWages(uint turnNumber, GamePopCenter center )
        {
            foreach( var pop in Workers)
            {
                pop.Key.WealthAtLocations[center].AddAmount( center.LocalCurrency, Wage * pop.Value );
                Wallet.RemoveAmount( center.LocalCurrency, Wage * pop.Value);
            }
        }
        public float TotalWages()
        {
            float total = 0.0f;
            foreach (var pop in Workers)
            {
                total += Wage * pop.Value;
            }
            return total;
        }

        public float ValueOfOutputGoods()
        {
            float totalVal = 0;

            foreach (Recipe recipe in agentType.Recipes)
            {
                foreach (var output in recipe.Outputs)
                {
                    var itemName = output.ItemName;

                    totalVal += Inventory.GetTotalWorthForItem(itemName);
                }
            }

            return totalVal;
        }
        public void BuyGoods(uint turnNumber, GamePopCenter center)
        {
            if (_priceBeliefs == null)
                Init(center);

            foreach (Recipe recipe in agentType.Recipes)
            {
                foreach (var input in recipe.Inputs)
                {
                    var itemName = input.ItemName;
                    int shortage = this.Inventory.Shortage(itemName, input.IdealQuantity * Level);
                   
                    float price = StaticRandom.Instance.Range(_priceBeliefs[itemName].X, _priceBeliefs[itemName].Y);

                    if (shortage <= 0)
                        continue;

                    // If there are none for sale over 3 turns, we are going to float a buy offer at a low price to stimulate demand
                    if( center.MarketPlace.GetTotalSellQuantity(itemName,3) == 0 )
                    {
                        price = 0.01f;
                        shortage = 40;
                    }

                    // Determine the maximum quantity that can be afforded by the buyer
                    int maxAffordableQuantity = (int)(Wallet.GetAmount(center.LocalCurrency) / price);

                    // Use the minimum of the affordable quantity and the requested quantity
                    int quantityToProcess = Math.Min(shortage, maxAffordableQuantity);

                    if (quantityToProcess < 1)
                        continue;

                    Offer offer = new Offer(itemName, price, quantityToProcess, OfferType.Buy, center.LocalCurrency)
                    {
                        owner = this
                    };

                    OffersFromLastTurn.Add(offer);

                    center.MarketPlace.PlaceOffer(offer);
                }
            }
        }

        void SellGoods(uint turnNumber, GamePopCenter center)
        {
            foreach (Recipe recipe in agentType.Recipes)
            {
                foreach (var output in recipe.Outputs)
                {
                    var itemName = output.ItemName;
                    int qtyToSell = this.Inventory.GetQuantityOfItem(itemName);

                    if (qtyToSell <= 0)
                        continue;

                    float productionCost = CalculateProductionCost(recipe);
                    float desiredProfitMargin = StaticRandom.Instance.Range(1.25f, 2.5f);

                    // Calculate the pressure factor based on the turns since the last sale
                    float pressureFactor = 1.0f;
                    if (turnsSinceLastSale >= 3)
                    {
                        pressureFactor = Math.Max(1.0f - (0.1f * (turnsSinceLastSale - 2)), 0.70f);
                    }

                    float price = productionCost * desiredProfitMargin * pressureFactor;

                    Offer offer = new Offer(itemName, price, qtyToSell, OfferType.Sell, center.LocalCurrency)
                    {
                        owner = this
                    };

                    OffersFromLastTurn.Add(offer);

                    center.MarketPlace.PlaceOffer(offer);
                }
            }
        }

        float CalculateProductionPurchaseCost(Recipe recipe, GamePopCenter center)
        {
            float laborCost = center.GetAverageWage();
            float materialCost = center.MarketPlace.EstimateRecipeCost(recipe);

            return materialCost + laborCost;
        }

        float CalculateProductionCost(Recipe recipe)
        {
            float materialCost = 0;

            foreach( var output in recipe.Outputs)
            {
                materialCost += this.Inventory.GetLowestCostForItem(output.ItemName);
            }
            
            // Labor was already included when we made the item
            return materialCost;
        }



        void ProcessRecipes( uint turnNumber, GamePopCenter center )
        {
            bool wasProduced = false;

            float efficiencyRate = TotalWorkers / WorkersRequired;

            // Create the factory product, consuming inputs and creating outputs
            foreach (var recipe in agentType.Recipes)
            {
                var availableResourcesInCell = center.Location.AvailableResourceSources.Keys.ToList();
                var availableResourcesInCellAsString = center.Location.AvailableResourceSources.Keys.ToList().ConvertAll(x => x.Name);
                bool cellContainsResourceForRecipe = recipe.ResourcesRequiredAsObjects.All(x => availableResourcesInCell.Any(y => x.Name == y.Name));

                bool hasRoomForOutput = Inventory.InventorySpaceLeft(recipe.Outputs[0].ItemName) >= recipe.Outputs[0].Quantity;

                for (int i = 0; i < Level; i++)
                {
                    if (this.Inventory.CanProcessRecipe(recipe) && cellContainsResourceForRecipe)
                    {
                        wasProduced = true;

                        if (hasRoomForOutput)
                        {
                            this.Inventory.ProcessRecipe(recipe, efficiencyRate, Wage * (WorkersRequired/Level));
                        }
                    }
                }
            }

            if (wasProduced)
            {
                turnsSinceLastProduction = 0;
            }
            else
            {
                turnsSinceLastProduction++;
            }
        }
        public string ToMarkdown(bool fullInventory = false)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("## Factory: " + Name);
            sb.AppendLine("* Level: " + Level);
            sb.AppendLine("* Wage: " + Wage.ToString("##.##"));
            sb.AppendLine("* AgentType: " + agentType.ToString());

            sb.AppendLine("\n## Wallet:");

            foreach (var currency in Wallet.Currencies)
            {
                sb.AppendLine("  * " + currency.Key.Name + ": " + currency.Value);
            }

            sb.AppendLine("### Inventory:");
            sb.AppendLine("| Item Name | Cost | Cost Per Unit | Quantity | Original Quantity |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            if (fullInventory)
            {
                foreach (var item in Inventory.ItemsContainer)
                {
                    foreach (var record in item.Value)//item.Value.FindAll( pred => pred.Quantity > 0 ) )
                    {
                        sb.AppendFormat("| {0} | {1} | {2} | {3} | {4} |\n",
                                        record.ItemName,
                                        record.Cost.ToString("$##.##"),
                                        record.CostPerUnit.ToString("$##.##"),
                                        record.Quantity,
                                        record.OriginalQuantity);
                    }
                }
            } else
            {
                foreach (var item in Inventory.ItemsContainer)
                {
                    foreach (var record in item.Value.FindAll( pred => pred.Quantity > 0 ) )
                    {
                        sb.AppendFormat("| {0} | {1} | {2} | {3} | {4} |\n",
                                        record.ItemName,
                                        record.Cost.ToString("$##.##"),
                                        record.CostPerUnit.ToString("$##.##"),
                                        record.Quantity,
                                        record.OriginalQuantity);
                    }
                }
            }

            sb.AppendLine("* Workers: " + Workers.Values.Sum().ToString());

            if (_priceBeliefs != null)
            {
                sb.AppendLine("\n## Price Beliefs:");
                foreach (var item in _priceBeliefs)
                {
                    sb.AppendLine("  * " + item.Key + " x " + item.Value.X + ":" + item.Value.Y);
                }
            }


            return sb.ToString();
        }



        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Factory: {0}\n", Name);
            sb.AppendFormat("Level: {0}\n", Level);
            sb.AppendFormat("Wage: {0}\n", Wage.ToString("##.##"));
            sb.AppendFormat("AgentType: {0}\n", agentType.ToString());
            sb.AppendFormat("Inventory: {0}\n", Inventory.ToString());
            sb.AppendFormat("Workers: {0}\n", Workers.Values.Sum().ToString());

            if (_priceBeliefs != null)
            {
                sb.AppendLine("\n\tPrice Beliefs:");
                foreach (var item in _priceBeliefs)
                {
                    sb.AppendFormat("\t\t{0} x {1}:{2}\n", item.Key, item.Value.X, item.Value.Y);
                }
            }

            sb.AppendLine("\n\tWallet:");

            foreach ( var currency in Wallet.Currencies)
            {
                sb.AppendFormat("\t{0}: {1}\n", currency.Key.Name, currency.Value);
            }

            //sb.AppendLine("Workers:");
            //foreach (var kvp in Workers)
            //{
            //    sb.AppendFormat("\t{0}: {1}\n", kvp.Key.ToString(), kvp.Value);
            //}

            return sb.ToString();
        }
       

        public FactoryContentMsg ToContentMsg()
        {
            FactoryContentMsg msg = new FactoryContentMsg();
            msg.Name = Name;
            msg.Wealth = Wallet.Currencies.First().Value;
            msg.Inventory = Inventory;

            return msg;
        }
    }
}
