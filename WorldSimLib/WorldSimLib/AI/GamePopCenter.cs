using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using WorldSimAPI;
using WorldSimLib.DataObjects;
using WorldSimLib.Utils;

namespace WorldSimLib.AI
{
    public class ConstructionTimer : Tuple<Factory, int>
    {
        public ConstructionTimer(Factory item1, int item2) : base(item1, item2)
        {
        }
    }

    public class TravelTimer : Tuple<GamePopGroup, List<HexTile>>
    {
        public TravelTimer(GamePopGroup item1, List<HexTile> item2) : base(item1, item2)
        {

        }
    }

    public class GamePopCenter : GamePopGroup
    {
        public GameCurrency LocalCurrency { get; private set; }
        public SettlementType SettlementLevel { get; set; }

        public MarketPlace MarketPlace { get; set; }

        public List<Factory> Factorys { get; set; }
        public List<Factory> BankruptFactorys { get; set; }

        public List<ConstructionTimer> ConstructionQueue { get; set; } = new List<ConstructionTimer>();
        public List<TravelTimer> TravelQueue { get; set; } = new List<TravelTimer>();
        public List<TravelTimer> CaravanQueue { get; set; } = new List<TravelTimer>();
        public List<TravelTimer> SettlerQueue { get; set; } = new List<TravelTimer>();

        public int MaxConstructionQueueSize { get; set; } = 3;

        public float TaxRate { get; set; }
        public float IncomeTaxRate { get; set; }
        float LastTaxCollected { get; set; } = 0;

        int _turnsSinceLastTaxAdjustment = 0;

        bool isPayingOffCosts = false;

        float _costOfLastConstruction = 0;
        public float CostOfLastConstruction
        {
            get
            {
                return _costOfLastConstruction;
            }
            set
            {
                _costOfLastConstruction = Math.Max(0, value);
            }
        }

        int initialSearchRadius = 3;
        public List<GamePopCenter> KnownNeighbors = new List<GamePopCenter>();
        public List<HexTile> VisitedTiles = new List<HexTile>();

        public GamePopCenter(string name, HexTile location, Dictionary<GamePop,int> populations, SettlementType settlementLevel) : base(name)
        {
            Location = location;
            Populations = populations;
            SettlementLevel = settlementLevel;

            LocalCurrency = new GameCurrency()
            {
                Name = name + " Currency",
                Issuer = this
            };

            MarketPlace = new MarketPlace();
            Factorys = new List<Factory>();
            BankruptFactorys = new List<Factory>();

            TaxRate = 0.1f; // 5% tax rate per transaction
            IncomeTaxRate = 0.25f;
            MarketPlace.TaxRate = TaxRate;

            this.Wallet.SetAmount(LocalCurrency, 500);

            // Add settlement resource to cell
            Location.AvailableResourceSources.TryAdd(
                GameOracle.Instance.GameData.Resources.Find(pred => pred.Name == "Settlement"),
                1
            );

            // Add default set of factories for a settlement
            foreach( AgentType agentType in this.GameData.AgentTypes )
            {
                if (agentType.Level > 1) continue;

                int iterations = 1;

                if (agentType.Name == "Bake Bread")
                    iterations = 3;

                for (int i = 0; i < iterations; i++)
                {
                    var itemName = agentType.Recipes[0].Outputs[0].ItemName;
                    var newFactory = new Factory($"Factory {i}: {itemName}", agentType);

                    newFactory.Wallet.SetAmount(LocalCurrency, 100);

                    // Add initial inventory supplies for agent
                    foreach (var slot in agentType.StartingInventory)
                    {
                        newFactory.Inventory.AddToInventory(slot.ItemName, slot.Quantity, newFactory.Wage);
                    }

                    Factorys.Add(newFactory);
                }
            }
        }

        private GamePopCenter() : base("PopCenter")
        {
            Populations = new Dictionary<GamePop, int>();
            MarketPlace = new MarketPlace();
            Factorys = new List<Factory>();
        }

