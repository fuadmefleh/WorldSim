using System;
using System.Collections.Generic;
using System.Text;
using WorldSimLib.DataObjects;
using System.Linq;
using WorldSimAPI;
using WorldSimLib.Utils;
using System.Numerics;
using System.Diagnostics;

namespace WorldSimLib.AI
{
    public class NeedHappinessCollection : List<Tuple<PopNeed, float>>
    {
        public NeedHappinessCollection(PopNeed need, float level)
        {
            Add(new Tuple<PopNeed, float>(need, level));
        }
        public void AddNeed(PopNeed need, float level)
        {
            Add(new Tuple<PopNeed, float>(need, level));
        }
    }

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

        public List<PopTechnology> Technologies { get; set; }

        public Dictionary<GamePopCenter, int> Locations { get; set; }

        public Dictionary<GamePopCenter, int> EmploymentAtLocations { get; set; }

        public Dictionary<GamePopCenter, float> StdOfLivingSumAtLocations { get; set; }

        public Dictionary<GamePopCenter, Inventory> InventoryAtLocations { get; set; }

        public Dictionary<GamePopCenter, GameAgentWallet> WealthAtLocations { get; set; }

        #region Internal Variables

        float previousQualityOfLife = 1f;

        float birthRate = 0.05f;

        Dictionary<GamePopCenter, NeedHappinessCollection> NeedHappinessAtLocations { get; set; }

        #endregion

        #region Operators
        public static bool operator ==(GamePop lhs, GamePop rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GamePop lhs, GamePop rhs) => !(lhs == rhs);

        public override bool Equals(object obj) => this.Equals(obj as GamePop);

