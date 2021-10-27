using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

/* Attack Warning script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Placed on the minimap on the area where the player's units/buildings are under attack.
    /// </summary>
    public class AttackWarning : MonoBehaviour
    {
        public Vector3 targetPosition { private set; get; } //the source position of the enemy contact is registerd here

        private Image imageUI; //image component of this UI object

        //manager component:
        AttackWarningManager manager;

        /// <summary>
        /// Initializes the AttackWarning component (called from the AttackWarningManager)
        /// </summary>
        public void Init (AttackWarningManager manager, Vector3 targetPosition)
        {
            this.manager = manager;
            this.targetPosition = targetPosition;

            imageUI = gameObject.GetComponent<Image>();
            Assert.IsNotNull(imageUI, "[AttackWarning] The object doesn't have an 'Image' component attached!");

            InvokeRepeating("Blink", 0.0f, 0.3f); //effect of the warning image
        }

        /// <summary>
        /// Updates the attack warning's image aplha color to make the blinking effect
        /// </summary>
        private void Blink()
        {
            imageUI.color = new Color(imageUI.color.r, imageUI.color.g, imageUI.color.b, (imageUI.color.a == 0.0f) ? 0.5f : 0.0f);
        }

        /// <summary>
        /// Disables the attack warning and hides to be used again.
        /// </summary>
        public void Disable ()
        {
            CancelInvoke("Blink"); //stop the blinking effect
            manager.OnAttackWarningDisabled(this); 
        }
    }

}
