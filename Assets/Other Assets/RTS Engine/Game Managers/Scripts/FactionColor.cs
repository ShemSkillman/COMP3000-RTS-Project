using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public struct FactionColor
    {
        public FactionColor(Color color)
        {
            this.color = color;
            unitColorMaterial = null;
            buildingColorMaterial = null;
        }

        public Color color;
        public Material unitColorMaterial;
        public Material buildingColorMaterial;
    }
}