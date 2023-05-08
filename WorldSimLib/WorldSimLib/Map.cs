using System.Collections;
using System.Collections.Generic;
using System;
using System.Numerics;
using Newtonsoft.Json;
using WorldSimLib.Utils;
using TinkerWorX.AccidentalNoiseLibrary;

using WorldSimAPI;
using WorldSimLib.DataObjects;
using System.Linq;

namespace WorldSimLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Map
    {
        [JsonProperty]
        public int mapWidth;

        [JsonProperty]
        public int mapHeight;

        public float gridHeight = 0.75f;
        public float c = 0.25f;
        public float gridWidth = 1.0f;

        public float minElevation = float.PositiveInfinity;
        public float maxElevation = float.NegativeInfinity;
        public float minHeatValue = float.PositiveInfinity;
        public float maxHeatValue = float.NegativeInfinity;
        public float minMoistureValue = float.PositiveInfinity;
        public float maxMoistureValue = float.NegativeInfinity;

        public float CELL_WIDTH = 2000; // 2000 meters is the physical length of the side of a cell
        public float CELL_AREA = 10390000f; // square meters

        public float MAX_ELEVATION = 0;

        public int continentCount;

        [JsonProperty]
        public List<HexTile> TileData { 
            get { return tileData; } 
            set { tileData = value; }
        }

        List<HexTile> tileData;

        List<HexTile> weatherData;
        List<River> rivers;

        System.Random randomGenerator;

        Perlin perlin = new Perlin();

        float GLOBAL_WARMING_PEAK_VALUE = 0;

        // The number of cycles of the basic noise pattern that are repeated
        // over the width and height of the texture.
        public float scale = 8.5F;

        BiomeType[,] BiomeTable = new BiomeType[6, 6] {   
		    //COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
		    { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
		    { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
		    { BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
		    { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
		    { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
		    { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
	    };

        public string[] tileTypes = {
        "PLAINS",
        "WATER",
        "FOREST",
        "SCRUBLANDS",
        "HIGHLANDS",
        "DIRT",
        "DESERT",
        "HILLS",
        "BASE",
        "MOUNTAIN"
    };

        // Noise generator module
        ImplicitFractal NoiseGenerator;
        ImplicitCombiner HeatMapGenerator;
        ImplicitFractal MoistureMapGenerator;

        int TerrainOctaves = 6;
        double TerrainFrequency = 1.25;
        float DeepWaterLevel_Default = 0.2f;
        float ShallowWaterLevel_Default = 0.4f;
        float DirtLevel_Default = 0.5f;
        float HillsLevel_Default = 0.65f;
        float HighlandsLevel_Default = 0.8f;

        int HeatOctaves = 4;
        double HeatFrequency = 3.0;        
        float ColdestValue = 0.05f;
        float ColderValue = 0.18f;
        float ColdValue = 0.4f;
        float WarmValue = 0.6f;
        float WarmerValue = 0.8f;

        int MoistureOctaves = 4;
        double MoistureFrequency = 3.0;
        float DryerValue = 0.27f;
        float DryValue = 0.4f;
        float WetValue = 0.6f;
        float WetterValue = 0.8f;
        float WettestValue = 0.9f;

        int RiverCount = 40;
        float MinRiverHeight = 0.6f;
        int MaxRiverAttempts = 1000;
        int MinRiverTurns = 18;
        int MinRiverLength = 10;
        int MaxRiverIntersections = 2;

        List<TileGroup> Waters;
        List<TileGroup> Lands;

        int Seed;

        public Map(int width, int height)
        {
            randomGenerator = new System.Random();

            Seed = randomGenerator.Next(0, int.MaxValue);

            NoiseGenerator = new ImplicitFractal(FractalType.Multi, BasisType.Simplex, InterpolationType.Quintic);
            NoiseGenerator.Octaves = TerrainOctaves;
            NoiseGenerator.Frequency = TerrainFrequency;
            NoiseGenerator.Seed = Seed;

            ImplicitGradient gradient = new ImplicitGradient(1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1);
            ImplicitFractal heatFractal = new ImplicitFractal(FractalType.Multi, BasisType.Simplex, InterpolationType.Quintic);
            heatFractal.Octaves = HeatOctaves;
            heatFractal.Frequency = HeatFrequency;
            heatFractal.Seed = Seed;

            HeatMapGenerator = new ImplicitCombiner(CombinerType.Multiply);
            HeatMapGenerator.AddSource(gradient);
            HeatMapGenerator.AddSource(heatFractal);

            MoistureMapGenerator = new ImplicitFractal(FractalType.Multi, BasisType.Simplex, InterpolationType.Quintic);
            MoistureMapGenerator.Octaves = MoistureOctaves;
            MoistureMapGenerator.Frequency = MoistureFrequency;
            MoistureMapGenerator.Seed = Seed;

            MAX_ELEVATION = randomGenerator.Range(7000f, 10000f); // meters
            GLOBAL_WARMING_PEAK_VALUE = randomGenerator.Range(37f, 39f);

            mapWidth = width;
            mapHeight = height;
        }

        public void Create()
        {
            tileData = new List<HexTile>();

            Waters = new List<TileGroup>();
            Lands = new List<TileGroup>();
            rivers = new List<River>();

            // Create the underlying data reprsentation
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero (z = -x - y)
                    tileData.Add(new HexTile(x, y, -x - y));
                }
            }

            ConnectNeighbors();

            GenerateHeightmap();

          //  GenerateWeatherData();

            //CalculateWindDirections();

            AssignTileTypes();

            GenerateWaterAndLandGroups();

            GenerateRivers();

            AssignTileResources();

            Console.WriteLine("Total Land Groups: {0}\nTotal Water Groups: {1}", Lands.Count, Waters.Count);

           // CalculateInitialHumidity();
        }
        public BiomeType GetBiomeType(HexTile tile)
        {
            return BiomeTable[(int)tile.MoistureType, (int)tile.HeatType];
        }

        public void EndTurn()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    HexTile curTile = tileData[tileIdx];

                    Circulate(curTile);

                    //CalculateEvaporation(curTile);

                    //Precipitate(curTile);

                    //AssignTileType(curTile);
                }
            }


        }

        public List<HexTile> GetTilesWithinRadius(Vector3 centerPosition, float radius)
        {
            Dictionary<Vector3, HexTile> tileLookup = TileData.ToDictionary(t => t.position);
            List<HexTile> tilesWithinRadius = new List<HexTile>();

            int centerX = (int)centerPosition.X;
            int centerY = (int)centerPosition.Y;
            int centerZ = (int)centerPosition.Z;

            for (int dx = -(int)Math.Floor(radius); dx <= (int)Math.Floor(radius); dx++)
            {
                for (int dy = -(int)Math.Floor(radius); dy <= (int)Math.Floor(radius); dy++)
                {
                    for (int dz = -(int)Math.Floor(radius); dz <= (int)Math.Floor(radius); dz++)
                    {
                        if (dx + dy + dz == 0)
                        {
                            Vector3 currentPosition = new Vector3(centerX + dx, centerY + dy, centerZ + dz);

                            if (tileLookup.ContainsKey(currentPosition))
                            {
                                tilesWithinRadius.Add(tileLookup[currentPosition]);
                            }
                        }
                    }
                }
            }

            return tilesWithinRadius;
        }


        float CalculateLatitude(int yPos)
        {
            int centerY = mapHeight / 2;

            float latitude = (float)yPos / (float)centerY;

            if (yPos > centerY)
                latitude = (float)(mapHeight - yPos) / (float)centerY;

            return (90 - (latitude * 90));
        }

        void AssignTileResources()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    var curTile = tileData[tileIdx];

                    foreach( Resource resource in GameOracle.Instance.GameData.Resources)
                    {
                        if( resource.HeightTypes.Contains(curTile.HeightType) && resource.Biomes.Contains( curTile.BiomeType ) && resource.IsWorldGenerated )
                        {
                            curTile.AvailableResourceSources.Add(resource, 40);
                        } 
                    }
                }
            }
        }


        void AssignTileType(HexTile curTile)
        {
            // Normalize Elevation         
            if (curTile.elevation < DeepWaterLevel_Default)
                curTile.HeightType = HeightType.DeepWater;
            else if (curTile.elevation < ShallowWaterLevel_Default)
                curTile.HeightType = HeightType.ShallowWater;
            else if (curTile.elevation < DirtLevel_Default)
                curTile.HeightType = HeightType.Dirt;
            else if (curTile.elevation < HillsLevel_Default)
                curTile.HeightType = HeightType.Hills;
            else if (curTile.elevation < HighlandsLevel_Default)
                curTile.HeightType = HeightType.Highlands;
            else
                curTile.HeightType = HeightType.Mountain;

            if (curTile.HeightType == WorldSimAPI.HeightType.DeepWater || curTile.HeightType == HeightType.ShallowWater )
                curTile.Collidable = false;
            else
                curTile.Collidable = true;

            //adjust moisture based on height
            if (curTile.HeightType == HeightType.DeepWater)
            {
                curTile.humidity += 8f * curTile.elevation;
            }
            else if (curTile.HeightType == HeightType.ShallowWater)
            {
                curTile.humidity += 3f * curTile.elevation;
            }
            else if (curTile.HeightType == HeightType.Shore)
            {
                curTile.humidity += 1f * curTile.elevation;
            }
            else if (curTile.HeightType == HeightType.Sand)
            {
                curTile.humidity += 0.2f * curTile.elevation;
            }

            //Moisture Map Analyze	
            float moistureValue = curTile.humidity;
            moistureValue = (moistureValue - minMoistureValue) / (maxMoistureValue - minMoistureValue);
            curTile.humidity = moistureValue;

            //set moisture type
            if (moistureValue < DryerValue) curTile.MoistureType = MoistureType.Dryest;
            else if (moistureValue < DryValue) curTile.MoistureType = MoistureType.Dryer;
            else if (moistureValue < WetValue) curTile.MoistureType = MoistureType.Dry;
            else if (moistureValue < WetterValue) curTile.MoistureType = MoistureType.Wet;
            else if (moistureValue < WettestValue) curTile.MoistureType = MoistureType.Wetter;
            else curTile.MoistureType = MoistureType.Wettest;

            if(curTile.HeightType == HeightType.Dirt && moistureValue > WetValue )
            {
                curTile.HeightType = HeightType.Grass;
            }
            if (curTile.HeightType == HeightType.Dirt && curTile.MoistureType == MoistureType.Dryest )
            {
                curTile.HeightType = HeightType.Sand;
            }

            // set heat type
            if (curTile.baseTemperature < ColdestValue * maxHeatValue) curTile.HeatType = HeatType.Coldest;
            else if (curTile.baseTemperature < ColderValue * maxHeatValue) curTile.HeatType = HeatType.Colder;
            else if (curTile.baseTemperature < ColdValue * maxHeatValue) curTile.HeatType = HeatType.Cold;
            else if (curTile.baseTemperature < WarmValue * maxHeatValue) curTile.HeatType = HeatType.Warm;
            else if (curTile.baseTemperature < WarmerValue * maxHeatValue) curTile.HeatType = HeatType.Warmer;
            else curTile.HeatType = HeatType.Warmest;

            // set the biome
            curTile.BiomeType = GetBiomeType(curTile);
        }

        void AssignTileTypes()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    var curTile = tileData[tileIdx];

                    AssignTileType(curTile);
                }
            }
        }

        void GenerateHeightmap()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    //Noise range
                    float x1 = 0, x2 = 2;
                    float y1 = 0, y2 = 2;
                    float dx = x2 - x1;
                    float dy = y2 - y1;

                    //Sample noise at smaller intervals
                    float s = (float)x / (float)mapWidth;
                    float t = (float)y / (float)mapHeight;

                    // Calculate our 3D coordinates
                    float nx = x1 + (float)Math.Cos(s * 2 * (float)Math.PI) * dx / (2 * (float)Math.PI);
                    float ny = y1 + (float)Math.Cos(t * 2 * (float)Math.PI) * dy / (2 * (float)Math.PI);
                    float nz = x1 + (float)Math.Sin(s * 2 * (float)Math.PI) * dx / (2 * (float)Math.PI);
                    float nw = y1 + (float)Math.Sin(t * 2 * (float)Math.PI) * dy / (2 * (float)Math.PI);

                    float sample = (float)NoiseGenerator.Get(nx, ny, nz, nw);
                    float heatSample = (float)HeatMapGenerator.Get(nx, ny, nz, nw);
                    float moistureSample = (float)MoistureMapGenerator.Get(nx, ny, nz, nw);

                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    if (sample > maxElevation) maxElevation = sample;
                    if (sample < minElevation) minElevation = sample;

                    if (heatSample > maxHeatValue) maxHeatValue = heatSample;
                    if (heatSample < minHeatValue) minHeatValue = heatSample;

                    if (moistureSample > maxMoistureValue) maxMoistureValue = moistureSample;
                    if (moistureSample < minMoistureValue) minMoistureValue = moistureSample;

                    tileData[tileIdx].elevation = sample;
                    tileData[tileIdx].baseTemperature = heatSample;

                    tileData[tileIdx].humidity = moistureSample;
                }
            }

            // Normalize the values now
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    var curTile = tileData[tileIdx];

                    // Normalize Elevation
                    var curElevation = tileData[tileIdx].elevation;
                    curElevation = (curElevation - minElevation) / (maxElevation - minElevation);
                    tileData[tileIdx].elevation = curElevation;

                    // Adjust Heat Map based on Height - Higher == colder
                    if (curTile.elevation < ShallowWaterLevel_Default)
                    {
                        curTile.baseTemperature += 0.01f * curTile.elevation;
                    }
                    else if (curTile.elevation < DirtLevel_Default)
                    {
                        curTile.baseTemperature -= 0.1f * curTile.elevation;
                    }
                    else if (curTile.elevation < HillsLevel_Default)
                    {
                        curTile.baseTemperature -= 0.2f * curTile.elevation;                     
                    }
                    else if (curTile.elevation == HighlandsLevel_Default)
                    {
                        curTile.baseTemperature -= 0.3f * curTile.elevation;                        
                    }
                    else
                    {
                        curTile.baseTemperature -= 0.4f * curTile.elevation;
                    }

                    // Set heat value
                    float heatValue = curTile.baseTemperature;
                    heatValue = (heatValue - minHeatValue) / (maxHeatValue - minHeatValue);
                    curTile.baseTemperature = heatValue;
                }
            }
        }

        void GenerateRivers()
        {            
            int attempts = 0;
            int rivercount = RiverCount;
            //  Rivers = new List();

            var listOfViableTiles = tileData.FindAll(pred => pred.Collidable == true && pred.elevation > MinRiverHeight);
            listOfViableTiles.Shuffle();

            foreach( var viableTile in listOfViableTiles )
            {
                if (attempts > rivercount)
                    break;

                if (viableTile.hasRiver)
                    continue;

                // Tile is good to start river from

                // Figure out the direction this river will try to flow
                //river.CurrentDirection = viableTile.GetLowestNeighborDirection();

                int maxIterations = 1000;
                int riverLength = 1;

                HexTile nextTile = viableTile;
                //viableTile.hasRiver = true;

                List<HexTile> potentialRiver = new List<HexTile>();
                potentialRiver.Add(viableTile);

                while ( maxIterations > 0 )
                {
                    nextTile = nextTile.GetLowestNeighbor();

                    if (!nextTile.Collidable || potentialRiver.Contains(nextTile))
                        break;

                    potentialRiver.Add(nextTile);

                    maxIterations--;
                }

                if( potentialRiver.Count > MinRiverLength )
                {
                    foreach (var tile in potentialRiver)
                        tile.hasRiver = true;
                }
                
                attempts++;
            }
        }


        void GenerateWeatherData()
        {
            int centerY = mapHeight / 2;

            float MAX_TEMP_DIFF = 40;
            float MIN_TEMP_DIFF = 5;

            // Create the underlying data reprsentation
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero
                    int tileIdx = Convert2DToIndex(x, y);
                    var tileToProcess = tileData[tileIdx];

                    if (tileIdx == -1)
                        continue;

                    float intensity = (float)y / (float)centerY;

                    float equitorialCalmZoneSize = randomGenerator.Range(0.025f, 0.05f); // 2.5% - 5% of the overall height

                    if (y > centerY)
                        intensity = (float)(mapHeight - y) / (float)centerY;

                    if (1 - intensity < equitorialCalmZoneSize)
                        intensity = 1;

                    tileToProcess.baseTemperature = intensity * GLOBAL_WARMING_PEAK_VALUE;
                    tileToProcess.temperature = tileToProcess.baseTemperature;

                    tileToProcess.annualTemperatureRange = new Vector2(tileToProcess.temperature - randomGenerator.Range(MIN_TEMP_DIFF * (1 - intensity), MAX_TEMP_DIFF * (1 - intensity)),
                        tileToProcess.temperature + randomGenerator.Range(MIN_TEMP_DIFF * (1 - intensity), MAX_TEMP_DIFF * (1 - intensity)));

                    tileToProcess.dailyTemperatureRange = new Vector2(tileToProcess.temperature - randomGenerator.Range(MIN_TEMP_DIFF * (1 - intensity), MAX_TEMP_DIFF * (1 - intensity)),
                        tileToProcess.temperature + randomGenerator.Range(MIN_TEMP_DIFF * (1 - intensity), MAX_TEMP_DIFF * (1 - intensity)));
                }
            }
        }

        void CalculateWindDirection(HexTile tile)
        {
            List<float> temperatures = new List<float>();

            Vector3 maxDiffPosition = tile.position;
            float maxDiff = float.NegativeInfinity;

            foreach (var adjTile in tile.adjacentTiles)
            {
                // We want to create the surface level winds so the hot air will be rising and displaced by colder air
                // Therefore, we want the wind to be in the direction of increasing temp
                if (adjTile.temperature - tile.temperature > maxDiff)
                {
                    maxDiff = adjTile.temperature - tile.temperature;
                    maxDiffPosition = adjTile.position;
                }
            }

            tile.windDirection = new Vector2(maxDiffPosition.X - tile.position.X, maxDiffPosition.Y - tile.position.Y);
            tile.windSpeed = randomGenerator.Range(0.9f, 4.5f);
        }

        public Vector3 CalculateHeatSourcePosition(float elapsedTime)
        {
            //256 is the number of days to complete an orbit
            float orbitRadius = 1000f; // adjust this value to change the radius of the orbit
            float orbitSpeed = 2f * MathF.PI / 256f; // adjust this value to change the speed of the orbit

            // Calculate the angle of the heat source based on the elapsed time and orbit speed
            float angle = elapsedTime * orbitSpeed;

            // Calculate the x and z positions of the heat source based on the angle and orbit radius
            float x = orbitRadius * MathF.Cos(angle);
            float z = orbitRadius * MathF.Sin(angle);

            // The y position of the heat source can be set to a fixed value, or adjusted to simulate changes in the height of the orbit
            float y = 500f;

            // Return the position of the heat source
            return new Vector3(x, y, z);
        }

        public float CalculateRotationAngle(float time, float dayLength, float axialTilt)
        {
            // Calculate the fraction of a full day that has passed
            float fractionOfDay = time / dayLength;

            // Calculate the angle of rotation around the planet's axis
            float rotationAngle = fractionOfDay * 360f;

            // Calculate the tilt angle
            float tiltAngle = axialTilt * MathF.Sin((2f * MathF.PI * fractionOfDay) - (MathF.PI / 2f));

            // Apply the tilt angle to the rotation angle
            rotationAngle += tiltAngle;

            return rotationAngle;
        }

        void GenerateHeatMap()
        {
            ImplicitGradient gradient = new ImplicitGradient(1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;
                }
            }
        }

        void CalculateWindDirections()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    CalculateWindDirection(tileData[tileIdx]);
                }
            }
        }

        void CalculateInitialHumidity()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    var tileToUse = tileData[tileIdx];

                    var maxConcentration = CalculateMaxConcentration(tileToUse);

                    tileToUse.humidity = maxConcentration * randomGenerator.Range(0.5f, 0.65f);
                }
            }
        }

        void Circulate(HexTile curTile)
        {
            //float tempDelta = curTile.temperature - 50;
            //float tempRatio = tempDelta / tempRange;

            //// Calculate air mass movement based on temperature gradient
            //float xMovement = 0f;
            //float yMovement = 0f;

            //if (tempRatio > 0.5f)
            //{
            //    xMovement = tempRatio * 2f;
            //}
            //else
            //{
            //    yMovement = (0.5f - tempRatio) * 2f;
            //}

            //// Move air mass to neighboring tiles
            //foreach (var neighbor in curTile.adjacentTiles)
            //{
            //    float xDiff = neighbor.position.X - curTile.position.X;
            //    float yDiff = neighbor.position.Y - curTile.position.Y;

            //    if (MathF.Abs(xDiff) <= 1f && MathF.Abs(yDiff) <= 1f)
            //    {
            //        float distance = MathF.Sqrt(xDiff * xDiff + yDiff * yDiff);

            //        if (distance > 0f)
            //        {
            //            float xAmt = xMovement / distance;
            //            float yAmt = yMovement / distance;

            //            neighbor.airMass.x += xAmt * timeStep;
            //            neighbor.airMass.y += yAmt * timeStep;
            //        }
            //    }
            //}
        }


        void CirculateOld(HexTile curTile)
        {
            List<HexTile> neighborsToDiffuseTo = new List<HexTile>();

            if (curTile.windDirection.Y > 0)
            {
                neighborsToDiffuseTo = curTile.adjacentTiles.FindAll(pred => pred.position.Y > curTile.position.Y);

            }
            else if (curTile.windDirection.Y < 0)
            {
                neighborsToDiffuseTo = curTile.adjacentTiles.FindAll(pred => pred.position.Y < curTile.position.Y);
            }

            var neighborsWithLessHumidity = neighborsToDiffuseTo.FindAll(pred => pred.humidity < curTile.humidity);

            foreach (var neighbor in neighborsWithLessHumidity)
            {
                if (neighbor.humidity < curTile.humidity)
                {
                    // 75 is used to increase the effect of the windspeed over the entire tile.
                    float humidity_diff = curTile.humidity - neighbor.humidity;
                    //float changeIntensity = humidity_diff / ((curTile.humidity + neighbor.humidity) / 2);

                    //if (changeIntensity < 0.25f)
                    //    continue;

                    // Soil moisture should dampen the flow
                    //if (curTile.soilMoisture > 0)
                    //  humidity_diff = humidity_diff * randomGenerator.Range(0.8f, 0.9f);

                    var wind_percent = (curTile.windSpeed * 500) / CELL_WIDTH;

                    var amt_to_circulate = (humidity_diff * wind_percent);
                    neighbor.humidity += amt_to_circulate;
                    curTile.humidity -= amt_to_circulate;
                }
            }
        }

        //void AdjustWindSpeeds()
        //{
        //    for (int x = 0; x < mapWidth; x++)
        //    {
        //        for (int y = 0; y < mapHeight; y++)
        //        {
        //            // All three coords will always sum to zero
        //            int tileIdx = Convert2DToIndex(x, y);

        //            if (tileIdx == -1)
        //                continue;

        //            HexTile curTile = tileData[tileIdx];

        //            Vector3Int maxDiffPosition = curTile.position;
        //            float maxDiff = Mathf.NegativeInfinity;

        //            foreach (var adjTile in curTile.adjacentTiles)
        //            {
        //                float humidity_diff = curTile.humidity - adjTile.humidity;

        //                // We want to create the surface level winds so the hot air will be rising and displaced by colder air
        //                // Therefore, we want the wind to be in the direction of increasing temp
        //                if (humidity_diff > maxDiff)
        //                {
        //                    maxDiff = adjTile.temperature - curTile.temperature;
        //                    maxDiffPosition = adjTile.position;
        //                }
        //            }

        //            foreach (var neighbor in neighborsToDiffuseTo)
        //            {
        //                if (neighbor.humidity < curTile.humidity)
        //                {
        //                    // 75 is used to increase the effect of the windspeed over the entire tile.
        //                    float humidity_diff = curTile.humidity - neighbor.humidity;
        //                    //float changeIntensity = humidity_diff / ((curTile.humidity + neighbor.humidity) / 2);

        //                    //if (changeIntensity < 0.25f)
        //                    //    continue;

        //                    var wind_percent = (curTile.windSpeed * 50) / CELL_WIDTH;
        //                    neighbor.humidity += humidity_diff * wind_percent;
        //                    curTile.humidity -= humidity_diff * wind_percent;
        //                }
        //            }
        //        }
        //    }
        //}

        void Precipitate(HexTile curTile)
        {
            float humidityRatio = curTile.humidity / CalculateMaxConcentration(curTile);

            if (humidityRatio > 0.75f)
            {
                // Create rain
                var chanceOfRain = randomGenerator.Range(0.05f, 0.1f);

                if (humidityRatio > 2f)
                    chanceOfRain = randomGenerator.Range(0.1f, 0.15f);
                if (humidityRatio > 3f)
                    chanceOfRain = randomGenerator.Range(0.15f, 0.2f);
                if (humidityRatio > 5f)
                    chanceOfRain = randomGenerator.Range(0.25f, 0.3f);
                if (humidityRatio > 7f)
                    chanceOfRain = randomGenerator.Range(0.35f, 0.4f);
                if (humidityRatio > 9f)
                    chanceOfRain = randomGenerator.Range(0.7f, 0.9f);

                if (chanceOfRain < randomGenerator.NextDouble())
                    return;

                var amountToRain = curTile.humidity - CalculateMaxConcentration(curTile) * 0.75f;
                amountToRain *= randomGenerator.Range(0.95f, 1.05f);

                curTile.soilMoisture += amountToRain;
                curTile.humidity -= amountToRain;
            }
        }

        /// <summary>
        /// Returns the max mass in kg of water a cell can hold in its air column
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public float CalculateMaxConcentration(HexTile tile)
        {
            float a = 67.046f;
            float b = 0.64f;
            // Min temp is -25C, lower than that is 0

            if (tile.temperature <= -25)
                return 0;

            float maxConcentrationOverCubicMeter = a * (float)Math.Log(tile.temperature + 25) + b;

            return (maxConcentrationOverCubicMeter * CELL_AREA) / 1000;
        }

        void CalculateEvaporation(HexTile curTile)
        {
            // Anything not an ocean will have water taken from its humidity
            if (curTile.tileType != "OCEAN")
            {
                // 24 degrees celcius is around 75 F, water holds nice
                float intensity = curTile.temperature.Remap(-25f, GLOBAL_WARMING_PEAK_VALUE, 0.005f, 0.03f);

                curTile.humidity -= (curTile.humidity * intensity);
            }
            else
            {
                float evap_coef = 0.0007786f * curTile.temperature + 0.003767f;

                // if we are below 8 celcius, it is too cold to evaporate the water
                if (curTile.temperature < 8)
                    return;

                float evap_per_hour = (34.5f) * (CELL_AREA) * (evap_coef - 0.0098f);
                float evaporation_per_day = evap_per_hour * 24;

                curTile.humidity += evap_per_hour * randomGenerator.Range(0.35f, 0.5f);
            }
        }

        void ConnectNeighbors()
        {
            // Create the underlying data reprsentation
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    int tileIdx = Convert2DToIndex(x, y);

                    // All three coords will always sum to zero
                    int z = -x - y;

                    HexTile curTile = tileData[tileIdx];

                    // Populate the neighbors


                    if (y - 1 > 0 && x + 1 < mapWidth)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x + 1, y - 1)]);
                    if (y + 1 < mapHeight && x - 1 > 0)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x - 1, y + 1)]);


                    // Handle Wraparound
                    // Reached the max extent on the x side, set neighbor to zero x
                    if (x - 1 >= 0)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x - 1, y)]);
                    else if (x - 1 == -1)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(mapWidth - 1, y)]);


                    if (x + 1 < mapWidth)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x + 1, y)]);
                    else if (x + 1 == mapWidth)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(0, y)]);


                    if (y - 1 >= 0)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x, y - 1)]);
                    else if (y - 1 == -1)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x, mapHeight - 1)]);

                    if (y + 1 < mapHeight)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x, y + 1)]);
                    else if (y + 1 == mapHeight)
                        curTile.adjacentTiles.Add(tileData[Convert2DToIndex(x, 0)]);

                }
            }

        }

        void GenerateWaterAndLandGroups()
        {
            Stack<HexTile> tilesToProcess = new Stack<HexTile>();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // All three coords will always sum to zero
                    int tileIdx = Convert2DToIndex(x, y);

                    if (tileIdx == -1)
                        continue;

                    var tileToUse = tileData[tileIdx];

                    if (tileToUse.FloodFilled)
                        continue;

                    // Land
                    if(tileToUse.Collidable)
                    {
                        TileGroup group = new TileGroup();
                        group.Type = TileGroupType.Land;
                        tilesToProcess.Push(tileToUse);

                        while(tilesToProcess.Count > 0 )
                        {
                            GenerateWaterAndLandGroups(tilesToProcess.Pop(), ref group, ref tilesToProcess);
                        }

                        if (group.Tiles.Count > 0)
                            Lands.Add(group);
                    }
                    // Water
                    else
                    {
                        TileGroup group = new TileGroup();
                        group.Type = TileGroupType.Water;
                        tilesToProcess.Push(tileToUse);

                        while (tilesToProcess.Count > 0)
                        {
                            GenerateWaterAndLandGroups(tilesToProcess.Pop(), ref group, ref tilesToProcess);
                        }

                        if (group.Tiles.Count > 0)
                            Waters.Add(group);
                    }
                }
            }
        }

        private void GenerateWaterAndLandGroups(HexTile tile, ref TileGroup tiles, ref Stack<HexTile> stack)
        {
            // Validate
            if (tile.FloodFilled)
                return;
            if (tiles.Type == TileGroupType.Land && !tile.Collidable)
                return;
            if (tiles.Type == TileGroupType.Water && tile.Collidable)
                return;

            // Add to TileGroup
            tiles.Tiles.Add(tile);
            tile.FloodFilled = true;

            // floodfill into neighbors
            foreach (var neighborTile in tile.adjacentTiles)
            {
                if (!neighborTile.FloodFilled && tile.Collidable == neighborTile.Collidable)
                {
                    stack.Push(neighborTile);
                }             
            }
        }

        int StepFloodFillGeneration(List<HexTile> tilesToProcess)
        {
            List<HexTile> newTilesToProcess = new List<HexTile>();

            foreach (var tile in tilesToProcess)
            {
                var neighborsWithoutContinent = tile.adjacentTiles.FindAll(adjTile => adjTile.continentID == -1);

                foreach (var neighbor in neighborsWithoutContinent)
                {
                    neighbor.continentID = tile.continentID;
                }

                newTilesToProcess.AddRange(neighborsWithoutContinent);
            }

            tilesToProcess.Clear();
            tilesToProcess.AddRange(newTilesToProcess);

            return tilesToProcess.Count;
        }


        public List<HexTile> GetSuitablePopStartPoints()
        {
            List<BiomeType> suitableBiomes = new List<BiomeType>();
            suitableBiomes.Add(BiomeType.Savanna);
            suitableBiomes.Add(BiomeType.TropicalRainforest);
            suitableBiomes.Add(BiomeType.Grassland);
            suitableBiomes.Add(BiomeType.Woodland);
            suitableBiomes.Add(BiomeType.BorealForest);
            suitableBiomes.Add(BiomeType.TemperateRainforest);
            suitableBiomes.Add(BiomeType.Tundra);
            suitableBiomes.Add(BiomeType.SeasonalForest);

            List<HeightType> suitableHeightTypes = new List<HeightType>();
            suitableHeightTypes.Add(HeightType.Highlands);
            suitableHeightTypes.Add(HeightType.Scrublands);
            suitableHeightTypes.Add(HeightType.Dirt);
            suitableHeightTypes.Add(HeightType.Forest);
            suitableHeightTypes.Add(HeightType.Grass);
            suitableHeightTypes.Add(HeightType.Hills);

            return tileData.FindAll( (pred) => {
                if (pred.Collidable && suitableBiomes.Contains(pred.BiomeType) )
                    return true;

                return false;
            });
        }

        private int Convert2DToIndex(int x, int y)
        {
            int idx = y + (x * mapHeight);

            if (tileData.Count <= idx)
                return -1;

            return idx;
        }

        public HexTile TileAtMapPos(int x, int y)
        {
            if (x < 0 || y < 0)
                return null;

            var idx = Convert2DToIndex(x, y);

            if (idx == -1) return null;

            return tileData[idx];
        }

        public HexTile TileAt(float x, float y)
        {
            float halfWidth = gridWidth / 2;
            float m = c / halfWidth;

            // Find the row and column of the box that the point falls in.
            int row = (int)(y / gridHeight);
            int column;

            bool rowIsOdd = row % 2 == 1;

            // Is the row an odd number?
            if (rowIsOdd)// Yes: Offset x to match the indent of the row
                column = (int)((x + halfWidth) / gridWidth);
            else// No: Calculate normally
                column = (int)Math.Floor(x / gridWidth);

            //// Work out the position of the point relative to the box it is in
            double relY = y - (row * gridHeight);
            double relX;

            if (rowIsOdd)
                relX = (x - (column * gridWidth)) + halfWidth;
            else
                relX = x - (column * gridWidth);

            // Work out if the point is above either of the hexagon's top edges
            if (relY < (-m * relX) + c) // Right edge
            {
                row--;
                if (rowIsOdd)
                {
                    column--;
                }
            }
            else if (relY < (m * relX) - c) // Left edge
            {
                row--;
                if (!rowIsOdd)
                    column++;
            }

            int tileIdx = Convert2DToIndex(column, row);

            if (tileIdx == -1)
                return null;

            return tileData[tileIdx];
        }
    }
}