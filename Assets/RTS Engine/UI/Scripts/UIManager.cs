using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/* UI Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class UIManager : MonoBehaviour 
	{

#if UNITY_EDITOR
        public int tabID;
#endif

        //Winng and loosing menus:
        [SerializeField]
        private GameObject winMenu = null; //activated when the player wins the game.
        [SerializeField]
        private GameObject loseMenu = null; //activated when the player loses the game.
        [SerializeField]
        private GameObject pauseMenu = null; //activated when the player is pausing.
        [SerializeField]
        private GameObject freezeMenu = null; //activated when waiting for players to sync in a MP game.

        //toggle the pause menu here
        public void TogglePauseMenu ()
        {
            if (GameManager.GameState == GameState.running)
                GameManager.SetGameState(GameState.pause);
            else if (GameManager.GameState == GameState.pause)
                GameManager.SetGameState(GameState.running);
        }

        //enable a menu:
        private void UpdateMenu ()
        {
            winMenu.SetActive(GameManager.GameState == GameState.won);
            loseMenu.SetActive(GameManager.GameState == GameState.lost);
            pauseMenu.SetActive(GameManager.GameState == GameState.pause);
            freezeMenu.SetActive(GameManager.GameState == GameState.frozen);
        }

        [SerializeField]
        private TaskPanelUI taskPanel = new TaskPanelUI(); //the part that controls the task panel only.

        [SerializeField]
        private SingleSelectionPanelUI singleSelection = new SingleSelectionPanelUI(); //the part that controls the selection UI for a single unit, building and resource
        
        [SerializeField]
        private GridLayoutGroup multipleSelectionPanel = null; //unit multiple selection
        public Transform GetMultipleSelectionPanel() { return multipleSelectionPanel.transform; }

        [SerializeField]
        private int maxMultipleSelectionTasks = 10; //if the multiply selected entities is over this number, each type of the selected entities will have one task with the amount displayed

        public void UpdateMultipleSelectionPanel () //update the multiple selection panel to display the selected units group
        {
            //show the multiple selection menu:
            taskPanel.Hide(true);
            singleSelection.Hide();    
            multipleSelectionPanel.gameObject.SetActive (true);

            //if the amount of selected units is higher than the maximum allowed multiple selection tasks that represent each entity individually:
            if (gameMgr.SelectionMgr.Selected.Count > maxMultipleSelectionTasks)
            {
                //get the selected units in a form of a dictionary with each selected entity's code as key:
                Dictionary<string, List<Entity>> selectedEntities = gameMgr.SelectionMgr.Selected.GetEntitiesDictionary(EntityTypes.none, true, false);
                
                foreach(string code in selectedEntities.Keys)
                {
                    taskPanel.Add(new TaskUIAttributes
                    {
                        sourceList = selectedEntities[code],
                        type = TaskTypes.deselectMul,
                        icon = selectedEntities[code][0].GetIcon()
                    }, 0, TaskUI.Types.multipleSelectionMul);
                }
            }
            else
            {
                List<Entity> selectedEntities = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.none, true, false);

                foreach (Entity entity in selectedEntities)
                {
                    //only each seleced unit's icon.
                    taskPanel.Add(new TaskUIAttributes
                    {
                        source = entity,
                        type = TaskTypes.deselectIndiv,
                        icon = entity.GetIcon()

                    }, 0, TaskUI.Types.multipleSelectionIndiv);
                }
            }
        }

        public void HideMultipleSelectionPanel () //hide the multiple selection panel along with all the tasks in the selection panel
        {
            taskPanel.Hide(true);
            multipleSelectionPanel.gameObject.SetActive(false);
        }

        [SerializeField]
        private HoverHealthBarUI hoverHealthBar = new HoverHealthBarUI(); //a health bar that appears over units and buildings when the mouse is over them

        //a tool tip allows to display messages when hovering over tasks for example 
        [SerializeField]
        private GameObject tooltipPanel = null;
        [SerializeField]
        private Text tooltipText = null;

        //a method to enable the tooltip panel:
        public void ShowTooltip(string message)
        {
            tooltipPanel.SetActive(true); //activate the panel
            tooltipText.text = message; //display the message
        }

        //a method to hide the tooltip panel:
        public void HideTooltip()
        {
            tooltipPanel.SetActive(false); //hide the panel
            tooltipText.text = ""; //remove the last message
        }

        //Population UI:
        [SerializeField]
        private Text populationText = null; //a text that shows the faction's population
        
        //a method that allows to update the population UI count
        private void UpdatePopulationUI (FactionSlot factionSlot, int value)
		{
            if (factionSlot.PlayerControlled) //only if this is the local player's faction
                populationText.text = factionSlot.GetCurrentPopulation().ToString() + "/" + factionSlot.GetMaxPopulation().ToString();
		}

		//Player Message UI:
		public enum MessageTypes {error, info};
        [SerializeField]
        private Text playerMessageText = null;
        [SerializeField]
		private float playerMessageDuration = 3.0f; //how long does the player message shows for.
		float playerMessageTimer;
        private IEnumerator disablePlayerMessage;

		//a method that contrls showing messags to the player:
		public void ShowPlayerMessage(string message, MessageTypes type = MessageTypes.error)
		{
			playerMessageText.gameObject.SetActive (true);
			playerMessageText.text = message;

            //new disable player message coroutine
            if(disablePlayerMessage != null)
                StopCoroutine(disablePlayerMessage);

            disablePlayerMessage = DisablePlayerMessage();
            StartCoroutine(disablePlayerMessage);
		}

        private IEnumerator DisablePlayerMessage ()
        {
            yield return new WaitForSeconds(playerMessageDuration);

			playerMessageText.text = "";
		    playerMessageText.gameObject.SetActive (false);
        }

        [SerializeField]
        private PeaceTimeUI peaceTime = new PeaceTimeUI(); //display the peace time in the peace time UI panel
        public PeaceTimeUI PeaceTime
        {
            private set
            {
                peaceTime = value;
            }
            get
            {
                return peaceTime;
            }
        }

        //other components:
        GameManager gameMgr;

		public void Init (GameManager gameMgr)
		{
            //assign components:
            this.gameMgr = gameMgr;

            //init the UI components:
            taskPanel.Init(gameMgr);
            taskPanel.Hide();

            singleSelection.Init(this.gameMgr);
            singleSelection.Hide();

            hoverHealthBar.Init(this);

            //custom events to detect changes in the selection and task related elements:
            CustomEvents.GameStateUpdated += UpdateMenu;

            CustomEvents.CurrentPopulationUpdated += UpdatePopulationUI;
            CustomEvents.MaxPopulationUpdated += UpdatePopulationUI;

            CustomEvents.EntitySelected += OnEntitySelected;
            CustomEvents.EntityDeselected += OnEntityDeselected;

            CustomEvents.FactionEntityHealthUpdated += OnFactionEntityHealthUpdated;
		}

        //when this component is destroyed, stop listening to custom events
        private void OnDestroy()
        {
            //disable UI components:
            taskPanel.Disable();
            singleSelection.Disable();
            hoverHealthBar.Disable();

            CustomEvents.GameStateUpdated -= UpdateMenu;

            CustomEvents.CurrentPopulationUpdated -= UpdatePopulationUI;
            CustomEvents.MaxPopulationUpdated -= UpdatePopulationUI;

            CustomEvents.EntitySelected -= OnEntitySelected;
            CustomEvents.EntityDeselected -= OnEntityDeselected;

            CustomEvents.FactionEntityHealthUpdated -= OnFactionEntityHealthUpdated;
        }

        //called each time an entity is selected.
        private void OnEntitySelected (Entity entity)
        {
            taskPanel.Update(); //update the task panel first

            if(gameMgr.SelectionMgr.Selected.Count > 1) //more than one is selected
            {
                UpdateMultipleSelectionPanel(); //update the multiple entity selection
                return;
            }

            switch(entity.Type)
            {
                case EntityTypes.unit:
                    singleSelection.UpdateUnitUI((Unit)entity);
                    break;
                case EntityTypes.building:
                    singleSelection.UpdateBuildingUI((Building)entity);
                    break;
                case EntityTypes.resource:
                    singleSelection.UpdateResourceUI((Resource)entity);
                    break;
            }
        }

        //called each time an entity is deselected
        private void OnEntityDeselected(Entity entity)
        {
            int selectionCount = gameMgr.SelectionMgr.Selected.Count;

            if (selectionCount > 1) //more than one is selected
            {
                UpdateMultipleSelectionPanel(); //update the multiple selection
                taskPanel.Update(); //and update thas task panel
            }
            else if (selectionCount == 1) //only one entity left
                OnEntitySelected(gameMgr.SelectionMgr.Selected.GetSingleEntity(EntityTypes.none, false)); //display its single selection panel
            else //none left
                Hide(); //hide everything
        }

        //health related events:
        private void OnFactionEntityHealthUpdated (FactionEntity factionEntity, int value, FactionEntity source)
        {
            hoverHealthBar.Update(factionEntity); //update the hover health bar in case this unit is selected

            if (factionEntity.GetSelection().IsSelectedOnly) //if this is the only selected entity
                singleSelection.UpdateFactionEntityHealthUI(factionEntity); //update the health UI.
        }

        //hide selection related panels
        public void Hide ()
        {
            taskPanel.Hide();
            singleSelection.Hide();
            HideMultipleSelectionPanel();
            HideTooltip();
        }
    }
}