        public float CollectIncomeTax(uint turnNumber)
        {
            if (turnNumber <= 1)
                return 0;

            float total = 0;

            foreach( var factory in Factorys )
            {
                if(!factory.WealthAtTurn.ContainsKey(turnNumber -1))
                {
                    continue;
                }

                var income = factory.Wallet.GetAmount(LocalCurrency) - factory.WealthAtTurn[turnNumber - 1].GetAmount(LocalCurrency);

                if( income > 0 )
                {
                    var taxOwed = income * IncomeTaxRate;

                    total += taxOwed;

                    factory.Wallet.RemoveAmount(LocalCurrency, taxOwed);
                    Wallet.AddAmount(LocalCurrency, taxOwed);
                }
            }

            return total;
        }

        public void PayUnemployment()
        {
            foreach (var pop in Populations.Keys)
            {
                var unemployedCount = pop.Locations[this] - pop.EmploymentAtLocations[this];

                if (unemployedCount < 0) continue;

                var payout = unemployedCount * GetAverageWage() * 0.25f;
                pop.WealthAtLocations[this].AddAmount(LocalCurrency, payout);
                Wallet.RemoveAmount(LocalCurrency, payout);
            }
        }

        public float UnemploymentPaymentAmount()
        {
            float total = 0;
            foreach (var pop in Populations.Keys)
            {
                var unemployedCount = pop.Locations[this] - pop.EmploymentAtLocations[this];

                if (unemployedCount < 0) continue;

                total += unemployedCount * GetAverageWage() * 0.25f;
            }

            return total;
        }

        public new void EndTurn(uint turnNumber)
        {
            WealthAtTurn[turnNumber] = new GameAgentWallet( Wallet );

            ProcessConstructionQueue(turnNumber);

            ProcessTravelQueue(turnNumber);

            ProcessSettlerQueue(turnNumber);

            PayUnemployment();

            foreach (var factory in Factorys)
            {
                factory.EndTurn(turnNumber, this);

                if ( (factory.ValueOfOutputGoods() * 1.2f)  + factory.Wallet.GetAmount(LocalCurrency) < 0)
                {
                    Console.WriteLine($"Factory: {factory.Name} went bankrupt");

                    this.Inventory.Merge(factory.Inventory);
                }
            }

            var newBankruptFactories = Factorys.FindAll(pred => pred.ValueOfOutputGoods() + pred.Wallet.GetAmount(LocalCurrency) < 0);
            BankruptFactorys.AddRange(newBankruptFactories);
            foreach(var factory in newBankruptFactories)
            {
                factory.FireAllWorkers(this);
            }
            Factorys.RemoveAll(pred => pred.ValueOfOutputGoods() + pred.Wallet.GetAmount(LocalCurrency) < 0);

            FindProfitableTradeWithNeighbor(turnNumber);

            // Run the marketplace offers
            MarketPlace.EndTurn(turnNumber);

            var totalIncomeTax = CollectIncomeTax(turnNumber);

            // Collect tax from marketplace
            var rd = MarketPlace.RoundDataForTurn(turnNumber);
            var totalSalesTax = rd.Sum(pred => pred.Value.taxCollected);

            Wallet.AddAmount(LocalCurrency, totalSalesTax);
            MarketPlace.Wallet.RemoveAmount( LocalCurrency, totalSalesTax);

            CostOfLastConstruction -= totalSalesTax;
            CostOfLastConstruction -= totalIncomeTax;

            LastTaxCollected = totalIncomeTax + totalSalesTax;

            if (turnNumber > 5 && ProfitOverTurns(turnNumber, 5).GetAmount(LocalCurrency) < 0)
            {
                _turnsSinceLastTaxAdjustment++;
            }
            else if (turnNumber > 5 && ProfitOverTurns(turnNumber, 5).GetAmount(LocalCurrency) > 0)
            {
                _turnsSinceLastTaxAdjustment--;
            }

            if (_turnsSinceLastTaxAdjustment <= -3)
            {
                TaxRate *= 0.9f;
                IncomeTaxRate *= 0.9f;

                _turnsSinceLastTaxAdjustment = 0;
            }

            if (_turnsSinceLastTaxAdjustment >= 3 )
            {
                TaxRate *= 1.1f;
                IncomeTaxRate *= 1.05f;

                _turnsSinceLastTaxAdjustment = 0;
            }

            if (CostOfLastConstruction < 0)
                isPayingOffCosts = false;

            ProcessNewConstructions();

            FindNeighbors();

            FindSettlementLocation();
        }

