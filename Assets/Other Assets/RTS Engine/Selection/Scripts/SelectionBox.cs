using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RTSEngine
{
    [System.Serializable]
    public class SelectionBox
    {
        [SerializeField]
        private RectTransform canvasTransform = null; //the transform component of the separate canvas that holds the selection box image
        [SerializeField]
        private RectTransform image = null; //the box's image goes here

        [SerializeField]
        private float minSize = 10.0f; //the minimum allowed size of the selection box so it is drawn and can select units

        private bool isDrawing = false; //when the player is drawing the selection box, this is set to true
        public bool IsActive { private set; get; } //active only if it's drawing above the min size
        private Vector3 initialMousePosition; //initial mouse position recorded when the player starts drawing the selection box
        private Vector3 finalMousePosition; //final mouse position recorded when the player stops drawing the selection box

        //manager components
        SelectionManager manager;
        GameManager gameMgr;

        public void Init(SelectionManager manager, GameManager gameMgr)
        {
            this.manager = manager; //assign the selection manager component
            this.gameMgr = gameMgr;

            if (minSize < 0) //min size must be at least 0.0f
                minSize = 0.0f;

            Disable(); //disable the component by default
        }

        public void Update ()
        {
            if (canvasTransform == null || image == null) //if the canvas transform or the image haven't been assigned, no selection box
                return;

            if (Input.GetMouseButton(0)) //if the player is holding the left mouse button -> potentially drawing a selection box
                OnDrawingProgress();

            if (Input.GetMouseButtonUp(0)) //if the player releases the left mouse button and was drawing
            {
                OnDrawingComplete();
                Disable();
            }
        }

        //called when the player is drawing a selection box
        private void OnDrawingProgress ()
        {
            if (isDrawing == false) //just started drawing?
            {
                if (gameMgr.CamMgr.MinimapCameraController.IsMouseOverMinimap(out RaycastHit hit)) //do not start drawing the box if the mouse is currently over the minimap
                    return;

                initialMousePosition = finalMousePosition = Input.mousePosition; //set the initial mouse position
                isDrawing = true;
            }
            else
                finalMousePosition = Input.mousePosition; //if not, keep updating the final mouse position

            if (Vector3.Distance(finalMousePosition, initialMousePosition) < minSize) //as long as the box's size is still under the minimum
                return; //do not display it

            if (IsActive == false) //if the selection box image is disabled, enable it
            {
                image.gameObject.SetActive(true);
                IsActive = true;
            }

            //update the size and position of the selection box image
            image.sizeDelta = new Vector2(Mathf.Abs(finalMousePosition.x - initialMousePosition.x), Mathf.Abs(finalMousePosition.y - initialMousePosition.y));
            //center the selection box position between initial and final mouse positions and offset by the canvas position
            image.localPosition = (initialMousePosition + finalMousePosition)/2.0f - canvasTransform.localPosition;
        }

        //called when the player is done drawing a selection box
        private void OnDrawingComplete ()
        {
            if (!IsActive || Vector3.Distance(finalMousePosition, initialMousePosition) < minSize) //if the box's size is too small
                return; //do not continue

            if (!manager.MultipleSelectionKeyDown) //if the player is not holding down the multiple selection key down
                manager.Selected.RemoveAll(); //deselect units

            //get the lower left and upper right corners coordinates
            Vector2 lowerLeftCorner = new Vector2(Mathf.Min(finalMousePosition.x, initialMousePosition.x), Mathf.Min(finalMousePosition.y, initialMousePosition.y));
            Vector2 upperRightCorner = new Vector2(Mathf.Max(finalMousePosition.x, initialMousePosition.x), Mathf.Max(finalMousePosition.y, initialMousePosition.y));

            foreach(Unit unit in GameManager.PlayerFactionMgr.GetUnits()) //go through the local player's units
            {
                Vector3 unitScreenPosition = gameMgr.CamMgr.MainCamera.WorldToScreenPoint(unit.GetSelection().transform.position); //get the unit's position on screen
                if(unit.gameObject.activeInHierarchy && //make sure the unit is active
                    unitScreenPosition.x >= lowerLeftCorner.x && unitScreenPosition.x <= upperRightCorner.x //check if the unit's screen position is in the selection box
                    && unitScreenPosition.y >= lowerLeftCorner.y && unitScreenPosition.y <= upperRightCorner.y)
                {
                    manager.Selected.Add(unit, SelectionTypes.multiple); 
                }
            }
        }

        //disable the selection box using this method
        public void Disable ()
        {
            isDrawing = false; //no longer drawing
            image.gameObject.SetActive(false); //hide the selection box
            IsActive = false; //no longer active
        }
    }
}
