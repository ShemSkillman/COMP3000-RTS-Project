using RTSEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColdAlliances.AI
{
    public class AIEconomyManager : MonoBehaviour
    {
        Dictionary<string, List<CollectionTask>> collectionTasksDic = new Dictionary<string, List<CollectionTask>>();

        GameManager gameMgr;
        FactionManager factionMgr;

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;
        }

        private void OnEnable()
        {
            CustomEvents.UnitHealthUpdated += OnUnitHealthUpdated;
        }

        private void OnDisable()
        {
            CustomEvents.UnitHealthUpdated += OnUnitHealthUpdated;
        }

        private List<Unit> hurtVillagers = new List<Unit>();
        public List<Unit> HurtVillagers { get { return hurtVillagers; } }

        private void OnUnitHealthUpdated(Unit unit, float healthPoints, FactionEntity source)
        {
            if (unit.FactionID == factionMgr.FactionID && unit.GetCode() == "villager")
            {
                if (!hurtVillagers.Contains(unit))
                {
                    hurtVillagers.Add(unit);
                }
            }
        }

        bool pollRequired = false;
        private void Update()
        {
            pollRequired = true;
        }

        private void Poll()
        {
            if (!pollRequired)
            {
                return;
            }

            foreach (List<CollectionTask> collectionTasks in collectionTasksDic.Values)
            {
                collectionTasks.RemoveAll(CollectionTask.IsCollectionTaskInvalid);
            }
        }

        public int GetResourceCollectorCount(Resource resource)
        {
            Poll();

            List<Unit> collectors = factionMgr.GetVillagersCollectingResource(resource.GetResourceType().Key);

            int collectionTaskCount = 0;
            if (collectionTasksDic.ContainsKey(resource.GetResourceType().Key))
            {
                collectionTaskCount = collectionTasksDic[resource.GetResourceType().Key].Count;
            }

            return collectors.Count + collectionTaskCount;
        }

        public bool AssignVillagerToResource(Unit villager, Resource resource)
        {
            BasicTargetPicker targetPicker = new BasicTargetPicker(resource.GetCode());

            if (gameMgr.GridSearch.Search<Entity>(villager.transform.position,
                                        1000f,
                                        true,
                                        targetPicker.IsValidTarget,
                                        out Entity potentialTarget) == ErrorMessage.none)
            {
                Resource closestResource = potentialTarget as Resource;

                villager.CollectorComp.SetTarget(closestResource);

                CollectionTask task = new CollectionTask(villager.CollectorComp, resource.GetResourceType());

                if (!collectionTasksDic.ContainsKey(resource.GetResourceType().Key))
                {
                    collectionTasksDic[resource.GetResourceType().Key] = new List<CollectionTask>();
                }

                collectionTasksDic[resource.GetResourceType().Key].Add(task);

                return true;
            }
            else
            {
                return false;
            }
        }

        class CollectionTask
        {
            public ResourceCollector Collector { get; set; }
            public ResourceTypeInfo ToCollect { get; set; }

            public CollectionTask(ResourceCollector collector, ResourceTypeInfo toCollect)
            {
                Collector = collector;
                ToCollect = toCollect;
            }

            public static bool IsCollectionTaskInvalid(CollectionTask task)
            {
                return task.Collector.GetTarget() == null ||
                    task.Collector.GetTarget().GetResourceType().Key != task.ToCollect.Key ||
                    task.Collector.InProgress;
            }
        }
    }    
}