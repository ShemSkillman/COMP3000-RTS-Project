using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Animation;
using System.Collections;

namespace RTSEngine
{
    public class Unit : FactionEntity
    {
        public override EntityTypes Type { get { return EntityTypes.unit; } }

        [SerializeField]
        private int populationSlots = 1; //how many population slots will this unit occupy?
        public int GetPopulationSlots() { return populationSlots; }
        public void SetPopulationSlots (int value) { populationSlots = value; }

        [SerializeField]
        private int apcSlots = 1; //this defines the capacity of this unit when it enters an APC
        public int GetAPCSlots () { return apcSlots; }
        public APC currAPC { set; get; }

        [SerializeField]
        private bool canBeConverted = true; //can this unit be converted?
        public bool CanBeConverted() { return canBeConverted; }

        public Building Creator { private set; get; } //the building that produced this unit

        public int LastWorkerPosID { set; get; } //if this unit was constructing/collecting resource, this would the last worker position it had.

        [SerializeField]
        private Animator animator = null; //the animator component
        private UnitAnimatorState currAnimatorState; //holds the current animator state
        public UnitAnimatorState GetCurrAnimatorState() { return currAnimatorState; }
        [SerializeField]
        private AnimatorOverrideController animatorOverrideController = null; //the unit's main animator override controller component
        public bool LockAnimState { set; get; }//When true, it won't be possible to change the animator state using the SetAnimState method.

        [SerializeField, Tooltip("The Transform from which the look at position is set, when the unit spawns.")]
        private Transform spawnLookAt = null;

        //NPC Related:
        [SerializeField, Tooltip("Data required to manage the creation of this unit in a NPC faction.")]
        private NPCUnitRegulatorDataSingle regulatorData = new NPCUnitRegulatorDataSingle();
        /// <summary>
        /// Gets a NPCUnitRegulatorData instance that suits the input requirements.
        /// </summary>
        /// <param name="factionType">FactionTypeInfo instance that defines the faction type of the regulator data.</param>
        /// <param name="npcManagerCode">The NPCManager instance code that defines the NPC Manager type.</param>
        /// <returns>NPCUnitRegulatorData instance if both requirements are met.</returns>
        public NPCUnitRegulatorData GetRegulatorData (FactionTypeInfo factionType, string npcManagerCode) {
            return regulatorData.Filter(factionType, npcManagerCode); }

        //Unit components:
        public UnitHealth HealthComp { private set; get; }
        public Converter ConverterComp { private set; get; }
        public UnitMovement MovementComp { private set; get; }
        public Wander WanderComp { private set; get; }
        public EscapeOnAttack EscapeComp { private set; get; }
        public Builder BuilderComp { private set; get; }
        public ResourceCollector CollectorComp { private set; get; }
        public Healer HealerComp { private set; get; }

        public UnitAttack AttackComp { private set; get; }
        public override void UpdateAttackComp(AttackEntity attackEntity) { AttackComp = (UnitAttack)attackEntity; }

        public void Init(GameManager gameMgr, int fID, bool free, Building createdBy, Vector3 gotoPosition)
        {
            base.Init(gameMgr, fID, free);

            //get the unit's components
            HealthComp = GetComponent<UnitHealth>();
            ConverterComp = GetComponent<Converter>();
            MovementComp = GetComponent<UnitMovement>();
            WanderComp = GetComponent<Wander>();
            EscapeComp = GetComponent<EscapeOnAttack>();
            BuilderComp = GetComponent<Builder>();
            CollectorComp = GetComponent<ResourceCollector>();
            HealerComp = GetComponent<Healer>();

            //initialize them:
            if (ConverterComp)
                ConverterComp.Init(gameMgr, this);
            if (MovementComp)
                MovementComp.Init(gameMgr, this);
            if (WanderComp)
                WanderComp.Init(gameMgr, this);
            if (EscapeComp)
                EscapeComp.Init(gameMgr, this);
            if (BuilderComp)
                BuilderComp.Init(gameMgr, this);
            if (CollectorComp)
                CollectorComp.Init(gameMgr, this);
            if (HealerComp)
                HealerComp.Init(gameMgr, this);
            if (allAttackComp.Length > 0)
                AttackComp = allAttackComp[0] as UnitAttack;
            if (TaskLauncherComp) //if the entity has a task launcher component
                TaskLauncherComp.Init(gameMgr, this); //initialize it

            if (animator == null) //no animator component?
                Debug.LogError("[Unit] The " + GetName() + "'s Animator hasn't been assigned to the 'animator' field");

            if (animator != null) //as long as there's an animator component
            {
                if (animatorOverrideController == null) //if there's no animator override controller assigned..
                    animatorOverrideController = gameMgr.UnitMgr.GetDefaultAnimController();
                ResetAnimatorOverrideController(); //set the default override controller
                //Set the initial animator state to idle
                SetAnimState(UnitAnimatorState.idle);
            }

            if (spawnLookAt) //if we have a set a position for the unit to look at when it is spawned.
                transform.LookAt(spawnLookAt);
            else if (createdBy) //if not, see if there is a building creator for the unit and look in the opposite direction of it.
            {
                Vector3 lookAwayPosition = createdBy.transform.position;
                lookAwayPosition.y = transform.position.y;
                RTSHelper.LookAwayFrom(transform, lookAwayPosition);
            }

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null) //no rigidbody component?
                Debug.LogError("[Unit] The " + GetName() + "'s main object is missing a rigidbody component");

