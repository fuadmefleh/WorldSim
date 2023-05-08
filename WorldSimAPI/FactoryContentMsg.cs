using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI.BaseTypes;

namespace WorldSimAPI
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FactoryContentMsg
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public float Wealth { get; set; }

        [JsonProperty]
        public InventoryData Inventory { get; set; }

        public FactoryContentMsg(string name, float wealth, InventoryData inventoryData )
        {
            Name = name;
            Wealth = wealth;
            Inventory = inventoryData;
        }

        public FactoryContentMsg()
        {

        }

        public override string ToString()
        {
            string retStr = "FactoryContentMsg: \n";

            retStr += "Inventory Data:\n";
            retStr += Inventory.ToString();

            return retStr;
        }
    }
}
