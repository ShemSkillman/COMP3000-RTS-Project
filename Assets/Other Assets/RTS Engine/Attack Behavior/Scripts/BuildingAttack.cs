using System.Collections;
using UnityEngine;

/* BuildingAttack script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(Building))]
    public class BuildingAttack : AttackEntity
    {
        Building building; //the building's main component

        /// <summary>
        /// Initializer method required for each entity component that gets called by the Entity instance that the component is attached to.
        /// </summary>
        /// <param name="gameMgr">Active instance of the GameManager component.</param>
        /// <param name="entity">Entity instance that the component is attached to.</param>
        public override void Init(GameManager gameMgr, Entity entity)
        {
            base.Init(gameMgr, entity);
            building = entity as Building;
        }

        //can the building engage in an attack:
        public override bool CanEngage() //only if the building has health and is not in construction phase
        {
            return building.HealthComp.IsDead() == false && building.IsBuilt == true && coolDownTimer <= 0.0f;
        }

        //the building is always marked as in idle mode
        public override bool IsIdle() { return true; }

        /// <summary>
        /// Checks whether a potential target position is inside the attack range.
        /// </summary>
        /// <param name="targetPosition">Vector3 that represents the potential target position.</param>
        /// <returns>True if the potential target position is inside the attack range, otherwise false.</returns>
        public override bool IsTargetInRange(Vector3 attackPosition, Vector3 targetPosition, FactionEntity potentialTarget)
        {
            return Vector3.Distance(attackPosition, targetPosition) <= searchRange;
        }

        //update in case the building has an attack target:
        protected override void OnTargetUpdate()
        {
            if (IsTargetInRange(transform.position, GetTargetPosition(), Target) == false) //if the building's target is no longer in range
            {
                Stop(); //stop the attack.
                return; //and do not proceed
            }

            base.OnTargetUpdate();
        }

        //called when the building picks a target
        public override ErrorMessage SetTarget(FactionEntity newTarget, Vector3 newTargetPosition)
        {
            if (GameManager.MultiplayerGame == false) //single player game, go ahead
                SetTargetLocal(newTarget, newTargetPosition);
            else if(RTSHelper.IsLocalPlayer(building) == true) //only if this is a local player
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.building,
                    targetMode = (byte)InputMode.attack,
                    targetPosition = newTargetPosition,
                };
                InputManager.SendInput(newInput, building, newTarget); //send input
            }

            return ErrorMessage.none;
        }
    }
}
