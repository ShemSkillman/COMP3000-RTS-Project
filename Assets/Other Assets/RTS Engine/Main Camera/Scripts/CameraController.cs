using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;
using RTSEngine;

namespace RTSCamera
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField, Header("General"), Tooltip("The main camera in the scene.")]
        private Camera mainCamera = null; //the main RTS camera
        /// <summary>
        /// Gets the main camera in the game.
        /// </summary>
        public Camera MainCamera { get { return mainCamera; } }
        [SerializeField, Tooltip("Child of the main camera, used to display UI elements only.")]
        private Camera mainCameraUI = null; //a camera, child objet of the main camera (local positon = 0,0,0), optional and used to display the UI elements only
        //a method that returns the viewport point of a screen point regarding the main camera
        public Vector3 ScreenToViewportPoint(Vector3 position) { return mainCamera.ScreenToViewportPoint(position); }

        [SerializeField, Tooltip("The object with the MinimapCameraController component.")]
        private MinimapCameraController minimapCameraController = null; //the component responsible for handling minimap camera
        public MinimapCameraController MinimapCameraController { get { return minimapCameraController; } }

        //the layer mask that holds the layers that identify the terrain objects
        [SerializeField, Tooltip("Layers assigned to the terrain objects.")]
        private LayerMask terrainLayerMask = new LayerMask();
        //determine if an object belongs to the terrain:
        public bool IsTerrain(GameObject obj)
        {
            return terrainLayerMask == (terrainLayerMask | (1 << obj.layer));
        }


        //speed struct that allows to control a movement/rotation speed by accelerating it towards a max value and decelerating it towards 0
        [System.Serializable]
        public struct Speed
        {
            public float value;
            public float smoothFactor;
        }

        [SerializeField, Header("Panning"), Tooltip("How fast does the camera pan?")]
        private Speed panningSpeed = new Speed { value = 20.0f, smoothFactor = 0.1f };

        //if the input axis panning is enabling, axis defined in the input manager can be used to move the camera
        [System.Serializable]
        public struct InputAxisPanning
        {
            public bool enabled;
            public string horizontal;
            public string vertical; 
        }
        [SerializeField, Tooltip("Pan the camera using input axis.")]
        private InputAxisPanning inputAxisPanning = new InputAxisPanning { enabled = true, horizontal = "Horizontal", vertical = "Vertical" };

        //if the keyboard button panning is enabling, player will be able to use keyboard keys to move the camera
        [System.Serializable]
        public struct KeyboardKeyPanning
        {
            public bool enabled;
            public KeyCode up;
            public KeyCode down;
            public KeyCode right;
            public KeyCode left;
        }
        [SerializeField, Tooltip("Pan the camera using keys.")]
        private KeyboardKeyPanning keyboardKeyPanning = new KeyboardKeyPanning { enabled = false};

        //when the player's mouse cursor is on the edge of the screen, should the camera move or not?
        [System.Serializable]
        public struct ScreenEdgePanning
        {
            public bool enabled;
            public float size; //size of the screen edge
            public bool ignoreUI; //if enabled and the player's mouse cursor is on the screen edge but over a UI element, camera won't move.
        }
        [SerializeField, Tooltip("Pan the camera when the mouse is over the screen edge.")]
        private ScreenEdgePanning screenEdgePanning = new ScreenEdgePanning { enabled = true, size = 25.0f, ignoreUI = false};

        //Limit the pan of the camera on the x and z axis? 
        [System.Serializable]
        public struct PanningLimit
        {
            public bool enabled;
            public Vector2 minPosition; //the minimum (x,z) values that the camera is allowed to have as its position
            public Vector2 maxPosition; //the maximum (x,z) values that the camera is allowed to have as its position
        }
        [SerializeField, Tooltip("Limit the position that the camera can pan to.")]
        private PanningLimit panLimit = new PanningLimit { enabled = true, minPosition = new Vector2(-20.0f, -20.0f), maxPosition = new Vector2(120.0f, 120.0f)};
        
        private Vector3 currPanDirection = Vector3.zero; //holds the current and last direction for the next movement
        private Vector3 lastPanDirection = Vector3.zero;
        public bool IsPanning () { return currPanDirection != Vector3.zero; } //is the camera moving according to the panning inputs?

        [SerializeField, Header("Rotation"), Tooltip("Defines the initial rotation of the main camera.")] //rotation of the main camera on the y axis
        private Vector3 initialEulerAngles = new Vector3(45.0f, 45.0f, 0.0f); //the initial rotation of the main camera
        private Quaternion initialRotation;
        [SerializeField, Tooltip("Update the minimap's rotation to fit the main camera?")]
        private bool updateMinimapCamRotation = true; //override the minimap camera rotation using input from the main camera rotation

        [SerializeField, Tooltip("Have a fixed rotation when the camera is panning?")]
        private bool fixPanRotation = true; //the rotation will get reset to the initial one when the player is moving the main camera
        [SerializeField, Min(0), Tooltip("How far can the camera move before reverting to the initial rotation (if above field is enabled).")]
        private float allowedRotationPanSize = 0.2f; //when the size of the panning direction is lower than this value, rotation is allowed even if fix pan rotation is enabled

        [SerializeField, Tooltip("How fast can the camera rotate?")]
        private Speed rotationSpeed = new Speed { value = 40.0f, smoothFactor = 0.1f}; //how fast will the camera rotation be?

        [System.Serializable]
        public struct RotationLimit
        {
            public bool enabled;
            public float min;
            public float max;
        }
        [SerializeField, Tooltip("Limit the roation of the main camera.")]
        private RotationLimit rotationLimit = new RotationLimit { enabled = true, min = 0.0f, max = 90.0f};

        //if enabled, the player will be able to rotate the camera using keyboard keys
        [System.Serializable]
        public struct KeyboardKeyRotation
        {
            public bool enabled;
            public KeyCode positive;
            public KeyCode negative;
        }
        [SerializeField, Tooltip("Rotate the camera with keys")]
        private KeyboardKeyRotation keyboardKeyRotation = new KeyboardKeyRotation { enabled = false };

        //if enabled, the player will be able to use the mousewheel button click to rotate the camera:
        [System.Serializable]
        public struct MouseWheelRotation
        {
            public bool enabled;
            public float smoothFactor;
        }
        [SerializeField, Tooltip("Rotate the camera with the mouse wheel.")]
        private MouseWheelRotation mouseWheelRotation = new MouseWheelRotation { enabled = true, smoothFactor = 0.1f };

        private float currRotationValue = 0; //the current and last rotation value that is determined using the different rotation inputs.
        private float lastRotationValue;

        //zooming in/out manipualtes the main camera's FOV (field of view)
        [SerializeField, Header("Zoom"), Tooltip("How fast can the main camera zoom?")]
        private Speed zoomSpeed = new Speed { value = 1.0f, smoothFactor = 0.1f };

        [SerializeField, Tooltip("Enable to allow the player to zoom in/out while placing a building.")]
        private bool buildingPlacementZoomEnabled = true;

        //when enabled, the player will be able to use the mouse axis to zoom in/out the main camera
        [System.Serializable]
        public struct MouseWheelZoom
        {
            public bool enabled;
            public bool invert;
            public string name;
            public float sensitivity;
        }
        [SerializeField, Tooltip("Use the mouse wheel to zoom.")]
        private MouseWheelZoom mouseWheelZoom = new MouseWheelZoom { enabled = true, invert = false, name = "Mouse ScrollWheel", sensitivity = 20.0f};

        //when enabled, the player will be able to use keyboard keys to zoom in/out the main camera
        [System.Serializable]
        public struct KeyboardKeyZoom
        {
            public bool enabled;
            public KeyCode inKey;
            public KeyCode outKey;
        }
        [SerializeField, Tooltip("Zoom using keys.")]
        private KeyboardKeyZoom keyboardKeyZoom = new KeyboardKeyZoom { enabled = false};

        [SerializeField, Tooltip("Enable to zoom using the camera's field of view.")]
        private bool zoomFOV = false; //adjust the field of view of the camera instead of the height (position on the y-axis)?
        [SerializeField, Tooltip("The height that the main camera starts with.")]
        private float initialHeight = 20.0f; //the initial height that the camera starts with
        private float zoomValue = 0.0f; //gets either incremented or decremented depending on the zoom inputs
        //the minimum and maximum camera height that the player is allowed to go through
        [SerializeField, Tooltip("The min height the main camera is allowed to have.")]
        private float minHeight = 20.0f;
        [SerializeField, Tooltip("The max height the main camera is allowed to have.")]
        private float maxHeight = 50.0f;

        [SerializeField, Header("Look At"), Tooltip("X-axis camera position offset value.")]
        //camera look at offset values, when the camera is looking at a position, this value is the offset that the camera position will have on the x and z axis
        //the value of the offset depends on the camera's rotation on the x-axis
        private float offsetX = -15;
        [SerializeField, Tooltip("Z-axis camera position offset value.")]
        private float offsetZ = -15;
        //the above two floats are the initial offset values used for the initial height of the main camera, the curr offsets are updated when the player changes the height
        private float currOffsetX;
        private float currOffsetZ;

        //[SerializeField, Tooltip("The targe that the camera follows.")]
        private Transform followTarget = null; //when assigned, the camera will follow the transform
        /// <summary>
        /// Updates the target that the camera will be following
        /// </summary>
        public void SetFollowTarget (Transform transform) {
            followTarget = transform;
            //reset movement inputs
            currPanDirection = Vector3.zero;
            lastPanDirection = Vector3.zero;
        }
        public bool IsFollowingTarget () { return followTarget != null; } 
        [SerializeField, Tooltip("Does the camera follow its target smoothly?")]
        private bool smoothFollow = true;
        [SerializeField, Tooltip("How smooth does the camera follow its target?")]
        private float smoothFollowFactor = 0.1f;
        [SerializeField, Tooltip("Does the camera stop following its target when it moves?")]
        private bool stopFollowingOnMovement = true; //when enabled, the camera will stop following the follow target if the player moves the camera

        //other fields
        private Vector3 lastMousePosition;

        //manager components
        private GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            initialRotation = Quaternion.Euler(initialEulerAngles); //set the initial rotation and apply it to the main camera

            Assert.IsNotNull(mainCamera, "[RTS Camera Controller] The field 'Main Camera' hasn't been assigned!");
            mainCamera.transform.rotation = initialRotation;

            Assert.IsNotNull(mainCamera, "[RTS Camera Controller] The field 'Minimap Camera Controller' hasn't been assigned!");
            minimapCameraController.Init(gameMgr, this, updateMinimapCamRotation, initialEulerAngles.y, terrainLayerMask); //initialize the minimap camera controller component

            //set the initial height of the main camera
            Assert.IsTrue(initialHeight >= minHeight && initialHeight <= maxHeight,
                "[RTS Camera Controller] The initial height must be between the minimum and maximum allowed height values.");

            zoomValue = (initialHeight - minHeight) / (maxHeight - minHeight);
            if (zoomFOV) //if we're adjusting the field of view for the zoom
                UpdateCameraFOV(initialHeight);
            else //else simply set the height of the camera
                mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, initialHeight, mainCamera.transform.position.y);

            currOffsetX = offsetX;
            currOffsetZ = offsetZ;
        }

        private void Update()
        {
            //update all the inputs in Update
            UpdatePanInput();
            UpdateRotationInput();
            UpdateZoomInput();

            lastMousePosition = Input.mousePosition;
        }

        private void LateUpdate()
        {
            //handle movement and rotation in FixedUpdate
            Pan();
            Rotate();
            Zoom();
        }

        /// <summary>
        /// Updates the input required to pan the main camera.
        /// </summary>
        private void UpdatePanInput()
        {
            currPanDirection = Vector3.zero;

            //if the pan on screen edge is enabled and we either are ignoring UI elements on the edge of the screen or the player's mouse is not over one
            if(screenEdgePanning.enabled && (screenEdgePanning.ignoreUI || !EventSystem.current.IsPointerOverGameObject() ) ) 
            {
                //if the mouse is in either one of the 4 edges of the screen then move it accordinly  
                if (Input.mousePosition.x <= screenEdgePanning.size && Input.mousePosition.x >= 0.0f)
                    currPanDirection.x = -1.0f;
                else if (Input.mousePosition.x >= Screen.width - screenEdgePanning.size && Input.mousePosition.x <= Screen.width)
                    currPanDirection.x = 1.0f;

                if (Input.mousePosition.y <= screenEdgePanning.size && Input.mousePosition.y >= 0.0f)
                    currPanDirection.z = -1.0f;
                else if (Input.mousePosition.y >= Screen.height - screenEdgePanning.size && Input.mousePosition.y <= Screen.height)
                    currPanDirection.z = 1.0f;
            }

            //camera pan on axis input (overwrites the screen edge pan if it has been enabled and has effect on this frame)
            if(keyboardKeyPanning.enabled)
            {
                if (Input.GetKey(keyboardKeyPanning.up))
                    currPanDirection.z = 1.0f;
                if (Input.GetKey(keyboardKeyPanning.down))
                    currPanDirection.z = -1.0f;

                if (Input.GetKey(keyboardKeyPanning.right))
                    currPanDirection.x = 1.0f;
                if (Input.GetKey(keyboardKeyPanning.left))
                    currPanDirection.x = -1.0f;
            }

            //camera pan on axis input (overwrites the screen edge pan/input axis if it has been enabled and has effect on this frame)
            if (inputAxisPanning.enabled)
            {
                if (Mathf.Abs(Input.GetAxis(inputAxisPanning.horizontal)) > 0.25f)
                    currPanDirection.x = Input.GetAxis(inputAxisPanning.horizontal) > 0.0f ? 1.0f : -1.0f;
                if (Mathf.Abs(Input.GetAxis(inputAxisPanning.vertical)) > 0.25f)
                    currPanDirection.z = Input.GetAxis(inputAxisPanning.vertical) > 0.0f ? 1.0f : -1.0f;
            }
        }

        /// <summary>
        /// Moves the main camera.
        /// </summary>
        private void Pan ()
        {
            if (followTarget) //if there's a target transform to follow
            {
                LookAt(followTarget.position, smoothFollow, smoothFollowFactor);

                if (currPanDirection != Vector3.zero && stopFollowingOnMovement) //if the camera should stop following the target on movement and the player has panned the camera
                    followTarget = null; //stop following current target
            }
            else //normal camera movement
            {
                //smoothly update the last panning direction towards the current one
                lastPanDirection = Vector3.Lerp(lastPanDirection, currPanDirection, panningSpeed.smoothFactor);

                //move the camera
                mainCamera.transform.Translate(Quaternion.Euler(new Vector3(0f, mainCamera.transform.eulerAngles.y, 0f)) * lastPanDirection * panningSpeed.value * Time.deltaTime, Space.World);
            }

            //if there's a panning limit defined, clamp the camera's movement
            mainCamera.transform.position = ApplyPanLimit(mainCamera.transform.position);

            if(minimapCameraController) //if there's a minimap camera controller
            {
                //keep updating the minimap cursor
                Vector3 screenCenterWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0.0f));
                minimapCameraController.UpdateCursorPosition(
                    new Vector3(screenCenterWorldPos.x - currOffsetX, screenCenterWorldPos.y, screenCenterWorldPos.z - currOffsetZ));
            }
        }

        private Vector3 ApplyPanLimit(Vector3 position)
        {
            return panLimit.enabled
                ? new Vector3(
                    Mathf.Clamp(position.x, panLimit.minPosition.x, panLimit.maxPosition.x),
                    position.y,
                    Mathf.Clamp(position.z, panLimit.minPosition.y, panLimit.maxPosition.y))
                : position;
        }

        //make the camera look at a target position and return the final position while considering the offset values
        public void LookAt (Vector3 targetPosition, bool smooth, float smoothFactor = 0.1f)
        {
            targetPosition = ApplyPanLimit(new Vector3(targetPosition.x + currOffsetX, mainCamera.transform.position.y, targetPosition.z + currOffsetZ));

            mainCamera.transform.position = 
                smooth 
                ? Vector3.Lerp(mainCamera.transform.position, targetPosition, smoothFactor) 
                : targetPosition;
        }

        /// <summary>
        /// Updates the input required to rotate the main camera.
        /// </summary>
        private void UpdateRotationInput ()
        {
            currRotationValue = 0.0f;

            //if the keyboard keys rotation is enabled, check for the positive and negative rotation keys and update the current rotation value accordinly
            if(keyboardKeyRotation.enabled)
            {
                if (Input.GetKey(keyboardKeyRotation.positive))
                    currRotationValue = 1.0f;
                else if (Input.GetKey(keyboardKeyRotation.negative))
                    currRotationValue = -1.0f;
            }

            //if the mouse wheel rotation is enabled and the player is holding the mouse wheel button, update the rotation value accordinly
            if(mouseWheelRotation.enabled && Input.GetMouseButton(2))
                currRotationValue = (Input.mousePosition.x - lastMousePosition.x) * mouseWheelRotation.smoothFactor;

            //smoothly update the last rotation value towards the current one
            lastRotationValue = Mathf.Lerp(lastRotationValue, currRotationValue, rotationSpeed.smoothFactor);
        }

        /// <summary>
        /// Rotates the main camera.
        /// </summary>
        private void Rotate ()
        {
            //if the player is moving the camera and the camera's rotation must be fixed during movement...
            //... or if the camera is following a target, lock camera rotation to default value
            if((fixPanRotation && lastPanDirection.magnitude > allowedRotationPanSize) || followTarget)
            {
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, initialRotation, rotationSpeed.smoothFactor);
                return;
            }

            //rotate the main camera:
            Vector3 nextEulerAngles = mainCamera.transform.rotation.eulerAngles;
            nextEulerAngles.y += rotationSpeed.value * Time.deltaTime * lastRotationValue;

            //limit the y euler angle if that's enabled
            if (rotationLimit.enabled)
                nextEulerAngles.y = Mathf.Clamp(nextEulerAngles.y, rotationLimit.min, rotationLimit.max);

            mainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }

        /// <summary>
        /// Updates the input required to handle zooming for the main camera.
        /// </summary>
        private void UpdateZoomInput()
        {
            if (!buildingPlacementZoomEnabled && gameMgr.PlacementMgr.IsBuilding())
                return;

            //camera zoom on keyboard keys
            if(keyboardKeyZoom.enabled)
            {
                if (Input.GetKey(keyboardKeyZoom.inKey))
                    zoomValue -= Time.deltaTime;
                else if (Input.GetKey(keyboardKeyZoom.outKey))
                    zoomValue += Time.deltaTime;
            }

            //camera zoom when the player is moving the mouse scroll wheel
            if (mouseWheelZoom.enabled)
                zoomValue += Input.GetAxis("Mouse ScrollWheel") * mouseWheelZoom.sensitivity 
                    * (mouseWheelZoom.invert ? -1.0f : 1.0f) * Time.deltaTime;
        }

        /// <summary>
        /// Zooms the main camera in & out
        /// </summary>
        private void Zoom ()
        {
            zoomValue = Mathf.Clamp01(zoomValue);
            float targetHeight = Mathf.Lerp(minHeight, maxHeight, zoomValue);

            if (zoomFOV) //if we're using the field of view for zooming, no need to adjust the offset values
            {
                UpdateCameraFOV(Mathf.Lerp(mainCamera.fieldOfView, targetHeight, Time.deltaTime * zoomSpeed.value));
                return;
            }

            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, 
                new Vector3(
                    mainCamera.transform.position.x, targetHeight, mainCamera.transform.position.z), Time.deltaTime * zoomSpeed.value);

            //update the current camera offset since the height has ben modified
            currOffsetX = (offsetX * mainCamera.transform.position.y) / initialHeight;
            currOffsetZ = (offsetZ * mainCamera.transform.position.y) / initialHeight;
        }

        /// <summary>
        /// Updates the main camera (and its UI camera counterpart) field of view value.
        /// </summary>
        private void UpdateCameraFOV (float value)
        {
            mainCamera.fieldOfView = value;
            if (mainCameraUI) //if there's a UI camera assigned
                mainCameraUI.fieldOfView = value;
        }
    }
}
 