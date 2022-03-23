using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class EnemyTargetPicker : TargetPicker<FactionEntity, int>
    {
        private int myFactionID;

        public override ErrorMessage IsValidTarget(FactionEntity entity)
        {
            if (base.IsValidTarget(entity) == ErrorMessage.none &&
                EntitySatisfiesConditions(entity))
            {
                return ErrorMessage.none;
            }


            return ErrorMessage.invalid;
        }

        protected virtual bool EntitySatisfiesConditions(FactionEntity entity)
        {
            return entity.FactionID != myFactionID;
        }

        public EnemyTargetPicker(int myFactionID)
        {
            this.myFactionID = myFactionID;
        }

        protected override bool IsInList(FactionEntity entity)
        {
            return true;
        }
    }
}