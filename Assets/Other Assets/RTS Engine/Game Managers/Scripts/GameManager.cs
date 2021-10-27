using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Networking;
using RTSCamera;

/* Game Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public enum GameState { running, lost, won, pause, frozen }
    public enum DefeatConditions { destroyCapital, eliminateAll }

    public class GameManager : MonoBehaviour
    {

#if UNITY_EDITOR
        //used to keep track of the custom editor tabs
        public int editorElementID = 0;
        public int editorTabID = 0;
#endif

        [SerializeField]
        private string mainMenuScene = "Menu"; //Main menu scene name, this is the scene that will be loaded when the player decides to leave the game.

        [SerializeField]
        private DefeatConditions defeatCondition = DefeatConditions.destroyCapital; //this presents the condition that defines when a faction is defeated: either when the capital is destroyed or all units/buildings are destroyed
        public DefeatConditions GetDefeatCondition() { return defeatCondition; }

        [SerializeField]
        private float speedModifier = 1.0f; //speed modifier for unit movement, construction and collection time!
        public float GetSpeedModifier () { return speedModifier >= 1.0f ? speedModifier : 1.0f;  } //must be at least 1.0f

        public static GameState GameState { private set; get; } //game state
        public static void SetGameState (GameState newState)
        {
            GameState = newState; //update the state

            if (MultiplayerGame == false) //only in single player games
            {
                //if the new state is set to running:
                if (GameState == GameState.running)
                    Time.timeScale = 1.0f; //set the time scale to 1.0 to allow the game to run
                else if (GameState == GameState.pause) //if the game is paused
                    Time.timeScale = 0.0f; //to freeze the game
            }

            CustomEvents.OnGameStateUpdated(); //and trigger the custom event
        }

        [SerializeField]
        private List<FactionSlot> factions = new List<FactionSlot>();
        public FactionSlot GetFaction (int id) { return factions[id]; }
        public IEnumerable<FactionSlot> GetFactions () { return factions; }
        public int GetFactionCount () { return factions.Count; }
        [SerializeField]
        private bool randomFactionSlots = true; //shuffle the factions list when the game starts so the player doesn't start in the same faction location everytime?
        private int activefactionsAmount = 0; //Amount of spawned factions;

        public static int PlayerFactionID { private set; get; } //Faction ID of the team controlled by the player.
        public static FactionManager PlayerFactionMgr { private set; get; } //The faction manager component of the faction controlled by the player.
        public static FactionSlot PlayerFaction { private set; get; } //the local player faction slot

        public bool Initialized { private set; get; } //Are all factions stats ready? 

        //Peace time:
        [SerializeField]
        private float peaceTime = 60.0f; //Time (in seconds) after the game starts, when no faction can attack the other.
        public bool InPeaceTime () { return peaceTime > 0.0f; }

        [SerializeField]
        private AudioClip winGameAudio = null;
        [SerializeField]
        private AudioClip loseGameAudio = null;

        //Other scripts:
        public AudioManager AudioMgr { private set; get; }
        public GridSearchHandler GridSearch { private set; get; }
        public ResourceManager ResourceMgr { private set; get; }
        public UIManager UIMgr { private set; get; }
        public CameraController CamMgr { private set; get; }
        public BuildingManager BuildingMgr { private set; get; }
        public SelectionManager SelectionMgr { private set; get; }
        public UnitGroupSelection GroupSelection { private set; get; }
        public TaskManager TaskMgr { private set; get; }
        public BuildingPlacement PlacementMgr { private set; get; }
        public UnitManager UnitMgr { private set; get; }
        public TerrainManager TerrainMgr { private set; get; }
        public MovementManager MvtMgr { private set; get; }
        public ErrorMessageHandler ErrorMessageMgr { private set; get; }
        public MissionManager MissionMgr { private set; get; }
        public MinimapIconManager MinimapIconMgr { private set; get; }
        public AttackWarningManager AttackWarningMgr { private set; get; }
        public AttackManager AttackMgr { private set; get; }
        public EffectObjPool EffectPool { private set; get; }
        public UpgradeManager UpgradeMgr { private set; get; }
        public InputManager InputMgr { private set; get; }
#if RTSENGINE_FOW
        public FogOfWarRTSManager FoWMgr { private set; get; }
#endif

        //Multiplayer related:
        public static bool MultiplayerGame { private set; get; } //If it's a multiplayer game, this will be true.
        public static int HostFactionID { private set; get; } //This is the Faction ID that represents the server/host of the multiplayer game.
#if RTSENGINE_MIRROR
        public NetworkLobbyManager_Mirror NetworkManager_Mirror { private set; get; }
#endif

        protected virtual void Awake()
        {
            Time.timeScale = 1.0f; //unfreeze game if it was frozen from a previous game.

            Initialized = false; //faction stats are not ready, yet

            //Get components:
            AudioMgr = FindObjectOfType(typeof(AudioManager)) as AudioManager; 
            GridSearch = FindObjectOfType(typeof(GridSearchHandler)) as GridSearchHandler; 
            CamMgr = FindObjectOfType(typeof(CameraController)) as CameraController;
            ResourceMgr = FindObjectOfType(typeof(ResourceManager)) as ResourceManager; 
            BuildingMgr = FindObjectOfType(typeof(BuildingManager)) as BuildingManager;
            TaskMgr = FindObjectOfType(typeof(TaskManager)) as TaskManager;
            UnitMgr = FindObjectOfType(typeof(UnitManager)) as UnitManager;
            SelectionMgr = FindObjectOfType(typeof(SelectionManager)) as SelectionManager;
            GroupSelection = FindObjectOfType(typeof(UnitGroupSelection)) as UnitGroupSelection;
            PlacementMgr = FindObjectOfType(typeof(BuildingPlacement)) as BuildingPlacement;
            TerrainMgr = FindObjectOfType(typeof(TerrainManager)) as TerrainManager;
            MvtMgr = FindObjectOfType(typeof(MovementManager)) as MovementManager;
            UIMgr = FindObjectOfType(typeof(UIManager)) as UIManager; 
            ErrorMessageMgr = FindObjectOfType(typeof(ErrorMessageHandler)) as ErrorMessageHandler;
            MinimapIconMgr = FindObjectOfType(typeof(MinimapIconManager)) as MinimapIconManager;
            AttackWarningMgr = FindObjectOfType(typeof(AttackWarningManager)) as AttackWarningManager;
            AttackMgr = FindObjectOfType(typeof(AttackManager)) as AttackManager;
            EffectPool = FindObjectOfType(typeof(EffectObjPool)) as EffectObjPool;
            UpgradeMgr = FindObjectOfType(typeof(UpgradeManager)) as UpgradeManager;
            InputMgr = FindObjectOfType(typeof(InputManager)) as InputManager;
#if RTSENGINE_FOW
            FoWMgr = FindObjectOfType(typeof(FogOfWarRTSManager)) as FogOfWarRTSManager;
#endif

#if RTSENGINE_FOW
            if(FoWMgr) //only if there's a fog of war manager in the scene
                FoWMgr.Init(this);
#endif

            AudioMgr.Init(this);
            GridSearch.Init(this);
            CamMgr.Init(this);
            MinimapIconMgr?.Init(this);
            MvtMgr.Init(this);
            TaskMgr.Init(this);
            SelectionMgr.Init(this);
            GroupSelection.Init(this);
            PlacementMgr.Init(this);
            TerrainMgr.Init(this);
            UIMgr.Init(this); //initialize the UI manager.
            ErrorMessageMgr.Init(this);
            AttackWarningMgr?.Init(this);
            AttackMgr.Init(this);
            EffectPool.Init(this);
            UpgradeMgr.Init(this);
            ResourceMgr.Init(this);
            BuildingMgr.Init(this);
            UnitMgr.Init(this);
            InputMgr.Init(this);

            MultiplayerGame = false; //We start by assuming it's a single player game.

            if (!InitMultiplayerGame() && !InitSinglePlayerGame()) //if it's not neither a multiplayer nor a single player game
            {
                InitFactions(); //we're starting the game directly from the editor in the map scene, init factions directly
            }

            SetPlayerFaction(); //pick the player faction ID.

            ResourceMgr.InitFactionResources(); //init resources for factions.

            InitCampaignGame(); //initialise a game where the player is coming from the campaign menu to play a mission
            InitDefaultEntities(); //initialise the pre-spawned faction units and factions

            UnitMgr.OnFactionSlotsInitialized(); //init free units
            BuildingMgr.OnFactionSlotsInitialized(); //init free buildings
            InputMgr.OnFactionSlotsInitialized(); //init spawnable prefabs and default entities in a multiplayer game

            ResourceMgr.InitResources(); //init the pre-spawned resources.

            //In order to avoid having buildings that are being placed by AI players and units collide, we will ignore physics between their two layers:
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hidden"), LayerMask.NameToLayer("Unit"));

            //Set the amount of the active factions:
            activefactionsAmount = factions.Count;

            SetGameState(GameState.running); //the game state is now set to running

            UIMgr.PeaceTime.Toggle(peaceTime > 0.0f); //enable the peace time panel if there's actual peace time assigned

            //reaching this point means that all faction info/stats in the game manager are ready:
            Initialized = true;
        }

        private bool InitMirrorMultiplayerGame()
        {
#if RTSENGINE_MIRROR
            NetworkManager_Mirror = (NetworkLobbyManager_Mirror)NetworkLobbyManager_Mirror.singleton;

            if (NetworkManager_Mirror == null) //if there's mirror network lobby manager component in the scene then this is not a multiplayer game
                return false;

            //randomize the faction slots in this map before starting the assignment of each faction slot
            RandomizeFactionSlots(NetworkManager_Mirror.FactionSlotIndexes);

            MultiplayerGame = true; //this is now a multiplayer game.
            List<NetworkLobbyFaction_Mirror> lobbyfactions = NetworkLobbyManager_Mirror.LobbyFactions; //get all lobby faction components into a list

            if (lobbyfactions.Count > factions.Count) //if there aren't enough slots in this map for all factions.
            {
                Debug.LogError("[Game Manager]: Not enough slots available for all the factions coming from the multiplayer menu.");
                return false;
            }

            //set the defeat condition & speed modifier:
            defeatCondition = NetworkManager_Mirror.UIMgr.defeatConditionMenu.GetValue();
            speedModifier = NetworkManager_Mirror.UIMgr.speedModifierMenu.GetValue();

            //we have enough slots:
            for (int i = 0; i < lobbyfactions.Count; i++) //Loop through all the slots and set up the factions.
            {
                int index = lobbyfactions[i].index; //get the faction ID.
                factions[index].Init(
                    lobbyfactions[i].GetFactionName(),
                    NetworkManager_Mirror.GetFactionTypeInfo(lobbyfactions[i].GetFactionTypeID()),
                    NetworkManager_Mirror.FactionColor.Get(lobbyfactions[i].GetFactionColorID()),
                    lobbyfactions[i].isLocalPlayer,
                    NetworkManager_Mirror.GetMapInitialPopulation(),
                    null,
                    gameObject.AddComponent<FactionManager>(), i, this);

                if (lobbyfactions[i].IsHost) //does this lobby faction belong to the host?
                    HostFactionID = i; //mark it.

                factions[index].InitMultiplayer(lobbyfactions[i]);
            }

            //if there are more slots than required, remove them
            while (lobbyfactions.Count < factions.Count)
            {
                factions[factions.Count - 1].InitDestroy();
                factions.RemoveAt(factions.Count - 1);
            }
#endif
            return true;
        }

        //a method that initializes a multiplayer game
        private bool InitMultiplayerGame()
        {
#if RTSENGINE_MIRROR
            return InitMirrorMultiplayerGame();
#else
            return false;
#endif

        }

        //a method that initializes a single player game:
        private bool InitSinglePlayerGame()
        {
            //if there's no single player manager then
            if (LobbyManager.instance == null)
                return false; //do not proceed.

            //If there's a map manager script in the scene, it means that we just came from the single player menu, so we need to set the NPC players settings!

            //randomzie the faction slots
            List<int> factionSlots = RTSHelper.GenerateIndexList(factions.Count);
            RTSHelper.ShuffleList<int>(factionSlots); //randomize the faction slots indexes list IDs by shuffling it.
            RandomizeFactionSlots(factionSlots.ToArray());

            //This where we will set the NPC settings using the info from the single player manager:
            //First check if we have enough faction slots available:
            if (LobbyManager.instance.LobbyFactions.Count <= factions.Count)
            {
                defeatCondition = LobbyManager.instance.UIMgr.defeatConditionMenu.GetValue(); //set defeat condition
                speedModifier = LobbyManager.instance.UIMgr.speedModifierMenu.GetValue(); //set speed modifier

                //loop through the factions slots of this map:
                for (int i = 0; i < LobbyManager.instance.LobbyFactions.Count; i++)
                {
                    factions[i].Init(
                        LobbyManager.instance.LobbyFactions[i].GetFactionName(),
                        LobbyManager.instance.LobbyFactions[i].GetFactionType(),
                        LobbyManager.instance.LobbyFactions[i].GetFactionColor(),
                        LobbyManager.instance.LobbyFactions[i].PlayerControlled,
                        LobbyManager.instance.GetCurrentMap().GetInitialPopulation(),
                        LobbyManager.instance.LobbyFactions[i].GetNPCType(),
                        gameObject.AddComponent<FactionManager>(), i, this);
                }

                //if there are more slots than required.
                while (LobbyManager.instance.LobbyFactions.Count < factions.Count)
                {
                    //remove the extra slots:
                    factions[factions.Count - 1].InitDestroy();
                    factions.RemoveAt(factions.Count - 1);
                }

                //Destroy the map manager script because we don't really need it anymore:
                DestroyImmediate(LobbyManager.instance.gameObject);

                return true;
            }
            else
            {
                Debug.LogError("[Game Manager]: Not enough slots available for all the factions coming from the single player menu.");
                return false;
            }
        }

        //when starting the game directly in the map's scene in the editor, init the factions here
        private void InitFactions ()
        {
            for (int i = 0; i < factions.Count; i++)
                factions[i].Init(i, gameObject.AddComponent<FactionManager>(), this);
        }

        //used to intialise the faction entities that are pre-spawne in the map when the games starts:
        private void InitDefaultEntities ()
        {
            if (GameManager.MultiplayerGame) //if this is no multiplayer game, we'll create the capital buildings and init them here, else, input manager handles it
                return;

            for (int i = 0; i < factions.Count; i++)
                factions[i].SpawnFactionEntities(this);
        }

        //initialise a campaign game where the player needs to complete mission(s)
        private void InitCampaignGame ()
        {
            MissionMgr = FindObjectOfType(typeof(MissionManager)) as MissionManager;
            ScenarioLoader scenarioLoader = FindObjectOfType(typeof(ScenarioLoader)) as ScenarioLoader;

            if (MissionMgr) //if there's a mission manager in this current map
                MissionMgr.Init(this, scenarioLoader != null ? scenarioLoader.LoadedSceneario : null); //initiailise it

            if(scenarioLoader != null) //destroy the scenario loader as it is no longer needed
                Destroy(scenarioLoader.gameObject);
        }

        //a method that sets the player faction ID
        private bool SetPlayerFaction()
        {
            PlayerFactionID = -1;
            for (int i = 0; i < factions.Count; i++) //go through the factions list
            {
                if (factions[i].PlayerControlled == true) //is this the player controlled faction?
                {
                    //if we have a player faction ID already:
                    if (PlayerFactionID != -1)
                    {
                        Debug.LogError("[Game Manager]: There's more than one faction labeled as player controlled.");
                        return false;
                    }
                    //if the player faction hasn't been set yet:
                    if (PlayerFactionID == -1)
                    {
                        PlayerFactionID = i;
                        PlayerFactionMgr = factions[i].FactionMgr; //& set the player faction manager as well
                        PlayerFaction = factions[i];
                    }
                }
            }
            if (PlayerFactionID == -1) //if the player faction ID hasn't been set.
            {
                Debug.LogWarning("[Game Manager]: There's no faction labeled as player controlled.");
                return false;
            }

#if RTSENGINE_FOW //if we're using the FOW asset, update the FOW team ID
            if (FoWMgr)
                FoWMgr.UpdateTeam(PlayerFactionID);
#endif

            return true;
        }

        void Update()
        {
            //Peace timer:
            if (peaceTime > 0)
            {
                peaceTime -= Time.deltaTime;
                UIMgr.PeaceTime.Update(peaceTime); //update the peace timer UI each time.
            }
            else
            {
                //when peace timer is ended:
                peaceTime = 0.0f;
                UIMgr.PeaceTime.Toggle(false);
            }
        }

        // Are we in peace time?
        public bool InpeaceTime()
        {
            return peaceTime > 0.0f;
        }

        //Randomize the order of the faction slots:
        private void RandomizeFactionSlots(int[] indexSeedList)
        {
            //do not randomize? okay. also make sure the index seed list has the same length as the faction slots amount
            if (randomFactionSlots == false || indexSeedList.Length != factions.Count)
                return;

            int i = 0;
            while(i < indexSeedList.Length) //this seed list provides the randomized faction slot indexes
            {
                if (i == indexSeedList[i] || i > indexSeedList[i]) //to avoid reswapping faction slots
                {
                    i++;
                    continue;
                }

                FactionSlot tempSlot = factions[i];
                factions[i] = factions[indexSeedList[i]];
                factions[indexSeedList[i]] = tempSlot;

                i++;
            }
        }

        //Game state methods:

        //called when a faction is defeated
        public void OnFactionDefeated(int factionID)
        {
            if (MultiplayerGame == false) //in the case of a singleplayer game
                OnFactionDefeatedLocal(factionID); //directly mark the faction as defeated
            else //multiplayer game:
            {
                NetworkInput NewInputAction = new NetworkInput() //ask the server to announce that the faction has been defeated.
                {
                    sourceMode = (byte)InputMode.destroy,
                    targetMode = (byte)InputMode.faction,
                    value = factionID,
                };

                InputManager.SendInput(NewInputAction, null, null); //send input action to the input manager
            }
        }

        //called locally when a faction is defeated (its capital building has fallen):
        public void OnFactionDefeatedLocal(int factionID)
        {
            if (factions[factionID].Lost)
                return;

            factions[factionID].Disable(); //mark the faction slot as disabled

            activefactionsAmount--; //decrease the amount of active factions:
            CustomEvents.OnFactionEliminated(factions[factionID]); //call the custom event.

            //Show UI message.
            UIMgr.ShowPlayerMessage(factions[factionID].GetName() + " (Faction ID:" + factionID.ToString() + ") has been defeated.", UIManager.MessageTypes.info);
            
            //Destroy all buildings and kill all units:
            if (factions[factionID].IsNPCFaction() == true) //if his is a NPC faction
                //destroy the active instance of the NPC Manager component:
                Destroy(factions[factionID].GetNPCMgrIns().gameObject);

            //go through all the units/buildings that this faction owns and destroy them
            //because we'll be modifying the source list by destroying the buildings, we need to create a temporary list
            List<FactionEntity> factionEntities = factions[factionID].FactionMgr.GetFactionEntities().ToList();
            foreach (FactionEntity entity in factionEntities)
                entity.EntityHealthComp.DestroyFactionEntityLocal(false);

            if (factionID == PlayerFactionID)
                //If the player is defeated then:
                LooseGame();
            //If one of the other factions was defeated:
            //Check if only the player was left undefeated!
            else if (activefactionsAmount == 1)
            {
                WinGame(); //Win the game!
                CustomEvents.OnFactionWin(factions[factionID]); //call the custom event.
            }
        }

        //Win the game:
        public void WinGame()
        {
            //when all the other factions are defeated, 

            //stop whatever the player is doing:
            SelectionMgr.Selected.RemoveAll();

            AudioMgr.PlaySFX(winGameAudio, false);

            Time.timeScale = 0.0f; //freeze the game
            SetGameState(GameState.won); //the game state is now over and the player won
        }

        //called when the player's faction is defeated:
        public void LooseGame()
        {
            AudioMgr.PlaySFX(loseGameAudio, false);

            Time.timeScale = 0.0f; //freeze the game
            SetGameState(GameState.lost); //the game state is now over and the player lost
        }

        //allows the player to leave the current game:
        public void LeaveGame()
        {
            if (MultiplayerGame == false)
                SceneManager.LoadScene(mainMenuScene); //go back to main menu
            else
            {
#if RTSENGINE_MIRROR
                //OnFactionDefeated(PlayerFactionID); //mark the player's faction as defeated.
                NetworkManager_Mirror.LastDisconnectionType = DisconnectionType.left;
                NetworkManager_Mirror.LeaveLobby(); //leave the lobby.
#endif
            }
        }
    }
}