            //rigidbody settings:
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            //set the radius value:
            radius = MovementComp.Controller.Radius;

            //if this is a free unit
            if (this.free)
                UpdateFactionColors(gameMgr.UnitMgr.GetFreeUnitColor()); //set the free unit color
            else
                gameMgr.ResourceMgr.UpdateResource(factionID, initResources); //add the initialization resources to the unit's faction.

            gameMgr.MinimapIconMgr?.Assign(selection); //ask the minimap icon manager to create the a minimap icon for this unit

            SetInitialTargetPosition(createdBy, gotoPosition); //set creator building and make unity move to its goto position

            CanRunComponents = true; //as soon as a unit is initialized, it run its entitiy components.

            CustomEvents.OnUnitCreated(this); //trigger custom event
        }

        //a method that is used to move the unit to its initial position after it spawns:
        public void SetInitialTargetPosition(Building source, Vector3 gotoPosition)
        {
            Creator = source; //set the building creator

            //only if the is owned by the local player or this is not a multiplayer game
            if (RTSHelper.IsLocalPlayer(this) || GameManager.MultiplayerGame == false)
            {
                if (Creator != null) //if the creator is assigned
                    Creator.SendUnitToRallyPoint(this); //send unit to rally point
                else if (Vector3.Distance(gotoPosition, transform.position) > gameMgr.MvtMgr.StoppingDistance) //only if the goto position is not within the stopping distance of this unit
                    gameMgr.MvtMgr.Move(this, gotoPosition, 0.0f, null, InputMode.movement, false); //no creator building? move player to its goto position
            }
        }

        //a method that converts this unit to the converter's faction
        public void Convert(Unit converter, int targetFactionID)
        {
            if (targetFactionID == FactionID) //if the converter and this unit have the same faction, then, what a waste of time and resources.
                return;

            if (GameManager.MultiplayerGame == false) //if this is a single player game
                ConvertLocal(converter, targetFactionID); //convert unit directly
            else if (RTSHelper.IsLocalPlayer(this)) //online game and this is the local player
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.convert,
                    initialPosition = transform.position,
                    value = targetFactionID
                };

