using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    /// <summary>
    /// Includes data that will be used to regulate the creation of the assigned unit by an NPC faction.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitRegulatorData", menuName = "RTS Engine/NPC Unit Regulator Data", order = 52)]
    public class NPCUnitRegulatorData : NPCRegulatorData
    {
        //define the units amount ratio in relation to the population slots available for the faction
        [SerializeField, Tooltip("Instances of this unit amount to available population slots target ratio.")]
        private FloatRange ratioRange = new FloatRange(0.1f, 0.2f);
        public float GetRatio () { return ratioRange.getRandomValue(); }
    }
}
