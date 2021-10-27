using System.Collections.Generic;
using UnityEngine;
using RTSEngine.EntityComponent;

/* Entity script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public enum EntityTypes { none, unit, building, resource, all }; //possible types of the entities

    public abstract class Entity : MonoBehaviour
    {
        public abstract EntityTypes Type { get; }

        [SerializeField]
        private string _name = "entity_name"; //the name of the entity that will be displayd when it is selected.
        public string GetName() { return _name; }
        [SerializeField]
        private string code = "entity_code"; //unique code for each entity that is used to identify it in the system.
        public string GetCode () { return code; }
        [SerializeField]
        private string category = "entity_category"; //the category that this entity belongs to.
        public string GetCategory() { return category; }
        [SerializeField]
        private string description = "entity_description"; //the description of the entity that will be displayed when it is selected.
        public string GetDescription() { return description; }

        [SerializeField]
        private Sprite icon = null; //the icon that will be displayed when the faction entity is selected.
        public Sprite GetIcon() { return icon; }

        [SerializeField]
        protected bool free = false; //does this entity belong to no faction?
        public bool IsFree() { return free; }
        public void SetFree (bool free) { this.free = free; }

        //Audio:
        [SerializeField]
        protected AudioClipFetcherCooldown selectionAudio = new AudioClipFetcherCooldown(); //Audio played when the building is selected.
        public AudioClip SelectionAudio => selectionAudio.Fetch();

        //Faction entity components:
        [SerializeField]
        protected GameObject plane = null;

        [SerializeField]
        protected GameObject model = null; //the entity's model goes here.
        public void ToggleModel(bool show) { model.SetActive(show); } //hide the entity's model

        [SerializeField]
        protected SelectionEntity selection = null; //The selection object of the entity goes here.
        public SelectionEntity GetSelection () { return selection; }

        //selection flash:
        float flashTimer;
        bool isFlashActive = false; //is the selection flash currently flashing?
        protected Color color; //the selection flash color is assigned here.
        public Color GetColor () { return color; }

        //other components that can be attached to a entity:
        public Renderer PlaneRenderer { private set; get; } //this is the Renderer component of the plane's object.

        [SerializeField]
        protected float radius = 2.0f; //the radius of this resource that defines where units can interact with it
        public float GetRadius() { return radius; }

        /// <summary>
        /// Can other entities interact with the Entity instance?
        /// </summary>
        public bool Interactable { set; get; }

        //entity components:
        public AudioSource AudioSourceComp { private set; get; }
#if RTSENGINE_FOW
        public HideInFogRTS HideInFog { private set; get; }
#endif

        private IEntityComponent[] components = new IEntityComponent[0];
        /// <summary>
        /// Returns the IEnumerable instance of the faction entity attached components that implement the IFactionEntityComponentTaskSingle interface.
        /// </summary>
        public IEnumerable<IEntityComponent> ComponentTasks { get { return components; } }

        /// <summary>
        /// Can the Entity instance run the IEntityComponent components attached to it
        /// </summary>
        public bool CanRunComponents { protected set; get; }

        /// <summary>
        /// Component attached to the entity that implements the IAddableUnit interface.
        /// Currently an entity can only have one recognized instance of this component type attached to it.
        /// </summary>
        public IAddableUnit AddableUnitComp { private set; get; }

        //other components
        protected GameManager gameMgr;

        //multiplayer related:
        public int MultiplayerKey { private set; get; }

        #region Entity Events
        /// <summary>
        /// Delegate used for Entity related events.
        /// </summary>
        /// <param name="entity">Entity instance.</param>
        public delegate void EntityEventHandler (Entity entity);

        /// <summary>
        /// Event triggered when an Entity instance is initialized.
        /// </summary>
        public static event EntityEventHandler EntityCreated = delegate { };
        #endregion

        public virtual void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            Interactable = true; //by default, a faction entity is interactable.

            //get the audio source component attached to the entity main object:
            AudioSourceComp = GetComponent<AudioSource>();
#if RTSENGINE_FOW
            HideInFog = selection.GetComponent<HideInFogRTS>();
#endif
            CanRunComponents = false; //by default, entities can not run components when first initialized.
            //finding and initializing entity components.
            components = GetComponents<IEntityComponent>();
            foreach (IEntityComponent component in components)
                component.Init(gameMgr, this);

            AddableUnitComp = GetComponent<IAddableUnit>();

            if (GameManager.MultiplayerGame && (Type != EntityTypes.building || !((Building)this).PlacementInstance)) //if this a multiplayer game:
                //and this is either not a building or a building that's not a placement instance
                MultiplayerKey = gameMgr.InputMgr.RegisterObject(this); //register the new entity

            //get the components that are attached to the entity:
            PlaneRenderer = plane.GetComponent<Renderer>(); //get the entity's plane renderer here

            gameObject.layer = 2; //set the layer to IgnoreRaycast as we don't want any raycast to recongize this.

            if (plane == null) //if the selection plane is not available.
                Debug.LogError("[Entity]: You must attach a plane object for the entity and assign it to 'Plane' in the inspector.");
            else
                plane.SetActive(false);

            if (selection == null) //invalid selection object
                Debug.LogError("[Entity]: The Selection component is missing.");
            else
                selection.Init(this.gameMgr, this);

            //V1
            selectionAudio.Cooldown.Init(this);

            EntityCreated(this); //trigger the custom event
        }

        protected virtual void Update()
        {
            if (isFlashActive == false)
                return;

            //selection plane flash timer:
            if (flashTimer > 0)
                flashTimer -= Time.deltaTime;
            else
                DisableSelectionFlash();
        }

        //a method to enable the selection flash
        public void EnableSelectionFlash(float duration, float repeatTime, Color color)
        {
            plane.GetComponent<Renderer>().material.color = color; //set the flashing color first
            InvokeRepeating("SelectionFlash", 0.0f, repeatTime);
            flashTimer = duration;
            isFlashActive = true;
        }

        //a method to disable the selection flash
        public void DisableSelectionFlash()
        {
            CancelInvoke("SelectionFlash");
            isFlashActive = false;

            if (selection.IsSelected) //if the entity was selected before the selection flash.
                EnableSelectionPlane(); //enable the selection plane with the correct colors again
            else
                plane.SetActive(false); //if not, just hide the selection plane.
        }

        //flashing entity selection
        public void SelectionFlash()
        {
            plane.SetActive(!plane.activeInHierarchy);
        }

        //enabling the entity's plane when selected:
        public void EnableSelectionPlane ()
        {
            plane.SetActive(true); //Activate the plane object where we will show the selection texture.

            Color nextColor = free ? gameMgr.SelectionMgr.GetFreeSelectionColor() : color; //get the color
            plane.GetComponent<Renderer>().material.color = new Color(nextColor.r, nextColor.g, nextColor.b, 0.5f); //update the plane's color
        }

        //disable the entity's selection plane:
        public void DisableSelectionPlane ()
        {
            plane.SetActive(false);
        }

        //Draw the resource's radius in blue
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
