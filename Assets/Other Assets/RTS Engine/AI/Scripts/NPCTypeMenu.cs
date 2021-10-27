using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;

namespace RTSEngine
{
    /// <summary>
    /// Used to hold NPCTypeInfo assets that can be used for a NPC faction and assigned from a lobby menu.
    /// </summary>
    [System.Serializable]
    public class NPCTypeMenu
    {
        [SerializeField]
        private NPCTypeInfo[] assets = new NPCTypeInfo[0]; //list of available NPCTypeInfo assets that can be assigned to a NPC faction

        /// <summary>
        /// Gets the names of all assigned NPCTypeInfo assets.
        /// </summary>
        /// <returns>List of assigned NPC type names.</returns>
        public List<string> GetNames () { return assets.Select(prefab => prefab.GetName()).ToList(); }

        /// <summary>
        /// Gets the assigned NPCTypeInfo asset of a given index.
        /// </summary>
        /// <param name="index">Index of the target NPCTypeInfo asset</param>
        /// <returns></returns>
        public NPCTypeInfo Get(int index) { return assets[index]; }

        /// <summary>
        /// Gets all the assigned NPCTypeInfo assets.
        /// </summary>
        /// <returns>IEnumerable instance of assigned NPCTypeInfo element.</returns>
        public IEnumerable<NPCTypeInfo> GetAll() { return assets; }

        /// <summary>
        /// Validates whether NPCTypeInfo assets have been correctly assigned or not.
        /// </summary>
        /// <param name="source">True if NPCTypeInfo assets have been correctly assigned, otherwise false.</param>
        public void Validate (string source)
        {
            Assert.IsTrue(assets.Length > 0, $"[{source}] At least one NPCTypeInfo asset must be assigned.");
            Assert.IsTrue(assets.All(prefab => prefab != null), $"[{source}] Make sure all NPCTypeInfo assets are not null.");
        }
    }
}
