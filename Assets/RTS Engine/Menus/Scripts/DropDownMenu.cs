using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;

/* Drop Down Menu class created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    public class DropDownMenu<T>
    {
        protected Dictionary<int, T> elementsDic = new Dictionary<int, T>();

        [SerializeField]
        private Dropdown menu = null; //the actual drop down menu goes here

        private T defaultValue; //the default value of the type to return if the drop down menu value isn't valid.
        private string name; //the name of the value that this drop down menu is handling

        //constructor
        public DropDownMenu(T defaultValue, string name)
        {
            this.defaultValue = defaultValue;
            this.name = name;
        }

        //get the value set by the drop down menu
        public T GetValue()
        {
            return (elementsDic.TryGetValue(menu.value, out T returnValue)) ? returnValue : defaultValue;
        }

        //get/set the drop down menu value/defeat condition index:
        public int MenuIndex
        {
            set
            {
                menu.value = value;
            }
            get
            {
                return menu.value;
            }
        }

        //set the drop down menu interactable status
        public void ToggleInteracting(bool enable) { menu.interactable = enable; }

        public virtual void Init(List<string> optionsList)
        {
            //set the defeat condition drop down menu:
            Assert.IsNotNull(menu, "[Single Player Manager] The drop down menu of the " + name + " menu hasn't been assigned.");

            menu.ClearOptions();
            menu.AddOptions(optionsList); //add the names of the defeat conditions to the drop down menu
        }
    }

    //Because we can't serialize classes with generic data types, child classes need to be created for each drop down menu type so they can be displayed in the inspector
    [System.Serializable]
    public class DefeatConditionMenu : DropDownMenu<DefeatConditions> //for the defeat condition drop down menu
    {
        [System.Serializable]
        public struct DefeatConditionUIElement //for each element, input the name that will be displayed in the drop down menu and then the correspondant value
        {
            public string name;
            public DefeatConditions condition;
        }
        [SerializeField]
        private DefeatConditionUIElement[] defeatConditionUIElements = new DefeatConditionUIElement[0];

        public DefeatConditionMenu() : base(DefeatConditions.destroyCapital, "Defeat Condition") { }

        public override void Init(List<string> optionsList = null)
        {
            elementsDic.Clear();
            foreach (DefeatConditionUIElement element in defeatConditionUIElements)
                elementsDic.Add(elementsDic.Count, element.condition);

            base.Init(defeatConditionUIElements.Select(element => element.name).ToList());
        }

    }

    [System.Serializable]
    public class SpeedModifierMenu : DropDownMenu<float> //for the game speed drop down menu
    {
        [System.Serializable]
        public struct SpeedModifierUIElement //for each element, input the name that will be displayed in the drop down menu and then the correspondant value
        {
            public string name;
            public float speedModifier;
        }
        [SerializeField]
        private SpeedModifierUIElement[] speedModifierUIElements = new SpeedModifierUIElement[0];

        public SpeedModifierMenu() : base(1.0f, "Game Speed") { }

        public override void Init(List<string> optionsList = null)
        {
            elementsDic.Clear();
            foreach (SpeedModifierUIElement element in speedModifierUIElements)
                elementsDic.Add(elementsDic.Count, element.speedModifier);

            base.Init(speedModifierUIElements.Select(element => element.name).ToList());
        }
    }
}
