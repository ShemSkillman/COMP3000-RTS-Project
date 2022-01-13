using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RTSEngine
{
    public class IdleBuilderTargetPicker : BasicTargetPicker
    {
        public IdleBuilderTargetPicker(params string[] entityKeys) : base(entityKeys)
        {
        }

        protected override bool EntitySatisfiesConditions(Entity entity)
        {
            if (entity is Unit)
            {
                return (entity as Unit).BuilderComp.BuildingTarget == null;
            }
            else
            {
                return false;
            }            
        }
    }
}
