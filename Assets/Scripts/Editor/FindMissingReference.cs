using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tools.FindMissingReference
{
    public class FindMissingReferenceWindow : EditorWindow
    {
        [MenuItem("Tools/Find Missing Prefabs")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FindMissingReferenceWindow));
        }

        private void OnGUI()
        {
            GUILayout.Label("Find Missing Reference", EditorStyles.largeLabel);
            if (GUILayout.Button("Scan Project Assets"))
            {
                ScanProject();
            }
        }

        private void ScanProject()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            CheckPrefabs(guids.Select(AssetDatabase.GUIDToAssetPath).Select(x => new DatabasePrefab
                { Path = x, Prefab = PrefabUtility.LoadPrefabContents(x) }).ToList());
        }

        private void CheckPrefabs(List<DatabasePrefab> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                FindMissingPrefabs(prefab.Prefab, prefab.Path, true);
            }
        }

        private void FindMissingPrefabs(GameObject prefab, string prefabName, bool isRoot)
        {
            if (prefab == null)
            {
                return;
            }

            if (prefab.name.Contains("Missing Prefab"))
            {
                Debug.LogError($"<b>{prefabName}</b> has missing prefab {prefab.name}");
                return;
            }

            if (PrefabUtility.IsPrefabAssetMissing(prefab))
            {
                Debug.LogError($"<b>{prefabName}</b> has missing prefab {prefab.name}");
                return;
            }

            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefab))
            {
                Debug.LogError($"<b>{prefabName}</b> has missing prefab {prefab.name}");
                return;
            }

            if (!isRoot)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(prefab))
                {
                    return;
                }

                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(prefab);
                if (root == prefab)
                {
                    return;
                }
            }

            foreach (Transform childT in prefab.transform)
            {
                FindMissingPrefabs(childT.gameObject, prefabName, false);
            }
        }
    }

    public class DatabasePrefab
    {
        public string Path;
        public GameObject Prefab;
    }
}