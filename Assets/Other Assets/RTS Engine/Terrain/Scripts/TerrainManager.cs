using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/* Terrain Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class TerrainManager : MonoBehaviour
    {
        [SerializeField]
        private LayerMask groundTerrainMask = new LayerMask(); //layers used for the ground terrain objects
        public LayerMask GetGroundTerrainMask() { return groundTerrainMask; }

        //when sampling height, the multiplier starts with 2.0f and gets incremented each time a non valid position on the navmesh position is found until reaching this max value.
        [SerializeField, Tooltip("Maximum multiplier of the SampleHeight() method search range, if you have a map with varying height levels then make sure to increase this value to fit height variations."), Min(2.0f)]
        private float maxHeightSampleRangeMultiplier = 10.0f;

        [SerializeField]
        private GameObject airTerrain = null; //necessary for flying units, its height determine where flying units will be moving
        public float GetFlyingHeight () { return airTerrain.transform.position.y; }

        //the map's approximate size (usually width*height).
        [SerializeField]
        private float mapSize = 16900;
        public float GetMapSize () { return mapSize; }

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }


        public float SampleHeight(Vector3 position, float radius, LayerMask navLayerMask)
        {
            float multiplier = 2.0f;
            while (multiplier <= maxHeightSampleRangeMultiplier)
            {
                //use sample position to sample the height of the navmesh at the provided position
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, radius * maxHeightSampleRangeMultiplier, navLayerMask))
                    return hit.position.y;

                multiplier += 1.0f;
            }

            return position.y;
        }

        //determine if an object belongs to the terrain tiles: (only regarding ground terrain objects)
        public bool IsTerrainTile(GameObject obj)
        {
            return groundTerrainMask == (groundTerrainMask | (1 << obj.layer));
        }

        //a method that applies a vertical raycast down until hitting a ground terrain position
        public Vector3 GetGroundTerrainPosition (Vector3 position)
        {
            Ray downRay = new Ray(position, Vector3.down); //create a ray that goes down vertically
            if (Physics.Raycast(downRay, out RaycastHit hit, Mathf.Infinity, groundTerrainMask)) //if the ground position is found return it
                return hit.point;

            //else just give back the input position
            return position;
        }
    }
}