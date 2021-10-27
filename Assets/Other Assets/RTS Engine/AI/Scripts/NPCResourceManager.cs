using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* NPCResourceManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Holds required information regarding a building center and its idle/exploited resources
    /// </summary>
    public class BuildingCenterResources
    {
        //a list that holds resources that aren't being collected by this faction.
        public List<Resource> idleResources = new List<Resource>();
        //a list that holds resources that are currently being exploited inside the territory of the above center:
        public List<Resource> exploitedResources = new List<Resource>();

        /// <summary>
        /// Attempts to find an idle resource instance of a certain type.
        /// </summary>
        /// <param name="resourceType">The ResourceTypeInfo instance that represents the type of resource to search for?</param>
        /// <returns>Resource instance that matches the requested type if found, otherwise null.</returns>
        public Resource GetIdleResourceOfType (ResourceTypeInfo resourceType)
        {
            //go through the idle resources
            foreach(Resource ir in idleResources)
                if (ir.GetResourceType() == resourceType) //if the resource type matches.
                    return ir;

            return null;
        }
    }

    /// <summary>
    /// Responsible for managing the NPC faction's resources.
    /// </summary>
    public class NPCResourceManager : NPCComponent
    {
        #region Class Properties
        //Must always be >= 1.0. This determines how safe the NPC team would like to use their resources.
        //For example, if the "ResourceNeedRatio" is set to 2.0 and the faction needs 200 woods. Only when 400 (200 x 2) wood is available, the 200 can be used.
        [SerializeField, Tooltip("How safe does the NPC faction use their resources? The higher, the safer. Must be >= 1.0!")]
        private FloatRange resourceNeedRatioRange = new FloatRange(1.0f, 1.2f);

        //determine how many resources will be exploited by default by the faction.
        //0.0f -> no resources, 1.0f -> all available resources.
        [SerializeField, Tooltip("Ratio of the resources to be explored by default by the NPC faction.")]
        private FloatRange resourceExploitRatioRange = new FloatRange(0.8f, 1.0f);

        private Dictionary<Building, BuildingCenterResources> centerResources = new Dictionary<Building, BuildingCenterResources>(); //resources that belong to this faction...
        //...and which are not being collected will remain in this list with the building centers they belong to.
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the NPCResourceManager instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            //set the resource need ratio for the faction:
            gameMgr.ResourceMgr.GetFactionResources(factionMgr.FactionID).UpdateResourceNeedRatio(resourceNeedRatioRange.getRandomValue());

            //start listening to the required delegate events:
            CustomEvents.NPCFactionInit += OnNPCFactionInit;

            Border.BorderResourceAdded += OnBorderResourceAdded;
            Border.BorderResourceRemoved += OnBorderResourceRemoved;
            CustomEvents.BorderDeactivated += OnBorderDeactivated;

            CustomEvents.ResourceEmpty += OnResourceEmpty;
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.NPCFactionInit -= OnNPCFactionInit;

            Border.BorderResourceAdded -= OnBorderResourceAdded;
            Border.BorderResourceRemoved -= OnBorderResourceRemoved;
            CustomEvents.BorderDeactivated -= OnBorderDeactivated;

            CustomEvents.ResourceEmpty -= OnResourceEmpty;
        }

        /// <summary>
        /// Called when a NPC faction is done initializing its components.
        /// </summary>
        /// <param name="factionSlot">FactionSlot of the NPC faction.</param>
        private void OnNPCFactionInit (FactionSlot factionSlot)
        {
            if (factionSlot.FactionMgr != factionMgr) //different NPC faction?
                return;

            //go through the spawned building centers and init their registered resources.
            //can't really rely on the custom events for initializing since the Border components and Resource components will get initialiazed before this one.
            foreach(Building buildingCenter in factionMgr.GetBuildingCenters())
                foreach(Resource resource in buildingCenter.BorderComp.GetResourcesInRange())
                    AddBuildingCenterResource (buildingCenter, resource);
        }

        #endregion

        #region Faction Resources Manipulation
        /// <summary>
        /// Creates a BuildingCenterResources instance for a Resource instance that belongs to the NPC faction's Building instance.
        /// </summary>
        /// <param name="buildingCenter">The Building center instance to create the BuildingCenterResources instance for.</param>
        /// <param name="resource">Resource instance to add.</param>
        public void AddBuildingCenterResource (Building buildingCenter, Resource resource)
        {
            //when border is activated then we'd send some of them to the resources to collect list of the NPC Resource Collector...
            //...and leave the rest idle and that is all depending on the resource exploit ratio defined in this component:

            //if the building center hasn't been registerd then add it.
            if (!centerResources.TryGetValue(buildingCenter, out BuildingCenterResources bcr))
                //add the new instance to the centerResources list:
                centerResources.Add(buildingCenter,
                new BuildingCenterResources //create a new slot in the faction resources for the new border:
                {
                    idleResources = new List<Resource>(),
                    exploitedResources = new List<Resource>()
                });

            //make sure the resource hasn't been added already.
            if (!centerResources[buildingCenter].exploitedResources.Contains(resource) && !centerResources[buildingCenter].idleResources.Contains(resource))
            {
                //randomly decided if this resource is to be exploited:
                if (Random.Range(0.0f, 1.0f) <= resourceExploitRatioRange.getRandomValue())
                {
                    //if yes, then add it:
                    centerResources[buildingCenter].exploitedResources.Add(resource);
                    npcMgr.GetNPCComp<NPCResourceCollector>().AddResourceToCollect(resource);
                }
                else
                    //if not add resource to idle list:
                    centerResources[buildingCenter].idleResources.Add(resource);
            }
        }

        /// <summary>
        /// Removes a Resource instance from being regulated by this component inside the territory of a building center (Border instance).
        /// </summary>
        /// <param name="buildingCenter">Building instance that has a Border component attached to it where the resource will be searched for.</param>
        /// <param name="resource">Resource instance to remove.</param>
        /// <returns>True if the resource is found and successfully removed, otherwise false.</returns>
        private bool RemoveBuildingCenterResource (Building buildingCenter, Resource resource)
        {
            //make sure the building center is already registered to find the target resource to remove
            if (!centerResources.TryGetValue(buildingCenter, out BuildingCenterResources bcr))
            {
                bcr.exploitedResources.Remove(resource);
                bcr.idleResources.Remove(resource);

                //remove them from the resources to collect list in the NPC Resource Collector
                npcMgr.GetNPCComp<NPCResourceCollector>().RemoveResourceToCollect(resource);

                foreach (Unit unit in resource.WorkerMgr.GetAll().ToList())
                    unit.CollectorComp.Stop();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Disables and removes the active BuildingCenterResources instance that manages the idle and exploited resources inside a building center's territory.
        /// </summary>
        public void DestroyBuildingCenterResources (Building buildingCenter)
        {
            //see if the building center already has an entry in the centerResources dictionary
            if(centerResources.TryGetValue(buildingCenter, out BuildingCenterResources value))
                //go through exploited resources:
                foreach (Resource r in value.exploitedResources)
                    foreach (Unit unit in r.WorkerMgr.GetAll().ToList())
                        unit.CollectorComp.Stop();

            centerResources.Remove(buildingCenter); //remove the entry for this building center instance
        }

        /// <summary>
        /// Attempts to replace an empty resource by another one of the same type to be exploited.
        /// </summary>
        /// <param name="emptyResource">The Resource instance that is now empty.</param>
        private void ReplaceEmptyResource (Resource emptyResource)
        {
            //search for it in the building center resources instances
            foreach (BuildingCenterResources bcr in centerResources.Values)
                if(bcr.exploitedResources.Contains(emptyResource)) //if this is where the empty list is at:
                {
                    //attempt to find a resource type that's idle and of the same type as the empty resource
                    Resource nextExploitedResource = bcr.GetIdleResourceOfType(emptyResource.GetResourceType());

                    if (nextExploitedResource == null) //nothing found, stop here
                        break;

                    //an idle resource to exploit has been found:
                    bcr.idleResources.Remove(nextExploitedResource);
                    bcr.exploitedResources.Add(nextExploitedResource);
                    //request the resource collector component to handle the collection of that resource
                    npcMgr.GetNPCComp<NPCResourceCollector>().AddResourceToCollect(nextExploitedResource);

                    break;
                }
        }
        #endregion

        #region Border Events Callbacks
        /// <summary>
        /// Called whenever a Border instance adds a new Resource instance to its territory.
        /// </summary>
        /// <param name="border">Border instance that includes the new resource.</param>
        /// <param name="resource">Resource instance that is registered in the border.</param>
        private void OnBorderResourceAdded(Border border, Resource resource)
        {
            //if the building center that activated the resource belongs to this faction
            if (border.building.FactionID == factionMgr.FactionID)
                AddBuildingCenterResource(border.building, resource);
        }

        /// <summary>
        /// Called whenever a Border instance removes a Resource instance from being regulated inside its territory.
        /// </summary>
        /// <param name="border">Border instance that removed the resource.</param>
        /// <param name="resource">Resource instance that is removed from the border.</param>
        private void OnBorderResourceRemoved (Border border, Resource resource)
        {
            //if the building center that removed the resource belongs to this faction
            if (border.building.FactionID == factionMgr.FactionID)
                RemoveBuildingCenterResource(border.building, resource);
        }

        /// <summary>
        /// Called whenever a Border instance is deactivated.
        /// </summary>
        /// <param name="border">The Border instance that has been deactivated.</param>
        private void OnBorderDeactivated(Border border)
        {
            //if the building belongs to this faction & has a Border component:
            if (border.building.FactionID == factionMgr.FactionID)
                DestroyBuildingCenterResources(border.building);
        }
        #endregion

        #region Resource Events Callbacks
        /// <summary>
        /// Called when a resource is empty.
        /// </summary>
        /// <param name="emptyResource">The Resource instance that is now empty.</param>
        public void OnResourceEmpty (Resource resource)
        {
            if (resource.FactionID == factionMgr.FactionID) //if the resource was being collected by this NPC faction.
                ReplaceEmptyResource(resource);
        }
        #endregion
    }
}
