using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* NPCBuildingConstructor script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Responsible for constructing buildings for a NPC faction.
    /// </summary>
    public class NPCBuildingConstructor : NPCComponent
    {
        #region Component Properties
        [SerializeField, Tooltip("Potential list of units with the Builder component that the NPC faction can use.")]
        private List<Builder> builders = new List<Builder>(); //potential units with a builder component.
        private NPCActiveRegulatorMonitor builderMonitor = new NPCActiveRegulatorMonitor(); //monitors the active instances of the builders

        private List<Building> buildingsToConstruct = new List<Building>(); //holds a list of the faction buildings that need construction.
        [SerializeField, Tooltip("How often does this component check whether are buildings to construct/repare?")]
        //timer that each it's through and this component is active: it goes over the buildings in the above list and sends builders to construct it.
        private FloatRange constructionTimerRange = new FloatRange(4.0f, 7.0f);
        private float constructionTimer;

        [SerializeField, Tooltip("Constructing buildings is added to the NPCTaskManager and then handled there, this is the construction task initial priority.")]
        private int constructionTaskPriority = 1;

        //the value below represensts the ratio of the builders that will be assigned to each building:
        [SerializeField, Tooltip("Amount of builders to send for construction to the maximum allowed amount of builders of a building ratio.")]
        private FloatRange targetBuildersRatio = new FloatRange(0.5f, 0.8f);

        [SerializeField, Tooltip("When enabled, other NPC components can request to construct buildings.")]
        private bool constructOnDemand = true; //if another NPC component requests the construction of one of a building, is it allowed or not?
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the NPCBuildingConstructor instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            //reset construction timer:
            constructionTimer = constructionTimerRange.getRandomValue();

            Activate();

            ActivateBuilderRegulator();

            //add event listeners for following delegate events:
            CustomEvents.BuildingPlaced += OnBuildingPlaced;
            CustomEvents.BuildingHealthUpdated += OnBuildingHealthUpdated;
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        private void OnDisable()
        {
            //remove delegate event listeners:
            CustomEvents.BuildingPlaced -= OnBuildingPlaced;
            CustomEvents.BuildingHealthUpdated -= OnBuildingHealthUpdated;

            builderMonitor.Disable();
        }

        /// <summary>
        /// Activates the main NPCUnitRegulator instance for the main builder unit.
        /// </summary>
        private void ActivateBuilderRegulator ()
        {
            builderMonitor.Init(factionMgr);

            //Go ahead and add the builder regulator (if there's one)..
            foreach (Builder builder in builders)
            {
                Assert.IsNotNull(builder, 
                    $"[NPCBuildingConstructor] NPC Faction ID: {factionMgr.FactionID} 'Builders' list has some unassigned elements.");

                NPCUnitRegulator nextRegulator = null;
                //as soon a builder prefab produces a valid builder regulator instance (matches the faction type and faction npc manager), add it to be monitored:
                if ((nextRegulator = npcMgr.GetNPCComp<NPCUnitCreator>().ActivateUnitRegulator(builder.GetComponent<Unit>())) != null)
                    builderMonitor.Replace("", nextRegulator.Code);
            }

            Assert.IsTrue(builderMonitor.GetCount() > 0, 
                $"[NPCBuildingConstructor] NPC Faction ID: {factionMgr.FactionID} doesn't have a builder regulator assigned!");
        }
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Called when a building is placed.
        /// </summary>
        /// <param name="building">The Building instance that got placed.</param>
        private void OnBuildingPlaced (Building building)
        {
            if (building.FactionID == factionMgr.FactionID) //if it belongs to this faction
                //add it to the buildings in construction list:
                OnBuildingEnterConstruction(building);
        }

        /// <summary>
        /// Called when a building's health has been updated.
        /// </summary>
        /// <param name="building">The Building instance that had its health updated.</param>
        /// <param name="value">How much did the building's health been updated by?</param>
        /// <param name="source">The FactionEntity instance responsible for this health update.</param>
        private void OnBuildingHealthUpdated (Building building, int value, FactionEntity source)
        {
            //if the building belongs to this faction:
            if (building.FactionID == factionMgr.FactionID)
            {
                //if the building doesn't have max health:
                if (building.HealthComp.CurrHealth < building.HealthComp.MaxHealth)
                    //add it to the buildings in construction list.
                    OnBuildingEnterConstruction(building);
                else
                    //remove it from the buildings in construction list.
                    OnBuildingExitConstruction(building);
            }
        }
        #endregion

        #region Building Construction
        /// <summary>
        /// Called when a building that belongs to the NPC faction enters its construction state.
        /// </summary>
        /// <param name="building">The Building instance that entered construction.</param>
        private void OnBuildingEnterConstruction (Building building)
        {
            if (buildingsToConstruct.Contains(building)) //if the building is already in the constructions list
                return;

            buildingsToConstruct.Add(building); //add building to constructions buildings list
            Activate(); //NPC Building Constructor is now active again.

            //add the construction task to the task manager queue:
            npcMgr.GetNPCComp<NPCTaskManager>().AddTask(new NPCTask()
            {
                type = NPCTaskType.constructBuilding,
                target = building
            }, constructionTaskPriority);
        }
        
        /// <summary>
        /// Called when a building that belongs to the NPC faction exists its construction state.
        /// </summary>
        /// <param name="building">The Building instance that is no longer under construction.</param>
        private void OnBuildingExitConstruction (Building building)
        {
            buildingsToConstruct.Remove(building); //simply remove the building from the list
        }

        /// <summary>
        /// Checks whether a building construction status is handled by the NPCBuildingConstructor instance or not.
        /// </summary>
        /// <param name="building">The Building instance to check.</param>
        /// <returns>True if the building's construction is managed by the NPCBuildingConstructor instance, otherwise false.</returns>
        public bool IsBuildingUnderConstruction (Building building)
        {
            return buildingsToConstruct.Contains(building);
        }

        /// <summary>
        /// Updates the construction check timer when the NPCBuildingConstructor instance is active.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            //checking buildings timer:
            if (constructionTimer > 0)
                constructionTimer -= Time.deltaTime;
            else
            {
                //reset construction timer:
                constructionTimer = constructionTimerRange.getRandomValue();

                //set to non active at the beginning.
                Deactivate();

                //go through the buildings to construct list
                foreach (Building b in buildingsToConstruct)
                {
                    //there are still buildings to consruct, then:
                    Activate(); //we'll be checking again soon.

                    //see if the amount hasn't been reached:
                    if (GetTargetBuildersAmount(b) > b.WorkerMgr.currWorkers)
                    {
                        //request to send more workers then:
                        OnBuildingConstructionRequest(b, true, false);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the amount of builders to send to a building for construction.
        /// </summary>
        /// <param name="building">The Building instance that is under construction.</param>
        /// <returns>Amount of builders to send to construct building.</returns>
        public int GetTargetBuildersAmount (Building building)
        {
            //how many builders do we need to assign for this building?
            int targetBuildersAmount = (int)(building.WorkerMgr.GetAvailableSlots() * targetBuildersRatio.getRandomValue());

            return Mathf.Max(targetBuildersAmount, 1); //can't be lower than 1
        }

        /// <summary>
        /// Called to request the NPCBuildingConstructor instance to send builders to construct a building.
        /// </summary>
        /// <param name="building">The Building instance to construct.</param>
        /// <param name="auto">True if the request is coming from the actual NPCBuildingConstructor instance.</param>
        /// <param name="force">True if the building's construction is urged.</param>
        public void OnBuildingConstructionRequest(Building building, bool auto, bool force)
        {
            if (building == null //if the building instance is not valid
                || building.HealthComp.CurrHealth >= building.HealthComp.MaxHealth //or if the building has enough health points
                || (!force && auto == false && constructOnDemand == false)) //or if this is a request from another NPC component and this component doesn't allow that.
                return; //do not proceed.

            //how much builders does this building can have?
            int requiredBuilders = GetTargetBuildersAmount(building) - building.WorkerMgr.currWorkers;

            int i = 0; //counter.
            List<Unit> currentBuilders = npcMgr.GetNPCComp<NPCUnitCreator>().GetActiveUnitRegulator(builderMonitor.GetRandomCode()).GetIdleUnitsFirst(); //get the list of the current faction builders.

            //while we still need builders for the building and we haven't gone through all builders.
            while (i < currentBuilders.Count && requiredBuilders > 0)
            {
                //making sure the builder is valid:
                if (currentBuilders[i] != null)
                    //is the builder currently in idle mode or do we force him to construct this building?
                    //& make sure it's not already constructing a building.
                    if ((currentBuilders[i].IsIdle() || force == true) && !currentBuilders[i].BuilderComp.HasTarget)
                    {
                        //send to construct the building:
                        currentBuilders[i].BuilderComp.SetTarget(building);
                        //decrement amount of required builders:
                        requiredBuilders--;
                    }

                i++;
            }
        }
        #endregion
    }
}
