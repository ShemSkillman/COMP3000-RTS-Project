using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* NPCBuildingRegulator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Regulates the creation of a building type for NPC factions.
    /// </summary>
    public class NPCBuildingRegulator : NPCRegulator<Building>
    {
        #region Class Properties
        /// <summary>
        /// Holds information regarding the creation of the building type.
        /// </summary>
        public NPCBuildingRegulatorData Data { private set; get; } 

        /// <summary>
        /// The Building prefab that is regulated by the NPCBuildingRegulator intance.
        /// </summary>
        public Building Prefab { private set; get; } //the Building prefab that is managed by this component

        private Building buildingCenter; //the building center where the instance of the regulator is active.

        //building creator:
        private NPCBuildingCreator buildingCreator_NPC; //the NPC Building Creator used to create all instances of the buildings regulated here.
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// NPCBuildingRegulator constructor.
        /// </summary>
        /// <param name="data">Holds information regarding how the building type that will be regulated.</param>
        /// <param name="prefab">Actual Building prefab to regulate.</param>
        /// <param name="gameMgr">GameManager instance of the currently active game.</param>
        /// <param name="npcMgr">NPCManager instance that manages the NPC faction to whome the regulator component belongs.</param>
        /// <param name="buildingCreator_NPC">NPCBuildingCreator instance of the NPC faction that's responsible for creating buildings.</param>
        public NPCBuildingRegulator (NPCBuildingRegulatorData data, Building prefab, GameManager gameMgr, NPCManager npcMgr, NPCBuildingCreator buildingCreator_NPC, Building buildingCenter)
            : base(data, prefab, gameMgr, npcMgr)
        {
            this.Data = data;
            Assert.IsNotNull(this.Data,
                $"[NPCBuildingRegulator] Initializing without NPCBuildingRegulatorData instance is not allowed!");

            this.buildingCreator_NPC = buildingCreator_NPC;
            Assert.IsNotNull(this.buildingCreator_NPC,
                $"[NPCBuildingRegulator] Initializing without a reference to the NPCBuildingCreator instance is not allowed!");

            this.Prefab = prefab;
            Assert.IsNotNull(this.Prefab,
                $"[NPCBuildingRegulator] Initializing without a reference to the building's prefab is not allowed!");

            this.buildingCenter = buildingCenter;
            Assert.IsNotNull(this.Prefab,
                $"[NPCBuildingRegulator] Initializing without a reference to the regulator's building center is not allowed!");

            //go through all spawned buildings to see if the buildings that should be regulated by this instance are created or not:
            foreach (Building b in this.factionMgr.GetBuildings())
                AddExisting(b);

            //start listening to the required delegate events:
            CustomEvents.BuildingDestroyed += Remove;
            CustomEvents.BuildingStopPlacement += Remove;
            CustomEvents.BuildingStartPlacement += AddPending;
            CustomEvents.BuildingPlaced += Add;
        }

        /// <summary>
        /// Disables the NPCBuildingRegulator instance.
        /// </summary>
        public void Disable()
        {
            //stop listening to the delegate events:
            CustomEvents.BuildingDestroyed -= Remove;
            CustomEvents.BuildingStopPlacement -= Remove;
            CustomEvents.BuildingStartPlacement -= AddPending;
            CustomEvents.BuildingPlaced -= Add;
        }
        #endregion

        #region Spawned Instances Manipulation
        /// <summary>
        /// Tests whether a building can be regulated by the NPCBuildingRegulator instance or not.
        /// </summary>
        /// <param name="factionEntity">The Building instance to test.</param>
        /// <returns>True if the building can be regulated by the NPCBuildingRegulator instance, otherwise false.</returns>
        public override bool CanBeRegulated(Building building)
        {
            return base.CanBeRegulated(building) 
                && (!Data.RegulatePerBuildingCenter
                    || building.PlacerComp.PlaceOutsideBorder
                    || (building.CurrentCenter && buildingCenter == building.CurrentCenter.building)); //or it is regularted per building center and it belongs to the same one tracked by this conponent
        }

        /// <summary>
        /// Called when a Building instance is successfully removed from being tracked and regulated by the NPCBuildingRegulator instance.
        /// </summary>
        /// <param name="building">Building instance that was removed.</param>
        protected override void OnSuccessfulRemove (Building building)
        {
            if(!HasReachedMaxAmount()) //and maximum allowed amount hasn't been reached yet
                buildingCreator_NPC.Activate(); //activate building creator to create more instances of this unit type.
        }
        #endregion
    }
}
