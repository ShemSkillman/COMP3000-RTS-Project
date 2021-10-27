using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace RTSEngine.Movement
{
    /// <summary>
    /// Handles unit movement using a NavMeshAgent component and Unity's NavMesh solution.
    /// </summary>
    public class NavMeshAgentController : IMovementController
    {
        /// <summary>
        /// Toggles the NavMeshAgent component responsible for handling the movement.
        /// </summary>
        public bool IsActive { set { navAgent.isStopped = !value; } get { return !navAgent.isStopped; } }

        private NavMeshAgent navAgent; //Navigation Agent component attached to the unit's object.
        private NavMeshPath navPath; //we'll be using the navigation agent to compute the path and store it here then move the unit manually

        /// <summary>
        /// The navigation mesh area mask in which the unit can move.
        /// </summary>
        public LayerMask AreaMask { get { return navAgent.areaMask; } }

        /// <summary>
        /// The size that the unit occupies in the navigation mesh while moving.
        /// </summary>
        public float Radius { get { return navAgent.radius; } }

        /// <summary>
        /// How fast does the unit navigate the mesh?
        /// </summary>
        public float Speed { set { navAgent.speed = value; } get { return navAgent.speed; } }

        /// <summary>
        /// The position of the next corner of the unit's active path.
        /// </summary>
        public Vector3 NextPathTarget { get { return navAgent.steeringTarget; } }

        /// <summary>
        /// The position of the last corner tof the unit's active path, AKA, the path's destination.
        /// </summary>
        public Vector3 FinalTarget { get { return navAgent.destination; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit">Unit instance whose movement is handled by this component.</param>
        /// <param name="speed">Initial speed value.</param>
        /// <param name="acceleration">Initial acceleration value.</param>
        /// <param name="angularSpeed">Initial movement angular speed.</param>
        /// <param name="stoppingDistance">Initial stopping distance, fetched from the MovementManager component.</param>
        public NavMeshAgentController (Unit unit, float speed, float acceleration, float angularSpeed, float stoppingDistance)
        {
            Assert.IsNotNull(unit, "[NavMeshAgentController] Can not initialize without a valid Unit instance.");

            navAgent = unit.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
            Assert.IsNotNull(navAgent, "[NavMeshAgentController] NavMeshAgent component is not attached to the unit.");
            navAgent.enabled = true;

            navPath = new NavMeshPath();

            //always set to none as Navmesh's obstacle avoidance desyncs multiplayer game since it is far from determinsitci
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            //make sure the NavMeshAgent component updates our unit's position.
            navAgent.updatePosition = true;

            //set the initial navagent params
            Speed = speed;
            navAgent.acceleration = acceleration;
            navAgent.angularSpeed = angularSpeed;
            navAgent.stoppingDistance = stoppingDistance;
        }

        /// <summary>
        /// Attempts to calculate a valid path for the specified destination position.
        /// </summary>
        /// <param name="destination">Vector3 that represents the movement's target position.</param>
        /// <returns>True if the path is valid and complete, otherwise false.</returns>
        public bool Prepare(Vector3 destination)
        {
            navAgent.CalculatePath(destination, navPath); 

            return navPath != null && navPath.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>
        /// Starts the unit movement using the last calculated path from the "Prepare()" method.
        /// </summary>
        public void Start ()
        {
            navAgent.SetPath(navPath);
            navAgent.isStopped = false;
        }
    }
}
