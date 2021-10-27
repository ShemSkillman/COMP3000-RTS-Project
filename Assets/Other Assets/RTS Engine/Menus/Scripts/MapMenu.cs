using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace RTSEngine
{
    [System.Serializable]
    public class MapMenu
    {
        [SerializeField]
        private string scene = "map_scene"; //the scene's name.
        public string GetScene() { return scene; }

        [SerializeField]
        private string name = "map_name"; //the map's name to be displayed in the UI.
        public string GetName() { return name; }

        [SerializeField]
        private string description = "map_description"; //the map's description.
        public string GetDescription() { return description; }

        [SerializeField, Min(1)]
        private int minFactions = 2; //the minimum amount of factions that this map supports
        public int GetMinFactions() { return minFactions; }

        [SerializeField, Min(1)]
        private int maxFactions = 4; //the maximum amount of factions that this map supports
        public int GetMaxFactions() { return maxFactions; }

        [SerializeField]
        private int initialPopulation = 5; //the map's initial population.
        public int GetInitialPopulation() { return initialPopulation; }

        [SerializeField]
        private FactionTypeInfo[] factionTypes = new FactionTypeInfo[0]; //the available types of factions that can play in this map.

        public FactionTypeInfo GetFactionTypeInfo(int index) //get the faction type info with a certain index
        {
            return factionTypes[index];
        }

        public List<string> GetFactionTypeNames() //returns the names of the faction types in this map
        {
            List<string> names = new List<string>();
            foreach (FactionTypeInfo factionType in factionTypes)
                names.Add(factionType.GetName());

            return names;
        }

        public void Validate(int ID, string source)
        {
            Assert.IsNotNull(scene, $"[{source}] Invalid scene assigned to map ID {ID}");
            Assert.IsTrue(maxFactions >= 2, $"[{source}] Map ID {ID} maximum amount of factions must be at least 2");
            Assert.IsTrue(maxFactions >= 1, $"[{source}] Map ID {ID} initial population must be at least 1");
        }
    }
}
