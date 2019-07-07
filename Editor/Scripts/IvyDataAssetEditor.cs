using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Hedera {
    [CustomEditor( typeof(IvyDataAsset))]
    public class IvyDataAssetEditor : Editor
    {
        public override void OnInspectorGUI() {
            var data = (IvyDataAsset)target;
            EditorGUILayout.LabelField("Mesh list " + data.meshList.Count);

            GUI.enabled = false;
            foreach ( var kvp in data.meshList ) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField( kvp.Key.ToString() );
                EditorGUILayout.ObjectField( kvp.Value, typeof(Mesh), false);
                EditorGUILayout.EndHorizontal();
            }
            GUI.enabled = true;
        }

        // thanks, networm!
        // adapted from https://github.com/networm/FindReferencesInProject/blob/master/FindReferencesInProject.cs
        static void CleanupUnusedMeshes()
        {
            // var sw = new System.Diagnostics.Stopwatch();
            // sw.Start();

            var referenceCache = new Dictionary<string, List<string>>();

            string[] guids = AssetDatabase.FindAssets("");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

                foreach (var dependency in dependencies)
                {
                    if (referenceCache.ContainsKey(dependency))
                    {
                        if (!referenceCache[dependency].Contains(assetPath))
                        {
                            referenceCache[dependency].Add(assetPath);
                        }
                    }
                    else
                    {
                        referenceCache[dependency] = new List<string>(){ assetPath };
                    }
                }
            }

            // Debug.Log("Build index takes " + sw.ElapsedMilliseconds + " milliseconds");

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            Debug.Log("Find: " + path, Selection.activeObject);
            if (referenceCache.ContainsKey(path))
            {
                foreach (var reference in referenceCache[path])
                {
                    Debug.Log(reference, AssetDatabase.LoadMainAssetAtPath(reference));
                }
            }
            else
            {
                Debug.LogWarning("No references");
            }

            referenceCache.Clear();
        }
    }
}
