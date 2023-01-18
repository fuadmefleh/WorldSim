using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace WorldSimAPI
{
    public class MapContentQueryMsg
    {
        public Vector2 startPos;
        public Vector2 endPos;

        public MapContentQueryMsg( Vector2 start, Vector2 end )
        {
            startPos = start;
            endPos = end;
        }
    }

    public class MapContentReplyMsg
    {
        public Vector2 startPos;
        public Vector2 endPos;

        public List<HexTileContentMsg> Tiles { get; set; }

        public MapContentReplyMsg(Vector2 start, Vector2 end)
        {
            startPos = start;
            endPos = end;
            
            Tiles = new List<HexTileContentMsg>();
        }
    }
}