        public float TotalWealth()
        {
            return Wallet.GetAmount(LocalCurrency) + 
                Inventory.GetTotalWorth() +
                Factorys.Sum(factory => factory.Wallet.GetAmount(LocalCurrency) + factory.Inventory.GetTotalWorth()) + 
                MarketPlace.Wallet.GetAmount(LocalCurrency) + 
                Populations.Keys.Sum(pop => pop.WealthAtLocations[this].GetAmount(LocalCurrency) + pop.InventoryAtLocations[this].GetTotalWorth());
        }

        public float TotalPopulation
        {
            get { return Populations.Values.Sum(); }
        }

        public int UnEmployedPopCount()
        {
            int count = 0;

            foreach( var pop in Populations.Keys )
            {
                count += pop.Locations[this] - pop.EmploymentAtLocations[this];
            }
                        
            return count;
        }

        public float CalculateExchangeRate(GamePopCenter referenceCenter)
        {
            // Calculate the total wealth for the current GamePopCenter
            float totalWealth = TotalWealth();

            // Calculate the total value of goods for the current GamePopCenter
            float totalValueOfGoods = Inventory.GetTotalWorth() + MarketPlace.Inventory.GetTotalWorth() + Factorys.Sum(pred=>pred.Inventory.GetTotalWorth());

            // Calculate the total wealth for the reference GamePopCenter
            float referenceTotalWealth = referenceCenter.Wallet.GetAmount(referenceCenter.LocalCurrency) + referenceCenter.Factorys.Sum(factory => factory.Wallet.GetAmount(referenceCenter.LocalCurrency)) + referenceCenter.MarketPlace.Wallet.GetAmount(referenceCenter.LocalCurrency);

            // Calculate the total value of goods for the reference GamePopCenter
            float referenceTotalValueOfGoods = referenceCenter.Inventory.GetTotalWorth() + referenceCenter.MarketPlace.Inventory.GetTotalWorth() + referenceCenter.Factorys.Sum(pred => pred.Inventory.GetTotalWorth());

            // Calculate the exchange rate by comparing the ratios of total wealth and total value of goods
            float exchangeRate = (totalWealth / totalValueOfGoods) / (referenceTotalWealth / referenceTotalValueOfGoods);

            return exchangeRate;
        }

        public float GetAverageWage()
        {
            return Factorys.Average(pred => pred.Wage);
        }

        public float CalculatePriceDifference(GamePopCenter center2, string itemName)
        {
            float price1 = MarketPlace.GetAveragePrice(itemName);
            float price2 = center2.MarketPlace.GetAveragePrice(itemName);

            return Math.Abs(price1 - price2);
        }

