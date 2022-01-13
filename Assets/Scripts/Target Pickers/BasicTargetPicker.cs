using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class BasicTargetPicker : TargetPicker<Entity, string>
    {

        public override ErrorMessage IsValidTarget(Entity entity)
        {
            if (base.IsValidTarget(entity) == ErrorMessage.none &&
                EntitySatisfiesConditions(entity))
            {
                return ErrorMessage.none;
            }
            

            return ErrorMessage.invalid;
        }

        protected virtual bool EntitySatisfiesConditions(Entity entity)
        {
            return true;
        }

        public BasicTargetPicker(params string[] entitykeys)
        {
            type = TargetPickerType.allInList;

            foreach (string key in entitykeys)
            {
                targetList.Add(key);
            }
        }

        protected override bool IsInList(Entity entity)
        {
            foreach (string targetCode in targetList)
                if (entity.GetCode() == targetCode)
                    return true;

            return false;
        }
    }
}
