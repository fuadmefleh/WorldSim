using System;
using System.Collections.Generic;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;
using WorldSimLib;
using Newtonsoft.Json;

using WorldSimAPI;
using WorldSimService;
using WorldSimService.RequestHandlers;

namespace Example
{
    public class Laputa : WebSocketBehavior
    {
        Dictionary<string, RequestHandler> messageRouter;

        public Laputa()
        {
            messageRouter = new Dictionary<string, RequestHandler>();

            messageRouter.Add("MAP_UPDATE", new MapUpdateRequestHandler());
            messageRouter.Add("TILE_INFO", new TileInfoRequestHandler());
            messageRouter.Add("POP_UPDATE", new GamePopCentersRequestHandler());

        }

        protected override void OnOpen()
        {
            Console.WriteLine("Client connected");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var inputMsg = e.Data;


            WorldSimMsg msg = JsonConvert.DeserializeObject<WorldSimMsg>(inputMsg);

            if( messageRouter.ContainsKey( msg.Topic ) )
            {
                var reply = messageRouter[msg.Topic].HandleMsg(msg);
                Send(JsonConvert.SerializeObject(reply));
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var wssv = new WebSocketServer(2567);
            wssv.AddWebSocketService<Laputa>("/Laputa");
            wssv.Start();

            var go = GameOracle.Instance;
            go.LoadGameAssets("./");
            go.CreateNewGame();

            //var selectedTile = go.gameMap.TileAtMapPos(5, 5);

            // Make some of all the agent types
           // var agentTypes = GameOracle.Instance.GameData.AgentTypes;

            //Community community = new Community("Village 1");

            //community.Position = selectedTile.position;

            //foreach (var agentType in agentTypes)
            //{
            //    for (int i = 0; i < 50; i++)
            //    {
            //        GameAgent ga = new GameAgent(agentType.Name + " " + i.ToString(), agentType, GameOracle.Instance, community);
            //        ga.PlayerData.gold = 100;
            //        community.Agents.Add(ga);
            //    }
            //}

            //GameOracle.Instance.Communities.Add(community);
            
            Console.WriteLine("Press Enter to Kill Server");


            var lastTime = Environment.TickCount;

            while (true) {

                //System.Threading.Thread.Sleep(50);

                if( Environment.TickCount - lastTime > 50 )
                {
                    lastTime = Environment.TickCount;

                    // GameOracle.Instance.EndTurn();

                    if (Console.KeyAvailable)
                    {
                        break;
                    }

                }
            }
            
            wssv.Stop();
        }



        public static void MyMethod(object sender, ElapsedEventArgs e)
        {
        }
}
}