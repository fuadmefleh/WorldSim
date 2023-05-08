using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.DataObjects
{
    public class PopAction
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public List<string> TechnologyRequired { get; set; }
        public List<PopTechnology> PopTechRequired { get; set; }

        public static void Prime(GameData data, List<PopAction> actionsToPrime)
        {
            
        }
    }
}
