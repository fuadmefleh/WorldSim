using System;
using System.Numerics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldSimLib.DataObjects;

namespace WorldSimLib
{
    public struct RoundData
	{
		public int successfulTrades;       //# of successful trades this round
		public float moneyTraded;          //amount of money traded this round
		public float unitsTraded;            //amount of goods traded this round
		public float avgPrice;               //avg clearing price this round

		public override string ToString()
		{
			string retStr = "Round Data: ";
			retStr += "\nSuccessful Trades: " + successfulTrades.ToString();
			retStr += "\nMoney Traded: " + moneyTraded.ToString();
			retStr += "\nUnits Traded: " + unitsTraded.ToString();
			retStr += "\nAverage Price: " + avgPrice.ToString();
			retStr += "\n";

			return retStr;
		}
	}
	public class MarketPlace
	{
		// List<Offer> allHistoricalOffers;
		List<Offer> allActiveOffers;
		Dictionary<uint, List<Offer>> allHistoricalOffers;
		Dictionary<uint, Dictionary<string, RoundData>> allRoundData;

		Dictionary<uint, Dictionary<uint, List<Offer>>> cachedData;

		float MAX_BID_ASK_SPREAD = 0.25f;

		public MarketPlace()
		{
			allHistoricalOffers = new Dictionary<uint, List<Offer>>();
			allActiveOffers = new List<Offer>();

			allRoundData = new Dictionary<uint, Dictionary<string, RoundData>>();

			cachedData = new Dictionary<uint, Dictionary<uint, List<Offer>>>();
		}

		public Dictionary<uint, List<Offer>> GetCachedDataForTurn(uint turnNumber)
		{
			if (!cachedData.ContainsKey(turnNumber))
			{
				// Create an entry for the data cache
				cachedData.TryAdd(turnNumber, new Dictionary<uint, List<Offer>>());
			}

			return cachedData[turnNumber];
		}

		public new void ProcessTurn()
		{
			// CURRENT AI DIRECTIVES LIST
			// 
		}

		public void EndTurn(uint turnNumber)
		{
			// Create an entry for the round data
			allRoundData.TryAdd(turnNumber, new Dictionary<string, RoundData>());

			var allItems = GameOracle.Instance.GameData.Items;

			foreach (var item in allItems)
			{
				RoundData rd = new RoundData();
				ResolveOffers(item.Name, ref rd);

				allRoundData[turnNumber].TryAdd(item.Name, rd);
			}

			var rdDictionary = allRoundData[turnNumber];

			//update history
			allHistoricalOffers.Add(GameOracle.Instance.TurnNumber, new List<Offer>(allActiveOffers));
			allActiveOffers.Clear();
		}

		public Dictionary<string, RoundData> RoundDataForTurn(uint turnNumber)
		{
			if (allRoundData.ContainsKey(turnNumber))
				return allRoundData[turnNumber];
			else
				return null;
		}

		public List<Offer> OffersOverPeriod(uint lookback)
		{
			// Check if cache already has this data
			var cachedData = GetCachedDataForTurn(GameOracle.Instance.TurnNumber);
			if (cachedData.ContainsKey(lookback))
				return cachedData[lookback];

			uint startTurn = GameOracle.Instance.TurnNumber - lookback;

			if (lookback > GameOracle.Instance.TurnNumber)
				startTurn = 1;

			if (startTurn == 0)
				startTurn = 1;

			List<Offer> allOffersOverPeriod = new List<Offer>();

			for (uint i = startTurn; i < GameOracle.Instance.TurnNumber; i++)
			{
				if (allHistoricalOffers.ContainsKey(i))
					allOffersOverPeriod.AddRange(allHistoricalOffers[i]);
			}

			cachedData.TryAdd(lookback, allOffersOverPeriod);

			return allOffersOverPeriod;
		}

