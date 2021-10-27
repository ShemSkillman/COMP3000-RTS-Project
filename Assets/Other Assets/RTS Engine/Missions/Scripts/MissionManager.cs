using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    //the status of a scenario
    public enum ScenarioStatus
    {
        none,
        active,
        failed,
        success,
    }

    public class MissionManager : MonoBehaviour
    {
        //each mission manager handles a scenario composed of a list of consecutive missions
        [SerializeField]
        private Scenario scenario = null;
        
        private int currMissionID; //the current active mission index (in the above array).
        private Mission.TimeCondition currTimeCondition; //the current time condition of the currently enabled mission
        private string currMissionDescription; //since it could contain additional info other than the original description that don't need to be updated every frame

        private ScenarioStatus status; //this scenario's current status

        //UI Attributes:
        [SerializeField]
        private GameObject menu = null;
        [SerializeField]
        private Text scenarioNameText = null;
        [SerializeField]
        private Text missionNameText = null;
        [SerializeField]
        private Text missionDescriptionText = null;
        [SerializeField]
        private Image missionIconImage = null;

        //save progress: if the player finishes this scenario successfully, that will be saved so that you can unlock other missions in the menu for example
        [SerializeField]
        private bool save = true;

        //other components
        GameManager gameMgr;
        
        //called to initialise this component
        public void Init(GameManager gameMgr, Scenario scenario)
        {
            this.gameMgr = gameMgr;
            if (scenario != null)
                this.scenario = scenario;

            if (this.scenario == null) //if no scenario has been assigned:
            {
                Debug.LogWarning("[MissionManager] No scenario has been assigned, disabling MissionManager component");
                menu.SetActive(false);
                enabled = false;
                return;
            }
            else
                ActivateScenario(this.scenario); //scenario assigned, activate it
        }

        //method that starts a new scenario:
        public void ActivateScenario (Scenario scenario)
        {
            if (!scenario || status == ScenarioStatus.success || status == ScenarioStatus.failed) //if there was an active scenario that ended, do not proceed
                return;

            menu.SetActive(false); //start by hiding the menu

            if (status == ScenarioStatus.active && this.scenario) //if there was an active scenario already
            {
                CustomEvents.OnScenarioFail(scenario); //mark scenario as failed

                scenario.GetMission(currMissionID).Disable(); //disable the last active mission in that scenario
            }

            //assign new scenario
            this.scenario = scenario;

            currMissionID = -1;

            menu.SetActive(true); //show the scenario menu
            EnableNext(); //enable the first mission
            status = ScenarioStatus.active; //this mission scenario is now active

            CustomEvents.OnScenarioStart(scenario); //trigger custom event

            enabled = true; //enable this component
        }

        //called to move to the next mission:
        public void EnableNext ()
        {
            if (currMissionID >= 0) //if this is a valid index
                scenario.GetMission(currMissionID).Disable(); //disable the last active mission

            if(currMissionID == scenario.GetMissionCount()-1) //no more mission left?
            {
                OnSuccess();
                return;
            }

            currMissionID++; //move to the next mission

            //at least one more mission is still available:
            currTimeCondition = scenario.GetMission(currMissionID).Enable(gameMgr); //enable it
            RefreshUI(); //update the UI elements
        }

        //called when all of the missions in the scenario are completed
        public void OnSuccess ()
        {
            RefreshUI();

            status = ScenarioStatus.success;
            CustomEvents.OnScenarioSuccess(scenario); //trigger custom event

            gameMgr.WinGame(); //player wins the game

            if (save) //if saving is enabled
                MissionSaveLoad.SaveScenario(scenario.GetCode(), true);
        }

        //called when one of the missions has failed
        public void OnFailed ()
        {
            status = ScenarioStatus.failed;
            CustomEvents.OnScenarioFail(scenario); //trigger custom event

            scenario.GetMission(currMissionID).Disable(); //disable the last active mission

            gameMgr.LooseGame(); //player loses the game
        }

        private void Update()
        {
            if (status != ScenarioStatus.active) //if the current scenario isn't active
                return;

            string nextDescription = currMissionDescription;

            if (currTimeCondition.survivalTimeEnabled) //if survival time is enabled for the current mission
            {
                if(currTimeCondition.survivalTime <= 0.0f) //survival time is done
                {
                    scenario.GetMission(currMissionID).Complete(); //complete the mission since the player survived for the assigned time
                    return;
                }

                //keep displaying the remaining survival time and run the timer as well
                nextDescription += "\nSurvival Time: " + RTSHelper.TimeToString(currTimeCondition.survivalTime);
                currTimeCondition.survivalTime -= Time.deltaTime;
            }

            if (currTimeCondition.timeLimitEnabled) //if time limit (time to finish the mission before it is forfeited) is enabled
            {
                //if the time limit is already over:
                if (currTimeCondition.timeLimit <= 0.0f)
                {
                    OnFailed(); //scenario hasn't been successfully completed
                    return;
                }

                //display the time limit and run the timer:
                nextDescription += "\nTime Left: " + RTSHelper.TimeToString(currTimeCondition.timeLimit);
                currTimeCondition.timeLimit -= Time.deltaTime;
            }

            //update the mission's description
            missionDescriptionText.text = nextDescription;
        } 

        //refresh the UI elements to display the current mission's objectives/progress
        public void RefreshUI ()
        {
            if (status != ScenarioStatus.active) //if the scenario isn't active, hide all UI elements
                ToggleUI(false);

            ToggleUI(true);

            //show the scenario's name, mission name and description
            scenarioNameText.text = scenario.GetName() + $": {currMissionID+1}/{scenario.GetMissionCount()}";
            missionNameText.text = scenario.GetMission(currMissionID).GetName();
            currMissionDescription = scenario.GetMission(currMissionID).GetDescription();

            //if this a mission where player needs to collect, produce or eliminate an X amount of entities
            if(scenario.GetMission(currMissionID).GetMissionType() != Mission.Type.custom)
                currMissionDescription += ": " + scenario.GetMission(currMissionID).CurrAmount + "/" + scenario.GetMission(currMissionID).GetTargetAmount();

            missionDescriptionText.text = currMissionDescription;

            if (missionIconImage) //if there's an Image component assigned to display the mission's ico
                missionIconImage.sprite = scenario.GetMission(currMissionID).GetIcon();
        }
        
        //hide/show all the UI elements related to displaying the mission's progress
        public void ToggleUI (bool enable) 
        {
            scenarioNameText.gameObject.SetActive(enable);
            missionNameText.gameObject.SetActive(enable);
            missionDescriptionText.gameObject.SetActive(enable);
        }
    }
}
