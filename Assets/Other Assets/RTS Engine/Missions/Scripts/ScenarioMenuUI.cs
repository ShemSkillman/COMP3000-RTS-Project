using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    public class ScenarioMenuUI : MonoBehaviour
    {
        private int ID;

        [SerializeField]
        private Text nameText = null; //will display the associated scenario's name
        [SerializeField]
        private Text descriptionText = null; //will display the associated scenario's description
        [SerializeField]
        private Button loadButton = null; //will allow the player to load the scenario's scene in case it's unlocked

        //other components:
        ScenarioLoader manager;

        public void Init(ScenarioLoader manager, int index)
        {
            this.manager = manager;
            ID = index;
        }

        //called by the manager to refresh the content of the UI elements
        public void Refresh (string name, string description, bool available)
        {
            nameText.text = name;
            descriptionText.text = description;
            loadButton.interactable = available;
        }

        //called when the load button is clicked
        public void OnLoadButtonClick ()
        {
            manager.Load(ID);
        }
    }
}
