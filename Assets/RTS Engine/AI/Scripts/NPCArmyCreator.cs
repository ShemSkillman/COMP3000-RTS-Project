using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using RTSEngine.EntityComponent;

/* NPCArmyCreator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Responsible for forcing the creation of army (attack) units.
    /// </summary>
    public class NPCArmyCreator : NPCComponent
    {
        #region Class Properties
        //holds the regulators of the army units
        [SerializeField, Tooltip("A list of units that will participate when the NPC faction attacks another faction.")]
        private List<UnitAttack> armyUnits = new List<UnitAttack>();
        private NPCActiveRegulatorMonitor armyUnitsMonitor = new NPCActiveRegulatorMonitor(); //monitors the army units regulator instances

        //forcing army creation = minimum amount of the active unit regulator instances will be incremented to force faction to create them.
        [SerializeField, Tooltip("When enabled, creation of the minimum amount of every army unit will be prioritized.")]
        private bool forceArmyCreation = false;
        //timer at which the minimum amount of the above active regulators will be incremented in order to push for the army units creation
        [SerializeField, Tooltip("How often does the NPC faction check the forced creation of the army units?")]
        private FloatRange forceCreationReloadRange = new FloatRange(10.0f, 15.0f);
        private float forceCreationTimer;
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the NPCArmyCreator instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            //only activate if army unit creation is forced.
            if (forceArmyCreation)
                Activate();
            else
                Deactivate();

            ActivateArmyUnitRegulators();
        }

        /// <summary>
        /// Activates the NPCUnitRegulator instances for the NPC faction's army units.
        /// </summary>
        public void ActivateArmyUnitRegulators ()
        {
            armyUnitsMonitor.Init(factionMgr);

            //Go ahead and add the army units regulators (if there are valid ones)
            foreach (UnitAttack armyUnit in armyUnits)
            {
                Assert.IsNotNull(armyUnit, 
                    $"[NPCArmyCreator] NPC Faction ID: {factionMgr.FactionID} 'Army Unit' list has some unassigned elements.");

                NPCUnitRegulator nextRegulator = null;
                //only add the army unit regulators that match this NPC faction's type
                if ((nextRegulator = npcMgr.GetNPCComp<NPCUnitCreator>().ActivateUnitRegulator(armyUnit.GetComponent<Unit>())) != null)
                    armyUnitsMonitor.Replace("", nextRegulator.Code);
            }

            Assert.IsTrue(armyUnitsMonitor.GetCount() > 0, 
                $"[NPCArmyCreator] NPC Faction ID: {factionMgr.FactionID} doesn't have any active NPCUnitRegulator instance for army units!");
        }
        #endregion

        #region Monitoring Forced Army Creation
        /// <summary>
        /// Actively monitors forced army units creation.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            Deactivate();

            //go through the active instances of the army unit regulators:
            foreach (string unitCode in armyUnitsMonitor.GetAll())
            {
                NPCUnitRegulator unitRegulator = npcMgr.GetNPCComp<NPCUnitCreator>().GetActiveUnitRegulator(unitCode); //get the regulator instance for the unit code
                if (unitRegulator == null)
                    print(unitCode);

                //if the minimum amount hasn't hit the actual max amount
                if (unitRegulator.MaxAmount > unitRegulator.MinAmount)
                {
                    Activate(); //keep component active.
                    //increment the minimum amount to put pressure on creating a new instance for the army unit.
                    unitRegulator.IncMinAmount();
                }
            }
        }

        /// <summary>
        /// Activates the forced army units creation for the NPC faction.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            forceArmyCreation = true;
        }

        /// <summary>
        /// Deactivates the forced army units creation for the NPC faction.
        /// </summary>
        public override void Deactivate()
        {
            base.Activate();
            forceArmyCreation = false;
        }
        #endregion
    }
}
