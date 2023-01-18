using NetMQ;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI;
using WorldSimLib;

namespace WorldSimService.RequestHandlers
{
    public class MapUpdateRequestHandler : RequestHandler
    {
        public override WorldSimMsg HandleMsg(WorldSimMsg requestMsg)
        {
            MapContentQueryMsg msgContent = JsonConvert.DeserializeObject<MapContentQueryMsg>(requestMsg.Content);
            MapContentReplyMsg replyMsg = new MapContentReplyMsg(msgContent.startPos, msgContent.endPos);

            for (int x = (int)msgContent.startPos.X; x < (int)msgContent.endPos.X; x++)
            {
                if (x >= GameOracle.Instance.gameMap.mapWidth)
                {
                    continue;
                }

                for (int y = (int)msgContent.startPos.Y; y < (int)msgContent.endPos.Y; y++)
                {
                    if (y >= GameOracle.Instance.gameMap.mapHeight)
                        continue;

                    var gameMapTile = GameOracle.Instance.gameMap.TileAtMapPos(x, y);

                    if (gameMapTile == null)
                        continue;

                    var replyMsgContent = new HexTileContentMsg((int)gameMapTile.position.X, (int)gameMapTile.position.Y, gameMapTile.HeightType, gameMapTile.HeatType);
                    replyMsgContent.hasRiver = gameMapTile.hasRiver;
                    replyMsgContent.biomeType = gameMapTile.BiomeType;
                    replyMsgContent.moistureType = gameMapTile.MoistureType;

                    //replyMsgContent.hasGamePop


                    replyMsg.Tiles.Add(replyMsgContent);
                }
            }

            string json = JsonConvert.SerializeObject(replyMsg);

            requestMsg.Content = json.Base64Encode();

            return requestMsg;
        }
    }
}
