using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class FactionColorMenu
    {
        [SerializeField]
        private Color[] allowed = new Color[0]; //the colors that a faction is allowed to take.

        public Color Get(int index) { return allowed[index]; }
        public int GetNextIndex(int currentIndex) //gets the next index of the color inside the allowed colors array
        {
            if (currentIndex >= allowed.Length - 1)
                return 0;
            else
                return currentIndex + 1;
        }

    }
}
