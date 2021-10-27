using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSEngine
{
    public abstract class SelectionEntity : MonoBehaviour
    {
        public Entity Source { private set; get; } //the entity for whome this component controls selection
        public FactionEntity FactionEntity { set; get; } //faction entity (in case this selection entity is tied to a unit/building)

        [SerializeField]
        protected bool canSelect = true; //if this is set to false then the unit/building/resource will not be selectable
        [SerializeField]
        protected bool selectOwnerOnly = false; //if this is set to true then only the local player can select the object associated to this.
        public bool IsSelected { private set; get; } //is this entity currently selected?
        public bool IsSelectedOnly { set; get; } //is this is the only entity that is currently selected? this is decided by the selection manager

        [SerializeField]
        private float minimapIconSize = 0.5f; //the size of the selection main object icon in the minimap
        public float GetMinimapIconSize () { return minimapIconSize; }

        public MinimapIcon Icon { private set; get; } //reference to the minimap's icon of this selection entity

        protected GameManager gameMgr;

        public virtual void Init(GameManager gameMgr, Entity source)
        {
            this.gameMgr = gameMgr;
            this.Source = source; //assign the source

            gameObject.layer = 0; //setting it to the default layer because raycasting ignores building and resource layers.

            //in order for collision detection to work, we must assign the following settings to the collider and rigidbody.
            GetComponent<Collider>().isTrigger = true;
            GetComponent<Collider>().enabled = true;

            if (GetComponent<Rigidbody>() == null)
                gameObject.AddComponent<Rigidbody>();
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;

            IsSelected = false; //mark as not selected by default
            IsSelectedOnly = false;
        }

        public void UpdateMinimapIcon (MinimapIcon newIcon) { Icon = newIcon; }
        public void UpdateMinimapIconColor () //a method that updates the minimap icon color
        {
            if (Icon == null)//invalid icon assigned? 
                return;

            Color iconColor = Color.white;
            if (IsFree()) //if the entity is a free unit/building
                iconColor = Source.Type == EntityTypes.unit ? gameMgr.UnitMgr.GetFreeUnitColor() : gameMgr.BuildingMgr.GetFreeBuildingColor();
            else
                iconColor = Source.GetColor();

            Icon.SetColor(iconColor);
        }
        public void ToggleMinimapIcon(bool show) { Icon?.gameObject.SetActive(show); }
        public void DisableMinimapIcon() //a method that disables the minimap icon
        {
            Icon?.GetComponent<EffectObj>().Disable();
            Icon = null;
        }

        //use this method to change the selection status of this entity
        public void ToggleSelection(bool enable, bool ownerOnly)
        {
            canSelect = enable;
            selectOwnerOnly = ownerOnly;
        }

        //check if this entity can be selected
        public bool CanSelect ()
        {
            //if this can't be selected or it's tied to a faction entity where only owner can select and this is not the local player's entity
            return (canSelect == true
                && (FactionEntity == null
                    || (FactionEntity.EntityHealthComp.IsDead() == false
                        && (selectOwnerOnly == false || FactionEntity.FactionID == GameManager.PlayerFactionID))));
        }

        //a method called by the Selection Manager to attempt to select the object associated with this selection entity
        public virtual void OnSelected()
        {
            if(FactionEntity) //if this is a faction entity
                FactionEntity.OnMouseClick(); //trigger a mouse click in the faction entity's main component

            gameMgr.AudioMgr.PlaySFX(Source.SelectionAudio, false); //play the selection audio
            Source.EnableSelectionPlane(); //enable the selection plane of the source

            IsSelected = true;
            CustomEvents.OnEntitySelected(Source);
        }

        //deselect this entity
        public virtual void OnDeselected ()
        {
            if (IsSelected) //if the entity was previously selected
                CustomEvents.OnEntityDeselected(Source);

            Source.DisableSelectionPlane();
            IsSelected = false;
            IsSelectedOnly = false;
        }

        public bool IsFree() //is the entity managed by this selection component a faction entity or a free one?
        {
            if (FactionEntity != null)
                return FactionEntity.IsFree();
            return false;
        }

        //called when the mouse is over this selection entity's collider
        void OnMouseEnter()
        {
            //if the hover health bar feature is enabled (for faction entities only)
            if (FactionEntity != null && FactionEntity.EntityHealthComp.IsDead() == false
                && !EventSystem.current.IsPointerOverGameObject() 
                && !gameMgr.PlacementMgr.IsBuilding()) //as long as the player is not clicking on a UI object and is not placing a building
            {
                CustomEvents.OnFactionEntityMouseEnter(FactionEntity); //trigger custom event
            }
        }

        //if the mouse leaves this collider
        void OnMouseExit()
        {
            CustomEvents.OnFactionEntityMouseExit(FactionEntity); //trigger custom event
        }

        //action on selection entity:
        public abstract void OnAction(TaskTypes taskType);
    }
}
