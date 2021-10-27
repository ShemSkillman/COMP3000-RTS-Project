using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
    public class BuildingDropOff : MonoBehaviour
    {
        private Building building; //the main building component for which this component opeartes

        [SerializeField]
        private bool isActive = true;
        public bool IsActive { get { return isActive; } } //is this component active?

        [SerializeField]
        private Transform dropOffPosition = null;
        public Vector3 GetDropOffPosition () //if there's a drop off position assigned then use it, if not use the building's position
        {
            return dropOffPosition != null ? dropOffPosition.position : transform.position;
        }
        public float GetDropOffRadius () //get the radius of the drop off position depending on whether or not the drop off position is assigned
        {
            return dropOffPosition != null ? 0.0f : building.GetRadius();
        }

        [SerializeField]
        private bool acceptAllResources = true; //when true then the resource collectors can drop off all resources in this building, if not then the resource must be assigned in the array below
        //a list of the resources that this building accepts to be dropped off in case it doesn't accept all resources
        [SerializeField]
        private ResourceTypeInfo[] acceptedResources = new ResourceTypeInfo[0];

        //a method that initalizes this component:
        public void Init(Building building)
        {
            this.building = building; //get the main building component
            building.FactionMgr.UpdateCollectorsDropOffBuilding(); //go through all collectors and update their drop off buildings since a new one is added

            isActive = true; //mark as active
        }

        //Check if a resource can be dropped off here:
        public bool CanDrop(string Name)
        {
            if (!IsActive) //it's not a drop off building, return false
                return false;

            if (acceptAllResources == true) //it's a drop off building and it accepts all resources, return true
                return true;
            if (acceptedResources.Length > 0)
            { //it does accept some resources, look for the target resource
                for (int i = 0; i < acceptedResources.Length; i++)
                {
                    if (acceptedResources[i].Key == Name) //resource found then return true
                        return true;
                }
            }
            return false; //if target resource is not on the list then return false
        }
    }
}
