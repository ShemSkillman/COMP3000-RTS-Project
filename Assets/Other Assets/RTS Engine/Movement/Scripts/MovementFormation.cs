using UnityEngine;

/* MovementFormation script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    /// <summary>
    /// Defines the movement formation type and its settings.
    /// </summary>
    public struct MovementFormation
    {

#if UNITY_EDITOR
        [HideInInspector]
        public bool showProperties;
#endif

        /// <summary>
        /// Defines the types of possible movement formations.
        /// </summary>
        public enum Type { circle, row };

        [SerializeField, Tooltip("Type of the movement formation.")]
        public Type type;

        [SerializeField, Tooltip("Amount of formation positions per iteration."), Min(1)]
        public int amount; //in case of a row formation type, this is the amount of units per row.
        [SerializeField, Tooltip("Maximum amount of empty iterations before switching the formation type."), Min(1)]
        public int maxEmpty; //in case of a row formation type this is the maximum amount of empty rows before switching the formation.

        [SerializeField, Tooltip("Space between units in the formation will be increased by this value."), Min(0.0f)]
        public float spacing;
    }
}
