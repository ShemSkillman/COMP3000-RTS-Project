using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

/* MovementManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    //a movement task consists of the unit and its target position and movement info
    public struct MovementTask
    {
        public Unit unit;

        public Vector3 targetPosition;
        public Vector3 lookAtPosition;

        public Entity target;
        public InputMode targetMode;
    }

    public class MovementManager : MonoBehaviour
    {
        //The stopping distance when a unit moves to an empty space of the map:
        [SerializeField]
        private float stoppingDistance = 0.3f;

        public float StoppingDistance { get { return stoppingDistance; } }

        //Mvt target effect:
        [SerializeField]
        private EffectObj movementTargetEffect = null; //when assigned, visible when the player commands units to move
        [SerializeField]
        private EffectObj terrainAttackTargetEffect = null; //when assigned, visible when the player commands units to do a terrain attack
        public void SpawnTargetEffect (bool movement, Vector3 position)
        {
            EffectObj nextEffect = movement ? movementTargetEffect : terrainAttackTargetEffect;
            gameMgr.EffectPool.SpawnEffectObj(nextEffect, position);
        }

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        public ErrorMessage Move(Unit unit, Vector3 destination, float offsetRadius, Entity target, InputMode targetMode, bool playerCommand)
        {
            if (unit == null) //make sure that the source unit instance is valid
            {
                Debug.LogError("[MovementManager] Can not move an invalid unit!");
                return ErrorMessage.invalid;
            }
            else if (!unit.MovementComp.IsActive) //the unit can not move, do not continue
                return ErrorMessage.notMovable;

            if (!RTSHelper.IsLocalPlayer(unit)) return ErrorMessage.notLocalPlayer; //only allow local player to launch this command.

            if (playerCommand && !RTSHelper.IsPlayerFaction(unit)) //if the source unit is not part of the player's faction then, override the playerCommand value to false even if it was set to true
                playerCommand = false;

            if (!GameManager.MultiplayerGame) //single player game, directly prepare the unit's movement
                return MoveLocal(unit, destination, offsetRadius, target, targetMode, playerCommand);
            else 
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unit,
                    targetMode = (byte)targetMode,
                    initialPosition = unit.transform.position,
                    targetPosition = destination,
                    value = Mathf.FloorToInt(offsetRadius),
                    playerCommand = playerCommand
                };

                //sent input
                InputManager.SendInput(newInput, unit, target);

                return ErrorMessage.requestRelayed;
            }
        }

        //the PrepareMove method prepares the movement of a single unit
        public ErrorMessage MoveLocal (Unit unit, Vector3 destination, float offsetRadius, Entity target, InputMode targetMode, bool playerCommand)
        {
            unit.MovementComp.TargetPositionMarker.Toggle(false); //disable the target position marker so it won't intefer in the target position collider

            Vector3 originalDestination = destination; //to be used for the movement target effect and rotation look at of the unit.

            //first check if the actual destination is a valid target position, if it can't be then search for a target position
            //if the offset radius is not zero, then the unit will be moving towards a target entity and a calculation for a path destination around that target is required
            if (offsetRadius > 0.0f 
                || IsPositionClear(ref destination, unit.GetRadius(), unit.MovementComp.Controller.AreaMask, unit.MovementComp.AirUnit) != ErrorMessage.none)
            {
                //generate movement path destination
                gameMgr.MvtMgr.GeneratePathDestination(
                    unit,
                    destination,
                    offsetRadius, out List<Vector3> pathDestinations);

                if(pathDestinations.Count == 0) //if there's no valid movement destination produced, then stop here
                    return ErrorMessage.targetPositionNotFound;

                //get the closest target position
                destination = pathDestinations.OrderBy(pos => (pos - unit.transform.position).sqrMagnitude).First();
            }

            //if this is a player command and the unit belongs to the local player's faction and no targetEntity for the movement is assigned
            if (playerCommand && target == null && RTSHelper.IsPlayerFaction(unit)) 
                gameMgr.MvtMgr.SpawnTargetEffect(true, originalDestination);

            return unit.MovementComp.SetTargetLocal(destination, target, originalDestination, targetMode, playerCommand);
        }

        //the Move method is called when another component requests the movement of a list of units
        public ErrorMessage Move(List<Unit> units, Vector3 destination, float offsetRadius, Entity target, InputMode targetMode, bool playerCommand)
        {
            if (units.Count == 0)
            {
                Debug.LogWarning("[MovementManager] Attempting to move an empty list of units is not allowed!");
                return ErrorMessage.invalid;
            }
            else if(units.Count == 1) //dedicated method to move one unit.
                return Move(units[0], destination, offsetRadius, target, targetMode, playerCommand);

            if (!RTSHelper.IsLocalPlayer(units[0])) return ErrorMessage.notLocalPlayer; //only allow local player to launch this command.

            if (playerCommand && !RTSHelper.IsPlayerFaction(units[0])) //if the source unit is not part of the player's faction then, override the playerCommand value to false even if it was set to true
                playerCommand = false;

            if (!GameManager.MultiplayerGame) //single player game, directly prepare the unit's list movement
                return MoveLocal(units, destination, offsetRadius, target, targetMode, playerCommand);
            else
            {
                //send input action to the input manager
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.unitGroup,
                    targetMode = (byte)targetMode,
                    targetPosition = destination,
                    extraPosition = new Vector3(offsetRadius, 0.0f, 0.0f),
                    code = InputManager.UnitListToString(units),
                    playerCommand = playerCommand
                };

                //sent input
                InputManager.SendInput(newInput, null, target);

                return ErrorMessage.requestRelayed;
            }
        }

        //the PrepareMove method prepares the movement of a list of units by sorting them based on their radius, distance to target and generating target positions
        public ErrorMessage MoveLocal(List<Unit> units, Vector3 destination, float offsetRadius, Entity target, InputMode targetMode, bool playerCommand)
        {
            //sort the attack units based on their codes, we assume that units that share the same code (which is the defining property of an entity in the RTS Engine) are identical.
            //also filter out any units that are not movable.
            ChainedSortedList<string, Unit> sortedUnits = RTSHelper.SortEntitiesByCode<Unit>(units, unit => unit.MovementComp.IsActive);

            foreach (List<Unit> unitSet in sortedUnits.Values) //for each different list of units that share the same code
            {
                //disable the target position markers for each unit set before calculating the target positions so they won't interfer with deciding whether a destination is occupied or not
                foreach (Unit unit in unitSet)
                    unit.MovementComp.TargetPositionMarker.Toggle(false);

                //generate the path destinations for this unit set.
                GeneratePathDestination(unitSet, destination, unitSet[0].MovementComp.Formation, offsetRadius, out List<Vector3> pathDestinations);

                //no valid path destinations generated? do not continue as there is nowhere to move to
                if (pathDestinations.Count == 0)
                    return ErrorMessage.targetPositionNotFound;

                //compute the directions of the units we have so we know the direction they will face in regards to the target.
                Vector3 unitsDirection = RTSHelper.GetUnitsDirection(units, destination);
                unitsDirection.y = 0;

                int destinationID = 0; //index counter for the generated path destinations.
                foreach (Unit unit in unitSet) //go through the current unit list
                {
                    if (target != null) //if this movement is towards a target, pick the closest position to the target for each unit
                        pathDestinations = pathDestinations.OrderBy(pos => (pos - unit.transform.position).sqrMagnitude).ToList();

                    if (unit.MovementComp.SetTargetLocal(pathDestinations[destinationID], target, pathDestinations[destinationID] + unitsDirection, targetMode, playerCommand) != ErrorMessage.none)
                        continue;

                    //only move to the next path destination if we're moving towards a non target, if not keep removing the first element of the list which was the closest to the last unit
                    if(target == null)
                        destinationID++;
                    else
                        pathDestinations.RemoveAt(0);

                    if (destinationID >= pathDestinations.Count) //no more paths to test, stop moving units.
                        break;

                    if (playerCommand && RTSHelper.IsPlayerFaction(unit)) //if this is a player command.
                    {
                        if(target == null) //only if no target is assigned.
                            gameMgr.MvtMgr.SpawnTargetEffect(true, destination);

                        playerCommand = false; //we only want to the play the audio clip and show the terrain indicator once for a valid unit
                    }
                }
            }

            return ErrorMessage.none;
        }

        public ErrorMessage GeneratePathDestination (Unit unit, Vector3 targetPosition, float offset, out List<Vector3> pathDestinations)
        {
            return GeneratePathDestination(Enumerable.Repeat(unit, 1).ToList(), targetPosition, unit.MovementComp.Formation, offset, out pathDestinations);
        }

        /// <summary>
        /// Generates a list of Vector3 elements that represent a possible path destination for the reference unit provided in the first argument.
        /// </summary>
        /// <param name="refUnit">Unit instance that represents the unit type to consider when producing the path destinations.</param>
        /// <param name="amount">How many path destinations to generate?</param>
        /// <param name="targetPosition">Vector3 position that presents the position that the reference unit type is trying to move to.</param>
        /// <param name="offset">The initial offset which can be the offset radius of the target position.</param>
        /// <returns>A list of Vector3 elements that represent valid movement path destinations for the reference unit type.</returns>
        public ErrorMessage GeneratePathDestination (List<Unit> units, Vector3 targetPosition, MovementFormation formation, float offset, out List<Vector3> pathDestinations)
        {
            ErrorMessage errorMessage;
            pathDestinations = new List<Vector3>(); //the resulting list of path destinations.

            //check unit length and whether shit is valid or not.
            //assumptions, all units are of the same type

            Unit refUnit = units[0]; //the unit that will be used as a reference to the rest of the units of the same type.

            int amount = units.Count; //the amount of path destinations that we want to produce.

            //if the unit is a flying unit then set the height of the destinations to generate to the flying height to avoid any complex height sampling computations.
            //if the unit is a ground unit then start from the unit's height on the ground.
            if (refUnit.MovementComp.AirUnit)
                targetPosition.y = gameMgr.TerrainMgr.GetFlyingHeight();

            //if we only have one unit to generate a path destination for then no need to consider spacing between units:
            float spacing = amount <= 1 ? 0.0f : formation.spacing;

            //in case the path destination generation methods result into a failure, return with the failure's error code.

            switch (formation.type)
            {
                //produce path destinations that are placed circularly around the target position
                case MovementFormation.Type.circle:
                    while (amount > 0)
                        if ((errorMessage = GenerateCirclePathDestination(refUnit, ref amount, targetPosition, spacing, ref offset, ref pathDestinations, out int _)) != ErrorMessage.none)
                            return errorMessage;

                    break;

                //produce path destinations that are placed in lines facing the target.
                case MovementFormation.Type.row:

                    //first we need to compute the directions of the units we have so we know the direction they will face in regards to the target.
                    Vector3 unitsDirection = RTSHelper.GetUnitsDirection(units, targetPosition);
                    unitsDirection.y = 0; //we want to handle setting the height by sampling the terrain to get the correct height since there's no way to know it directly.

                    int emptyRowCount = 0; //how many "GenerateRectangularPathDestinations" method calls resulted into generating no valid destinations?

                    while (amount > 0)
                    {
                        if (emptyRowCount >= formation.maxEmpty) //if the amount of empty rows reaches the max allowed amount
                        {
                            //use the circular formation instead to get the rest of the path destinations
                            if ((errorMessage = GenerateCirclePathDestination(refUnit, ref amount, targetPosition, spacing, ref offset, ref pathDestinations, out int _)) != ErrorMessage.none)
                                return errorMessage;
                            continue;
                        }

                        //attempt to generate valid path destinations in the row defined by the arguments below.
                        //but if no valid positions have been produced.
                        if ((errorMessage = GenerateRowPathDestinations(refUnit, ref amount, formation.amount, targetPosition, spacing, ref offset, unitsDirection, ref pathDestinations, out int generatedAmount)) != ErrorMessage.none)
                            return errorMessage;

                        if(generatedAmount == 0)
                            emptyRowCount++; //a call that resulted in a completely empty row
                    }

                    break;
            }
            

            return ErrorMessage.none; //returned the computed path destinations, the count of the list is either smaller or equal to the initial value of the "amount" argument.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refUnit"></param>
        /// <param name="amount"></param>
        /// <param name="targetPosition"></param>
        /// <param name="offset"></param>
        /// <param name="pathDestinations"></param>
        /// <returns></returns>
        private ErrorMessage GenerateCirclePathDestination (Unit refUnit, ref int amount, Vector3 targetPosition, float spacing, ref float offset, ref List<Vector3> pathDestinations, out int generatedAmount)
        {
            ErrorMessage errorMessage;
            generatedAmount = 0; //how many valid path destinations did this call generate?

            //calculate the perimeter of the circle in which unoccupied positions will be searched
            //and then calculate the expected amount of free positions for the unit with unitRadius in the circle
            int expectedPositionCount = Mathf.FloorToInt(2.0f * Mathf.PI * offset / ((refUnit.GetRadius() + spacing) * 2.0f));
            //if no expected positions are to be found and the radius offset is zero then set the expected position count to 1 to test the actual target position if it is valid
            if (expectedPositionCount == 0 && offset == 0.0f)
                expectedPositionCount = 1;

            float angleIncValue = 360f / expectedPositionCount; //the increment value of the angle inside the current circle with the above perimeter
            float currentAngle = 0.0f;

            //get the initial path destination by picking the closest position on the circle around the target.
            Vector3 nextDestination = targetPosition + Vector3.right * offset;

            int counter = 0; //the while loop counter

            while (counter < expectedPositionCount) //as long as we haven't inspected all the expected free positions inside this cirlce
            {
                //always make sure that the next path destination has a correct height in regards to the height of the map.
                nextDestination.y = gameMgr.TerrainMgr.SampleHeight(nextDestination, refUnit.GetRadius(), refUnit.MovementComp.Controller.AreaMask);

                //check if there is no obstacle and no other reserved target position on the currently computed potential path destination
                if((errorMessage = IsPositionClear(ref nextDestination, refUnit.GetRadius(), refUnit.MovementComp.Controller.AreaMask, refUnit.MovementComp.AirUnit)) == ErrorMessage.none)
                {
                    amount--;
                    generatedAmount++;

                    pathDestinations.Add(nextDestination); //save the valid destination.
                }
                //if while checking the target position, we left the bounds of the search grid then stop generating target positions immediately as we are no longer searching inside the map
                else if (errorMessage == ErrorMessage.searchCellNotFound)
                    return errorMessage;
                        
                currentAngle += angleIncValue; //set the angle value

                //rotate the nextDestination vector around the y axis by the current angle value
                nextDestination = targetPosition + offset * new Vector3(Mathf.Cos(Mathf.Deg2Rad * currentAngle), 0.0f, Mathf.Sin(Mathf.Deg2Rad * currentAngle));

                counter++;
            }

            //increase the circle radius by the unit's radius so we can calculate a destination position in a wider circle in the next iteration
            offset += refUnit.GetRadius() + spacing;

            return ErrorMessage.none;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refUnit"></param>
        /// <param name="amount"></param>
        /// <param name="targetPosition"></param>
        /// <param name="offset"></param>
        /// <param name="unitsDirection"></param>
        /// <param name="pathDestinations"></param>
        /// <returns></returns>
        private ErrorMessage GenerateRowPathDestinations(Unit refUnit, ref int amount, int amountPerRow, Vector3 targetPosition, float spacing, ref float offset, Vector3 unitsDirection, ref List<Vector3> pathDestinations, out int generatedAmount)
        {
            ErrorMessage errorMessage;
            generatedAmount = 0; //how many valid path destinations did this call generate?

            //the row direction is simply the unitsDirection vector but with inverted x and z coordinates with negating the resulting z axis.
            //the y coordinate is set to 0 since we will rely on the terrain manager to generate the height of potential path destination each time.
            Vector3 rowDirection = new Vector3(unitsDirection.z, 0.0f, -unitsDirection.x).normalized;

            //before starting to compute the potential path destinations, we move the target position in the row directoin by the current offset and we start generating destinations at that point.
            targetPosition -= unitsDirection * offset;

            int multiplier = 1; //allows to alternate between going left and right while generating path destinations following one row

            int counter = 0; //the while loop counter
            //as long as there path destinations to produce and we still haven't got over the allowed amount of units per row
            while(counter < amountPerRow)
            {
                //each time, we once go right in the rowDirection and then we go left in the rowDirection using the same distance from the target position.
                Vector3 nextDestination = targetPosition 
                    + multiplier * (refUnit.GetRadius() + spacing) * rowDirection;

                //always make sure that the next path destination has a correct height in regards to the height of the map.
                nextDestination.y = gameMgr.TerrainMgr.SampleHeight(nextDestination, refUnit.GetRadius(), refUnit.MovementComp.Controller.AreaMask);

                //check if there is no obstacle and no other reserved target position on the currently computed potential path destination
                if((errorMessage = IsPositionClear(ref nextDestination, refUnit.GetRadius(), refUnit.MovementComp.Controller.AreaMask, refUnit.MovementComp.AirUnit)) == ErrorMessage.none)
                {
                    amount--;
                    generatedAmount++;

                    pathDestinations.Add(nextDestination); //save the valid destination.
                }
                //if while checking the target position, we left the bounds of the search grid then stop generating target positions immediately as we are no longer searching inside the map
                else if (errorMessage == ErrorMessage.searchCellNotFound)
                    return errorMessage;

                multiplier = -multiplier + (multiplier < 0 ? 2 : 0); //allows us to move right then left by the same distance each two iterations.

                counter++;
            }

            //for the next method call, this allows us to move one row back.
            offset += refUnit.GetRadius() + spacing;

            return ErrorMessage.none;
        }

        //a method that determines whether a target position is clear (unoccupied) or not.
        private ErrorMessage IsPositionClear(ref Vector3 targetPosition, float agentRadius, LayerMask agentAreaMask, bool isFlyUnit)
        {
            //first make sure that the target position is not reserved by another unit's target position marker.
            ErrorMessage errorMessage;
            if ((errorMessage = gameMgr.GridSearch.IsPositionReserved(targetPosition, agentRadius, isFlyUnit ? 1 : 0)) != ErrorMessage.none)
                return errorMessage;

            //next we'll sample the navigation mesh in the agent radius sized range of target position to get the free position inside that range.
            NavMeshHit hit;

            if (NavMesh.SamplePosition(targetPosition, out hit, agentRadius, agentAreaMask)) //so if there are no target position collider in the target position range and the nav mesh sample position returns an unoccupied position
            {
                targetPosition = hit.position; //assign position and return true.
                return ErrorMessage.none;
            }
            return ErrorMessage.positionOccupied;
        }

        public bool IsAreaMovable(Vector3 center, float radius, LayerMask areaMask)
        {
            return NavMesh.SamplePosition(center, out NavMeshHit hit, radius, areaMask);
        }

        //a method that picks a random movable position starting from on origin point and inside a certain range
        public Vector3 GetRandomMovablePosition(Unit unit, Vector3 origin, float range)
        {
            Vector3 randomDirection = Random.insideUnitSphere * range; //pick a random direction to go to
            randomDirection += origin;
            randomDirection.y = gameMgr.TerrainMgr.SampleHeight(randomDirection, range, unit.MovementComp.Controller.AreaMask);

            Vector3 targetPosition = unit.transform.position;
            //get the closet movable point to the random chosen direction
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, range, unit.MovementComp.Controller.AreaMask))
            {
                targetPosition = hit.position;
                IsPositionClear(ref targetPosition, unit.GetRadius(), unit.MovementComp.Controller.AreaMask, unit.MovementComp.AirUnit);
            }

            return targetPosition;
        }

    }

}