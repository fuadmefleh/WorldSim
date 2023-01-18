using NetMQ;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI;
using WorldSimLib;

namespace WorldSimService.RequestHandlers
{
    public class TileInfoRequestHandler : RequestHandler
    {
        public override WorldSimMsg HandleMsg(WorldSimMsg requestMsg)
        {
            HexTileContentQueryMsg msgContent = JsonConvert.DeserializeObject<HexTileContentQueryMsg>(requestMsg.Content);

            var gameMapTile = GameOracle.Instance.gameMap.TileAtMapPos(msgContent.xPos, msgContent.yPos);

            HexTileDetailedContentMsg replyMsg = null;

            if (gameMapTile == null)
            {
                replyMsg = new HexTileDetailedContentMsg(-1, -1, HeightType.DeepWater, HeatType.Cold);
            }
            else
            {
                replyMsg = new HexTileDetailedContentMsg(msgContent.xPos, msgContent.yPos, gameMapTile.HeightType, gameMapTile.HeatType);
                replyMsg.temperature = gameMapTile.baseTemperature;
                replyMsg.humidity = gameMapTile.humidity;
                replyMsg.moistureType = gameMapTile.MoistureType;
                replyMsg.elevation = gameMapTile.elevation;
                replyMsg.hasRiver = gameMapTile.hasRiver;
                replyMsg.biomeType = gameMapTile.BiomeType;
            }

            string json = JsonConvert.SerializeObject(replyMsg);

            requestMsg.Content = json;

            return requestMsg;
        }
    }
}
