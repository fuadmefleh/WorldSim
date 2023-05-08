using System;
using System.Collections.Generic;
using System.Text;
using WorldSimAPI;

namespace WorldSimLib.DataObjects
{
    public class Resource
    {
        public string Name { get; set; }
        public List<BiomeType> Biomes { get; set; }
        public List<HeightType> HeightTypes { get; set; }

        public bool IsWorldGenerated { get; set; }

        public override string ToString()
        {
            string retStr = "Recipe: \n";

            retStr += Name + "\n";
            retStr += Biomes.ToString() + "\n";
            retStr += HeightTypes.ToString() + "\n";

            return retStr;
        }
    }
}
