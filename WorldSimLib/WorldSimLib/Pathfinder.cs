using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;


namespace WorldSimLib
{

    public class Pathfinder
    {
        /// <summary>
        /// Finds path from given start point to end point. Returns an empty list if the path couldn't be found.
        /// </summary>
        /// <param name="startPoint">Start tile.</param>
        /// <param name="endPoint">Destination tile.</param>
        public static List<HexTile> FindPath(HexTile startPoint, HexTile endPoint)
        {
            List<HexTile> openPathTiles = new List<HexTile>();
            List<HexTile> closedPathTiles = new List<HexTile>();

            // Prepare the start tile.
            HexTile currentTile = startPoint;

            currentTile.g = 0;
            currentTile.h = GetEstimatedPathCost(startPoint.position, endPoint.position);

            // Add the start tile to the open list.
            openPathTiles.Add(currentTile);

            //return openPathTiles;

            while (openPathTiles.Count != 0)
            {
                // Sorting the open list to get the tile with the lowest F.
                openPathTiles = openPathTiles.OrderBy(x => x.F).ThenByDescending(x => x.g).ToList();
                currentTile = openPathTiles[0];

                // Removing the current tile from the open list and adding it to the closed list.
                openPathTiles.Remove(currentTile);
                closedPathTiles.Add(currentTile);

                int g = currentTile.g + 1;

                // If there is a target tile in the closed list, we have found a path.
                if (closedPathTiles.Contains(endPoint))
                {
                    break;
                }

                // Investigating each adjacent tile of the current tile.
                foreach (HexTile adjacentTile in currentTile.adjacentTiles)
                {
                    // Ignore not walkable adjacent tiles.
                    if (!adjacentTile.Collidable)
                    {
                        continue;
                    }

                    // Ignore the tile if it's already in the closed list.
                    if (closedPathTiles.Contains(adjacentTile))
                    {
                        continue;
                    }

                    // If it's not in the open list - add it and compute G and H.
                    if (!(openPathTiles.Contains(adjacentTile)))
                    {
                        adjacentTile.g = g;
                        adjacentTile.h = GetEstimatedPathCost(adjacentTile.position, endPoint.position);
                        openPathTiles.Add(adjacentTile);
                    }
                    // Otherwise check if using current G we can get a lower value of F, if so update it's value.
                    else if (adjacentTile.F > g + adjacentTile.h)
                    {
                        adjacentTile.g = g;
                    }
                }
            }

            List<HexTile> finalPathTiles = new List<HexTile>();

            // Backtracking - setting the final path.
            if (closedPathTiles.Contains(endPoint))
            {
                currentTile = endPoint;
                finalPathTiles.Add(currentTile);

                for (int i = endPoint.g - 1; i >= 0; i--)
                {
                    currentTile = closedPathTiles.Find(x => x.g == i && currentTile.adjacentTiles.Contains(x));
                    finalPathTiles.Add(currentTile);
                }

                finalPathTiles.Reverse();
            }

            return finalPathTiles;
        }

        /// <summary>
        /// Returns estimated path cost from given start position to target position of hex tile using Manhattan distance.
        /// </summary>
        /// <param name="startPosition">Start position.</param>
        /// <param name="targetPosition">Destination position.</param>
        protected static int GetEstimatedPathCost(Vector3 startPosition, Vector3 targetPosition)
        {
            return (int)Math.Max(Math.Abs(startPosition.Z - targetPosition.Z), Math.Max(Math.Abs(startPosition.X - targetPosition.X), Math.Abs(startPosition.Y - targetPosition.Y)));
        }
    }
}