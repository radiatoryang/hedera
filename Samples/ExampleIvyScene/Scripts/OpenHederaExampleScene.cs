using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Hedera {
    public class OpenHederaExampleScene
    {
            [MenuItem("Hedera/Open HederaExampleScene.unity")]
            public static void OpenExampleScene () {
                var scenes = AssetDatabase.FindAssets("HederaExampleScene t:Scene");
                if ( scenes != null && scenes.Length > 0) {
                    scenes[0] = AssetDatabase.GUIDToAssetPath( scenes[0] );
                } else {
                    Debug.LogWarning( "Hedera > Open HederaExampleScene couldn't find the example scene, which is usually in /Samples/ExampleIvyScene/HederaExampleScene.unity ... you may have renamed the scene file or deleted it.");
                    return;
                }
                if ( UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ) {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene( scenes[0], UnityEditor.SceneManagement.OpenSceneMode.Single);
                }
            }
    }
}
