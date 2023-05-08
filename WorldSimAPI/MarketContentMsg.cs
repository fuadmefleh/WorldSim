using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI.BaseTypes;
using WorldSimAPI.ContentMsg;

namespace WorldSimAPI
{
    public class MarketContentQueryMsg
    {
        public int xPos;
        public int yPos;

        public MarketContentQueryMsg(int x, int y)
        {
            xPos = x;
            yPos = y;
        }
    }

    public class MarketContentMsg
    {
        public List<OfferContentMsg> AllActiveOffers;

        public override string ToString()
        {
            string retStr = "";

            foreach( var offer in AllActiveOffers )
            {
                retStr += offer.ToString();
            }

            return retStr;
        }
    }
}
