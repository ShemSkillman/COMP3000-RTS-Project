using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    /// <summary>
    /// Target picker for FactionTypeInfo.
    /// </summary>
    [System.Serializable]
    public class FactionTypeTargetPicker : TargetPicker<FactionTypeInfo, FactionTypeInfo>
    {
        /// <summary>
        /// Is the FactionTypeInfo instance defined as a valid entry.
        /// </summary>
        /// <param name="factionEntity">FactionTypeInfo instance to test.</param>
        /// <returns>True if the input FactionTypeInfo instance is defined as valid entry in this target picker.</returns>
        protected override bool IsInList(FactionTypeInfo factionType)
        {
            foreach (FactionTypeInfo element in targetList)
                if (element == factionType)
                    return true;

            return false;
        }
    }
}
