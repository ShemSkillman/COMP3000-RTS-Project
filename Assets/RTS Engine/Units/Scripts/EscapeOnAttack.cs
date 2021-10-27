using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [RequireComponent(typeof(Unit))]
    public class EscapeOnAttack : MonoBehaviour
    {
        private Unit unit; //the main unit's component

        [SerializeField]
        private bool isActive = true; //is this component active?
        public bool IsActive() { return isActive; }

        [SerializeField]
        private bool NPCOnly = false; //when enabled, then this component will only be active for NPC units

        [SerializeField]
        private FloatRange range = new FloatRange(20.0f, 40.0f); //the range where the unit will escape.

        [SerializeField]
        private float speed = 10.0f; //the unit's movement speed can be modified when escaping
        public float GetSpeed () { return speed; }

        [SerializeField]
        private AnimatorOverrideController escapeAnimatorOverride = null; //escape animator override that is activated when the unit is escaping
        //the above animator override controller allows the unit to have a different movement animation when running

        private GameManager gameMgr;

        public void Init (GameManager gameMgr, Unit unit)
        {
            this.gameMgr = gameMgr;
            this.unit = unit;

            if (NPCOnly == true && gameMgr.GetFaction(unit.FactionID).IsNPCFaction() == false)
                isActive = false;

            speed *= gameMgr.GetSpeedModifier(); //apply the speed modifier
        }

        //a method to trigger the escape on attack behavior
        public void Trigger()
        {
            if (isActive == false) //do not proceed if the component is not active
                return;

            Vector3 targetPosition = gameMgr.MvtMgr.GetRandomMovablePosition(unit, transform.position, range.getRandomValue()); //find a random position to escape to

            if (GameManager.MultiplayerGame == false) //single player game
                TriggerLocal(targetPosition);
            else if (RTSHelper.IsLocalPlayer(unit) == true) //multiplayer game and this is the local player
            {
                NetworkInput newInput = new NetworkInput() //create new input for the escape task
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.unitEscape,
                    initialPosition = transform.position,
                    targetPosition = targetPosition
                };
                InputManager.SendInput(newInput, unit, null); //send input to input manager

            }
        }

        //a method to locally trigger the escape on attack behavior
        public void TriggerLocal(Vector3 targetPosition)
        {
            if (isActive == false) //do not proceed if the component is not active
                return;

            gameMgr.MvtMgr.MoveLocal(unit, targetPosition, 0.0f, null, InputMode.unitEscape, false); //move the unit locally

            if (escapeAnimatorOverride != null) //update the runtime animator controller to this one if it has been assigned
                unit.SetAnimatorOverrideController(escapeAnimatorOverride);
        }
    }
}
