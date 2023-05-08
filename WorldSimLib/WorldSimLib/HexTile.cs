using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using System.Linq;
using MoreLinq.Extensions;
using WorldSimAPI;
using WorldSimLib.DataObjects;
using System.Text;

namespace WorldSimLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HexTile
    {
        public HexTile(int xPos, int yPos, int zPos)
        {
            continentID = -1;

            position = new Vector3(xPos, yPos, zPos);

            // If the y position is even then the x will be a whole number
            float cX = xPos;

            if (yPos % 2 == 0)
                cX = xPos + 0.5f;

            float cY = (0.75f * yPos) + 0.5f;

            centerPoint = new Vector3(cX, cY, 0);

            soilMoisture = 0;
            humidity = 0;

            
        }

        public HeightType HeightType;
        public HeatType HeatType;
        public MoistureType MoistureType;
        public BiomeType BiomeType;

        public Dictionary<Resource, int> AvailableResourceSources = new Dictionary<Resource, int>();

        public List<River> Rivers = new List<River>();
        public List<(Direction, Direction)> RiverDirections = new List<(Direction, Direction)>();

        [JsonProperty]
        public string tileType;

        /// <summary>
        /// Sum of G and H.
        /// </summary>
        [JsonProperty]
        public int F => g + h;

        /// <summary>
        /// Cost from start tile to this tile.
        /// </summary>
        [JsonProperty] 
        public int g;

        /// <summary>
        /// Estimated cost from this tile to destination tile.
        /// </summary>
        [JsonProperty]
        public int h;

        /// <summary>
        /// Tile's coordinates.
        /// </summary>
        [JsonProperty]
        public Vector3 position;

        /// <summary>
        /// Tile's center point coordinates in world space.
        /// </summary>
        [JsonProperty]
        public Vector3 centerPoint;

        /// <summary>
        /// References to all adjacent tiles.
        /// </summary>
        public List<HexTile> adjacentTiles = new List<HexTile>();

        /// <summary>
        /// References to all adjacent tiles.
        /// </summary>
        // public List<Unit> unitsOnTile = new List<Unit>();

        /// <summary>
        /// If true - Tile is an obstacle impossible to pass.
        /// </summary>
        [JsonProperty]
        public bool isObstacle;

        public bool hasRiver;

        /// <summary>
        /// If true - Tile already has a unit on it.
        /// </summary>
        // public bool isOccupied => unitsOnTile.Count > 0;

        [JsonProperty]
        public int continentID;

        [JsonProperty]
        public float elevation;

        [JsonProperty]
        public float temperature;

        [JsonProperty]
        public float baseTemperature;

        [JsonProperty]
        public Vector2 annualTemperatureRange;
        
        [JsonProperty]
        public Vector2 dailyTemperatureRange;

        [JsonProperty]
        public float maxTemperatureDiff;

        [JsonProperty]
        public float humidity;

        [JsonProperty]
        public Vector2 windDirection;

        [JsonProperty]
        public float soilMoisture;

        /// <summary>
        /// meters per second
        /// </summary>
        [JsonProperty]
        public float windSpeed;

        public bool Collidable;
        public bool FloodFilled;

        public HexTile GetLowestNeighbor()
        {
            var minTile = adjacentTiles.MinBy(x => x.elevation);

            return minTile.First();
        }

        public Direction GetLowestNeighborDirection()
        {
            var minTile = adjacentTiles.MinBy(x => x.elevation).First();

            return GetTileDirection(minTile);
        }

        public Direction GetTileDirection(HexTile otherTile )
        {
            if (otherTile.position.X == this.position.X &&
                otherTile.position.Y > this.position.Y)
                return Direction.NorthEast;

            if (otherTile.position.X < this.position.X &&
                otherTile.position.Y > this.position.Y)
                return Direction.NorthWest;

            if (otherTile.position.X == this.position.X &&
                otherTile.position.Y < this.position.Y)
                return Direction.SouthEast;

            if (otherTile.position.X < this.position.X &&
                otherTile.position.Y < this.position.Y)
                return Direction.SouthWest;

            if (otherTile.position.X > this.position.X &&
               otherTile.position.Y == this.position.Y)
                return Direction.East;

            if (otherTile.position.X < this.position.X &&
               otherTile.position.Y == this.position.Y)
                return Direction.West;

            return Direction.North;
        }

        public HexTile GetTileInDirection(Direction direction)
        {
            switch( direction)
            {
                case Direction.East:
                    return adjacentTiles.Find(pred => pred.position.X > this.position.X && pred.position.Y == this.position.Y );
                case Direction.West:
                    return adjacentTiles.Find(pred => pred.position.X < this.position.X && pred.position.Y == this.position.Y);

                case Direction.NorthEast:
                    return adjacentTiles.Find(pred => pred.position.X == this.position.X && pred.position.Y > this.position.Y);
                case Direction.NorthWest:
                    return adjacentTiles.Find(pred => pred.position.X < this.position.X && pred.position.Y > this.position.Y);
                case Direction.SouthEast:
                    return adjacentTiles.Find(pred => pred.position.X == this.position.X && pred.position.Y < this.position.Y);
                case Direction.SouthWest:
                    return adjacentTiles.Find(pred => pred.position.X < this.position.X && pred.position.Y < this.position.Y);
            }

            return null;
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Hex Tile:");
            sb.AppendLine($"Position: {position}");
            sb.AppendLine($"Tile Type: {tileType}");
            sb.AppendLine($"Base Temperature: {baseTemperature}");
            sb.AppendLine($"Temperature: {temperature}");
            sb.AppendLine($"Humidity: {humidity}");
            sb.AppendLine($"Wind Direction: {windDirection}");
            sb.AppendLine($"Wind Speed: {windSpeed}");
            sb.AppendLine($"Continent ID: {continentID}");
            sb.AppendLine($"Elevation: {elevation}");
            //sb.AppendLine($"Is Occupied: {isOccupied}");

            return sb.ToString();
        }

    }

}