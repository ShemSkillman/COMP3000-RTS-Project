using RTSEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColdAlliances.AI
{
    public class AICombatManager : MonoBehaviour
    {
        GameManager gameMgr;
        FactionManager factionMgr;

        ArmyGroup armyGroup;
        public ArmyGroup GetArmyGroup()
        {
            armyGroup?.Validate();
            return armyGroup;
        }

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;
        }

        private void OnEnable()
        {
            CustomEvents.UnitCreated += UnitCreated;
        }

        private void OnDisable()
        {
            CustomEvents.UnitCreated -= UnitCreated;
        }

        private void UnitCreated(Unit unit)
        {
            if (unit.IsFree() || unit.FactionID != factionMgr.FactionID)
            {
                return;
            }

            if (unit.AttackComp != null)
            {
                if (armyGroup == null)
                {
                    armyGroup = new ArmyGroup(unit);
                }
                else
                {
                    armyGroup.AttackUnits.Add(unit);
                }
            }
        }        
    }

    public class ArmyGroup
    {
        public List<Unit> AttackUnits { get; set; }

        public ArmyGroup(Unit unit)
        {
            AttackUnits = new List<Unit>();
            AttackUnits.Add(unit);
        }

        public void Validate()
        {
            AttackUnits.RemoveAll(IsUnitDead);
        }

        private static bool IsUnitDead(Unit unit)
        {
            return unit == null || unit.HealthComp.IsDead();
        }

        public bool IsIdle()
        {
            foreach (Unit unit in AttackUnits)
            {
                if (unit.AttackComp.Target == null)
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetLocation()
        {
            Vector3 ret = Vector3.zero;
            foreach (Unit unit in AttackUnits)
            {
                ret += unit.transform.position;
            }

            return ret / AttackUnits.Count;
        }
    }
}