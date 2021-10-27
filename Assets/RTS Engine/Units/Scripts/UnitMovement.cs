using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;

using RTSEngine.Movement;
using RTSEngine.Animation;
using RTSEngine.UI;

/* UnitMovement script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    /// <summary>
    /// Handles movement and rotation for the Unit instance it is attached to.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class UnitMovement : MonoBehaviour, IEntityComponent
    {
        #region Attributes
        private Unit unit; //the main unit's component
        /// <summary>
        /// Entity instance that this component is attached to.
        /// </summary>
        public Entity Entity { get { return unit; } }

        [SerializeField, Tooltip("Can the unit move by default?")]
        private bool isActive = true;
        /// <summary>
        /// Is the unit able to move?
        /// </summary>
        public bool IsActive
        {
            get { return isActive && !unit.HealthComp.IsDead(); }
            set 
            { 
                isActive = value;

                if (movementTaskUI != null && movementTaskUI.Data.enabled //the task is valid and enabled.
                    && RTSHelper.IsPlayerFaction(unit)) //if the unit belongs to the player's faction then update the movement task in case it has been already drawn
                    CustomEvents.OnEntityComponentTaskReloadRequest(this, movementTaskUI.Data.code);
            }
        }

        [SerializeField, Tooltip("When true, the unit to spawn at the height of the air terrain when created.")]
        private bool airUnit = false; //when true, the unit will be able to fly over the terrain/map.
        /// <summary>
        /// Is the unit able to fly?
        /// </summary>
        public bool AirUnit { get { return airUnit; } }

        [SerializeField, Tooltip("Movement formation for this unit type.")]
        private MovementFormation formation = new MovementFormation { type = MovementFormation.Type.circle, amount = 4, maxEmpty = 1 };
        /// <summary>
        /// Gets the MovementFormation struct that defines the movement formation for the unit.
        /// </summary>
        public MovementFormation Formation { get { return formation; } }

        [SerializeField, Tooltip("Enable to reset unit's movement when it receives a new command while movement is already active.")]
        private bool resetPendingMovement = false; //when set to false, this allows the movement animation not to be reset when the unit receives changes their path while moving.

        /// <summary>
        /// Get whether the unit is currently actively moving through a path or not.
        /// </summary>
        public bool IsMoving { private set; get; }

        /// <summary>
        /// An instance that extends the IMovementController interface which is responsible for computing the navigation path and moving the unit.
        /// </summary>
        public IMovementController Controller { private set; get; }

        /// <summary>
        /// Target destination that the unit is moving towards.
        /// </summary>
        public Vector3 Target { get; private set; } 

        /// <summary>
        /// The current corner that the unit is moving towards in its current path.
        /// </summary>
        public Vector3 NextCorner { get; private set; } 


        /// <summary>
        /// Has the unit reached its current's path destination?
        /// </summary>
        public bool DestinationReached { private set; get; } 

        [SerializeField, Tooltip("Default movement speed.")]
        private float speed = 10.0f; 

        [SerializeField, Tooltip("How fast will the unit reach its movement speed?")]
        private float acceleration = 10.0f;

        //unit addable target: components that allow the unit to be added after it moves towards them.
        private IAddableUnit addableTarget;
        //holds the last interaction position of the addable target instance, this is used to update the movement in case of the addable target moves.
        private Vector3 lastAddablePosition;

        [SerializeField, Tooltip("How fast does the unit rotate while moving?"), Header("Rotation")]
        private float mvtAngularSpeed = 250.0f; //How fast does the rotation update?

        [SerializeField, Tooltip("When disabled, the unit will have to rotate to face the next corner of the path before moving to it.")]
        private bool canMoveRotate = true; //can the unit rotate and move at the same time? 

        [SerializeField, Tooltip("If 'Can Move Rotate' is disabled, this value represents the angle that the unit must face in each corner of the path before moving towards it.")]
        private float minMoveAngle = 40.0f; //the close this value to 0.0f, the closer must the unit face its next destination in its path to move.
        private bool facingNextCorner = false; //is the unit facing the next corner on the path regarding the min move angle value?.

        [SerializeField, Tooltip("Can the unit rotate while not moving?")]
        private bool canIdleRotate = true; //can the unit rotate when not moving?
        [SerializeField, Tooltip("Is the idle rotation smooth or instant?")]
        private bool smoothIdleRotation = true;
        [SerializeField, Tooltip("How fast does the unit rotate while attempting to face its next corner in the path or while idle? Only if the idle rotation is smooth.")]
        private float idleAngularSpeed = 2.0f; //The angular speed of the unit when it is not moving.

        //rotation helper fields.
        private Quaternion nextRotationTarget;

        public Vector3 IdleLookAtPosition { set; get; } //Where should the unit look at as soon as it stops moving? only valid in case the IdleLookAtTransform is not assigned.
        public Transform IdleLookAtTransform { set; get; } //the object that this unit should be look at when not moving, if it exists.

        /// <summary>
        /// The UnitTargetPositionMarker instance assigned to the unit movement that marks the position that the unit is moving towards.
        /// </summary>
        public UnitTargetPositionMarker TargetPositionMarker { get; private set; }

        [SerializeField, Tooltip("Defines information used to display the unit movement task in the task panel."), Header("UI")]
        private EntityComponentTaskUIData movementTaskUI = null; //the task that will appear on the task panel when the attack entity is selected.

        //Audio:
        [SerializeField, Tooltip("What audio clip to play when the unit is ordered to move?"), Header("Audio")]
        private AudioClipFetcher mvtOrderAudio = new AudioClipFetcher(); //Audio played when the unit is ordered to move.
        [SerializeField, Tooltip("What audio clip to loop when the unit is moving?")]
        private AudioClipFetcher mvtAudio = new AudioClipFetcher(); //Audio clip played when the unit is moving.
        [SerializeField, Tooltip("What audio clip to play when is unable to move?")]
        private AudioClipFetcher invalidMvtPathAudio = new AudioClipFetcher(); //When the movement path is invalid, this audio is played.

        //other components
        private GameManager gameMgr;
        #endregion

        #region Events
        /// <summary>
        /// Event triggered when the unit starts moving.
        /// </summary>
        public event CustomEvents.UnitEventHandler UnitMovementStart = delegate { };

        /// <summary>
        /// Event triggered when a the unit stops moving.
        /// </summary>
        public event CustomEvents.UnitEventHandler UnitMovementStop = delegate { };
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the UnitMovement component. Must be called from the Unit instance that this UnitMovement instance is attached to.
        /// </summary>
        /// <param name="gameMgr">The current active GameManager instance, passed from the Unit instance.</param>
        /// <param name="entity">The Entity instance that represents the Unit instance which called this method.</param>
        public void Init(GameManager gameMgr, Entity entity)
        {
            this.gameMgr = gameMgr;
            Assert.IsNotNull(gameMgr,
                "[UnitMovement] Initializing with an invalid instance of the GameManager class.");

            this.unit = entity as Unit;
            Assert.IsNotNull(unit,
                "[UnitMovement] Initializing with an invalid instance of the Unit class.");

            //apply the speed modifier to both the speed and acceleration values
            speed *= this.gameMgr.GetSpeedModifier();
            acceleration *= this.gameMgr.GetSpeedModifier();

            //currently, the only movement controller is the NavMesh one. More will be added in the future.
            Controller = new NavMeshAgentController(unit, speed, acceleration, mvtAngularSpeed, gameMgr.MvtMgr.StoppingDistance);

            //set the radius and layer of the target position marker.
            TargetPositionMarker = new UnitTargetPositionMarker(Controller.Radius, airUnit ? 1 : 0, gameMgr.GridSearch); 

            IsMoving = false;
        }
        #endregion

        #region Task UI
        /// <summary>
        /// Called by the TaskPanelUI component to fetch the movement task UI attributes.
        /// </summary>
        /// <param name="taskUIAttributes">The movement task UI attributes are added as the single element of the IEnumerable, if conditions are met.</param>
        /// <param name="disabledTaskCodes">In case the movement task is to be disabeld, it will be the single element of this IEnumerable.</param>
        /// <returns>True if the movement task can be displayed in the task panel, otherwise false.</returns>
        public bool OnTaskUIRequest(out IEnumerable<TaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = null;
            disabledTaskCodes = null;

            if (movementTaskUI == null) //task is not assigned
                return false;

            disabledTaskCodes = Enumerable.Repeat(movementTaskUI.Data.code, 1);

            if(!unit.CanRunComponents //if the associated faction entity can not run this component
                || !IsActive //if the component is not active
                || !RTSHelper.IsPlayerFaction(unit) //or this is not the local player's faction
                || !movementTaskUI.Data.enabled) //or the task is not enabled
                return false; //no task to display

            //set the movement task.
            taskUIAttributes = Enumerable.Repeat(
                new TaskUIAttributes()
                {
                    entityComp = movementTaskUI.Data,
                    icon = movementTaskUI.Data.icon
                },
                1);

            //no task to disable
            disabledTaskCodes = null;

            return true;
        }

        /// <summary>
        /// Called by the TaskUI instance that handles the unit movement task, if it is drawn on the panel, to launch the movement task.
        /// </summary>
        /// <param name="taskCode">Code of the unit movement task. In other more complex components, multiple tasks can be drawn from the same component, this allows to define which task has been clicked.</param>
        public void OnTaskUIClick (string taskCode)
        {
            gameMgr.TaskMgr.UnitComponent.SetAwaitingTaskType(TaskTypes.movement, movementTaskUI.Data.icon);
        }
        #endregion

        #region Updating Unit State
        /// <summary>
        /// Handles updating the unit state whether it is in its idle or movement state.
        /// </summary>
        void FixedUpdate()
        {
            if (unit.HealthComp.IsDead()) //if the unit is already dead
                return; //do not update movement

            if (IsMoving == false)
            {
                UpdateIdleRotation();
                return;
            }

            //to sync the unit's movement with its animation state, only handle movement if the unit is in its mvt animator state.
            if (!unit.IsInAnimatorMvtState())
                return;

            UpdateMovementRotation(); 

            if (addableTarget != null //we have an addable target
                //and it moved further away from the fetched addable position when the path was calculated and movement started.
                && Vector3.Distance(lastAddablePosition, addableTarget.AddablePosition) > gameMgr.MvtMgr.StoppingDistance)
            {
                IAddableUnit lastAddableTarget = addableTarget;

                //re-move unit towards its target.
                Stop();
                lastAddableTarget.Move(unit, false);
                return;
            }

            if (DestinationReached == false) //check if the unit has reached its target position or not
                DestinationReached = Vector3.Distance(transform.position, Target) <= gameMgr.MvtMgr.StoppingDistance;

            if (DestinationReached == true)
            {
                IAddableUnit lastAddableTarget = addableTarget;

                Stop(); //stop the unit mvt

                if (lastAddableTarget != null) //unit is supposed to be added to this instance.
                {
                    IdleLookAtTransform = null; //so that the unit does not look at the IAddableUnit entity after it is added.
                    lastAddableTarget.Add(unit);
                }
            }
        }

        /// <summary>
        /// Handles updating the unit's rotation while in idle state.
        /// </summary>
        private void UpdateIdleRotation ()
        {
            if (!canIdleRotate || nextRotationTarget == Quaternion.identity) //can the unit rotate when idle + there's a valid rotation target
                return;

            if (IdleLookAtTransform != null) //if there's a target object to look at.
                nextRotationTarget = RTSHelper.GetLookRotation(transform, IdleLookAtTransform.position); //keep updating the rotation target as the target object might keep changing position

            if (smoothIdleRotation)
                transform.rotation = Quaternion.Slerp(transform.rotation, nextRotationTarget, Time.deltaTime * idleAngularSpeed);
            else
                transform.rotation = nextRotationTarget;
        }

        /// <summary>
        /// Deactivates the movement controller and sets the unit's rotation target to the next corner in the path.
        /// </summary>
        private void EnableMovementRotation ()
        {
            facingNextCorner = false; //to trigger checking for correct rotation properties
            Controller.IsActive = false; //stop handling rotation using the movement controller

            NextCorner = Controller.NextPathTarget; //assign new corner in path
            //set the rotation target to the next corner.
            nextRotationTarget = RTSHelper.GetLookRotation(transform, NextCorner);
        }

        /// <summary>
        /// Handles updating the unit's rotation while it is in its movement state.
        /// This mainly handles blocking the movement controller and rotating the unit if it is required to rotate toward its target before moving.
        /// </summary>
        private void UpdateMovementRotation()
        {
            if (canMoveRotate) //can move and rotate? do not proceed.
                return;

            if (NextCorner != Controller.NextPathTarget) //if the next corner/destination on path has been updated
                EnableMovementRotation();

            if (facingNextCorner) //facing next corner? we good
                return;

            if (Controller.IsActive) //stop movement it if it's not already stopped
                Controller.IsActive = false;

            //keep checking if the angle between the unit and its next destination
            Vector3 IdleLookAt = NextCorner - transform.position;
            IdleLookAt.y = 0.0f;

            //as long as the angle is still over the min allowed movement angle, then do not proceed to keep moving
            //allow the controller to retake control of the movement if we're correctly facing the next path corner.
            if(facingNextCorner = Vector3.Angle(transform.forward, IdleLookAt) <= minMoveAngle)
            { 
                Controller.IsActive = true;
                return;
            }

            //update the rotation as long as the unit is attempting to look at the next target in the path before it the Controller takes over movement (and rotation)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                nextRotationTarget,
                Time.deltaTime * idleAngularSpeed);
        }
        #endregion

        #region Updating Movement Target
        /// <summary>
        /// Sets a new movement target for the unit. This method relies on the "Move()" method from the MovementManager component to calculate the target position.
        /// </summary>
        /// <param name="targetPosition">Destination to move to.</param>
        /// <param name="targetEntity">The Entity instance to move towards.</param>
        /// <param name="targetMode">Type of the movement.</param>
        /// <param name="playerCommand">True if this was called by the player directly.</param>
        /// <returns>ErrorMessage.none if the movement is valid, otherwise failure error code.</returns>
        public ErrorMessage SetTarget(Vector3 targetPosition, Entity targetEntity, InputMode targetMode, bool playerCommand)
        {
            return gameMgr.MvtMgr.Move(unit, targetPosition, targetEntity ? targetEntity.GetRadius() : 0.0f, targetEntity, targetMode, playerCommand);
        }

        /// <summary>
        /// Calculates a movement path for the unit to move towards the target position and starts the movement if the calculated path is valid.
        /// This assumes that the target position is valid (in a movable area) and the movement conditions for the unit have been checked.
        /// </summary>
        /// <param name="targetPosition">Destination to move to.</param>
        /// <param name="targetEntity">The Entity instance to move towards.</param>
        /// <param name="idleLookAtPosition">The position that the unit will look at it when the destination is reached.</param>
        /// <param name="targetMode">Type of the movement.</param>
        /// <param name="playerCommand">True if this was called by the player directly.</param>
        /// <returns>ErrorMessage.none if the movement is valid, otherwise failure error code.</returns>
        public ErrorMessage SetTargetLocal(Vector3 targetPosition, Entity targetEntity, Vector3 idleLookAtPosition, InputMode targetMode, bool playerCommand)
        {
            //if the calculate path is invalid/incomplete
            if (!Controller.Prepare(targetPosition))
            {
                Stop(); //if the unit was moving, stop it.
                unit.CancelJob(Unit.jobType.all); //stop all unit's current jobs.

                if (playerCommand && RTSHelper.IsPlayerFaction(unit)) //if the local player owns this unit and the player called this
                    gameMgr.AudioMgr.PlaySFX(invalidMvtPathAudio.Fetch());

                return ErrorMessage.invalidMvtPath;
            }

            if(playerCommand && RTSHelper.IsPlayerFaction(unit))
                //valid movement path, start moving:
                gameMgr.AudioMgr.PlaySFX(mvtOrderAudio.Fetch(), false); //play the movement audio.

            //enable the target position marker and set the unit's current target destination to reserve it
            TargetPositionMarker.Toggle(true, targetPosition);

            addableTarget = null;
            if (targetMode == InputMode.addUnit) //if the movement target mode is to add a unit, then get the entity's IAddableUnit component.
            {
                addableTarget = targetEntity.AddableUnitComp;
                lastAddablePosition = targetEntity.AddableUnitComp.AddablePosition;
            }

            IsMoving = true; //player is now marked as moving

            CustomEvents.OnUnitStartMoving(unit); //trigger custom event
            UnitMovementStart(unit);

            DestinationReached = false; //destination is not reached by default

            if (unit.GetCurrAnimatorState() == UnitAnimatorState.moving) //if the unit was already moving, then lock changing the animator state briefly
                unit.LockAnimState = true;

            List<Unit.jobType> jobsToCancel = new List<Unit.jobType>(); //holds the jobs that will be later cancelled
            jobsToCancel.AddRange(new Unit.jobType[] { Unit.jobType.attack, Unit.jobType.healing, Unit.jobType.converting, Unit.jobType.building, Unit.jobType.collecting });

            if (targetEntity && targetEntity.gameObject.activeInHierarchy == true) //if the unit is moving towards an active object
            {
                Resource targetResource = targetEntity as Resource;
                Building targetBuilding = targetEntity as Building;
                Unit targetUnit = targetEntity as Unit;

                if (unit.AttackComp 
                    && targetMode == InputMode.attack 
                    && unit.AttackComp.Target == targetEntity as FactionEntity) //if the unit is set to attack the target object
                    jobsToCancel.Remove(Unit.jobType.attack);

                if (targetBuilding && targetBuilding.FactionID == unit.FactionID) //if the target object is a building that belongs to the unit's faction
                {
                    if (unit.BuilderComp != null && unit.BuilderComp.GetTarget() == targetBuilding) //is the unit going to construct this building?
                        jobsToCancel.Remove(Unit.jobType.building);
                    else if (unit.CollectorComp != null && unit.CollectorComp.IsDroppingOff() == true) //is the unit dropping off resources?
                        jobsToCancel.Remove(Unit.jobType.collecting);
                }
                else if (targetUnit) //if the target object is a unit
                {
                    if (unit.HealerComp && targetUnit.FactionID == unit.FactionID && addableTarget == null) //same faction and unit is not going towards a unit addable entity -> healing
                        jobsToCancel.Remove(Unit.jobType.healing);
                    else if (unit.ConverterComp && unit.ConverterComp.GetTarget() == targetUnit) //different faction but unit is going for conversion
                        jobsToCancel.Remove(Unit.jobType.converting);
                }
                else if (targetResource && targetResource == unit.CollectorComp.GetTarget()) //if the target object is a resource and the unit is going to collect it
                    jobsToCancel.Remove(Unit.jobType.collecting);

            }

            unit.CancelJob(jobsToCancel.ToArray()); //cancel the jobs that need to be stopped

            unit.LockAnimState = false; //unlock animation state and play the movement anim
            unit.SetAnimState(UnitAnimatorState.moving);

            Controller.Start();

            //set final path destination and first corner on path
            Target = Controller.FinalTarget; //set the target destination
            NextCorner = Controller.NextPathTarget; //set the current target destination corner

            if (!canMoveRotate) //can not move before facing the next corner in the path by a certain angle?
                EnableMovementRotation();

            this.IdleLookAtPosition = idleLookAtPosition;
            IdleLookAtTransform = targetEntity ? targetEntity.transform : null;

            gameMgr.AudioMgr.PlaySFX(unit.AudioSourceComp, mvtAudio.Fetch(), true);

            return ErrorMessage.none;
        }

        /// <summary>
        /// Stops the current unit's movement.
        /// </summary>
        /// <param name="prepareNextMovement">When true, not all movement settings will be reset since a new movement command will be followed.</param>
        public void Stop(bool prepareNextMovement = false)
        {
            gameMgr.AudioMgr.StopSFX(unit.AudioSourceComp); //stop the movement audio from playing

            if (IsMoving == false) //if the unit is not moving already then stop here
                return;

            IsMoving = false; //marked as not moving

            CustomEvents.OnUnitStopMoving(unit); //trigger custom event
            UnitMovementStop(unit);

            //set the movement speed to the default one in case it was changed by the Attack on Escape component.
            Controller.Speed = speed;

            //unit doesn't have a target APC or Portal to move to anymore
            addableTarget = null;

            if (!resetPendingMovement && prepareNextMovement) //if we're preparing for another movement command that will follow this call here, then no need to reset some of the params
                return;

            Controller.IsActive = false; 

            //update the next rotation target using the registered IdleLookAt position for the idle rotation.
            //only do this once the unit stops moving in case there's no IdleLookAt object.
            nextRotationTarget = RTSHelper.GetLookRotation(transform, IdleLookAtPosition); 

            TargetPositionMarker.Toggle(true, transform.position);

            if (!unit.HealthComp.IsDead()) //if the unit is not dead
                unit.SetAnimState(UnitAnimatorState.idle); //get into idle state
        }
        #endregion
    }
}
