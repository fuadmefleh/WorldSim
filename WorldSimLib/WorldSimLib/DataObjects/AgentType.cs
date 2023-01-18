using System.Collections;
using System.Collections.Generic;


namespace WorldSimLib.DataObjects
{

    public class Recipe
    {
        public string Name { get; set; }
        public float ChanceOfFailure { get; set; }
        public List<RecipeInput> Inputs { get; set; }
        public List<RecipeOuput> Outputs { get; set; }

        public override string ToString()
        {
            string retStr = "Recipe: \n";

            retStr += Name + "\n";
            retStr += ChanceOfFailure.ToString("##") + "\n";

            foreach (var input in Inputs)
            {
                retStr += input.ToString();
            }

            foreach (var output in Outputs)
            {
                retStr += output.ToString();
            }

            return retStr;
        }
    }

    public class RecipeInput
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public int IdealQuantity { get; set; }
        public float ChanceOfConsuming { get; set; }

        public override string ToString()
        {
            string retStr = "RecipeInput: \n";

            retStr += ItemName + "\n";
            retStr += Quantity.ToString("##") + "\n";
            retStr += IdealQuantity.ToString("##") + "\n";
            retStr += "ChanceOfConsuming: " + ChanceOfConsuming.ToString("##.##") + "\n";

            return retStr;
        }
    }

    public class RecipeOuput
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }

        public override string ToString()
        {
            string retStr = "RecipeOuput: \n";

            retStr += ItemName + "\n";
            retStr += Quantity.ToString("##") + "\n";

            return retStr;
        }
    }

    public class InventorySlot
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }

        public override string ToString()
        {
            string retStr = "InventorySlot: \n";

            retStr += ItemName + "\n";
            retStr += Quantity.ToString("##") + "\n";

            return retStr;
        }
    }

    public class AgentType
    {
        public string Name { get; set; }
        public List<string> RecipeNames { get; set; }

        public float StartingGold { get; set; }
        public List<InventorySlot> StartingInventory { get; set; }

        public List<Recipe> Recipes
        {
            get
            {
                if (_recipes == null)
                {
                    _recipes = new List<Recipe>();

                    foreach (string recipeName in RecipeNames)
                    {
                        _recipes.Add(GameOracle.Instance.GameData.RecipeFromName(recipeName));
                    }
                }

                return _recipes;
            }
        }

        private List<Recipe> _recipes = null;

        public override string ToString()
        {
            string retStr = "AgentType: \n";

            retStr += Name + "\n";

            foreach (var input in RecipeNames)
            {
                retStr += input.ToString() + "\n";
            }

            return retStr;
        }
    }

}