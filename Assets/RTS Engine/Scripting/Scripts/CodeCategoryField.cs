using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public struct CodeCategoryField
    {
        public enum CodeType {entityCode, entityCategory};

        public CodeType type; //type of the code to be inserted (either the direct code or a category)
        public List<string> code; //either the codes of entitie (unit/building) or categories here

        public bool Contains (string entityCode, string category) //check if the input is inside the codes list
        {
            return type == CodeType.entityCode ? code.Contains(entityCode) : code.Contains(category);
        }
    }
}
