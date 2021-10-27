using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public class UnitComponentTaskManager
    {
        public TaskTypes AwaitingTaskType { private set; get; }

        [SerializeField]
        private bool changeCursorTexture = false; //change the mouse texture when having an awaiting task type?
        [SerializeField]
        private Sprite defaultCursorSprite = null; //leave unassigned if you want to use the default cursor
        [SerializeField]
        private Vector2 customCursorHotspot = Vector2.zero; //if your cursors use a different hotspot, assign it here

        public void Init()
        {
            AwaitingTaskType = TaskTypes.none;

            if (defaultCursorSprite != null) //if there's a default texture for the cursor
                Cursor.SetCursor(defaultCursorSprite.texture, customCursorHotspot, CursorMode.Auto);
        }

        //set the awaiting task unit component type
        public void SetAwaitingTaskType(TaskTypes type, Sprite icon)
        {
            AwaitingTaskType = type; //set the new task type
            if (changeCursorTexture == true && icon != null)
            { //if it is allowed to change the mouse texture
              //change it:
                Cursor.SetCursor(icon.texture, customCursorHotspot, CursorMode.Auto);
            }
        }

        //reset the awaiting task type:
        public void ResetAwaitingTaskType()
        {
            AwaitingTaskType = TaskTypes.none;

            if (defaultCursorSprite == null)
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            else
                Cursor.SetCursor(defaultCursorSprite?.texture, customCursorHotspot, CursorMode.Auto);
        }
    }
}
