using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimAPI
{
    public class WorldSimMsg
    {
        public string Topic { get; set; }
        public string Content { get; set; }

        public WorldSimMsg( string topic, string content)
        {
            this.Topic = topic;
            this.Content = content;
        }
    }
}
