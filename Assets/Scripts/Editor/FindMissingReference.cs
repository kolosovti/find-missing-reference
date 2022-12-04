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
            var prefabs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(x => new PrefabContextObject(PrefabUtility.LoadPrefabContents(x), x))
                .ToList();
            CheckPrefabs(prefabs);
        }

        private void CheckPrefabs(List<PrefabContextObject> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                FindMissingPrefabs(prefab, true);
            }
        }

        private void FindMissingPrefabs(PrefabContextObject prefab, bool isRoot)
        {
            FindMissingReferences(prefab, true);
            if (prefab.GameObject == null)
            {
                return;
            }

            if (prefab.GameObject.name.Contains("Missing Prefab"))
            {
                Debug.LogError($"<b>{prefab.ContextPath}</b> has missing prefab {prefab.GameObject.name}");
                return;
            }

            if (PrefabUtility.IsPrefabAssetMissing(prefab.GameObject))
            {
                Debug.LogError($"<b>{prefab.ContextPath}</b> has missing prefab {prefab.GameObject.name}");
                return;
            }

            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefab.GameObject))
            {
                Debug.LogError($"<b>{prefab.ContextPath}</b> has missing prefab {prefab.GameObject.name}");
                return;
            }

            if (!isRoot)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(prefab.GameObject))
                {
                    return;
                }

                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(prefab.GameObject);
                if (root == prefab.GameObject)
                {
                    return;
                }
            }

            foreach (Transform childT in prefab.GameObject.transform)
            {
                FindMissingPrefabs(new PrefabContextObject(childT.gameObject, prefab.ContextPath), false);
            }
        }

        private void FindMissingReferences(PrefabContextObject prefab, bool findInChildren = false)
        {
            var components = prefab.GameObject.GetComponents<Component>();

            for (var j = 0; j<components.Length; j++)
            {
                var c = components[j];
                if (!c)
                {
                    Debug.LogError($"Missing Component in GameObject: {FullPath(prefab.GameObject)} in {prefab.ContextPath}", prefab.GameObject);
                    continue;
                }
                
                var property = new SerializedObject(c).GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (property.objectReferenceValue == null
                            && property.objectReferenceInstanceIDValue != 0)
                        {
                            Debug.LogError(
                                $"<b>{prefab.ContextPath}</b> has missing reference! " +
                                $"Component: <b>{c.GetType().Name}</b>, " +
                                $"Property: {ObjectNames.NicifyVariableName(property.name)}",
                                prefab.GameObject);
                        }
                    }
                }
            }

            if (findInChildren)
            {
                foreach (Transform child in prefab.GameObject.transform)
                {
                    FindMissingReferences(new PrefabContextObject(child.gameObject, prefab.ContextPath), true);
                }
            }
        }

        private string FullPath(GameObject go)
        {
            var parent = go.transform.parent;
            return parent == null ? go.name : FullPath(parent.gameObject) + "/" + go.name;
        }
    }

    public class PrefabContextObject
    {
        public string ContextPath;
        public GameObject GameObject;

        public PrefabContextObject(GameObject go, string contextPath)
        {
            GameObject = go;
            ContextPath = contextPath;
        }
    }
}