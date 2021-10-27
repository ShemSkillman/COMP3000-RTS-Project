using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* Unit Group Selection script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(SelectionManager))]
    public class UnitGroupSelection : MonoBehaviour
    {
        //each GroupSelectionSlot instance presents a slot where a group of units can be stored.
        [System.Serializable]
        public struct GroupSelectionSlot
        {
            public KeyCode key;
            [HideInInspector]
            public List<Unit> units;
        }
        [SerializeField]
        private GroupSelectionSlot[] groupSelectionSlots = new GroupSelectionSlot[0]; //the array that holds all the group selection slots

        //when this key is pressed along with one of the group selection slots' key then the group selection slot will be set.
        [SerializeField]
        private KeyCode assignGroupKey = KeyCode.LeftShift;

        [SerializeField]
        private bool showUIMessages = true; //when enabled, each group assign/selection will show a UI message to the player

        [SerializeField, Tooltip("Audio clip to play when a selection group is assigned.")]
        private AudioClipFetcher assignGroupAudio = new AudioClipFetcher(); //played when a selection group slot is assigned
        [SerializeField, Tooltip("Audio clip to play when a selection group is activated (selected).")]
        private AudioClipFetcher selectGroupAudio = new AudioClipFetcher(); //played when a selection group slot is activated
        [SerializeField, Tooltip("Audio clip to play when a selection group is activated but it is empty.")]
        private AudioClipFetcher groupEmptyAudio = new AudioClipFetcher(); //played when the player attempts to activate the selection of a group slot but it happens to be empty

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        private void Update()
        {
            foreach(GroupSelectionSlot slot in groupSelectionSlots) //go through all the group selection slots
            {
                if(Input.GetKeyDown(slot.key)) //if the player presses both the slot specific key
                {
                    if(Input.GetKey(assignGroupKey)) //if the player presses the group slot assign key at the same time -> assign group
                    {
                        List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, true, true).Cast<Unit>().ToList(); //get selected units from player faction

                        if(selectedUnits.Count > 0) //make sure that there's at least one unit selected
                        {
                            //assign this new group to them
                            slot.units.Clear();
                            slot.units.AddRange(selectedUnits);

                            //play audio:
                            gameMgr.AudioMgr.PlaySFX(assignGroupAudio.Fetch(), false);

                            //inform player about assigning a new selection group:
                            if (showUIMessages)
                                ErrorMessageHandler.OnErrorMessage(ErrorMessage.unitGroupSet, null);
                        }
                    }
                    else //the assign group key hasn't been assigned -> select units in this slot if there are any
                    {
                        bool found = false; //determines whether there are actually units in the list
                        //it might be that the previously assigned units to this slot are all dead and therefore all slots are referencing null

                        int i = 0; //we'll be also clearing empty slots
                        while(i < slot.units.Count)
                        {
                            if (slot.units[i] == null) //if this element is invalid
                                slot.units.RemoveAt(i); //remove it
                            else
                            {
                                if (found == false) //first time encountering a valid
                                    gameMgr.SelectionMgr.Selected.RemoveAll(); //deselect the currently selected units.

                                gameMgr.SelectionMgr.Selected.Add(slot.units[i], SelectionTypes.multiple); //add unit to selection
                                found = true;
                            }

                            i++;
                        }

                        if(found == true) //making sure that there are valid units in the list that have been selected:
                        {
                            //play audio:
                            gameMgr.AudioMgr.PlaySFX(selectGroupAudio.Fetch(), false);

                            //inform player about selecting:
                            if (showUIMessages)
                                ErrorMessageHandler.OnErrorMessage(ErrorMessage.unitGroupSelected, null);
                        }
                        else //the list is either empty or all elements are invalid
                        {
                            //play audio:
                            gameMgr.AudioMgr.PlaySFX(groupEmptyAudio.Fetch(), false);

                            //inform player about the empty group:
                            if (showUIMessages)
                                ErrorMessageHandler.OnErrorMessage(ErrorMessage.unitGroupEmpty, null);
                        }
                    }
                }
            }
        }
    }
}
