using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

public class VillageCapture : MonoBehaviour
{
    [SerializeField] private float captureRadius = 15f;

    SphereCollider captureCollider;

    Dictionary<int, FactionCaptureForce> factionCaptureForces;

    CaptureableBuilding[] villageBuildings;

    private void Awake()
    {
        captureCollider = GetComponent<SphereCollider>();

        factionCaptureForces = new Dictionary<int, FactionCaptureForce>();

        villageBuildings = GetVillageBuildings();

        gameObject.AddComponent<SphereCollider>();
        captureCollider = GetComponent<SphereCollider>();
    }

    private void Start()
    {
        captureCollider.isTrigger = true;
        captureCollider.center = GetVillageCentreLocal();
        captureCollider.radius = captureRadius;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetVillageCentreWorld(), captureRadius);
    }

    private Vector3 GetVillageCentreWorld()
    {
        Vector3 centrePos = Vector3.zero;

        for (int i = 0; i < transform.childCount; i++)
        {
            centrePos += transform.GetChild(i).transform.position;
        }

        centrePos /= transform.childCount;
        centrePos.y = 0;

        return centrePos;
    }

    private Vector3 GetVillageCentreLocal()
    {
        return transform.InverseTransformPoint(GetVillageCentreWorld());
    }

    private void Update()
    {
        ValidateFactionForces();

        if (factionCaptureForces.Count < 1)
        {
            return;
        }

        UpdateCaptureBuildings();
    }

    private void ValidateFactionForces()
    {
        if (factionCaptureForces.Count < 1)
        {
            return;
        }

        FactionCaptureForce[] caputreForces = new FactionCaptureForce[factionCaptureForces.Count];
        factionCaptureForces.Values.CopyTo(caputreForces, 0);

        foreach (FactionCaptureForce captureForce in caputreForces)
        {
            if (!captureForce.ValidateFactionUnits())
            {
                factionCaptureForces.Remove(captureForce.FactionID);
            }
        }
    }

    private void UpdateCaptureBuildings()
    {
        List<CaptureableBuilding> vulnerableBuildings = GetVulnerableBuildings();

        foreach (FactionCaptureForce factionForce in factionCaptureForces.Values)
        {
            if (!factionForce.IsCapturing() && factionForce.CapturePointsPerSecond > 0
                && vulnerableBuildings.Count > 0)
            {
                FactionCaptureForce toModify = factionForce;

                int randomIndex = Random.Range(0, vulnerableBuildings.Count);

                toModify.SetCapturingBuilding(vulnerableBuildings[randomIndex]);

                vulnerableBuildings.RemoveAt(randomIndex);
            }

            factionForce.CaptureProgress();
        }
    }

    private List<CaptureableBuilding> GetVulnerableBuildings()
    {
        List<CaptureableBuilding> ret = new List<CaptureableBuilding>();

        foreach (CaptureableBuilding building in villageBuildings)
        {
            if (!building.IsBeingCaptured() && !factionCaptureForces.ContainsKey(building.FactionID))
            {
                ret.Add(building);
            }
        }

        return ret;
    }   

    private CaptureableBuilding[] GetVillageBuildings()
    {
        CaptureableBuilding[] buildings = new CaptureableBuilding[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            buildings[i] = transform.GetChild(i).GetComponentInChildren<CaptureableBuilding>();
        }

        return buildings;
    }

    private void OnTriggerEnter(Collider other)
    {
        CaptorEntity captorEntity = other.GetComponentInParent<CaptorEntity>();
        if (captorEntity != null)
        {
            if (factionCaptureForces.ContainsKey(captorEntity.FactionID))
            {
                factionCaptureForces[captorEntity.FactionID].AddUnit(captorEntity);
            }
            else
            {
                factionCaptureForces[captorEntity.FactionID] = new FactionCaptureForce(captorEntity);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CaptorEntity captorEntity = other.GetComponentInParent<CaptorEntity>();
        if (captorEntity != null)
        {
            if (factionCaptureForces.ContainsKey(captorEntity.FactionID))
            {
                factionCaptureForces[captorEntity.FactionID].RemoveUnit(captorEntity);
            }
        }
    }
}