using System.Collections;
using System.Collections.Generic;
using WorldSimLib.DataObjects;

namespace WorldSimLib
{

    public class GamePlayer
    {
        public string name;

        public float gold;

        public Inventory Inventory
        {
            get { return _inventory; }
        }

        #region Internal Use Only
        protected GameOracle _oracle;
        protected Inventory _inventory;
        //protected GamePlayerData _playerData;
        protected GameData _gameData;
        #endregion

        public GamePlayer(string name, GameOracle oracle)
        {
            this.name = name;

            _inventory = new Inventory();

            _oracle = oracle;
            _gameData = oracle.GameData;
        }


        public void ProcessTurn()
        {
            // CURRENT AI DIRECTIVES LIST
            // 
            // Look at the market for each game pop center
            // See which products have a deficit
        }

        public virtual void EndTurn(uint turnNumber) { }

    }

}