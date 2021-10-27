using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

using RTSEngine.Attack;
using RTSEngine.UI;

/* AttackEntity script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(FactionEntity))]
    public abstract class AttackEntity : MonoBehaviour, IEntityComponent
    {

#if UNITY_EDITOR
        public int tabID = 0;
#endif
        /// <summary>
        /// FactionEntity instance that the AttackEntity component is attached to.
        /// </summary>
        public FactionEntity FactionEntity { private set; get; }
        /// <summary>
        /// Entity instance that the AttackEntity component is attached to.
        /// </summary>
        public Entity Entity { get { return FactionEntity; } }

        //general attributes:
        [SerializeField]
        private bool isLocked = false; //is the attack type locked? when locked, the player can't switch to it
        public bool IsLocked
        {
            set { isLocked = value; }
            get { return isLocked; }
        }

        [SerializeField, Tooltip("Can the attack be used by default?")]
        private bool isActive = true;
        /// <summary>
        /// Is the faction entity able to use the attack defined in the AttackEntity component?
        /// </summary>
        public bool IsActive
        {
            get { return isActive && !FactionEntity.EntityHealthComp.IsDead(); }
            set
            {
                isActive = value;

                if (attackTaskUI != null && attackTaskUI.Data.enabled //the task is valid and enabled.
                    && RTSHelper.IsPlayerFaction(FactionEntity)) //if the faction entity belongs to the player's faction then update the attack task in case it has been already drawn
                    CustomEvents.OnEntityComponentTaskReloadRequest(this, attackTaskUI.Data.code);
            }
        }

        [SerializeField]
        private string code = "new_attack_code"; //input a unique code for each attack type
        public string GetCode() { return code; }

        [SerializeField, Tooltip("The attack type that is enabled by default.")]
        private bool basic = true; //is this is the basic attack that is enabled by default for the faction entity?
        /// <summary>
        /// Is the AttackEntity instance marked as the basic/default attack for the faction entity?
        /// </summary>
        public bool IsBasic { get { return basic; } }

        //a list of the buildings/units codes that this faction will attempt to attack:
        [SerializeField, Tooltip("Define the buildings/units that can be targetted and attacked using their code and/or categories.")]
        private AttackTargetPicker targetPicker = new AttackTargetPicker();

        //attack type-related attributes:
        [SerializeField]
        private bool direct = false; //when true, the faction entity will simply trigger the attack without sending an attack object.
        [SerializeField]
        private bool engageOnAssign = true; //can this attack entity engage an enemy when the local player assings it to?
        public bool CanEngageOnAssign() { return engageOnAssign; }
        [SerializeField]
        private bool engageWhenAttacked = false; //can this attack entity engage units that attack it?
        public bool CanEngageWhenAttacked() { return engageWhenAttacked; }
        [SerializeField]
        private bool engageOnce = false; //does this attack entity trigger one attack then stops? till it's reassigned to engage once again
        [SerializeField]
        private bool engageFriendly = false; //can this attack entity engage friendly faction entites?
        public bool CanEngageFriendly() { return engageFriendly; }

        [SerializeField]
        private bool engageInRange = true; //can the attack entity engage enemy units when they are within a certain range?
        [SerializeField, Tooltip("Disable to allow faction entities that are not in idle state to search for attack targets.")]
        private bool engageInRangeIdleOnly = true; //can the attack entity engage enemies in range while it is in idle only?
        [SerializeField]
        protected float searchRange = 10.0f; //the range where the attack entity can search for enemy units to engage if the above field is enable
        [SerializeField]
        private float searchReload = 1.0f; //how frequent will the attack entity look for targets to engage?
        private float searchTimer;

        //AI-related attributes
        public Border SearchRangeCenter { set; get; } //decides which building center this AI attack entity must protect

        //delay-related attributes:
        [SerializeField]
        private float delayDuration = 0.0f; //how long does the delay last for?
        protected float delayTimer;

        [SerializeField]
        private bool delayTriggerEnabled = false; //if set to true, then another component (other than this component must trigger the attack).
        protected bool triggered = false; //is attack already triggered?
        public void TriggerAttack() { triggered = true; }

        //reload-related attributes:
        [SerializeField]
        private bool useReload = true; //does this attack type have a reload time?
        [SerializeField]
        private float reloadDuration = 2.0f; //duration of the reload
        protected float reloadTimer = 0.0f;

        //cooldown-related attributes:
        [SerializeField]
        private bool coolDownEnabled = false; //enable cooldown before the attack entity can pick another target
        public bool CoolDownActive { private set; get; }
        [SerializeField]
        private float coolDownDuration = 10.0f; //how long would the cooldown last?
        protected float coolDownTimer;

        [SerializeField]
        private bool requireTarget = true; //does this attack type require a target to be assigned in order to launch it?
        public bool RequireTarget() { return requireTarget; }
        private bool terrainAttackActive = false; //if the attacker doesn't require a target and this is enabled -> attacker is about to launch attack to land on terrain

        private bool HasTarget() { return Target != null || (!requireTarget && terrainAttackActive); }
        public FactionEntity Target { private set; get; } //the current attack target
        //the target position is either the assigned target's current position or the last registered target position
        public Vector3 GetTargetPosition() { return Target ? Target.GetSelection().transform.position : lastTargetPosition; }
        protected Vector3 lastTargetPosition; //where was the attack target when the last attack was triggered?

        protected bool wasInTargetRange = false; //is the attack entity already in its target range?
        protected float initialEngagementDistance = 0.0f; //holds the distance between the target and the attacker when the attacker first enters in range of the target.

        [SerializeField]
        private AttackDamage damage = new AttackDamage(); //handles attack type damage settings
        public AttackDamage GetDamageSettings() { return damage; }
        private int dealtDamage; //amount of damage that the attack entity deal to its target(s)
        public int GetDealtDamage() { return dealtDamage; }
        [SerializeField]
        private bool reloadDealtDamage = false; //everytime the attack entity gets a new target, the above field will be reset

        [SerializeField]
        private AttackWeapon weapon = new AttackWeapon(); //handles the attack's weapon
        public AttackWeapon GetWeapon() { return weapon; }

        [SerializeField]
        private AttackLOS lineOfSight = new AttackLOS(); //handles the LOS settings for this attack type
        public bool IsInLineOfSight() {
            return FactionEntity.Type == EntityTypes.building || lineOfSight.IsInSight(GetTargetPosition(), weapon.GetWeaponObject(), transform); 
        }

        [SerializeField]
        private AttackObjectLauncher attackObjectLauncher = new AttackObjectLauncher(); //handles launching attack objects in case of an indirect attack.

        [SerializeField, Tooltip("Defines information used to display the attack engagement task in the task panel.")]
        private EntityComponentTaskUIData attackTaskUI = null;

        //Events: Besides the custom delegate events, you can directly use the event triggers below to further customize the behavior of the attack:
        [SerializeField]
        private UnityEvent attackerInRangeEvent = null;
        [SerializeField]
        private UnityEvent targetLockedEvent = null;
        [SerializeField]
        private UnityEvent attackPerformedEvent = null;
        public void InvokeAttackPerformedEvent() { attackPerformedEvent.Invoke(); }
        [SerializeField]
        private UnityEvent attackDamageDealtEvent = null;
        public void InvokeAttackDamageDealtEvent() { attackDamageDealtEvent.Invoke(); }

        //UI:
        [SerializeField, Tooltip("In case the MultipleAttackManager is attached and switching attacks through tasks is possible, this defines the attack switch task data.")]
        private EntityComponentTaskUIData switchTaskUI = null;
        /// <summary>
        /// Gets the attack switch task UI data for the AttackEntity component.
        /// </summary>
        public EntityComponentTaskUIData SwitchTaskUI { get { return switchTaskUI; } }

        //Audio:
        [SerializeField, Tooltip("What audio clip to play when the entity is ordered to attack?")]
        private AudioClipFetcher orderAudio = new AudioClipFetcher(); //played when the attack entity is ordered to attack
        public AudioClip GetOrderAudio () { return orderAudio.Fetch(); }
        [SerializeField, Tooltip("What audio clip to play when the attack is launched?")]
        private AudioClipFetcher attackAudio = new AudioClipFetcher(); //played each time the attack entity attacks.

        //other components:
        protected GameManager gameMgr;

        /// <summary>
        /// Initializer method required for each entity component that gets called by the Entity instance that the component is attached to.
        /// </summary>
        /// <param name="gameMgr">Active instance of the GameManager component.</param>
        /// <param name="entity">Entity instance that the component is attached to.</param>
        public virtual void Init(GameManager gameMgr, Entity entity)
        {
            this.gameMgr = gameMgr;
            FactionEntity = entity as FactionEntity;

            CoolDownActive = false;
            coolDownTimer = 0.0f;

            searchTimer = 0.0f;
            reloadTimer = 0.0f;

            damage.Init(gameMgr, this); //initialize the damage settings
            attackObjectLauncher.Init(gameMgr, this);
            weapon.Init();

            reloadDuration /= gameMgr.GetSpeedModifier(); //apply the speed modifier
        }

        public abstract bool CanEngage(); //can the faction entity engage in an attack?

        //a method that cancels an attack
        public virtual void Stop()
        {
            terrainAttackActive = false; //in case this was active before stopping the attack
            //reloadTimer = reloadDuration; //reset the reload timer
            ResetDelay(); //set the delay timer
            Target = null;
            weapon.SetIdleRotation(); //set the weapon back to idle rotation
        }

        //check if the attacker can engage target:
        public virtual ErrorMessage IsTargetValid (FactionEntity potentialTarget)
        {
            if (gameMgr.InPeaceTime() == true)
                return ErrorMessage.peaceTime;
            else if (potentialTarget == null) //if there's no target assigned
                return !requireTarget ? ErrorMessage.none : ErrorMessage.attackTargetRequired; //see if the attack entity can attack without target
            if (potentialTarget == FactionEntity)
                return ErrorMessage.invalid;
            else if (!potentialTarget.gameObject.activeInHierarchy)
                return ErrorMessage.inactive;
            else if (!potentialTarget.Interactable)
                return ErrorMessage.uninteractable;
            else if (potentialTarget.EntityHealthComp.IsDead())
                return ErrorMessage.targetDead;
            else if (potentialTarget.FactionID == FactionEntity.FactionID && engageFriendly == false)
                return ErrorMessage.targetSameFaction;
            else if (potentialTarget.EntityHealthComp.CanBeAttacked == false) //peace time, same faction ID or target can't be attacked? -> nope
                return ErrorMessage.targetNoAttack;

            return targetPicker.IsValidTarget(potentialTarget);
        }

        public virtual ErrorMessage IsTargetAttackPositionValid (FactionEntity potentialTarget)
        {
            return IsTargetValid(potentialTarget);
        }

        public abstract bool IsIdle(); //is the faction entity idle?

        /// <summary>
        /// Checks whether a potential target position is inside the attack range.
        /// </summary>
        /// <param name="attackPosition">Vector3 that represents the position from which the attack will be considered to launch.</param>
        /// <param name="targetPosition">Vector3 that represents the potential target position.</param>
        /// <returns>True if the potential target position is inside the attack range, otherwise false.</returns>
        public abstract bool IsTargetInRange(Vector3 attackPosition, Vector3 targetPosition, FactionEntity potentialTarget);

        //method that updates the cooldown
        void UpdateCoolDown()
        {
            if (coolDownTimer > 0) //cooldown timer
                coolDownTimer -= Time.deltaTime;
            else
            {
                CoolDownActive = false;

                CustomEvents.OnAttackCooldownUpdated(this, Target);
            }
        }

        protected virtual void Update ()
        {
            if (CoolDownActive == true) //cooldown:
                UpdateCoolDown();

            if (useReload == true && reloadTimer > 0) //reload timer update
                reloadTimer -= Time.deltaTime;

            //if the attack component is not active or is locked, the attack entity can't attack
            if (IsActive == false || isLocked || CanEngage() == false) 
                return;

            if(!HasTarget()) //if there's a target assigned and no terrain attack is active
                OnNoTargetUpdate();
            else
            {  
                if (Target?.EntityHealthComp.IsDead() == true) //if the target is already dead
                {
                    Stop(); //stop the attack
                    return;
                }
                weapon.UpdateActiveRotation(GetTargetPosition(), wasInTargetRange);
                OnTargetUpdate();
            }
        }

        //update the attack entity when there's no target
        protected virtual void OnNoTargetUpdate ()
        {
            weapon.UpdateIdleRotation();

            if (searchTimer > 0)
            {
                searchTimer -= Time.deltaTime;
                return;
            }

            if (!RTSHelper.IsLocalPlayer(FactionEntity) //this is the local player..
                || gameMgr.InPeaceTime() //game is still in peace time
                || !engageInRange //can't search for target
                || (engageInRangeIdleOnly && !IsIdle())) //or unit is just currently not idle
                return;

            SearchTarget();
            searchTimer = searchReload; //reload search timer
        }

        //search for a target to attack
        private void SearchTarget ()
        {
            float searchSize = searchRange;
            Vector3 searchCenter = transform.position;

            foreach(int i in Enumerable.Range(0,2))
            {
                if(gameMgr.GridSearch.Search(searchCenter,
                                             searchSize,
                                             false,
                                             IsTargetAttackPositionValid,
                                             out FactionEntity potentialTarget) == ErrorMessage.none)
                    SetTarget(potentialTarget, potentialTarget.transform.position);

                if (SearchRangeCenter == null)
                    break;

                searchSize = SearchRangeCenter.Size;
                searchCenter = SearchRangeCenter.transform.position;
            }
        }

        //update the attack entity when there's a target
        protected virtual void OnTargetUpdate()
        {
            //the override of this method in both the UnitAttack and BuildingAttack components are responsible for checking the conditions for which an attack can be continued or not
            //reaching this stage means that all conditions have been met and it's safe to continue with the attack
            if (wasInTargetRange == false) //if the attacker never was in the target's range but just entered it:
                OnEnterTargetRange();

            if (reloadTimer > 0 || IsInLineOfSight() == false) //still in reload time or not facing target? do not proceed
                return;

            if (delayTimer > 0) //delay timer
            {
                delayTimer -= Time.deltaTime;
                return;
            }

            //If the attack delay is over, the attack is triggered and LOS conditions are met
            if (triggered)
            {
                if (direct == true) //Is this a direct attack (no use of attack objects)?
                {
                    attackPerformedEvent.Invoke();
                    //not an area attack:
                    CustomEvents.OnAttackPerformed(this, Target, GetTargetPosition());

                    damage.TriggerAttack(Target, GetTargetPosition());
                    gameMgr.AudioMgr.PlaySFX(FactionEntity.AudioSourceComp, attackAudio.Fetch(), false);
                    OnAttackComplete();
                }
                else if (attackObjectLauncher.OnIndirectAttackUpdate()) //indirect attack ? -> must launch attack objects
                    gameMgr.AudioMgr.PlaySFX(FactionEntity.AudioSourceComp, attackAudio.Fetch(), false);
            }

        }

        protected void OnEnterTargetRange()
        {
            attackerInRangeEvent.Invoke();
            CustomEvents.OnAttackerInRange(this, Target, GetTargetPosition()); //launch custom event

            wasInTargetRange = true; //mark as in target's range.
            initialEngagementDistance = Vector3.Distance(transform.position, GetTargetPosition());
        }

        //method called when an attack is complete
        public virtual void OnAttackComplete()
        {
            terrainAttackActive = false; //disable if this was active
            reloadTimer = reloadDuration; //reset the reload timer

            StartCoolDown(); //start the cooldown

            //If this not the basic attack then revert back to the basic attack after done if that's the condition
            if (!basic && FactionEntity.MultipleAttackMgr != null && FactionEntity.MultipleAttackMgr.RevertToBasicAttack)
                FactionEntity.MultipleAttackMgr.SetTarget(FactionEntity.MultipleAttackMgr.BasicAttackCode);

            //Attack once? cancel attack to prevent source from attacking again
            if (engageOnce == true)
                Stop();

            ResetDelay();
        }

        //a method that resets the attack's delay options
        void ResetDelay()
        {
            delayTimer = delayDuration; //reset the attack delay timer
            triggered = !delayTriggerEnabled; //does the attack need to be triggered from an external component?
        }

        //a method that starts the attack cooldown:
        public void StartCoolDown()
        {
            if (coolDownEnabled == true) //only if the cooldown option is enabled
            {
                coolDownTimer = coolDownDuration;
                CoolDownActive = true;

                CustomEvents.OnAttackCooldownUpdated(this, Target);
            }
        }

        //set the attack target
        public abstract ErrorMessage SetTarget(FactionEntity newTarget, Vector3 newTargetPosition);

        //set the attack target locally
        public virtual void SetTargetLocal(FactionEntity newTarget, Vector3 newTargetPosition)
        {
            ResetDelay(); //reload the attack delay

            lastTargetPosition = newTargetPosition; //mark the last position of the target

            if (newTarget != null) //if there's an attack target
            {
                if (newTarget != Target) //if this is a different target than the last one assigned
                {
                    Target = newTarget; //set new target
                    damage.UpdateCurrentDamage(Target); //update the current damage value to apply to the target in an attack
                }
            }
            else if (!requireTarget) //no target assigned but this is allowed for this attacker
                terrainAttackActive = true; //incoming terrain attack

            if (direct == false) //if the attack type is in-direct (attack is done by launching attack objects)
                attackObjectLauncher.Activate();

            wasInTargetRange = false;

            //new target, check to reload damage dealt:
            if (reloadDealtDamage == true)
                dealtDamage = 0;

            //events:
            targetLockedEvent.Invoke();
            CustomEvents.OnAttackTargetLocked(this, Target, GetTargetPosition());
        }

        //reset the attack attributes
        public void Reset()
        {
            reloadTimer = reloadDuration; //reset the reload timer
            ResetDelay();
            Target = null;
            wasInTargetRange = false;
        }

        //increase/decrease the dealt damage
        public void AddDamageDealt(int value)
        {
            dealtDamage += value;
        }

        //get the current damage dealt.
        public float GetDamageDealt()
        {
            return dealtDamage;
        }

        #region Task UI
        /// <summary>
        /// Called by the TaskPanelUI component to fetch the attack task UI attributes.
        /// </summary>
        /// <param name="taskUIAttributes">The attack task UI attributes are added as the single element of the IEnumerable, if conditions are met.</param>
        /// <param name="disabledTaskCodes">In case the attack task is to be disabeld, it will be the single element of this IEnumerable.</param>
        /// <returns>True if the attack task can be displayed in the task panel, otherwise false.</returns>
        public bool OnTaskUIRequest(out IEnumerable<TaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = null;
            disabledTaskCodes = null;

            if (attackTaskUI == null) //task is not assigned
                return false;

            disabledTaskCodes = Enumerable.Repeat(attackTaskUI.Data.code, 1);

            if(!FactionEntity.CanRunComponents //if the associated faction entity can not run this component
                || !IsActive //if the component is not active
                || !RTSHelper.IsPlayerFaction(FactionEntity) //or this is not the local player's faction
                || !attackTaskUI.Data.enabled) //or the task is not enabled
                return false; //no task to display

            //set the movement task.
            taskUIAttributes = Enumerable.Repeat(
                new TaskUIAttributes()
                {
                    entityComp = attackTaskUI.Data,
                    icon = attackTaskUI.Data.icon
                },
                1);

            //no task to disable
            disabledTaskCodes = null;

            return true;
        }

        /// <summary>
        /// Called by the TaskUI instance that handles the attack engagement task, if it is drawn on the panel, to launch the attack task.
        /// </summary>
        /// <param name="taskCode">Code of the attack engagement task. In other more complex components, multiple tasks can be drawn from the same component, this allows to define which task has been clicked.</param>
        public void OnTaskUIClick (string taskCode)
        {
            gameMgr.TaskMgr.UnitComponent.SetAwaitingTaskType(TaskTypes.attack, attackTaskUI.Data.icon);
        }
        #endregion
    }
}
