using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Chained Sorted List component created by Oussama  Bouanani, SoumiDelRio
 * This component is part of the Unity RTS Engine asset
 * This serves as a normal sorted list that accepts multiple values for one key
 * */

namespace RTSEngine
{
    public class ChainedSortedList<K,V>
    {
        SortedList<K, List<V>> sortedList;

        /// <summary>
        /// Returns a list of the values (where each value is a list since we're allowed to have multiple values per key).
        /// </summary>
        public IList<List<V>> Values
        {
            get { return sortedList.Values; }
        }

        public ChainedSortedList () //constructor #1
        {
            sortedList = new SortedList<K, List<V>>();
        }

        //adds a new (key,value) pair to the chained sorted list (where a key can have multiple values).
        public void Add(K key, V value)
        {
            List<V> currentList;
            //if the key already exists in the sorted list, its "value list" will be outputed in the current list and we'll then simply add the new value
            //if the key doesn't exist in the sorted list, we'll initialize a new value list then
            if(!sortedList.TryGetValue(key, out currentList))
            {
                currentList = new List<V>();
                sortedList[key] = currentList;
            }

            currentList.Add(value); //add the new value to the list in the position of the key.
        }

        //enumerator:

        public IEnumerator<KeyValuePair<K, List<V>>> GetEnumerator()
        {
            return sortedList.GetEnumerator();
        }
    }
}
