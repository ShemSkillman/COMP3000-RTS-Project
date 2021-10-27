using UnityEngine;

using RTSEngine.EntityComponent;

/* Portal script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
	public class Portal : MonoBehaviour, IAddableUnit
    {
        public Building building { private set; get; } //the main building component for which this component opeartes
        public bool IsActive { private set; get; } //is this component active?

        [SerializeField]
        private Transform spawnPosition = null; //the position where units come out of this portal

        /// <summary>
        /// Gets the position that a unit needs to reach in order to use the portal.
        /// </summary>
        public Vector3 AddablePosition { get { return spawnPosition.position; } }

        [SerializeField]
        private Transform gotoPosition = null; //if there's a goto pos, then the unit will move to this position when they spawn.

        [SerializeField]
		private Portal targetPortal = null; //the target's portal that this portal teleports to.

        [SerializeField]
        private bool allowAllUnits = true; //does this portal allow all units? if this is true, then the two next attributes can be ignored
        [SerializeField]
        private bool allowInListOnly = false; //if the above option is disabled, then when this is true, only unit types with the codes in the list below will be allowed
        //...however, if set to false, then all unit types but the ones specified in the list below will be allowed.
        [SerializeField]
        private CodeCategoryField codesList = new CodeCategoryField(); //a list of the allowed unit codes that are not allowed to use the portal

        //audio clips:
        [SerializeField, Tooltip("What audio clip to play when a unit enters the portal?")]
        private AudioClipFetcher teleportAudio = new AudioClipFetcher(); //audio clip played when a unit enters this portal

		//double clicking on the portal changes the camera view to the target portal
		private float doubleClickTimer;
		private bool clickedOnce = false;

        private GameManager gameMgr;

        public void Init(GameManager gameMgr, Building building)
        {
            this.gameMgr = gameMgr;
            this.building = building; //get the main building's component

            if (spawnPosition == null)
                Debug.LogError("[Portal]: You must assign a spawn position (transform) for the portal to spawn units at");

            //initial settings for the double click
            clickedOnce = false;
			doubleClickTimer = 0.0f;

            IsActive = true;
        }

		void Update ()
		{
            if (!IsActive)
                return;

			//double click timer:
			if (clickedOnce == true) {
				if (doubleClickTimer > 0)
                    doubleClickTimer -= Time.deltaTime;
                if (doubleClickTimer <= 0)
                    clickedOnce = false;
            }
		}

        /// <summary>
        /// Teleports a unit to the target of the portal.
        /// </summary>
        /// <param name="unit">Unit instance to teleport.</param>
        /// <returns>ErrorMessage.none if the unit is successfully added to the APC, otherwise failure error code.</returns>
		public ErrorMessage Add (Unit unit)
		{
            if (!IsActive) //if the component is not active, do not continue
                return ErrorMessage.inactive;

            unit.gameObject.SetActive(false); //deactivate the unit's object
            unit.transform.position = targetPortal.spawnPosition.position; //move the unit to the target portal's spawn position
            unit.gameObject.SetActive(true); //activate the unit's object again

            CustomEvents.OnUnitTeleport(this, targetPortal, unit); //trigger custom events

            //if the target portal has a goto position, move the unit there
            if(targetPortal.gotoPosition)
                gameMgr.MvtMgr.Move(unit, targetPortal.gotoPosition.position, 0.0f, null, InputMode.movement, false);

            return ErrorMessage.none;
        }

        //a method that is called when a mouse click on this portal is detected
        public void OnMouseClick ()
		{
            if (!IsActive) //if the component is not active, do not continue
                return;

            if (clickedOnce == false)
            { //if the player hasn't clicked on this portal shortly before this click
                doubleClickTimer = 0.5f; //launch the double click timer
                clickedOnce = true; //change to true to mark that the second click (to finish the double click) is awaited
            }
            else if (targetPortal != null)
            { //if this is the second click (double click)
                CustomEvents.OnPortalDoubleClick(this, targetPortal, null); //trigger the custom event

                gameMgr.AudioMgr.PlaySFX(building.AudioSourceComp, teleportAudio.Fetch(), false); //play the teleport audio clip
                gameMgr.CamMgr.LookAt(targetPortal.transform.position, false); //change the main camera's view to look at the target portal
                //gameMgr.CamMgr.SetMiniMapCursorPos(targetPortal.transform.position); //change the minimap's cursor position to be over the target portal
            }
		}

        /// <summary>
        /// Move a unit towards the interaction position of the portal to be added.
        /// </summary>
        /// <param name="unit">Unit instance to move to the portal to eventually teleport.</param>
        /// <param name="playerCommand">True if the method was called through a direct player command, otherwise false.</param>
        /// <returns>ErrorMessage.none if the unit can be moved to use the portal (fullfils certain conditions), otherwise failure error code.</returns>
        public ErrorMessage Move (Unit unit, bool playerCommand)
        {
            if (targetPortal == null || targetPortal.spawnPosition == null) //if this portal doesn't have a target portal
                return ErrorMessage.targetPortalMissing; //let the player know that this portal doesn't have a target.

            if (!CanUsePortal(unit.FactionID)) //can't the unit's faction use the portal?
            {
                print("here");
                return ErrorMessage.noFactionAccess;
            }

            if (!IsUnitAllowed(unit)) //can't the unit specifically use this portal?
                return ErrorMessage.entityNotAllowed; 

            //move the unit to the portal
            gameMgr.MvtMgr.Move(unit, AddablePosition, 0.0f, building, InputMode.addUnit, playerCommand);

            return ErrorMessage.none;
        }

        //can a certain faction use this portal? 
        public bool CanUsePortal (int factionID)
        {
            //a unit can use the portal only if it's built and it either has the same faction ID as the portal or if the portal is a free building
            return IsActive && (building.FactionID == factionID || building.IsFree() == true);
        }

		//What units are allowed through this portal?
		public bool IsUnitAllowed (Unit unit)
		{
			if (allowAllUnits) //if all units are allowed, then yes
				return true;
            //depending on the portal's settings, see if the unit is allowed through the portal or not
            return (allowInListOnly == codesList.Contains(unit.GetCode(), unit.GetCategory()));
		}
	}
}