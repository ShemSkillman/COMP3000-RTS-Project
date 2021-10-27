using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

namespace RTSEngine.EditorOnly
{
    public class MenuItems
    {
        public const string NewMapConfigPrefabName = "NewMap";

        [MenuItem("RTS Engine/Configure New Map", false, 51)]
        private static void ConfigNewMapOption()
        {
            //destroy the objects in the current scene:
            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>() as GameObject[])
                Object.DestroyImmediate(obj);

            GameObject newMap = Object.Instantiate(Resources.Load(NewMapConfigPrefabName, typeof(GameObject))) as GameObject;

            newMap.transform.DetachChildren();

            Object.DestroyImmediate(newMap);

            Debug.Log("Please set up the factions in order to fully configure the new map: http://soumidelrio.com/docs/unity-rts-engine/getting-started-create-a-new-map/");
        }

        [MenuItem("RTS Engine/Single Player Menu", false, 101)]
        private static void SinglePlayerMenuOption()
        {
            //destroy the objects in the current scene:
            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>() as GameObject[])
                Object.DestroyImmediate(obj);

            GameObject singlePlayerMenu = Object.Instantiate(Resources.Load("SinglePlayerMenu_Demo", typeof(GameObject))) as GameObject;

            singlePlayerMenu.transform.DetachChildren();

            Object.DestroyImmediate(singlePlayerMenu);
        }

#if RTSENGINE_MIRROR
        [MenuItem("RTS Engine/Multiplayer Menu (Mirror)", false, 102)]
        private static void MultiplayerMenuMenu_Mirror()
        {
            //destroy the objects in the current scene:
            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>() as GameObject[])
                Object.DestroyImmediate(obj);

            GameObject multiplayerMenu_Mirror = Object.Instantiate(Resources.Load("MultiplayerMenu_Mirror_Demo", typeof(GameObject))) as GameObject;

            multiplayerMenu_Mirror.transform.DetachChildren();

            Object.DestroyImmediate(multiplayerMenu_Mirror);
        }
#endif

        [MenuItem("RTS Engine/New Unit", false, 151)]
        private static void NewUnitOption()
        {
            Object.Instantiate(Resources.Load("NewUnit", typeof(GameObject)));
        }

        [MenuItem("RTS Engine/New Building", false, 152)]
        private static void NewBuildingOption()
        {
            Object.Instantiate(Resources.Load("NewBuilding", typeof(GameObject)));
        }

        [MenuItem("RTS Engine/New Resource", false, 153)]
        private static void NewResourceOption()
        {
            Object.Instantiate(Resources.Load("NewResource", typeof(GameObject)));
        }

        [MenuItem("RTS Engine/New Attack Object", false, 154)]
        private static void NewAttackObject()
        {
            Object.Instantiate(Resources.Load("NewAttackObject", typeof(GameObject)));
        }

        [MenuItem("RTS Engine/New NPC Manager", false, 155)]
        private static void NewNPCManager()
        {
            Object.Instantiate(Resources.Load("NewNPCManager", typeof(GameObject)));
        }

        [MenuItem("RTS Engine/Documentation", false, 201)]
        private static void DocOption()
        {
            Application.OpenURL("http://soumidelrio.com/docs/unity-rts-engine/");
        }
        [MenuItem("RTS Engine/Review", false, 202)]
        private static void ReviewOption()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/templates/packs/rts-engine-79732");
        }
    }
}
