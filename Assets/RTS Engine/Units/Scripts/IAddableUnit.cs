using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IAddableUnit
    {
        /// <summary>
        /// Represents the interaction position at which the unit can be added.
        /// </summary>
        Vector3 AddablePosition { get; }

        /// <summary>
        /// Move a unit towards the addable position to be added.
        /// </summary>
        /// <param name="unit">Unit instance to move towards the interaction position.</param>
        /// <param name="playerCommand">True if the method was called through a direct player command, otherwise false.</param>
        /// <returns>ErrorMessage.none if the unit can be moved to be added, otherwise failure error code.</returns>
        ErrorMessage Move(Unit unit, bool playerCommand);

        /// <summary>
        /// Adds a unit to the component that implements the IAddableUnit interface.
        /// </summary>
        /// <param name="unit">Unit instance to add.</param>
        /// <returns>ErrorMessage.none if the unit is successfully added, otherwise failure error code.</returns>
        ErrorMessage Add (Unit unit);
    }
}
