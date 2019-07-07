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
            EditorGUILayout.LabelField("Mesh list: " + data.meshList.Count);

            GUI.enabled = false;
            foreach ( var kvp in data.meshList ) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField( kvp.Key );
                EditorGUILayout.ObjectField( kvp.Value, typeof(Mesh), false);
                EditorGUILayout.EndHorizontal();
            }
            GUI.enabled = true;
        }
    }
}
