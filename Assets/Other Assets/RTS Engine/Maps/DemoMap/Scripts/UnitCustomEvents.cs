using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

/* Unit Custom Events created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngineDemo
{
    public class UnitCustomEvents : MonoBehaviour
    {
        //This component uses the custom delegate events to monitor unit related custom events and modify the unit's behavior in the demo scene

        //listen to custom events
        private void OnEnable()
        {
            CustomEvents.UnitDead += OnUnitDead;
        }

        private void OnDisable()
        {
            CustomEvents.UnitDead -= OnUnitDead;
        }

        //called each time a unit is dead
        private void OnUnitDead (Unit unit)
        {
            if(unit.gameObject.GetComponent<UnitRagdollEffect>())
                unit.gameObject.GetComponent<UnitRagdollEffect>().Trigger(); //trigger the ragdoll effect

            unit.GetSelection().ToggleSelection(false, false); //disable the unit's selection since it's dead.
        }
    }
}