using UnityEngine;

namespace RTSEngine.Movement
{
    /// <summary>
    /// Defines properties and methods required for a movement controller that allows a Unit instance to navigate the map.
    /// </summary>
    public interface IMovementController
    {
        bool IsActive { set; get; }

        LayerMask AreaMask { get; }

        float Radius { get; }

        float Speed { set; get; }

        Vector3 FinalTarget { get; }

        Vector3 NextPathTarget { get; }

        bool Prepare(Vector3 destination);

        void Start();
    }
}
