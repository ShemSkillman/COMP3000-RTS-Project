using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

using RTSEngine.UI;

/* RTSEditorHelper editor script created by Oussama Bouanani,  SoumiDelRio
 * This script is part of the RTS Engine */

namespace RTSEngine
{
    [InitializeOnLoad] //constructor of class is ran as soon as the project is open.
    public static class RTSEditorHelper
    {
        static RTSEditorHelper()
        {
            RefreshAssetFiles();
        }

        [MenuItem("RTS Engine/Refresh Asset Files", false, 1001)]
        private static void RefreshAssetFiles()
        {
            //cache asset files
            RefreshFactionTypes(); 
            RefreshResourceTypes();
            RefreshNPCTypes();
            RefreshEntityComponentTaskUIDataTypes();

            Debug.Log("[RTSEngineEditorHelper] Cached faction type, resource type, NPC type and entity component task UI data asset files.");
        }

        private static bool TryGetAllAssetFiles <T>(out List<T> assets, string filter = "DefaultAsset l:noLabel t:noType") where T : ScriptableObject
        {
            assets = new List<T> { };
            string[] guids = AssetDatabase.FindAssets(filter);

            if (guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    assets.Add(AssetDatabase.LoadAssetAtPath(assetPath, typeof(T)) as T);
                }

                return true;
            }
            else
            {
                Debug.Log(AssetDatabase.FindAssets("t:FactionTypeInfo").Length);

                return false;
            }
        }

        private static bool CacheAssetFiles <T> (out IEnumerable<T> targetEnumerable) where T : ScriptableObject, IAssetFile
        {
            targetEnumerable = null;
            if (TryGetAllAssetFiles<T>(out List<T> assetsList, $"t:{typeof(T).ToString()}"))
            {
                targetEnumerable = assetsList;
                return true;
            }

            return false;
        }

        public static bool GetAssetFilesDic <T> (out Dictionary<string, T> resultDic, IEnumerable<T> cached) where T : ScriptableObject, IAssetFile
        {
            resultDic = new Dictionary<string, T>();
            if (cached == null)
            {
                Debug.LogError($"Cache of {typeof(T).ToString()} asset files is empty!");
                return false;
            }

            resultDic.Add("Unassigned", null);

            foreach(T t in cached)
            {
                if (t == null)
                    continue;

                if (resultDic.ContainsKey(t.Key))
                {
                    Debug.LogError($"[RTSEditorHelper] '{t.Key}' is a duplicate key for the '{typeof(T).ToString()}' type in '{t.name}' and '{resultDic[t.Key].name}' asset files.", t);
                    return false;
                }

                resultDic.Add(t.Key, t);
            }

            return true;
        }

        private static void RefreshAssetTypes<T> (ref IEnumerable<T> cache, bool requireTest, T testType) where T : ScriptableObject, IAssetFile
        {
            if (!requireTest || cache == null || !cache.Contains(testType))
                CacheAssetFiles(out cache);
        }

        private static IEnumerable<FactionTypeInfo> factionTypes; //holds currently available FactionTypeInfo asset files.
        public static IEnumerable<FactionTypeInfo> FactionTypes { get { return factionTypes; } }
        public static void RefreshFactionTypes (bool requireTest = false, FactionTypeInfo testType = null)
        {
            RefreshAssetTypes(ref factionTypes, requireTest, testType);
        }

        private static IEnumerable<NPCTypeInfo> npcTypes = null; //holds currently available NPCTypeInfo asset files.
        public static IEnumerable<NPCTypeInfo> NPCTypes { get { return npcTypes; } }
        public static void RefreshNPCTypes (bool requireTest = false, NPCTypeInfo testType = null)
        {
            RefreshAssetTypes(ref npcTypes, requireTest, testType);
        }

        private static IEnumerable<ResourceTypeInfo> resourceTypes = null; //holds currently available ResourceTypeInfo asset files.
        public static IEnumerable<ResourceTypeInfo> ResourceTypes { get { return resourceTypes; } }
        public static void RefreshResourceTypes (bool requireTest = false, ResourceTypeInfo testType = null)
        {
            RefreshAssetTypes(ref resourceTypes, requireTest, testType);
        }

        private static IEnumerable<EntityComponentTaskUIData> entityComponentTaskUIDataTypes = null; //holds currently available NPCTypeInfo asset files.
        public static IEnumerable<EntityComponentTaskUIData> EntityComponentTaskUIDataTypes { get { return entityComponentTaskUIDataTypes; } }
        public static void RefreshEntityComponentTaskUIDataTypes (bool requireTest = false, EntityComponentTaskUIData testType = null)
        {
            RefreshAssetTypes(ref entityComponentTaskUIDataTypes, requireTest, testType); 
        }

        public static void Navigate (ref int index, int step, int max)
        {
            if (index + step >= 0 && index + step < max)
                index += step;
        }
    }
}