		public List<Offer> BuysOverPeriod(uint lookback)
		{
			List<Offer> allOffersOverPeriod = OffersOverPeriod(lookback);

			return allOffersOverPeriod.FindAll(pred => pred.offerType == OfferType.Buy);
		}

		public List<Offer> SellsOverPeriod(uint lookback)
		{
			List<Offer> allOffersOverPeriod = OffersOverPeriod(lookback);

			return allOffersOverPeriod.FindAll(pred => pred.offerType == OfferType.Sell);
		}

		public float GetAveragePrice(string itemName, uint lookback = 1)
		{
			List<Offer> allOffersOverPeriod = OffersOverPeriod(lookback);

			var allProccessedOffersForItem = allOffersOverPeriod.FindAll(pred => pred.itemName == itemName && pred.IsProcessed);

			float runningTotal = 0.0f;

			allProccessedOffersForItem.ForEach(pred => runningTotal += pred.ClearingPrice);

			if (allProccessedOffersForItem.Count == 0)
				return 1;

			return runningTotal / allProccessedOffersForItem.Count;
		}

		public float GetAverageSellPrice(string itemName, uint lookback = 1)
		{
			var allSellsOverPeriod = SellsOverPeriod(lookback);

			var allSellsForItem = allSellsOverPeriod.FindAll(pred => pred.itemName == itemName);

			if (allSellsForItem.Count == 0)
				return 1;

			float runningTotal = 0.0f;

			allSellsForItem.ForEach(pred => runningTotal += pred.pricePerUnit);

			return runningTotal / allSellsForItem.Count;
		}

		public float GetAverageBuyPrice(string itemName, uint lookback = 1)
		{
			var allOffersOverPeriod = OffersOverPeriod(lookback);

			var allBidsForItem = allOffersOverPeriod.FindAll(pred => pred.itemName == itemName && pred.offerType == OfferType.Buy);

			if (allBidsForItem.Count == 0)
				return 1;

			float runningTotal = 0.0f;

			allBidsForItem.ForEach(pred => runningTotal += pred.pricePerUnit);

			return runningTotal / allBidsForItem.Count;
		}

		public Vector2 GetHistoricalTradingRange(string itemName, uint lookback = 1)
		{
			var allOffersOverPeriod = OffersOverPeriod(lookback);

			var allOffersForItem = allOffersOverPeriod.FindAll(pred => pred.itemName == itemName);
			var allPrices = allOffersForItem.Select(offer => offer.pricePerUnit).ToArray();

			if (allPrices.Length == 0)
				return new Vector2(0, 0);

			return new Vector2(allPrices.Min(), allPrices.Max());
		}

		public float EstimateRecipeCost(Recipe recipe)
		{
			float totalInputMaterialCost = 0.0f;

			foreach (var input in recipe.Inputs)
			{
				var averageCostOfItem = GetAverageSellPrice(input.ItemName, 10);
				totalInputMaterialCost += averageCostOfItem * input.Quantity;
			}

			return totalInputMaterialCost;
		}

		public float HasItemsForRecipeAvailable(Recipe recipe)
		{
			float totalInputMaterialCost = 0.0f;

			foreach (var input in recipe.Inputs)
			{
				var averageCostOfItem = GetAverageSellPrice(input.ItemName);
				totalInputMaterialCost += averageCostOfItem * input.Quantity;
			}

			return totalInputMaterialCost;
		}

