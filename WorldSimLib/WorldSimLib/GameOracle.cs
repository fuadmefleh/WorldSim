using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using WorldSimLib.AI;
using MoreLinq;
using System.Linq;
using WorldSimLib.DataObjects;

namespace WorldSimLib
{
    public class GameOracle
    {
        private static GameOracle instance;

        public Map gameMap;

        public int mapWidth = 100;
        public int mapHeight = 100;

        // public List<Unit> gameUnits = new List<Unit>();

       // List<Community> _communities;
        List<GamePopCenter> _popCenters;

        public Dictionary<(string culture, string religon, string occupation), GamePop> GamePopulations { get; set; }

        public uint TurnNumber { get; private set; }

        public GameData GameData { get; set; }

        //public List<Community> Communities
        //{
        //    get { return _communities; }
        //}
        
        public List<GamePopCenter> PopCenters
        {
            get { return _popCenters; }
        }

        public void CreateNewGame()
        {
            GamePopulations = new Dictionary<(string culture, string religon, string occupation), GamePop>();

            gameMap = new Map(mapWidth, mapHeight);
            gameMap.Create();
            // marketPlace = new MarketPlace();
            TurnNumber = 1;

          //  _communities = new List<Community>();
            _popCenters = new List<GamePopCenter>();

            // Create the first seeds of groups of communities
            // Get all tiles of viable starting point
            var startPoints = gameMap.GetSuitablePopStartPoints();
            var shuffledStartPoints = startPoints.Shuffle().ToList();

            int lengthToUse = startPoints.Count >= 10 ? 10 : startPoints.Count;

            for (int i = 0; i < lengthToUse; i++)
            {
                GamePopCenter newPopCenter = new GamePopCenter
                {
                    SettlementLevel = WorldSimAPI.SettlementType.Hamlet,
                    Location = shuffledStartPoints[i]
                };

                GamePop newPop = new GamePop("GamePop " + i.ToString())
                {
                    Culture = "Culture " + i.ToString(),
                    Religion = "Religion " + i.ToString(),
                    Occupation = "Nomad"
                };

                newPop.Locations.Add(newPopCenter, 100);
                newPopCenter.Populations.Add(newPop);

                Console.WriteLine("Added new pop");

                GamePopulations.Add((newPop.Culture, newPop.Religion, newPop.Occupation), newPop);
                PopCenters.Add(newPopCenter);
            }
        }

        //public Unit CreateNewUnit(Vector3Int pos)
        //{
        //    var newUnit =  new Unit(pos.x, pos.y, pos.z);
        //    gameUnits.Add(newUnit);
        //    return newUnit;
        //}

        #region Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static GameOracle Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameOracle();
                }
                return instance;
            }
        }

        #endregion

        
        public void LoadGameAssets(string assetsDir)
        {
            GameData = JsonConvert.DeserializeObject<GameData>(File.ReadAllText(Path.Combine(assetsDir, "gameSettings.json")));

  //          Console.WriteLine("Loaded Game Assets from {0}", Path.Combine(assetsDir, "gameSettings.json"));
//            Console.WriteLine("Loaded Game Assets {0}", GameData);
        }

        public void EndTurn()
        {
            // Block user input

            //foreach (Community community in Communities)
            //{
            //    foreach (var agent in community.Agents)
            //    {
            //        agent.EndTurn(TurnNumber);
            //    }

            //    community.MarketPlace.EndTurn(TurnNumber);
            //}

            gameMap.EndTurn();

            //marketPlace.EndTurn(TurnNumber);

            // Increase the turn number
            TurnNumber += 1;
            // Unblock user input
        }
    }

}