        public bool Equals(GamePop p)
        {
            if (p is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, p))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != p.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (Name == p.Name) && (Culture == p.Culture) && (Religion == p.Religion) && (Occupation == p.Occupation);
        }

        public override int GetHashCode() => (Name, Culture, Religion, Occupation).GetHashCode();

        #endregion

        float NeedMultiplier = 10.0f;

        #region Helpers



        public int StandardOfLiving(GamePopCenter center)
        {
            return (int)Math.Min( Math.Floor(StdOfLivingSumAtLocations[center] / 100), 100 );
        }

        public int TotalPopulation()
        {
            int ret = 0;

            foreach (var item in Locations)
            {
                ret += item.Value;
            }

            return ret;
        }

        //float StandaradOfLivingSum { 
        //    get { return standardOfLivingSum; } 
        //    set
        //    {
        //        if (value < 100) standardOfLivingSum = 100;
        //        else standardOfLivingSum = value;
        //    }
        //}

        #endregion


        public GamePop(string name) : base(name)
        {
            Locations = new Dictionary<GamePopCenter, int>();
            EmploymentAtLocations = new Dictionary<GamePopCenter, int>();
            InventoryAtLocations = new Dictionary<GamePopCenter, Inventory>();
            WealthAtLocations = new Dictionary<GamePopCenter, GameAgentWallet>();
            Needs = new List<PopNeed>();
            Technologies = new List<PopTechnology>();
            NeedHappinessAtLocations = new Dictionary<GamePopCenter, NeedHappinessCollection>();
            StdOfLivingSumAtLocations = new Dictionary<GamePopCenter, float>();
        }

        void Init(GamePopCenter center)
        {
            _priceBeliefs = new Dictionary<string, Vector2>();

            var listOfItems = GameOracle.Instance.GameData.Items;

            foreach (Item item in listOfItems)
            {
                float price = center.MarketPlace.GetAveragePrice(item.Name);

                _priceBeliefs.Add(item.Name, new Vector2(price * 0.65f, price * 1.65f));
            }
        }

        void ProcessNeeds(uint turnNumber, GamePopCenter center)
        {
            foreach (var need in Needs)
            {
                if (need.MinSOLLevel > StandardOfLiving(center))
                    continue;

                if (need.MaxSOLLevel < StandardOfLiving(center))
                    continue;

                foreach (var itemType in need.AssociatedItemTypes)
                {
                    // For each 10 units of population, consume some items
                    var amtToPurchase = (int)Math.Ceiling(Locations[center] / NeedMultiplier);
                    var item = GetClosestItemForQoL(itemType, CalculateQualityOfLife(center));

                    GameAgent tempAgent = new GameAgent(this.Name)
                    {
                        Wallet = WealthAtLocations[center],
                        Inventory = InventoryAtLocations[center]
                    };

                    var needsForLocation = NeedHappinessAtLocations[center];
                    var needToAdjustIdx = needsForLocation.FindIndex(pred => pred.Item1 == need);
                    var needToAdjust = needsForLocation[needToAdjustIdx];

                    (var popNeed, var happinessLevel) = needToAdjust.ToValueTuple();

                    // Consume from inventory to fulfill need
                    int actualAmt = tempAgent.Inventory.GetQuantityOfItem(item.Name);
                    if (actualAmt > 0)
                    {
                        int amtToRemove = Math.Min(amtToPurchase, actualAmt);

                        happinessLevel += 10f * amtToRemove;

                        if (happinessLevel > 100)
                        {
                            StdOfLivingSumAtLocations[center] += happinessLevel - 100;
                            happinessLevel = 100;
                        }

                        tempAgent.Inventory.RemoveFromInventory(item.Name, amtToRemove);
                    }
                    else
                    {
                        happinessLevel -= 7.5f * amtToPurchase;

                        if (happinessLevel < 0)
                        {
                            StdOfLivingSumAtLocations[center] += happinessLevel;
                            happinessLevel = 0;
                        }
                    }

                    if (StdOfLivingSumAtLocations[center] < 100)
                    {
                        StdOfLivingSumAtLocations[center] = 100;
                    }

                    needsForLocation[needToAdjustIdx] = new Tuple<PopNeed, float>(popNeed, happinessLevel);
                }
            }
        }


        //void ProcessNeeds( uint turnNumber, GamePopCenter center )
        //{
        //    foreach (var need in Needs)
        //    {
        //        if (need.MinSOLLevel > StandardOfLiving(center))
        //            continue;

        //        if (need.MaxSOLLevel < StandardOfLiving(center))
        //            continue;

        //        foreach (var itemType in need.AssociatedItemTypes)
        //        {
        //            // For each 10 units of population, consume some items
        //            var amtToPurchase = (int)Math.Ceiling(Locations[center] / NeedMultiplier);
        //            var item = GetClosestItemForQoL(itemType, CalculateQualityOfLife(center));

        //            GameAgent tempAgent = new GameAgent(this.Name)
        //            {
        //                Wallet = WealthAtLocations[center],
        //                Inventory = InventoryAtLocations[center]
        //            };

        //            var needsForLocation = NeedHappinessAtLocations[center];
        //            var needToAdjustIdx = needsForLocation.FindIndex(pred => pred.Item1 == need);
        //            var needToAdjust = needsForLocation[needToAdjustIdx];

        //            (var popNeed, var happinessLevel) = needToAdjust.ToValueTuple();

        //            // Consume from inventory to fulfill need
        //            if (tempAgent.Inventory.ContainsItemAndQty(item.Name, (int)amtToPurchase))
        //            {
        //                happinessLevel += 10f * amtToPurchase;

        //                if (happinessLevel > 100)
        //                {
        //                    StdOfLivingSumAtLocations[center] += happinessLevel - 100;
        //                    happinessLevel = 100;
        //                }                            

        //                tempAgent.Inventory.RemoveFromInventory(item.Name, (int)amtToPurchase);
        //            }
        //            else
        //            {
        //                happinessLevel -= 7.5f * amtToPurchase;

        //                if (happinessLevel < 0)
        //                {
        //                    StdOfLivingSumAtLocations[center] += happinessLevel;
        //                    happinessLevel = 0;
        //                }
        //            }

        //            if(StdOfLivingSumAtLocations[center] < 100)
        //            {
        //                StdOfLivingSumAtLocations[center] = 100;
        //            }

        //            needsForLocation[needToAdjustIdx] = new Tuple<PopNeed, float>(popNeed, happinessLevel);
        //        }
        //    }
        //}

        void PurchaseForNeeds(uint turnNumber, GamePopCenter center)
        {
            foreach (var need in Needs)
            {
                if (need.MinSOLLevel > StandardOfLiving(center))
                    continue;

                if (need.MaxSOLLevel < StandardOfLiving(center))
                    continue;

                foreach (var itemType in need.AssociatedItemTypes)
                {
                    // For each 100 units of population, purchase some items
                    var amtToPurchase = (int)Math.Ceiling(Locations[center] / NeedMultiplier);
                    var item = GetClosestItemForQoL(itemType, CalculateQualityOfLife(center));

                    GameAgent tempAgent = new GameAgent(this.Name)
                    {
                        Wallet = WealthAtLocations[center],
                        Inventory = InventoryAtLocations[center]
                    };

                    //var averageSellPrice = market.GetHighestSellPrice(item, 2);
                    float price = StaticRandom.Instance.Range(_priceBeliefs[item.Name].X, _priceBeliefs[item.Name].Y);

                    // Determine the maximum quantity that can be afforded by the buyer
                    int maxAffordableQuantity = (int)(tempAgent.Wallet.GetAmount(center.LocalCurrency) / price);

                    // Use the minimum of the affordable quantity and the requested quantity
                    int quantityToProcess = (int)Math.Min(amtToPurchase * 1.5f, maxAffordableQuantity);

                    if (quantityToProcess < 1)
                        continue;

                    Offer offer = new Offer(item.Name, price, quantityToProcess, OfferType.Buy, center.LocalCurrency)
                    {
                        owner = tempAgent
                    };

                    center.MarketPlace.PlaceOffer(offer);
                    OffersFromLastTurn.Add(offer);
                }
            }
        }

        public new void EndTurn(uint turnNumber)
        {
            var popCenters = Locations.Keys.ToArray();

            for (int i = 0; i < popCenters.Length; i++)
            {
                var center = popCenters[i];

                if (_priceBeliefs == null)
                    Init(center);

                AdjustPriceBeliefs(turnNumber, center);

                PurchaseForNeeds(turnNumber, center);

                ProcessNeeds(turnNumber, center);

                FindJobs(center);

                //if (standardOfLivingSum > previousSOLSum)
                //{

                //}

                if (turnNumber % 2 == 0)
                {
                    var newPopAmt = (int)(Locations[center] * birthRate);

                    AddPopToLocation(center, newPopAmt);
                    center.Populations[this] += newPopAmt;
                }

                //foreach (var need in Needs)
                //{
                //    var needsHappiness = NeedHappinessAtLocations[center];
                //    var needToAdjust = needsHappiness.Find(pred => pred.Item1 == need);

                //    // XXX Change the max to come from the need itself
                //    if (needToAdjust.Item2 > 100)
                //    {
                //        float diff = needToAdjust.Item2 - 100;

                //        AddPopToLocation(center, (int)Math.Round(diff));
                //    }
                //}
            }
        }

        public Item GetClosestItemForQoL(ItemType itemType, float currentQoL)
        {
            Item selected = null;
            float closestDifference = float.PositiveInfinity;

            var itemsWithType = GameOracle.Instance.GameData.Items.FindAll(pred => pred.IType == itemType);

            foreach (var item in itemsWithType)
            {
                //float difference = currentQoL - item.QualityOfLife;

                //if (difference >= 0 && difference < closestDifference)
                //{
                  //  closestDifference = difference;
                    selected = item;
                //}
            }

            return selected;
        }



        void FindBetterJobs(GamePopCenter center)
        {
            // Sort the factories by wage in descending order
            var sortedFactories = center.Factorys.OrderBy(factory => factory.Wage).ThenByDescending(pred => pred.TotalWorkers).ToList();
            var factoriesWithHighestWage = center.Factorys.OrderByDescending(pred => pred.WorkersNeeded).ThenByDescending(factory => factory.Wage).ToList();
            sortedFactories.RemoveAll(pred => pred.TotalWorkers == 0);
            var factoryWithLowestWage = sortedFactories[0];            

            if( factoryWithLowestWage != null && factoryWithLowestWage.TotalWorkers > 0)
            {
                int randomWorkerCount = StaticRandom.Instance.Next( 1, factoryWithLowestWage.TotalWorkers );

                foreach( var factory in factoriesWithHighestWage )
                {
                    int workersNeeded = factory.WorkersNeeded;

                    if (workersNeeded <= randomWorkerCount )
                    {
                        if (factoryWithLowestWage.Workers.ContainsKey(this))
                        {
                            factoryWithLowestWage.Workers[this] -= workersNeeded;

                            if( !factory.Workers.TryAdd( this, workersNeeded ) )
                                factory.Workers[this] += workersNeeded;
                        }

                        randomWorkerCount -= workersNeeded;
                    } 
                    else
                    {
                        if (factoryWithLowestWage.Workers.ContainsKey(this))
                        {
                            factoryWithLowestWage.Workers[this] -= randomWorkerCount;

                            if (!factory.Workers.TryAdd(this, randomWorkerCount))
                                factory.Workers[this] += randomWorkerCount;
                        }

                        randomWorkerCount = 0;
                    }

                    if (randomWorkerCount == 0)
                        break;
                }
            }
            //foreach (var factory in sortedFactories)
            //{
            //    int requiredWorkers = factory.agentType.RequiredWorkers;
            //    int totalWorkers = factory.Workers.Values.Sum();

            //    if (totalWorkers >= requiredWorkers)
            //    {
            //        continue;
            //    }

            //    int workerDeficit = requiredWorkers - totalWorkers;
            //    int workersToEmploy = Math.Min(availableToEmploy, workerDeficit);
            //    EmploymentAtLocations[center] += workersToEmploy;

            //    if (!factory.Workers.TryAdd(this, workersToEmploy))
            //    {
            //        factory.Workers[this] += workersToEmploy;
            //    }

            //    availableToEmploy -= workersToEmploy;

            //    if (availableToEmploy <= 0)
            //    {
            //        return;
            //    }
            //}
        }


        void FindJobs(GamePopCenter center)
        {
            int availableToEmploy = Locations[center] - EmploymentAtLocations[center];

            if (availableToEmploy <= 0)
            {
                FindBetterJobs(center);
                return;
            }

            // Sort the factories by wage in descending order
            var sortedFactories = center.Factorys.OrderByDescending(factory => factory.Wage);

            foreach (var factory in sortedFactories)
            {
                int requiredWorkers = factory.WorkersRequired;
                int totalWorkers = factory.TotalWorkers;

                if (totalWorkers >= requiredWorkers)
                {
                    continue;
                }

                int workerDeficit = requiredWorkers - totalWorkers;
                int workersToEmploy = Math.Min(availableToEmploy, workerDeficit);
                EmploymentAtLocations[center] += workersToEmploy;

                if (!factory.Workers.TryAdd(this, workersToEmploy))
                {
                    factory.Workers[this] += workersToEmploy;
                }

                availableToEmploy -= workersToEmploy;

                if (availableToEmploy <= 0)
                {
                    return;
                }
            }
        }

        public void FireWorkers(GamePopCenter center, int maxWorkers)
        {
            if( !EmploymentAtLocations.ContainsKey(center) )
            {
                return;
            }

            EmploymentAtLocations[center] -= maxWorkers;
        }

        #region Public Helper Functions

        public void AddPopToLocation(GamePopCenter center, int amt)
        {
            if (Locations.ContainsKey(center))
            {
                Locations[center] += amt;
            }
            else
            {
                Locations.Add(center, amt);
                EmploymentAtLocations.Add(center, 0);
                InventoryAtLocations.Add(center, new Inventory());
                WealthAtLocations.Add(center, new GameAgentWallet(center.LocalCurrency, 0));
                StdOfLivingSumAtLocations.Add(center, 250);
                AddNeeds(center, Needs);
            }
        }

        public void RemovePopFromLocation(GamePopCenter center, int amt)
        {
            if (Locations.ContainsKey(center))
            {
                Locations[center] -= amt;
            }
        }

        public void AddNeed(GamePopCenter center, PopNeed newNeed)
        {
            if (NeedHappinessAtLocations.ContainsKey(center))
            {
                if (NeedHappinessAtLocations[center].Exists(pred => pred.Item1.Name == newNeed.Name))
                {
                    Console.Error.WriteLine("Attempting to add need to pop with existing need");
                }
                else
                {
                    NeedHappinessAtLocations[center].AddNeed(newNeed, 100);
                }
            }
            else
            {
                NeedHappinessAtLocations.Add(center, new NeedHappinessCollection(newNeed, 100));
            }

            if( !Needs.Contains(newNeed))
                Needs.Add(newNeed);
        }

        public void AddNeeds(GamePopCenter center, List<PopNeed> newNeeds)
        {
            foreach (PopNeed need in newNeeds)
            {
                AddNeed(center, need);
            }
        }

        protected override void AdjustPriceBeliefs(uint turnNumber, GamePopCenter center)
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
            }

            OffersFromLastTurn.Clear();
        }


        public float CalculateQualityOfLife(GamePopCenter center)
        {
            float qualityOfLife = 0;
            float totalHappiness = 0;

            MarketPlace market = center.MarketPlace;

            foreach (var need in Needs)
            {
                float needCost = 0;
                float needHappiness = 0;

                foreach (var itemType in need.AssociatedItemTypes)
                {
                    var item = GetClosestItemForQoL(itemType, previousQualityOfLife);

                    // Increase the cost a lot if we can't buy the product
                    if (market.GetTotalSellQuantity(item.Name, 5) == 0)
                    {
                        needCost += 1000;
                    }
                    else
                    {
                        float avgPrice = market.GetAverageSellPrice(item.Name, 5);
                        needCost += avgPrice;
                    }
                }

                var needsForLocation = NeedHappinessAtLocations[center];
                var needToAdjust = needsForLocation.Find(pred => pred.Item1 == need);
                needHappiness = needToAdjust.Item2;

                totalHappiness += needHappiness;

                // Calculate the weighted cost for this need (lower cost is better)
                float weightedCost = (needHappiness / needCost);
                qualityOfLife += weightedCost;
            }

            // Normalize the quality of life by dividing it by the total happiness
            qualityOfLife /= totalHappiness;

            previousQualityOfLife = qualityOfLife;

            return qualityOfLife;
        }


        #endregion

        #region Public Conversion Functions

        public GamePopContentMsg ToContentMsg(GamePopCenter center)
        {
            GamePopContentMsg retMsg = new GamePopContentMsg
            {
                Culture = this.Culture,
                Quantity = this.Locations[center],
                Occupation = this.Occupation,
                EducationLevel = this.EducationLevel,
                Religion = this.Religion,
                Name = this.Name,
                Wealth = this.WealthAtLocations[center].Currencies.First().Value,
                OverallHappiness = this.NeedHappinessAtLocations[center].Average(pred => pred.Item2)
            };

            return retMsg;
        }

        public string ToMarkdown()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("### GamePop: {0}\n", Name);
            sb.AppendFormat("* **Culture:** {0}\n", Culture);
            sb.AppendFormat("* **Religion:** {0}\n", Religion);
            sb.AppendFormat("* **Education Level:** {0}\n", EducationLevel);
            sb.AppendFormat("* **Occupation:** {0}\n", Occupation);

            sb.AppendLine("#### Standard of Living at Location:");
            foreach (var kvp in StdOfLivingSumAtLocations)
            {
                sb.AppendFormat("* {0}: {1}\n", kvp.Key.Name, kvp.Value);
            }

            //sb.AppendLine("#### Needs:");
            //foreach (var need in Needs)
            //{
            //    sb.AppendFormat("* {0}\n", need.ToMarkdown());
            //}

            //sb.AppendLine("#### Technologies:");
            //foreach (var tech in Technologies)
            //{
            //    sb.AppendFormat("* {0}\n", tech.ToString());
            //}

            sb.AppendLine("#### Locations:");
            foreach (var kvp in Locations)
            {
                sb.AppendFormat("* {0} ({1})\n", kvp.Key.Name, kvp.Value);
            }

            sb.AppendLine("#### Inventory at Locations:");
            foreach (var kvp in InventoryAtLocations)
            {
                sb.AppendFormat("##### {0}:\n", kvp.Key.Name);

                sb.AppendLine("| Item Name | Cost | Cost Per Unit | Quantity | Original Quantity |");
                sb.AppendLine("| --- | --- | --- | --- | --- |");

                foreach (var item in kvp.Value.ItemsContainer) { 
                    foreach (var record in item.Value.FindAll(pred=>pred.Quantity > 0))
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

            sb.AppendLine("#### Wealth at Locations:");
            foreach (var kvp in WealthAtLocations)
            {
                sb.AppendFormat("* {0}: {1}\n", kvp.Key.Name, kvp.Value);
            }

            sb.AppendLine("#### Need Happiness");
            foreach (var kvp in NeedHappinessAtLocations)
            {
                sb.AppendFormat("* {0}:\n", kvp.Key.Name);

                foreach (var item in kvp.Value)
                {
                    sb.AppendFormat("  * {0} x {1}\n", item.Item1.Name, item.Item2);
                }
            }

            return sb.ToString();
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("GamePop: {0}\n", Name);
            sb.AppendFormat("Culture: {0}\n", Culture);
            sb.AppendFormat("Religion: {0}\n", Religion);
            sb.AppendFormat("EducationLevel: {0}\n", EducationLevel);
            sb.AppendFormat("Occupation: {0}\n", Occupation);
            
            sb.AppendLine("Standard of Living at Location:");
            foreach (var kvp in StdOfLivingSumAtLocations)
            {
                sb.AppendFormat("\t{0}: {1}\n", kvp.Key.Name, kvp.Value);
            }

            sb.AppendLine("Needs:");
            foreach (var need in Needs)
            {
                sb.AppendFormat("\t{0}\n", need.ToString());
            }

            sb.AppendLine("Technologies:");
            foreach (var tech in Technologies)
            {
                sb.AppendFormat("\t{0}\n", tech.ToString());
            }

            sb.AppendLine("Locations:");
            foreach (var kvp in Locations)
            {
                sb.AppendFormat("\t{0} ({1})\n", kvp.Key.Name, kvp.Value);
            }

            sb.AppendLine("Inventory at Locations:");
            foreach (var kvp in InventoryAtLocations)
            {
                sb.AppendFormat("\t{0}:\n", kvp.Key.Name);
                foreach (var item in kvp.Value.ItemsContainer)
                {
                    sb.AppendFormat("\t\t{0} x {1}\n", item.Key.ToString(), item.Value);
                }
            }

            sb.AppendLine("Wealth at Locations:");
            foreach (var kvp in WealthAtLocations)
            {
                sb.AppendFormat("\t{0}: {1}\n", kvp.Key.Name, kvp.Value);
            }

            sb.AppendLine("Need Happiness");
            foreach (var kvp in NeedHappinessAtLocations)
            {
                sb.AppendFormat("\t{0}:\n", kvp.Key.Name);

                foreach (var item in kvp.Value)
                {
                    sb.AppendFormat("\t\t{0} x {1}\n", item.Item1.Name, item.Item2);
                }
            }

            return sb.ToString();
        }

        #endregion

    }
}
