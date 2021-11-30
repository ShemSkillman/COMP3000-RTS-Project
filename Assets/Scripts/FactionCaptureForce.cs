using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

public class FactionCaptureForce
{
    private int factionId;
    private Dictionary<Unit, bool> factionUnits;
    private CaptureableBuilding capturingBuilding;

    public int FactionID { get { return factionId; } }

    private float capturePointsPerSecond;

    public FactionCaptureForce(Unit factionUnit)
    {
        factionId = factionUnit.FactionID;

        factionUnits = new Dictionary<Unit, bool>();

        AddUnit(factionUnit);
    }

    public void CaptureProgress()
    {
        if (!IsCapturing())
        {
            return;
        }

        capturingBuilding.CaptureProgress(capturePointsPerSecond * Time.deltaTime);
    }

    public bool ValidateFactionUnits()
    {
        if (IsEmpty())
        {
            return false;
        }

        Unit[] arr = new Unit[factionUnits.Count];
        factionUnits.Keys.CopyTo(arr, 0);

        foreach (Unit unit in arr)
        {
            if (unit.EntityHealthComp.IsDead())
            {
            factionUnits.Remove(unit);
            }
        }

        return !IsEmpty();
    }

    public void SetCapturingBuilding(CaptureableBuilding toCapture)
    {
        capturingBuilding = toCapture;

        capturingBuilding.StartCapture(factionId);
    }

    public void AddUnit(Unit toAdd)
    {
        factionUnits[toAdd] = true;
        capturePointsPerSecond += toAdd.CapturePointsPerSecond;
    }

    public void RemoveUnit(Unit toRemove)
    {
        factionUnits.Remove(toRemove);
        capturePointsPerSecond -= toRemove.CapturePointsPerSecond;

        if (IsEmpty() && IsCapturing())
        {
            capturingBuilding.StopCapture();
        }
    }

    public bool IsEmpty()
    {
        return factionUnits.Count < 1;
    }

    public bool IsCapturing()
    {
        return capturingBuilding != null && 
            capturingBuilding.IsBeingCaptured();
    }
}
