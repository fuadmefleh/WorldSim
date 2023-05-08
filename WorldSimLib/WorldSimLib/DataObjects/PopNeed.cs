using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WorldSimLib.DataObjects
{
    public class PopItemNeed
    {
        public string ItemName { get; set; }
        public int Qty { get; set; }
    }

    public class PopNeed
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public int MinSOLLevel { get; set; }

        public int MaxSOLLevel { get; set; }

        public List<ItemType> AssociatedItemTypes { get; set; }

        public static void Prime( GameData data, List<PopNeed> needsToPrime )
        {
           
        }
    }
}
