using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* Minimap Icon Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Manages spawning minimap icons for entities.
    /// </summary>
    public class MinimapIconManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab cloned to spawn a new minimap icon")]
        private EffectObj prefab = null; //the minimap's icon prefab
        [SerializeField, Tooltip("How high should the minimap icons be?")]
        private float height = 20.0f; //height of the minimap icon

        //Manager components
        GameManager gameMgr;

        /// <summary>
        /// Initializes the MinimapIconManager component
        /// </summary>
        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            Assert.IsNotNull(prefab, "[Minimap Icon Manager] The 'Prefab' field hasn't been assigned!");
        }

        /// <summary>
        /// Assigns a minimap icon to a SelectionEntity instance
        /// </summary>
        public void Assign (SelectionEntity selection)
        {
            selection.UpdateMinimapIcon(Get(selection.Source.transform, selection.GetMinimapIconSize())); //assign the minimap icon to the selection obj component
            selection.UpdateMinimapIconColor(); //update the new minimap icon color
        }

        /// <summary>
        /// Creates a new minimap icon or gets an inactive one.
        /// </summary>
        private MinimapIcon Get (Transform source, float size)
        {
            //get a new minimap icon from object pool
            MinimapIcon nextIcon = gameMgr.EffectPool.SpawnEffectObj(prefab,
                new Vector3(source.position.x, height, source.position.z), prefab.transform.rotation, null, false).GetComponent<MinimapIcon>();

            nextIcon.Init();

            //set the size of the icon
            nextIcon.transform.localScale = Vector3.one * size;
            //set its parent object
            nextIcon.transform.SetParent(source, true);

            return nextIcon;
        }
    }
}