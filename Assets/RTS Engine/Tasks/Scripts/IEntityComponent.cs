using System.Collections.Generic;

/* ÍEntityComponent script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    /// <summary>
    /// Used for components that can be attached to units, buildings and resources (selectable entities) and which can have a task on the task panel when selected.
    /// Call "CustomEvents.OnEntityComponentTaskReloadRequest(IEntityComponent, taskCode)" on component that implement the IEntityComponent interface
    /// Everytime changes happen regarding the task(s) managed by the IEntityComponent.
    /// </summary>
    public interface IEntityComponent
    {
        /// <summary>
        /// Is the task component currently active?
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Entity instance where the component is attached.
        /// </summary>
        Entity Entity { get; }

        /// <summary>
        /// Initializer method required for each entity component that gets called by the Entity instance that the component is attached to.
        /// </summary>
        /// <param name="gameMgr">Active instance of the GameManager component.</param>
        /// <param name="entity">Entity instance that the component is attached to.</param>
        void Init(GameManager gameMgr, Entity entity);

        /// <summary>
        /// Allows to provide information regarding a task or multiple tasks, if there are any, that is displayed in the task panel.
        /// </summary>
        /// <param name="taskUIAttributes">TaskUIAttributes instances that contain the information required to display the component's tasks.</param>
        /// <param name="disabledTaskCodes">Holds the unique codes of the tasks to be disabled, if they are already displayed.</param>
        /// <returns>True if there's at least one task that requires to be displayed, otherwise false.</returns>
        bool OnTaskUIRequest(out IEnumerable<TaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes);

        /// <summary>
        /// Called when the player clicks on the task of the faction entity component.
        /// </summary>
        /// <param name="taskCode"></param>
        void OnTaskUIClick(string taskCode);
    }
}
