using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

/* Resource script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Resource : Entity
    {
        public override EntityTypes Type { get { return EntityTypes.resource; } }

        [Header("General"), SerializeField]
        private ResourceTypeInfo resourceType = null; //the resoruce type that defines this resource object
        public ResourceTypeInfo GetResourceType() { return resourceType; }

        public int ID { private set; get; } //the resource ID of this resource type in the resource manager.

        [SerializeField]
        private bool infiniteAmount = false; //when true, the resource will never get empty
        public bool IsInfinite() { return infiniteAmount; }
        [SerializeField]
        private int amount = 1000; //amount that's available to be collected
        public int GetAmount() { return amount; }
        public bool IsEmpty() { return amount <= 0 && infiniteAmount == false; }

        [SerializeField]
        private bool destroyOnEmpty = true; //destroy the resource object once the amount hits zero ?

        [SerializeField] private float destroyTime = 30f;

        public int FactionID { set; get; } //the ID of the faction that includes this resource object inside its territory

        [SerializeField]
        private GameObject secondaryModel = null; //when assigned, this model is enabled as soon as a unit starts exploiting this resource. the initial model will be then hidden.
        [SerializeField]
        private ResourceSelection secondarySelection = null; //if assigned, it replaces the selection field when the secondaryModel gameobject is enabled

        private bool collected = false;

        [Header("Collection & Drop Off"), SerializeField]
        private bool canCollectOutsideBorder = false; //can the player collect this resource outside the borders?
        public bool CanCollectOutsideBorder() { return canCollectOutsideBorder; }

        [SerializeField]
        private float collectOneUnitDuration = 1.5f; //how much time is needed to collect 1 from the resource amount.
        public float GetCollectOneUnitDuration() { return collectOneUnitDuration; }
        public void UpdateCollectOneUnitDuration(float value) { collectOneUnitDuration += value; }

        [Header("UI")]
        [SerializeField]
        private bool showCollectors = true; //show the current collectors in the UI.
        public bool ShowCollectors() { return showCollectors; }
        [SerializeField]
        private bool showAmount = true; //show the amount in the UI.
        public bool ShowAmount() { return showAmount; }

        public Treasure TreasureComp { private set; get; } //the treasure component attached to this resource (if there's any)
        public WorkerManager WorkerMgr { set; get; } //this component manages resource collectors

        public override void Init(GameManager gameMgr)
        {
            base.Init(gameMgr);

            //get the resource components
            TreasureComp = GetComponent<Treasure>();
            WorkerMgr = GetComponent<WorkerManager>();

            //initialize the components
            WorkerMgr.Init();

            FactionID = -1; //set faction ID to -1 by default.

            plane.gameObject.SetActive(false); //hide the resource's plane initially

            ID = gameMgr.ResourceMgr.GetResourceID(resourceType.Key); //get the resource ID from the resource manager
            color = new FactionColor(resourceType.GetMinimapIconColor(), null, null); //assign the resource's color

            if (secondaryModel != null) //if there's a secondary model, enable it and disable the initial one
            {
                secondaryModel.SetActive(false);

                if (secondarySelection != null && secondarySelection != selection) //if there's secondary selection
                {
                    secondarySelection.gameObject.SetActive(false); //and hide it
                    secondarySelection.Init(this.gameMgr, this); //assign the same source for the secondary selection component
                }
            }

            collectOneUnitDuration /= gameMgr.GetSpeedModifier(); //set collection time regarding speed modifier

            ToggleModel(true); //show the initial model
            gameMgr.MinimapIconMgr?.Assign(selection); //assign the minimap icon

            gameMgr.ResourceMgr.AddResource(this); //register the resource in the resource manager

            CanRunComponents = true; //the resource can now run its entity components

            CustomEvents.OnResourceAdded(this); //trigger the resource added custom event
        }
        //add/remove to/from the resource
        public void AddAmount(int value, Unit source)
        {
            if (GameManager.MultiplayerGame == false) //if this is a single player game -> go ahead directly
                AddAmountLocal(value, source);
            else if (RTSHelper.IsLocalPlayer(source)) //multiplayer game and the resource collector belongs to the local faction
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.resource,
                    targetMode = (byte)InputMode.health,
                    value = value
                };
                InputManager.SendInput(newInput, this, source);
            }
        }

        //add/remove to/from the resource locally
        public void AddAmountLocal(int value, Unit source)
        {
            if (collected == false) //if the resource hasn't been collected before now
            {
                collected = true;
            }

            if (infiniteAmount == false) //only change the amount if the resource doesn't have infinite amount
            {
                amount += Mathf.FloorToInt(value);
                CustomEvents.OnResourceAmountUpdated(this); //trigger custom event
            }

            if (gameMgr.ResourceMgr.CanAutoCollect()) //if resources are automatically collected
                gameMgr.ResourceMgr.UpdateResource(source.FactionID, resourceType.Key, -value); //then add the resource to the faction.
            else //resources need to dropped off at a building
                source.CollectorComp.UpdateDropOffResources(ID, -value);

            if (amount <= 0 && infiniteAmount == false) //if the resource is empty
                DestroyResource(source);
        }

        //a method called when the resource object is to be destroyed
        public void DestroyResource(Unit source)
        {
            if (GameManager.MultiplayerGame == false) //if this is a single player game -> go ahead directly
                DestroyResourceLocal(source);
            else if (RTSHelper.IsLocalPlayer(source)) //multiplayer game and the resource collector belongs to the local faction
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.destroy,
                    targetMode = (byte)InputMode.resource,
                };
                InputManager.SendInput(newInput, this, source);
            }
        }

        //a method called when the resource object is to be locally destroyed
        public void DestroyResourceLocal(Unit source)
        {
            amount = 0; //empty the resource
            CustomEvents.OnResourceEmpty(this); //trigger custom event

            if (secondaryModel != null) //if there's a secondary model, enable it and disable the initial one
            {
                ToggleModel(false); //hide the primary model
                secondaryModel.SetActive(true);

                selection.DisableMinimapIcon(); //disable the first minimap icon
                selection.gameObject.SetActive(false); //disable the selection object
            }

            if (destroyOnEmpty == false) //if the resource is not supposed to be destroyed
                return;

            if (TreasureComp) //if this has a treasure component
                TreasureComp.Trigger(source.FactionID, gameMgr); //trigger the treasure for the collector's faction

            gameMgr.ResourceMgr.RemoveResource(this); //remove resource from all resources list

            //gameMgr.SelectionMgr.Selected.Remove(this); //in case the resource was selected, deselect it
            selection.DisableMinimapIcon(); //remove resource's minimap icon from the minimap

            Destroy(gameObject, destroyTime);

            CustomEvents.OnResourceDestroyed(this);
        }
    }
}