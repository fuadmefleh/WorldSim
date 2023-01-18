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
    public class GamePopCentersRequestHandler : RequestHandler
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

            foreach( var popCenter in gamePopCenters )
            {
                var pos = popCenter.Location.position;

                // If this requested range contains this pop center
                if( rangeRect.Contains(new Point((int)pos.X, (int)pos.Y)) )
                {
                    GamePopCenterContentMsg popCenterContentMsg = new GamePopCenterContentMsg((int)pos.X, (int)pos.Y);
                    popCenterContentMsg.settlementType = popCenter.SettlementLevel;
                    
                    foreach( var gamePop in popCenter.Populations )
                    {
                        GamePopContentMsg popContentMsg = new GamePopContentMsg();
                        popContentMsg.Culture = gamePop.Culture;
                        popContentMsg.Quantity = gamePop.Locations[popCenter];
                        popContentMsg.Occupation = gamePop.Occupation;
                        popContentMsg.EducationLevel = gamePop.EducationLevel;
                        popContentMsg.Religion = gamePop.Religion;
                        popContentMsg.Name = gamePop.Name;

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
