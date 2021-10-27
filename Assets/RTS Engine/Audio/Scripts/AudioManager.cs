using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Linq;
using System;

/* AudioManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Manages playing audio clips during the game.
    /// </summary>
	public class AudioManager : MonoBehaviour {

        #region Class Properties
        //Music Loops:
        [SerializeField, Tooltip("Audio clips for music to be played during the game."), Header("Music")]
        private AudioClipFetcher music = new AudioClipFetcher();

        [SerializeField, Tooltip("Play the music audio clips as soon as the game starts.")]
        private bool playMusicOnStart = true;

        /// <summary>
        /// Are the music loops currently active and playing?
        /// </summary>
        public bool IsMusicActive { private set; get; } //is the music currently playing?

        [SerializeField, Tooltip("Volume of the music audio clips."), Range(0.0f, 1.0f)]
        private float musicVolume = 1.0f;

        [SerializeField, Tooltip("UI Slider that allows to modify the music loop's volume.")]
        private Slider musicVolumeSlider = null;

        [SerializeField, Tooltip("AudioSource component that plays the music loops.")]
        private AudioSource musicAudioSource = null;

        private Coroutine musicCoroutine; //references the music coroutine, responsible for playing music clips one after another

        //SFX:
        [SerializeField, Tooltip("AudioSource component that plays the global sound effects during the game."), Header("SFX")]
        private AudioSource globalSFXAudioSource = null;

        [SerializeField, Tooltip("Volume of the audio clips that play from the Global SFX Audio Source and from local audio sources."), Range(0.0f, 1.0f)]
        private float SFXVolume = 1.0f;

        [SerializeField, Tooltip("UI Slider that allows to modify the SFX loop's volume.")]
        private Slider SFXVolumeSlider = null;

        private List<AudioSource> localAudioSources = new List<AudioSource>(); //holds all local audio source instances in the game (coming from units, buildings, resources and custom events).

        //other components:
        private GameManager gameMgr;
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the active AudioManager instance in the game.
        /// </summary>
        /// <param name="gameMgr">The active GameManager instance responsible for managing the current game.</param>
        public void Init (GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            if(globalSFXAudioSource == null)
                Debug.LogWarning("[AudioManager] 'Global SFX Audio Source' hasn't been assigned!");

            if(musicAudioSource == null)
                Debug.LogWarning("[AudioManager] 'Music Audio Source' hasn't been assigned!");

            IsMusicActive = false;
            if (playMusicOnStart == true) //if we're able to start playing the music on start
                PlayMusic();

            //subscribe to following events to monitor creation and destruction of entities:
            CustomEvents.UnitCreated += OnUnitResourceCreated;
            CustomEvents.ResourceAdded += OnUnitResourceCreated;

            CustomEvents.FactionEntityDead += OnEntityDead;
            CustomEvents.ResourceEmpty += OnEntityDead;
            CustomEvents.BuildingPlaced += OnBuildingPlaced;

            EffectObj.EffectObjCreated += OnEffectObjCreated;
            EffectObj.EffectObjDestroyed += OnEffectObjDestroyed;

            //set initial volume
            UpdateSFXVolume(SFXVolume);
            UpdateMusicVolume(musicVolume);
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        private void OnDisable()
        {
            //unsub to following events:
            CustomEvents.UnitCreated -= OnUnitResourceCreated;
            CustomEvents.ResourceAdded -= OnUnitResourceCreated;
            CustomEvents.BuildingPlaced -= OnBuildingPlaced;

            CustomEvents.FactionEntityDead -= OnEntityDead;
            CustomEvents.ResourceEmpty -= OnEntityDead;

            EffectObj.EffectObjCreated -= OnEffectObjCreated;
            EffectObj.EffectObjDestroyed -= OnEffectObjDestroyed;
        }
        #endregion

        #region Custom Events
        /// <summary>
        /// Called when an Entity instance is initialzed.
        /// </summary>
        /// <param name="entity">Entity instance that is initialized.</param>
        private void OnUnitResourceCreated (Entity entity)
        {
            AddLocalAudioSource(entity.AudioSourceComp); //add the entity's audio source component to the list.
        }

        private void OnBuildingPlaced(Building building)
        {
            AddLocalAudioSource(building.AudioSourceComp);
        }

        /// <summary>
        /// Called when an EffectObj instance is created and initialized.
        /// </summary>
        /// <param name="effectObj">EffectObj instance that is initialized.</param>
        private void OnEffectObjCreated (EffectObj effectObj)
        {
            AddLocalAudioSource(effectObj.AudioSourceComp); //add the effect object's audio source component to the list.
        }

        /// <summary>
        /// Called when an EffectObj instance is destroyed.
        /// </summary>
        /// <param name="effectObj">EffectObj instance that is destroyed.</param>
        private void OnEffectObjDestroyed (EffectObj effectObj)
        {
            localAudioSources.Remove(effectObj.AudioSourceComp); //remove effect object's audio source component from list.
        }

        /// <summary>
        /// Called when an Entity instance is dead.
        /// </summary>
        /// <param name="entity">Entity instance that is dead.</param>
        private void OnEntityDead (Entity entity)
        {
            localAudioSources.Remove(entity.AudioSourceComp); //remove the entity's audio source component to the list.
        }
        #endregion

        #region Local/Global SFX
        /// <summary>
        /// Called when the local and global SFX volume slider's value is updated.
        /// </summary>
        public void OnSFXVolumeSliderUpdated ()
        {
            UpdateSFXVolume(SFXVolumeSlider.value);
        }

        private void AddLocalAudioSource(AudioSource newSource)
        {
            if (newSource == null)
                return;

            newSource.volume = SFXVolume;
            localAudioSources.Add(newSource);
        }

        /// <summary>
        /// Updates the volume of the local SFX AudioSource instances (coming from units, buildings, resources and attack objects) and the global SFX AudioSource instance.
        /// </summary>
        /// <param name="volume">The new volume value for the local and global audio sources.</param>
        public void UpdateSFXVolume (float volume)
        {
            SFXVolume = Mathf.Clamp01(volume); //volume's value can be only in [0.0, 1.0]

            foreach (AudioSource source in localAudioSources)
                source.volume = SFXVolume;

            if(globalSFXAudioSource)
                globalSFXAudioSource.volume = SFXVolume;

            if(SFXVolumeSlider)
                SFXVolumeSlider.value = SFXVolume; //update the music's volume UI slider as well
        }
        /// <summary>
        /// Plays an AudioClip instance on a given AudioSource instance (Used for local sound effects).
        /// </summary>
        /// <param name="source">AudioSource instance to play the clip.</param>
        /// <param name="clip">AudioClip instance to be played.</param>
        /// <param name="loop">When true, the audio clip will be looped.</param>
        public void PlaySFX (AudioSource source, AudioClip clip, bool loop = false)
		{
            if (clip == null) //in case no clip is assigned, do not continue.
                return;

            //make sure that there's a valid audio source before playing the clip
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] AudioSource is missing, can not play the audio clip.");
                return;
            }

            source.Stop (); //stop the current audio clip from playing.

            source.clip = clip;
            source.loop = loop;

            source.Play (); //play the new clip
		}

        /// <summary>
        /// Plays an AudioClip instance in the global SFX audio source (Used for global sound effects).
        /// </summary>
        /// <param name="clip">AudioClip instance to be played.</param>
        /// <param name="loop">When true, the audio clip will be looped.</param>
        public void PlaySFX (AudioClip clip, bool loop = false)
        {
            PlaySFX(globalSFXAudioSource, clip, loop); 
        }

        /// <summary>
        /// Stops playing audio from an AudioSource instance (Used for local sound effects).
        /// </summary>
        /// <param name="source">AudioSource instance to stop.</param>
		public void StopSFX (AudioSource source)
		{
            if (source == null)
                return;

            source.Stop();
		}

        /// <summary>
        /// Stops playing audio from the global SFX audio source.
        /// </summary>
        public void StopSFX ()
        {
            globalSFXAudioSource.Stop();
        }
        #endregion

        #region Music Loops
        /// <summary>
        /// Called when the music's volume slider's value is updated.
        /// </summary>
        public void OnMusicVolumeSliderUpdated ()
        {
            UpdateMusicVolume(musicVolumeSlider.value);
        }

        /// <summary>
        /// Updates the volume of the music loops.
        /// </summary>
        /// <param name="volume">The new volume value for the music loops.</param>
        public void UpdateMusicVolume (float volume)
        {
            musicVolume = Mathf.Clamp01(volume);

            if (musicAudioSource)
                musicAudioSource.volume = musicVolume;

            if(musicVolumeSlider)
                musicVolumeSlider.value = musicVolume; //update the music's volume UI slider as well
        }

        /// <summary>
        /// Starts playing the music loops.
        /// </summary>
        public void PlayMusic()
        {
            if(!IsMusicActive)
                musicCoroutine = StartCoroutine(OnMusicCoroutine());
        }

        /// <summary>
        /// Coroutine that plays music loops.
        /// </summary>
        private IEnumerator OnMusicCoroutine ()
        {
            if (music.Count <= 0 || musicAudioSource == null) //if no music clips have been assigned then do not play anything
                yield break;

            IsMusicActive = true;

            while (true)
            {
                //get the next audio clip and play it
                musicAudioSource.clip = music.Fetch();
                musicAudioSource.Play();

                //wait for the current music clip to end to play the next one:
                yield return new WaitForSeconds(musicAudioSource.clip.length);
            }
        }

        /// <summary>
        /// Stops playing music loops.
        /// </summary>
        public void StopMusic ()
        {
            if (!IsMusicActive)
                return;

            StopCoroutine(musicCoroutine);
            IsMusicActive = false;
        }
        #endregion
    }
}