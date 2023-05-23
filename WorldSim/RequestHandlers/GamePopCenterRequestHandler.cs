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
    public class GamePopCenterRequestHandler : RequestHandler
    {
        public override WorldSimMsg HandleMsg(WorldSimMsg requestMsg)
        {
            GamePopCenterContentQueryMsg msgContent = JsonConvert.DeserializeObject<GamePopCenterContentQueryMsg>(requestMsg.Content);
            GamePopCenterContentReplyMsg replyMsg = new GamePopCenterContentReplyMsg(msgContent.startPos, msgContent.endPos);

            var gamePopCenters = GameOracle.Instance.PopCenters;
            Rectangle rangeRect = new Rectangle(
                (int)msgContent.startPos.X,
                (int)msgContent.startPos.Y,
                (int)(msgContent.endPos.X - msgContent.startPos.X),
                (int)(msgContent.endPos.Y - msgContent.startPos.Y)
            );

            foreach (var popCenter in gamePopCenters)
            {
                var pos = popCenter.Location.position;

                // If this requested range contains this pop center
                if (rangeRect.Contains(new Point((int)pos.X, (int)pos.Y)))
                {
                    GamePopCenterContentMsg popCenterContentMsg = new GamePopCenterContentMsg((int)pos.X, (int)pos.Y);
                    popCenterContentMsg.name = popCenter.Name;
                    popCenterContentMsg.settlementType = popCenter.SettlementLevel;
                    popCenterContentMsg.factoryData = popCenter.Factorys.ConvertAll(x => x.ToContentMsg());
                    popCenterContentMsg.marketContent = popCenter.MarketPlace.ToContentMsg();
                    popCenterContentMsg.wealth = popCenter.Wallet.GetAmount(popCenter.LocalCurrency);

                    foreach (var gamePop in popCenter.Populations.Keys)
                    {
                        GamePopContentMsg popContentMsg = gamePop.ToContentMsg(popCenter);                      

                        popCenterContentMsg.gamePops.Add(popContentMsg);
                    }

                    replyMsg.gamePopCenters.Add(popCenterContentMsg);
                }
            }

            string json = JsonConvert.SerializeObject(replyMsg);

            requestMsg.Content = json.Base64Encode();

            return requestMsg;
        }
    }
}
