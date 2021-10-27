using UnityEngine;

using RTSEngine.Animation;

/* Unit Component script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

[System.Serializable]
public class AutoUnitBehavior
{
    [SerializeField]
    private bool enabled = false; //if set to true, when the unit is idle, it will search for a target and automatically move to active state
    public bool IsEnabled() { return enabled; }
    public void Toggle(bool enable) { enabled = enable; }

    [SerializeField]
    private float searchReload = 5.0f; //time needed before the unit starts another search
    float searchTimer;

    [SerializeField]
    private float searchRange = 20.0f; //the range where the unit where search for a target
    public float GetSearchRange() { return searchRange; }
    public void ReloadSearchTimer() { searchTimer = searchReload; }

    //a method that updates the search timer
    public void UpdateTimer()
    {
        if (searchTimer >= 0)
            searchTimer -= Time.deltaTime;
    }

    //can the unit search for a target?
    public bool CanSearch()
    {
        if (searchTimer <= 0.0f) //if the search timer is through
        {
            ReloadSearchTimer(); //reload the timer
            return true; //allow unit to search for target
        }
        return false;
    }
}

namespace RTSEngine
{

    public abstract class UnitComponent<E> : MonoBehaviour where E : Entity
    {
        protected bool searchResources = false;

        protected Unit unit; //the unit's main component

        protected bool inProgress = false; //is the unit currently performing what this unit component is supposed to do?
        /// <summary>
        /// Is the faction entity currently actively working the entity component?
        /// </summary>
        public bool InProgress { get { return inProgress; } }

        protected E target; //the target object that this unit component deals with.
        /// <summary>
        /// Does this entity component has an active valid target?
        /// </summary>
        public bool HasTarget { get { return target != null; } }
        public E GetTarget() { return target; }

        protected float timer;

        [SerializeField]
        protected GameObject inProgressObject; //a gameobject (child of the main unit object) that is activated when the unit's job is in progress

        [SerializeField]
        public EffectObj sourceEffect = null; //triggered on the source unit when this component is in progress.
        private EffectObj currSourceEffect;
        [SerializeField]
        public EffectObj targetEffect = null; //triggered on the unit's target when this component is in progress.
        private EffectObj currTargetEffect;

        [SerializeField, Tooltip("What audio clip to play when the unit is ordered to perform this task?")]
        protected AudioClipFetcher orderAudio = new AudioClipFetcher(); //audio clip played when the unit is ordered to perform the task associated with this component
        public AudioClip GetOrderAudio() { return orderAudio.Fetch(); }

        [SerializeField]
        protected AutoUnitBehavior autoBehavior = new AutoUnitBehavior(); //can the unit search for a target automatically?

        [SerializeField]
        public EntityComponentTaskUI taskUI = new EntityComponentTaskUI { enabled = false }; //the task that will appear on the task panel when a unit with this component is selected

        //other components:
        protected GameManager gameMgr;

        public virtual void Init(GameManager gameMgr, Unit unit)
        {
            this.gameMgr = gameMgr;
            this.unit = unit;

            autoBehavior.ReloadSearchTimer(); //reload the search timer of the auto behavior

            if (!RTSHelper.IsLocalPlayer(unit)) //if this unit doesn't belong to the player's local faction
                autoBehavior.Toggle(false); //disable the automatic behavior.
        }

        public virtual void Update()
        {
            if (unit.HealthComp.IsDead() == true) //if the unit is dead, do not proceed.
                return;

            if (target != null) //unit has target -> active
                OnActiveUpdate(0.0f, UnitAnimatorState.idle, null); //on active update
            else //no target? -> inactive
                OnInactiveUpdate();
        }

        //update in case the unit has a target
        protected virtual bool OnActiveUpdate(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            if (breakCondition) //if break condition is met then the unit can no longer be active
            {
                unit.MovementComp.Stop(); //stop the movement of the unit in case it was moving towards its target
                Stop(); //cancel the current job
                return false;
            }

            if (inProgress == false && inProgressEnableCondition) //if the unit has reached its target and it hasn't started its job yet + the provided additional condition
                OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            if (inProgress == true && inProgressCondition) //if the unit's job is currently in progress
            {
                if (timer > 0) //construction timer
                    timer -= Time.deltaTime;
                else
                {
                    OnInProgress();
                    timer = reloadTime; //reload timer
                }
            }

            return true;
        }

        //called when the unit's job is enabled
        protected virtual void OnInProgressEnabled(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio)
        {
            if (unit.MovementComp.IsMoving)
                unit.MovementComp.Stop();

            unit.SetAnimState(activeAnimState);

            gameMgr.AudioMgr.PlaySFX(unit.AudioSourceComp, inProgressAudio, true); //play a random one

            if (inProgressObject != null) //show the in progress object
                inProgressObject.SetActive(true);

            timer = reloadTime; //start timer
            inProgress = true; //the unit's job is now in progress

            ToggleSourceTargetEffect(true); //enable the source and target effect objects
        }

        //method called when the unit's progresses in its active job
        protected virtual void OnInProgress()
        {

        }

        //update when the unit doesn't have a target
        protected virtual void OnInactiveUpdate()
        {
            if (inProgress == true) //if the unit doesn't have a target but its job is marked as in progress
            {
                unit.MovementComp.Stop(); //stop the movement of the unit in case it was moving towards its target
                Stop(); //cancel job
            }

            if ((GameManager.MultiplayerGame == false || RTSHelper.IsLocalPlayer(unit)) && autoBehavior.IsEnabled() == true && unit.IsIdle() == true) //if the auto behavior is, the unit is idle and this is the local player's faction or a single player game
            {
                if (autoBehavior.CanSearch() == true) //can the unit search for a target
                {
                    OnTargetSearch();
                }
                else
                    autoBehavior.UpdateTimer(); //update the timer.
            }
        }

        public abstract ErrorMessage IsTargetValid(E potentialTarget);

        //called when the unit's auto behavior launches a search for a target
        protected virtual void OnTargetSearch()
        {
            if (gameMgr.GridSearch.Search<E>(transform.position, autoBehavior.GetSearchRange(), searchResources, IsTargetValid, out E potentialTarget) == ErrorMessage.none)
                SetTarget(potentialTarget);
        }

        //called when the unit attempts to set a new target
        public virtual ErrorMessage SetTarget(E newTarget, InputMode targetMode = InputMode.none)
        {
            if (!RTSHelper.IsLocalPlayer(unit)) return ErrorMessage.notLocalPlayer; //only allow local player to launch this command.

            if (!GameManager.MultiplayerGame) //if this is a singleplayer game then go ahead directly
                SetTargetLocal(newTarget);
            else //multiplayer game and this is the unit's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)targetMode,
                    initialPosition = transform.position,
                    targetPosition = newTarget.transform.position
                };

                InputManager.SendInput(newInput, unit, newTarget);

                return ErrorMessage.requestRelayed;
            }

            return ErrorMessage.none;
        }

        public abstract void SetTargetLocal(E newTarget);

        public virtual bool Stop()
        {
            if (!inProgress && !HasTarget) //the component is not active
                return false; //do not proceed

            if (inProgressObject != null) //hide the in progress object
                inProgressObject.SetActive(false);

            //reset construction settings
            target = null;
            inProgress = false;

            unit.SetAnimState(UnitAnimatorState.idle); //back to idle state

            gameMgr.AudioMgr.StopSFX(unit.AudioSourceComp); //stop construction audio

            ToggleSourceTargetEffect(false);

            unit.MovementComp.IdleLookAtTransform = null;

            return true;
        }

        protected void ToggleSourceTargetEffect (bool enable)
        {
            if(enable)
            {
                if (sourceEffect != null)
                    currSourceEffect = gameMgr.EffectPool.SpawnEffectObj(
                        sourceEffect,
                        transform.position,
                        sourceEffect.transform.rotation,
                        transform,
                        false); //spawn the source effect on the source unit and don't enable the life timer

                if (targetEffect != null)
                    currTargetEffect = gameMgr.EffectPool.SpawnEffectObj(
                        targetEffect,
                        target.transform.position,
                        targetEffect.transform.rotation,
                        target.transform,
                        false); //spawn the target effect on the target and don't enable the life timer
            }
            else
            {
                if (currSourceEffect != null) //if the source unit effect was assigned and it's still valid
                {
                    currSourceEffect.Disable(); //stop it
                    currSourceEffect = null;
                }

                if (currTargetEffect != null) //if a target effect was assigned and it's still valid
                {
                    currTargetEffect.Disable(); //stop it
                    currTargetEffect = null;
                }
            }
        }
    }
}
