using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Effect Object Spawner script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class EffectObjSpawner : MonoBehaviour
    {
        //this script allows you to spawn an effect object using the properties from the following fields:

        [SerializeField]
        private EffectObj prefab = null;
        [SerializeField]
        private Transform spawnPosition = null;
        [SerializeField]
        private Transform parent = null;
        [SerializeField]
        private bool enableLifeTime = true;
        [SerializeField]
        private bool autoLifeTime = true;
        [SerializeField]
        private float customLifeTime = 0.0f;

        //other components
        GameManager gameMgr;

        public void Init (GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        //the method used to spawn the effect object.
        public void Spawn ()
        {
            if (prefab == null || transform == null)
                return;

            gameMgr.EffectPool.SpawnEffectObj(prefab, spawnPosition.position, prefab.transform.rotation, parent, enableLifeTime, autoLifeTime, customLifeTime);
        }
    }
}
