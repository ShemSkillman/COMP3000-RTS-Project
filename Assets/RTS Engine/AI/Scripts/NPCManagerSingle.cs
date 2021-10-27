using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPCManagerSingle script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Defines groups of FactionTypeInfo asset files where each group uses a single NPCManager prefab.
    /// </summary>
    [System.Serializable]
    public class NPCManagerSingle : TypeFilteredValue<FactionTypeInfo, NPCManager>
    {
        /// <summary>
        /// Each instance includes a list of FactionTypeInfo asset files that share one NPCManager prefab.
        /// </summary>
        [System.Serializable]
        public struct Element
        {
            public List<FactionTypeInfo> factionTypes;
            public NPCManager prefab;
        }
        [SerializeField, Tooltip("Each element allows to define a group of faction types that share one NPCManager prefab.")]
        private List<Element> typeSpecific = new List<Element>();

        /// <summary>
        /// Filters the content of the typeSpecific list depending on the given faction type.
        /// </summary>
        /// <param name="factionType">FactionTypeInfo instance to search for a match for.</param>
        /// <returns>NPCManager prefab that matches the given faction type.</returns>
        public override NPCManager Filter(FactionTypeInfo searchType)
        {
            filtered = allTypes; //NPCManager prefab assigned to the allTypes element is available for all faction type

            //as for faction type specific NPCManager prefabs:
            foreach (Element e in typeSpecific)
                if (e.factionTypes.Contains(searchType))
                {
                    filtered = e.prefab;
                    break;
                }

            return filtered; //filtered element is now the searched NPCManager prefab.
        }
    }
}
