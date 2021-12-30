using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

//Singleplayer Lobby Manager:

namespace RTSEngine
{
    /* 
     * unlocked: player is able to change the game and map settings and add/remove factions.
     * startingGame: player presses the "Start Game" button after all conditions are met and the game starts (scene loaded) after a reload time.
     * */
    public enum LobbyStatus {unlocked, startingGame};

    public class LobbyManager : MonoBehaviour
    {
        public LobbyStatus Status { private set; get; }

        public static LobbyManager instance = null;
       
        [SerializeField]
        private LobbyFaction lobbyFactionPrefab = null; //lobby faction prefab, cloned for each faction slot in the lobby
        [SerializeField, Min(1)]
        private int minLobbyFactions = 2; //min amount of lobby factions required to start a game

        public List<LobbyFaction> LobbyFactions { private set; get; } //all the lobby faction components are stored in this list

        [SerializeField]
        private FactionColorMenu factionColor = new FactionColorMenu();
        public FactionColorMenu FactionColor { private set { factionColor = value; } get { return factionColor; } }

        [SerializeField]
        private MapMenu[] availableMaps = new MapMenu[0]; //a list of the game maps that the host can pick for a game
        public MapMenu GetCurrentMap() { return availableMaps[CurrentMapID]; }
        public int CurrentMapID { private set; get; } //the ID of the currently chosen map.

        public List<string> GetMapFactionTypeNames() //get the faction type names of a certain map
        {
            return availableMaps[CurrentMapID].GetFactionTypeNames();
        }

        public FactionTypeInfo GetFactionTypeInfo(int index) //get the faction type info with a certain index
        {
            return availableMaps[CurrentMapID].GetFactionTypeInfo(index);
        }

        [SerializeField]
        private NPCTypeMenu npcTypes = new NPCTypeMenu();
        public NPCTypeMenu NPCTypes { private set { npcTypes = value; } get { return npcTypes; } }

        [SerializeField, Tooltip("Time (in seconds) to wait after the 'Start Game' button is pressed when all game start conditions before the map scene is loaded")]
        private float startGameDelay = 1.0f;
        private float startGameDelayTimer;

        [SerializeField, Tooltip("Triggered when the player clicks on the 'Start Game' button when all game start conditions are met.")]
        private UnityEvent startGameEvent;

        //other components:
        public LobbyManagerUI UIMgr { private set; get; }

        private void Awake()
        {
            //make sure only one instancce of this component is available
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Destroy(this);
                return;
            }

            Status = LobbyStatus.unlocked;

            Time.timeScale = 1.0f; //When the game ends, the time scale is set to 0.0f, we reset it now in case we're coming from a finished game

            LobbyFactions = new List<LobbyFaction>(); //clear the lobby factions list

            npcTypes.Validate("LobbyManager");

            Assert.IsTrue(availableMaps.Length > 0, "[LobbyManager] At least one map has to be assigned.");

            CurrentMapID = 0; //set the map ID to 0 by default.
            List<string> mapNames = new List<string>(); //this list will hold all map names

            for (int i = 0; i < availableMaps.Length; i++) //go through all available maps
            {
                availableMaps[i].Validate(i, "LobbyManager"); //see if their attributes are valid or not
                mapNames.Add(availableMaps[i].GetName()); //add the map name to the list
            }

            UIMgr = GetComponent<LobbyManagerUI>();
            UIMgr.Init(this, mapNames);
            UIMgr.UpdateMapDropDownMenu(mapNames); //to show the available map names in the map drop down menu

            DontDestroyOnLoad(this.gameObject); //we need this component to pass to the map scene

            //add first local faction for the player and the rest of min lobby factions as NPC factionsA
            for(int i = 0; i < Mathf.Max(GetCurrentMap().GetMinFactions(), minLobbyFactions); i++)
                AddFaction();
        }

        //add a new faction to the lobby
        public void AddFaction()
        {
            if (Status != LobbyStatus.unlocked)
                return;

            if(LobbyFactions.Count >= GetCurrentMap().GetMaxFactions()) //make sure we haven't reached the max factions yet
            {
                UIMgr.ShowInfoMessage($"Maximum factions amount {GetCurrentMap().GetMaxFactions()} has been reached.");
                return;
            }

            LobbyFaction newFaction = Instantiate(lobbyFactionPrefab.gameObject).GetComponent<LobbyFaction>();
            UIMgr.SetLobbyFactionParent(newFaction.transform);

            LobbyFactions.Add(newFaction);
            newFaction.Init(this, LobbyFactions.Count > 1 ? false : true); //make sure one faction only is player controlled

            UIMgr.ShowInfoMessage($"New faction slot added.");
        }

        //remove a faction instance from the lobby
        public void RemoveFaction (LobbyFaction instance)
        {
            if (Status != LobbyStatus.unlocked)
                return;

            if(LobbyFactions.Count <= Mathf.Max(GetCurrentMap().GetMinFactions(), minLobbyFactions)) //make sure there's more than the min allowed amount of lobby factions
            {
                UIMgr.ShowInfoMessage($"Can not have less than {Mathf.Max(GetCurrentMap().GetMinFactions(), minLobbyFactions)} factions in lobby for this map.");
                return;
            }

            LobbyFactions.Remove(instance);
            Destroy(instance.gameObject);

            UIMgr.ShowInfoMessage($"Faction slot removed.");
        }

        //update the map info when it's changed by the player:
        public void OnMapUpdated ()
        {
            if (Status != LobbyStatus.unlocked)
                return;

            int nextMapID = UIMgr.GetMapDropDownValue();
            if (nextMapID == CurrentMapID) //if map hasn't changed
                return;

            CurrentMapID = nextMapID;
            UIMgr.UpdateMapUIInfo();

            //remove excess factions:
            while (LobbyFactions.Count > GetCurrentMap().GetMaxFactions())
                RemoveFaction(LobbyFactions[LobbyFactions.Count - 1]);

            foreach (LobbyFaction lobbyFaction in LobbyFactions) //reset the faction types for the new map
                lobbyFaction.ResetFactionType();
        }

        public void StartGame()
        {
            if (Status != LobbyStatus.unlocked)
                return;

            Status = LobbyStatus.startingGame;
            startGameDelayTimer = startGameDelay;

            startGameEvent.Invoke();

            UIMgr.ShowInfoMessage($"Starting game...");
        }

        private void Update()
        {
            if (Status == LobbyStatus.unlocked)
                return;

            if(startGameDelayTimer > 0.0f)
                startGameDelayTimer -= Time.deltaTime;
            else
            {
                SceneManager.LoadScene (GetCurrentMap().GetScene());
            }
        }
    }
}
