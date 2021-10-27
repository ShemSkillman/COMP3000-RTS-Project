using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* NPCPopulationManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Holds information regarding handling other NPC components request to increase population slots.
    /// </summary>
    [System.Serializable]
    public class PopulationOnDemand
    {
        [SerializeField, Tooltip("Allow other NPC components to request to increase population slots?")]
        private bool enabled = true; //increase the population when another component requests more population slots?
        //if the above option is set to false, then the population manager would ignore all requests to increase population.

        [SerializeField, Tooltip("Between 0.0 and 1.0, the higher the value, the more chance a population slots increase is accepted.")]
        private FloatRange acceptanceRange = new FloatRange(0.5f, 0.7f); //between 0.0 and 1.0, with 0.0 meaning always refuse and 1.0 meaning always accept.
        //if the population on demand is enabled, then a random value X (between 0.0 and 1.0) will be generated and compared to a random value Y between the above...
        //...values and the population increase request will be acceped if it is X <= Y.

        /// <summary>
        /// Determines whether a demand to place a population building is accepted or not.
        /// </summary>
        /// <returns>True if the population building placement demand is accepted, otherwise false.</returns>
        public bool CanAccept () { return enabled && Random.value <= Mathf.Clamp(acceptanceRange.getRandomValue(), 0.0f, 1.0f); }
    }

    /// <summary>
    /// Responsible for managing the NPC faction's population slots.
    /// </summary>
    public class NPCPopulationManager : NPCComponent
    {
        #region Class Properties
        [SerializeField, Tooltip("List of potential population buildings (that add population slots to faction) for the NPC faction.")]
        private List<Building> populationBuildings = new List<Building>(); //main building regulator that the AI will use to increase its population slots.
        private NPCActiveRegulatorMonitor populationBuildingsMonitor = new NPCActiveRegulatorMonitor(); //monitors the active population buildings

        [SerializeField, Tooltip("The target population range that the NPC faction will aim to reach.")]
        private IntRange targetPopulationRange = new IntRange(40, 50); //the population slots that the NPC faction aims to reach
        private int targetPopulation;
        //the amount of population slots that will be given by buildings that are still getting placed by the AI.
        private int pendingPopulationSlots;

        [SerializeField, Tooltip("How often does the NPC faction attempt to update its population and reach its goal?")]
        //timer to send population building requests.
        private FloatRange monitorPopulationReloadRange = new FloatRange(10.0f, 15.0f);
        private float monitorPopulationTimer;
        
        //when checking for free population slots, always add "pendingPopulationSlotsSlots" value
        //when pending population slots+ free population slots <= minfree population slots && <= targetPopulationRange...
        //Activate component, else deactivate

        [SerializeField, Tooltip("How to handle other NPC components requests to increase population slots?")]
        private PopulationOnDemand populationOnDemand = new PopulationOnDemand();

        [SerializeField, Tooltip("Allow this component to automatically place population buildings?")]
        private bool autoPlace = true;
        [SerializeField, Tooltip("When the free population slots reaches this value, this component will attempt to place a new population building.")]
        private int minFreePopulationSlots = 3; //whenever the free population slots amount is <= this value then this would attempt to place a new population building.
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the NPCPopulationManager instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            //pick a target population amount:
            targetPopulation = Random.Range(targetPopulationRange.min, targetPopulationRange.max);

            //pending population is null by default:
            pendingPopulationSlots = 0;

            //start the population monitor timer:
            monitorPopulationTimer = monitorPopulationReloadRange.getRandomValue();

            //custom event listeners:
            CustomEvents.MaxPopulationUpdated += OnPopulationUpdated;
            CustomEvents.CurrentPopulationUpdated += OnPopulationUpdated;

            CustomEvents.BuildingStartPlacement += OnPendingPopulationEnter;
            CustomEvents.BuildingStopPlacement += OnPendingPopulationExit;
            CustomEvents.BuildingBuilt += OnPendingPopulationExit;
        }

        /// <summary>
        /// Activates the NPCBuildingRegulator instances for the NPC faction's population buildings
        /// </summary>
        public void ActivatePopulationBuildingRegulators ()
        {
            populationBuildingsMonitor.Init(factionMgr);

            //Go ahead and add the army units regulators (if there are valid ones)
            foreach (Building building in populationBuildings)
            {
                Assert.IsNotNull(building, 
                    $"[NPCPopulationManager] NPC Faction ID: {factionMgr.FactionID} 'Population Buildings' list has some unassigned elements.");

                Assert.IsTrue(building.GetAddedPopulationSlots() > 0,
                    $"[NPCPopulationManager] NPC Faction ID: {factionMgr.FactionID} 'Population Buildings' list has includes Building of code: {building.GetCode()} that does not add population slots.");

                NPCBuildingRegulator nextRegulator = null;
                //only add the army unit regulators that match this NPC faction's type
                if ((nextRegulator = npcMgr.GetNPCComp<NPCBuildingCreator>().ActivateBuildingRegulator(
                    building,
                    npcMgr.GetNPCComp<NPCBuildingCreator>().GetCapitalBuildingRegualtor())) != null)
                    populationBuildingsMonitor.Replace("", nextRegulator.Code);
            }

            if(populationBuildingsMonitor.GetCount() <= 0)
                Debug.LogWarning($"[NPCPopulationManager] NPC Faction ID: {factionMgr.FactionID} doesn't have any active NPCBuildingRegulator instances for population buildings.");
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        private void OnDisable()
        {
            //custom event listeners:
            CustomEvents.MaxPopulationUpdated -= OnPopulationUpdated;
            CustomEvents.CurrentPopulationUpdated -= OnPopulationUpdated;

            CustomEvents.BuildingStartPlacement -= OnPendingPopulationEnter;
            CustomEvents.BuildingStopPlacement -= OnPendingPopulationExit;
            CustomEvents.BuildingBuilt -= OnPendingPopulationExit;

            populationBuildingsMonitor.Disable();
        }
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Called whenever a faction has its maximum or current populations slots updated.
        /// </summary>
        /// <param name="factionSlot">The FactionSlot instance that manages the faction whose max or current population is updated.</param>
        /// <param name="value">The value by which the max or current population is updated.</param>
        private void OnPopulationUpdated(FactionSlot factionSlot, int value)
        {
            if (factionSlot.ID != factionMgr.FactionID) //not this NPC faction?
                return;

            if(autoPlace //assuming this component can automatically place buildings.
                && factionSlot.GetFreePopulation() + pendingPopulationSlots < minFreePopulationSlots) //if the amount of free population slots is too low
                Activate(); //activate this component so it can push to place population buildings.
        }

        /// <summary>
        /// Called whenever a building has started placement with a potential of having addition of population slots when it's fully constructed.
        /// </summary>
        /// <param name="building">The Building instance that started placement.</param>
        private void OnPendingPopulationEnter(Building building)
        {
            if (building.FactionID == factionMgr.FactionID) //make sure the building belongs to this NPC faction.
                pendingPopulationSlots += building.GetAddedPopulationSlots(); //increase pending population slots amount since it hasn't been added yet to faction slot's population.
        }

        /// <summary>
        /// Called whenever a building has stopped placement or is fully constructed with a potential of having addition of population slots when it's fully constructed.
        /// </summary>
        /// <param name="building">The Building instance that stopped placement or is fully constructed.</param>
        private void OnPendingPopulationExit(Building building)
        {
            if (building.FactionID == factionMgr.FactionID) //make sure the building belongs to this NPC faction.
                pendingPopulationSlots -= building.GetAddedPopulationSlots(); //decrease pending population slots amount as it has been officially added to faction population slots
        }
        #endregion

        #region Placing Population Buildings
        /// <summary>
        /// Runs the monitor population timer.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            if (monitorPopulationTimer > 0.0f)
                monitorPopulationTimer -= Time.deltaTime;
            else
            {
                monitorPopulationTimer = monitorPopulationReloadRange.getRandomValue(); //reload timer

                if (!OnAddPopulationRequest(false)) //attempt to add population building, if not successfull, then stop the monitor population timer
                    Deactivate();
            }
        }

        /// <summary>
        /// Requests to place a new popultion building if all requirements are met.
        /// </summary>
        /// <param name="auto">True if the request is coming from the NPCPopulationManager instance.</param>
        /// <returns>True if the request is accepted by the NPCBuildingCreator instance, otherwise false.</returns>
        public bool OnAddPopulationRequest(bool auto)
        {
            //if this request has been made by another NPC component but it can't be accepted...
            if (!auto && !populationOnDemand.CanAccept()
                //or the NPC faction already reached its target population slots amount
                || factionMgr.Slot.GetMaxPopulation() >= targetPopulation)
                return false;

            //attempt to add population building
            return npcMgr.GetNPCComp<NPCBuildingCreator>().OnCreateBuildingRequest(
                populationBuildingsMonitor.GetRandomCode(), //choose a random population building
                false, //request is coming from outside the NPCBuildingCreator instance component
                null); //building center hasn't been specified
        }
        #endregion
    }
}
