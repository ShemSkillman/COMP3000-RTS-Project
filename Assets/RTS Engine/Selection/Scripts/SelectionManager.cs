using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Linq;

/* Selection Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class SelectionManager : MonoBehaviour
    {
        //double selection range:
        [SerializeField]
        private float doubleClickSelectRange = 10.0f;

        //idle units selection options
        [System.Serializable]
        public struct IdleUnitsSelection
        {
            public bool enabled; //true to enable idle unit selection
            public KeyCode key; //key code that selects idle units
            public bool workersOnly; //when selecting idle units, should only builders and resource collectors be selected?
        }
        [SerializeField]
        private IdleUnitsSelection idleUnitsSelection = new IdleUnitsSelection();

        [SerializeField]
        private KeyCode multipleSelectionKey = KeyCode.LeftControl; //key used to select multiple units.
        public bool MultipleSelectionKeyDown { private set; get; }

        [SerializeField]
        private SelectionBox box = new SelectionBox(); //manages the selection box
        public SelectionBox Box
        {
            private set {
                box = value;
            }
            get
            {
                return box;
            }
        }

        [SerializeField]
        private SelectedEntities selected = new SelectedEntities();
        public SelectedEntities Selected
        {
            private set
            {
                selected = value;
            }
            get
            {
                return selected;
            }
        }

        [SerializeField]
        private Color freeSelectionColor = Color.grey;
        public Color GetFreeSelectionColor() { return freeSelectionColor; }

        //some attributes required to select entities:
        [SerializeField]
        private LayerMask rayLayerMask; //layers which the selection manager can detect to perform entity selections
        RaycastHit rayHit;
        Ray rayCheck;

        //when enabled, player will be able to use the below key to follow a selected entity using the camera
        [System.Serializable]
        public struct CameraFollow
        {
            public bool enabled;
            public KeyCode key;
            public bool iterate; //when enabled, player will be able to iterate through the selected entities and camera follow them one by one.
        }
        [SerializeField]
        private CameraFollow camFollow = new CameraFollow { enabled = true, key = KeyCode.Space, iterate = true };
        private Entity camFollowEntity = null; //holds the entity that the camera is following
        private int nextCamFollowID = 0; //used to know the next selected entity id to camera follow next
        private List<Entity> camFollowEntities = new List<Entity>(); //holds a list of the selected entities to iterate through them on camera follow

        [SerializeField]
        private float flashTime = 1.0f; //This is the total time at which the building/resource/unit selection texture will be chosen after the player sent units to construct/collect it.
        [SerializeField]
        private Color friendlyFlashColor = Color.green; //The color of a friendly building/resource/unit selection texture when it starts flashing.
        [SerializeField]
        private Color enemyFlashColor = Color.red; //the color of an enemy building/unit selection texture when it's flashing due to an attack order
        [SerializeField]
        private float flashRepeatTime = 0.2f; //The selection texture flash repeat time.

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            box.Init(this, gameMgr);
            selected.Init(this);

            MultipleSelectionKeyDown = false;

            //listen to events
            CustomEvents.EntityDeselected += OnEntityDeselected;
        }

        private void OnDisable () 
        {
            CustomEvents.EntityDeselected -= OnEntityDeselected;
        }

        //called each time an entity is deselected
        private void OnEntityDeselected (Entity entity)
        {
            if (entity == camFollowEntity) //if this is camera follow entity
                ResetCameraFollow();
        }

        void Update()
        {
            //If the game is not running or the player is currently placing a building
            if (GameManager.GameState != GameState.running || gameMgr.PlacementMgr.IsBuilding())
                return; //don't proceed

            UpdateCameraFollow(); //following entities with the camera

            //Checking if the selection key is held down or not!
            MultipleSelectionKeyDown = Input.GetKey(multipleSelectionKey);

            box.Update(); //update the selection box

            if (Input.GetKeyDown(idleUnitsSelection.key)) //if the player clicks the idle unit selection key
                SelectIdleUnits();

            //did the player just press one of these buttons?
            bool leftButtonDown = Input.GetMouseButtonDown(0);
            bool rightButtonDown = Input.GetMouseButtonDown(1);
            
            //if the mouse pointer is over a UI element or the minimap, we will not detect entity selections. Also make sure that one of the mouse buttons are down
            if (EventSystem.current.IsPointerOverGameObject() 
                || gameMgr.CamMgr.MinimapCameraController.IsMouseOverMinimap(out RaycastHit hit) || (!leftButtonDown && !rightButtonDown))
                return;

            rayCheck = Camera.main.ScreenPointToRay(Input.mousePosition); //create a ray on the main camera from the mouse position
            if(Physics.Raycast(rayCheck, out rayHit, Mathf.Infinity, rayLayerMask.value))
            {
                SelectionEntity hitSelection = rayHit.transform.gameObject.GetComponent<SelectionEntity>(); //see if we have hit a selection entity
                bool hitTerrain = gameMgr.TerrainMgr.IsTerrainTile(rayHit.transform.gameObject); //did we hit the terrain?
                
                if(rightButtonDown) //right mouse button is down, this is an action on the hit selection entity
                {
                    if (gameMgr.TaskMgr.UnitComponent.AwaitingTaskType != TaskTypes.none)
                    { //if we click with the right mouse button while having an awaiting component task..
                        gameMgr.TaskMgr.UnitComponent.ResetAwaitingTaskType(); //reset it.
                        return;
                    }

                    if (hitSelection) //if a selection entity was hit
                    {
                        hitSelection.OnAction(TaskTypes.none); //trigger action
                    }
                    else if (hitTerrain) //no selection entity was hit but the terrain was hit
                        OnSelectedUnitsAction(rayHit.point, TaskTypes.none); //either move selected units or launch a terrain attack

                    Building selectedBuilding = (Building)selected.GetSingleEntity(EntityTypes.building, true); //get the single selected player faction building
                    if (selectedBuilding != null && (hitSelection != null || hitTerrain)) //valid player faction building? update rally point if we hit something or we hit the terrain
                        selectedBuilding.UpdateRallyPoint(rayHit.point, hitSelection?.Source);
                }
                else if(leftButtonDown) //if the left mouse button is down then this a selection action
                {
                    if(hitSelection == null) //no selection entity hit
                    {
                        TaskTypes currTask = gameMgr.TaskMgr.UnitComponent.AwaitingTaskType;

                        //if terrain was not hit or it was hit but there's no selected unit action
                        if (!hitTerrain 
                            || (currTask != TaskTypes.movement && currTask != TaskTypes.attack)
                            || !OnSelectedUnitsAction(rayHit.point, currTask) )
                            selected.RemoveAll(); //player is just clicking to clear selection

                        gameMgr.TaskMgr.UnitComponent.ResetAwaitingTaskType(); //& reset awaiting task type
                        return;
                    }

                    if (gameMgr.TaskMgr.UnitComponent.AwaitingTaskType != TaskTypes.none) //if there's a pending unit component task
                    {
                        hitSelection.OnAction(gameMgr.TaskMgr.UnitComponent.AwaitingTaskType); //trigger action
                        gameMgr.TaskMgr.UnitComponent.ResetAwaitingTaskType();
                    }
                    else //no pending unit component task -> select
                        selected.Add(hitSelection.Source, SelectionTypes.single); //select entity
                }
            }
        }

        //monitor the player input to follow selected entities:
        private void UpdateCameraFollow ()
        {
            if (!camFollow.enabled) //if this feature is disabled, stop here.
                return;

            if(Input.GetKeyDown(camFollow.key) && selected.Count > 0 && !gameMgr.CamMgr.IsPanning()) 
                //if the camera follow has been pressed by the player, there are entities selected and the camera is not being moved by the player
            {
                if (!gameMgr.CamMgr.IsFollowingTarget()) //if the camera wasn't following a target
                    ResetCameraFollow();

                if (nextCamFollowID >= selected.Count) //if the selected entities is now too small
                    nextCamFollowID = 0; //reset the identifier for the next camera follow

                //if (nextCamFollowID == 0) //if this is the first time the player is attempting to camera follow a selected entity*/

                camFollowEntities = selected.GetEntitiesList(EntityTypes.none, false, false); //get the currently selected entities

                camFollowEntity = camFollowEntities[nextCamFollowID];

                gameMgr.CamMgr.SetFollowTarget(camFollowEntity.transform); //set the next target to follow

                if (camFollow.iterate) //if the iteration feature is enabled
                {
                    nextCamFollowID++;
                    if (nextCamFollowID >= camFollowEntities.Count) //reset identifier if we reached the end of the list
                        nextCamFollowID = 0;
                }
            }
        }

        //used to reset the camera follow
        public void ResetCameraFollow ()
        {
            //reset camera follow
            nextCamFollowID = 0;
            camFollowEntity = null;
            gameMgr.CamMgr.SetFollowTarget(null);
        }

        //move a list of selected units
        private bool OnSelectedUnitsAction (Vector3 targetPosition, TaskTypes taskType)
        {
            List<Unit> selectedUnits = selected.GetEntitiesList(EntityTypes.unit, false, true).Cast<Unit>().ToList(); //get selected units from player faction
            if (selectedUnits.Count == 0) //do not proceed if no units are selected
                return false;

            if (taskType == TaskTypes.attack) //if this is an attack task type
            {
                gameMgr.AttackMgr.LaunchTerrainAttack(selectedUnits, targetPosition, true); //only check for launching terrain attack
                return true;
            }

            //if this is a movemen task or it's no assigned task type but terrain attack is not launched 
            if (taskType == TaskTypes.movement 
                || (taskType == TaskTypes.none && !gameMgr.AttackMgr.LaunchTerrainAttack(selectedUnits, targetPosition)) )
            {
                gameMgr.MvtMgr.Move(selectedUnits, targetPosition, 0.0f, null, InputMode.movement, true); //move selected units
                return true;
            }

            return false;
        }
        
        //Flash an object's selection plane (friendly or enemy flash):
        public void FlashSelection(Entity entity, bool isFriendly)
        {
            if (entity == null)
                return;

            Color color = (isFriendly == true) ? friendlyFlashColor : enemyFlashColor; //the color to use. enemy or friendly flash?
            entity.EnableSelectionFlash(flashTime, flashRepeatTime, color);
        }

        //a method that selects the local player faction's idle units
        public void SelectIdleUnits () //if worker only is set to true then only units with the builder or the resource collector components will be selected
        {
            selected.RemoveAll(); //deselect all currently selected entities.
            foreach(Unit unit in GameManager.PlayerFactionMgr.GetUnits()) //go through all units in player faction
            {
                //if the unit is idle and check whether we have to select workers only (builder & collectors) or not
                if (unit.IsIdle() == true && (idleUnitsSelection.workersOnly == false || unit.BuilderComp != null || unit.CollectorComp))
                    selected.Add(unit, SelectionTypes.multiple);
            }
        }
        
        //a method to select faction entities of the same type inside a defined range
        public void SelectFactionEntitiesInRange(FactionEntity source)
        {
            if (source.FactionID != GameManager.PlayerFactionID) //if the source doesn't belong to the player's faction
                return; //nope

            foreach(FactionEntity entity in GameManager.PlayerFactionMgr.GetFactionEntities()) //go through all faction entities in the player's faction
            {
                //if the entity's codes match and it's inside the double click select range
                if(entity.GetCode() == source.GetCode() 
                    && Vector3.Distance(entity.transform.position, source.transform.position) <= doubleClickSelectRange)
                {
                    selected.Add(entity, SelectionTypes.multiple); //add to selection
                }
            }
        }

        //a method that checks the selection status of an entity
        public static bool IsSelected (SelectionEntity selectionEntity, bool only, bool playerFaction)
        {
            return selectionEntity != null //valid selection entity
                && selectionEntity.IsSelected //must be selected
                && (only == false || selectionEntity.IsSelectedOnly == true) //must it be only selected?
                //and finally check for the player faction
                && (playerFaction == false || selectionEntity.FactionEntity == null || selectionEntity.FactionEntity.FactionID == GameManager.PlayerFactionID);
        }
    }
}