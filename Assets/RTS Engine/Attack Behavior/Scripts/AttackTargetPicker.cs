using UnityEngine;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackTargetPicker : TargetPicker<FactionEntity, CodeCategoryField>
    {
        [SerializeField, Tooltip("Target and attack units?")]
        private bool engageUnits = true; //can attack units?
        [SerializeField, Tooltip("Target and attack flying units?")]
        private bool engageFlyingUnits = true; //can attack flying units?
        [SerializeField, Tooltip("Target and attack buildings?")]
        private bool engageBuildings = true; //can attack buildings?

        /// <summary>
        /// Determines whether a FactionEntity instance can be picked as a valid attack target.
        /// </summary>
        /// <param name="factionEntity">FactionEntity instance to test.</param>
        /// <returns>ErrorMessage.none if the faction entity can be picked, otherwise ErrorMessage.invalidTarget.</returns>
        public override ErrorMessage IsValidTarget(FactionEntity factionEntity)
        {
            return (factionEntity.Type == EntityTypes.building && !engageBuildings)
                || (factionEntity.Type == EntityTypes.unit
                    && (!engageUnits || ((factionEntity as Unit).MovementComp.AirUnit && !engageFlyingUnits)))
                ? ErrorMessage.invalidTarget : base.IsValidTarget(factionEntity);
        }

        /// <summary>
        /// Is the FactionEntity instance defined in the CodeCategoryField type of list?
        /// </summary>
        /// <param name="factionEntity">FactionEntity instance to test.</param>
        /// <returns>True if the faction entity's code/category is defined in one of the CodeCategoryField entries in the list, otherwise false.</returns>
        protected override bool IsInList(FactionEntity factionEntity)
        {
            foreach (CodeCategoryField ccf in targetList)
                if (ccf.Contains(factionEntity.GetCode(), factionEntity.GetCategory()))
                    return true;

            return false;
        }
    }
}