                InputManager.SendInput(newInput, this, converter); //send conversion input to the input manager
            }
        }

        //a method that converts this unit to the converter's faction, locally
        public void ConvertLocal(Unit converter, int targetFactionID)
        {
            CustomEvents.OnUnitConversionStart(converter, this);

            Disable(false); //remove it first from its current faction

            AssignFaction(gameMgr.GetFaction(targetFactionID).FactionMgr); //assign the new faction

            if(converter) //if there's a source converter
                converter.ConverterComp.EnableConvertEffect(); //enable the conversion effect on the converter

            //deselect the unit if it was selected:
            if (selection.IsSelected)
                gameMgr.SelectionMgr.Selected.Remove(this);

            CustomEvents.OnUnitConversionComplete(converter, this); //trigger the custom event
        }

        public enum jobType { attack, building, collecting, healing, converting, all} //these are the components that the unit is allowed to have.

        //this method allows to cancel one or more jobs.
        public void CancelJob (jobType[] jobs)
        {
            foreach (jobType job in jobs)
                CancelJob(job);
        }

        public void CancelJob (jobType job)
        {
            if (AttackComp && (job == jobType.all || job == jobType.attack))
                AttackComp.Stop();
            if (BuilderComp && (job == jobType.all || job == jobType.building))
                BuilderComp.Stop();
            if (CollectorComp && (job == jobType.all || job == jobType.collecting))
            {
                CollectorComp.CancelDropOff();
                CollectorComp.Stop();
            }
            if (HealerComp && (job == jobType.all || job == jobType.healing))
                HealerComp.Stop();
            if (ConverterComp && (job == jobType.all || job == jobType.converting))
                ConverterComp.Stop();
        }

        //a method that assings a new faction for the unit
        public void AssignFaction(FactionManager factionMgr)
        {
            FactionMgr = factionMgr; //set the new faction
            FactionID = FactionMgr.FactionID; //set the new faction ID
            free = false; //if this was a free unit then not anymore

            Creator = gameMgr.GetFaction(FactionID).CapitalBuilding; //make the unit's producer, the capital of the new faction

            UpdateFactionColors(gameMgr.GetFaction(FactionID).GetColor()); //set the new faction colors
            selection.UpdateMinimapIconColor(); //assign the new faction color for the unit in the minimap icon

            gameMgr.GetFaction(FactionID).UpdateCurrentPopulation(populationSlots);

            if (TaskLauncherComp != null) //if the unit has a task launcher 
                TaskLauncherComp.Init(gameMgr, this); //update the task launcher info
        }

        //a method that removes this unit from its current faction
        public override void Disable(bool destroyed)
        {
            base.Disable(destroyed);

            if(!free)
                gameMgr.GetFaction(FactionID).UpdateCurrentPopulation(-GetPopulationSlots());

            MovementComp.TargetPositionMarker.Toggle(false); //disable the target position marker component

            MovementComp.Stop(); //stop the unit's movement

            CancelJob(jobType.all); //cancel all jobs
        }

        //See if the unit is in idle state or not:
        public bool IsIdle()
        {
            return !(MovementComp.IsMoving
                || (BuilderComp && BuilderComp.HasTarget)
                || (CollectorComp && CollectorComp.HasTarget)
                || (AttackComp && AttackComp.IsActive && AttackComp.Target != null)
                || (HealerComp && HealerComp.HasTarget)
                || (ConverterComp && ConverterComp.HasTarget));
        }

        //a method to change the animator state
        public void SetAnimState(UnitAnimatorState newState)
        {
            if (LockAnimState == true || animator == null) //if our animation state is locked or there's no animator assigned then don't proceed.
                return;

            if (currAnimatorState == UnitAnimatorState.dead //if the current animation state is not the death one
                || (newState == UnitAnimatorState.takingDamage && HealthComp.IsDamageAnimationEnabled() == false) //if taking damage animation is disabled
                || (HealthComp.IsDamageAnimationActive() && newState != UnitAnimatorState.dead) ) //or if it's enabled and it's in progress and the requested animator state is not a death one
                return;

            currAnimatorState = newState; //update the current animator state

            animator.SetBool(UnitAnimatorParameter.takingDamage, currAnimatorState == UnitAnimatorState.takingDamage);
            animator.SetBool(UnitAnimatorParameter.idle, currAnimatorState==UnitAnimatorState.idle); //stop the idle animation in case take damage animation is played since the take damage animation is broken by the idle anim

            if (currAnimatorState == UnitAnimatorState.takingDamage) //because we want to get back to the last anim state after the taking damage anim is done
                return;

            animator.SetBool(UnitAnimatorParameter.building, currAnimatorState == UnitAnimatorState.building);
            animator.SetBool(UnitAnimatorParameter.collecting, currAnimatorState == UnitAnimatorState.collecting);
            animator.SetBool(UnitAnimatorParameter.moving, currAnimatorState == UnitAnimatorState.moving);
            animator.SetBool(UnitAnimatorParameter.attacking, currAnimatorState == UnitAnimatorState.attacking);
            animator.SetBool(UnitAnimatorParameter.healing, currAnimatorState == UnitAnimatorState.healing);
            animator.SetBool(UnitAnimatorParameter.converting, currAnimatorState == UnitAnimatorState.converting);
            animator.SetBool(UnitAnimatorParameter.dead, currAnimatorState == UnitAnimatorState.dead);
        }

        /// <summary>
        /// Using a parameter in the Animator component, this determines whether the unit is currently in the moving animator state or not.
        /// This allows other components to handle movement related actions smoothly and sync them correctly with the unit's movement
        /// </summary>
        /// <returns>True if the unit is its movement animator state, otherwise false.</returns>
        public bool IsInAnimatorMvtState () { return animator.GetBool(UnitAnimatorParameter.movingState); }

        //a method to change the animator override controller:
        public void SetAnimatorOverrideController(AnimatorOverrideController newOverrideController)
        {
            //only if the unit is not in its dead animation state do we reset the override controller
            //and since all parameters reset when the unit is dead and the unit is locked in its death state
            //reseting the controller makes it start from its "entry state" back to "idle" state, this makes the unit leave its death state while still marked as dead in the currAnimatorState
            if (newOverrideController == null
                || currAnimatorState == UnitAnimatorState.dead)
                return;

            animator.runtimeAnimatorController = newOverrideController; //set the runtime controller to the new override controller
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f); //reload the runtime animator controller

            // Since changing the override controller resets all parameters, we need to re-set the current animator state
            SetAnimState(currAnimatorState);
        }

        private Coroutine animatorResetCoroutine;

        public void ResetAnimatorOverrideControllerOnIdle()
        {
            animatorResetCoroutine = StartCoroutine(HandleResetAnimatorOverrideControllerOnIdle());
        }

        private IEnumerator HandleResetAnimatorOverrideControllerOnIdle()
        {
            yield return new WaitWhile(() => currAnimatorState != UnitAnimatorState.idle);

            ResetAnimatorOverrideController();
        }

        //a method that changes the animator override controller back to the default one
        public void ResetAnimatorOverrideController ()
        {
            if (animatorResetCoroutine != null)
                StopCoroutine(animatorResetCoroutine);

            SetAnimatorOverrideController(animatorOverrideController);
        }
    }
}
