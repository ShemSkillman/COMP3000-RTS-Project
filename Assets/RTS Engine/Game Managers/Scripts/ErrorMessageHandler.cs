using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public enum ErrorMessage
    {
        none,
        invalid,
        inactive,
        uninteractable,
        blocked,

        notLocalPlayer,
        requestRelayed,

        notMovable, targetPositionNotFound, positionReserved, positionOccupied, invalidMvtPath,

        invalidTarget,

        searchCellNotFound, searchTargetNotFound,

        underAttack,

        unitGroupSet, unitGroupEmpty, unitGroupSelected,

        targetDifferentFaction, targetSameFaction, noFactionAccess,

        targetNoConversion, targetNoAttack,

        sourceDead, targetDead, targetMaxHealth, sourceLowHealth,

        lowResources,

        targetMaxWorkers, targetMaxCapacityReached, sourceMaxCapacityReached, targetEmpty,

        targetOutsideTerritory,

        targetPortalMissing,

        entityNotAllowed,

        buildingNotBuilt, buildingNotPlaced,

        peaceTime,

        attackTypeLocked, attackTypeNotFound, attackInCooldown, attackTargetNoChange, attackTargetRequired, attackTargetOutOfRange, canNotAttack, attackPositionNotFound, attackPositionOutOfRange,
        moveToTargetNoAttack,

        dropoffBuildingMissing,

        componentDisabled,

        sourceUpgrading,

        maxPopulationReached,

        factionLimitReached,
        alreadyInAttackPosition,
    }

    public class ErrorMessageHandler : MonoBehaviour
    {
        //other components:
        static GameManager gameMgr;

        public void Init(GameManager gameMgrIns)
        {
            gameMgr = gameMgrIns;
        }

        public static void OnErrorMessage (ErrorMessage message, Entity source, Entity target = null)
        {
            switch (message)
            {
                case ErrorMessage.invalid:
                    gameMgr.UIMgr.ShowPlayerMessage("Invalid target.", UIManager.MessageTypes.info);
                    break;
                case ErrorMessage.inactive:
                    gameMgr.UIMgr.ShowPlayerMessage("Inactive target.", UIManager.MessageTypes.info);
                    break;
                case ErrorMessage.uninteractable:
                    gameMgr.UIMgr.ShowPlayerMessage("Unteractable target.", UIManager.MessageTypes.info);
                    break;


                case ErrorMessage.blocked:
                    //gameMgr.UIMgr.ShowPlayerMessage("Faction Entity blocked.", UIManager.MessageTypes.info);
                    break;

                case ErrorMessage.unitGroupSet:
                    gameMgr.UIMgr.ShowPlayerMessage("Unit group assigned.", UIManager.MessageTypes.info);
                    break;
                case ErrorMessage.unitGroupSelected:
                    gameMgr.UIMgr.ShowPlayerMessage("Unit group selected.", UIManager.MessageTypes.info);
                    break;
                case ErrorMessage.unitGroupEmpty:
                    gameMgr.UIMgr.ShowPlayerMessage("Unit group empty.");
                    break;
               
                case ErrorMessage.targetDifferentFaction:
                    gameMgr.UIMgr.ShowPlayerMessage("The target doesn't belong to your faction!");
                    break;
                case ErrorMessage.targetSameFaction:
                    gameMgr.UIMgr.ShowPlayerMessage("The target belongs to your faction!");
                    break;
                case ErrorMessage.noFactionAccess:
                    gameMgr.UIMgr.ShowPlayerMessage("Your faction doesn't have access!");
                    break;

                case ErrorMessage.targetNoConversion:
                    gameMgr.UIMgr.ShowPlayerMessage("The target can't be converted!");
                    break;
                case ErrorMessage.targetNoAttack:
                    gameMgr.UIMgr.ShowPlayerMessage("The target can't be attacked!");
                    break;
                    
                case ErrorMessage.sourceDead:
                    gameMgr.UIMgr.ShowPlayerMessage("The source is dead!");
                    break;
                case ErrorMessage.targetDead:
                    gameMgr.UIMgr.ShowPlayerMessage("The target is dead!");
                    break;
                case ErrorMessage.targetMaxHealth:
                    gameMgr.UIMgr.ShowPlayerMessage("Your target has reached maximum health!");
                    break;
                case ErrorMessage.sourceLowHealth:
                    gameMgr.UIMgr.ShowPlayerMessage("The source has low health!");
                    break;

                case ErrorMessage.lowResources:
                    gameMgr.UIMgr.ShowPlayerMessage("Not enough resources");
                    break;

                case ErrorMessage.targetMaxWorkers:
                    gameMgr.UIMgr.ShowPlayerMessage("The target has maximum workers!");
                    break;
                case ErrorMessage.targetMaxCapacityReached:
                    gameMgr.UIMgr.ShowPlayerMessage("The target has reached maximum capacity!");
                    break;
                case ErrorMessage.sourceMaxCapacityReached:
                    gameMgr.UIMgr.ShowPlayerMessage("The source has reached maximum capacity!");
                    break;
                case ErrorMessage.targetEmpty:
                    gameMgr.UIMgr.ShowPlayerMessage("The target is empty!");
                    break;

                case ErrorMessage.targetOutsideTerritory:
                    gameMgr.UIMgr.ShowPlayerMessage("The target is outside your faction's territory!");
                    break;

                case ErrorMessage.targetPortalMissing:
                    gameMgr.UIMgr.ShowPlayerMessage("The target portal is missing!");
                    break;

                case ErrorMessage.entityNotAllowed:
                    gameMgr.UIMgr.ShowPlayerMessage("This entity is not allowed!");
                    break;

                case ErrorMessage.buildingNotBuilt:
                    gameMgr.UIMgr.ShowPlayerMessage("The building is not built yet!");
                    break;
                case ErrorMessage.buildingNotPlaced:
                    gameMgr.UIMgr.ShowPlayerMessage("This building is not placed!");
                    break;

                case ErrorMessage.peaceTime:
                    gameMgr.UIMgr.ShowPlayerMessage("Can't attack in peace time!");
                    break;

                case ErrorMessage.attackInCooldown:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack is currently in cooldown!");
                    break;
                case ErrorMessage.attackTargetNoChange:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack target can't be changed!");
                    break;
                case ErrorMessage.attackTargetRequired:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack target can't be changed!");
                    break;
                case ErrorMessage.attackTargetOutOfRange:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack target out of range!");
                    break;
                case ErrorMessage.canNotAttack:
                    gameMgr.UIMgr.ShowPlayerMessage("Source can not launch an attack!");
                    break;
                case ErrorMessage.attackPositionNotFound:
                    gameMgr.UIMgr.ShowPlayerMessage("No valid attack position can be found!");
                    break;
                case ErrorMessage.attackPositionOutOfRange:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack engagement position is out of range!");
                    break;

                case ErrorMessage.dropoffBuildingMissing:
                    gameMgr.UIMgr.ShowPlayerMessage("The dropoff building is missing!");
                    break;

                case ErrorMessage.notMovable:
                    gameMgr.UIMgr.ShowPlayerMessage("The unit is not movable!");
                    break;
                case ErrorMessage.targetPositionNotFound:
                    gameMgr.UIMgr.ShowPlayerMessage("The target position is not found!");
                    break;

                case ErrorMessage.componentDisabled:
                    gameMgr.UIMgr.ShowPlayerMessage("The component is disabled!");
                    break;

                case ErrorMessage.sourceUpgrading:
                    gameMgr.UIMgr.ShowPlayerMessage("The source is upgrading!");
                    break;

                case ErrorMessage.maxPopulationReached:
                    gameMgr.UIMgr.ShowPlayerMessage("Maximum population reached!");
                    break;

                case ErrorMessage.factionLimitReached:
                    gameMgr.UIMgr.ShowPlayerMessage("Faction limit has been reached!");
                    break;

                case ErrorMessage.attackTypeLocked:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack type is locked!");
                    break;
                case ErrorMessage.attackTypeNotFound:
                    gameMgr.UIMgr.ShowPlayerMessage("Attack type is not found!");
                    break;

            }
        }
    }
}
