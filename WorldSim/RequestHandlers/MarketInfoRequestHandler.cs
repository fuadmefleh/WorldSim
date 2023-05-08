using NetMQ;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using WorldSimAPI;
using WorldSimLib;

namespace WorldSimService.RequestHandlers
{
    public class MarketInfoRequestHandler : RequestHandler
    {
        public override WorldSimMsg HandleMsg(WorldSimMsg requestMsg)
        {
            MarketContentQueryMsg msgContent = JsonConvert.DeserializeObject<MarketContentQueryMsg>(requestMsg.Content);

            //var gameMapTile = GameOracle.Instance.gameMap.TileAtMapPos(msgContent.xPos, msgContent.yPos);

            MarketContentMsg replyMsg = null;

            var gamePopCenters = GameOracle.Instance.PopCenters;

            var gamePopCenter = gamePopCenters.Find( pred => pred.Location.position.X == msgContent.xPos && pred.Location.position.Y == msgContent.yPos);           

            if (gamePopCenter == null)
            {
                Console.WriteLine("Null Game Pop center market info requested");
                replyMsg = new MarketContentMsg();// (-1, -1, HeightType.DeepWater, HeatType.Cold);
            }
            else
            {
                replyMsg = new MarketContentMsg();// msgContent.xPos, msgContent.yPos, gameMapTile.HeightType, gameMapTile.HeatType);
                replyMsg.AllActiveOffers = gamePopCenter.MarketPlace.OffersOverPeriod(1).ConvertAll(p => p.ToContentMsg());
            }

            string json = JsonConvert.SerializeObject(replyMsg);

            requestMsg.Content = json;

            return requestMsg;
        }
    }
}
