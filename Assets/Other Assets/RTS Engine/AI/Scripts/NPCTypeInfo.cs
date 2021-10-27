using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPCTypeInfo script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Includes data that will be used to regulate the creation of the assigned unit by an NPC faction.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNPCTypeInfo", menuName = "RTS Engine/NPC Type", order = 51)]
    public class NPCTypeInfo : ScriptableObject, IAssetFile
    {
        [SerializeField, Tooltip("Name of the NPC type to be displayed in UI elements.")]
        private string _name = "New NPC Type";
        /// <summary>
        /// Gets the name of the NPC faction type.
        /// </summary>
        /// <returns>Name of the NPC faction type.</returns>
        public string GetName() { return _name; }

        [SerializeField, Tooltip("Unique code for each type of NPC.")]
        private string code = "new_npc_type";
        /// <summary>
        /// Gets the unique code of the NPCFactionType.
        /// </summary>
        public string Key { get { return code; } }

        [SerializeField, Tooltip("Defines NPCManager prefabs to be used with groups of faction types.")]
        private NPCManagerSingle npcManagers = new NPCManagerSingle();
        /// <summary>
        /// Gets the NPCManager prefab that can be used for the input FactionTypeInfo asset file.
        /// </summary>
        /// <param name="factionType">FactionTypeInfo instance to search a match for.</param>
        /// <returns>NPCManager prefab that manages the NPC type with the given faction type.</returns>
        public NPCManager GetNPCManagerPrefab (FactionTypeInfo factionType)
        {
            return npcManagers.Filter(factionType);
        }
    }
}
