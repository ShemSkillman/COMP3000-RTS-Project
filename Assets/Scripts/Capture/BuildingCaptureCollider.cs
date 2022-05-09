using UnityEngine;
using RTSEngine;

public class BuildingCaptureCollider : MonoBehaviour
{
    Collider col;
    Building building;

    private void Awake()
    {
        col = GetComponent<Collider>();
        building = GetComponentInParent<Building>();
    }

    private void Update()
    {
        col.enabled = (building.IsBuilt && building.Placed);
    }
}