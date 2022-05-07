using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RTSEngine
{
    public class IdleBuilderTargetPicker : BasicTargetPicker
    {
        private int myFactionID;

        public IdleBuilderTargetPicker(int factionID, params string[] entityKeys) : base(entityKeys)
        {
            myFactionID = factionID;
        }

        protected override bool EntitySatisfiesConditions(Entity entity)
        {
            if (entity is Unit)
            {
                return (entity as Unit).FactionID == myFactionID && (entity as Unit).BuilderComp.BuildingTarget == null;
            }
            else
            {
                return false;
            }            
        }
    }
}
