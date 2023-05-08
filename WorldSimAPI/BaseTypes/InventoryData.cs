using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WorldSimAPI.BaseTypes
{
    public class InventoryRecord
    {
        public float Cost { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; } 

        public int OriginalQuantity { get; set; }
        
        public InventoryRecord( string itemName, float cost, int qty )
        {
            this.ItemName = itemName;
            this.Cost = cost;
            this.Quantity = qty;
            this.OriginalQuantity = qty;
        }

        public float CostPerUnit
        {
            get { return Cost / OriginalQuantity; }
        }

        public override string ToString()
        {
            return $"Item: {ItemName}, Cost: {Cost}, Quantity: {Quantity}, OriginalQuantity: {OriginalQuantity}, CostPerUnit: {CostPerUnit}";
        }
    }

    public class InventoryRecordCollection : List<InventoryRecord>
    {
        public InventoryRecordCollection SortByCostPerUnit()
        {
            InventoryRecordCollection sortedCollection = new InventoryRecordCollection();
            var sortedRecords = this.OrderBy(record => record.CostPerUnit);

            foreach (var record in sortedRecords)
            {
                sortedCollection.Add(record);
            }

            return sortedCollection;
        }

        public float GetCheapestPrice()
        {
            return SortByCostPerUnit().Last().CostPerUnit;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Inventory Record Collection:");
            foreach (var record in this)
            {
                sb.AppendLine("\t" + record.ToString());
            }

            return sb.ToString();
        }
    }

    public class InventoryData
    {
        public Dictionary<string, InventoryRecordCollection> ItemsContainer { get; set; }

        public override string ToString()
        {
            string retStr = "InventoryData: \n";

            foreach( var itemName in ItemsContainer )
            {
                retStr += itemName.Key + "=" + itemName.Value;
                retStr += "\n";
            }

            return retStr;
        }
    }
}
