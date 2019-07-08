using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Hedera {
    [CustomEditor( typeof(IvyDataAsset))]
    public class IvyDataAssetEditor : Editor
    {
        public override void OnInspectorGUI() {
            var data = (IvyDataAsset)target;
            var allSubassets = AssetDatabase.LoadAllAssetRepresentationsAtPath( AssetDatabase.GetAssetPath(data) );

            EditorGUILayout.HelpBox("When you paint ivy in a scene, Hedera stores the 3D mesh in this database file.", MessageType.Info);
            EditorGUILayout.HelpBox("You can manually delete meshes here if you're sure you won't need them anymore. But be careful! Deletion cannot be undone.", MessageType.Warning);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( string.Format("{0} mesh references / {1} mesh data saved", data.meshList.Count, allSubassets.Length));
            var content = new GUIContent("Cleanup Unreferenced Meshes", "Sometimes there's bugs in Hedera, and sometimes it doesn't properly clean up mesh data. Click this button to delete meshes that aren't being used anymore.");
            if ( GUILayout.Button(content) ) {
                var allReferencedMeshes = data.meshList.Values.ToList();
                for ( int i=0; i<allSubassets.Length; i++ ) {
                    if (!allReferencedMeshes.Contains((Mesh)allSubassets[i])) {
                        Object.DestroyImmediate(allSubassets[i], true);
                    }
                }
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.Space();

            foreach ( var kvp in data.meshList ) {
                EditorGUILayout.BeginHorizontal();
                if ( GUILayout.Button("x", EditorStyles.miniButton, GUILayout.MaxWidth(20)) ) {
                    if ( kvp.Value != null) {
                        Object.DestroyImmediate( kvp.Value, true);
                    }
                    data.meshList.Remove( kvp.Key );
                    EditorGUILayout.EndHorizontal();
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
                    break;
                }
                EditorGUILayout.LabelField( kvp.Key.ToString(), EditorStyles.miniBoldLabel, GUILayout.MaxWidth(128) );
                EditorGUILayout.ObjectField( kvp.Value, typeof(Mesh), false);
                EditorGUILayout.EndHorizontal();
            }
        }

    }
}
