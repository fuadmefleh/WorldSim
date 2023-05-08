using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

    public class TravelTimer : Tuple<GameCaravan, List<HexTile>>
    {
        public TravelTimer(GameCaravan item1, List<HexTile> item2) : base(item1, item2)
        {

        }
    }

    public class GamePopCenter : GameAgent
    {
        public HexTile Location { get; set; }
        public List<GamePop> Populations { get; set; }

        public SettlementType SettlementLevel { get; set; }

        public MarketPlace MarketPlace { get; set; }

        public List<Factory> Factorys { get; set; }

        public List<ConstructionTimer> ConstructionQueue { get; set; } = new List<ConstructionTimer>();
        public List<TravelTimer> TravelQueue { get; set; } = new List<TravelTimer>();

        public int MaxConstructionQueueSize { get; set; } = 3;

        GameData GameData { get { return GameOracle.Instance.GameData; } }

        public float TaxRate { get; set; }
        public float IncomeTaxRate { get; set; }
        float LastTaxCollected { get; set; } = 0;

        bool isPayingOffCosts = false;

        float CostOfLastConstruction = 0;

        int initialSearchRadius = 3;
        public List<GamePopCenter> KnownNeighbors = new List<GamePopCenter>();
        public List<HexTile> VisitedTiles = new List<HexTile>();

        public GamePopCenter(string name, HexTile location, List<GamePop> populations, SettlementType settlementLevel) : base(name)
        {
            Location = location;
            Populations = populations;
            SettlementLevel = settlementLevel;

            MarketPlace = new MarketPlace();
            Factorys = new List<Factory>();

            TaxRate = 0.05f; // 5% tax rate per transaction
            IncomeTaxRate = 0.15f;
            MarketPlace.TaxRate = TaxRate;

            Wealth = 500;

            // Add settlement resource to cell
            Location.AvailableResourceSources.Add(
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

                    newFactory.Wealth = 100;

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
            Populations = new List<GamePop>();
            MarketPlace = new MarketPlace();
            Factorys = new List<Factory>();
        }

        public float CollectIncomeTax()
        {
            float total = 0;

            foreach( var factory in Factorys )
            {
                var income = factory.Wealth - factory.previousWealth;

                if( income > 0 )
                {
                    var taxOwed = income * IncomeTaxRate;

                    total += taxOwed;

                    factory.Wealth -= taxOwed;
                    Wealth += taxOwed;
                }
            }

            return total;
        }

        public void PayUnemployment()
        {
            foreach (var pop in Populations)
            {
                var unemployedCount = pop.Locations[this] - pop.EmploymentAtLocations[this];

                if (unemployedCount < 0) continue;

                var payout = unemployedCount * GetAverageWage() * 0.25f;
                pop.WealthAtLocations[this].Amount += payout;
                this.Wealth -= payout;
            }
        }

        public float UnemploymentPaymentAmount()
        {
            float total = 0;
            foreach (var pop in Populations)
            {
                var unemployedCount = pop.Locations[this] - pop.EmploymentAtLocations[this];

                if (unemployedCount < 0) continue;

                total += unemployedCount * GetAverageWage() * 0.25f;
            }

            return total;
        }

        public new void EndTurn(uint turnNumber)
        {
            ProcessConstructionQueue(turnNumber);

            ProcessTravelQueue(turnNumber);

            PayUnemployment();

            foreach (var factory in Factorys)
            {
                factory.EndTurn(turnNumber, this);

                if (factory.ValueOfOutputGoods() + factory.Wealth < 0)
                {
                    Console.WriteLine($"Factory: {factory.Name} went bankrupt");

                    this.Inventory.Merge(factory.Inventory);
                }
            }

            Factorys.RemoveAll(pred => pred.ValueOfOutputGoods() + pred.Wealth < 0);

            // Run the marketplace offers
            MarketPlace.EndTurn(turnNumber);

            var totalIncomeTax = CollectIncomeTax();

            // Collect tax from marketplace
            var rd = MarketPlace.RoundDataForTurn(turnNumber);
            var totalSalesTax = rd.Sum(pred => pred.Value.taxCollected);

            Wealth += totalSalesTax;
            MarketPlace.Wealth -= totalSalesTax;

            CostOfLastConstruction -= totalSalesTax;
            CostOfLastConstruction -= totalIncomeTax;

            LastTaxCollected = totalIncomeTax + totalSalesTax;

            if (CostOfLastConstruction < 0)
                isPayingOffCosts = false;

            ProcessNewConstructions();

            FindNeighbors();
        }

        public float TotalWealth()
        {
            return Wealth + Factorys.Sum(factory => factory.Wealth) + MarketPlace.Wealth + Populations.Sum(pop => pop.WealthAtLocations[this].Amount);
        }

        public int UnEmployedPopCount()
        {
            int count = 0;

            foreach( var pop in Populations )
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
            float referenceTotalWealth = referenceCenter.Wealth + referenceCenter.Factorys.Sum(factory => factory.Wealth) + referenceCenter.MarketPlace.Wealth;

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

                // Pay for the journey (trade items like transportation are used to pay for the trip)
                var distanceInTiles = computedPath.Count;
                Inventory.RemoveFromInventory(tradeItem.Name, distanceInTiles);

                // Create a temp game agent to serve as the traveler
                var caravan = new GameCaravan("Scout")
                {
                    Location = this.Location
                };

                if( !caravan.Populations.TryAdd( Populations[0], 10 ) )
                    caravan.Populations[Populations[0]] += 10;

                this.Populations[0].RemovePopFromLocation( this, 10 );
                
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
                int maxAffordableQuantity = (int)(Wallet.Amount / averageSellPrice);

                // Use the minimum of the affordable quantity and the requested quantity
                int quantityToProcess = (int)Math.Min(amtToPurchase * 1.5f, maxAffordableQuantity);

                if (quantityToProcess < 1)
                    return;

                Offer offer = new Offer(itemToBuy.Name, averageSellPrice * 1.1f, quantityToProcess, OfferType.Buy)
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
                if (ratio > 2 && ratio > bestRatio && Wealth > costToBuildAgent && agentType.RequiredWorkers < this.UnEmployedPopCount())
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

            newFactory.Wealth = costToBuildAgent;
            Wealth -= costToBuildAgent;

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
                    Factorys.Add(factory);
                    Console.WriteLine($"Factory finished {factory.Name}");
                }
            }
            ConstructionQueue.RemoveAll((conTimer) => conTimer.Item2 == 0);
        }

        void ProcessTravelQueue(uint turnNumber)
        {
            var timersToRemove = new List<TravelTimer>();

            // Process travel queue
            for (int i = 0; i < TravelQueue.Count; i++)
            {
                (var caravan, List<HexTile> path) = TravelQueue[i].ToValueTuple();

                var posOnPath = path.FindIndex(pred => pred.position == caravan.Location.position);

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

                    Populations[0].AddPopToLocation(this, caravan.Populations[Populations[0]]);
                }

                return removeTimer;
            });

        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"GamePopCenter: {Name}");
            sb.AppendLine($"\tLocation: \n\t\t{Location.ToString().Replace("\n", "\n\t\t\t")}");
            sb.AppendLine($"\tSettlement Level: {SettlementLevel}");
            sb.AppendLine($"\tPopulation Count: {Populations.Count}");
            sb.AppendLine($"\tTotal Population: {Populations.Sum(pred => pred.Locations[this])}");
            sb.AppendLine($"\tUnemployed Population: {UnEmployedPopCount()}");
            sb.AppendLine($"\tUnemployment Payment: {UnemploymentPaymentAmount()}");
            sb.AppendLine($"\tFactory Count: {Factorys.Count}");
            sb.AppendLine($"\tConstruction Queue Size: {ConstructionQueue.Count}/{MaxConstructionQueueSize}");
            sb.AppendLine($"\tTax Rate: {TaxRate * 100}%");
            sb.AppendLine($"\tLast Tax Collected: {LastTaxCollected}");
            sb.AppendLine($"\tWealth: {Wealth}");
            sb.AppendLine($"\tTotal Wealth: {TotalWealth()}");

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