        public void FindSettlementLocation()
        {
            if (TotalPopulation < 300) return;
            if (SettlerQueue.Count > 0) return;

            var suitableLocations = GameOracle.Instance.gameMap.GetSuitablePopStartPoints(VisitedTiles);

            suitableLocations.RemoveAll(pred =>
            {
                var surroundingTiles = GameOracle.Instance.gameMap.GetTilesWithinRadius(pred.position, 3);
                return surroundingTiles.Exists(a => GameOracle.Instance.IsPopCenterAtLocation(a.position));
            });

            if (suitableLocations.Count == 0)
                return;

            // Create a temp game agent to serve as the traveler
            var caravan = new GamePopGroup("Settler")
            {
                Location = this.Location
            };

            var firstPop = Populations.Keys.ToList()[0];

            if (!caravan.Populations.TryAdd(firstPop, 100))
                caravan.Populations[firstPop] += 100;

            firstPop.RemovePopFromLocation(this, 100);
            Populations[firstPop] -= 100;

            var computedPath = Pathfinder.FindPath(Location, suitableLocations[0]);

            if (computedPath == null) return;

            TravelTimer travelTimer = new TravelTimer(caravan, computedPath);

            Console.WriteLine($"Settler created");

            SettlerQueue.Add(travelTimer);
        }
        public void FindNeighbors()
        {
            if( Inventory.ContainsItemAndQtyOfType(ItemType.Trade, initialSearchRadius) )
            {
                var tradeItem = Inventory.GetItemOfType(ItemType.Trade);

                var gameMap = GameOracle.Instance.gameMap;
                var tilesInRadius = gameMap.GetTilesWithinRadius(Location.position, initialSearchRadius);

                tilesInRadius.RemoveAll(pred => !pred.Collidable);

                // Start an expedition to explore a distant tile
                var unvisitedTiles = tilesInRadius.Except(VisitedTiles).ToList();

                if (unvisitedTiles.Count == 0)
                {
                    initialSearchRadius += 1;
                    return;
                }

                // Pick a random tile to visit
                var randomUnvisitedTile = unvisitedTiles[StaticRandom.Instance.Next(unvisitedTiles.Count)];
                
                // Get a path to the tile
                var computedPath = Pathfinder.FindPath(Location, randomUnvisitedTile);

                if (computedPath == null) return;

                // Pay for the journey (trade items like transportation are used to pay for the trip)
                var distanceInTiles = computedPath.Count;
                Inventory.RemoveFromInventory(tradeItem.Name, distanceInTiles);

                // Create a temp game agent to serve as the traveler
                var caravan = new GamePopGroup("Scout")
                {
                    Location = this.Location
                };

                var firstPop = Populations.Keys.ToList()[0];
                if ( !caravan.Populations.TryAdd(firstPop, 10 ) )
                    caravan.Populations[firstPop] += 10;

                firstPop.RemovePopFromLocation( this, 10 );
                Populations[firstPop] -= 10;
                
                TravelTimer travelTimer = new TravelTimer(caravan, computedPath);

                Console.WriteLine($"Caravan created");

                TravelQueue.Add(travelTimer);
            } 
            else // Purchase goods for launching an expedition
            {
                var amtToPurchase = initialSearchRadius;

                var itemToBuy = MarketPlace.GetAvailableItemWithType(ItemType.Trade);

                if (itemToBuy == null) return;

                var averageSellPrice = MarketPlace.GetAverageSellPrice(itemToBuy.Name, 2);

                // Determine the maximum quantity that can be afforded by the buyer
                int maxAffordableQuantity = (int)(Wallet.GetAmount(LocalCurrency) / averageSellPrice);

                // Use the minimum of the affordable quantity and the requested quantity
                int quantityToProcess = (int)Math.Min(amtToPurchase * 1.5f, maxAffordableQuantity);

                if (quantityToProcess < 1)
                    return;

                Offer offer = new Offer(itemToBuy.Name, averageSellPrice * 1.1f, quantityToProcess, OfferType.Buy, LocalCurrency)
                {
                    owner = this
                };

                MarketPlace.PlaceOffer(offer);
                OffersFromLastTurn.Add(offer);
            }
        }

