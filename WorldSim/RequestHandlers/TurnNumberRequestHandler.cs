using Newtonsoft.Json;
using System.Drawing;
using WorldSimAPI;
using WorldSimAPI.ContentMsg;
using WorldSimLib;
using System.Collections.Generic;
using System;

namespace WorldSimService.RequestHandlers
{
    public class TurnNumberRequestHandler : RequestHandler
    {
        public override WorldSimMsg HandleMsg(WorldSimMsg requestMsg)
        {
            TurnNumberQueryMsg msgContent = JsonConvert.DeserializeObject<TurnNumberQueryMsg>(requestMsg.Content);

            if( msgContent.UpdateTurn )
            {
                GameOracle.Instance.EndTurn();
            }

            TurnNumberContentMsg replyContent = new TurnNumberContentMsg();

            replyContent.TurnNumber = (int)GameOracle.Instance.TurnNumber;
            PopulatePopCenterContentMessage(replyContent.PopulationCenters);
            PopulateCaravanContentMessage(replyContent.Caravans);
            PopulateSettlerContentMessage(replyContent.Settlers);

            TurnNumberReplyMsg replyMsg = new TurnNumberReplyMsg(replyContent);

            string json = JsonConvert.SerializeObject(replyMsg);

            requestMsg.Content = json;

            return requestMsg;
        }

        void PopulateCaravanContentMessage( List<CaravanContentMsg> caravanList )
        {
            var gamePopCenters = GameOracle.Instance.PopCenters;

            foreach (var popCenter in gamePopCenters)
            {
                foreach (var travelTimer in popCenter.TravelQueue)
                {
                    (var caravan, var path) = travelTimer.ToValueTuple();

                    CaravanContentMsg caravanMsg = new CaravanContentMsg();
                    caravanMsg.X = (int)caravan.Location.position.X;
                    caravanMsg.Y = (int)caravan.Location.position.Y;

                    caravanList.Add(caravanMsg);
                }
            }
        }

        void PopulateSettlerContentMessage(List<CaravanContentMsg> caravanList)
        {
            var gamePopCenters = GameOracle.Instance.PopCenters;

            foreach (var popCenter in gamePopCenters)
            {
                foreach (var travelTimer in popCenter.SettlerQueue)
                {
                    (var caravan, var path) = travelTimer.ToValueTuple();

                    CaravanContentMsg caravanMsg = new CaravanContentMsg();
                    caravanMsg.X = (int)caravan.Location.position.X;
                    caravanMsg.Y = (int)caravan.Location.position.Y;

                    caravanList.Add(caravanMsg);
                }
            }
        }

        void PopulatePopCenterContentMessage( List<GamePopCenterContentMsg> popCentersList )
        {           
            var gamePopCenters = GameOracle.Instance.PopCenters;

            foreach (var popCenter in gamePopCenters)
            {
                var pos = popCenter.Location.position;

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

                popCentersList.Add(popCenterContentMsg);
            }
        }
    }
}