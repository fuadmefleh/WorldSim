using System.Collections;
using System.Collections.Generic;


namespace WorldSimLib
{
    public enum OfferType
    {
        Buy = 0,
        Sell = 1
    }

    public class Offer
    {
        public OfferType offerType; // offer type {buy or sell}
        public string itemName; // what item is the bid for
                                // public float value; // what is the total value of the bid
        public int qty; // how many of the item
        public int origQty;
        public int duration; // how many turns is the bid valid for (typically only used in sell bids)
        public GameAgent owner; // who owns the bid
        public int age;
        public float pricePerUnit;
        public bool IsMarketOrder { get; set; }
        float _clearingPrice;

        public bool IsProcessed
        {
            get; private set;
        }

        public float ClearingPrice
        {
            get { return _clearingPrice; }
        }

        public class OfferDecPriceBasedComparer : IComparer<Offer>
        {
            public int Compare(Offer a, Offer b)
            {
                if (a.pricePerUnit < b.pricePerUnit) return 1;
                if (a.pricePerUnit > b.pricePerUnit) return -1;
                return 0;
            }
        }

        public class OfferIncPriceBasedComparer : IComparer<Offer>
        {
            public int Compare(Offer a, Offer b)
            {
                if (a.pricePerUnit > b.pricePerUnit) return 1;
                if (a.pricePerUnit < b.pricePerUnit) return -1;
                return 0;
            }
        }

        public Offer()
        {

        }

        public override string ToString()
        {
            string retStr = "Offer: \n";

            retStr += "itemName: ";
            retStr += itemName + "\n";

            retStr += "pricePerUnit: ";
            retStr += pricePerUnit + "\n";

            retStr += "qty: ";
            retStr += qty + "\n";

            retStr += "duration: ";
            retStr += duration + "\n";

            retStr += "age: ";
            retStr += age + "\n";

            return retStr;
        }

        public Offer(string iName, float pricePerUnit, int quantity, OfferType oType, int dur = 1)
        {
            this.itemName = iName;
            this.qty = quantity;
            this.pricePerUnit = pricePerUnit;
            this.offerType = oType;
            this.duration = dur;
            this.origQty = quantity;
            this.IsMarketOrder = false;
        }

        public void MarkAsProcessed(float clearingPrice)
        {
            this._clearingPrice = clearingPrice;
            this.IsProcessed = true;
        }
    }

}