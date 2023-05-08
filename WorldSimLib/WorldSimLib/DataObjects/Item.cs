using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;

using WorldSimAPI;

namespace WorldSimLib.DataObjects
{
    public enum ItemType
    {
        [Description("Food")]
        Food = 0,
        [Description("Semi")]
        Semi = 1,
        [Description("Trade")]
        Trade = 2,
        [Description("Clothing")]
        Clothing = 3,
        [Description("Fuel")]
        Fuel = 4
    }

    public class Item
    {
        public string Name { get; set; }
        public ItemType IType { get; set; }

        public float BaseQty { get; set; }
        public float QualityOfLife { get; set; }
        public float NeedFulfillment { get; set; }

        public Item(string name, ItemType it)
        {
            Name = name;
            IType = it;
        }

        public override string ToString()
        {
            string retStr = "Item: \n";

            retStr += Name + "\n";
            retStr += IType.GetDescription() + "\n";

            return retStr;
        }
    }

}