		private void ProcessTransaction(Offer buyOffer, Offer sellOffer, float clearing_price, ref RoundData rd)
		{
			int buyer_qty = buyOffer.qty;

			var quantity_traded = Math.Min(sellOffer.qty, buyer_qty);

			if (quantity_traded > 0)
			{
				//transfer the goods for the agreed price
				sellOffer.qty -= quantity_traded;
				buyOffer.qty -= quantity_traded;

				TransferGood(buyOffer.itemName, quantity_traded, clearing_price, sellOffer.owner, buyOffer.owner);
				TransferMoney(quantity_traded * clearing_price, sellOffer.owner, buyOffer.owner);

				//update agent price beliefs based on successful transaction				
				//(buyOffer.owner as GameAgent).UpdatePriceModel(OfferType.Buy, buyOffer.itemName, true, clearing_price);
			//	(sellOffer.owner as GameAgent).UpdatePriceModel(OfferType.Sell, sellOffer.itemName, true, clearing_price);

				//log the stats
				rd.moneyTraded += (quantity_traded * clearing_price);
				rd.unitsTraded += quantity_traded;
				rd.avgPrice += clearing_price;
				rd.successfulTrades++;
			}
		}

		private void ResolveOffers(string itemName, ref RoundData rd)
		{
			if (allActiveOffers.Count == 0)
				return;

			var bids = allActiveOffers.FindAll(pred => (pred.offerType == OfferType.Buy && pred.itemName == itemName));
			var asks = allActiveOffers.FindAll(pred => (pred.offerType == OfferType.Sell && pred.itemName == itemName));

			bids = bids.Shuffle();
			asks = asks.Shuffle();

			bids.Sort(new Offer.OfferDecPriceBasedComparer());
			asks.Sort(new Offer.OfferIncPriceBasedComparer());

			rd.successfulTrades = 0;        //# of successful trades this round
			rd.moneyTraded = 0;          //amount of money traded this round
			rd.unitsTraded = 0;            //amount of goods traded this round
			rd.avgPrice = 0;               //avg clearing price this round
			float numAsks = 0;
			float numBids = 0;

			int failsafe = 0;

			for (int i = 0; i < bids.Count; i++)
			{
				numBids += bids[i].qty;
			}

			for (int i = 0; i < asks.Count; i++)
			{
				numAsks += asks[i].qty;
			}

			//foreach (var sellOrder in asks)
			//{
			//	// Try to find a buyer
			//	foreach (var buyOrder in bids)
			//	{
			//		if (buyOrder.IsProcessed)
			//			continue;

			//		if (Mathf.Abs(buyOrder.pricePerUnit - sellOrder.pricePerUnit) < MAX_BID_ASK_SPREAD)
			//		{
			//			float clearing_price = ExtensionMethods.Average(buyOrder.pricePerUnit, sellOrder.pricePerUnit);

			//			ProcessTransaction(buyOrder, sellOrder, clearing_price, ref rd);

			//			if (buyOrder.qty == 0)     //buyer is out of offered good
			//			{
			//				buyOrder.MarkAsProcessed( clearing_price );
			//			}

			//			if (sellOrder.qty == 0)
			//			{
			//				sellOrder.MarkAsProcessed(clearing_price);
			//				break;
			//			}
			//		}
			//	}
			//}

			//asks.RemoveAll(pred => pred.IsProcessed == true);
			//bids.RemoveAll(pred => pred.IsProcessed == true);

			//	//if( matchedBuyOrder != null )
			//	//{


			//	//	if (sellOrder.qty == 0)        //seller is out of offered good
			//	//	{
			//	//		sellOrder.IsProcessed = true;
			//	//		asks.RemoveAt(0);       //remove ask
			//	//	}
			//	//	if (matchedBuyOrder.qty == 0)     //buyer is out of offered good
			//	//	{
			//	//		matchedBuyOrder.IsProcessed = true;
			//	//		bids.Remove(matchedBuyOrder);       //remove bid
			//	//	}

			//		//var clearing_price = ExtensionMethods.Average(sellOrder.pricePerUnit, matchedBuyOrder.pricePerUnit);
			//}

			// march through and try to clear orders
			while (bids.Count > 0 && asks.Count > 0)        //while both books are non-empty
			{
				Offer buyer = bids[0];
				Offer seller = asks[0];

				if (buyer.pricePerUnit < seller.pricePerUnit)
					break;

				float clearing_price = ExtensionMethods.Average(buyer.pricePerUnit, seller.pricePerUnit);

				ProcessTransaction(buyer, seller, clearing_price, ref rd);

				if (seller.qty == 0)        //seller is out of offered good
				{
					seller.MarkAsProcessed(clearing_price);
					asks.RemoveAt(0);       //remove ask
					failsafe = 0;
				}
				if (buyer.qty == 0)     //buyer is out of offered good
				{
					buyer.MarkAsProcessed(clearing_price);
					bids.RemoveAt(0);       //remove bid
					failsafe = 0;
				}

				failsafe++;

				if (failsafe > 1000)
				{
					//Debug.Log("Failsafe hit");
					break;
				}
			}

			//reject all remaining offers,
			//update price belief models based on unsuccessful transaction
			while (bids.Count > 0)
			{
				var buyer = bids[0];
				var buyer_a = buyer.owner;
				//(buyer_a as GameAgent).UpdatePriceModel(OfferType.Buy, itemName, false);

				bids.RemoveAt(0);
			}

			while (asks.Count > 0)
			{
				var seller = asks[0];
				var seller_a = seller.owner;
				//(seller_a as GameAgent).UpdatePriceModel(OfferType.Sell, itemName, false);

				asks.RemoveAt(0);
			}

			if (rd.successfulTrades > 0)
				rd.avgPrice = rd.avgPrice / rd.successfulTrades;
		}

