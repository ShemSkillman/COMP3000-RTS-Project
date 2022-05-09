using UnityEngine;
using RTSEngine;

public class CaptorEntity : MonoBehaviour
{
    [SerializeField] private float capturePointsPerSecond = 1;
    public float CapturePointsPerSecond { get { return capturePointsPerSecond; } }
    public FactionEntity FactioEntity { get; private set; }

    public bool IsDead
    {
        get
        {
            return FactioEntity.EntityHealthComp.IsDead();
        }
    }

    public int FactionID
    {
        get
        {
            return FactioEntity.FactionID;
        }
    }

    private void Awake()
    {
        FactioEntity = GetComponentInParent<FactionEntity>();
    }

   
}