using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tools.FindMissingReference
{
    public class FindMissingReferenceTool
    {
        public void ScanProject(ref List<BrokenPrefab> brokenPrefabs)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var prefabs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(x =>
                    new PrefabContextObject(AssetDatabase.LoadAssetAtPath(x, typeof(GameObject)) as GameObject, x))
                .ToList();
            CheckPrefabs(prefabs, ref brokenPrefabs);
        }

        private void CheckPrefabs(List<PrefabContextObject> prefabs, ref List<BrokenPrefab> brokenPrefabs)
        {
            foreach (var prefab in prefabs)
            {
                try
                {
                    FindMissingPrefabs(prefab, true, ref brokenPrefabs);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void FindMissingPrefabs(PrefabContextObject prefab, bool isRoot, ref List<BrokenPrefab> brokenPrefabs)
        {
            FindMissingReferences(prefab, true, ref brokenPrefabs);
            if (prefab.GameObject == null)
            {
                return;
            }

            if (prefab.GameObject.name.Contains("Missing Prefab"))
            {
                brokenPrefabs.Add(new BrokenPrefab(prefab.GameObject,
                    $"Missing prefab in <b>{prefab.ContextPath}</b>\nPrefab: <b>{prefab.GameObject.name}</b>"));
                return;
            }

            if (PrefabUtility.IsPrefabAssetMissing(prefab.GameObject))
            {
                brokenPrefabs.Add(new BrokenPrefab(prefab.GameObject,
                    $"Missing prefab in <b>{prefab.ContextPath}</b>\nPrefab: <b>{prefab.GameObject.name}</b>"));
                return;
            }

            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefab.GameObject))
            {
                brokenPrefabs.Add(new BrokenPrefab(prefab.GameObject,
                    $"Missing prefab in <b>{prefab.ContextPath}</b>\nPrefab: <b>{prefab.GameObject.name}</b>"));
                return;
            }

            if (!isRoot)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(prefab.GameObject))
                {
                    return;
                }

                var root = PrefabUtility.GetNearestPrefabInstanceRoot(prefab.GameObject);
                if (root == prefab.GameObject)
                {
                    return;
                }
            }

            foreach (Transform childT in prefab.GameObject.transform)
            {
                FindMissingPrefabs(new PrefabContextObject(childT.gameObject, prefab.ContextPath), false,
                    ref brokenPrefabs);
            }
        }

        private void FindMissingReferences(PrefabContextObject prefab, bool findInChildren,
            ref List<BrokenPrefab> brokenPrefabs)
        {
            var components = prefab.GameObject.GetComponents<Component>();

            for (var j = 0; j < components.Length; j++)
            {
                var c = components[j];
                if (!c)
                {
                    brokenPrefabs.Add(new BrokenPrefab(prefab.GameObject,
                        $"Missing Component in <b>{prefab.ContextPath}</b>\nGameObject: <b>{FullPath(prefab.GameObject)}</b>"));
                    continue;
                }

                var property = new SerializedObject(c).GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    {
                        brokenPrefabs.Add(new BrokenPrefab(prefab.GameObject,
                            $"Missing reference in <b>{prefab.ContextPath}</b>\nComponent: <b>{c.GetType().Name}</b>\nProperty: <b>{ObjectNames.NicifyVariableName(property.name)}</b>"));
                    }
                }
            }

            if (findInChildren)
            {
                foreach (Transform child in prefab.GameObject.transform)
                {
                    FindMissingReferences(new PrefabContextObject(child.gameObject, prefab.ContextPath), true,
                        ref brokenPrefabs);
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