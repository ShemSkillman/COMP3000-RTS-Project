using UnityEngine;

using RTSEngine.Animation;

/* UnitAttack script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(Unit))]
    public class UnitAttack : AttackEntity
    {
        Unit unit; //the building's main component

        [SerializeField, Tooltip("Defines the unit's stopping distance when engaging in an attack.")]
        private UnitAttackRange range = new UnitAttackRange(); //defines how the unit's stopping distance when engaging in an attack.
        /// <summary>
        /// Gets the UnitAttackRange instance that defines how the unit's stopping distance when engaging in an attack.
        /// </summary>
        /// <returns>UnitAttackRange instance of the attack unit.</returns>
        public UnitAttackRange GetRange () { return range; }

        [SerializeField]
        private bool moveOnAttack = false; //is the unit allowed to trigger its attack while moving?
        [SerializeField]
        private float followDistance = 15.0f; //if the attack target's leaves the attack entity range (defined in the attack manager), then this is max distance between this and the target where the attack entity can follow its target before stopping the attack

        //animation related attributes:
        [SerializeField]
        private AnimatorOverrideController attackAnimOverrideController = null; //so that each attack component can have a different attack animation
        private bool canTriggerAnimation = true; //play the unit's attack animation?
        [SerializeField]
        private bool triggerAnimationInDelay = false; //true => the attack animation is triggered when the delay starts. if false, it will only be triggered when the attack is triggered

        /// <summary>
        /// Initializer method required for each entity component that gets called by the Entity instance that the component is attached to.
        /// </summary>
        /// <param name="gameMgr">Active instance of the GameManager component.</param>
        /// <param name="entity">Entity instance that the component is attached to.</param>
        public override void Init(GameManager gameMgr, Entity entity)
        {
            base.Init(gameMgr, entity);
            unit = entity as Unit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="potentialTarget"></param>
        /// <param name="forceInRange"></param>
        /// <returns></returns>
        public ErrorMessage TryUpdateValidAttackPosition (FactionEntity potentialTarget, bool forceInRange, out Vector3 nextAttackPosition)
        {
            nextAttackPosition = default;

            if (potentialTarget == null) //the potentialTarget parameter must be valid
                return ErrorMessage.invalid;

            nextAttackPosition = gameMgr.AttackMgr.GetAttackPosition(unit, potentialTarget, potentialTarget.transform.position);

            if (forceInRange && !IsTargetInRange(nextAttackPosition, potentialTarget.transform.position, potentialTarget))
                return ErrorMessage.attackPositionNotFound;

            return ErrorMessage.none;
        }

        //can the unit engage in an attack:
        public override bool CanEngage() //make sure the unit is not dead
        {
            return unit.HealthComp.IsDead() == false && coolDownTimer <= 0.0f;
        }

        //check whether the unit is in idle mode or not:
        public override bool IsIdle()
        {
            return unit.IsIdle();
        }

        /// <summary>
        /// Checks whether a potential target position is inside the attack range.
        /// </summary>
        /// <param name="targetPosition">Vector3 that represents the potential target position.</param>
        /// <returns>True if the potential target position is inside the attack range, otherwise false.</returns>
        public override bool IsTargetInRange(Vector3 attackPosition, Vector3 targetPosition, FactionEntity potentialTarget)
        {
            if (!unit.MovementComp.IsActive) //if the source unit can't move
                return Vector3.Distance(attackPosition, targetPosition) <= searchRange; //we use the search range as the allowed attack range
            else
            {
                return Vector3.Distance(attackPosition, targetPosition) <=
                    range.GetStoppingDistance(potentialTarget, false)  //the actual attack range
                    + (moveOnAttack ? range.GetMoveOnAttackOffset() : 0.0f); //an offset added in case the unit can move and attack
            }
        }

        //update in case the unit has an attack target:
        protected override void OnTargetUpdate()
        {
            if (unit.MovementComp.IsActive) //only if the unit is able to move
            {
                if (Target?.Type == EntityTypes.unit && (Target as Unit).MovementComp.IsMoving) //if there's a target unit, only track it if it is moving
                {
                    //if this is not a AI unit defending a building and there's a target unit (not building) and it was already once inside the attack range of the target but the target moved away (distance is higher than the allowed follow distance) and the 
                    if (SearchRangeCenter == null 
                        && wasInTargetRange == true 
                        && Vector3.Distance(transform.position, GetTargetPosition()) > Mathf.Max(followDistance, initialEngagementDistance))
                    {
                        Stop(); //stop the attack.
                        return; //and do not proceed
                    }

                    //if the attack target unit changed its position before this unit reached it
                    if(range.CanUpdateMvt(lastTargetPosition, GetTargetPosition(), unit.MovementComp.Target))
                    {
                        //launch the attack again so that the unit moves closer to its target
                        if (TryUpdateValidAttackPosition(Target, true, out Vector3 nextAttackPosition) == ErrorMessage.none)
                            SetTargetLocal(Target, GetTargetPosition(), nextAttackPosition, false);
                        else
                        {
                            Stop();
                            return;
                        }
                    }
                }
                //unit is not moving yet it is not inside the target range
                else if(!unit.MovementComp.IsMoving 
                    && !IsTargetInRange(transform.position, GetTargetPosition(), Target))
                {
                    Stop();
                    return;
                }

            }
            //if the source unit can not move
            //check if it was already in target range and if the target leaves the attacking range, then stop the attack
            else if(SearchRangeCenter == null 
                && wasInTargetRange 
                && !IsTargetInRange(transform.position, GetTargetPosition(), Target)) 
            {
                Stop();
                return;
            }

            //if the unit is not in los or it can't attack while moving
            if (IsInLineOfSight() == false
                || (moveOnAttack == false && unit.MovementComp.IsMoving == true)
                || !IsTargetInRange(transform.position, GetTargetPosition(), Target))
                return;

            //if the reload timer is done and we can play the attack animation and if the delay conditions are met
            if (reloadTimer <= 0.0f && canTriggerAnimation && (triggerAnimationInDelay || (delayTimer <= 0.0 && triggered)))
                TriggerAnimation();

            base.OnTargetUpdate();
        }

        //a method that triggers the unit's attack animation
        public void TriggerAnimation()
        {
            if(attackAnimOverrideController)
                unit.SetAnimatorOverrideController(attackAnimOverrideController); //set the anim override controller if there's one

            unit.SetAnimState(UnitAnimatorState.attacking);

            canTriggerAnimation = false; //can only play attack animation again after the attack is done
        }

        //a method called when the unit attack is over:
        public override void Stop()
        {
            base.Stop();

            unit.SetAnimState(UnitAnimatorState.idle);
            unit.ResetAnimatorOverrideControllerOnIdle(); //reset the animator override controller in case it has been changed
        }

        //a method called when an attack is complete:
        public override void OnAttackComplete()
        {
            base.OnAttackComplete();
            canTriggerAnimation = true; //attack animation can be triggered for the next attack
        }

        /// <summary>
        /// Assumes that the next attack position has been set.
        /// </summary>
        /// <param name="newTarget"></param>
        /// <param name="newTargetPosition"></param>
        /// <returns></returns>
        public override ErrorMessage SetTarget(FactionEntity newTarget, Vector3 newTargetPosition)
        {
            return gameMgr.AttackMgr.LaunchAttack(unit, newTarget, newTargetPosition, false);
        }

        /// <summary>
        /// Assumes that the attack position is set.
        /// </summary>
        /// <param name="newTarget"></param>
        /// <param name="newTargetPosition"></param>
        public override void SetTargetLocal(FactionEntity newTarget, Vector3 newTargetPosition)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newTarget"></param>
        /// <param name="newTargetPosition"></param>
        /// <param name="newAttackPosition"></param>
        public ErrorMessage SetTargetLocal (FactionEntity newTarget, Vector3 newTargetPosition, Vector3 newAttackPosition, bool moveOnOutOfRange)
        {
            ErrorMessage errorMessage = ErrorMessage.none;

            if (IsTargetInRange(transform.position, newTargetPosition, newTarget)) //if unit can already attack from its position, inform AttackManager about it (which might have called this method).
                errorMessage = ErrorMessage.alreadyInAttackPosition;
            else if (!IsTargetInRange(newAttackPosition, newTargetPosition, newTarget)) //check if the attack position is outside the unit's attacking range.
            {
                //if we're allowed to move even if the attack position is out of range then do it.
                if (unit.MovementComp.IsActive && moveOnOutOfRange)
                {
                    //move towards attack position without attacking the target.
                    unit.MovementComp.SetTargetLocal(newAttackPosition, null, newTargetPosition, InputMode.movement, false);

                    return ErrorMessage.moveToTargetNoAttack; //if an attack unit is supposed to move even if it is out of range then no error is produced.
                }

                return ErrorMessage.attackPositionOutOfRange;
            }

            base.SetTargetLocal(newTarget, newTargetPosition);

            bool updateRotation = true; //update rotation directly instead of allowing the UnitMovement component to update it (in case unit should not be moved)

            if (unit.MovementComp.IsActive && errorMessage != ErrorMessage.alreadyInAttackPosition) //only if the current unit's position is not valid for the attack
            {
                updateRotation = false;
                //move towards attack position.
                unit.MovementComp.SetTargetLocal(newAttackPosition, newTarget, newTargetPosition, InputMode.attack, false);
            }
            else //current unit position is valid for attack, do not move but set rotation but mark attack range as entered.
            {
                unit.MovementComp.Stop(); //stop unit from moving in case they were already moving.

                OnEnterTargetRange();
            }

            if(updateRotation)
            {
                //just rotate the attack unit towards its target.
                unit.MovementComp.IdleLookAtPosition = newTargetPosition;
                unit.MovementComp.IdleLookAtTransform = newTarget ? newTarget.transform : null;
            }

            return errorMessage;
        }
    }
}