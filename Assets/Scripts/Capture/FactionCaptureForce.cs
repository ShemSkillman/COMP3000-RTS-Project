using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

public class FactionCaptureForce
{
    private int factionId;
    private Dictionary<CaptorEntity, bool> captorEntities;
    private CaptureableBuilding capturingBuilding;

    public int FactionID { get { return factionId; } }

    public float CapturePointsPerSecond { get { return capturePointsPerSecond; } }

    private float capturePointsPerSecond;

    public FactionCaptureForce(CaptorEntity captorEntity)
    {
        factionId = captorEntity.FactioEntity.FactionID;

        captorEntities = new Dictionary<CaptorEntity, bool>();

        AddUnit(captorEntity);
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

        CaptorEntity[] arr = new CaptorEntity[captorEntities.Count];
        captorEntities.Keys.CopyTo(arr, 0);

        foreach (CaptorEntity entity in arr)
        {
            if (entity.IsDead)
            {
            captorEntities.Remove(entity);
            }
        }

        return !IsEmpty();
    }

    public void SetCapturingBuilding(CaptureableBuilding toCapture)
    {
        capturingBuilding = toCapture;

        capturingBuilding.StartCapture(factionId);
    }

    public void AddUnit(CaptorEntity toAdd)
    {
        if (captorEntities.ContainsKey(toAdd))
        {
            return;
        }

        captorEntities[toAdd] = true;
        capturePointsPerSecond += toAdd.CapturePointsPerSecond;
    }

    public void RemoveUnit(CaptorEntity toRemove)
    {
        if (!captorEntities.ContainsKey(toRemove))
        {
            return;
        }

        captorEntities.Remove(toRemove);
        capturePointsPerSecond -= toRemove.CapturePointsPerSecond;

        if ((IsEmpty() || capturePointsPerSecond == 0) && IsCapturing())
        {
            capturingBuilding.StopCapture();
        }
    }

    public bool IsEmpty()
    {
        return captorEntities.Count < 1;
    }

    public bool IsCapturing()
    {
        return capturingBuilding != null && 
            capturingBuilding.IsBeingCaptured();
    }
}
