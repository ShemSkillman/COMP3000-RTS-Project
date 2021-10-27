using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;

/* NPCManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Responsible for initializing and managing all NPC components for a NPC faction.
    /// </summary>
    public class NPCManager : MonoBehaviour
    {
        #region Class Properties
        /// <summary>
        /// The NPCTypeInfo asset that is responsible for launching this instance of NPCManager.
        /// </summary>
        public NPCTypeInfo NPCType { private set; get; }

        //Holds the NPC components that extend NPCComponent that regulate the behavior of this instance of a NPC faction
        private Dictionary<Type, NPCComponent> npcCompDic = new Dictionary<Type, NPCComponent>();

        //other components
        GameManager gameMgr;
        public FactionManager FactionMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes NPCManager instance component from NPC faction's FactionSlot instance.
        /// </summary>
        /// <param name="npcType">The NPCTypeInfo instance that defines the NPC faction's type.</param>
        /// <param name="gameMgr">GameManager instance of currently active game.</param>
        /// <param name="factionMgr">FactionManager instance of the NPC faction.</param>
        public void Init(NPCTypeInfo npcType, GameManager gameMgr, FactionManager factionMgr)
        {
            this.NPCType = npcType;
            this.gameMgr = gameMgr;
            this.FactionMgr = factionMgr;

            //subscribe to event
            CustomEvents.FactionDefaultEntitiesInit += OnFactionDefaultEntitiesInit;
        }

        /// <summary>
        /// Called when the NPCManager instance is destroyed.
        /// </summary>
        private void OnDisable()
        {
            CustomEvents.FactionDefaultEntitiesInit -= OnFactionDefaultEntitiesInit;

            //destroy the active unit regulators
            GetNPCComp<NPCUnitCreator>().DestroyAllActiveRegulators();
            //destroy the active building regulators:
            GetNPCComp<NPCBuildingCreator>().DestroyAllActiveRegulators();
        }
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Called when the default entities of a faction are initialized.
        /// </summary>
        /// <param name="factionSlot">FactionSlot instance of the faction whose default entities are initialized.</param>
        private void OnFactionDefaultEntitiesInit(FactionSlot factionSlot)
        {
            if (factionSlot.FactionMgr == FactionMgr) //if the faction slot is handled by this component
            {
                foreach (NPCComponent comp in GetComponentsInChildren<NPCComponent>()) //go through the NPC components and init them
                {
                    npcCompDic.Add(comp.GetType(), comp);
                    comp.Init(this.gameMgr, this, this.FactionMgr);
                }

                CustomEvents.OnNPCFactionInit(FactionMgr.Slot);
            }
        }
        #endregion

        #region NPC Component Handling
        /// <summary>
        /// Gets the NPCComponent instance of a given type that belongs to the NPC faction regulated by this instance of the NPCManager.
        /// /// </summary>
        /// <typeparam name="T">Type that extends NPCComponent.</typeparam>
        /// <returns>Active instance of the NPCComponent extended type</returns>
        public T GetNPCComp<T> () where T : NPCComponent
        {
            Assert.IsTrue(npcCompDic.ContainsKey(typeof(T)),
                $"[NPCManager] NPC Faction ID {FactionMgr.FactionID} does not have an active instance of NPCComponent type: {typeof(T)}!");

            if (npcCompDic.TryGetValue(typeof(T), out NPCComponent value))
                return value as T;

            return null;
        }
        #endregion
    }
}
