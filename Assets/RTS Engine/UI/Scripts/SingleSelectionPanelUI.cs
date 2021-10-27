using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    [System.Serializable]
    public class SingleSelectionPanelUI
    {
        [SerializeField]
        private GameObject panel = null; //the single selection menu, parent object of the following UI objects:
        [SerializeField]
        private Text nameText = null; //name of the selected entity will be displayed here.
        [SerializeField]
        public Text descriptionText = null; //description of the selected entity will be displayed here.
        [SerializeField]
        private bool showPopulationSlots = true; //when enabled, the population slots of the entity will be displayed in the desription
        [SerializeField]
        private Image icon = null; //displays the icon of the selected entity

        [SerializeField]
        private Text healthText = null; //displays the health of the selected entity.
        //health bar fields for the selected entity
        [SerializeField]
        private ProgressBarUI healthBar = new ProgressBarUI();

        //initilaize the task panel UI component
        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            healthBar.Init(); //initiliase the health bar

            //custom events to detect changes regarding the single selected entities:
            CustomEvents.BuildingBuilt += OnBuildingBuilt;
            CustomEvents.UnitStartBuilding += OnBuilderStatusUpdated;
            CustomEvents.UnitStopBuilding += OnBuilderStatusUpdated;

            CustomEvents.ResourceAmountUpdated += OnResourceAmountUpdated;
            CustomEvents.UnitStartCollecting += OnCollectorStatusUpdated;
            CustomEvents.UnitStopCollecting += OnCollectorStatusUpdated;

            CustomEvents.APCAddUnit += OnAPCUpdated;
            CustomEvents.APCRemoveUnit += OnAPCUpdated;
        }

        //called to disable this component
        public void Disable ()
        {
            //stop listening to the custom events:
            CustomEvents.BuildingBuilt -= OnBuildingBuilt;
            CustomEvents.UnitStartBuilding -= OnBuilderStatusUpdated;
            CustomEvents.UnitStopBuilding -= OnBuilderStatusUpdated;

            CustomEvents.ResourceAmountUpdated -= OnResourceAmountUpdated;
            CustomEvents.UnitStartCollecting -= OnCollectorStatusUpdated;
            CustomEvents.UnitStopCollecting -= OnCollectorStatusUpdated;

            CustomEvents.APCAddUnit -= OnAPCUpdated;
            CustomEvents.APCRemoveUnit -= OnAPCUpdated;
        }


        //building related events:

        //called each time a building is built:
        private void OnBuildingBuilt (Building building)
        {
            if (building.GetSelection().IsSelectedOnly) //if the building is the only entity selected
                UpdateBuildingUI(building); //reload the single selection panel
        }

        //called each time a unit starts/stop constructing a building
        private void OnBuilderStatusUpdated (Unit unit, Building targetBuilding)
        {
            if (targetBuilding.GetSelection().IsSelectedOnly) //if the target building is the only entity selected
                UpdateBuildingUI(targetBuilding); //reload the single selection panel
        }

        //resource related events:
        
        //called each time a resource's amount is changed:
        private void OnResourceAmountUpdated (Resource resource)
        {
            if (resource.GetSelection().IsSelectedOnly) //if the resource is selected
                UpdateResourceUI(resource); //reload the single selection panel
        }

        //called each time a unit starts/stop collecting a resource
        private void OnCollectorStatusUpdated (Unit unit, Resource targetResource)
        {
            if (targetResource.GetSelection().IsSelectedOnly) //if the target resource is selected
                UpdateResourceUI(targetResource); //reload the single selection panel
        }

        //called each time a unit is added/removed to/from an APC
        private void OnAPCUpdated (APC apc, Unit unit)
        {
            //show APC tasks only if the apc is the only entity selected
            if (apc.FactionEntity.GetSelection().IsSelectedOnly)
            {
                if (apc.FactionEntity.Type == EntityTypes.unit)
                    UpdateUnitUI((Unit)apc.FactionEntity);
                else
                    UpdateBuildingUI((Building)apc.FactionEntity);
            }
                
        }

        public void Hide () //disable the single selection menu:
        {
            panel.SetActive(false);
        }

        //Display the input faction entity info:
        private void UpdateEntityUI (string name, string description, Sprite sprite)
        {
            gameMgr.UIMgr.HideMultipleSelectionPanel(); //hide the multiple selection panel
            panel.SetActive(true); //activate the single selection panel

            if (nameText) //show the name of the selected entity
            {
                nameText.gameObject.SetActive(true);
                nameText.text = name;
            }

            if(descriptionText) //show the description of the selected entity
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = description;
            }

            if(icon) //show the selected entity sprite
            {
                icon.gameObject.SetActive(true);
                icon.sprite = sprite;
            }
        }

		//show the input building UI info
		public void UpdateBuildingUI (Building building)
		{
            string factionTypeName = (building.IsFree() == false && gameMgr.GetFaction(building.FactionID).GetTypeInfo() != null)
                ? gameMgr.GetFaction(building.FactionID).GetTypeInfo().GetName() : "";

            string description = building.GetDescription() + (building.WorkerMgr.currWorkers > 0 ? building.WorkerMgr.currWorkers.ToString() + "/" + building.WorkerMgr.GetAvailableSlots() : "");

            if (building.APCComp) { //show the capacity in case it's an APC
                description += "\nCapacity: " + building.APCComp.GetCount().ToString() + "/" + building.APCComp.GetCapacity().ToString();
            }

            UpdateEntityUI(
                building.GetName() + " (" + (building.IsFree() == true ? "Free Building" : (factionTypeName + " - " + gameMgr.GetFaction(building.FactionID).GetName())) + ")",
                description,
                building.GetIcon()
                );

            //update the building's health:
            UpdateFactionEntityHealthUI (building);
		}

		//show the input resource UI info
		public void UpdateResourceUI (Resource resource)
		{
            DisableFactionEntityHealthUI();

            UpdateEntityUI(
                resource.GetName(),
                resource.GetDescription()
                    + (resource.ShowCollectors() ? "\nCollectors: " + resource.WorkerMgr.currWorkers.ToString() + "/" + resource.WorkerMgr.GetAvailableSlots().ToString() : ""),
                resource.GetResourceType().GetIcon()
                );

            if (healthText && resource.ShowAmount()) //show the resource amount
            {
                healthText.gameObject.SetActive(true);
                healthText.text = resource.IsInfinite() == true ? "Infinite Amount" : "Amount " + resource.GetAmount();
            }
		}

		//update the input unit UI info
		public void UpdateUnitUI (Unit unit)
		{
            string factionTypeName = (unit.IsFree() == false && gameMgr.GetFaction(unit.FactionID).GetTypeInfo() != null) 
                ? gameMgr.GetFaction(unit.FactionID).GetTypeInfo().GetName() : "";

            string description = unit.GetDescription(); //get the unit description
            if (unit.APCComp) { //show the capacity in case it's an APC
                description += "\nCapacity: " + unit.APCComp.GetCount().ToString() + "/" + unit.APCComp.GetCapacity().ToString();
            }
            if(showPopulationSlots == true)
                description += "\n<b>Population Slots:</b> " + unit.GetPopulationSlots().ToString();

            UpdateEntityUI(
                unit.GetName() + " (" + (unit.IsFree() == true ? "Free Unit" : (factionTypeName + " - " + gameMgr.GetFaction(unit.FactionID).GetName())) + ")",
                description,
                unit.GetIcon()
                );

            //update the unit's health:
            UpdateFactionEntityHealthUI (unit);
		}

        //hides the health related UI elements:
        private void DisableFactionEntityHealthUI ()
        {
            if (healthText)
                healthText.gameObject.SetActive(false);
            healthBar.Toggle(false);
        }

        //updates the select faction entity (unit/building) health bar:
        public void UpdateFactionEntityHealthUI(FactionEntity factionEntity)
        {
            if(healthText) //show the faction entity health:
            {
                healthText.gameObject.SetActive(true);
                healthText.text = factionEntity.EntityHealthComp.CurrHealth.ToString() + "/" + factionEntity.EntityHealthComp.MaxHealth.ToString();
            }

            //health bar:
            healthBar.Toggle(true);

            //Update the health bar:
            healthBar.Update(factionEntity.EntityHealthComp.CurrHealth / (float)factionEntity.EntityHealthComp.MaxHealth);
        }

    }
}
