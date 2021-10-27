using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RTSEngine
{
    public class ScenarioLoader : MonoBehaviour
    {
        //this component is an example on how to set up a campaign menu where the player can load maps to play scenarios in

        [System.Serializable]
        public struct ScenarioScene
        {
            public Scenario source; //the scenario that will be loaded in the target scene
            public string sceneName; //the target scene that will be loaded where the player will go through the missions of the above scenario
        }
        [SerializeField]
        private ScenarioScene[] scenarios = new ScenarioScene[0]; //campaign length = this array's length.
        //each scenario in this array is locked if the previous scenario hasn't been completed (except the first one)
        public Scenario LoadedSceneario { private set; get; } //when the map scene is loaded, the Game Manager refers to this field to get the scenario to play

        [SerializeField]
        private ScenarioMenuUI scenarioUIPrefab = null; //each scenario will be displayed using a ScenarioMenuUI
        [SerializeField]
        private GridLayoutGroup scenarioUIParent = null; //parent object with GridLayoutGroup component that includes scenario UI elements
        private List<ScenarioMenuUI> scenarioUIList = new List<ScenarioMenuUI>();

        //main menu
        [SerializeField]
        private string mainMenuScene = "main_menu";
        public void LoadMainMenu() { SceneManager.LoadScene(mainMenuScene); }

        /// <summary>
        /// Called when the ScenarioLoader component is initialized.
        /// </summary>
        private void Start()
        {
            RefreshUI(true); //draw the UI elements for the scenarios.
        }

        /// <summary>
        /// Refreshes the UI elements of the available scenarios.
        /// </summary>
        /// <param name="redraw">When true, previous UI elements will be destroyed and redrawn. When false, only the state of the scenario in the UI elements will be updated.</param>
        public void RefreshUI(bool redraw)
        {
            if (redraw) //redraw the UI elemets for each scenario?
            {
                while(scenarioUIList.Count > 0) //go through the already drawn scenario UI elements
                {
                    Destroy(scenarioUIList[0]); //destroy the UI element
                    scenarioUIList.RemoveAt(0);; //remove the element's entry
                }
            }

            Dictionary<string, bool> savedScenarios = MissionSaveLoad.LoadScenarios(); //get the saved scenarios

            //start by creating the assigned scenarios
            for(int i = 0; i < scenarios.Length; i++)
            {
                ScenarioMenuUI nextScenarioUI = scenarioUIList.Count > i 
                    ? scenarioUIList[i] 
                    : null;

                if (nextScenarioUI == null) //the scenario UI element is not found, create one
                {
                    nextScenarioUI = Instantiate(scenarioUIPrefab.gameObject, scenarioUIParent.transform).GetComponent<ScenarioMenuUI>(); //create a new scenario UI menu element
                    scenarioUIList.Add(nextScenarioUI);
                }

                nextScenarioUI.Init(this, i); //initialise it

                bool unlocked = true;
                if (i > 0) //only if this is not the first scenario in the list
                    savedScenarios.TryGetValue(scenarios[i-1].source.GetCode(), out unlocked); //check if it's unlocked or not

                nextScenarioUI.Refresh(scenarios[i].source.GetName(), scenarios[i].source.GetDescription(), unlocked); //refresh the content of the scenario UI menu
            }
        }

        //load a map's scene with the scenario under the input index
        public void Load(int index)
        {
            DontDestroyOnLoad(this); //we need to move this object to move to the target scene

            LoadedSceneario = scenarios[index].source; //assign the scenario to play in the target map scene
            SceneManager.LoadScene(scenarios[index].sceneName); //load the associated scene 
        }

        //resets the player progress on all saved scenarios:
        public void Reset()
        {
            MissionSaveLoad.ClearSavedScenarios();

            RefreshUI(false); //update the UI status of the scenarios.
        }

        /// <summary>
        /// Unlocks all the scenarios stored in the 'scenarios' list.
        /// </summary>
        public void UnlockAll()
        {
            foreach (ScenarioScene ss in scenarios) //go through all scenarios and unlock them.
                MissionSaveLoad.UnlockScenario(ss.source.GetCode());

            RefreshUI(false); //refresh the UI status of the scenarios to allow them to be loadable
        }
    }
}
