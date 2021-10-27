using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;

namespace RTSEngine
{
    //The array that holds all the current teams information.
    [System.Serializable]
    public class FactionSlot
    {
        [SerializeField]
        private string name = "FACTION_NAME"; //Faction's name.
        public string GetName () { return name; }

        [SerializeField, Tooltip("Default faction type for this slot.")]
        private FactionTypeInfo typeInfo = null; //Type of this faction (the type determines which extra buildings/units can this faction use).
        public FactionTypeInfo GetTypeInfo() { return typeInfo; }

        [SerializeField]
        private Color color = Color.blue; //Faction's color.
        public Color GetColor () { return color; }

        [SerializeField]
        private bool playerControlled = false; //Is the team controlled by the player, make sure that only one team is controlled by the player.
        public bool PlayerControlled
        {
            private set
            {
                playerControlled = value;
            }
            get
            {
                return playerControlled;
            }
        }

        [SerializeField]
        private int maxPopulation = 5; //Maximum number of units that can be present at the same time (which can be increased in the game by constructing certain buildings)

        //update the maximum population
        public void UpdateMaxPopulation(int value, bool add = true)
        {
            if (add)
                maxPopulation += value;
            else
                maxPopulation = value;

            //custom event trigger:
            CustomEvents.OnMaxPopulationUpdated(this, value);
        }
        //get the maximum population
        public int GetMaxPopulation() { return maxPopulation; }

        private int currentPopulation; //current number of spawned units.

        //update the current population
        public void UpdateCurrentPopulation(int value)
        {
            currentPopulation += value;
            //custom event trigger:
            CustomEvents.OnCurrentPopulationUpdated(this, value);
        }

        //get the current population
        public int GetCurrentPopulation() { return currentPopulation; }

        //get the amount of free slots:
        public int GetFreePopulation()
        {
            return maxPopulation - currentPopulation;
        }

        [SerializeField,]
        private Building capitalBuilding = null; //The capital building that MUST be placed in the map before startng the game.
        public Building CapitalBuilding
        {
            set 
            {
                if (value == null)
                    return;

                capitalBuilding = value;
            }
            get
            {
                return capitalBuilding;
            }
        }
        public Vector3 CapitalPosition { private set; get; } //The capital building's position is stored in this variable because when it's a new multiplayer game, the capital buildings are re-spawned in order to be synced in all players screens.

        [SerializeField]
        private Transform camLookAtPos = null; //if assigned, this represent the position where the camera will look at when the game starts.
        //if not assigned, then the position of the capital building will be used.
        public Vector3 GetCamLookAtPosition () { return camLookAtPos.position; }

        public int ID { private set; get; }
        public FactionManager FactionMgr { private set; get; } //The faction manager is a component that stores the faction data. Each faction is required to have one.

        //the active instance of the NPCManager of the correpondant NPC type.
        [SerializeField, Tooltip("Default NPC type for this slot.")]
        private NPCTypeInfo npcType = null; //Drag and drop the NPCTypeInfo asset here.

        private NPCManager npcMgrIns; //the active instance of the NPC manager prefab.
        //get the NPC Manager instance:
        public NPCManager GetNPCMgrIns() { return npcMgrIns; }

        //init the npc manager:
        public void InitNPCMgr(GameManager gameMgr)
        {
            //making sure there's a valid npc type and a valid NPCManager prefab for it:
            Assert.IsNotNull(npcType,
                $"[FactionSlot] NPC Faction of ID: {ID} does not have a NPCTypeInfo assigned, will be initiated as dummy faction.");

            NPCManager npcMgrPrefab = npcType.GetNPCManagerPrefab(typeInfo);
            Assert.IsNotNull(npcMgrPrefab,
                $"[FactionSlot] NPC Faction of ID: {ID} does not have a NPCManager prefab defined for its type, will be initiated as dummy faction.");

            npcMgrIns = Object.Instantiate(npcMgrPrefab.gameObject).GetComponent<NPCManager>();
            
            //init the npc manager instance:
            npcMgrIns.Init(npcType, gameMgr, FactionMgr);
        }

        public bool IsNPCFaction() //is this faction NPC?
        {
            return PlayerControlled == false && npcType != null;
        }

        public bool Lost { private set; get; } //true when the faction is defeated and can no longer have an impact on the game.

        //units/buildings that are spawned by default must be added to the following list
        [System.Serializable]
        public struct DefaultFactionEntity
        {
            public List<FactionEntity> instances;
            public FactionTypeInfo[] factionTypes; //leave empty if you want the faction entity to remain for all faction types, if not, specify the allowed faction types here
        }
        [SerializeField]
        private DefaultFactionEntity[] defaultFactionEntities = new DefaultFactionEntity[0];

        //multiplayer related attributes:
#if RTSENGINE_MIRROR
        //Mirror: 
        public NetworkLobbyFaction_Mirror LobbyFaction_Mirror { private set; get; }
        public NetworkFactionManager_Mirror FactionManager_Mirror { set; get; }
        public int ConnID_Mirror { set; get; }

        public void InitMultiplayer (NetworkLobbyFaction_Mirror lobbyFaction)
        {
            this.LobbyFaction_Mirror = lobbyFaction;
        }
#endif

