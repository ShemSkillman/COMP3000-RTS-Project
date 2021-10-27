using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* AttackManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// 
    /// </summary>
    public enum LaunchAttackMode { none, change, full, assigned }

    public class AttackManager : MonoBehaviour
    {
        //when attack units which do not require a target are selected and the following key is held down by the player, a terrain attack can be launched
        [SerializeField]
        private bool terrainAttackEnabled = true;
        [SerializeField]
        private KeyCode terrainAttackKey = KeyCode.T;

        private GameManager gameMgr;

        public void Init (GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        //a method called to launch a terrain attack
        public bool LaunchTerrainAttack (List<Unit> units, Vector3 attackPosition, bool direct = false)
        {
            //when direct is set to true, it will ignore whether or not the player is holding down the terrain attack key
            //if the terrain attack feature is disabled or the trigger key isn't pressed by the player
            if (!terrainAttackEnabled || ( !direct && !Input.GetKey(terrainAttackKey) ))
                return false;

            //get the units which do have an attack component and which do not require a target to be assigned
            units = units.Where(unit => IsAttackUnit(unit) && !unit.AttackComp.RequireTarget()).ToList();

            if (units.Count > 0) //if there are still units allowed to launch a terrain attack
            {
                LaunchAttack(units, null, attackPosition, true);
                return true;
            }

            return false;
        }

        public ErrorMessage LaunchAttack(List<Unit> units, FactionEntity targetEntity, Vector3 targetPosition, bool playerCommand)
        {
            //make sure that the input list is not empty
            if (units.Count == 0)
            {
                Debug.LogWarning("[AttackManager] Attempting to launch an attack with an empty list of units is not allowed!");
                return ErrorMessage.invalid;
            }
            else if (units.Count == 1) //we have a dedicated method for launching an attack with one unit.
                return LaunchAttack(units[0], targetEntity, targetPosition, playerCommand);

            //if the source unit is not part of the player's faction then, override the playerCommand value to false even if it was set to true
            if (playerCommand && !RTSHelper.IsPlayerFaction(units[0])) 
                playerCommand = false;

            if (!GameManager.MultiplayerGame) //single player game, directly launch the attack.
                return LaunchAttackLocal(units, targetEntity, targetPosition, playerCommand);
            else
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unitGroup,
                    targetMode = (byte)InputMode.attack,
                    targetPosition = targetPosition,
                    code = InputManager.UnitListToString(units),
                    playerCommand = playerCommand
                };

                //sent input
                InputManager.SendInput(newInput, null, targetEntity);

                return ErrorMessage.requestRelayed;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="units"></param>
        /// <param name="targetEntity"></param>
        /// <param name="targetPosition"></param>
        /// <param name="playerCommand"></param>
        public ErrorMessage LaunchAttackLocal(List<Unit> units, FactionEntity targetEntity, Vector3 targetPosition, bool playerCommand)
        {
            //sort the attack units based on their codes, we assume that units that share the same code (which is the defining property of an entity in the RTS Engine) are identical.
            //and filter out any units that do not have an attack component.
            ChainedSortedList<string, Unit> sortedUnits = RTSHelper.SortEntitiesByCode<Unit>(units, IsAttackUnit);

            Unit refUnit = null; //unit where the attack order will be played from, if assigned.

            //for each unit type.
            foreach (List<Unit> unitSet in sortedUnits.Values)
            {
                //if the current unit type is unable to have the entity as the target, move to the next unit type list
                if (unitSet[0].AttackComp.IsTargetValid(targetEntity) != ErrorMessage.none)
                    continue;

                //generate movement path destinations for the current list of identical unit types:
                gameMgr.MvtMgr.GeneratePathDestination(
                    unitSet,
                    targetPosition,
                    unitSet[0].AttackComp.GetRange().Formation,
                    unitSet[0].AttackComp.GetRange().GetStoppingDistance(targetEntity, true), out List<Vector3> pathDestinations);

                //no valid path destinations generated? do not continue as there is nowhere to move to
                if (pathDestinations.Count == 0)
                    continue;

                int destinationID = 0; //index counter for the generated path destinations.
                foreach(Unit unit in unitSet) //go through the current unit list
                {
                    if (targetEntity != null) //if this attack movement is towards a target, pick the closest position to the target for each unit
                        pathDestinations = pathDestinations.OrderBy(pos => (pos - unit.transform.position).sqrMagnitude).ToList();

                    //if current unit is able to engage with its target using the computed path then move to the next path, if not, test the path on the next unit.
                    //the last argument of the SetTarget method is set to the playerCommand because we still want to move the units to computed attack position...
                    //...even if it is out of the attack range because the player issued the attack/movement command.
                    ErrorMessage errorMessage = unit.AttackComp.SetTargetLocal(targetEntity, targetPosition, pathDestinations[destinationID], playerCommand);
                    if(errorMessage == ErrorMessage.none || errorMessage == ErrorMessage.moveToTargetNoAttack)
                    {
                        if(refUnit == null) //assign the reference unit from which the attack order will be played.
                            refUnit = unit;

                        //only move to the next path destination if we're not attacking a valid target (terrain attack?), if not keep removing the first element of the list which was the closest to the last unit
                        if(targetEntity == null)
                            destinationID++;
                        else
                            pathDestinations.RemoveAt(0);

                        if (destinationID >= pathDestinations.Count) //no more paths to test, stop moving units to attack.
                            break;
                    }
                }
            }

            if (playerCommand && refUnit && RTSHelper.IsPlayerFaction(refUnit)) //if this is a player command and the unit belongs to the player's faction.
            {
                if (targetEntity == null) //no target? terrain attack, show the indicator.
                    gameMgr.MvtMgr.SpawnTargetEffect(false, targetPosition);

                gameMgr.AudioMgr.PlaySFX(refUnit.AttackComp.GetOrderAudio(), false); //play the attack order audio.

                playerCommand = false; //we only want to the play the audio clip and show the terrain indicator once for a valid unit
            }

            return ErrorMessage.none;
        }

        public ErrorMessage LaunchAttack(Unit unit, FactionEntity targetEntity, Vector3 targetPosition, bool playerCommand)
        {
            if(unit == null) //make sure that the source unit instance is valid
            {
                Debug.LogError("[AttackManager] Can not launch an attack with an invalid unit!");
                return ErrorMessage.invalid;
            }
            else if(!IsAttackUnit(unit)) //if the unit can not attack or it does not have an attack component at all.
                return ErrorMessage.canNotAttack;

            if (!RTSHelper.IsLocalPlayer(unit)) return ErrorMessage.notLocalPlayer; //only allow local player to launch this command.

            ErrorMessage errorMessage;
            if ((errorMessage = unit.AttackComp.IsTargetValid(targetEntity)) != ErrorMessage.none) //check whether the new target is valid for this attack type.
                return errorMessage;

            //if the source unit is not part of the player's faction then, override the playerCommand value to false even if it was set to true
            if (playerCommand && !RTSHelper.IsPlayerFaction(unit)) 
                playerCommand = false;

            if(!GameManager.MultiplayerGame) //singleplayer game -> directly set target locally.
                return LaunchAttackLocal(unit, targetEntity, targetPosition, playerCommand);
            else //multiplayer game
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)InputMode.attack,
                    initialPosition = unit.transform.position,
                    targetPosition = targetPosition,
                    playerCommand = playerCommand
                };

                //sent input
                InputManager.SendInput(newInput, unit, targetEntity);

                return ErrorMessage.requestRelayed;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="targetEntity"></param>
        /// <param name="targetPosition"></param>
        /// <param name="playerCommand"></param>
        public ErrorMessage LaunchAttackLocal(Unit unit, FactionEntity targetEntity, Vector3 targetPosition, bool playerCommand)
        {
            //if the attack order was issued by the local player and this is the local player's instance.
            if(playerCommand && RTSHelper.IsLocalPlayer(unit))
            {
                if (targetEntity == null) //no target? terrain attack, show the indicator.
                    gameMgr.MvtMgr.SpawnTargetEffect(false, targetPosition);

                gameMgr.AudioMgr.PlaySFX(unit.AttackComp.GetOrderAudio(), false); //play the attack order audio.
            }

            //calculate a target attack position and attempt to set a new attack target for the source unit.
            //and if the target has been successuflly set and this is a player command.
            return unit.AttackComp.SetTargetLocal(targetEntity, targetPosition, GetAttackPosition(unit, targetEntity, targetPosition), playerCommand); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="potentialTarget"></param>
        /// <param name="potentialTargetPosition"></param>
        /// <returns></returns>
        public Vector3 GetAttackPosition (Unit unit, FactionEntity potentialTarget, Vector3 potentialTargetPosition)
        {
            if(unit == null || !IsAttackUnit(unit)) //make sure that the source unit instance is valid
            {
                Debug.LogError("[AttackManager] Can not calculate an attack position with an invalid unit instance or a non attack unit!");
                return Vector3.positiveInfinity;
            }

            //generate movement attack path destination for the new target.
            gameMgr.MvtMgr.GeneratePathDestination(
                unit,
                potentialTargetPosition,
                unit.AttackComp.GetRange().GetStoppingDistance(potentialTarget, true), out List<Vector3> pathDestinations);

            if(pathDestinations.Count > 0) //if there's a valid attack movement destination produced
                //get the closest target position
                return pathDestinations.OrderBy(pos => (pos - unit.transform.position).sqrMagnitude).First();

            return Vector3.positiveInfinity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static bool IsAttackUnit (Unit unit)
        {
            return unit.AttackComp && unit.AttackComp.IsActive; //unit must have an active attack component
        }
    }
}