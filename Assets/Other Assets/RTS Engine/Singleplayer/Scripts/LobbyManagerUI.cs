using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RTSEngine
{
    public class LobbyManagerUI : MonoBehaviour
    {
        [SerializeField]
        private string mainMenuScene = ""; //the name of the scene that the player gets back to when leaving the singleplayer menu

        [SerializeField]
        private GridLayoutGroup lobbyFactionParent = null; //the parent object of all the faction lobby objects.

        public void SetLobbyFactionParent(Transform lobbyFaction)
        {
            lobbyFaction.SetParent(lobbyFactionParent.transform, false);
            lobbyFaction.localScale = Vector3.one;
        }

        [SerializeField]
        private Dropdown mapDropDownMenu = null; //the menu that allows the host to select the map.
        public int GetMapDropDownValue () { return mapDropDownMenu.value; }

        public void UpdateMapDropDownMenu(List<string> mapNames) //update the map drop down menu with the available map names
        {
            mapDropDownMenu.ClearOptions();
            mapDropDownMenu.AddOptions(mapNames);

            mapDropDownMenu.value = 0;
            UpdateMapUIInfo();
        }

        [SerializeField]
        private Text mapInitialPopulationText = null; //The UI text to show the map's initial population.
        [SerializeField]
        private Text mapDescriptionText = null; //The UI text to show the selected map's description.
        [SerializeField]
        private Text mapMaxFactionsText = null; //The UI text to show the map's maximum faction.

        //a method that updates the selected map's UI
        public void UpdateMapUIInfo()
        {
            //show the map's info: population, description and max factions:
            mapInitialPopulationText.text = manager.GetCurrentMap().GetInitialPopulation().ToString();
            mapDescriptionText.text = manager.GetCurrentMap().GetDescription();
            mapMaxFactionsText.text = manager.GetCurrentMap().GetMaxFactions().ToString();
        }

        //defeat condition:
        public DefeatConditionMenu defeatConditionMenu = new DefeatConditionMenu(); //the host can pick the defeat condition for the game using this menu

        public SpeedModifierMenu speedModifierMenu = new SpeedModifierMenu(); //the host can pick the speed modifier for the game using this menu

        [SerializeField]
        private Text infoMessageText = null; //a message shown whenever there's an error/warning.
        [SerializeField]
        private float infoMessageReload = 2.0f;
        private float infoMessageTimer; //how long will the message be shown for.
        public void ShowInfoMessage(string message) //a method that activates the info message text and show a message
        {
            if (infoMessageText == null) //if the info message text UI element is invalid (in case it's destroyed when loading the game).
                return; //do not proceed.

            infoMessageTimer = infoMessageReload;
            infoMessageText.text = message;
            infoMessageText.gameObject.SetActive(true);
        }

        LobbyManager manager;

        public void Init(LobbyManager manager, List<string> mapNames)
        {
            this.manager = manager;
            UpdateMapDropDownMenu(mapNames);

            //initialize the defeat condition & speed modifier drop down menu options.
            defeatConditionMenu.Init(); 
            speedModifierMenu.Init(); 

            infoMessageText.gameObject.SetActive(false); //hide the info message text UI element
        }

        private void Update()
        {
            if (infoMessageText && infoMessageText.gameObject.activeInHierarchy == true) //if the info message text is valid enabled
            {
                if (infoMessageTimer > 0) //run the timer
                    infoMessageTimer -= Time.deltaTime;
                else
                    infoMessageText.gameObject.SetActive(false);
            }
        }

        //load the main menu
        public void LoadMainMenu ()
        {
            Destroy(gameObject); //destroy the object that has the lobby manager component
            SceneManager.LoadScene(mainMenuScene); //move to the main menu scene
        }
    }
}