        public Tuple<AgentType, float> DetermineNewFactory()
        {
            AgentType bestFactory = null;
            float bestRatio = 0;
            MarketPlace market = MarketPlace;

            // Iterate through potential factories
            foreach (AgentType agentType in GameData.AgentTypes)
            {
                // Check if the required resources for the factory are available in the tile
                bool resourcesAvailable = true;

                string product = null;

                foreach (Recipe recipe in agentType.Recipes) {
                    product = recipe.Outputs[0].ItemName;

                    foreach (Resource requiredResource in recipe.ResourcesRequiredAsObjects)
                    {
                        if (!Location.AvailableResourceSources.ContainsKey(requiredResource))
                        {
                            resourcesAvailable = false;
                            break;
                        }
                    }
                }

                // If the required resources are not available, skip this factory
                if (!resourcesAvailable)
                {
                    continue;
                }

                // Ignore an agent type we already building
                if (ConstructionQueue.Exists(pred => pred.Item1.agentType.Name == agentType.Name))
                    continue;

                // Calculate the ratio of quantities bought and sold for the product produced by the factory
               
                int buyQty = market.GetTotalBuyQuantity(product);
                int sellQty = market.GetTotalSellQuantity(product);

                // Prevent division by zero
                if (sellQty == 0)
                {
                    sellQty = 1;
                }

                float ratio = (float)buyQty / sellQty;

                // Calculate if the factory can be afforded to build
                float costToBuildAgent = CostToBuildAgent(agentType);

                // If the ratio is better than the current best ratio, update the best factory and best ratio
                if (ratio > 2 && ratio > bestRatio && Wallet.GetAmount(LocalCurrency) > costToBuildAgent && agentType.RequiredWorkers < this.UnEmployedPopCount())
                {
                    bestFactory = agentType;
                    bestRatio = ratio;
                }
            }

            return new Tuple<AgentType, float>(bestFactory, bestRatio);
        }

        float CostToBuildAgent(AgentType agent)
        {
            float cost = 0;

            foreach (var recipe in agent.Recipes)
            {
                cost += MarketPlace.EstimateRecipeCost(recipe);
            }

            return cost;
        }

        void ProcessNewConstructions()
        {
            if (ConstructionQueue.Count >= MaxConstructionQueueSize)
                return;

            if( isPayingOffCosts )
            {
                // Haven't paid off the last construction yet
                return;
            }

            (var bestAgentToCreate, var bestRatio) = DetermineNewFactory().ToValueTuple();

            if (bestAgentToCreate == null)
            {
                Console.WriteLine("No best agent found");
                return;
            }

            if (bestRatio < 4)
            {
                Console.WriteLine("Best ratio below 10");
                return;
            }

            if (ConstructionQueue.Exists(pred=>pred.Item1.agentType.Name == bestAgentToCreate.Name))
            {
                Console.WriteLine("Already creating agent of same type");
                return;
            }

            Console.WriteLine(bestRatio.ToString());

            var newFactory = new Factory(bestAgentToCreate.RecipeNames[0] + " " + Factorys.Count.ToString(), bestAgentToCreate);

            float costToBuildAgent = CostToBuildAgent(bestAgentToCreate) * 5;

            costToBuildAgent += (GetAverageWage() * bestAgentToCreate.RequiredWorkers) * 5;

            CostOfLastConstruction += costToBuildAgent;

            newFactory.Wallet.SetAmount(LocalCurrency, costToBuildAgent);
            Wallet.RemoveAmount(LocalCurrency, costToBuildAgent);

            Console.WriteLine($"Building new factory: {newFactory.Name} with cost of {costToBuildAgent}");

            ConstructionQueue.Add( new ConstructionTimer( newFactory, bestAgentToCreate.TurnsToBuild));

            if( ConstructionQueue.Count == MaxConstructionQueueSize )
                isPayingOffCosts = true;
        }

