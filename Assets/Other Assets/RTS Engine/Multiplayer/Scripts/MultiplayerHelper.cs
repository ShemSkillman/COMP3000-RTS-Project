using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{

    public enum MultiplayerMenu { main, loading, lobby };

    public enum DisconnectionType { left, kick, gameVersion, timeOut, abort }

    /// <summary>
    /// 
    /// addUnit: used when the unit is moving towards an entity that can add it (entity that includes a component that implements IAddableUnit)
    /// </summary>
    public enum InputMode
    {
        none,
        create,
        customCommand,
        destroy,
        unitGroup,
        multipleAttack,
        research,
        unitEscape,
        self,
        movement,
        factionEntity,
        unit,
        building,
        resource,
        collect,
        dropoff,
        faction,
        attack,
        portal,
        upgrade,
        APC,
        APCEject,
        APCEjectAll,
        heal,
        convertOrder,
        convert,
        builder,
        health,

        addUnit, //used when the unit is moving towards an entity that can add it (entity that includes a component that implements IAddableUnit)
    }; //allowed types of the input's target.

    //these are the attributes that an input can have.
    public struct NetworkInput
    {
        public int factionID; //input player's faction ID

        public byte sourceMode; //input source's mode
        public byte targetMode; //input's target mode

        public int sourceID; //object that launched the command
        public string code; //a string that holds a group of unit sources for group unit related commands or just a task code for other commands

        public int targetID; //target object that will get the command

        public Vector3 initialPosition; //initial position of the source obj
        public Vector3 targetPosition; //target position.

        public Vector3 extraPosition; //this field allows to add 3 extra float values in the network input struct.

        public bool playerCommand; //has this input command been requested directly by the player?

        public int value; //extra int attribute
    }
}