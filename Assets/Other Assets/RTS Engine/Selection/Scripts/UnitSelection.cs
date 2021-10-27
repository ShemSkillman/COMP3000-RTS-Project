using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RTSEngine
{
    public class UnitSelection : SelectionEntity
    {
        Unit unit; //the unit component attached to this selection entity.

        public override void Init(GameManager gameMgr, Entity source)
        {
            base.Init(gameMgr, source);

            FactionEntity = (FactionEntity)source;
            unit = (Unit)source;
        }

        public override void OnSelected()
        {
            base.OnSelected();

            CustomEvents.OnUnitSelected(unit); //trigger custom event
        }

        //deselect the unit if it's selected:
        public override void OnDeselected()
        {
            if(IsSelected == true) //if the unit was selected
                CustomEvents.OnUnitDeselected(unit); //trigger custom event

            base.OnDeselected();
        }

        //action on unit:
        public override void OnAction(TaskTypes taskType)
        {
            List<Building> selectedBuildings = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.building, false, true).Cast<Building>().ToList(); //get a list of the selected buildings
            List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, false, true).Cast<Unit>().ToList(); //get the list of selected units

            if (unit.HealthComp.IsDead() == true || (selectedBuildings.Count == 0 && selectedUnits.Count == 0)) //if the building is marked as dead or no units are selected
                return;

            AudioClip audioClip = null; //audio clip to play
            bool flashSelection = false; //flash selection?
            bool isFriendlyFlash = true; //flash friendly selection?

            ErrorMessage lastErrorMessage = ErrorMessage.none;

            if (selectedBuildings.Count > 0 && unit.FactionID != selectedBuildings[0].FactionID) //if there are buildings selected and this unit doesn't belong to player's faction
            {
                foreach (Building building in selectedBuildings) //go through player selected buildings
                {
                    if(building.AttackComp && (taskType == TaskTypes.none || taskType == TaskTypes.attack)) //if the building has an attack component
                    {
                        building.AttackComp.SetTarget(unit, unit.transform.position); //assign target
                        flashSelection = true;
                        isFriendlyFlash = false;
                        audioClip = building.AttackComp.GetOrderAudio(); //assign audio clip
                    }
                }
            }

            if (selectedUnits.Count > 0) //units selected
            {
                foreach (Unit selectedUnit in selectedUnits)
                {
                    //converting target unit
                    if (selectedUnit.ConverterComp && (taskType == TaskTypes.none || taskType == TaskTypes.convert))
                    {
                        lastErrorMessage = selectedUnit.ConverterComp.SetTarget(unit);
                        if (lastErrorMessage == ErrorMessage.none)
                        {
                            flashSelection = true;
                            audioClip = selectedUnit.ConverterComp.GetOrderAudio();
                            continue;
                        }
                    }

                    //moving to APC
                    if (unit.APCComp && (taskType == TaskTypes.none || taskType == TaskTypes.movement))
                    {
                        lastErrorMessage = unit.APCComp.Move(selectedUnit, true);
                        if (lastErrorMessage == ErrorMessage.none)
                        {
                            flashSelection = true;
                            continue;
                        }
                    }

                    //healing unit
                    if (selectedUnit.HealerComp && (taskType == TaskTypes.none || taskType == TaskTypes.heal))
                    {
                        lastErrorMessage = selectedUnit.HealerComp.SetTarget(unit);
                        if (lastErrorMessage == ErrorMessage.none)
                        {
                            flashSelection = true;
                            audioClip = selectedUnit.HealerComp.GetOrderAudio();
                        }
                        continue;
                    }
                }

                //attack action, only if unit belongs to other factions
                if (unit.FactionID != selectedUnits[0].FactionID)
                {
                    gameMgr.AttackMgr.LaunchAttack(selectedUnits, unit, transform.position, true);
                    flashSelection = true;
                    isFriendlyFlash = false;
                }
            }

            gameMgr.AudioMgr.PlaySFX(audioClip, false);
            if (flashSelection) //flashing a selection means that at least one of the units in the list has been assigned a task
                gameMgr.SelectionMgr.FlashSelection(unit, isFriendlyFlash);
            else //selection not flashing means that no unit has been assigned a task
            {
                ErrorMessageHandler.OnErrorMessage(lastErrorMessage, Source);
                //show error message.
                //gameMgr.SelectionMgr.FlashSelection(unit, false);
            }


        }

    }
}
