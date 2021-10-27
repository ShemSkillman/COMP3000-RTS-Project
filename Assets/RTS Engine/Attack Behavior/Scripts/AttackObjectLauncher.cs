using UnityEngine;

using RTSEngine.EntityComponent;

/* AttackObject Launcher script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackObjectLauncher
    {
        AttackEntity source;

        public enum LaunchTypes { random, inOrder};
        //random: one attack source from the below array will be randomly chosen and triggered
        //in Order: the attack will trigger all elements of the below array in their order.
        [SerializeField]
        private LaunchTypes launchType = LaunchTypes.inOrder;

        [System.Serializable]
        public class Source
        {
            [SerializeField]
            private EffectObj attackObject = null; //the attack object that will be launched from this source.
            [SerializeField]
            private Transform launchPosition = null; //this is where the attack object will be launched from.
            [SerializeField, Tooltip("The initial rotation that the attack object will have as soon as it is spawned.")]
            private Vector3 launchRotationAngles = Vector3.zero;

            [SerializeField]
            private Vector3 accuracyModifier = Vector3.zero; //the higher this value is, the less accurate the attack object movement is (single player only currently).

            [SerializeField]
            private float delayTime = 0f; //delay time before an attack is triggered from this source
            [SerializeField]
            private bool createInDelay = false; //is the attack object of this source created when the delay time starts or must the delay time end first?
            [SerializeField]
            private bool damageInDelay = true; //if the above field is enabled, this decides whether the attack object will apply damage in delay or not.
            [SerializeField]
            private Transform delayParentObject = null; //if the attack object is created in delay, then it will be child of this transform (if assigned)

            //a method that returns the delay time for the source step timer:
            public float GetDelay ()
            { //if the attack object is to be created in delay time, the delay will be handled by the attack object itself
                return createInDelay == false ? delayTime : 0.0f;
            }
            
            //a method that launches the attack object towards the target
            public void Launch (EffectObjPool effectPool, AttackEntity source)
            {
                AttackObject newAttackObject = effectPool.SpawnEffectObj(attackObject, launchPosition.position, Quaternion.identity).GetComponent<AttackObject>();

                Vector3 targetPosition = source.GetTargetPosition();
                if (GameManager.MultiplayerGame == false) //if this is a singleplayer game, we can play with accuracy:
                    targetPosition += new Vector3(Random.Range(-accuracyModifier.x, accuracyModifier.x), Random.Range(-accuracyModifier.y, accuracyModifier.y), Random.Range(-accuracyModifier.z, accuracyModifier.z));

                newAttackObject.Enable(source, source.Target, targetPosition,
                (createInDelay == true) ? delayTime : 0.0f,
                damageInDelay,
                delayParentObject,
                source.CanEngageFriendly(),
                launchRotationAngles);
            }
        }
        [SerializeField]
        private Source[] sources = new Source[0]; //the attack objects are launched from sources inside this array 

        //for in order attack source types
        private int sourceStep; //at which attack source is the launcher currently at
        private float sourceStepTimer; //the attack source timer (which accounts for the delays for each source). 

        GameManager gameMgr;

        public void Init (GameManager gameMgr, AttackEntity source)
        {
            this.gameMgr = gameMgr;
            this.source = source;
        }

        //a method that activates this component (when a new target is set):
        public void Activate ()
        {
            if (launchType == LaunchTypes.random) //pick a random source to launch an attack object from
                sourceStep = Random.Range(0, sources.Length);
            else if (launchType == LaunchTypes.inOrder) //start with the first source
                sourceStep = 0;

            sourceStepTimer = sources[sourceStep].GetDelay(); //set the timer to the delay time of the source
        }

        //update for an indirect attack
        public bool OnIndirectAttackUpdate ()
        {
            if (sourceStepTimer > 0)
                sourceStepTimer -= Time.deltaTime;
            else
            {
                source.InvokeAttackPerformedEvent();
                //not an area attack:
                CustomEvents.OnAttackPerformed(source, source.Target, source.GetTargetPosition());

                sources[sourceStep].Launch(gameMgr.EffectPool, source);
                if (launchType == LaunchTypes.inOrder) //if the attack is supposed to go through attack objects in order and launch them
                    sourceStep++; //increment the source step

                if (sourceStep >= sources.Length || launchType == LaunchTypes.random) //if we reached the last attack object or the launch type is set to random
                {
                    sourceStep = 0; //end of attack
                    source.OnAttackComplete();
                }
                else //move to next attack object
                {
                    sourceStepTimer = sources[sourceStep].GetDelay();
                }

                return true; //return whenever an attack object is launched
            }

            return false;
        }
    }
}
