using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RTSEngine
{
    public class ResourceSelection : SelectionEntity
    {
        Resource resource; //the resource component attached to this selection entity.

        public override void Init(GameManager gameMgr, Entity source)
        {
            base.Init(gameMgr, source);

            FactionEntity = null;
            resource = (Resource)source;
        }

        public override void OnSelected()
        {
            base.OnSelected();

            CustomEvents.OnResourceSelected(resource); //trigger custom event
        }

        //deselect the resource if it's selected:
        public override void OnDeselected()
        {
            if(IsSelected == true) //if the resource was selected
                CustomEvents.OnResourceDeselected(resource); //trigger custom event

            base.OnDeselected();
        }

        //action on resource:
        public override void OnAction(TaskTypes taskType)
        {
            List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, false, true).Cast<Unit>().ToList(); //get the list of selected units
            if (resource.IsEmpty() == true || selectedUnits.Count == 0) //if the resource is marked as empty or no units are selected
                return;

            AudioClip audioClip = null; //audio clip to play
            bool flashSelection = false; //flash selection?

            ErrorMessage lastErrorMessage = ErrorMessage.none;

            foreach(Unit unit in selectedUnits)
            {
                //collecting resource
                if (unit.CollectorComp && (taskType == TaskTypes.none || taskType == TaskTypes.build))
                {
                    lastErrorMessage = unit.CollectorComp.SetTarget(resource);
                    if (lastErrorMessage == ErrorMessage.none)
                    {
                        flashSelection = true;
                        audioClip = unit.CollectorComp.GetOrderAudio();
                    }
                    continue;
                }
            }

            gameMgr.AudioMgr.PlaySFX(audioClip, false);
            if (flashSelection) //flashing a selection means that at least one of the units in the list has been assigned a task
                gameMgr.SelectionMgr.FlashSelection(resource, true);
            else //selection not flashing means that no unit has been assigned a task
            {
                ErrorMessageHandler.OnErrorMessage(lastErrorMessage, Source);
                //show error message.
                //gameMgr.SelectionMgr.FlashSelection(resource, false);
            }
        }

    }
}