using UnityEngine;

using RTSEngine.EntityComponent;

namespace RTSEngine
{
    [RequireComponent(typeof(UnitMovement))]
    public class Wander : MonoBehaviour
    {
        private Unit unit; //the main unit's component

        [SerializeField]
        private bool isActive = false; //is this component active?
        public bool IsActive () { return isActive; }
        public void Toggle () { //toggle the wandering behavior
            isActive = !isActive;
            if(isActive) //if wandering is now enabled
            {
                if (fixedCenter == true) //if the wandering behavior uses a fixed center to wander around
                    center = transform.position; //make it the current player's position
                Trigger(); //start wandering
            }

            CustomEvents.OnUnitWanderToggled(unit); 
        }

        [SerializeField]
        private bool fixedCenter = true; //wander around spawn position (if wander by default is enabled) or wander around the position where the wandering is enbaled
        private Vector3 center; //center of wandering.
        [SerializeField]
        private FloatRange range = new FloatRange(10.0f, 15.0f); //range of wandering 
        [SerializeField]
        private FloatRange reloadRange = new FloatRange(2.0f, 4.0f); //time before the unit decides to change the wandering destination
        float timer;

        //UI:
        [SerializeField]
        private Sprite enableIcon = null;
        [SerializeField]
        private Sprite disableIcon = null;
        public Sprite GetIcon() { return isActive == false ? enableIcon : disableIcon; } //two different icons to show depending on the current state of the wander comp

        [SerializeField]
        private int taskPanelCategory = 0; //task panel category for the enable/disable wander tasks
        public int GetTaskPanelCategory() { return taskPanelCategory; }

        GameManager gameMgr;

        public void Init(GameManager gameMgr, Unit unit)
        {
            this.gameMgr = gameMgr;
            this.unit = unit;

            //set the wandering center
            if (fixedCenter == true)
                center = transform.position;

            timer = reloadRange.getRandomValue(); //reload the wander timer
        }

        //a method that triggers the wandering behavior
        public void Trigger()
        {
            if (isActive == false || !unit.IsIdle() || !unit.MovementComp.IsActive) //if this is not enabled, the unit is not idle or it can not move
                return; //do not proceed

            if (fixedCenter == false) //if the unit's wander position is not fixed as the spawn position
                center = transform.position; //set the wander center each time here.

            //move the unit to a random position in the wander range
            Vector3 targetPosition = gameMgr.MvtMgr.GetRandomMovablePosition(unit, center, range.getRandomValue());

            gameMgr.MvtMgr.Move(unit, targetPosition, 0.0f, null, InputMode.movement, false);

            timer = reloadRange.getRandomValue(); //reload the wander timer
        }

        private void Update()
        {
            if (isActive == false || unit.HealthComp.IsDead() == true || !RTSHelper.IsLocalPlayer(unit)) //if this is not enabled or this is a multiplayer game where this is not the local player's unit
                return; //do not proceed

            if (unit.IsIdle() == true && unit.HealthComp.IsDamageAnimationActive() == false) //only if the player is idle and can wander + is not currently playing the take damage animation
            {
                //as long as the wander timer is running, do nothing
                if (timer > 0)
                    timer -= Time.deltaTime;
                if (timer <= 0) //when the wander timer is over
                    Trigger(); //trigger it again
            }
        }

    }
}