using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* NPCTerritoryManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Responsible for handling expanding on the map for a NPC faction.
    /// </summary>
    public class NPCTerritoryManager : NPCComponent
    {
        #region Class Properties
        //the building regulator for the building center which will be used to expand the territory:
        [SerializeField, Tooltip("Potential list of building centers that a NPC faction can use to expand its territory.")]
        private List<Border> centers = new List<Border>(); //potential buildings that have a Border component
        public NPCActiveRegulatorMonitor centerMonitor = new NPCActiveRegulatorMonitor(); //monitors the active building center regulators

        [SerializeField, Tooltip("Can other NPC components request to expand faction territory?")]
        private bool expandOnDemand = true; //Can other NPC components request to expand the faction's territory?

        //min & max map territory ratio to control:
        //the territory manager will actively attempt at least control the min ratio & will not exceed the max ratio specified below:
        [SerializeField, Tooltip("Minimum and Maximum target ratio of the map's territory to control.")]
        private FloatRange territoryRatio = new FloatRange(0.1f, 0.5f);
        private float currentTerritoryRatio = 0;

        //time reload at which this component decides whether to expand or not
        [SerializeField, Tooltip("Delay (in seconds) before the NPC faction considers expanding.")]
        private FloatRange expandDelayRange = new FloatRange(200.0f, 300.0f);
        private float expandDelayTimer;

        //time reload at which this component decides whether to expand or not
        [SerializeField, Tooltip("How often does the NPC faction check whether to expand or not?")]
        private FloatRange expandReloadRange = new FloatRange(2.0f, 7.0f);
        private float expandTimer;
        #endregion

        #region Initializing/Terminating:
        /// <summary>
        /// Initializes the NPCTerritoryManager instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init (GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            currentTerritoryRatio = 0.0f; //initially set to 0.0f

            //start the expand timers:
            expandDelayTimer = expandDelayRange.getRandomValue();
            expandTimer = expandReloadRange.getRandomValue();

            Activate();

            //listen to delegate events:
            CustomEvents.NPCFactionInit += OnNPCFactionInit;

            CustomEvents.BorderDeactivated += OnBorderDeactivated;
            CustomEvents.BorderActivated += OnBorderActivated;
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        void OnDisable()
        {
            //listen to delegate events:
            CustomEvents.NPCFactionInit -= OnNPCFactionInit;

            CustomEvents.BorderDeactivated -= OnBorderDeactivated;
            CustomEvents.BorderActivated -= OnBorderActivated;

            centerMonitor.Disable();
        }

        /// <summary>
        /// Called when a NPC faction is done initializing its components.
        /// </summary>
        /// <param name="factionSlot">FactionSlot of the NPC faction.</param>
        private void OnNPCFactionInit (FactionSlot factionSlot)
        {
            if (factionSlot.FactionMgr != factionMgr) //different NPC faction?
                return;

            //go through the spawned building centers and add their territory size to the faction's territory count
            foreach(Building buildingCenter in factionMgr.GetBuildingCenters())
                UpdateCurrentTerritory(Mathf.PI * Mathf.Pow(buildingCenter.BorderComp.Size, 2));
        }

        /// <summary>
        /// Activates the main NPCBuildingRegulator instance for the main building center.
        /// </summary>
        public void ActivateCenterRegulator ()
        {
            centerMonitor.Init(factionMgr);

            //Go ahead and add the building center regulator
            foreach (Border center in centers)
            {
                Assert.IsNotNull(center, 
                    $"[NPCTerritoryManager] NPC Faction ID: {factionMgr.FactionID} 'Centers' list has some unassigned elements.");

                NPCBuildingRegulator nextRegulator = null;
                //as soon a center prefab produces a valid building regulator instance (matches the faction type and faction npc manager), add to monitoring
                if ((nextRegulator = npcMgr.GetNPCComp<NPCBuildingCreator>().ActivateBuildingRegulator(
                    center.GetComponent<Building>(),
                    npcMgr.GetNPCComp<NPCBuildingCreator>().GetCapitalBuildingRegualtor())) != null)
                    centerMonitor.Replace("", nextRegulator.Code);
            }

            Assert.IsTrue(centerMonitor.GetCount() > 0, 
                $"[NPCTerritoryManager] NPC Faction ID: {factionMgr.FactionID} doesn't have a center building regulator assigned!");
        }
        #endregion

        #region Border Events Callbacks
        /// <summary>
        /// Called whenever a Border instance is activated.
        /// </summary>
        /// <param name="border">The Border instance that has been activated.</param>
        private void OnBorderActivated (Border border)
        {
            //if the border's building belongs to this faction.
            if (border.building.FactionID == factionMgr.FactionID)
                //increase current territory
                UpdateCurrentTerritory(Mathf.PI * Mathf.Pow(border.Size, 2));
        }

        /// <summary>
        /// Called whenever a Border instance is deactivated.
        /// </summary>
        /// <param name="border">The Border instance that has been deactivated.</param>
        void OnBorderDeactivated(Border border)
        {
            //if the border's building belongs to this faction
            if (border.building.FactionID == factionMgr.FactionID)
                //decrease the current territory ratio.
                UpdateCurrentTerritory(-Mathf.PI * Mathf.Pow(border.Size, 2));
        }
        #endregion

        #region Territory Ratio Manipulation
        /// <summary>
        /// Updates the 'currentTerritoryRatio' value and checks whether max or min territory ratios are met
        /// </summary>
        /// <param name="value">The value to update with.</param>
        private void UpdateCurrentTerritory (float value)
        {
            currentTerritoryRatio += (value/gameMgr.TerrainMgr.GetMapSize()); //update the value.

            if (HasReachedMaxTerritory()) //target maximum territory ratio reached?
                Deactivate();

            else if (!HasReachedMinTerritory()) //minimum territory ratio not met?
                Activate();
        }

        /// <summary>
        /// Did the faction reach the minimum required territory ratio?
        /// </summary>
        /// <returns>True if the faction has reached the minimum required territory ratio, otherwise false.</returns>
        private bool HasReachedMinTerritory ()
        {
            return currentTerritoryRatio >= territoryRatio.min;
        }

        /// <summary>
        /// Did the faction reach the maximum allowed territory ratio?
        /// </summary>
        /// <returns>True if the faction has reached the maximum allowed territory ratio, otherwise false.</returns>
        private bool HasReachedMaxTerritory ()
        {
            return currentTerritoryRatio >= territoryRatio.max;
        }
        #endregion

        #region Expanding Territory
        /// <summary>
        /// Updates territory expansion timer.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            //do not continue if we're still in the delay timer
            if(expandDelayTimer > 0.0f)
            {
                expandDelayTimer -= Time.deltaTime;
                return;
            }

            //expansion timer:
            if (expandTimer > 0)
                expandTimer -= Time.deltaTime;
            else
            {
                //reload timer:
                expandTimer = expandReloadRange.getRandomValue();
                OnExpandRequest(true); //send expansion request
            }
        }

        /// <summary>
        /// Called to request for the NPC faction to expand its territory by creating a new center building.
        /// </summary>
        /// <param name="auto">True if the call was made by the NPCTerritoryManager instance, otherwise false.</param>
        public void OnExpandRequest (bool auto)
        {
            //if this has been requested by another NPC component yet it's not allowed:
            if (auto == false && expandOnDemand == false)
                return; //do not proceed.

            //request building creator to create new instance:
            npcMgr.GetNPCComp<NPCBuildingCreator>().OnCreateBuildingRequest(
                centerMonitor.GetRandomCode(),
                false, 
                npcMgr.GetNPCComp<NPCBuildingCreator>().GetCapitalBuildingRegualtor().buildingCenter);
        }
        #endregion
    }
}
