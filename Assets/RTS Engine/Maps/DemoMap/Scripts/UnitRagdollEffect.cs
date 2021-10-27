using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

/* Unit Rag Doll Effect created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngineDemo
{
    public class UnitRagdollEffect : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody[] rigidbodies = new Rigidbody[0]; //a list of the rigidbodies of the unit model's parts
                                                            //by default, the rigidbodies should have isKinematic set to true and useGravity set to false

        [SerializeField]
        private FloatRange forceIntensityRange = new FloatRange(-2.5f, 2.5f);

        private void Awake()
        {
            foreach (Rigidbody r in rigidbodies) //enable kinematic mode, disable gravity and enable trigger
            {
                r.isKinematic = true;
                r.useGravity = false;
                r.gameObject.GetComponent<Collider>().isTrigger = true;
                r.gameObject.GetComponent<Collider>().enabled = false;
            }
        }

        //trigger the ragdoll effect when the unit is dead (used in the demo scene).
        public void Trigger()
        {
            if (GetComponent<Collider>())
                GetComponent<Collider>().enabled = false; //disable the unit's boundary collider

            if (GetComponent<Animator>())
                GetComponent<Animator>().enabled = false; //disable the animator

            foreach (Rigidbody r in rigidbodies) //disable kinematic mode and enable gravity
            {
                r.isKinematic = false;
                r.useGravity = true;
                r.gameObject.GetComponent<Collider>().enabled = true;
                r.gameObject.GetComponent<Collider>().isTrigger = false;

                //add force to the model's parts
                r.AddForce(new Vector3(forceIntensityRange.getRandomValue(), forceIntensityRange.getRandomValue(), forceIntensityRange.getRandomValue()), ForceMode.Impulse);
            }
        }
    }
}
