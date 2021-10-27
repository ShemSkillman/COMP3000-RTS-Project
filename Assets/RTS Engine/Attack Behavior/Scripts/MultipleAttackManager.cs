using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* Multiple Attack Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(AttackEntity))]
    public class MultipleAttackManager : MonoBehaviour, IEntityComponent
    {
        private FactionEntity factionEntity = null; //the source component handling this comp
        /// <summary>
        /// Entity instance that the MultipleAttackManager component is attached to.
        /// </summary>
        public Entity Entity { get { return factionEntity; } }

        [SerializeField, Tooltip("Can the faction entities' attacks be switched in game?")]
        private bool isActive = true;
        /// <summary>
        /// Is the faction entity able to use the attack defined in the AttackEntity component?
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        //holds all the Attack Entity components attached to the faction entity main object where the key is the code of each attack type.
        private Dictionary<string, AttackEntity> attackEntities = new Dictionary<string, AttackEntity>();
        /// <summary>
        /// Gets all the AttackEntity instances attached to the faction entity.
        /// </summary>
        public IEnumerable<AttackEntity> AttackEntities { get { return attackEntities.Values; } }

        //holds each AttackEntity that the faction entity can switch it as a key and the task UI data used for the attack switch task as the value.
        public Dictionary<string, AttackEntity> switchAttackTaskCodes = new Dictionary<string, AttackEntity>();

        public string BasicAttackCode { private set; get; } //the unique code of the basic attack that the faction entity uses
        private AttackEntity activeAttack; //the attack entity component that is currently active is held by this ref var

        [SerializeField, Tooltip("Everytime a non-basic attack type is used, revert back to the basic attack type?")]
        private bool revertToBasicAttack = true; //when enabled, everytime the faction entity uses a non-basic attack, it will revert back to the basic attack
        public bool RevertToBasicAttack { get { return revertToBasicAttack; } }

        /// <summary>
        /// Initializer method required for each entity component that gets called by the Entity instance that the component is attached to.
        /// </summary>
        /// <param name="gameMgr">Active instance of the GameManager component.</param>
        /// <param name="entity">Entity instance that the component is attached to.</param>
        public void Init(GameManager gameMgr, Entity entity)
        {
            factionEntity = entity as FactionEntity;
            activeAttack = null;

            foreach(AttackEntity attackEntity in GetComponents<AttackEntity>())
            {
                attackEntities.Add(attackEntity.GetCode(), attackEntity);

                if (attackEntity.IsBasic) //if this is the attack that is enabled by default, save its code
                {
                    BasicAttackCode = attackEntity.GetCode();
                    SetTargetLocal(BasicAttackCode);
                }
                else
                    attackEntity.IsActive = false; //decativate attack types that are not marked as basic/default

                if (attackEntity.SwitchTaskUI != null)
                    //create the attack switch data for each AttackEntity instance:
                    switchAttackTaskCodes.Add(attackEntity.SwitchTaskUI.Data.code, attackEntity);
            }

            if (attackEntities.Count < 2) //if there's not any more that one attack entity type then disable this component
                isActive = false;
        }

        /// <summary>
        /// Test whether an AttackEntity instance defined by a given code can be switched to or not.
        /// </summary>
        /// <param name="code">Code of the AttackEntity instance to test.</param>
        /// <returns>ErorrMessage.none if the attack type can be switched to, otherwise failure's error code.</returns>
        public ErrorMessage IsTargetValid (string code)
        {
            if (!attackEntities.TryGetValue(code, out AttackEntity targetAttackEntity)) //invalid attack entity code?
                return ErrorMessage.attackTypeNotFound;
            else if (targetAttackEntity.IsLocked) //locked attack type, can not switch
                return ErrorMessage.attackTypeLocked;
            else if(targetAttackEntity.CoolDownActive == true) //if the target attack type is in cool down right now:
                return ErrorMessage.attackInCooldown;

            return ErrorMessage.none;
        }

        /// <summary>
        /// Switches the attack type to the AttackEntity instance with the given unique code.
        /// </summary>
        /// <param name="code">Code of the AttackEntity instance to switch to.</param>
        /// <returns>ErrorMessage.none if the attack type is to be switched directly, ErrorMessage.requestRelyed in case the command is relayed to the InputManager (multiplayer game), otherwise failure's error code.</returns>
        public ErrorMessage SetTarget (string code)
        {
            if (!RTSHelper.IsLocalPlayer(factionEntity)) return ErrorMessage.notLocalPlayer; //only allow local player to launch this command.

            ErrorMessage errorMessage;
            if ((errorMessage = IsTargetValid(code)) != ErrorMessage.none)
                return errorMessage;

            if (!GameManager.MultiplayerGame) //single player game -> directly enable attack entity
                return SetTargetLocal (code);
            else
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.factionEntity,
                    targetMode = (byte)InputMode.multipleAttack,
                    code = code,
                };
                InputManager.SendInput(newInput, factionEntity, null); //send input to input manager

                return ErrorMessage.requestRelayed;
            }
        }

        /// <summary>
        /// Switches the attack type to the AttackEntity instance with the given unique code. Assumes that the target attack type code is valid and all attack switch conditions are met.
        /// </summary>
        /// <param name="code">Code of the AttackEntity instance to switch to.</param>
        /// <returns>ErrorMessage.none if the attack type is to be switched directly, otherwise failure's error code.</returns>
        public ErrorMessage SetTargetLocal (string code)
        {
            if(activeAttack != null) //if there was a previously active attack
            {
                activeAttack.Stop(); //stop if there's a current attack
                activeAttack.Reset(); //reset it
                activeAttack.IsActive = false; //deactivate it
                activeAttack.GetWeapon()?.Toggle(false); //disable the wepaon object
            }

            activeAttack = attackEntities[code]; //new active attack.

            activeAttack.IsActive = true;
            activeAttack.FactionEntity.UpdateAttackComp(activeAttack);
            activeAttack.GetWeapon()?.Toggle(true); //enable the wepaon object

            CustomEvents.OnAttackSwitch(activeAttack); //trigger custom event

            if(RTSHelper.IsPlayerFaction(factionEntity)) //if the faction entity belongs to the player's faction then update the attack switch tasks in case it has been already drawn
                CustomEvents.OnEntityComponentTaskReloadRequest(this, code); //the attack switch task code is the code of the actual attack type to switch to.

            return ErrorMessage.none;
        }

        #region Task UI
        /// <summary>
        /// Called by the TaskPanelUI component to fetch the switch attack task UI attributes.
        /// </summary>
        /// <param name="taskUIAttributes">The switch attack task UI attributes are added as elements the IEnumerable for each attack type, if they can be switched to.</param>
        /// <returns>True if at least one switch attack type task can be displayed in the task panel, otherwise false.</returns>
        public bool OnTaskUIRequest(out IEnumerable<TaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = null;
            disabledTaskCodes = switchAttackTaskCodes.Where(elem => elem.Value.IsLocked || elem.Value.IsActive).Select(elem => elem.Key);

            if (!factionEntity.CanRunComponents //if the associated faction entity can not run this component
                || !IsActive //if the component is not active
                || !RTSHelper.IsPlayerFaction(factionEntity)) //or this is not the local player's faction
                return false; //no task to display

            //used for the color of the switch attack tasks where the cool down is enabled.
            Color semiTransparent = Color.white;
            semiTransparent.a = 0.5f;

            taskUIAttributes = switchAttackTaskCodes
                .Where(elem => !elem.Value.IsLocked && !elem.Value.IsActive)
                .Select(elem => new TaskUIAttributes
                {
                    entityComp = elem.Value.SwitchTaskUI.Data,
                    icon = elem.Value.SwitchTaskUI.Data.icon,
                    color = elem.Value.CoolDownActive ? semiTransparent : Color.white,
                });

            return taskUIAttributes.Count() > 0;
        }

        /// <summary>
        /// Called by the TaskUI instance that handles the switch attack engagement task, if it is drawn on the panel, to launch the attack switch task.
        /// </summary>
        /// <param name="taskCode">Code of the attack switch task which is the code of the AttackEntity type that the attack is supposed to switch to.</param>
        public void OnTaskUIClick (string taskCode)
        {
            if(switchAttackTaskCodes.TryGetValue(taskCode, out AttackEntity targetAttackEntity))
                SetTarget(targetAttackEntity.GetCode());
        }
        #endregion
    }
}