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
using WorldSimAPI.ContentMsg;
using System.IO;

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
            messageRouter.Add("POP_CENTER_INFO", new GamePopCenterRequestHandler());
            messageRouter.Add("MARKET_INFO", new MarketInfoRequestHandler());
            messageRouter.Add("TURN_NUMBER", new TurnNumberRequestHandler());
            messageRouter.Add("NEW_USER", new UserAuthRequestHandler());

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
        public static WebSocketServer wssv;

        public static void Main(string[] args)
        {
            wssv = new WebSocketServer(2567);
            wssv.AddWebSocketService<Laputa>("/Laputa");
            wssv.Start();

            var go = GameOracle.Instance;
            go.LoadGameAssets("./");
            go.CreateNewGame();

            go.OnTurnEnded += OnTurnEnded;
            
            Console.WriteLine("Press Escape to Kill Server");

            var lastTime = Environment.TickCount;

            while (true) {

                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey();
                    if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    else if (keyInfo.Key == ConsoleKey.P)
                    {
                        Console.WriteLine("Writing turn to file");
                        System.IO.File.WriteAllText(
                            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"worldsim.txt"),
                            GameOracle.Instance.ToString()
                        );
                    }
                    else if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine("Ending Turn");
                        GameOracle.Instance.EndTurn();                        
                    }
                }
                //}
            }
            
            wssv.Stop();
        }

        public static void OnTurnEnded()
        {
            var gameSessions = wssv.WebSocketServices["/Laputa"].Sessions;

            foreach (var session in gameSessions.Sessions)
            {
                TurnNumberContentMsg replyContent = new TurnNumberContentMsg();
                replyContent.TurnNumber = (int)GameOracle.Instance.TurnNumber;
                TurnNumberReplyMsg replyMsg = new TurnNumberReplyMsg(replyContent);

                string json = JsonConvert.SerializeObject(replyMsg);

                WorldSimMsg requestMsg = new WorldSimMsg("TURN_NUMBER", json);

                session.Context.WebSocket.Send(JsonConvert.SerializeObject(requestMsg));
            }
        }


        public static void MyMethod(object sender, ElapsedEventArgs e)
        {
        }
}
}