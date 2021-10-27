using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RTSEngine
{
    public enum SelectionTypes {single, multiple}; //type of adding selection

    [System.Serializable]
    public class SelectedEntities
    {
        private Dictionary<string, List<Entity>> selectedDic = new Dictionary<string, List<Entity>>();
        private Entity singleSelected = null;
        public int Count { private set; get; } //the amount of currently selected entities

        private EntityTypes lastEntityType = EntityTypes.none; //keep the last selected entity type in mind
        private bool currExclusive = false; //is the current selection type exclusive?

        //each entity type has their own selection options:
        [System.Serializable]
        public struct SelectionOptions
        {
            public EntityTypes entityType;
            public bool allowMultiple; //allow the associated entity type to be multiply selected
            public bool exclusive; //when true, then the associated entity type can't be selected with other entities
        }
        [SerializeField]
        private SelectionOptions[] selectionOptions = new SelectionOptions[0];

        SelectionManager manager;

        public void Init (SelectionManager manager) //method to init this instance
        {
            this.manager = manager;
            Count = 0; //reset selection count
        }

        public bool IsSelected(Entity entity) {
            if(selectedDic.TryGetValue(entity.GetCode(), out List<Entity> selectedList))
            {
                return selectedList.Contains(entity);
            }
            return false;
        } //is the input entity selected?
        
        //get a list of the selected entities of a certain type
        public List<Entity> GetEntitiesList (EntityTypes type, bool exclusive, bool playerFaction)
        {
            List<Entity> entities = new List<Entity>();

            foreach(string code in selectedDic.Keys) //get all the entity codes (keys) stored in the selected dictionary
            {
                Entity entity = selectedDic[code][0]; //get the first selected entity of the current type

                if (entity as FactionEntity != null) //there's a faction entity component
                {
                    if (playerFaction && (entity as FactionEntity).FactionID != GameManager.PlayerFactionID) //if we requested player faction units only and this entity doesn't belong to player's faction
                        return new List<Entity>();
                }

                if (type == EntityTypes.none || entity.Type == type) //if this matches the type we're looking for
                {
                    entities.AddRange(selectedDic[code].Select(selectedEntitiy => selectedEntitiy));
                    continue;
                }

                if (exclusive == true) //the entity is not a unit, return an empty list 
                    return new List<Entity>();
            }

            return entities;
        }

        //get a dictionary with key: entity type and value: list of selected instances of that type:
        public Dictionary<string, List<Entity>> GetEntitiesDictionary (EntityTypes type, bool exclusive, bool playerFaction)
        {
            Dictionary<string, List<Entity>> entities = new Dictionary<string, List<Entity>>();

            foreach(string code in selectedDic.Keys) //get all the entity codes (keys) stored in the selected dictionary
            {
                Entity entity = selectedDic[code][0]; //get the first selected entity of the current type
                if (entity as FactionEntity != null) //there's a faction entity component
                {
                    if (playerFaction && (entity as FactionEntity).FactionID != GameManager.PlayerFactionID) //if we requested player faction units only and this entity doesn't belong to player's faction
                        continue;
                }

                if (type == EntityTypes.none || entity.Type == type) //if this matches the type we're looking for
                {
                    entities.Add(code, selectedDic[code]);
                    continue;
                }

                if (exclusive == true) //the entity is not a unit, return an empty list 
                    continue;
            }

            return entities;
        }

        //returns the requested type if there's only one selected
        public Entity GetSingleEntity (EntityTypes type, bool playerFaction)
        {
            if (Count != 1 || (singleSelected.Type != type && type != EntityTypes.none)) //not one single entity is selected or the type does not match the input
                return null;

            //if we're requesting this single entity to be in the player's faction
            if (playerFaction && (singleSelected as FactionEntity) != null && (singleSelected as FactionEntity).FactionID != GameManager.PlayerFactionID)
                return null;

            return singleSelected;
        }

        //is the input selection entity is selected?
        public virtual bool Contains (Entity entity)
        {
            if (selectedDic.TryGetValue(entity.GetCode(), out List<Entity> selectedList))
                return selectedList.Contains(entity);

            return false;
        }

        //add an entity to the selection
        public virtual bool Add (IEnumerable<Entity> entitiesList) //select a list of entities
        {
            RemoveAll(); //remove all selected entities
            foreach (Entity e in entitiesList) //go through each entity and select it
                Add(e, SelectionTypes.multiple);

            return true;
        }
        
        public virtual bool Add (Entity newEntity, SelectionTypes type)
        {
            if (newEntity == null) //invalid entity
                return false;

            if (manager.MultipleSelectionKeyDown == true) //if the player is holding down the multiple selection key
                type = SelectionTypes.multiple; //multiple selection incoming

            if (currExclusive && newEntity.Type != lastEntityType) //if the last selection type was exclusive and this doesn't match the last selected entity type
                type = SelectionTypes.single; //single selection now (and all previous elements will be deselected)

            bool exclusiveOnSuccess = false; //will the selection be marked as exclusive in case the entity is successfully selected? by default no

            foreach(SelectionOptions options in selectionOptions) //go through all the selection options
            {
                if (newEntity.Type == options.entityType) //if the entity type matches
                {
                    if (options.exclusive == true) //if this entity type can be selected only exclusively
                    {
                        exclusiveOnSuccess = true; //mark selection as exclusive on success

                        if(newEntity.Type != lastEntityType) //the last selected entity does not match with the current type
                            type = SelectionTypes.single; //all previous selected elements will be deselected
                    }

                    if (type == SelectionTypes.multiple && options.allowMultiple == false) //if the selection type is multiple but that's not allowed for this entity type
                        type = SelectionTypes.single; //set type back to single to deselect previous elements

                    break; //entity type match found, no need to see the rest of the options
                }
            }

            switch(type)
            {
                case SelectionTypes.single: //single selection

                    RemoveAll(); //remove all selected entities
                    break;
                case SelectionTypes.multiple: //multiple selection:

                    if (manager.MultipleSelectionKeyDown && IsSelected(newEntity)) //if the multiple selection key is down & entity is already selected, selecting a new entity -> removing it from the already selected group
                    {
                        Remove(newEntity);
                        return false;
                    }
                    break;
            }

            if(newEntity.GetSelection().CanSelect() == true && IsSelected(newEntity) == false) //can be selected and not already selected? 
            {
                if (selectedDic.TryGetValue(newEntity.GetCode(), out List<Entity> targetList)) //if there's at least another entity of the same type that is already selected
                    targetList.Add(newEntity);
                else //if not add it to new list and new entry to dictionary
                {
                    selectedDic.Add(newEntity.GetCode(), new List<Entity> { newEntity});
                }

                Count++; //increment the selection count

                if (Count == 1) //first entity to get selected?
                {
                    singleSelected = newEntity; //assign as the single selected entity
                    singleSelected.GetSelection().IsSelectedOnly = true; //mark as selected only
                }
                else if (Count == 2) //only one entity was selected before
                    singleSelected.GetSelection().IsSelectedOnly = false; //not the only selected entity any more.

                newEntity.GetSelection().OnSelected();

                lastEntityType = newEntity.Type; //set the last selected entity type
                currExclusive = exclusiveOnSuccess; //is the selection exclusive now?

                return true;
            }

            return false;
        }

        //remove an entity from the selected list
        public void Remove (List<Entity> entitiesList) { while(entitiesList.Count > 0) { Remove(entitiesList[0]); } }
        public void Remove (Entity entity)
        {
            if (selectedDic.TryGetValue(entity.GetCode(), out List<Entity> selectedList)) //try to find the dictionary entry for the entity's type
            {
                selectedList.Remove(entity);

                Count--; //decrement selection count

                if (selectedList.Count == 0) //if the selection list is now empty, remove the whole entry from the dictionary
                    selectedDic.Remove(entity.GetCode());

                if (Count == 1) //if only one entity is left selected, assign it as the single selected entity
                {
                    singleSelected = GetEntitiesList(EntityTypes.none, false, false)[0];
                    singleSelected.GetSelection().IsSelectedOnly = true; //mark as selected only
                }
            }

            entity.GetSelection().OnDeselected();
        }

        //remove all selected entities from the selected list
        public void RemoveAll ()
        {
            string[] keys = new string[0]; //copy keys into new array because the dic will be modified during the foreach loop
            keys = selectedDic.Keys.Select(key => key).ToArray();

            Count = 0; //reset selection count

            foreach (string code in keys) //go through all the keys of the selection dictionary
            {
                List<Entity> selectedList = selectedDic[code]; //get the selection list associated with the current code
                selectedDic.Remove(code);

                while (selectedList.Count > 0) //go through all selected entities and deselect them
                {
                    Entity currEntity = selectedList[0];
                    selectedList.RemoveAt(0);
                    currEntity.GetSelection().OnDeselected();
                }
            }

            selectedDic.Clear(); //clear all entries from the selection dictionary
            singleSelected = null; //reset single selected entity reference
        }
    } 
}
