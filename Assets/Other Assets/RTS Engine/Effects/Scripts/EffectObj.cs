using UnityEngine;
using UnityEngine.Events;

using RTSEngine.Attack;

/* Effect Object script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class EffectObj : MonoBehaviour {

        [SerializeField]
        private string code = ""; //each effect object prefab must have a unique code
        public string GetCode() { return code; }

        [SerializeField]
        private bool enableLifeTime = true; //Control the lifetime of this effect object using the time right below?

        [SerializeField]
        private float defaultLifeTime = 3.0f; //The default duration during which the effect object will be shown before it's deactivated.
        private float timer;

        [SerializeField]
        private float disableTime = 0.0f; //when > 0, the disable events will be invoked and then timer with this length will start and then the effect object will be hidden

        public enum State { inactive, running, disabling };
        public State CurrentState { set; get; }

        //method that enables/disables the life timer and sets its duration
        public void ReloadTimer(bool enable, bool useDefault = true, float duration = 0.0f) {
            timer = useDefault ? defaultLifeTime : duration;
            enableLifeTime = enable;
        }

        [SerializeField]
        private Vector3 spawnPositionOffset = Vector3.zero;

        [SerializeField]
        private UnityEvent onEnableEvent = null; //invoked when the effect object is enabled.
        [SerializeField]
        private UnityEvent onDisableEvent = null; //invoked when the effect object is disabled.

        //effect object components:
        public AudioSource AudioSourceComp { private set; get; }
        public AttackObject AttackObj { private set; get; }

        //other components:
        GameManager gameMgr;

        #region Effect Object Events
        /// <summary>
        /// Delegate used for EffectObj related events.
        /// </summary>
        /// <param name="effectObj"></param>
        public delegate void EffectObjEventHandler(EffectObj effectObj);

        /// <summary>
        /// Event triggered when the EffectObj instance is first created and initialized.
        /// </summary>
        public static event EffectObjEventHandler EffectObjCreated = delegate {};

        /// <summary>
        /// Event triggered when the EffectObj instance is destroyed.
        /// </summary>
        public static event EffectObjEventHandler EffectObjDestroyed = delegate {};

        /// <summary>
        /// Event triggered when the EffectObj instance is enabled to be used.
        /// </summary>
        public static event EffectObjEventHandler EffectObjEnabled = delegate {};

        /// <summary>
        /// Event triggered when the EffectObj instance is hidden and disabled for future use.
        /// </summary>
        public static event EffectObjEventHandler EffectObjDisabled = delegate {};
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the EffectObj instance when it is first created.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the currently active game.</param>
        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            //init the attack object component if there's one
            if (AttackObj = GetComponent<AttackObject>())
                AttackObj.Init(gameMgr);

            AudioSourceComp = GetComponent<AudioSource>();

            EffectObjCreated(this); //trigger custom event
        }

        /// <summary>
        /// Called when the EffectObj instance is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            EffectObjDestroyed(this); //trigger custom event.
        }
        #endregion

        //enable the effect object
        public void Enable()
        {
            gameObject.SetActive(true);
            CurrentState = State.running;

            transform.position += spawnPositionOffset; //set spawn position offset.

            onEnableEvent.Invoke(); //invoke the event

            EffectObjEnabled(this); //trigger custom event
        }

        void Update ()
		{
            if (CurrentState != State.inactive) //if the effect object is not inactive then run timer
            {
                if (timer > 0.0f)
                    timer -= Time.deltaTime;
                else //life timer is through
                {
                    switch (CurrentState)
                    {
                        case State.running: //if the effect object is running (active)
                            if (enableLifeTime == true) //make sure the life time is enabled
                                Disable(); //disable the effect object
                            break;
                        case State.disabling: //if the effect object is getting disabled
                            DisableInternal(); //disable the effect object completely
                            break;

                    }
                }
            }
        }

        //hide the effect object
        public void Disable()
        {
            if (CurrentState == State.disabling) //if the effect object is already being disabled
                return;

            onDisableEvent.Invoke(); //invoke the event.

            timer = disableTime; //start the disable timer
            CurrentState = State.disabling; //we're now disabling the effect object
        }

        private void DisableInternal ()
        {
            transform.SetParent(null, true); //Make sure it has no parent object anymore.
            CurrentState = State.inactive;
            gameObject.SetActive(false);
            gameMgr.EffectPool.AddFreeEffectObj(this);

            EffectObjDisabled(this); //trigger custom event
        }
    }
}