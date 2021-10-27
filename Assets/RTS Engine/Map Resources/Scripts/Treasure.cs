using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Treasure script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class Treasure : MonoBehaviour {

        [SerializeField]
        private ResourceInput[] content = new ResourceInput[0]; //the resources to give the faction when it claims this treasure.
        [SerializeField, Tooltip("What audio clip to play when the treasure is claimed by the local player?")]
        private AudioClipFetcher claimAudio = new AudioClipFetcher(); //audio played when the treasure is claimed by a faction
        [SerializeField]
        private EffectObj claimEffect = null; //effect spawned when the treasure is claimed

        //a method called to assign the treasure for a faction
        public void Trigger (int factionID, GameManager gameMgr)
        {
            gameMgr.ResourceMgr.UpdateRequiredResources(content, true, factionID); //add the treasure's resources

            if(factionID == GameManager.PlayerFactionID) //if this is the local player's faction then play the claim audio
                gameMgr.AudioMgr.PlaySFX(claimAudio.Fetch(), false);

            if (claimEffect == null) //if there's no effect object, stop here
                return;

            gameMgr.EffectPool.SpawnEffectObj(claimEffect, transform.position, Quaternion.identity); //spawn the claim effect object
        }
	}
}