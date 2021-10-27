using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    public class LobbyFaction : MonoBehaviour
    {
        private string factionName = "faction_name"; //holds the player's faction name.
        public string GetFactionName() { return factionName; }

        private int factionTypeID = 0; //holds the player's faction type ID
        public FactionTypeInfo GetFactionType () { return manager.GetCurrentMap().GetFactionTypeInfo(factionTypeID); }
        
        private int factionColorID = 0; //the color ID of the faction
        public int GetFactionColorID () { return factionColorID; }
        public Color GetFactionColor () { return manager.FactionColor.Get(factionColorID); }

        private int npcManagerID = 0;
        public NPCTypeInfo GetNPCType () { return manager.NPCTypes.Get(npcManagerID); }

        public bool PlayerControlled { private set; get; }

        //UI attributes:
        [SerializeField]
        private Image factionColorImage = null; //showing the faction's color
        [SerializeField]
        private InputField factionNameInput = null; //input/show the faction's name
        [SerializeField]
        private Dropdown factionTypeMenu = null; //UI Dropdown used to display the list of possible faction types that can be used in the currently selected maps.
        [SerializeField]
        private Dropdown npcTypeMenu = null; //UI Dropdown used to display the list of possible NPC manager components to pick for the NPC faction
        [SerializeField]
        private Button removeButton = null; //the button that the host can use to kick this player.

        //other components
        private LobbyManager manager;

        public void Init (LobbyManager manager, bool playerControlled)
        {
            this.manager = manager;

            //set default names
            factionName = "faction_name";
            factionNameInput.text = factionName;

            //set faction type options
            ResetFactionType();

            //set faction default color
            factionColorID = manager.LobbyFactions.Count > 1 ?
                manager.FactionColor.GetNextIndex(manager.LobbyFactions[manager.LobbyFactions.Count - 2].GetFactionColorID()) : 0;
            factionColorImage.color = this.manager.FactionColor.Get(factionColorID);

            //set default faction type:
            npcTypeMenu.ClearOptions();
            npcTypeMenu.AddOptions(this.manager.NPCTypes.GetNames());
            npcManagerID = 0;
            npcTypeMenu.value = npcManagerID;

            this.PlayerControlled = playerControlled; //is this faction player controlled?
            if(this.PlayerControlled) //if this is the local player's faction
            {
                npcTypeMenu.gameObject.SetActive(false); //hide the npc manager drop down menu
                removeButton.gameObject.SetActive(false); //can not remove this faction
            }
        }

        public void OnFactionNameUpdated ()
        {
            if (factionNameInput.text.Trim() == "") //invalid faction name
            {
                factionNameInput.text = factionName; //reset name
                return; //do not proceed
            }

            factionName = factionNameInput.text.Trim();
        }

        public void OnFactionTypeUpdated ()
        {
            factionTypeID = factionTypeMenu.value;
        }

        //reset the faction type drop down menu options depending on the map 
        public void ResetFactionType()
        {
            factionTypeMenu.ClearOptions(); //clear all the faction type options.
            factionTypeMenu.AddOptions(manager.GetMapFactionTypeNames()); //add the names of the faction types of the current map
            factionTypeID = 0;
            factionTypeMenu.value = factionTypeID;
        }

        //update the faction color when the player clicks on the faction color image
        public void OnFactionColorUpdated ()
        {
            factionColorID = manager.FactionColor.GetNextIndex(factionColorID);
            factionColorImage.color = manager.FactionColor.Get(factionColorID);
        }

        //update the faction npc manager
        public void OnFactionNPCTypeUpdated ()
        {
            npcManagerID = npcTypeMenu.value;
        }

        //remove the faction from the lobby
        public void Remove ()
        {
            manager.RemoveFaction(this);
        }
    }
}
