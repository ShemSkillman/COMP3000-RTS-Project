using UnityEngine;
using RTSEngine;
using UnityEngine.UI;

public class CaptureableBuilding : MonoBehaviour
{
    [SerializeField] private float requiredCapturePoints = 100f;

    private int captorFactionId = -1;
    private float capturePointProgress = -1;

    Building buildingComp;
    Image progressUI;

    public int FactionID
    {
        get
        {
            return buildingComp.FactionID;
        }
    }

    private void Awake()
    {
        buildingComp = GetComponentInParent<Building>();
        progressUI = GetComponentInChildren<Image>();
    }

    private void Update()
    {
        progressUI.gameObject.SetActive(IsBeingCaptured());
    }

    public void StartCapture(int captorFactionId)
    {
        this.captorFactionId = captorFactionId;
        capturePointProgress = 0;
        
        Color captorColor = buildingComp.GetFactionColor(captorFactionId).color;
        captorColor.a = 1;

        progressUI.color = captorColor;
    }

    public void StopCapture()
    {
        capturePointProgress = -1;
        captorFactionId = FactionID;
    }

    public void CaptureProgress(float capturePoints)
    {
        if (IsCaptured())
        {
            return;
        }

        capturePointProgress += capturePoints;

        progressUI.fillAmount = capturePointProgress / requiredCapturePoints;

        if (capturePointProgress >= requiredCapturePoints)
        {
            capturePointProgress = -1;
            buildingComp.SetFaction(captorFactionId);
        }
    }

    public bool IsBeingCaptured()
    {
        return captorFactionId != FactionID && capturePointProgress >= 0;
    }

    public bool IsCaptured()
    {
        return captorFactionId == FactionID;
    }
}
