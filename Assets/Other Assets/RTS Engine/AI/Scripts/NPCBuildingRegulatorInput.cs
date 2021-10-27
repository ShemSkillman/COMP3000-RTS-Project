using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class NPCBuildingRegulatorDataSingle : TypeFilteredValue<FactionTypeInfo, NPCBuildingRegulatorData, string>
    {
        [System.Serializable]
        public struct Element
        {
            public bool ignoreFactionType;
            public FactionTypeInfo type;
            public bool ignoreNPCType;
            public NPCTypeInfo npcType;
            public NPCBuildingRegulatorData value;
        }
        public List<Element> typeSpecific = new List<Element>();

        /// <summary>
        /// Filters the content of the typeSpecific list depending on the given faction type and NPC Manager code
        /// </summary>
        public override NPCBuildingRegulatorData Filter(FactionTypeInfo factionType, string npcManagerCode)
        {
            filtered = allTypes; //regulators assigned to the allTypes list are available for all faction types

            //as for faction specific unit regulators
            foreach (Element e in typeSpecific)
                //we can either ignore the faction
                if ((e.ignoreFactionType || e.type == factionType) && (e.ignoreNPCType || e.npcType?.Key == npcManagerCode))
                {
                    filtered = e.value;
                    break;
                }

            return filtered; //filtered list that includes regulators available for the given faction type only
        }
    }
}
