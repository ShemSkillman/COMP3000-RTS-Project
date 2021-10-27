using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Assertions;

/* Resource Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    public struct ResourceInput
    {
        [ResourceType]
        public string Name;
        public int Amount;
    }

    [System.Serializable]
    public struct ResourceInputRange
    {
        [ResourceType]
        public string Name;
        public IntRange Amount;
    }

    public class ResourceManager : MonoBehaviour {

        [SerializeField]
        private Transform resourcesParent = null; //All resources must be placed as children of the same object.

        [System.Serializable]
        //This array appears in the inspector, it's where you can create the resources types:
        public class MapResource
        {
            [SerializeField]
            private ResourceTypeInfo type = null; //the resource type info asset file goes here
            public ResourceTypeInfo GetResourceType() { return type; }

            //UI attributes:
            [SerializeField]
            private bool showUI = false; //show this resource in the UI panel? 

            //If the resource type is supposed to be displayed in UI, 
            [SerializeField, Tooltip("Define the faction types for which is it allowed to display this resource type in the UI.")]
            private FactionTypeTargetPicker allowedFactionTypes = new FactionTypeTargetPicker();

            [SerializeField]
            private Image imageUI = null; //Resource UI image.
            [SerializeField]
            private Text textUI = null; //Resource UI text to display the resource amount.

            //runtime attributes:
            private int currAmount; //the current amount of this resource.
            public void UpdateCurrAmount(int value) { currAmount += value; }
            public int GetCurrAmount() { return currAmount; }
            public void ResetCurrAmount() { currAmount = type.GetStartingAmount(); } //resets the curr amount to the starting amount of the resource type

            //NPC Resource Tasks:
            public int TargetAmount { set; get; } //the target amount that the faction wants to reach.
            public int LastCenterID { set; get; } //Whenever a resource is missing, we start searching for it from a faction center. This variable holds the last ID of the faction center that we started the search from.

            //constructor
            public MapResource (ResourceTypeInfo type)
            {
                this.type = type;
                this.currAmount = this.type.GetStartingAmount();
            }

            /// <summary>
            /// Initializes the UI elements of the resource type by checking whether they are valid or not (for player faction only).
            /// </summary>
            /// <param name="sourceFactionType">The FactionTypeInfo instance assigned to the player's faction.</param>
            public void InitUI (FactionTypeInfo sourceFactionType)
            {
                if (!showUI //if the UI is already deactivated.
                    || allowedFactionTypes.IsValidTarget(sourceFactionType) == ErrorMessage.invalidTarget) //make sure the faction type for which this resource is defined is valid to display for UI
                {
                    showUI = false;
                    //in case it is not, then hide the image and text UI elements in case they have been assigned.
                    imageUI?.gameObject.SetActive(false);
                    textUI?.gameObject.SetActive(false);
                    return;
                }

                //UI is still active, make sure that there's a proper text UI that can be assigned the amount of the resource
                Assert.IsNotNull(textUI,
                    $"[ResourceManager] UI is active for resource type {type.Key} however no UI text object is assigned to display the resource's amount.");

                if(imageUI) //if there's a UI image assigned, then set it to the resource's icon
                    imageUI.sprite = type.GetIcon();
            }

            /// <summary>
            /// Updates the resource's counter on the assigned UI text element.
            /// </summary>
            public void UpdateUI ()
            {
                if(showUI) //if we're allowed to display the resource's amount in UI
                    textUI.text = currAmount.ToString();
            }


        }
        [SerializeField]
        private MapResource[] mapResources = new MapResource[0];
        public int GetMapResourcesCount () { return mapResources.Length; }
        public IEnumerable<MapResource> GetMapResources () { return mapResources; }
        public int GetResourceID (string name) { return mapResourcesTable.TryGetValue(name, out int id) ? id : -1; }

        //key = resource_name (string) / value = resource id in above "mapResources" array (int)
        private Dictionary<string, int> mapResourcesTable = new Dictionary<string, int>();

        //This array doesn't appear in the inspector, its values are set by the game manager depending on the number of factions playing
        [System.Serializable]
        public class FactionResources
        {
            public MapResource[] Resources { private set; get; } //For each team, we'll associate all the resources types.
            public void UpdateUI() { foreach (MapResource r in Resources) { r.UpdateUI(); } }
            
            // >= 1.0f, when faction needs to spend amount X of a resource, it must have X * resourceExploitRatio from that resource
            private float resourceNeedRatio = 1.0f;
            public float ResourceNeedRatio {
                set {
                    if (value >= 1.0f) //the value must be >+ 1.0
                        resourceNeedRatio = value;
                }
                get
                {
                    return resourceNeedRatio;
                }
            }

            /// <summary>
            /// Sets the resource need ratio.
            /// </summary>
            /// <param name="newRatio">Ney ratio value in [0.0, 1.0]</param>
            public void UpdateResourceNeedRatio (float newRatio)
            {
                resourceNeedRatio = Mathf.Clamp(newRatio, 0.0f, 1.0f);
            }


            //constructor
            public FactionResources (MapResource[] mapResources, bool playerFaction, float needRatio, FactionTypeInfo sourceFactionType)
            {
                if (playerFaction) //if this is the player's faction
                {
                    Resources = mapResources; //because we want to keep the UI elements references
                    foreach (MapResource r in Resources)
                    {
                        r.ResetCurrAmount();
                        r.InitUI(sourceFactionType); //initialize the resource's UI elements
                    }
                }
                else //if not..
                {
                    //make a deep copy of the map resources
                    Resources = new MapResource[mapResources.Length];
                    for (int i = 0; i < Resources.Length; i++)
                        Resources[i] = new MapResource(mapResources[i].GetResourceType());
                }

                resourceNeedRatio = needRatio;
            }
        }
        private FactionResources[] factionsResources = new FactionResources[0];
        public FactionResources PlayerFactionResources { private set; get; }

        /// <summary>
        /// Gets the FactionResources instance for faction.
        /// </summary>
        /// <param name="factionID">ID of the faction.</param>
        /// <returns>FactionResources instance of the faction of the ID is valid, otherwise null.</returns>
        public FactionResources GetFactionResources(int factionID) {
            Assert.IsTrue(factionID >= 0 && factionID < factionsResources.Length,
                $"[ResourceManager] Invalid Faction ID!");

            return factionsResources[factionID];
        }

        [SerializeField]
        private bool autoCollect = true; //Collect resources automatically when true. if false, the unit must drop off the collected resources each time at a building that allow that.
        public bool CanAutoCollect () { return autoCollect; }

        //selection color the resources:
        [SerializeField]
        private Color resourceSelectionColor = Color.green;

        private List<Resource> allResources = new List<Resource>(); //holds a list of all resources
        public IEnumerable<Resource> GetAllResources() { return allResources; }
        public int GetResourcesCount() { return allResources.Count; }
        public void AddResource(Resource r) { if(!allResources.Contains(r)) allResources.Add(r); }
        public void RemoveResource(Resource r) { allResources.Remove(r); }

		private GameManager gameMgr;

        //called before initializing the faction slots
        public void Init (GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            //add all map resource types to the hashtable
            for (int i = 0; i < mapResources.Length; i++)
                mapResourcesTable.Add(mapResources[i].GetResourceType().Key, i);
        }

        //called by the game manager after initializing the faction slots
        public void InitResources ()
        {
            allResources = new List<Resource>(); //go through all the resources that are children of the Resources Parent game object and init them by adding them to the all resources list
            foreach (Resource r in resourcesParent.GetComponentsInChildren<Resource>(true))
                r.Init(this.gameMgr);
        }

        //a method that initializes resources for factions.
        public void InitFactionResources ()
        {
            //Create as many faction  slots as the amount of the spawned factions.
            factionsResources = new FactionResources[gameMgr.GetFactionCount()];

            //Loop through all the factions:
            for (int i = 0; i < factionsResources.Length; i++)
            {
                factionsResources[i] = new FactionResources(
                    mapResources,
                    i == GameManager.PlayerFactionID,
                    1.0f,
                    gameMgr.GetFaction(i).GetTypeInfo());

                if (i == GameManager.PlayerFactionID)
                    PlayerFactionResources = factionsResources[i];
            }
            PlayerFactionResources.UpdateUI(); //right after setting up the resource settings above, refresh the resource UI.
        }

        /// <summary>
        /// Adds/removes an amount of a faction's resource.
        /// </summary>
        /// <param name="factionID">ID of the faction whose resources will be updated.</param>
        /// <param name="name">Name of the resource type to add/remove.</param>
        /// <param name="amount">Amount of the resource to add/remove.</param>
        /// <param name="add">Adds the resources when true, otherwise removes the resources from the faction.</param>
		public void UpdateResource (int factionID, string name, int amount, bool add = true)
		{
            if (!add && factionID == GameManager.PlayerFactionID && GodMode.Enabled)
                return;

            amount = (add ? 1 : -1) * amount; //add or remove the resources?

            if(mapResourcesTable.TryGetValue(name, out int id)) //if the resource name corresponds to a valid ID
            {
                //Add the resource amount.
                factionsResources[factionID].Resources[id].UpdateCurrAmount(amount);
                if (factionID == GameManager.PlayerFactionID) //if this is the player faction, update the UI
                    PlayerFactionResources.UpdateUI();

                CustomEvents.OnFactionResourceUpdate(factionsResources[factionID].Resources[id].GetResourceType(), factionID, amount);
			}
            else
                Debug.LogError($"[ResourceManager] The resource type with name: {name} is not defined in the mapResources array.");
		}

        /// <summary>
        /// Adds/removes an amount of a faction's resource.
        /// </summary>
        /// <param name="factionID">ID of the faction whose resources will be updated.</param>
        /// <param name="resourceInputArray">Array where each element defines a resource type and the amount to add/remove.</param>
        /// <param name="add">Adds the resources when true, otherwise removes the resources from the faction.</param>
        public void UpdateResource(int factionID, IEnumerable<ResourceInput> resourceInputArray, bool add = true)
        {
            foreach (ResourceInput ri in resourceInputArray)
                UpdateResource(factionID, ri.Name, ri.Amount, add);
        }

        /// <summary>
        /// Adds/removes an amount of a faction's resource.
        /// </summary>
        /// <param name="factionID">ID of the faction whose resources will be updated.</param>
        /// <param name="resourceInput">Defines the resource type and amount to add/remove.</param>
        /// <param name="add">Adds the resources when true, otherwise removes the resources from the faction.</param>
        public void UpdateResource(int factionID, ResourceInput resourceInput, bool add = true)
        {
            UpdateResource(factionID, resourceInput.Name, resourceInput.Amount, add);
        }

        //a method that gets the resource amount by providing the faction ID and name of the resource.
		public int GetResourceAmount (int factionID, string name)
		{
            if (mapResourcesTable.TryGetValue(name, out int id))
                return factionsResources[factionID].Resources[id].GetCurrAmount();
            else
            {
                Debug.LogError($"[Resource Manager] The resource type with name: {name} is not defined in the mapResources array.");
                return 0;
            }
		}

		//a method that gets called to check whether a faction has the resoureces defined in the first param or not
		public bool HasRequiredResources (ResourceInput[] requiredResources, int factionID)
		{
            //if this is the local player and god mode is enabled
            if (factionID == GameManager.PlayerFactionID && GodMode.Enabled == true)
                return true;

            foreach(ResourceInput r in requiredResources) //go through all required resources
            {
                //if required resource amount can not be provided by the faction
                if (GetResourceAmount(factionID, r.Name) < r.Amount * factionsResources[factionID].ResourceNeedRatio)
                    return false;
            }

            return true; //reaching this point means that the faction has all required resources
		}

        //a method that adds/removes resources using the required resources param
        public void UpdateRequiredResources (ResourceInput[] requiredResources, bool add, int factionID)
        {
            //if we're taking resources from the player's faction while god mode is enabled
            if (!add && factionID == GameManager.PlayerFactionID && GodMode.Enabled)
                return; //do not take nothing

            foreach(ResourceInput r in requiredResources) //go through all required resources
                UpdateResource(factionID, r.Name, (add ? 1 : -1) * r.Amount); //add or remove resources
        }

        //a method that spawns a resource instance:
        public void CreateResource(Resource resourcePrefab, Vector3 spawnPosition)
        {
            if (resourcePrefab == null) //invalid prefab
                return;

            if (GameManager.MultiplayerGame == false) //single player game:
                CreateResourceLocal(resourcePrefab, spawnPosition);
            else
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.resource,

                    initialPosition = spawnPosition
                };
                InputManager.SendInput(newInput, resourcePrefab, null);
            }
        }

        //a method that creates a resource instance locally:
        public Resource CreateResourceLocal (Resource resourcePrefab, Vector3 spawnPosition)
        {
            if (resourcePrefab == null) //invalid prefab
                return null;

            Resource newResource = Instantiate(resourcePrefab.gameObject, spawnPosition, resourcePrefab.transform.rotation).GetComponent<Resource>(); //spawn the new resource

            newResource.Init(gameMgr); //initiate resource settings

            return newResource;
        }

        //static help methods regarding resources:

        //filter a resource list depending on a certain name
        public static List<Resource> FilterResourceList(IEnumerable<Resource> resourceList, string name)
        {
            //result list:
            List<Resource> filteredResourceList = new List<Resource>();
            //go through the input resource list:
            foreach(Resource r in resourceList)
            {
                if (r.GetResourceType().Key == name) //if it has the name we need
                    filteredResourceList.Add(r); //add it
            }

            return filteredResourceList;
        }
	}
}