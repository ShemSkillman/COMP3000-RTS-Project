using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* Attack Warning Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Manages spawning AttackWarning instances on the minimap.
    /// </summary>
    public class AttackWarningManager : MonoBehaviour
    {
        [SerializeField, Tooltip("UI canvas used to render minimap UI elements.")]
        private Canvas minimapCanvas = null; //minimap canvas that include the warning icons
        [SerializeField, Tooltip("Camera used to render the minimap.")]
        private Camera minimapCamera = null; //the minimap camera

        [SerializeField, Tooltip("Attack warning prefab and an effect object.")]
        private EffectObj prefab = null; //the prefab used to display the warning on a minimap
        private List<AttackWarning> activeList = new List<AttackWarning>(); //a list that holds the currently active attack warnings

        [SerializeField, Tooltip("Played when a new attack warning is spawned.")]
        private AudioClipFetcher audioClip = new AudioClipFetcher(); //when assigned, this will be played each time an attack warning is shown.
        [SerializeField, Tooltip("When enabled, a UI message is displayed when a new attack warning is spawned.")]
        private bool showUIMessage = true; //show UI message when a new attack warning is displayed.

        [SerializeField, Tooltip("The minimum distance required between all active attack warnings.")]
        private float minDistance = 10.0f; //each two attack warnings must have a distance over this between each other

        GameManager gameMgr;

        /// <summary>
        /// Initializes the AttackWarningManager component (called from the GameManager)
        /// </summary>
        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            //component invariants:
            Assert.IsNotNull(minimapCanvas, "[Attack Warning Manager] The 'Minimap Canvas' field hasn't been assigned!");
            Assert.IsNotNull(minimapCamera, "[Attack Warning Manager] The 'Minimap Camera' field hasn't been assigned!");
            Assert.IsNotNull(prefab, "[Attack Warning Manager] The 'Prefab' field hasn't been assigned!");
        }

        /// <summary>
        /// Checks whether a new attack warning can be added in a potential position
        /// </summary>
        public bool CanAdd (Vector3 potentialPosition)
        {
            //distance check: if there's another attack warning in the chosen max distance, then there's no need to show it another time
            //go through all the spawned attack warnings
            foreach (AttackWarning aw in activeList)
                //check the distance between this one and the requested position
                if (Vector3.Distance(aw.targetPosition, potentialPosition) < minDistance)
                    return false; //the new attack warning will be too close to another active one

            return true;
        }

        /// <summary>
        /// Spawns a new attack warning
        /// </summary>
        public void Add (Vector3 targetPosition)
        {
            //first we check whether we can actually add a attack warning (if there isn't one already in that range)
            if (!CanAdd(targetPosition))
                return;

            //set the the new attack warning's position
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                minimapCanvas.GetComponent<RectTransform>(),
                minimapCamera.WorldToScreenPoint(targetPosition), minimapCamera, out Vector2 canvasPos))
            {
                Vector3 spawnPosition = new Vector3(canvasPos.x, canvasPos.y, 0.0f);

                //create new attack warning element (or get an inactive one)
                AttackWarning newWarning = gameMgr.EffectPool.SpawnEffectObj(prefab, spawnPosition, prefab.GetComponent<RectTransform>().localRotation,
                    minimapCanvas.transform, true, true, 0.0f, true).GetComponent<AttackWarning>();

                //set the attack warning parameters:
                newWarning.Init(this, targetPosition);
                activeList.Add(newWarning); //add it to the active attack warnings list

                //play audio:
                gameMgr.AudioMgr.PlaySFX(audioClip.Fetch(), false);

                if (showUIMessage == true)
                    ErrorMessageHandler.OnErrorMessage(ErrorMessage.underAttack, null);
            }
            else
                Debug.LogError("[AttackWarningManager] Unable to find the target position for the new attack warning on the minimap canvas!");
        }

        /// <summary>
        /// Called from an AttackWarning instance when it is disabled.
        /// </summary>
        public void OnAttackWarningDisabled (AttackWarning attackWarning)
        {
            activeList.Remove(attackWarning); //remove the disabled attack warning from the active list
        }
    }
}