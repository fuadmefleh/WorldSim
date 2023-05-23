using System;
using System.Collections;
using System.Collections.Generic;
using WorldSimLib.DataObjects;
using WorldSimLib.Utils;
using WorldSimAPI.BaseTypes;
using System.Linq;

namespace WorldSimLib
{
 
    public class Inventory : InventoryData
    {
        public int MaxSlotSize = 50;

        public Inventory()
        {
            ItemsContainer = new Dictionary<string, InventoryRecordCollection>();
        }

        public int GetQuantityOfItem( string itemName )
        {
            if (!ItemsContainer.ContainsKey(itemName))
                return 0;

            var collectionForItem = ItemsContainer[itemName];

            return collectionForItem.Sum(pred => pred.Quantity);
        }

        public int InventorySpaceLeft(string itemName)
        {
            var currentQty = GetQuantityOfItem(itemName);
            return MaxSlotSize - currentQty;
        }


        public void AddToInventory(string itemName, int qty, float pricePerUnit )
        {           
            if (!ItemsContainer.TryAdd(itemName, new InventoryRecordCollection()))
                ItemsContainer[itemName].Add(new InventoryRecord(itemName, pricePerUnit * qty, qty));
            else
                ItemsContainer[itemName].Add(new InventoryRecord(itemName, pricePerUnit * qty, qty));

        }

        public float RemoveFromInventory(string itemName, int qty)
        {
            float totalCost = 0.0f;
            int qtyLeftToRemove = qty;

            if (!ItemsContainer.ContainsKey(itemName))
                return 0;

            var collectionForItem = ItemsContainer[itemName].SortByCostPerUnit();

            foreach( var inventoryRecord in collectionForItem )
            {
                if( inventoryRecord.Quantity > qtyLeftToRemove )
                {
                    var amountToRemove = qtyLeftToRemove;
                    inventoryRecord.Quantity -= amountToRemove;
                    qtyLeftToRemove -= amountToRemove;
                    totalCost += inventoryRecord.CostPerUnit * qtyLeftToRemove;
                }
                else
                {
                    totalCost += inventoryRecord.CostPerUnit * inventoryRecord.Quantity;
                    qtyLeftToRemove -= inventoryRecord.Quantity;
                    inventoryRecord.Quantity = 0;
                }

                if (qtyLeftToRemove == 0)
                    break;
            }

            // Remove empty records
            //collectionForItem.RemoveAll(pred => pred.Quantity <= 0);

            ItemsContainer[itemName] = collectionForItem;

            return totalCost;
        }

        public bool ContainsItem(string itemName)
        {
            return ItemsContainer.ContainsKey(itemName);
        }

        public bool ContainsItemAndQtyOfType(ItemType itemType, int qty)
        {
            foreach( var item in ItemsContainer.Keys )
            {
                var itemObj = GameOracle.Instance.GameData.ItemFromName(item);
                if (itemType == itemObj.IType && ContainsItemAndQty(item,qty))
                    return true;
            }
            return false;
        }

        public Item GetItemOfType(ItemType itemType)
        {
            foreach (var item in ItemsContainer.Keys)
            {
                var itemObj = GameOracle.Instance.GameData.ItemFromName(item);
                if (itemType == itemObj.IType)
                    return itemObj;
            }
            return null;
        }

        public bool ContainsItemAndQty(string itemName, int qty)
        {
            if (!ContainsItem(itemName)) return false;

            if (GetQuantityOfItem(itemName) < qty)
                return false;

            return true;
        }
        public int Shortage(string itemName, int idealAmt)
        {
            if (!ContainsItem(itemName)) return idealAmt;

            int amt = GetQuantityOfItem(itemName);

            if (amt < idealAmt)
            {
                return (idealAmt - amt);
            }
            return 0;
        }

        public void Merge(Inventory other)
        {
            foreach( var item in other.ItemsContainer)
            {
                if( ItemsContainer.ContainsKey( item.Key ) )
                {
                    ItemsContainer[item.Key].AddRange(item.Value);
                }
            }
        }
        public float GetTotalWorthForItem(string itemName)
        {
            float totalWorth = 0;

            if (!ContainsItem(itemName))
                return totalWorth;

            foreach( var record in ItemsContainer[itemName])
            {
                totalWorth += record.Quantity * record.CostPerUnit;
            }
            return totalWorth;
        }

        public float GetTotalWorth()
        {
            float totalWorth = 0;
            foreach (var entry in ItemsContainer)
            {
                foreach (var record in entry.Value)
                {
                    totalWorth += record.Quantity * record.CostPerUnit;
                }
            }
            return totalWorth;
        }
        public int Surplus(string itemName, int idealAmt = 1)
        {
            if (!ContainsItem(itemName)) return 0;

            int amt = GetQuantityOfItem(itemName);

            if (amt > idealAmt)
            {
                return (amt - idealAmt);
            }
            return 0;
        }

        public bool CanProcessRecipe(Recipe recipe)
        {
            // Does the inventory have the inputs required
            foreach (var input in recipe.Inputs)
            {
                if (!ContainsItemAndQty(input.ItemName, input.Quantity)) return false;
            }

            return true;
        }

        public float GetLowestCostForItem(string itemName)
        {
            if (!ItemsContainer.ContainsKey(itemName))
                throw new Exception("Item is not in the inventory and is requested for lowest price");

            return ItemsContainer[itemName].GetCheapestPrice();
        }

        public float EstimateRecipeCost(Recipe recipe, MarketPlace market)
        {
            float totalInputMaterialCost = 0.0f;

            foreach (var input in recipe.Inputs)
            {
                float averageInputPrice = market.GetAveragePrice(input.ItemName);
                totalInputMaterialCost += input.Quantity * averageInputPrice;
            }

            return totalInputMaterialCost;
        }

        public void IncreaseItemCost(string itemName, float amount)
        {
            if (!ItemsContainer.ContainsKey(itemName))
                return;

            var collectionForItem = ItemsContainer[itemName];
            int totalQuantity = collectionForItem.FindAll(record => record.Quantity > 0).Count;

            if (totalQuantity == 0)
                return;

            float costIncreasePerRecord = amount / totalQuantity;

            foreach (var record in collectionForItem)
            {
                record.Cost += costIncreasePerRecord;
            }
        }


        public void ProcessRecipe(Recipe recipe, float efficiency = 1, float laborCost = 0)
        {
            if (!CanProcessRecipe(recipe))
                return;

            float totalInputMaterialCost = 0.0f;

            // Remove the inputs required
            foreach (var input in recipe.Inputs)
            {
                // Only add items in that we consumed during making
                if (StaticRandom.Instance.NextDouble() <= input.ChanceOfConsuming)
                {
                    totalInputMaterialCost += RemoveFromInventory(input.ItemName, input.Quantity);
                }
            }

            totalInputMaterialCost += laborCost;

            // Put the output into the inventory
            foreach (var output in recipe.Outputs)
            {
                AddToInventory(output.ItemName, output.Quantity, totalInputMaterialCost / output.Quantity);
                Console.WriteLine("Created: {0} of {1}", output.Quantity, output.ItemName);
            }
        }
    }
}