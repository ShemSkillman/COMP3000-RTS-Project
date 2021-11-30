using System.Collections.Generic;
using UnityEngine;
#if RTSENGINE_FOW
using FoW;
#endif

using RTSEngine.EntityComponent;

namespace RTSEngine
{
    public abstract class FactionEntity : Entity
    {
        [SerializeField]
        protected int factionID = 0; //the faction ID that this entity belongs to.
        public int FactionID { set { factionID = value; } get { return factionID; } }
        public FactionManager FactionMgr { set; get; } //the faction manager that this entity belongs to.

        [System.Serializable]
        public struct ColoredRenderer
        {
            public Renderer renderer;
            public int materialID;

            //a method that updates the renderer's material color
            
        }

        protected virtual void UpdateRendererColor(ColoredRenderer cRenderer, FactionColor fColor)
        {
            cRenderer.renderer.materials[cRenderer.materialID].color = fColor.color;
        }

        [SerializeField]
        private ColoredRenderer[] coloredRenderers = new ColoredRenderer[0]; //The materials of the assigned Renderer components in this array will be colored by the faction entity's faction color

        //double clicking on the unit allows to select all units of the same type within a certain range
        private float doubleClickTimer;
        private bool clickedOnce = false;

        //resources to add when the faction entity is added or removed
        [SerializeField, Tooltip("Resources added/removed to/from the owner faction when the faction entity is successfully initialized.")]
        protected ResourceInput[] initResources = new ResourceInput[0];
        [SerializeField, Tooltip("Resources added/removed to/from the owner faction when the faction entity is disabled (disabled (destroyed/converted)")]
        private ResourceInput[] disableResources = new ResourceInput[0];

        //faction entity components:
        public TaskLauncher TaskLauncherComp { private set; get; }
        public APC APCComp { private set; get; }

        protected AttackEntity[] allAttackComp = new AttackEntity[0];
        /// <summary>
        /// Gets all AttackEntity instances attached to the faction entity.
        /// </summary>
        public IEnumerable<AttackEntity> AllAttackComp { get { return allAttackComp; } }

        public MultipleAttackManager MultipleAttackMgr { private set; get; }
        public FactionEntityHealth EntityHealthComp { private set; get; }

#if RTSENGINE_FOW
        public FogOfWarUnit FoWUnit { private set; get; }
#endif

        public abstract void UpdateAttackComp(AttackEntity attackEntity);

        //initialize the faction entity
        protected virtual void Init(GameManager gameMgr, int fID, bool free)
        {
            base.Init(gameMgr);

            //get the components that are attached to the faction entity:
            TaskLauncherComp = GetComponent<TaskLauncher>();
            APCComp = GetComponent<APC>();
            allAttackComp = GetComponents<AttackEntity>();
            MultipleAttackMgr = GetComponent<MultipleAttackManager>();
            EntityHealthComp = GetComponent<FactionEntityHealth>();

            //initialize these components
            //task launcher must be initialized separately on units and buildings
            if (APCComp)
                APCComp.Init(gameMgr, this);
            EntityHealthComp.Init(gameMgr, this);

#if RTSENGINE_FOW
            FoWUnit = GetComponent<FogOfWarUnit>();
#endif

            selection.FactionEntity = this; //assign as the selection's source faction entity

            //initial settings for the double click
            clickedOnce = false;
            doubleClickTimer = 0.0f;

            this.free = free;
            FactionMgr = gameMgr.GetFaction(factionID).FactionMgr; //get the faction manager

            if (this.free == false) //if the entity belongs to a faction
            {
                factionID = fID; //set the faction ID.
                UpdateFactionColors(gameMgr.GetFaction(factionID).GetColor()); //update the faction colors on the unit
            }
            else
            {
                UpdateFactionColors(gameMgr.BuildingMgr.GetFreeBuildingColor());
                factionID = -1;
            }
        }

        public void SetFaction(int fID)
        {
            if (fID < 0)
            {
                this.free = true;
            }
            else
            {
                this.free = false;
            }

            factionID = fID; //set the faction ID.
            UpdateFactionColors(GetFactionColor(factionID)); //update the faction colors on the unit
        }

        public FactionColor GetFactionColor(int factionId)
        {
            if (factionId < 0)
            {
                return gameMgr.BuildingMgr.GetFreeBuildingColor();
            }
            else
            {
                return gameMgr.GetFaction(factionId).GetColor();
            }
            
        }

        //method called to set a faction entity's faction colors:
        protected void UpdateFactionColors(FactionColor newColor)
        {
            color = newColor; //set the faction color

            foreach (ColoredRenderer cr in coloredRenderers) //go through all renderers that can be colored
                UpdateRendererColor(cr, color);
        }

        protected override void Update()
        {
            base.Update();

            //double click timer:
            if (clickedOnce == true)
            {
                if (doubleClickTimer > 0)
                    doubleClickTimer -= Time.deltaTime;
                if (doubleClickTimer <= 0)
                    clickedOnce = false;
            }
        }

        //a method that is called when a mouse click on this unit is detected
        public virtual void OnMouseClick()
        {
            if (clickedOnce == false)
            { //if the player hasn't clicked on this portal shortly before this click
                DisableSelectionFlash(); //disable the selection flash

                if (gameMgr.SelectionMgr.MultipleSelectionKeyDown == false) //if the player doesn't have the multiple selection key down (not looking to select multiple units one by one)
                { 
                    //start the double click timer
                    doubleClickTimer = 0.5f;
                    clickedOnce = true;
                }
            }
            else //if this is the second click (double click), select all units of the same type within a certain range
                gameMgr.SelectionMgr.SelectFactionEntitiesInRange(this);
        }

        /// <summary>
        /// Disables the FactionEntity instance to prepare for it to be destroyed, upgraded or converted.
        /// </summary>
        /// <param name="destroyed">True if the faction entity is being disabled because it's getting destroyed, otherwise false.</param>
        public virtual void Disable(bool destroyed)
        {
            if(TaskLauncherComp)
                TaskLauncherComp.Disable(); //cancel all the in progress tasks

            if(APCComp)
                APCComp.EjectAll(destroyed); //remove all the units stored in the APC

            if(!free)
                gameMgr.ResourceMgr.UpdateResource(factionID, disableResources); //add the disable resources to the entity's faction.
        }
    }
}
