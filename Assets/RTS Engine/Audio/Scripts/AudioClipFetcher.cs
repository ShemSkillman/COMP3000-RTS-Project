using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* AudioClipFetcher script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Defines the types of fetching an AudioClip instance from a set where:
    /// random: One audio clip is randomly chosen each time.
    /// randomNoRep: One audio clip is randomly chosen each time with the guarantee that the same audio clip will not be chosen consecutively.
    /// inOrder: Fetch audio clips in the order they were defined in.
    /// </summary>
    public enum AudioClipFetchType { random, randomNoRep, inOrder }

    /// <summary>
    /// Allows to define a set of AudioClip instances and retrieve one of them depending on the chosen type.
    /// </summary>
    [System.Serializable]
    public class AudioClipFetcher
    {
        #region Class Properties
        [SerializeField, Tooltip("How would the audio clip be fetched each time?")]
        private AudioClipFetchType fetchType = AudioClipFetchType.random;

        [SerializeField, Tooltip("An array of audio clips that can be potentially fetched.")]
        private AudioClip[] audioClips = new AudioClip[0];

        /// <summary>
        /// Returns the amount of AudioClip instances assigned to the AudioClipFetcher instance.
        /// </summary>
        public int Count
        {
            get
            {
                return audioClips.Length;
            }
        }

        private int cursor = 0; //for the inOrder and randomNoRep fetch types, this is used to track the last fetched audio clip.
        #endregion

        #region Audio Clip Fetching
        /// <summary>
        /// Gets the next AudioClip instance in the 'audioClips' array depending on the 'cursor' value.
        /// </summary>
        /// <returns>AudioClip instance from the 'audioClips' array.</returns>
        private AudioClip GetNext ()
        {
            //move the cursor one step further through the array
            if (cursor >= audioClips.Length - 1)
                cursor = 0;
            else
                cursor++;

            return audioClips[cursor]; //return the next audio clip in the array
        }

        /// <summary>
        /// Fetches an AudioClip instance from defined set depending on the fetch type.
        /// </summary>
        /// <returns>AudioClip instance from the 'audioClips' array.</returns>
        public virtual AudioClip Fetch ()
        {
            if (audioClips.Length <= 0)
                return null;

            switch (fetchType) //depending on the fetch type:
            {
                case AudioClipFetchType.randomNoRep:

                    int clipIndex = Random.Range(0, audioClips.Length); //pick a random audio clip index

                    if (clipIndex == cursor) //if this is the same as the last fetched audio clip:
                        return GetNext(); //get the next audio clip in the array
                    else //the random audio clip index does not match with the previous one
                    {
                        cursor = clipIndex; //set the cursor value and return the new audio clip
                        return audioClips[cursor];
                    }

                case AudioClipFetchType.inOrder: //fetch audio clips depending on their order in the array
                    return GetNext();

                default: //random case:
                    return audioClips[Random.Range(0, audioClips.Length)];
                        
            }
        }
        #endregion
    }
}
