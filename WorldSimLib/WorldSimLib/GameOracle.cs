using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using WorldSimLib.AI;
using MoreLinq;
using System.Linq;
using WorldSimLib.DataObjects;
using WorldSimAPI;
using MagicMau.ProceduralNameGenerator;
using WorldSimLib.Utils;
using System.Text;
using System.Numerics;

namespace WorldSimLib
{
    public class GameNameGenerators {

        public Dictionary<BiomeType, NameGenerator> BiomeNameGenerators = new Dictionary<BiomeType, NameGenerator>();

        public GameNameGenerators()
        {
            foreach( BiomeType biomeType in Enum.GetValues(typeof(BiomeType)))
            {
                var generator = new NameGenerator(TrainingData.GrasslandNames, 2, 0.01);

                BiomeNameGenerators[biomeType] = generator;
            }
            
        }
    }

    public class GameOracle
    {
        private static GameOracle instance;

        public delegate void OnTurnEndedHandler();

        public GameNameGenerators GameNameGenerators { get; set; }
        public Map gameMap;

        public int mapWidth = 100;
        public int mapHeight = 100;

        List<GamePopCenter> _popCenters;

        public Dictionary<(string culture, string religon, string occupation), GamePop> GamePopulations { get; set; }

        public uint TurnNumber { get; private set; }

        public GameData GameData { get; set; }
        
        public List<GamePopCenter> PopCenters
        {
            get { return _popCenters; }
        }

        public OnTurnEndedHandler OnTurnEnded { get; set; }

        public bool IsPopCenterAtLocation( Vector3 position )
        {
            return _popCenters.Exists(pred => pred.Location.position == position);
        }

        public GamePopCenter GetPopCenterAtLocation(Vector3 position)
        {
            return _popCenters.Find(pred => pred.Location.position == position);
        }

        public void CreateNewGame()
        {
            GamePopulations = new Dictionary<(string culture, string religon, string occupation), GamePop>();
            GameNameGenerators = new GameNameGenerators();

            gameMap = new Map(mapWidth, mapHeight);
            gameMap.Create();

            TurnNumber = 1;

            _popCenters = new List<GamePopCenter>();

            // Create the first seeds of groups of communities
            // Get all tiles of viable starting point
            var startPoints = gameMap.GetSuitablePopStartPoints();
            startPoints.Shuffle();

            int lengthToUse = 2;// startPoints.Count >= 10 ? 10 : startPoints.Count;

            for (int i = 0; i < lengthToUse; i++)
            {                
                GamePop newPop = new GamePop("GamePop " + i.ToString())
                {
                    Culture = "Culture " + i.ToString(),
                    Religion = "Religion " + i.ToString(),
                    Occupation = "Nomad"
                };

                newPop.Name = GameNameGenerators.BiomeNameGenerators[startPoints[i].BiomeType].GenerateName(3, 10, 0, null, StaticRandom.Instance); 

                while( newPop.Name == null )
                    newPop.Name = GameNameGenerators.BiomeNameGenerators[startPoints[i].BiomeType].GenerateName(3, 10, 0, null, StaticRandom.Instance);

                string newPopCenterName = GameNameGenerators.BiomeNameGenerators[startPoints[i].BiomeType].GenerateName(3, 10, 0, null, StaticRandom.Instance);

                while ( newPopCenterName == null )
                {
                    newPopCenterName = GameNameGenerators.BiomeNameGenerators[startPoints[i].BiomeType].GenerateName(3, 10, 0, null, StaticRandom.Instance);
                }
                
                GamePopCenter newPopCenter = new GamePopCenter(
                     newPopCenterName,
                     startPoints[i],
                     new Dictionary<GamePop, int>(),
                     WorldSimAPI.SettlementType.HunterGather
                );

                newPop.AddPopToLocation(newPopCenter, 100);                
                newPopCenter.Populations.Add(newPop, 100);
                newPop.WealthAtLocations[newPopCenter].AddAmount(newPopCenter.LocalCurrency, 100);

                newPop.AddNeeds(newPopCenter, GameData.PopNeeds);
                newPop.Technologies.Add(GameData.TechnologyFromName("Language"));

                Console.WriteLine("Added new pop: " + newPopCenterName);

                GamePopulations.Add((newPop.Culture, newPop.Religion, newPop.Occupation), newPop);
                PopCenters.Add(newPopCenter);
            }
        }

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


        public string ToMarkdown()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"# GameOracle: Turn {TurnNumber}");
            sb.AppendLine($"* **Map Dimensions:** {mapWidth} x {mapHeight}");
            sb.AppendLine($"* **Population Centers:** {_popCenters.Count}");

            for (int i = 0; i < _popCenters.Count; i++)
            {
                sb.AppendLine(_popCenters[i].ToMarkdown());
                sb.AppendLine();
            }


            sb.AppendLine($"* **Populations:** {GamePopulations.Count}");

            foreach (var pop in GamePopulations)
            {
                sb.AppendLine(pop.Value.ToMarkdown());
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"GameOracle: Turn {TurnNumber}");
            sb.AppendLine($"Map Dimensions: {mapWidth} x {mapHeight}");
            sb.AppendLine($"Population Centers: {_popCenters.Count}");

            for (int i = 0; i < _popCenters.Count; i++)
            {
                sb.AppendLine(_popCenters[i].ToString());
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void LoadGameAssets(string assetsDir)
        {
            GameData = JsonConvert.DeserializeObject<GameData>(File.ReadAllText(Path.Combine(assetsDir, "gameSettings.json")));

            GameData.Prime();
        }

        public void EndTurn()
        {
            gameMap.EndTurn();

            foreach (var population in GamePopulations)
            {
                population.Value.EndTurn(TurnNumber);
            }

            for (int i = 0; i < PopCenters.Count; i++)
            {
                GamePopCenter popCenter = PopCenters[i];

                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"Processing popCenter: {popCenter.Name}");

                Console.ResetColor();

                popCenter.EndTurn(TurnNumber);
               
            }

            Console.WriteLine("Turn Completed: " + TurnNumber.ToString());

            // Increase the turn number
            TurnNumber += 1;

            Console.WriteLine($"Exchange rate: {PopCenters[0].CalculateExchangeRate(PopCenters[1])}");

            OnTurnEnded?.Invoke();

            // Unblock user input
        }
    }



}
