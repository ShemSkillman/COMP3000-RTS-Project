namespace RTSEngine.Animation
{
    /// <summary>
    /// Defines all the possible states for the unit's animator controller.
    /// </summary>
    public enum UnitAnimatorState { idle, building, collecting, moving, attacking, healing, converting, takingDamage, dead } 

    /// <summary>
    /// Used to keep a constant reference to the parameters used in the unit's animator controller.
    /// </summary>
    public static class UnitAnimatorParameter
    {
        public const string idle = "IsIdle";
        public const string takingDamage = "TookDamage";
        public const string building = "IsBuilding";
        public const string collecting = "IsCollecting";
        public const string moving = "IsMoving";
        public const string movingState = "InMvtState";
        public const string attacking = "IsAttacking";
        public const string healing = "IsHealing";
        public const string converting = "IsConverting";
        public const string dead = "IsDead";
    }
}
