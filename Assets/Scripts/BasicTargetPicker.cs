using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class BasicTargetPicker : TargetPicker<FactionEntity, string>
    {
        public override ErrorMessage IsValidTarget(FactionEntity entity)
        {
            if (base.IsValidTarget(entity) == ErrorMessage.none &&
                (entity as Unit).BuilderComp.BuildingTarget == null)
            {
                return ErrorMessage.none;
            }
            

            return ErrorMessage.invalid;
        }

        public BasicTargetPicker(params FactionEntity[] entities)
        {
            type = TargetPickerType.allInList;

            foreach (FactionEntity entity in entities)
            {
                targetList.Add(entity.GetCode());
            }
        }

        protected override bool IsInList(FactionEntity factionEntity)
        {
            foreach (string targetCode in targetList)
                if (factionEntity.GetCode() == targetCode)
                    return true;

            return false;
        }
    }
}
