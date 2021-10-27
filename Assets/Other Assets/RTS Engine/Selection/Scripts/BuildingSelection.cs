using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RTSEngine
{
    public class BuildingSelection : SelectionEntity
    {
        Building building; //the building component attached to this selection entity.

        public override void Init(GameManager gameMgr, Entity source)
        {
            base.Init(gameMgr, source);

            FactionEntity = (FactionEntity)source;
            building = (Building)source;
        }

        public override void OnSelected()
        {
            base.OnSelected();

            //If the building has been already placed and built and has goto position that is currently its rally point
            if (building.FactionID == GameManager.PlayerFactionID && //building belongs to local player
                building.Placed && building.IsBuilt 
                && building.GotoPosition != null && building.RallyPoint == building.GotoPosition)
                building.GotoPosition.gameObject.SetActive(true);

            CustomEvents.OnBuildingSelected(building); //trigger custom event
        }

        //deselect the building if it's selected:
        public override void OnDeselected()
        {
            if(IsSelected == true) //if the building was selected
                CustomEvents.OnBuildingDeselected(building); //trigger custom event

            if (building.GotoPosition)
                building.GotoPosition.gameObject.SetActive(false);

            base.OnDeselected();
        }

        //action on building:
        public override void OnAction(TaskTypes taskType)
        {
            List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, false, true).Cast<Unit>().ToList(); //get the list of selected units
            if (building.HealthComp.IsDead() == true || selectedUnits.Count == 0) //if the building is marked as dead
                return;

            AudioClip audioClip = null; //audio clip to play
            bool flashSelection = false; //flash selection?
            bool isFriendlyFlash = true; //flash friendly selection?

            ErrorMessage lastErrorMessage = ErrorMessage.none;

            foreach(Unit unit in selectedUnits)
            {
                //moving to portal
                if (building.PortalComp && (taskType == TaskTypes.none || taskType == TaskTypes.movement))
                {
                    lastErrorMessage = building.PortalComp.Move(unit, true);
                    if (lastErrorMessage == ErrorMessage.none)
                    {
                        flashSelection = true;
                        continue;
                    }
                }

                //moving to APC
                if (building.APCComp && (taskType == TaskTypes.none || taskType == TaskTypes.movement))
                {
                    lastErrorMessage = building.APCComp.Move(unit, true);
                    if (lastErrorMessage == ErrorMessage.none)
                    {
                        flashSelection = true;
                        continue;
                    }
                }
                //constructing building
                if (unit.BuilderComp && (taskType == TaskTypes.none || taskType == TaskTypes.build))
                {
                    lastErrorMessage = unit.BuilderComp.SetTarget(building);
                    if (lastErrorMessage == ErrorMessage.none)
                    {
                        flashSelection = true;
                        audioClip = unit.BuilderComp.GetOrderAudio();
                    }
                    continue;
                }
            }

            //attack action, only if building belongs to other factions and it can be attacked
            if(building.FactionID != selectedUnits[0].FactionID && building.HealthComp.CanBeAttacked)
            {
                gameMgr.AttackMgr.LaunchAttack(selectedUnits, building, transform.position, true);
                flashSelection = true;
                isFriendlyFlash = false;
            }

            gameMgr.AudioMgr.PlaySFX(audioClip, false);
            if (flashSelection) //flashing a selection means that at least one of the units in the list has been assigned a task
                gameMgr.SelectionMgr.FlashSelection(building, isFriendlyFlash);
            else //selection not flashing means that no unit has been assigned a task
            {
                ErrorMessageHandler.OnErrorMessage(lastErrorMessage, Source);
                //show error message.
                //gameMgr.SelectionMgr.FlashSelection(building, false);
            }
        }
    }
}
