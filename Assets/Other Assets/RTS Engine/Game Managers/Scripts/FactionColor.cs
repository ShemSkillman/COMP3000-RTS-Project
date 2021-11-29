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
        public FactionColor(Color color, Material unitColorMaterial, Material buildingColorMaterial)
        {
            this.color = color;
            this.unitColorMaterial = unitColorMaterial;
            this.buildingColorMaterial = buildingColorMaterial;
        }

        public Color color;
        public Material unitColorMaterial;
        public Material buildingColorMaterial;
    }
}