        void ProcessConstructionQueue(uint turnNumber)
        {
            // Process construction queue
            for (int i = 0; i < ConstructionQueue.Count; i++)
            {
                (var factory, var timer) = ConstructionQueue[i].ToValueTuple();

                timer -= 1;
                factory.BuyGoods(turnNumber, this);

                ConstructionQueue[i] = new ConstructionTimer(factory, timer);

                if (timer == 0)
                {
                    var existingFactory = Factorys.Find(pred => pred.agentType == factory.agentType);

                    if (existingFactory == null)
                    {
                        Factorys.Add(factory);
                        Console.WriteLine($"Factory finished: {factory.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"Factory level updated: {factory.Name}");
                        existingFactory.Inventory.Merge(factory.Inventory);
                        existingFactory.Wallet += factory.Wallet;
                        existingFactory.Level += 1;
                    }
                }
            }
            ConstructionQueue.RemoveAll((conTimer) => conTimer.Item2 == 0);
        }

        void FindProfitableTradeWithNeighbor(uint turnNumber)
        {
            foreach( var neighbor in KnownNeighbors )
            {
                // Calculate the exchange rate
                var exchangeRate = CalculateExchangeRate( neighbor );

                if( exchangeRate > 1 )
                {
                    // Find an item to trade that is in demand locally and has a surplus in the neighbor
                    var itemWithDemand = MarketPlace.GetHottestGood();
                    var itemPriceInNeighborCurrency = neighbor.MarketPlace.GetAverageSellPrice(itemWithDemand, 3);
                    var itemPriceInLocalCurrency = itemPriceInNeighborCurrency / exchangeRate;

                    if( itemPriceInLocalCurrency > MarketPlace.GetAverageBuyPrice(itemWithDemand, 3) )
                    {
                        // The item is in demand locally and has a surplus in the neighbor
                        // Find the quantity to trade
                        var quantityToProcess = Math.Min(neighbor.MarketPlace.GetTotalSellQuantity(itemWithDemand), MarketPlace.GetTotalBuyQuantity(itemWithDemand));
                        
                        //// Create the offer
                        //Offer offer = new Offer(itemWithDemand, itemPriceInNeighborCurrency * 1.1f, quantityToProcess, OfferType.Buy, LocalCurrency)
                        //{
                        //    owner = this
                        //};
                 
                        //neighbor.MarketPlace.PlaceOffer(offer);
                    }
                }
            }
        }

        void ProcessTravelQueue(uint turnNumber)
        {
            var timersToRemove = new List<TravelTimer>();

            // Process travel queue
            for (int i = 0; i < TravelQueue.Count; i++)
            {
                (var caravan, List<HexTile> path) = TravelQueue[i].ToValueTuple();

                if( path == null )
                {
                    timersToRemove.Add(TravelQueue[i]);
                    continue;
                }

                var posOnPath = path.FindIndex(pred => pred.position == caravan.Location.position);

                if( posOnPath == -1 )
                {
                    // Caravan is not on the path
                    // Remove the caravan from the travel queue
                    timersToRemove.Add(TravelQueue[i]);
                    continue;
                }

                // Record the tile visited
                if (!VisitedTiles.Contains(path[posOnPath]))
                    VisitedTiles.Add(path[posOnPath]);

                if (posOnPath == path.Count - 1)
                {
                    if (path[posOnPath] == this.Location)
                        timersToRemove.Add(TravelQueue[i]);
                    else
                        path.Reverse();

                    continue;
                }

                Console.WriteLine($"Caravan traveling");

                caravan.Location = path[++posOnPath];

                if (GameOracle.Instance.IsPopCenterAtLocation( caravan.Location.position ) )
                {
                    KnownNeighbors.Add(GameOracle.Instance.GetPopCenterAtLocation(caravan.Location.position));
                }

                TravelQueue[i] = new TravelTimer(caravan, path);
            }

            TravelQueue.RemoveAll((travelTimer) => {
                bool removeTimer = timersToRemove.Contains(travelTimer);
                
                if( removeTimer )
                {
                    Console.WriteLine($"Caravan arrived");

                    (var caravan, var path) = travelTimer.ToValueTuple();

                    var firstPop = Populations.Keys.ToList()[0];
                    firstPop.AddPopToLocation(this, caravan.Populations[firstPop]);
                    Populations[firstPop] += caravan.Populations[firstPop];
                }

                return removeTimer;
            });

        }

        void ProcessSettlerQueue(uint turnNumber)
        {
            var timersToRemove = new List<TravelTimer>();

            // Process travel queue
            for (int i = 0; i < SettlerQueue.Count; i++)
            {
                (var caravan, List<HexTile> path) = SettlerQueue[i].ToValueTuple();

                var posOnPath = path.FindIndex(pred => pred.position == caravan.Location.position);

                if (posOnPath == path.Count - 1)
                {
                    timersToRemove.Add(SettlerQueue[i]);
                    continue;
                }

                Console.WriteLine($"Settler traveling");

                caravan.Location = path[++posOnPath];

                SettlerQueue[i] = new TravelTimer(caravan, path);
            }

            SettlerQueue.RemoveAll((travelTimer) => {
                bool removeTimer = timersToRemove.Contains(travelTimer);

                if (removeTimer)
                {
                    Console.WriteLine($"Settler arrived");

                    (var caravan, var path) = travelTimer.ToValueTuple();

                    var firstPop = caravan.Populations.Keys.ToList()[0];
                    //firstPop.AddPopToLocation(this, caravan.Populations[firstPop]);

                    var newPopCenterName = GameOracle.Instance.GameNameGenerators.BiomeNameGenerators[path.Last().BiomeType].GenerateName(3, 10, 0, null, StaticRandom.Instance);

                    while (newPopCenterName == null)
                    {
                        newPopCenterName = GameOracle.Instance.GameNameGenerators.BiomeNameGenerators[path.Last().BiomeType].GenerateName(3, 10, 0, null, StaticRandom.Instance);
                    }

                    GamePopCenter newPopCenter = new GamePopCenter(
                         newPopCenterName,
                         path.Last(),
                         new Dictionary<GamePop, int>(),
                         WorldSimAPI.SettlementType.HunterGather
                    );                    
                    
                    firstPop.AddPopToLocation(newPopCenter, caravan.Populations[firstPop]);
                    firstPop.WealthAtLocations[newPopCenter] += caravan.Wallet;

                    newPopCenter.Populations.Add(firstPop, caravan.Populations[firstPop]);

                    GameOracle.Instance.PopCenters.Add(newPopCenter);

                    KnownNeighbors.Add(newPopCenter);
                    newPopCenter.KnownNeighbors.Add(this);
                }

                return removeTimer;
            });

        }

        public string ToMarkdown()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"# GamePopCenter: {Name}");
            sb.AppendLine($"* **Location:** \n\t\t{Location.ToString().Replace("\n", "\n\t\t\t")}");
            sb.AppendLine($"* **Settlement Level:** {SettlementLevel}");
            sb.AppendLine($"* **Population Count:** {Populations.Count}");
            sb.AppendLine($"* **Total Population:** {Populations.Keys.ToList().Sum(pred => pred.Locations[this])}");
            sb.AppendLine($"* **Unemployed Population:** {UnEmployedPopCount()}");
            sb.AppendLine($"* **Unemployment Payment:** {UnemploymentPaymentAmount()}");
            sb.AppendLine($"* **Factory Count:** {Factorys.Count}");
            sb.AppendLine($"* **Construction Queue Size:** {ConstructionQueue.Count}/{MaxConstructionQueueSize}");
            sb.AppendLine($"* **Tax Rate:** {TaxRate * 100}%");
            sb.AppendLine($"* **Last Tax Collected:** {LastTaxCollected}");
            sb.AppendLine($"* **Wealth:** {Wallet.GetAmount(LocalCurrency)}");
            sb.AppendLine($"* **Total Wealth:** {TotalWealth()}");
            sb.AppendLine($"* **Total Wages Paid Out:** {Factorys.Sum(pred => pred.TotalWages())}");


