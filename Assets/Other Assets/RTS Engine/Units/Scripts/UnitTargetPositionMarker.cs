using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* UnitTargetPositionMarker script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Allows a unit to reserve a certain area in the map as its target position.
    /// </summary>
    public class UnitTargetPositionMarker
    {
        public SearchCell searchCell = null;

        //position reserved by the marker
        private Vector3 position;
        /// <summary>
        /// Gets the current position reserved by the marker.
        /// </summary>
        public Vector3 Position { get { return position; } }

        //the radius of the reserved area.
        private float radius;
        /// <summary>
        /// Gets the radius of the marker's reverse area.
        /// </summary>
        public float Radius { get { return radius; } }

        //is the marker currently enabled?
        private bool enabled = false;
        /// <summary>
        /// Gets whether the marker is enabled or not, a marker is only enabled when a unit uses it to reserve its target position.
        /// </summary>
        public bool Enabled { get { return enabled; } }

        //Layer of the marker, currently there are only two layers: 0 -> ground terrain and 1 -> air terrain
        private int layer = -1;
        /// <summary>
        /// Gets the layer ID of the marker.
        /// </summary>
        public int Layer { get { return layer; } }

        //other components:
        private GridSearchHandler gridSearchHandler;

        /// <summary>
        /// Initialzes the UnitTargetPositionMarker instance, called by the UnitMovement.
        /// </summary>
        /// <param name="radius">Radius of the area to reserve by the marker, this is usally the unit's radius.</param>
        /// <param name="layer">Layer of the marker for filtering purposes later when searching for markers in a certain area.</param>
        /// <param name="gridSearchHandler">The active instance of the GridSearchHandler component, this allows the marker to locate a search cell to belong to.</param>
        public UnitTargetPositionMarker (float radius, int layer, GridSearchHandler gridSearchHandler)
        {
            this.radius = radius;
            this.layer = layer;
            this.gridSearchHandler = gridSearchHandler;

            enabled = false; //by default, the marker is disabled.
            searchCell = null;
        }

        /// <summary>
        /// Enables or disables the marker.
        /// </summary>
        /// <param name="enable">True to enable and false to disable the marker.</param>
        /// <param name="position">New Vector3 position for the marker in case it is enabled.</param>
        public void Toggle(bool enable, Vector3 position = default)
        {
            enabled = enable;

            if (enable) //in case the marker is to enabled
            {
                this.position = position;

                if (gridSearchHandler.TryGetSearchCell(position, out SearchCell nextCell) == ErrorMessage.none
                    && searchCell != nextCell) //assign the new search cell that the marker now belongs to.
                {
                    searchCell?.Remove(this);

                    searchCell = nextCell;
                    searchCell.Add(this);
                }
            }
        }
    }
}
