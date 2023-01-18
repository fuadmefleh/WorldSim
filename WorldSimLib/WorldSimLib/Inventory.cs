using System;
using System.Collections;
using System.Collections.Generic;
using WorldSimLib.DataObjects;

namespace WorldSimLib
{
    public class Inventory
    {
        private Dictionary<string, int> ItemsContainer { get; set; }
        private Dictionary<string, float> ItemsAverageCostLedger { get; set; }

        public Inventory()
        {
            ItemsContainer = new Dictionary<string, int>();
            ItemsAverageCostLedger = new Dictionary<string, float>();
        }

        public void AddToInventory(Item item, int qty)
        {
            if (!ItemsContainer.TryAdd(item.Name, qty))
                ItemsContainer[item.Name] += qty;
        }

        public void RemoveFromInventory(Item item, int qty)
        {
            if (ItemsContainer.ContainsKey(item.Name))
                ItemsContainer[item.Name] -= qty;
        }

        public void AddToInventory(string itemName, int qty, float pricePerUnit = float.PositiveInfinity)
        {
            var item = GameOracle.Instance.GameData.ItemFromName(itemName);

            if (!ItemsContainer.TryAdd(item.Name, qty))
                ItemsContainer[item.Name] += qty;

            if (pricePerUnit != float.PositiveInfinity)
            {
                if (!ItemsAverageCostLedger.ContainsKey(itemName))
                {
                    ItemsAverageCostLedger[itemName] = pricePerUnit;
                }
                else
                {
                    var totalPrice = ItemsAverageCostLedger[itemName] + pricePerUnit;
                    var totalQty = ItemsContainer[itemName]; // Since we added the value already at the start
                    ItemsAverageCostLedger[itemName] = totalPrice / totalQty;
                }
            }
        }

        public void RemoveFromInventory(string itemName, int qty, float pricePerUnit = float.PositiveInfinity)
        {
            if (ItemsContainer.ContainsKey(itemName))
            {
                ItemsContainer[itemName] = Math.Min(0, ItemsContainer[itemName] - qty);
            }

            if (pricePerUnit != float.PositiveInfinity)
            {
                if (!ItemsAverageCostLedger.ContainsKey(itemName))
                {
                    ItemsAverageCostLedger[itemName] = -pricePerUnit;
                }
                else
                {
                    var totalPrice = ItemsAverageCostLedger[itemName] - pricePerUnit;
                    var totalQty = ItemsContainer[itemName] + qty;
                    ItemsAverageCostLedger[itemName] = totalPrice / totalQty;
                }
            }
        }

        public int GetQuantityOfItem(string itemName)
        {
            if (ItemsContainer.ContainsKey(itemName))
                return ItemsContainer[itemName];
            else
                return 0;
        }

        public bool ContainsItem(string itemName)
        {
            return ItemsContainer.ContainsKey(itemName);
        }

        public bool ContainsItemAndQty(string itemName, int qty)
        {
            if (!ContainsItem(itemName)) return false;

            if (ItemsContainer[itemName] < qty)
                return false;

            return true;
        }
        public int Shortage(string itemName, int idealAmt)
        {
            if (!ContainsItem(itemName)) return idealAmt;

            int amt = ItemsContainer[itemName];

            if (amt < idealAmt)
            {
                return (idealAmt - amt);
            }
            return 0;
        }

        public int Surplus(string itemName, int idealAmt = 1)
        {
            if (!ContainsItem(itemName)) return 0;

            int amt = ItemsContainer[itemName];

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

        public float EstimateRecipeCost(Recipe recipe)
        {
            float totalInputMaterialCost = 0.0f;

            foreach (var input in recipe.Inputs)
            {
                if (ItemsAverageCostLedger.ContainsKey(input.ItemName))
                    totalInputMaterialCost += ItemsAverageCostLedger[input.ItemName] * input.Quantity;
            }

            return totalInputMaterialCost;
        }
        //public void ProcessRecipe(Recipe recipe)
        //{
        //    if (!CanProcessRecipe(recipe))
        //        return;

        //    float totalInputMaterialCost = 0.0f;

        //    // Remove the inputs required
        //    foreach (var input in recipe.Inputs)
        //    {
        //        // Only add items in that we consumed during making
        //        if (Random. <= input.ChanceOfConsuming)
        //        {
        //            if (ItemsAverageCostLedger.ContainsKey(input.ItemName))
        //                totalInputMaterialCost += ItemsAverageCostLedger[input.ItemName] * input.Quantity;

        //            RemoveFromInventory(input.ItemName, input.Quantity);
        //        }
        //    }

        //    // Put the output into the inventory
        //    foreach (var output in recipe.Outputs)
        //    {
        //        AddToInventory(output.ItemName, output.Quantity, totalInputMaterialCost / output.Quantity);
        //        //Debug.LogFormat("Created: {0} of {1}", output.Quantity, output.ItemName);
        //    }


        //}
    }
}