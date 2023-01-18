using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using WorldSimLib.DataObjects;
using WorldSimLib.Utils;

namespace WorldSimLib
{
    public class GameAgentOld
    {
        AgentType agentType;

        public MarketPlace _marketPlace;

        public float Gold { get; set; }


        //  protected Community _community;

        public string name;
        public float gold;

        public static float SIGNIFICANT = 0.25f;        //25% more or less is "significant"
        public static float SIG_IMBALANCE = 0.33f;
        public static float LOW_INVENTORY = 0.1f;       //10% of ideal inventory = "LOW"
        public static float HIGH_INVENTORY = 2.0f;  //200% of ideal inventory = "HIGH"

        public static float MIN_PRICE = 0.01f;      //lowest possible price

        Dictionary<string, Vector2> _priceBeliefs;

        public GameAgentOld(string name, AgentType aType, GameOracle oracle)
        {
            agentType = aType;

            //_community = community;
            //_marketPlace = community.MarketPlace;

            Init();

            //  PlayerData.gold = agentType.StartingGold;

            //// Add initial inventory supplies for agent
            //foreach (var slot in agentType.StartingInventory)
            //{
            //    _inventory.AddToInventory(slot.ItemName, slot.Quantity);
            //}
        }


        protected void Init()
        {
            _priceBeliefs = new Dictionary<string, Vector2>();

            //var listOfItems = _oracle.GameData.Items;
            //foreach (Item item in listOfItems)
            //{
            //    float price = _marketPlace.GetAveragePrice(item.Name);

            //    _priceBeliefs.Add(item.Name, new Vector2(price * 5f, price * 10f));
            //}
        }

        public new void ProcessTurn()
        {
            //// Process our agents recipe
            //foreach (Recipe recipe in agentType.Recipes)
            //{
            //    //_inventory.ProcessRecipe(recipe);
            //}

            //// Buy goods for the recipes
            //foreach (Recipe recipe in agentType.Recipes)
            //{
            //    foreach (var input in recipe.Inputs)
            //    {
            //        Offer offer = CreateBuy(input.ItemName, input.IdealQuantity);

            //        if (offer == null)
            //            continue;

            //        _marketPlace.PlaceOffer(offer);
            //    }
            //}

            //// Sell goods that we produced
            //foreach (Recipe recipe in agentType.Recipes)
            //{
            //    foreach (var output in recipe.Outputs)
            //    {
            //        Offer offer = CreateSell(output.ItemName, 1);

            //        if (offer == null)
            //            continue;

            //        var stocks = _inventory.GetQuantityOfItem(output.ItemName);
            //        var ideal = CalculateIdealItemQtyToHold(output.ItemName);

            //        if (stocks > HIGH_INVENTORY * ideal)
            //        {
            //            offer.IsMarketOrder = true;
            //            //Debug.Log("Setting marketing order XXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
            //        }

            //        _marketPlace.PlaceOffer(offer);
            //    }
            //}
        }

        public void EndTurn(uint turnNumber)
        {
            // ProcessTurn();
        }

        //    public int CalculateIdealItemQty(string itemName)
        //    {
        //        int idealQty = 0;

        //        foreach (Recipe recipe in agentType.Recipes)
        //        {
        //            foreach (var input in recipe.Inputs)
        //            {
        //                if (input.ItemName == itemName)
        //                    idealQty += input.IdealQuantity;
        //            }
        //        }

        //        return idealQty;
        //    }

        //    public int CalculateIdealItemQtyToHold(string itemName)
        //    {
        //        int idealQty = 0;

        //        // How much of this item do we need for recipes
        //        var neededForRecipes = CalculateIdealItemQty(itemName);

        //        foreach (Recipe recipe in agentType.Recipes)
        //        {
        //            foreach (var ouput in recipe.Outputs)
        //            {
        //                if (ouput.ItemName == itemName)
        //                    idealQty += ouput.Quantity;
        //            }
        //        }

        //        idealQty -= neededForRecipes;

        //        return idealQty;
        //    }

        //    public void UpdatePriceModel(OfferType oType, string itemName, bool success, float unitPrice = 0)
        //    {
        //        var itemRecipe = _gameData.RecipeForItem(itemName);
        //        var estimatedItemCost = _inventory.EstimateRecipeCost(itemRecipe);

        //        var public_mean_price = _marketPlace.GetAveragePrice(itemName);

        //        var belief = _priceBeliefs[itemName];
        //        var mean = (belief.X + belief.Y) / 2;
        //        var wobble = 0.05f;

