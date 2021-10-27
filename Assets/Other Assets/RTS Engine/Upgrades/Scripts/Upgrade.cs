using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Upgrade script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(FactionEntity))]
    public class Upgrade : MonoBehaviour
    {
        public FactionEntity Source {
            private set { }
            get { return gameObject.GetComponent<FactionEntity>(); }
        } //the source instance (unit/building) that will be upgraded.

        [SerializeField]
        private FactionEntity[] target = new FactionEntity[0]; //the target prefabs (units/buildings) that this entity can upgrade to. The choice of which target to ugprade is determined in the task launcher's settings.
        public int GetTargetCount () { return target.Length; }
        public FactionEntity GetTarget (int index) { return target[index]; }

        [SerializeField]
        private EffectObj upgradeEffect = null; //the upgrade effect object that is spawned when the building upgrades (at the buildings pos).
        public EffectObj GetUpgradeEffect() { return upgradeEffect; }

        [SerializeField]
        private bool upgradeSpawnedInstances = true; //upgrade all spawned instances of the source
        public bool CanUpgradeSpawnedInstances () { return upgradeSpawnedInstances; }

        //a list of unit/building upgrades that will be triggered when this upgrade is launched.
        [SerializeField]
        private Upgrade[] triggerUpgrades = new Upgrade[0];
        public IEnumerable<Upgrade> GetTriggerUpgrades () { return triggerUpgrades; }

        [System.Serializable]
        //the following attributes will replace the attributes in the tasks where the unit to upgrade can be created:
        public struct NewTaskInfo
        {
            public string description;
            public Sprite icon;
            public float reloadTime;
            public ResourceInput[] newResources;
        }
        [SerializeField]
        private NewTaskInfo newTaskInfo = new NewTaskInfo();
        public NewTaskInfo GetNewTaskInfo() { return newTaskInfo; }
    }
}