		private void TransferGood(string itemName, int qty, float pricePerUnit, GameAgent seller, GameAgent buyer)
		{
			//seller.Inventory.RemoveFromInventory(itemName, qty);
			//buyer.Inventory.AddToInventory(itemName, qty);
		}

		public string GetGoodWithMostSupply()
		{
			var good_with_most_supply = "";
			var allItems = GameOracle.Instance.GameData.Items;
			var best_qty = float.NegativeInfinity;

			var asks = SellsOverPeriod(1);

			foreach (var item in allItems)
			{
				var itemAsks = asks.FindAll(pred => pred.itemName == item.Name);
				var itemBids = asks.FindAll(pred => pred.itemName == item.Name);

				float asksQty = 0;
				float bidsQty = 0;

				itemAsks.ForEach(pred => asksQty += pred.origQty);
				itemAsks.ForEach(pred => bidsQty += pred.origQty);

				float diff = asksQty - bidsQty;

				if (diff > best_qty && diff > 0)
				{
					best_qty = diff;
					good_with_most_supply = item.Name;
				}
			}

			return good_with_most_supply;
		}
		public string GetHottestGood()
		{
			var best_market = "";
			float minimum = 1.05f;
			var best_ratio = float.NegativeInfinity;
			var allItems = GameOracle.Instance.GameData.Items;

			var asks = SellsOverPeriod(1);
			var bids = BuysOverPeriod(1);


			foreach (var item in allItems)
			{
				var itemAsks = asks.FindAll(pred => pred.itemName == item.Name);
				var itemBids = bids.FindAll(pred => pred.itemName == item.Name);

				float asksQty = 0;
				float bidsQty = 0;

				itemAsks.ForEach(pred => asksQty += pred.origQty);
				itemBids.ForEach(pred => bidsQty += pred.origQty);

				float ratio = 0;
				if (asksQty == 0 && bidsQty > 0)
				{
					//If there are NONE on the market we artificially create a fake supply of 1/2 a unit to avoid the
					//crazy bias that "infinite" demand can cause...

					asksQty = 1;
				}

				ratio = bidsQty / asksQty;

				if (ratio > minimum && ratio > best_ratio)
				{
					best_ratio = ratio;
					best_market = item.Name;
				}
			}

			return best_market;
		}

		private void TransferMoney(float amount, GameAgent seller, GameAgent buyer)
		{
			//seller.PlayerData.gold += amount;
			//buyer.PlayerData.gold -= amount;
		}

		public void PlaceOffer(Offer offer)
		{
			allActiveOffers.Add(offer);
		}
	}
}