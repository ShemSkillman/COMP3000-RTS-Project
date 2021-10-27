using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;

/* Effect Object Pooling script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class EffectObjPool : MonoBehaviour
    {
        //because instantiating and destroying objects is heavy on memory and since we need to show/hide effect objects multiple times in a game...
        //...this component will handle pooling those effect objects.

        private Dictionary<string, Queue<EffectObj>> effectObjs = new Dictionary<string, Queue<EffectObj>>(); //this holds all inactive effect objects of different types.

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        //this method searches for a hidden effect object with a certain code so that it can be used again.
        public EffectObj GetFreeEffectObj(EffectObj prefab)
        {
            Assert.IsTrue(prefab != null, "[Effect Object Pool] invalid effect object prefab.");

            Queue<EffectObj> currentQueue;
            if(effectObjs.TryGetValue(prefab.GetCode(), out currentQueue) == false) //if the queue for this effect object type is not found
            {
                currentQueue = new Queue<EffectObj>();
                effectObjs.Add(prefab.GetCode(), currentQueue); //add it
            }

            if (currentQueue.Count == 0) //if the queue is empty then we need to create a new effect object of this types
            {
                //create new effect object, init it and add it to the queue
                EffectObj newEffect = Instantiate(prefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<EffectObj>();
                newEffect.Init(gameMgr);
                currentQueue.Enqueue(newEffect);
            }
            return currentQueue.Dequeue(); //return the first inactive effect object in this queue
        }

        public void AddFreeEffectObj (EffectObj instance)
        {
            Queue<EffectObj> currentQueue = new Queue<EffectObj>();
            if (effectObjs.TryGetValue(instance.GetCode(), out currentQueue) == false) //if the queue for this effect object type is not found
            {
                effectObjs.Add(instance.GetCode(), currentQueue); //add it
            }

            currentQueue.Enqueue(instance); //add the effect object to the right queue
        }

        //a method that spawns an effect object considering a couple of options
        public EffectObj SpawnEffectObj(EffectObj prefab, Vector3 spawnPosition, Quaternion spawnRotation = default, Transform parent = null, bool enableLifeTime = true, bool autoLifeTime = true, float lifeTime = 0.0f, bool UI = false)
        {
            if (prefab == null)
                return null;

            //get the attack effect (either create it or get one tht is inactive):
            EffectObj newEffect = GetFreeEffectObj(prefab);

            //make the object child of the assigned parent transform
            newEffect.transform.SetParent(parent, true);

            if (UI)
            {
                newEffect.GetComponent<RectTransform>().localPosition = spawnPosition;
                newEffect.GetComponent<RectTransform>().localRotation = spawnRotation;
            }
            else
            {
                //set the effect's position and rotation
                newEffect.transform.position = spawnPosition;
                newEffect.transform.rotation = spawnRotation;
            }

            newEffect.ReloadTimer(enableLifeTime, autoLifeTime, lifeTime); //reload lifetime

            newEffect.Enable();

            return newEffect; 
        }
    }
}