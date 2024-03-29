﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldSimLib.DataObjects
{
    public class GameData
    {
        public List<Item> Items { get; set; }
        public List<AgentType> AgentTypes { get; set; }
        public List<Recipe> Recipes { get; set; }
        public List<Resource> Resources { get; set; }
        public List<PopNeed> PopNeeds { get; set; }
        public List<PopAction> PopActions { get; set; }
        public List<PopTask> PopTasks { get; set; }

        public List<PopTechnology> PopTechnologies { get; set; }

        public void Prime()
        {
            PopNeed.Prime(this, PopNeeds);
            PopAction.Prime(this, PopActions);
            PopTask.Prime(this, PopTasks);
            PopTechnology.Prime(this, PopTechnologies);
            Recipe.Prime(this, Recipes);
        }

        public Item ItemFromName(string name)
        {
            return Items.Find(pred => pred.Name == name);
        }

        public PopTechnology TechnologyFromName(string name)
        {
            return PopTechnologies.Find(pred => pred.Name == name);
        }

        public AgentType AgentTypeFromName(string name)
        {
            return AgentTypes.Find(pred => pred.Name == name);
        }

        public AgentType AgentTypeForResource(Resource resource)
        {
            return AgentTypes.Find(pred => pred.Recipes.Any(pred => pred.RequiresResource(resource)));
        }

        public AgentType AgentTypeForItem(string itemName)
        {
            foreach (var agentType in AgentTypes)
            {
                foreach (var recipe in agentType.Recipes)
                {
                    foreach (var output in recipe.Outputs)
                    {
                        if (output.ItemName == itemName)
                            return agentType;
                    }
                }
            }

            return null;
        }

        //public AgentType AgentTypeWithItemInput(string itemName)
        //{
        //    List<AgentType> listOfAgents = new List<AgentType>();

        //    foreach (var agentType in AgentTypes)
        //    {
        //        foreach (var recipe in agentType.Recipes)
        //        {
        //            foreach (var input in recipe.Inputs)
        //            {
        //                if (input.ItemName == itemName)
        //                    listOfAgents.Add(agentType);
        //            }
        //        }
        //    }

        //    if (listOfAgents.Count == 0)
        //        return null;

        //    int randIdx = Random.Range(0, listOfAgents.Count);

        //    return listOfAgents[randIdx];
        //}

        public Recipe RecipeFromName(string name)
        {
            return Recipes.Find(pred => pred.Name == name);
        }

        public Recipe RecipeForItem(string itemName)
        {
            return Recipes.Find(pred => pred.Outputs.Find(output => output.ItemName == itemName) != null);
        }

        public override string ToString()
        {
            string retStr = "Data: \n";

            foreach (Item item in Items)
            {
                retStr += item.ToString();
            }

            foreach (var item in AgentTypes)
            {
                retStr += item.ToString();
            }

            foreach (var item in Recipes)
            {
                retStr += item.ToString();
            }

            return retStr;
        }
    }

}
