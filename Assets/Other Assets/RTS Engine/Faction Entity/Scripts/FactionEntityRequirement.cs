using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public struct FactionEntityRequirement
    {
        [Tooltip("The text that will be displayed in UI to represent this faction entity requirement")]
        public string name;

        [Tooltip("For each spawned faction unit/building that has its code/category defined here, it will count as +1.")]
        public CodeCategoryField codes;
        [Tooltip("How many of the above defined spawned faction units/buildings are needed to fulfill this requirement?")]
        public int amount;
    }
}