            sb.AppendLine("\n## Marketplace");
            sb.AppendLine($"{MarketPlace.ToMarkdown()}");

            sb.AppendLine("\n## Inventory:");
            sb.AppendLine("| Item Name | Cost | Cost Per Unit | Quantity | Original Quantity |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var item in Inventory.ItemsContainer)
            {
                foreach (var record in item.Value)
                {
                    sb.AppendFormat("| {0} | {1} | {2} | {3} | {4} |\n",
                        record.ItemName,
                        record.Cost.ToString("$##.##"),
                        record.CostPerUnit.ToString("$##.##"),
                        record.Quantity,
                        record.OriginalQuantity);
                }
            }

            sb.AppendLine($"* **Populations:** {Populations.Count}");

            foreach (var pop in Populations)
            {
                sb.AppendLine(pop.Key.ToMarkdown());
            }

            sb.AppendLine("\n## Factories:");
            foreach (var factory in Factorys)
            {
                sb.AppendLine(factory.ToMarkdown());
            }

            sb.AppendLine("\n## Bankrupt Factories:");
            foreach (var factory in BankruptFactorys)
            {
                sb.AppendLine(factory.ToMarkdown());
            }

            //sb.AppendLine("\n## Visited Tiles:");
            //foreach (var tile in VisitedTiles)
            //{
            //    sb.AppendLine(tile.ToString().Replace("\n", "\n\t\t"));
            //}

            return sb.ToString();
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"GamePopCenter: {Name}");
            sb.AppendLine($"\tLocation: \n\t\t{Location.ToString().Replace("\n", "\n\t\t\t")}");
            sb.AppendLine($"\tSettlement Level: {SettlementLevel}");
            sb.AppendLine($"\tPopulation Count: {Populations.Count}");
            sb.AppendLine($"\tTotal Population: {Populations.Keys.ToList().Sum(pred => pred.Locations[this])}");
            sb.AppendLine($"\tUnemployed Population: {UnEmployedPopCount()}");
            sb.AppendLine($"\tUnemployment Payment: {UnemploymentPaymentAmount()}");
            sb.AppendLine($"\tFactory Count: {Factorys.Count}");
            sb.AppendLine($"\tConstruction Queue Size: {ConstructionQueue.Count}/{MaxConstructionQueueSize}");
            sb.AppendLine($"\tTax Rate: {TaxRate * 100}%");
            sb.AppendLine($"\tLast Tax Collected: {LastTaxCollected}");
            sb.AppendLine($"\tWealth: {Wallet.GetAmount(LocalCurrency)}");
            sb.AppendLine($"\tTotal Wealth: {TotalWealth()}");
            sb.AppendLine($"\tTotal Wages Paid Out: {Factorys.Sum(pred=>pred.TotalWages())}");

            sb.AppendLine($"\n\tMarketplace: {MarketPlace.ToString()}");

            sb.AppendLine("\n\tInventory:");
            foreach (var item in Inventory.ItemsContainer)
            {
                sb.AppendFormat("\t\t{0} x {1}\n", item.Key.ToString(), item.Value);
            }

            sb.AppendLine("\n\tPopulations:");
            foreach (var pop in Populations)
            {
                sb.AppendLine("\t\t" + pop.ToString().Replace("\n", "\n\t\t"));
            }

            sb.AppendLine("\n\tFactories:");
            foreach (var factory in Factorys)
            {
                sb.AppendLine("\t\t" + factory.ToString().Replace("\n", "\n\t\t"));
            }

            sb.AppendLine("\n\tVisited Tiles:");
            foreach (var tile in VisitedTiles)
            {
                sb.AppendLine("\t\t" + tile.ToString().Replace("\n", "\n\t\t"));
            }

            return sb.ToString();
        }

    }
}
