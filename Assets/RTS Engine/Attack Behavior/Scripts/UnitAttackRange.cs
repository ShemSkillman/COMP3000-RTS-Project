using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* UnitAttackRange script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    /// <summary>
    /// Defines the stopping distances and the range in which a certain attack unit can launch its attacks in.
    /// </summary>
    public class UnitAttackRange
    {
        [SerializeField, Tooltip("Minimum and maximum stopping distance when targeting a unit.")]
        public FloatRange unitStoppingDistance = new FloatRange(2.0f, 6.0f); //stopping distance for target units
        [SerializeField, Tooltip("Minimum and maximum stopping distance when targeting a building.")]
        private FloatRange buildingStoppingDistance = new FloatRange(5.0f, 10.0f); //stopping distance when the unit has a target building to attack
        [SerializeField, Tooltip("Minimum and maximum stopping distance when not targeting a specific target.")]
        private FloatRange noTargetStoppingDistance = new FloatRange(5.0f, 10.0f);  //stopping distance when the unit is launching an attack without a target assigned.

        [SerializeField, Tooltip("If the unit is allowed to move and attack, the attack range is increased by this offset."), Min(0)]
        private float moveOnAttackOffset = 3.0f; //when the attack unit can move and attack, the range of attack increases by this value
        /// <summary>
        /// Gets the stopping distance offset for units that can move on attack.
        /// </summary>
        /// <returns>The attack stopping distance offset for units that can move on attack.</returns>
        public float GetMoveOnAttackOffset () { return moveOnAttackOffset; }

        [SerializeField, Tooltip("How far does the attack target need to move in order to recalculate the attacker's unit movement."), Min(0)]
        private float updateMvtDistance = 2.0f; //if the unit is moving towards a target and it changes its position by more than this distance, the attacker's movement will be recalculated
        /// <summary>
        /// Check if the current attack target has moved too far from its position when the attack is initiated.
        /// </summary>
        /// <param name="lastTargetPosition">The attack target's position when the attack was last initiated.</param>
        /// <param name="currTargetPosition">The current attack target's position.</param>
        /// <returns>True if the distance between the last and current attack target's position is greater or equal to the allowed update movement distance, otherwise false.</returns>
        public bool CanUpdateMvt (Vector3 lastTargetPosition, Vector3 currTargetPosition, Vector3 destination)
        {
            return Vector3.Distance(lastTargetPosition, currTargetPosition) > updateMvtDistance
                && Vector3.Distance(currTargetPosition, destination) > unitStoppingDistance.max;
        }

        [SerializeField, Tooltip("Attack movement formation for this unit type.")]
        private MovementFormation formation = new MovementFormation { type = MovementFormation.Type.circle, amount = 4, maxEmpty = 1 };
        /// <summary>
        /// Gets the MovementFormation struct that defines the attack movement formation for the unit.
        /// </summary>
        public MovementFormation Formation { get { return formation; } }

        /// <summary>
        /// Get the appropriate stopping distance for an attack depending on the target type.
        /// </summary>
        /// <param name="target">FactionEntity instance that represents the potential target for the unit.</param>
        /// <param name="min">True to get the minimum value of the stopping range and false to get the maximum value of the stopping range.</param>
        /// <returns>Stopping distance for the unit's movement to launch an attack.</returns>
        public float GetStoppingDistance (FactionEntity target, bool min = true)
        {
            float stoppingDistance;

            EntityTypes targetType = target ? target.Type : EntityTypes.none;
            switch(targetType)
            {
                case EntityTypes.unit:
                    stoppingDistance = min ? unitStoppingDistance.min : unitStoppingDistance.max;
                    break;
                case EntityTypes.building:
                    stoppingDistance = min ? buildingStoppingDistance.min : buildingStoppingDistance.max;
                    break;
                default:
                    stoppingDistance = min ? noTargetStoppingDistance.min : noTargetStoppingDistance.max;
                    break;
            }

            return stoppingDistance + (target ? target.GetRadius() : 0.0f);
        }
    }
}
