using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

public class VillageCapture : MonoBehaviour
{
    Dictionary<int, FactionCaptureForce> factionCaptureForces;

    CaptureableBuilding[] villageBuildings;

    private void Awake()
    {
        factionCaptureForces = new Dictionary<int, FactionCaptureForce>();

        villageBuildings = GetVillageBuildings();
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
            if (!factionForce.IsCapturing() && vulnerableBuildings.Count > 0)
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
        Unit factionUnit = other.GetComponentInParent<Unit>();
        if (factionUnit != null)
        {
            if (factionCaptureForces.ContainsKey(factionUnit.FactionID))
            {
                factionCaptureForces[factionUnit.FactionID].AddUnit(factionUnit);
            }
            else
            {
                factionCaptureForces[factionUnit.FactionID] = new FactionCaptureForce(factionUnit);
            }            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Unit factionUnit = other.GetComponentInParent<Unit>();
        if (factionUnit != null)
        {
            if (factionCaptureForces.ContainsKey(factionUnit.FactionID))
            {
                factionCaptureForces[factionUnit.FactionID].RemoveUnit(factionUnit);
            }
        }
    }
}