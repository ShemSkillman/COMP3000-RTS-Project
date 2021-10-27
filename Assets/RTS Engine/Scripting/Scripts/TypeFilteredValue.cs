using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public abstract class TypeFilteredValue <T,V,E>
    {
        public V allTypes;
        protected V filtered;
        public V GetFiltered () { return filtered; }
        public abstract V Filter(T t, E e);
    }

    [System.Serializable]
    public abstract class TypeFilteredValue <T,V>
    {
        public V allTypes;
        protected V filtered;
        public V GetFiltered () { return filtered; }
        public abstract V Filter(T t);
    }
}