        //init the faction slot and update the faction attributes
        public void Init(string name, FactionTypeInfo typeInfo, Color color, bool playerControlled, int population, NPCTypeInfo npcType, FactionManager factionMgr, int factionID, GameManager gameMgr)
        {
            this.name = name;
            this.typeInfo = typeInfo;
            this.color = color;
            this.PlayerControlled = playerControlled;

            this.npcType = this.PlayerControlled ? null : npcType;

            Init(factionID, factionMgr, gameMgr);

            UpdateMaxPopulation(population, false);
        }

        //init the faction without modifying the faction attributes
        public void Init (int factionID, FactionManager factionMgr, GameManager gameMgr)
        {
            this.ID = factionID;
            this.FactionMgr = factionMgr;

            FactionMgr.Init(gameMgr, ID, typeInfo ? typeInfo.GetLimits() : null, this); //init the faction manager component of this faction

            //depending on the faction type, add extra units/buildings (if there's actually any) to be created for each faction:
            if (playerControlled == true) //if this faction is player controlled:
            {
                if (typeInfo != null) //if this faction has a valid type.
                    gameMgr.PlacementMgr.AddBuildingRange(typeInfo.GetExtraBuildings()); //add the extra buildings so that this faction can use them.
            }
            else if (IsNPCFaction() == true) //if this is not controlled by the local player but rather NPC.
                //Init the NPC Faction manager:
                InitNPCMgr(gameMgr);

            Lost = false;

            CustomEvents.OnFactionInit(this);
        }

        public void SpawnFactionEntities (GameManager gameMgr)
        {
            //if the defeat condition is set to destroy capital then all factions must have a building assigned as their capital building before the game starts.
            Assert.IsFalse(gameMgr.GetDefeatCondition() == DefeatConditions.destroyCapital && capitalBuilding == null
                , $"[FactionSlot] Defeat condition is set to 'Destroy Capital' but faction ID {ID} does not have its 'Capital Building' field assigned!");

            CapitalPosition = Vector3.zero;

            //if the faction has a valid faction capital building assigned
            if (capitalBuilding != null)
            {
                CapitalPosition = capitalBuilding.transform.position; //assign its position

                //if there's a valid type and a valid capital building assigned to it
                if (typeInfo != null && typeInfo.GetCapitalBuilding() != null)
                {
                    Object.DestroyImmediate(this.capitalBuilding.gameObject); //destroy the default capital and spawn another one:

                    //create new faction center:
                    capitalBuilding = gameMgr.BuildingMgr.CreatePlacedInstanceLocal(
                        typeInfo.GetCapitalBuilding(),
                        CapitalPosition,
                        typeInfo.GetCapitalBuilding().transform.rotation.eulerAngles.y,
                        null, ID, true, true);
                }
                else //if not, just init the pre-placed capital building
                    capitalBuilding.Init(gameMgr, ID, false, null, true);
            }

            if (ID == GameManager.PlayerFactionID) //if this is the local player? (owner of this capital building)
            {
                //make sure either the capital building is assigned or the camera look at position is.
                Assert.IsFalse(capitalBuilding == null && camLookAtPos == null
                    , $"[FactionSlot] Neither the 'Capital Building' nor the 'Camera Look At Position' fields are assigned for the player faction (ID: {ID}, one must be assigned to set the initial position of the camera!");

                //Set the player's initial camera position (looking at the faction's look at position if it's assigned, else the capital building)
                Vector3 lookAtPos = camLookAtPos ? camLookAtPos.position : capitalBuilding.transform.position;
                gameMgr.CamMgr.LookAt(lookAtPos, false);
            }

            foreach(DefaultFactionEntity defaultFactionEntity in defaultFactionEntities) //go through the default faction entities
            {
                if (defaultFactionEntity.factionTypes.Length == 0 //if not faction types have been assigned
                    || defaultFactionEntity.factionTypes.Any(t => t == typeInfo)) //or the faction slot's type is specified in the array
                {
                    foreach (FactionEntity instance in defaultFactionEntity.instances)
                    {
                        if (instance.Type == EntityTypes.building) //if we're spawning a pre placed building, more initializing settings are required
                            (instance as Building).Init(gameMgr, ID, false, capitalBuilding?.BorderComp, false);
                        else
                            (instance as Unit).Init(gameMgr, ID, false, null, instance.transform.position);

                        //if this is a unit, then update the population and update the faction limits:
                        FactionMgr.UpdateLimitsList(instance.GetCode(), instance.GetCategory(), true);
                        if (instance.Type == EntityTypes.unit)
                        {
                            Unit defaultUnit = (Unit)instance;
                            UpdateCurrentPopulation(defaultUnit.GetPopulationSlots());
                        }
                    }
                }
                else
                    foreach (FactionEntity instance in defaultFactionEntity.instances)
                        Object.DestroyImmediate(instance.gameObject); //destroy the faction entity instance if it's not part of this faction type
            }

            CustomEvents.OnFactionDefaultEntitiesInit(this);
        }

        //a method called to destroy the faction slot when intializing the game
        public void InitDestroy()
        {
            if(capitalBuilding) //if there's a capital building, then..
                Object.DestroyImmediate(capitalBuilding.gameObject); //destroy it

            foreach (DefaultFactionEntity defaultFactionEntity in defaultFactionEntities) //and destroy all the default faction entities
                    foreach (FactionEntity instance in defaultFactionEntity.instances)
                        Object.DestroyImmediate(instance.gameObject);
        }

        //a method to disable this faction slot
        public void Disable ()
        {
            Lost = true;
        }
    }
}
