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

        ArmyGroup reserves, defenders, attackers;

        private float attackCountDown;

        public float GetRandomAttackCountDown()
        {
            return attackCountDown + (((Random.value * 2) - 1) * 30f);
        }

        public float AttackForcePercentage { get; private set; }

        public ArmyGroup GetAttackers()
        {
            attackers?.Validate();
            return attackers;
        }

        public ArmyGroup GetReserves()
        {
            reserves?.Validate();
            return reserves;
        }

        public ArmyGroup GetDefenders()
        {
            defenders?.Validate();
            return defenders;
        }

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;

            attackers = new ArmyGroup();
            defenders = new ArmyGroup();
            reserves = new ArmyGroup();

            AttackForcePercentage = Random.value;
            attackCountDown = Random.Range(1, 5) * 60;
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
                reserves.AttackUnits.Add(unit);
            }
        }        
    }

    public class ArmyGroup
    {
        public List<Unit> AttackUnits { get; set; }

        public ArmyGroup()
        {
            AttackUnits = new List<Unit>();
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

        public int ArmyPop()
        {
            int pop = 0;

            foreach (Unit unit in AttackUnits)
            {
                pop += unit.GetPopulationSlots();
            }

            return pop;
        }

        public void Add(ArmyGroup other)
        {
            AttackUnits.AddRange(other.AttackUnits);
            other.AttackUnits.Clear();
        }

        public void Add(ArmyGroup other, float percentage)
        {
            int targetPop = Mathf.RoundToInt(other.ArmyPop() * percentage);
            int currentPop = 0;

            while (currentPop < targetPop && other.AttackUnits.Count > 0)
            {
                currentPop += other.AttackUnits[0].GetPopulationSlots();
                this.AttackUnits.Add(other.AttackUnits[0]);
                other.AttackUnits.RemoveAt(0);
            }
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