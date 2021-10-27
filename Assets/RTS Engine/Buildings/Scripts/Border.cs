using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

/* Border script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
    public class Border : MonoBehaviour
    {
        public Building building { private set; get; } //the main building component for which this component opeartes

        public bool IsActive { private set; get; } //is the border active or not?

        [Header("Border Object:")]
        [SerializeField]
        private bool spawnObj = true; //spawn the border object?
        [SerializeField]
        private GameObject obj; //Use an object that is only visible on the terrain to avoid drawing borders outside the terrain.
        public GameObject Obj { set { obj = value; } get { return obj; } }
        public int Order { private set; get; } //the sorting order of this border, if border A has been activated before border B then border A has higher order than border B.
        //the order is used to determine which has priority over a common area of the map

        [SerializeField]
        private float height = 20.0f; //The height of the border object here
        [Range(0.0f, 1.0f), SerializeField]
        private float colorTransparency = 1.0f; //transparency of the border's object color
        [SerializeField]
        private float size = 10.0f; //The size of the border around this building.
        public float Size { private set { size = value; } get { return size; } }
        [SerializeField]
        private float sizeMultiplier = 2.0f; //To control the relation of the border obj's actual size and the border's map. Using different textures for the border objects will require using 

        //If the border belongs to the player, then this array will only represent the maximum amounts for each building
        //and if a building is not in this array, then the player is free to build as many as he wishes to build.
        //As for NPC Factions, this is handled by NPC components
        [System.Serializable]
        public class BuildingInBorder
        {
            [SerializeField]
            private Building prefab = null; //prefab of the building to be placed inside the border
            public string GetPrefabCode () { return prefab.GetCode(); }
            [SerializeField]
            private FactionTypeInfo factionType = null; //Leave empty if you want this building to be placed by all factions
            public string GetFactionCode () { return factionType.Key; }

            private int currAmount; //current amount of the building type inside this border
            public void UpdateCurrAmount (bool inc) { currAmount += (inc == true) ? 1 : -1; }
            [SerializeField]
            private int maxAmount = 10; //maximum allowed amount of this building type inside this border
            public bool IsMaxAmountReached () { return currAmount >= maxAmount; }
        }

        [Header("Border Buildings:"), SerializeField]
        private List<BuildingInBorder> buildingsInBorder = new List<BuildingInBorder>(); //a list of buildings that are defined to be placed inside this border

        private List<Building> buildingsInRange = new List<Building>(); //a list of the spawned buildings inside the territory defined by this border
        public IEnumerable<Building> GetBuildingsInRange () { return buildingsInRange; }
        private List<Resource> resourcesInRange = new List<Resource>(); //a list of the resources inside the territory defined by this border
        public IEnumerable<Resource> GetResourcesInRange () {
            return resourcesInRange;
        }

        //other components
        private GameManager gameMgr;

        #region Border-Resource Events:
        /// <summary>
        /// Delegate used for Border and Resource related events.
        /// </summary>
        /// <param name="border">Border instance.</param>
        /// <param name="resource">Resource instance.</param>
        public delegate void BorderResourceEventHandler(Border border, Resource resource);

        /// <summary>
        /// Event triggered when a Border instance registers a Resource instance.
        /// </summary>
        public static event BorderResourceEventHandler BorderResourceAdded = delegate { };

        /// <summary>
        /// Event triggered when a Resource instance is no longer registered in a Border instance.
        /// </summary>
        public static event BorderResourceEventHandler BorderResourceRemoved = delegate { };
        #endregion

        #region Initializing/Terminating
        //called to activate the border
        /// <summary>
        /// Initializes the border.
        /// </summary>
        /// <param name="gameMgr">Active GameManager instance in the game.</param>
        /// <param name="building">Building instance that this border is attached to.</param>
        public void Init(GameManager gameMgr, Building building)
        {
            //if the border is already active
            if (IsActive == true)
                return; //do not proceed

            this.gameMgr = gameMgr;
            this.building = building; //get the building that is controlling this border component
            buildingsInRange.Add(this.building); //add source buildings to buildings in range list

            Order = gameMgr.BuildingMgr.LastBorderSortingOrder;

            if (spawnObj == true) //if it's allowed to spawn the border object
            {
                obj = (GameObject)Instantiate(obj, new Vector3(transform.position.x, height, transform.position.z), Quaternion.identity); //create the border obj
                obj.transform.localScale = new Vector3(Size * sizeMultiplier, obj.transform.localScale.y, Size * sizeMultiplier); //set the correct size for the border obj
                obj.transform.SetParent(transform, true); //make sure it's a child object of the building main object

                Color FactionColor = gameMgr.GetFaction(this.building.FactionID).GetColor(); //set its color to the faction that it belongs to
                obj.GetComponent<MeshRenderer>().material.color = new Color(FactionColor.r, FactionColor.g, FactionColor.b, colorTransparency); //set the color transparency

                obj.GetComponent<MeshRenderer>().sortingOrder = Order; //set the border object's sorting order according to the previosuly placed borders
            }

            IsActive = true; //mark the border as active

            //subscribe to following events:
            CustomEvents.ResourceAdded += OnResourceAdded;
            CustomEvents.ResourceDestroyed += OnResourceDestroyed;

            BorderResourceRemoved += OnBorderResourceRemoved;

            CustomEvents.OnBorderActivated(this); //trigger custom event
        }

        /// <summary>
        /// Called when the GameObject where this component is attached is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            //unsubscribe from following events:
            CustomEvents.ResourceAdded -= OnResourceAdded;
            CustomEvents.ResourceDestroyed -= OnResourceDestroyed;

            BorderResourceRemoved -= OnBorderResourceRemoved;
        }

        /// <summary>
        /// Deactivates the border.
        /// </summary>
        public void Disable ()
        {
            if (IsActive == false) //if the border isn't even active:
                return; //do not proceed.

            RemoveAllResources(); //free all resources in this border's territory

            //unsubscribe from following events:
            OnDestroy();

            CustomEvents.OnBorderDeactivated(this); //trigger custom event

            //Destroy the border object if it has been created
            if (spawnObj == true)
                Destroy(obj);
        }
        #endregion

        #region Event Callbacks
        //called to check if one resource is inside the range of the border or not
        /// <summary>
        /// Called when a Resource instance is created and attempts to add it to the Border instance list.
        /// </summary>
        /// <param name="resource">Resource instance that is created.</param>
        private void OnResourceAdded (Resource resource)
        {
            AddResource(resource);
        }

        /// <summary>
        /// Called whenever a Resource instance is destroyed and attempts to remove the resource from the border's territory.
        /// </summary>
        /// <param name="resource">Resource instance that is destroyed.</param>
        private void OnResourceDestroyed (Resource resource)
        {
            RemoveResource(resource);
        }

        /// <summary>
        /// Called when a Border instance removes a Resource instance from its territory and attempts to add the resource to this border territory.
        /// </summary>
        /// <param name="border"></param>
        /// <param name="resource"></param>
        private void OnBorderResourceRemoved (Border border, Resource resource)
        {
            if (border == this) //make sure this is not the same border that removed the resource
                return;

            AddResource(resource); //attempt to add resource.
        }
        #endregion

        #region Border Resource Manipulation
        /// <summary>
        /// Adds a Resource instance to the border list if it belongs to it. 
        /// </summary>
        /// <param name="resource">Resource instance to add.</param>
        private void AddResource (Resource resource)
        {
            if (!resourcesInRange.Contains(resource) //resource is not already registered in this border
                && resource.FactionID == -1 //resource is not already registered to any faction's border
                && Vector3.Distance(resource.transform.position, transform.position) < Size) //make sure the resource is inside the border

            {
                resourcesInRange.Add(resource); //add it to the resources in range list
                resource.FactionID = building.FactionID; //mark it as belonging to this faction ID

                BorderResourceAdded(this, resource); //trigger event
            }
        }

        /// <summary>
        /// Removes all Resource instances registered as part of the border territory.
        /// </summary>
        public void RemoveAllResources()
        {
            while (resourcesInRange.Count > 0)
                RemoveResource(resourcesInRange[0]);
        }

        /// <summary>
        /// Removes a Resource instance from the border resources list.
        /// </summary>
        /// <param name="resource">Resource instance to remove from Border instance.</param>
        public void RemoveResource(Resource resource)
        {
            if(resourcesInRange.Contains(resource)) //only if the resource was already registered in this Border instance
            {
                //resource is no longer in this border's territory
                resourcesInRange.Remove(resource);
                resource.FactionID = -1;

                BorderResourceRemoved(this, resource);
            }
        }
        #endregion

        //register a new building in this border
        public void RegisterBuilding(Building newBuilding)
        {
            buildingsInRange.Add(newBuilding); //add the new building to the list
            foreach (BuildingInBorder bir in buildingsInBorder) //go through all buildings in border slots
                if (bir.GetPrefabCode() == newBuilding.GetCode()) //if the code matches
                    bir.UpdateCurrAmount(true); //increase the current amount
        }

        //unregister an old building from this border
        public void UnegisterBuilding(Building oldBuilding)
        {
            buildingsInRange.Remove(oldBuilding); //remove the building from the list
            foreach (BuildingInBorder bir in buildingsInBorder) //go through all buildings in border slots
                if (bir.GetPrefabCode() == oldBuilding.GetCode()) //if the code matches
                    bir.UpdateCurrAmount(false); //decrease the current amount
        }

        //check if a building is allowed inside this border or not (using the building's code
        public bool AllowBuildingInBorder(string code)
        {
            foreach (BuildingInBorder bir in buildingsInBorder) //go through all buildings in border slots
                if (bir.GetPrefabCode() == code) //if the code matches
                    return !bir.IsMaxAmountReached(); //allow if the current amount still hasn't reached the max amount

            return true; //if the building type doesn't have a defined slot in the buildings in border list, then it can be definitely accepted.
        }

        //check all buildings placed in the range of this border
        public void CheckBuildings()
        {
            int i = 0;
            while (i < buildingsInBorder.Count) //go through the buildings in border slots
                if (buildingsInBorder[i].GetFactionCode() != "" 
                    && (gameMgr.GetFaction(building.FactionID).GetTypeInfo() == null 
                        || gameMgr.GetFaction(building.FactionID).GetTypeInfo().Key != buildingsInBorder[i].GetFactionCode())) //if the faction code is specified and doesn't match the building's faction
                    buildingsInBorder.RemoveAt(i); //remove this slot
                else
                    i++;
        }
    }
}