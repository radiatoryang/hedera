using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Hedera
{
	[InitializeOnLoad]
    public class IvyCore
    {
		public static IvyCore Instance;

		public static double lastRefreshTime { get; private set; }
		static double refreshInterval = 0.1;

		public static List<IvyGenerator> ivyGenerators = new List<IvyGenerator>();

        // called on InitializeOnLoad
        static IvyCore()
        {
            if (Instance == null)
            {
                Instance = new IvyCore();
            }
            EditorApplication.update += Instance.OnEditorUpdate;
			ivyGenerators.Clear();
        }

        void OnEditorUpdate()
        {
			bool needsRepaint = false;
            if (EditorApplication.timeSinceStartup > lastRefreshTime + refreshInterval)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                foreach (var gen in ivyGenerators) {
					foreach ( var ivy in gen.ivyGraphs) {
						if ( ivy.isGrowing ) {
							ivy.GrowIvyStep(gen.ivyProfile);
							needsRepaint = true;
						}
					}
				}
				// if ( needsRepaint ) {
				// 	SceneView.RepaintAll();
				// }
            }
			
        }

        [MenuItem("Hedera/Create New Ivy Generator...")]
        public static void NewAssetFromHederaMenu()
        {
			string path = EditorUtility.SaveFilePanelInProject("Hedera: Create New Ivy Generator .asset file...", "NewIvyGenerator.asset", "asset", "Choose where in your project to save the new ivy generator asset file.");
            CreateNewAsset(path);
        }

		public static void CreateNewAsset(string path = "Assets/NewIvyGenerator.asset") {
			IvyGenerator asset = ScriptableObject.CreateInstance<IvyGenerator>();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
		}

		[MenuItem("Hedera/Force-Stop All Ivy Growing")]
        public static void ForceStopGrowing()
        {
            foreach ( var gen in ivyGenerators ) {
				foreach (var ivy in gen.ivyGraphs ) {
					ivy.isGrowing = false;
				}
			}
        }


    }
}