        //        var delta_to_mean = mean - public_mean_price;

        //        if (success)
        //        {
        //            if (oType == OfferType.Sell && unitPrice < estimatedItemCost)      //sold at a loss, adjust the price belief
        //            {
        //                belief.X = estimatedItemCost;                          //SHIFT towards mean
        //                belief.Y = estimatedItemCost * 1.5f;

        //            }
        //            else
        //            {
        //                if (oType == OfferType.Buy && delta_to_mean > SIGNIFICANT)          //overpaid
        //                {
        //                    belief.X -= delta_to_mean / 2;                          //SHIFT towards mean
        //                    belief.Y -= delta_to_mean / 2;
        //                }
        //                else if (oType == OfferType.Sell && delta_to_mean < -SIGNIFICANT && unitPrice > estimatedItemCost)      //undersold but still made a profit
        //                {
        //                    belief.X -= delta_to_mean / 2;                          //SHIFT towards mean
        //                    belief.Y -= delta_to_mean / 2;
        //                }

        //                belief.X += wobble * mean;   //increase the belief's certainty
        //                belief.Y -= wobble * mean;
        //            }
        //        }
        //        else
        //        {
        //            belief.X -= delta_to_mean / 2;  //SHIFT towards the mean
        //            belief.Y -= delta_to_mean / 2;

        //            var special_case = false;
        //            var stocks = _inventory.GetQuantityOfItem(itemName);

        //            var ideal = CalculateIdealItemQty(itemName);

        //            if (oType == OfferType.Sell)
        //                ideal = CalculateIdealItemQtyToHold(itemName);

        //            if (oType == OfferType.Buy && stocks < LOW_INVENTORY * ideal)
        //            {
        //                //very low on inventory AND can't buy
        //                wobble *= 2;            //bid more liberally
        //                special_case = true;
        //            }
        //            else if (oType == OfferType.Sell && stocks > HIGH_INVENTORY * ideal)
        //            {
        //                //very high on inventory AND can't sell, sell a market order
        //                wobble *= 2;            //ask more liberally
        //                special_case = true;
        //            }

        //            if (!special_case)
        //            {
        //                //Don't know what else to do? Check supply vs. demand
        //                var asks = _marketPlace.GetAverageSellPrice(itemName);
        //                var bids = _marketPlace.GetAverageBuyPrice(itemName);

        //                //supply_vs_demand: 0=balance, 1=all supply, -1=all demand
        //                var supply_vs_demand = (asks - bids) / (asks + bids);

        //                //too much supply, or too much demand
        //                if (supply_vs_demand > SIG_IMBALANCE || supply_vs_demand < -SIG_IMBALANCE)
        //                {
        //                    //too much supply: lower price
        //                    //too much demand: raise price

        //                    var new_mean = public_mean_price * (1 - supply_vs_demand);
        //                    delta_to_mean = mean - new_mean;

        //                    belief.X -= delta_to_mean / 2;  //SHIFT towards anticipated new mean
        //                    belief.Y -= delta_to_mean / 2;
        //                }
        //            }

        //            belief.X -= wobble * mean;   //decrease the belief's certainty
        //            belief.Y += wobble * mean;
        //        }

        //        if (belief.X < MIN_PRICE)
        //        {
        //            belief.X = MIN_PRICE;
        //        }
        //        else if (belief.Y < MIN_PRICE)
        //        {
        //            belief.Y = MIN_PRICE;
        //        }
        //    }

        //    private int DeterminePurchaseQuantity(string itemName, int shortage)
        //    {
        //        var mean = _marketPlace.GetAveragePrice(itemName);
        //        var trading_range = _marketPlace.GetHistoricalTradingRange(itemName);

        //        if (trading_range != null)
        //        {
        //            float favorability = mean.positionInRange(trading_range.X, trading_range.Y);
        //            favorability = 1 - favorability;
        //            //do 1 - favorability to see how close we are to the low end

        //            int amount_to_buy = (int)Math.Round(favorability * shortage);
        //            if (amount_to_buy < 1)
        //            {
        //                amount_to_buy = 1;
        //            }
        //            return amount_to_buy;
        //        }
        //        return 0;
        //    }

        //    private int DetermineSaleQuantity(string itemName, int surplus)
        //    {
        //        var mean = _marketPlace.GetAveragePrice(itemName);
        //        var trading_range = _marketPlace.GetHistoricalTradingRange(itemName);

