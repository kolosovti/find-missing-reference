using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tools.FindMissingReference
{
    public class FindMissingReferenceWindow : EditorWindow
    {
        private readonly float _space = 5f;
        [SerializeField] private List<BrokenPrefab> _brokenPrefabs = new List<BrokenPrefab>();
        private int _page;
        private readonly int _pageSize = 50;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Find Missing Prefabs")]
        public static void ShowWindow()
        {
            GetWindow(typeof(FindMissingReferenceWindow));
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var headerRect = EditorGUILayout.GetControlRect();
            headerRect.height = 100f;
            EditorGUI.DrawRect(headerRect, Color.gray);

            var titleRect = headerRect;
            titleRect.width -= _space * 2;
            titleRect.height = 50;
            titleRect.x += _space;
            titleRect.y += _space;

            var titleStyle = new GUIStyle(GUI.skin.box)
            {
                richText = true,
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    background = Texture2D.linearGrayTexture
                }
            };
            EditorGUI.LabelField(titleRect, "<b>Find Missing Reference Tool</b>", titleStyle);

            var buttonRect = headerRect;
            buttonRect.width /= 2;
            buttonRect.height = headerRect.height - titleRect.height - _space * 3;
            buttonRect.x = headerRect.width / 4;
            buttonRect.y = titleRect.height + _space * 2;

            if (GUI.Button(buttonRect, "Scan Project Assets"))
            {
                if (_brokenPrefabs != null)
                {
                    _brokenPrefabs.Clear();
                }
                else
                {
                    _brokenPrefabs = new List<BrokenPrefab>();
                }

                var tool = new FindMissingReferenceTool();
                tool.ScanProject(_brokenPrefabs);
            }

            GUILayout.Space(120f);

            if (_brokenPrefabs?.Count > 0)
            {
                var prefabObject = _brokenPrefabs.FirstOrDefault(x => x != null);
                if (prefabObject == null)
                {
                    return;
                }

                ScriptableObject target = this;
                var serializedObject = new SerializedObject(target);
                var serializedProperty = serializedObject.FindProperty(nameof(_brokenPrefabs));

                EditorGUILayout.HelpBox($"Found {serializedProperty.arraySize} broken prefabs!", MessageType.Warning);

                if (_page > 0)
                {
                    var previousPageButtonRect = EditorGUILayout.GetControlRect();
                    previousPageButtonRect.height = EditorGUIUtility.singleLineHeight * 2;
                    previousPageButtonRect.width = EditorGUIUtility.singleLineHeight * 3;
                    previousPageButtonRect.x = 3f;
                    previousPageButtonRect.y = 183;
                    if (GUI.Button(previousPageButtonRect, "◀"))
                    {
                        if (_page > 0)
                        {
                            _page -= 1;
                        }
                    }
                }

                if ((_page + 1) * _pageSize < _brokenPrefabs.Count)
                {
                    var nextPageButtonRect = EditorGUILayout.GetControlRect();
                    nextPageButtonRect.x = nextPageButtonRect.width - EditorGUIUtility.singleLineHeight * 3 + 3f;
                    nextPageButtonRect.y = 183;
                    nextPageButtonRect.height = EditorGUIUtility.singleLineHeight * 2;
                    nextPageButtonRect.width = EditorGUIUtility.singleLineHeight * 3;
                    if (GUI.Button(nextPageButtonRect, "▶"))
                    {
                        if (_page < _brokenPrefabs.Count / _pageSize)
                        {
                            _page += 1;
                        }
                    }
                }
                else
                {
                    if (_page == 0)
                    {
                        //Auto Layout fix
                        GUILayout.Space(20f);
                    }
                }

                var pageInfoRect = EditorGUILayout.GetControlRect();
                pageInfoRect.y = 183;
                pageInfoRect.height = EditorGUIUtility.singleLineHeight * 2;

                var pageStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUI.LabelField(pageInfoRect, $"Page {_page + 1}/{_brokenPrefabs.Count / _pageSize + 1}",
                    pageStyle);

                int latestElement;
                if (serializedProperty.arraySize > (_page + 1) * _pageSize)
                {
                    latestElement = (_page + 1) * _pageSize;
                }
                else
                {
                    latestElement = serializedProperty.arraySize;
                }

                for (var i = _page * _pageSize; i < latestElement; i++)
                {
                    EditorGUILayout.PropertyField(serializedProperty.GetArrayElementAtIndex(i));
                    if (i < latestElement - 1)
                    {
                        EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    [Serializable]
    public class BrokenPrefab
    {
        public string Log;
        public GameObject Prefab;

        public BrokenPrefab(GameObject go, string log)
        {
            Prefab = go;
            Log = log;
        }
    }

    [CustomPropertyDrawer(typeof(BrokenPrefab))]
    public class HumanPropertyDrawer : PropertyDrawer
    {
        private const float space = 5;

        public override void OnGUI(Rect rect,
            SerializedProperty property,
            GUIContent label)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var propertyRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                GetCustomPropertyHeight()
            );
            DrawMainProperties(propertyRect, property);

            EditorGUI.indentLevel = indent;
        }

        private void DrawMainProperties(Rect propertyRect,
            SerializedProperty human)
        {
            EditorGUI.DrawRect(propertyRect, Color.gray);

            var prefabRect = propertyRect;
            prefabRect.height -= space * 2;
            prefabRect.width = prefabRect.height * 2;
            prefabRect.x += space;
            prefabRect.y += space;
            EditorGUI.PropertyField(prefabRect, human.FindPropertyRelative(nameof(BrokenPrefab.Prefab)),
                GUIContent.none);

            var logRect = propertyRect;
            logRect.width -= prefabRect.width + space * 3;
            logRect.height -= space * 2;
            logRect.x += prefabRect.width + space * 2;
            logRect.y += space;

            var myStyle = new GUIStyle(GUI.skin.box);
            myStyle.richText = true;
            myStyle.fontSize -= 2;
            myStyle.alignment = TextAnchor.MiddleLeft;
            myStyle.normal.background = Texture2D.linearGrayTexture;
            EditorGUI.TextArea(logRect, human.FindPropertyRelative(nameof(BrokenPrefab.Log)).stringValue, myStyle);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetCustomPropertyHeight();
        }

        private float GetCustomPropertyHeight()
        {
            return EditorGUIUtility.singleLineHeight * 4;
        }
    }
}