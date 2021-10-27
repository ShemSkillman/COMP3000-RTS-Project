using System.Collections.Generic;

/* EntityComponentTaskUITracker script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.EntityComponent
{
    /// <summary>
    /// Tracks the components that implement IEntityComponent interface with active tasks in the task panel.
    /// </summary>
    public class EntityComponentTaskUITracker
    {
        //components that share the task tracked by this component
        private List<IEntityComponent> components = new List<IEntityComponent>();
        /// <summary>
        /// Gets the components that implement IEntityComponent interface that share the tracked task.
        /// </summary>
        public IEnumerable<IEntityComponent> Components { get { return components; } }

        /// <summary>
        /// TaskUI instance tracked by the EntityComponentTaskUITracker.
        /// </summary>
        public TaskUI Task { private set; get; }

        /// <summary>
        /// Gets the task panel category ID where the task tracked by this component is drawn.
        /// </summary>
        public int PanelCategory { get; private set; }

        /// <summary>
        /// Constructor for a new instance of the EntityComponentTaskUITracker class.
        /// </summary>
        /// <param name="component">A component that implements the IEntityComponent interface that activated a new task in the panel.</param>
        /// <param name="task">TaskUI instance of the active task.</param>
        /// <param name="category">Task panel category ID of the active task.</param>
        public EntityComponentTaskUITracker (IEntityComponent component, TaskUI taskUI, int panelCategory)
        {
            components.Clear();
            components.Add(component);

            Task = taskUI;
            PanelCategory = panelCategory;
        }

        /// <summary>
        /// Adds a new IEntityComponent component to the tracked list of components.
        /// </summary>
        /// <param name="component">New component to track.</param>
        public void AddComponent(IEntityComponent component)
        {
            if(!components.Contains(component)) //if the component is not already tracked.
                components.Add(component);
        }

        /// <summary>
        /// Refreshes the tracked task.
        /// </summary>
        /// <param name="newAttributes">New attributes to assign to the tracked task.</param>
        public void ReloadTask (TaskUIAttributes newAttributes)
        {
            Task.Reload(newAttributes, TaskUI.Types.entityComponent, this);
        }

        /// <summary>
        /// Disables tracking components and their active task.
        /// </summary>
        public void Disable ()
        {
            components.Clear();
            Task.Disable();
        }
    }
}
