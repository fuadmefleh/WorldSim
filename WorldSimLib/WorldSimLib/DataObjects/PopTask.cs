using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.DataObjects
{
    public class PopTask
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public List<string> Actions { get; set; }
        public List<PopAction> PopActions { get; set; }
        public static void Prime(GameData data, List<PopTask> tasksToPrime)
        {
           
        }
    }
}
