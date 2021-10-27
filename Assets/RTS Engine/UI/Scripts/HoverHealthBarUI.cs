using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    [System.Serializable]
    public class HoverHealthBarUI
    {
        [SerializeField]
        private bool enabled = true; //if set to true then whenever the mouse hovers over a unit/building, a health bar will appear on top of them
        [SerializeField]
        private bool playerFactionOnly = true; //only show the hover health bar for friendly units/buildings or for all factions

        [SerializeField]
        private Canvas canvas = null; //the canvas that holds the health bar sprites
        [SerializeField]
        private ProgressBarUI healthBar = new ProgressBarUI(); //the actual health progress bar, both empty and full bars are children of the canvas
        
        private bool isActive = false; //is the hover health bar active or not?
        private FactionEntity currSource; //the health bar will be showing this selection entity's health when it's enabled.
        
        //initilaize the task panel UI component
        UIManager manager;

        public void Init(UIManager manager)
        {
            this.manager = manager;

            isActive = false;
            currSource = null;

            healthBar.Init(); //initialise the health bar

            //listen to the events that affect the hover health bar
            CustomEvents.FactionEntityDead += Hide;

            CustomEvents.FactionEntityMouseEnter += Enable;
            CustomEvents.FactionEntityMouseExit += Hide;

            CustomEvents.BuildingInstanceUpgraded += Hide;
            CustomEvents.UnitInstanceUpgraded += Hide;
        }

        //called to disable this component
        public void Disable ()
        {
            //stop listening to the custom events:
            CustomEvents.FactionEntityDead -= Hide;

            CustomEvents.FactionEntityMouseEnter -= Enable;
            CustomEvents.FactionEntityMouseExit -= Hide;

            CustomEvents.BuildingInstanceUpgraded -= Hide;
            CustomEvents.UnitInstanceUpgraded -= Hide;
        }

        //hide the hover health bar:
        public void Hide (FactionEntity source)
        {
            if (currSource != null && currSource != source) //if there's a current active source and it's not the input one attempting to disable this
                return; //do not proceed

            //disable the hover health bar:
            canvas.gameObject.SetActive(false);
            //no transform parent anymore
            canvas.transform.SetParent(null, true);

            isActive = false;
        }

        //hover health bar:
        public void Enable(FactionEntity source)
        {
            //if disabled, the input source is invalid or the hover health bar is already active or if it's only enabled for player faction and the source doesn't belong to it:w
            if (enabled == false || source == null || isActive == true || (playerFactionOnly && source.FactionID != GameManager.PlayerFactionID))
                return; //do not proceed

            currSource = source; //set the new source
             
            //enable the hover health bar:
            canvas.gameObject.SetActive(true);
            //make the canvas a child object of the source obj:
            canvas.transform.SetParent(source.transform, true);

            isActive = true;
            //set the new health bar canvas position, the height is specified in either the Unit or Building component.
            canvas.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0.0f, source.EntityHealthComp.GetHoverHealthBarY(), 0.0f);

            Update(source); //update the health bar
        }

        //the method that updates the health bar of the unit/building that the player has their mouse on.
        public void Update(FactionEntity source)
        {
            if (enabled == false || source != currSource) //if the hover health bar is disabled
                return;

            healthBar.Update(source.EntityHealthComp.CurrHealth / (float)source.EntityHealthComp.MaxHealth);
        }
    }
}
