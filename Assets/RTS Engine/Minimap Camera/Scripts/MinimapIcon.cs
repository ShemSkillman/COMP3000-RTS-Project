using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* Minimap Icon script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Assigned to an entity to represent its icon on the minimap
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        /// <summary>
        /// Initializes the MinimapIcon component
        /// </summary>
        public void Init ()
        {
            meshRenderer = GetComponent<MeshRenderer>();

            Assert.IsNotNull(meshRenderer, "[MinimapIcon] There's no 'Mesh Renderer' component attached!");
        }

        /// <summary>
        /// Updates the color of the minimap icon.
        /// </summary>
        public void SetColor (Color color)
        {
            meshRenderer.material.color = color;
        }
    }
}