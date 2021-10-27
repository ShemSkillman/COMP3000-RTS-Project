using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace RTSEngine
{
    [System.Serializable]
    public class Mission
    {
        [SerializeField]
        private string code = "mission_code"; //assign a unique code for each mission
        public string GetCode () { return code; }
        [SerializeField]
        private string name = "mission_name"; //the name of the mission to be display in the UI mission menu
        public string GetName() { return name; }
        [SerializeField]
        private string description = "mission_description"; //the description of the mission to be displayed in the UI mission menu
        public string GetDescription () { return description; }
        [SerializeField]
        private Sprite icon = null; //the mission's icon
        public Sprite GetIcon () { return icon; }

        //the different types of missions
        public enum Type
        {
            collectResource,
            eliminate,
            produce,
            custom
        };

        [SerializeField]
        private Type type = Type.collectResource; //type of this mission
        public Type GetMissionType () { return type; }

        [SerializeField]
        private ResourceTypeInfo targetResource = null; //in case this is a collectResource mission type, this represents the resource type to be collected

        [SerializeField]
        private CodeCategoryField[] targetCode = new CodeCategoryField[0]; //unit/building codes or categories can be entered here to define targets for an eliminate/produce mission type

        [SerializeField]
        private int targetAmount = 1; //required amount to collect from a resource, eliminate or produce
        public int GetTargetAmount () { return targetAmount; }
        public int CurrAmount { private set; get; } //the current amount

        [System.Serializable]
        public struct TimeCondition
        {
            public bool survivalTimeEnabled; //when enabled, the mission is completed if the time just below passes or the quest condition is fulfilled, whichever comes first
            public float survivalTime; //how long does the player faction's need to survive to complete this mission (if mission type is surrive)

            public bool timeLimitEnabled; //when enabled, the player will have a time limit to complete the mission, otherwise, it is forfeited
            public float timeLimit; //the actual time limit is assinged here
        }

        [SerializeField]
        private TimeCondition timeCondition = new TimeCondition(); //the (survival/time limit that this mission has)

        //a list of faction entities (units/buildings) that the player needs to defend, otherwise, the mission is failed if one of the entities in this array die
        [SerializeField]
        private CodeCategoryField[] defendFactionEntities = new CodeCategoryField[0];

        //the resources in this array will be added to the player's faction when the mission is complete
        [SerializeField]
        private ResourceInput[] completeResources = new ResourceInput[0];

        //Audio clips:
        [SerializeField, Tooltip("What audio clip to play when the mission is complete?")]
        private AudioClipFetcher completeAudio = new AudioClipFetcher();

        //UnityEvents:
        [SerializeField]
        private UnityEvent startEvent = null;
        [SerializeField]
        private UnityEvent completeEvent = null;
        [SerializeField]
        private UnityEvent failEvent = null;

        //manager components:
        GameManager gameMgr;

        //called to enable a mission
        public TimeCondition Enable (GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            switch (type) //start listening to different RTS Engine events depending on the type of the mission
            {
                case Type.collectResource:

                    CustomEvents.FactionResourceUpdated += OnFactionResourceUpdated;
                    break;

                case Type.produce:

                    CustomEvents.UnitCreated += OnFactionEntityEvent;
                    CustomEvents.BuildingBuilt += OnFactionEntityEvent;
                    break;
            }

            CustomEvents.FactionEntityDead += OnFactionEntityEvent; //required for all mission types

            CurrAmount = 0; //default value for the current amount

            CustomEvents.OnMissionStart(this); //trigger custom event
            startEvent.Invoke();

            return timeCondition; //return the time condition to the manager component
        }

        //called to disable a mission
        public void Disable ()
        {
            switch(type) //start listening to different RTS Engine events depending on the type of the mission
            {
                case Type.collectResource:

                    CustomEvents.FactionResourceUpdated -= OnFactionResourceUpdated;
                    break;

                case Type.produce:

                    CustomEvents.UnitCreated -= OnFactionEntityEvent;
                    CustomEvents.BuildingBuilt -= OnFactionEntityEvent;
                    break;
            }

            CustomEvents.FactionEntityDead -= OnFactionEntityEvent;
        }

        //called each time a faction resource amount is updated if the mission type is set to "collectResource"
        private void OnFactionResourceUpdated (ResourceTypeInfo resourceType, int factionID, int amount)
        {
            if (factionID == GameManager.PlayerFactionID
                && amount > 0 && resourceType.Key == targetResource.Key) //only if the amount is > 0 and the source faction is the player's faction
                OnProgress(amount);
        }

        //called each time a unit/building is dead, only called when the mission type is set to "eliminate" or "produce"
        private void OnFactionEntityEvent (FactionEntity factionEntity)
        {
            //if the faction entity is dead and there are entities that the player is supposed to defend
            if(factionEntity.FactionID == GameManager.PlayerFactionID //only if the dead faction entity belongs to the player faction
                && defendFactionEntities.Length > 0 && factionEntity.EntityHealthComp.IsDead()) 
            {
                foreach(CodeCategoryField codeCategory in defendFactionEntities) //go through the entities that shouldn't be dead
                {
                    if (codeCategory.Contains(factionEntity.GetCode(), factionEntity.GetCategory())) //if the code/category matches
                    {
                        Forfeit(); //mission failed
                        return;
                    }
                }
            }

            if((factionEntity.EntityHealthComp.IsDead() && type == Type.eliminate && factionEntity.FactionID != GameManager.PlayerFactionID) //if this is an elimination mission and the entity doesn't belong to the player's faction
                || (!factionEntity.EntityHealthComp.IsDead() && type == Type.produce && factionEntity.FactionID == GameManager.PlayerFactionID)) //produce mission type and the entity belongs to the player faction
            foreach (CodeCategoryField codeCategory in targetCode) //go through all assigned codes
            {
                if (codeCategory.Contains(factionEntity.GetCode(), factionEntity.GetCategory())) //if the code/category matches
                {
                    OnProgress(1); //positive mission progress
                    break;
                }
            }
        }

        //called when there's positive progress regarding this mission
        public void OnProgress (int value)
        {
            CurrAmount += value; //increment the current amount
            gameMgr.MissionMgr.RefreshUI(); //refresh the UI

            if (CurrAmount >= targetAmount) //if the target amount has been reached
                Complete();
        }

        //called when the mission is completed
        public void Complete()
        {
            gameMgr.AudioMgr.PlaySFX(completeAudio.Fetch(), false); //play the complete mission audio

            gameMgr.ResourceMgr.UpdateRequiredResources(completeResources, true, GameManager.PlayerFactionID); //give the player's faction the complete resources

            CurrAmount = targetAmount; //make sure the current amount doesn't exceed the max amount
            gameMgr.MissionMgr.EnableNext(); //ask the manager to enable the next mission

            CustomEvents.OnMissionComplete(this); //trigger custom event
            completeEvent.Invoke();
        }

        //called when the mission is failed
        public void Forfeit()
        {
            gameMgr.MissionMgr.OnFailed(); //let the manager know 

            CustomEvents.OnMissionFail(this); //trigger custom event
            failEvent.Invoke();
        }
    }
}
