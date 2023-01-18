using System;
using System.Collections.Generic;
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
        public float WealthLevel { get; set; }
        public List<PopItemNeed> PopItemNeeds { get; set; }
    }
}
