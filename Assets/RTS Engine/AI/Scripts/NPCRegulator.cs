using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* NPCRegulator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Responsible for regulating the creation of a faction entity (unit or building) for NPC factions.
    /// </summary>
    /// <typeparam name="T">Inherits FactionEntity as a parent class.</typeparam>
    public abstract class NPCRegulator<T> where T : FactionEntity
    {
        #region Class Properties
        /// <summary>
        /// Code of the FactionEntity derived type that is regulated by this component.
        /// </summary>
        public string Code { protected set; get; }
        /// <summary>
        /// Category of the FactionEntity derived type that is regulated by this component.
        /// </summary>
        public string Category { protected set; get; }

        /// <summary>
        /// Minimum required amount for instances of the FactionEntity derived type that this component is regulating.
        /// </summary>
        public int MinAmount { protected set; get; }
        /// <summary>
        /// Increases the minimum required amount for instances of the FactionEntity derived type that this component is regulating. This is usually used to put 'pressure' on the NPC faction to create at least another instance of the FactionEntity derived type.
        /// </summary>
        public void IncMinAmount() { MinAmount++; }

        /// <summary>
        /// Maximum allowed amount for instances of the FactionEntity derived type that this component is regulating.
        /// </summary>
        public int MaxAmount { protected set; get; }

        /// <summary>
        /// Current amount of FactionEntity derived instances whose creation is still in progress.
        /// </summary>
        public int pendingAmount = 0;
        /// <summary>
        /// Maximum allowed amount of FactionEntity derived instances whose creation is still in progress.
        /// </summary>
        public int MaxPendingAmount { protected set; get; }

        /// <summary>
        /// Current amount of the regulated FactionEntity derived instances.
        /// </summary>
        public int Count {protected set; get;}
        protected List<T> instances = new List<T>(); //the list of spawned items from the defined type(s) in this component.
        /// <summary>
        /// Returns an IEnumerable instance of the current spawned faction entity instnaces regulated by the NPCRegulator instance.
        /// </summary>
        /// <returns>IEnumerable instance of FactionEntity instances.</returns>
        public IEnumerator<T> GetInstances()
        {
            return instances.GetEnumerator();
        }

        /// <summary>
        /// GameManager instance of the currently active game.
        /// </summary>
        protected GameManager gameMgr;
        /// <summary>
        /// FactionManager instance of the NPC faction in the currently active game.
        /// </summary>
        protected FactionManager factionMgr;
        /// <summary>
        /// NPCManager instance of the NPC faction in the currently active game.
        /// </summary>
        protected NPCManager npcMgr;
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// NPCRegulator constructor.
        /// </summary>
        /// <param name="data">Holds information regarding how the faction entity type that will be regulated.</param>
        /// <param name="prefab">FactionEntity derived prefab to regulate.</param>
        /// <param name="gameMgr">GameManager instance of the currently active game.</param>
        /// <param name="npcMgr">NPCManager instance that manages the NPC faction to whome the regulator component belongs.</param>
        public NPCRegulator (NPCRegulatorData data, FactionEntity prefab, GameManager gameMgr, NPCManager npcMgr)
        {
            //assign components
            this.gameMgr = gameMgr;
            Assert.IsNotNull(this.gameMgr, "[NPCRegulator] Initializing without a reference to the GameManager instance is not allowed!");

            this.npcMgr = npcMgr;
            Assert.IsNotNull(this.npcMgr, "[NPCRegulator] Initializing without a reference to the faction's NPCManager instance is not allowed!");

            this.factionMgr = npcMgr.FactionMgr;
            Assert.IsNotNull(this.factionMgr, "[NPCRegulator] Initializing without a reference to the faction's FactionManager instance is not allowed!");

            //pick the rest random settings from the given info.
            MaxAmount = data.GetMaxAmount();
            MinAmount = data.GetMinAmount();
            MaxPendingAmount = data.GetMaxPendingAmount();

            Count = 0;

            //get the code and category
            Code = prefab.GetCode();
            Category = prefab.GetCategory();
        }
        #endregion

        #region Spawned Instances Manipulation
        /// <summary>
        /// Tests whether a faction entity is regulated by the NPCRegulator instance or not.
        /// </summary>
        /// <param name="factionEntity">The FactionEntity instance to test.</param>
        /// <returns>True if the faction entity is regulated by the NPCRegulator instance, otherwise false.</returns>
        public virtual bool IsRegulated (T factionEntity)
        {
            //factionEntity must be in the regulated instances list or belong to the NPC faction and has a matching code.
            return instances.Contains(factionEntity);
        }

        /// <summary>
        /// Tests whether a faction entity can be regulated by the NPCRegulator instance or not.
        /// </summary>
        /// <param name="factionEntity">The FactionEntity instance to test.</param>
        /// <returns>True if the faction entity can be regulated by the NPCRegulator instance, otherwise false.</returns>
        public virtual bool CanBeRegulated (T factionEntity)
        {
            return (factionEntity.FactionID == factionMgr.FactionID && factionEntity.GetCode() == Code);
        }

        /// <summary>
        /// Adds a new faction entity that is already existing before the NPCRegulator instance is created.
        /// </summary>
        /// <param name="factionEntity"></param>
        public virtual void AddExisting(T factionEntity)
        {
            if (!CanBeRegulated(factionEntity)) //only proceed if the faction entity can be regulated by this component
                return;

            //add it to list:
            instances.Add(factionEntity);
            Count++;
        }

        /// <summary>
        /// Adds a new faction entity to be regulated by the NPCRegulator instance.
        /// </summary>
        /// <param name="factionEntity">The FactionEntity derived instance to add.</param>
        public virtual void Add(T factionEntity)
        {
            if (!CanBeRegulated(factionEntity)) //only proceed if the faction entity can be regulated by this component
                return;

            //add it to list:
            instances.Add(factionEntity);
            pendingAmount--; //decrease pending Count
        }

        /// <summary>
        /// Marks a new pending faction entity that will be regulated by the NPCRegulator instance once the faction entity is initialized.
        /// </summary>
        /// <param name="factionEntity">The pending FactionEntity derived instance to add, setting it to null means that the component will not check whether it can be regulated or not.</param>
        public virtual void AddPending (T factionEntity = null)
        {
            if (factionEntity != null && !CanBeRegulated(factionEntity)) //only proceed if the faction entity can be regulated by this component
                return;

            //increment current count and pending amount:
            Count++;
            pendingAmount++; //decrease pending Count
        }

        /// <summary>
        /// Removes a faction entity instance from regulated instances list.
        /// </summary>
        /// <param name="factionEntity">The pending FactionEntity derived instance to add, setting it to null means that the component will not check whether it can be regulated or not.</param>
        public virtual void Remove (T factionEntity = null)
        {
            if (factionEntity != null && !CanBeRegulated(factionEntity)) //faction entity can not be regulated by this component
                return;

            Count--; //decrease the amount of current items.
            //remove the item from the current items list:
            if (instances.Remove(factionEntity) == false) //if the item wasn't on the list to begin with
                pendingAmount--; //decrease pending amount

            if(factionEntity)
                OnSuccessfulRemove (factionEntity);
        }

        /// <summary>
        /// Called when a faction entity instance is successfully removed from being tracked and regulated by the NPCRegulator instance.
        /// </summary>
        /// <param name="factionEntity">FactionEntity derived instance that was removed.</param>
        protected abstract void OnSuccessfulRemove(T factionEntity);
        #endregion

        #region Count Tracking
        /// <summary>
        /// Determines whether the regulator component has reached the maximum allowed amount for active instances or not.
        /// </summary>
        /// <returns>True if the maximum amount of instances is reached, otherwise false.</returns>
        public bool HasReachedMaxAmount()
        {
            return Count >= MaxAmount || factionMgr.HasReachedLimit(Code, Category) || pendingAmount >= MaxPendingAmount;
        }

        /// <summary>
        /// Determines whether the regulator component has reached the minimum required amount for active instances or not.
        /// </summary>
        /// <returns>True if the minimum required amount of instances is reached, otherwise false.</returns>
        public bool HasReachedMinAmount()
        {
            return Count >= MinAmount;
        }
        #endregion
    }
}
