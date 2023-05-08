using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.DataObjects
{
    public class PopTechnology
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public List<string> Dependencies { get; set; }
        public List<PopTechnology> TechDependencies { get; set; }

        public static void Prime(GameData data, List<PopTechnology> technologiesToPrime)
        {
            foreach (var technology in technologiesToPrime)
            {
                technology.TechDependencies = new List<PopTechnology>();

                foreach (var dependency in technology.Dependencies)
                {
                    var popTech = data.PopTechnologies.Find(pred => pred.ID == dependency);

                    if (popTech != null)
                    {
                        technology.TechDependencies.Add(popTech);
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Failed to find popTech for id: " + dependency);
                    }
                }
            }
        }
    }
}
