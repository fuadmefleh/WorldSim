using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib
{
    public enum TileGroupType
    {
        Water,
        Land
    }

    public class TileGroup
    {
        public TileGroupType Type;
        public List<HexTile> Tiles;

        public TileGroup()
        {
            Tiles = new List<HexTile>();
        }
    }
}
