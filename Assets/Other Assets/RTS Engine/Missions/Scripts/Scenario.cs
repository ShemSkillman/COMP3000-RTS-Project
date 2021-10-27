using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [CreateAssetMenu(fileName = "NewScenario", menuName = "RTS Engine/Scenario", order = 3)]
    public class Scenario : ScriptableObject
    {
        [SerializeField]
        private string code = "scenario_code";
        public string GetCode() { return code; }
        [SerializeField]
        private string _name = "Scenario Name";
        public string GetName () { return _name; }
        [SerializeField]
        private string description = "Scenario Description"; //to be displayed in the campaign menu
        public string GetDescription () { return description; }

        [SerializeField]
        private Mission[] missions = new Mission[0]; //the missions (in sequential order) that the player needs to complete
        public int GetMissionCount () { return missions.Length; }
        public Mission GetMission (int index) { return missions[index]; }
    }
}
