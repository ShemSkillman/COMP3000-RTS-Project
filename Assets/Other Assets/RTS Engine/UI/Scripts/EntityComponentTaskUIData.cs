using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.UI
{
    [CreateAssetMenu(fileName = "NewEntityComponentTaskUIData", menuName = "RTS Engine/Entity Component Task UI Data", order = 100)]
    public class EntityComponentTaskUIData : ScriptableObject, IAssetFile
    {
        [SerializeField]
        private EntityComponentTaskUI data = new EntityComponentTaskUI
        {
            enabled = true,
            code = "unique_code",
            description = "tooltip text",

            hideTooltipOnClick = true,
            tooltipEnabled = true,
        };

        public EntityComponentTaskUI Data { get { return data; } }

        /// <summary>
        /// Get the unique code of the EntityComponentTaskUI instance.
        /// </summary>
        public string Key { get { return data.code; } }
    }
}
