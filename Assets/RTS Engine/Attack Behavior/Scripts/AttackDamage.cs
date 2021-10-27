using UnityEngine;

using RTSEngine.EntityComponent;

/* Attack Damage script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackDamage
    {
        //attack source:
        AttackEntity source;

        //damage related attributes:
        [SerializeField]
        private bool canDealDamage = true; //If set to false, then no damage will be dealt to the target but all custom events will still be triggered.

        private int currDamage; //the damage that will be applied to the current target unit is saved here.

        [SerializeField]
        private int unitDamage = 10; //damage that the attack type applies to units
        [SerializeField]
        private int buildingDamage = 10; //damage to apply to buildings when the attack is triggered

        [System.Serializable]
        public struct CustomDamage //some faction entities can receive custom damage instead of the above two fields
        {
            public CodeCategoryField code;
            public int damage; //damage points for the faction entity with the above code
        }
        [SerializeField]
        private CustomDamage[] customDamages = new CustomDamage[0];

        //area damage?
        [SerializeField]
        private bool areaAttackEnabled = false; //when enabled, the attack type will trigger an area damage.
        [System.Serializable]
        public class AttackRange //defines an attack range for an area attack
        {
            [SerializeField]
            private float range = 10.0f; //range of the attack.
            public float GetRange() { return range; }
            [SerializeField]
            private int unitDamage = 10; //damage that will be applied to units inside the above range
            public int GetUnitDamage () { return unitDamage; }
            [SerializeField]
            private int buildingDamage = 10; //damage that will be applied to buildings inside the above range
            public int GetBuildingDamage () { return buildingDamage; }
            [SerializeField]
            private CustomDamage[] customDamages = new CustomDamage[0];

            /// <summary>
            /// Gets the custom defined damage for the attack range in case the code and/or category of the attack target is defined.
            /// </summary>
            /// <param name="code">Code of the attack range target.</param>
            /// <param name="category">Category of the attack range target.</param>
            /// <param name="damage">The output damage if a custom damage is defined for the attack target.</param>
            /// <returns>True if a custom damage value is defined for the attack target, otherwise false.</returns>
            public bool GetCustomDamage (string code, string category, out int damage)
            {
                damage = 0;

                foreach(CustomDamage cd in customDamages) //see if the target's code or category is in the custom damages list
                    if(cd.code.Contains(code, category)) //if the target is found then pick the custom damage
                    {
                        damage = cd.damage;
                        return true;
                    }

                return false;
            }
        }
        [SerializeField]
        private AttackRange[] attackRanges = new AttackRange[0]; //add attack ranges in this array in increasing size of the range field.

        [SerializeField]
        private bool dotEnabled = false; //is damage over time enabled?
        [SerializeField]
        private DoTAttributes dotAttributes = new DoTAttributes(); //the DoT attributes that can be modified from the inspector 

        //effect related attributes:
        [SerializeField]
        private EffectObj effect = null; //effect object that appears on the target's object when the attack is triggered
        [SerializeField]
        private float effectLifeTime = 2.0f; //the above effect object uses a life time that is assigned in this field

        GameManager gameMgr;

        //called to init the attack damage settings
        public void Init (GameManager gameMgr, AttackEntity attackEntity)
        {
            this.gameMgr = gameMgr;
            source = attackEntity;
        }

        //a method that sets the damage to be applied to a target
        public void UpdateCurrentDamage (FactionEntity target)
        {
            currDamage = GetDamage(target);
        }

        //a method that returns the damage that is supposed to be dealt to a target.
        public int GetDamage (FactionEntity target)
        {
            foreach(CustomDamage cd in customDamages) //see if the target's code is in the custom damages list
                if(cd.code.Contains(target.GetCode(), target.GetCategory())) //if the target is found then pick the custom damage
                    return cd.damage;

            //no custom damage? pick either the damage for units or for buildings
            return target.Type == EntityTypes.unit ? unitDamage : buildingDamage;
        }

        //a method that triggers the attack
        public void TriggerAttack (FactionEntity target, Vector3 targetPosition, int sourceFactionID = -1, bool updateDamage = false)
        {
            if (areaAttackEnabled == true) //if this is an area attack:
                LaunchAreaDamage(target ? target.transform.position : targetPosition);
            else if (target != null) //direct attack, the target must be assigned
            {
                if (updateDamage == true) //update the current damage value, because of a new target?
                    currDamage = GetDamage(target);
                DealDamage(target);
            }
        }

        //a method that deals damage to a target:
        private void DealDamage (FactionEntity target)
        {
            if (canDealDamage == false || target == null) //can't deal damage then stop here
                return;

            if (dotEnabled == true) //is damage over time enabled?
            {
                DamageOverTime dot = null;

                foreach (DamageOverTime potentialDoT in target.EntityHealthComp.DotComps) //go through the potential damage over time components on the target
                {
                    if (potentialDoT.IsActive == false) //if it's not active, we can activate and use it
                    {
                        dot = potentialDoT;
                        break;
                    }
                }

                if (dot == null) //if no free DoT component has been found
                {
                    dot = target.gameObject.AddComponent<DamageOverTime>(); //create new instance of the Damage Over Time component on the target
                    target.EntityHealthComp.DotComps.Add(dot);
                }

                dot.Init(currDamage, dotAttributes, (source != null) ? source.FactionEntity : null, target.EntityHealthComp); //assign the DoT attributes.
            }
            else
                target.EntityHealthComp.AddHealth(-currDamage, source.FactionEntity);

            //spawn damage and effect objects:
            gameMgr.EffectPool.SpawnEffectObj(target.EntityHealthComp.GetDamageEffect(), target.transform.position, Quaternion.identity, target.transform);
            gameMgr.EffectPool.SpawnEffectObj(effect, target.transform.position, Quaternion.identity, target.transform, true, false, effectLifeTime);

            //if there's a valid source:
            if (source != null)
                source.AddDamageDealt(currDamage);

            //trigger attack damage dealt event
            source.InvokeAttackDamageDealtEvent();
            CustomEvents.OnAttackDamageDealt(source, target, currDamage);
        }

        //launch an area damage:
        public void LaunchAreaDamage (Vector3 center)
        {
            Collider[] collidersInRange = Physics.OverlapSphere(center, attackRanges[attackRanges.Length - 1].GetRange());
            foreach(Collider c in collidersInRange)
            {
                SelectionEntity selection = c.gameObject.GetComponent<SelectionEntity>();
                if (selection == null || selection.FactionEntity == null) //if the collider doesn't belong to an object with a selection component or it does but it's linked to a resource
                    continue; //move to next one

                if (source == null || (source.IsTargetValid(selection.FactionEntity) != ErrorMessage.none)) //make sure there's a valid source and that the current target can be attacked
                    continue;

                float distance = Vector3.Distance(selection.Source.transform.position, center);
                //target can be attacked by this source, go through the attack ranges to deal the right damage
                for (int i = 0; i < attackRanges.Length; i++)
                {
                    if (distance > attackRanges[i].GetRange()) //as long as the right range for this faction entity isn't found
                        continue;    //move to next one

                    //correct range is found, get either the unit or building damage value and apply damage
                    //but first check whether there's a custom damage defined for the target faction entitiy and if not, just apply the regular unit or building damage
                    if(!attackRanges[i].GetCustomDamage(selection.FactionEntity.GetCode(), selection.FactionEntity.GetCategory(), out currDamage))
                        currDamage = (selection.FactionEntity.Type == EntityTypes.unit) ? attackRanges[i].GetUnitDamage() : attackRanges[i].GetBuildingDamage();

                    DealDamage(selection.FactionEntity); //deal damage
                }
            }
        }
    }
}
