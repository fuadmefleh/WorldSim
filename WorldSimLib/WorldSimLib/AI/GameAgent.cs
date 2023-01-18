using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using WorldSimLib.DataObjects;
using WorldSimLib.Utils;

namespace WorldSimLib
{
    public class GameAgent
    {
        public string Name { get; set; }
        public float Wealth { get; set; }

        public GameAgent(string name)
        {
            Name = name;
            Wealth = 0;
        }


        public void EndTurn(uint turnNumber)
        {
            
        }

    }
}