using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using RTSEngine;
using System.Linq;

namespace RTSCamera
{
    public class MinimapCameraController : MonoBehaviour
    {
        [SerializeField, Header("General"), Tooltip("The camera used to render the minimap.")]
        private Camera minimapCamera = null; //drag and drop the minimap camera here
        private CameraController mainCameraController = null; //drag and drop the object holding the main camera controller here

        //move the main camera when the player clicks on the minimap?
        [SerializeField, Tooltip("Can the player move the main camera by clicking the minimap?")]
        private bool movementEnabled = true;
        [SerializeField, Tooltip("Can the player drag their mouse on the minimap to move the main camera?")]
        private bool dragMovementEnabled = true; //enable moving the main camera when the player is dragging their mouse (and clicking) on the minimap
        [SerializeField, Tooltip("Can the player move selected units by right-clicking on the minimap?")]
        private bool selectedUnitsMovementEnabled = true; //enable moving selected units when the player right clicks on a destination in the minimap.

        [SerializeField, Header("UI"), Tooltip("The UI image used to represent the minimap cursor.")]
        private RectTransform cursor = null;
        [SerializeField, Tooltip("The canvas where the minimap UI is.")]
        private RectTransform canvas = null;

        //the layer mask that defines the terrain, used for raycasting purposes
        private LayerMask terrainLayerMask = new LayerMask();

        //manager components
        GameManager gameMgr;

        /// <summary>
        /// initializes the minimap camera controller component.
        /// </summary>
        public void Init (GameManager gameMgr, CameraController mainCameraController, bool updateRotation, float yEulerAngle, LayerMask terrainLayerMask)
        {
            this.gameMgr = gameMgr;
            this.mainCameraController = mainCameraController;
            this.terrainLayerMask = terrainLayerMask;

            Assert.IsNotNull(minimapCamera, "[RTS Minimap Camera Controller] The field 'Minimap Camera' hasn't been assigned!");
            Assert.IsTrue(cursor == null || canvas != null, "[RTS Minimap Camera Controller] Either both the 'Cursor' and 'Canvas' are assigned or both aren't!");

            if (updateRotation) //set the rotation of the minimap camera:
                transform.rotation = Quaternion.Euler(new Vector3(90.0f, yEulerAngle, 0.0f));
        }
        
        private void Update()
        {
            bool leftMouseClick = Input.GetMouseButtonDown(0) || (dragMovementEnabled && Input.GetMouseButton(0));
            bool rightMouseClick = selectedUnitsMovementEnabled && Input.GetMouseButtonDown(1);

            if(movementEnabled //if the camera minimap click movement is enabled
                && (leftMouseClick || rightMouseClick) //if either of the mouse buttons are clicked
                && minimapCamera.rect.Contains(mainCameraController.ScreenToViewportPoint(Input.mousePosition)) ) //and the player's mouse is inside the minimap rect
            {
                OnClick(leftMouseClick); //trigger a minimap click
            }
        }

        /// <summary>
        /// Determines whether the mouse is over the minimap and provides the hitpoint
        /// </summary>
        public bool IsMouseOverMinimap (out RaycastHit hit)
        {
            hit = new RaycastHit();

            //draw a ray using the current mouse position towards the minimap camera and see if it hits the terrain
            if (minimapCamera.rect.Contains(mainCameraController.ScreenToViewportPoint(Input.mousePosition))  
                && Physics.Raycast(minimapCamera.ScreenPointToRay(Input.mousePosition), out hit, terrainLayerMask))
                return true;

            return false;
        }

        //called when the player clicks on the minimap
        private void OnClick (bool leftMouseButton)
        {
            if(IsMouseOverMinimap(out RaycastHit hit)) //only if the mouse is over the minimap
            {
                if (leftMouseButton) //if this was a left mouse button click, make the camera look at 
                    OnLeftMouseClick(hit.point);
                else
                    OnRightMouseClick(hit.point);
            }
        }

        //called when the player clicks on a terrain object on the minimap using the left mouse button
        protected virtual void OnLeftMouseClick (Vector3 hitPoint)
        {
            if (gameMgr.SelectionMgr.Box.IsActive) //if the selection box is active, do not move the main camera
                return;

            mainCameraController.SetFollowTarget(null); //stop following the camera's target if there was one
            mainCameraController.LookAt(hitPoint, true, 0.8f); //make the main camera look at the minimap hit position
        }

        //called when the player clicks on a terrain object on the minimap using the right mouse button
        protected virtual void OnRightMouseClick (Vector3 hitPoint)
        {
            List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, true, true).Cast<Unit>().ToList(); //get selected units from player faction

            if(selectedUnits.Count > 0)
                //move the selected units to the clicked position in the minimap
                gameMgr.MvtMgr.Move(selectedUnits, hitPoint, 0.0f, null, InputMode.movement, true);
        }

        //used to update the minimap's cursor position
		public void UpdateCursorPosition (Vector3 newPosition)
		{
            if (!cursor) //no minimap cursor is used
                return;

            //convert the input position to the minimap camera's screen space
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, minimapCamera.WorldToScreenPoint(newPosition), minimapCamera, out Vector2 localPoint))
                cursor.localPosition = new Vector3(localPoint.x, localPoint.y, cursor.localPosition.z);
		}
    }
}