        //        if (trading_range != null)
        //        {
        //            float favorability = mean.positionInRange(trading_range.X, trading_range.Y);
        //            favorability = 1 - favorability;
        //            //do 1 - favorability to see how close we are to the low end

        //            int amount_to_buy = (int)Math.Round(favorability * surplus);
        //            if (amount_to_buy < 1)
        //            {
        //                amount_to_buy = 1;
        //            }
        //            return amount_to_buy;
        //        }
        //        return 0;
        //    }

        //    public Offer CreateBuy(string itemName, int idealQty, Recipe recipe = null)
        //    {
        //        float buyPrice = DeterminePriceOf(itemName);
        //        int ideal = DeterminePurchaseQuantity(itemName, _inventory.Shortage(itemName, idealQty));

        //        var costToMake = _marketPlace.EstimateRecipeCost(GameOracle.Instance.GameData.RecipeForItem(itemName));

        //        var currentMarketSellingPrice = _marketPlace.GetAverageSellPrice(itemName);
        //        var currentMarketClosingPrice = _marketPlace.GetAveragePrice(itemName);
        //        var currentMarketBuyingPrice = _marketPlace.GetAverageBuyPrice(itemName);

        //        var priceFavorability = currentMarketClosingPrice.positionInRange(currentMarketBuyingPrice, currentMarketSellingPrice);

        //        // Over 0.5 indicates the average closing price is closer to the average sell price
        //        // Under 0.5 indicate the average closing price is closer to the average purchase price

        //        //if (buyPrice < costToMake)
        //        //  buyPrice = costToMake;

        //        if (currentMarketClosingPrice == 1)
        //        {
        //            currentMarketClosingPrice = currentMarketSellingPrice;
        //        }

        //        if (currentMarketClosingPrice <= costToMake) // Price to buy the item is less than cost to make it, its a good deal
        //        {
        //            buyPrice = StaticRandom.Instance.Range(currentMarketClosingPrice * 0.9f, currentMarketClosingPrice * 1.1f);
        //        }
        //        else // Cost to make the item is less than selling price
        //        {
        //            buyPrice = StaticRandom.Instance.Range(costToMake * 0.9f, costToMake * 1.1f);
        //        }

        //        // See how much it would cost to make our recipe 
        //        var costToMakeRecipeWithItem = 0.0f;
        //        if (recipe != null)
        //        {

        //        }

        //        //Debug.LogFormat("Buy Created: {0} : {1} : {2}", itemName, ideal, buyPrice);

        //        int inventoryQty = _inventory.GetQuantityOfItem(itemName);

        //        // Limit would be how much room you have in the whole inventory
        //        // We don't track size so we don't care.
        //        int qtyToBuy = ideal;

        //        if (qtyToBuy > 0 && PlayerData.gold > qtyToBuy * buyPrice)
        //        {
        //            var offer = new Offer(itemName, buyPrice, qtyToBuy, OfferType.Buy);
        //            offer.owner = this;

        //            return offer;
        //        }

        //        return null;
        //    }

        //    public Offer CreateSell(string itemName, float limit)
        //    {
        //        float sellPrice = DeterminePriceOf(itemName);
        //        var currentMarketClosingPrice = _marketPlace.GetAveragePrice(itemName, 10);

        //        var buyOrdersLastRound = _marketPlace.BuysOverPeriod(1);

        //        // Never sell below what it cost to make
        //        var costToMake = _inventory.EstimateRecipeCost(_gameData.RecipeForItem(itemName));

        //        //if (sellPrice < costToMake)
        //        sellPrice = costToMake;

        //        if (sellPrice < currentMarketClosingPrice)
        //            sellPrice = StaticRandom.Instance.Range(currentMarketClosingPrice * 0.9f, currentMarketClosingPrice * 1.1f);

        //        int ideal = DetermineSaleQuantity(itemName, _inventory.Surplus(itemName));

        //        //Debug.LogFormat("Sell Created: {0} : {1} : {2}", itemName, ideal, sellPrice);

        //        //can't sell less than limit
        //        int qtyToSell = ideal;// ideal<limit_? limit_ : ideal;
        //        if (qtyToSell > 0)
        //        {
        //            var offer = new Offer(itemName, sellPrice, qtyToSell, OfferType.Sell);
        //            offer.owner = this;

        //            return offer;
        //        }
        //        return null;
        //    }

        //    protected float DeterminePriceOf(string itemName)
        //    {
        //        var belief = _priceBeliefs[itemName];
        //        return StaticRandom.Instance.Range(belief.X, belief.Y);
        //    }
        //}
    }
}