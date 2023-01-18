using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI;

namespace WorldSimLib
{


	public class River
	{
		public int Length;
		public List<HexTile> Tiles;
		public int ID;

		public int Intersections;
		public float TurnCount;
		public Direction CurrentDirection;

		public River(int id)
		{
			ID = id;
			Tiles = new List<HexTile>();
		}

		public void AddTile(HexTile tile)
		{
		//	tile.SetRiverPath(this);
			Tiles.Add(tile);
		}
	}

	public class RiverGroup
	{
		public List<River> Rivers = new List<River>();
	}
}
