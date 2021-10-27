using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/* Input Manager: script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField]
        private bool snapDistanceEnabled = true; //enable the snap distance feature
        [SerializeField]
        private float snapDistance = 0.5f; //max distance allowed between a unit's current position and its initial position in an input before it gets snapped.

        private static List<Entity> spawnablePrefabs = new List<Entity>(); //what prefabs can this component actually create in a multiplayer game?

        private static Dictionary<int, Entity> spawnedObjects = new Dictionary<int, Entity>(); //the objects spawned by this component are saved in here.
        private int lastKey = 0;
        public int RegisterObject (Entity newEntity) {
            //add the new entity with its key
            spawnedObjects.Add(lastKey, newEntity);
            lastKey++; //increase the key to be used for the next entity
            return lastKey - 1; //return the key for the last added entity
        }

        //Mirror:
#if RTSENGINE_MIRROR
        public static NetworkFactionManager_Mirror FactionManager_Mirror { set; get; } //the local player's network faction manager component
#endif

        //other components
        GameManager gameMgr;

        public void Init (GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            spawnedObjects.Clear(); //the spawned objects list is empty by default.
            spawnablePrefabs.Clear();
        }

        /// <summary>
        /// Assigns the list of possible spawnable prefabs and initiliazes default faction entities in a multiplayer game.
        /// </summary>
        public void OnFactionSlotsInitialized ()
        {
            if (!GameManager.MultiplayerGame) //if this is not a multiplayer game, stop here
            {
                Destroy(this);
                return;
            }

#if RTSENGINE_MIRROR
            if(gameMgr.NetworkManager_Mirror)
                spawnablePrefabs = gameMgr.NetworkManager_Mirror.spawnablePrefabs;
#endif

            //spawn the faction entities:
            foreach (FactionSlot faction in gameMgr.GetFactions())
                faction.SpawnFactionEntities(gameMgr);
        }

        //this is the only way to communicate between objects in the scene and multiplayer faction managers.
        public static void SendInput(NetworkInput newInput, Entity source, Entity target)
        {
            if (source) //if there's a source object
            {
                if (newInput.sourceMode == (byte)InputMode.create) //if we're creating an object, then look in the spawnable prefabs list
                    newInput.sourceID = spawnablePrefabs.IndexOf(source); //get the index of the prefab from the spawnable prefabs list as the source ID
                else //for the rest of input source modes, get the ID from the spawned objects list
                    newInput.sourceID = source.MultiplayerKey;
            }
            else
                newInput.sourceID = -1; //no source object

            if (target) //if there's a valid target object
                newInput.targetID = target.MultiplayerKey; //get its index from the spawn objects and set it as the target ID.
            else
                newInput.targetID = -1; //no target object assigned

            newInput.factionID = GameManager.PlayerFactionID; //source of the input is the local player's faction

#if RTSENGINE_MIRROR
            if (FactionManager_Mirror != null) //network faction manager hasn't been assigned yet
                FactionManager_Mirror.CmdSendInput(newInput); //send the input
#endif
        }

        //a method called to execute commands (collected inputs) sent by the host/server
        public void LaunchCommand(NetworkInput command)
        {
            switch ((InputMode)command.sourceMode)
            {
                case InputMode.create:
                    OnCreateCommand(command);
                    break;
                case InputMode.destroy:
                    OnDestroyCommand(command);
                    break;
                case InputMode.factionEntity:
                    OnFactionEntityCommand(command);
                    break;
                case InputMode.unitGroup:
                    OnUnitGroupCommand(command);
                    break;
                case InputMode.unit:
                    OnUnitCommand(command);
                    break;
                case InputMode.building:
                    OnBuildingCommand(command);
                    break;
                case InputMode.resource:
                    OnResourceCommand(command);
                    break;
                case InputMode.APC:
                    OnAPCCommand(command);
                    break;
                case InputMode.customCommand:
                    CustomEvents.OnCustomCommand(command);
                    break;
                default:
                    Debug.LogError("[Input Manager] Invalid input source mode!");
                    break;
            }
        }

        //execute a command that creates a faction entity (unit/building):
        private void OnCreateCommand(NetworkInput command)
        {
            Entity prefab = spawnablePrefabs[command.sourceID]; //the prefab to spawn.

            switch ((InputMode)command.targetMode)
            {
                case InputMode.unit:

                    Entity creator = null; //get the creator building
                    spawnedObjects.TryGetValue(command.targetID, out creator);
                    gameMgr.UnitMgr.CreateUnitLocal(prefab as Unit, command.initialPosition, prefab.transform.rotation, command.targetPosition, command.factionID, (creator as Building), command.value == 0, command.value == 2);

                    break;
                case InputMode.building:

                    /*
             * 0 -> PlacedByDefault = false & Capital = false
             * 1 -> PlacedByDefault = true & Capital = false
             * 2 -> PlacedByDefault = false & Capital = true
             * 3 -> PlacedByDefault = true & Capital = true
             * */
                    //determine whether the building will be placed by default or if it's a capital building.
                    bool placedByDefault = (command.value == 1 || command.value == 3) ? true : false;
                    bool isCapital = (command.value == 2 || command.value == 3) ? true : false;

                    Entity center = null; //get the building's center border component
                    spawnedObjects.TryGetValue(command.targetID, out center);

                    gameMgr.BuildingMgr.CreatePlacedInstanceLocal(prefab as Building, command.initialPosition, command.targetPosition.y, 
                        (center as Building)?.BorderComp, command.factionID, placedByDefault, isCapital);
                    break;

                case InputMode.resource:
                    gameMgr.ResourceMgr.CreateResourceLocal(prefab as Resource, command.initialPosition);
                    break;
                default:
                    Debug.LogError("[Input Manager] Invalid input target mode for creation command.");
                    break;
            }
        }

        //execute a command that destroys a faction entity or a whole faction
        private void OnDestroyCommand(NetworkInput command)
        {
            switch ((InputMode)command.targetMode)
            {
                case InputMode.factionEntity: //destroying a unit
                    (spawnedObjects[command.sourceID] as FactionEntity).EntityHealthComp.DestroyFactionEntityLocal(command.value == 1 ? true : false);
                    break;
                case InputMode.resource: //destroy a resource and providing the last resource collector as a parameter (if it exists).
                    (spawnedObjects[command.sourceID] as Resource).DestroyResourceLocal(command.targetID >= 0 ? spawnedObjects[command.targetID] as Unit : null);
                    break;
                case InputMode.faction:

                    gameMgr.OnFactionDefeatedLocal(command.value); //the command.value holds the faction ID of the faction to destroy

                    if (GameManager.PlayerFactionID == GameManager.HostFactionID) //if this is the host
                    {
#if RTSENGINE_MIRROR
                        FactionManager_Mirror.OnFactionDefeated(command.value); //to mark the defeated player as disconnected.
#endif
                    }
                    break;
                default:
                    Debug.LogError("[Input Manager] Invalid input target mode for destroy command.");
                    break;
            }
        }

        //execute a unit related command
        public void OnFactionEntityCommand(NetworkInput command)
        {
            FactionEntity sourceFactionEntity = spawnedObjects[command.sourceID] as FactionEntity; //get the source faction entity
            Entity target = null; //get the target obj
            spawnedObjects.TryGetValue(command.targetID, out target);

            switch ((InputMode)command.targetMode)
            {
                case InputMode.health: //adding health

                    sourceFactionEntity.EntityHealthComp.AddHealthLocal(command.value, (target == null) ? null : target as FactionEntity);
                    break;
                case InputMode.multipleAttack:  //switching attack types.

                    sourceFactionEntity.MultipleAttackMgr.SetTargetLocal(command.code);
                    break;
            }
        }

        //execute a unit group movement command
        public void OnUnitGroupCommand(NetworkInput command)
        {
            List<Unit> unitList = StringToUnitList(command.code); //get the units list
            spawnedObjects.TryGetValue(command.targetID, out Entity target); //attempt to get the target Entity instance for this unit command

            if (unitList.Count > 0) //if there's actual units in the list
            {
                switch((InputMode)command.targetMode)
                {
                    case InputMode.attack:

                         //if the target mode is attack -> make the unit group launch an attack on the target.
                        gameMgr.AttackMgr.LaunchAttackLocal(unitList, target as FactionEntity, command.targetPosition, command.playerCommand);
                        break;

                    default:

                        gameMgr.MvtMgr.MoveLocal(unitList, command.targetPosition, command.value, target, (InputMode)command.targetMode, command.playerCommand);
                        break;
                }
            }
        }

        //execute a unit related command
        public void OnUnitCommand(NetworkInput command)
        {
            Unit sourceUnit = spawnedObjects[command.sourceID] as Unit; //get the source unit

            if (snapDistanceEnabled && sourceUnit.MovementComp.IsMoving == false && //if the unit is not moving
                Vector3.Distance(sourceUnit.transform.position, command.initialPosition) > snapDistance) //snap distance if the unit's current position has moved too far from the specified initial position
                sourceUnit.transform.position = command.initialPosition; //snap the unit's position

            spawnedObjects.TryGetValue(command.targetID, out Entity target); //attempt to get the target Entity instance for this unit command

            switch ((InputMode)command.targetMode)
            {
                case InputMode.heal: //healing the target

                    sourceUnit.HealerComp.SetTargetLocal(target as Unit); //heal target unit
                    break;

                case InputMode.convertOrder: //ordering the unit to convert

                    sourceUnit.ConverterComp.SetTargetLocal(target as Unit);
                    break;

                case InputMode.convert: //unit is getting converted

                    sourceUnit.ConvertLocal(target as Unit, command.value);
                    break;

                case InputMode.builder: //targetting a building

                    sourceUnit.BuilderComp.SetTargetLocal(target as Building);
                    break;

                case InputMode.collect: //collecting a resource

                    sourceUnit.CollectorComp.SetTargetLocal(target as Resource);
                    break;

                case InputMode.dropoff: //dropping off a resource

                    sourceUnit.CollectorComp.SendToDropOffLocal();
                    break;

                case InputMode.unitEscape:

                    sourceUnit.EscapeComp.TriggerLocal(command.targetPosition);
                    break;

                case InputMode.attack:

                    gameMgr.AttackMgr.LaunchAttackLocal(sourceUnit, target as FactionEntity, command.targetPosition, command.playerCommand);
                    break;

                default: //rest of the target modes for the unit commands are movement commands with different target modes.

                    gameMgr.MvtMgr.MoveLocal(sourceUnit, command.targetPosition, command.extraPosition.x, target, (InputMode)command.targetMode, command.playerCommand);
                    break;
            }
        }

        //execute a building related command:
        private void OnBuildingCommand(NetworkInput command)
        {
            Building sourceBuilding = spawnedObjects[command.sourceID] as Building; //get the source building
            Entity target = null; //get the target obj
            spawnedObjects.TryGetValue(command.targetID, out target);

            switch ((InputMode)command.targetMode)
            {
                case InputMode.attack: //attacking a target

                    sourceBuilding.AttackComp.SetTargetLocal(target ? target as FactionEntity : null, command.targetPosition);
                    break;
            }
        }

        //execute a resource related command:
        private void OnResourceCommand(NetworkInput command)
        {
            Resource sourceResource = spawnedObjects[command.sourceID] as Resource; //get the source resource

            if (spawnedObjects.TryGetValue(command.targetID, out Entity target)) //only if there's a target entity
            {
                switch ((InputMode)command.targetMode)
                {
                    case InputMode.health: //adding/removing an amount from the resource

                        sourceResource.AddAmountLocal(command.value, target as Unit);
                        break;
                }
            }
        }

        //execute a APC related command:
        private void OnAPCCommand(NetworkInput command)
        {
            APC sourceAPC = (spawnedObjects[command.sourceID] as FactionEntity).APCComp; //get the source resource

            switch ((InputMode)command.targetMode)
            {
                case InputMode.APCEjectAll: //ejecting all units from the APC

                    sourceAPC.EjectAllLocal(command.value == 1);
                    break;

                case InputMode.APCEject:

                    Unit targetUnit = (command.targetID >= 0 && command.targetID < spawnedObjects.Count) ? spawnedObjects[command.targetID] as Unit : null; //get the target unit
                    sourceAPC.EjectLocal(targetUnit, command.value == 1);
                    break;


            }
        }

        //convert a unit list into a string.
        public static string UnitListToString(List<Unit> unitList)
        {
            string resultString = "";
            foreach (Unit unit in unitList) //go through the unit list
                //get the ID of each unit in the list and add it to the string
                resultString += unit.MultiplayerKey.ToString() + ",";

            return resultString.TrimEnd(','); //trim the last ',' and voila.
        }

        //convert a string containing the IDs of units into a unit list:
        public static List<Unit> StringToUnitList(string inputString)
        {
            List<Unit> unitList = new List<Unit>();

            string[] unitKeys = inputString.Split(','); //get the unit indexes into a string array
            foreach (string key in unitKeys) //go through all the indexes
            {
                if (spawnedObjects.TryGetValue(Int32.Parse(key), out Entity entity))
                    unitList.Add(entity as Unit);
                 //add the unit that matches the index to the list
            }

            return unitList;
        }
    }
}