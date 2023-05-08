using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimAPI.ContentMsg
{
    public class OfferContentMsg
    {
        public string OfferType { get; set; } // offer type {buy or sell}
        public string ItemName { get; set; } // what item is the bid for
                                // public float value; // what is the total value of the bid
        public int Qty { get; set; } // how many of the item
        public int OrigQty { get; set; }
        public int Duration { get; set; } // how many turns is the bid valid for (typically only used in sell bids)
        public string OwnerName { get; set; } // who owns the bid
        public int Age { get; set; }
        public float PricePerUnit { get; set; }
        public bool IsMarketOrder { get; set; }

        public override string ToString()
        {
            return $"OfferType: {OfferType}\nItemName: {ItemName}\nQty: {Qty}\nOrigQty: {OrigQty}\nDuration: {Duration}\nOwnerName: {OwnerName}\nAge: {Age}\nPricePerUnit: {PricePerUnit}\nIsMarketOrder: {IsMarketOrder}";
        }

    }
}
