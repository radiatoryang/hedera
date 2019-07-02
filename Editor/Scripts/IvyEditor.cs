using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Hedera
{
    [CustomEditor(typeof(IvyBehavior))]
    public class IvyEditor : Editor
    {
        IvyBehavior ivyBehavior;

        bool isPlantingModeActive, showEditorFoldout;

        private Vector3 lastPos, mousePos, mouseNormal, mouseDirection;
        double lastEditorTime, deltaTime;
        // private Quaternion mouseRot;

        // got working painter code from https://github.com/marmitoTH/Unity-Prefab-Placement-Editor
        private void OnSceneGUI()
        {
            if ( ivyBehavior == null) {
                ivyBehavior = (IvyBehavior)target;
            }

            foreach ( var graph in ivyBehavior.ivyGraphs) {
                if ( !graph.generateMeshDuringGrowth ) {
                    DrawDebugIvy( graph );
                }
            }

            if ( !isPlantingModeActive ) {
                return;
            }

            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

            MousePosition();

            if (Event.current.type == EventType.Repaint ) {
                deltaTime = EditorApplication.timeSinceStartup - lastEditorTime;
                lastEditorTime = EditorApplication.timeSinceStartup;
            }

            if ((current.type == EventType.MouseDrag || current.type == EventType.MouseDown) )
            {
  
                if (current.button == 0 && (lastPos == Vector3.zero || CanDraw()) && !current.shift)
                {
                    mouseDirection = Vector3.MoveTowards( mouseDirection, (mousePos - lastPos).normalized, System.Convert.ToSingle(deltaTime) );
                    lastPos = mousePos;
                    ivyBehavior.ivyGraphs.Add( IvyCore.SeedNewIvyGraph(lastPos, mouseDirection, mouseNormal, ivyBehavior.generateMeshDuringGrowth) );

                } 
                else if (current.button == 0 && current.shift)
                {
                    lastPos = mousePos;
                    // erase
                }
            }

            if (current.type == EventType.MouseUp)
                lastPos = Vector3.zero;

            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            SceneView.RepaintAll();
        }

        public bool CanDraw ()
        { 
            float dist = Vector3.Distance(mousePos, lastPos);

            if (dist >= 0.5f)
                return true;
            else
                return false;
        }

        public void MousePosition ()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity)) // TODO: layer mask
            {
                mousePos = hit.point + hit.normal * 0.1f;
                mouseNormal = hit.normal;
                // mouseRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Handles.color = Color.blue;
                Handles.DrawWireDisc(mousePos, hit.normal, 0.5f / 2);
                Handles.DrawLine(mousePos, mousePos + hit.normal * 0.5f);
            }
        }

    	public void DrawDebugIvy(IvyGraph graph, Color debugColor = default(Color)) {
			if ( debugColor == default(Color)) {
				debugColor = Color.yellow;
			}
            Handles.color = debugColor;
            if ( graph.debugLineSegmentsArray != null ) {
                Handles.DrawLines( graph.debugLineSegmentsArray );
            }
		}

        //called whenever the inspector gui gets rendered
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if ( ivyBehavior == null) {
                ivyBehavior = (IvyBehavior)target;
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            ivyBehavior.profileAsset = EditorGUILayout.ObjectField( ivyBehavior.profileAsset, typeof(IvyProfileAsset), false ) as IvyProfileAsset;

            if ( ivyBehavior.profileAsset == null) {
                EditorGUILayout.HelpBox("Please assign an Ivy Profile Asset.", MessageType.Warning);
                if ( GUILayout.Button("Create new Ivy Profile Asset...") ) {
                    var newAsset = IvyCore.CreateNewAsset("");
                    if ( newAsset != null) {
                        ivyBehavior.profileAsset = newAsset;
                        Selection.activeGameObject = ivyBehavior.gameObject;
                    }
                }
                EditorGUILayout.EndVertical();
                return;
            }

            var ivyProfile = ivyBehavior.profileAsset.ivyProfile;

            if ( !IvyCore.ivyBehaviors.Contains(ivyBehavior) ) {
                IvyCore.ivyBehaviors.Add(ivyBehavior);
            }

            // EditorGUILayout.HelpBox("Hint:\nPress 'Start Plant' to begin a new ivy root, then press"
            //                 + " 'p' on your keyboard to place new root in the SceneView. "
            //                 + "In 3D Mode you have to place root onto objects with colliders."
            //                 + " You can only place one root at the scene view position.", MessageType.Info);

            
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            showEditorFoldout = EditorGUILayout.Foldout(showEditorFoldout, "Ivy Profile Settings");
            if (EditorGUILayout.BeginFadeGroup(showEditorFoldout ? 1 : 0))
            {
                GUI.changed = false;
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Growth", EditorStyles.boldLabel);
                GUIContent content = null;
                
                content = new GUIContent("Ivy Step Distance", "How far the ivy tries to move each frame. (Default: 0.25)\nSmaller = more curvy, smoother, more polygons.\nLarger = more angular and fewer polygons.");
                ivyProfile.ivyStepDistance = EditorGUILayout.Slider(content, ivyProfile.ivyStepDistance, 0, 1f);

                content = new GUIContent("Min/Max Length", "Force every branch to grow within this range. (Default: 1.0-5.0)");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField( content, GUILayout.MaxWidth(100) );
                ivyProfile.minLength = Mathf.Clamp(EditorGUILayout.FloatField( ivyProfile.minLength, GUILayout.MaxWidth(32) ), 0.01f, ivyProfile.maxLength-0.01f);
                EditorGUILayout.MinMaxSlider(ref ivyProfile.minLength, ref ivyProfile.maxLength, 0.01f, 10f);
                ivyProfile.maxLength = Mathf.Clamp(EditorGUILayout.FloatField( ivyProfile.maxLength, GUILayout.MaxWidth(32) ), ivyProfile.minLength+0.01f, 10f);
                EditorGUILayout.EndHorizontal();

                content = new GUIContent("Max Branches", "How many times each branch can branch.");
                ivyProfile.maxBranchesPerRoot = EditorGUILayout.IntSlider(content, ivyProfile.maxBranchesPerRoot, 1, 16);

                content = new GUIContent("Branch Probability : ", "defines the density of branching structure during growing");
                ivyProfile.branchingProbability = EditorGUILayout.Slider(content, ivyProfile.branchingProbability, 0, 1f);
   
                content = new GUIContent("Max Float Length : ", "defines the length at which a freely floating branch will die");
                ivyProfile.maxFloatLength = EditorGUILayout.Slider(content, ivyProfile.maxFloatLength, 0, 1f);

                content = new GUIContent("Max Adhesion Dist : ", "defines the maximum distance to a surface at which the surface will attract the ivy");
                ivyProfile.maxAdhesionDistance = EditorGUILayout.Slider(content, ivyProfile.maxAdhesionDistance, 0, 1f);

                ivyProfile.collisionMask = EditorGUILayout.MaskField("Collision Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(ivyProfile.collisionMask), InternalEditorUtility.layers);

                GUILayout.Label("Growth Weight Influences", EditorStyles.boldLabel);

                content = new GUIContent("Primary Weight %", "defines the weight of the primary growing direction");
                ivyProfile.primaryWeight = EditorGUILayout.Slider(content, ivyProfile.primaryWeight, 0, 1f);

                content = new GUIContent("Random Weight %", "defines the weight of a random growing direction");
                ivyProfile.randomWeight = EditorGUILayout.Slider(content, ivyProfile.randomWeight, 0, 1f);

                content = new GUIContent("Gravity Weight %", "defines the weight of gravity");
                ivyProfile.gravityWeight = EditorGUILayout.Slider(content, ivyProfile.gravityWeight, 0, 2f);

                content = new GUIContent("Adhesion Weight %", "defines the weight of adhesion towards attracting surfaces");
                ivyProfile.adhesionWeight = EditorGUILayout.Slider(content, ivyProfile.adhesionWeight, 0, 1f);

                GUILayout.Label("Mesh Generation", EditorStyles.boldLabel);

                content = new GUIContent("Ivy Branch Width", "defines the diameter of the branch geometry relative to the ivy size");
                ivyProfile.ivyBranchSize = EditorGUILayout.Slider(content, ivyProfile.ivyBranchSize, 0, 0.25f);
    
                content = new GUIContent("Ivy Leaf Size", "size of leaves, in world units (default: 0.05)");
                ivyProfile.ivyLeafSize = EditorGUILayout.Slider(content, ivyProfile.ivyLeafSize, 0, 1f);

                content = new GUIContent("Leaf Density", "probability of generating leaves");
                ivyProfile.leafProbability = EditorGUILayout.Slider(content, ivyProfile.leafProbability, 0, 1f);

                ivyProfile.branchMaterial = EditorGUILayout.ObjectField("Branch Material", ivyProfile.branchMaterial, typeof(Material), false) as Material;
                ivyProfile.leafMaterial = EditorGUILayout.ObjectField("Leaf Material", ivyProfile.leafMaterial, typeof(Material), false) as Material;
            
                if ( EditorGUI.EndChangeCheck() ) {
                    Undo.RecordObject(ivyBehavior.profileAsset, "Hedera > Edit Ivy Settings" );
                    EditorUtility.SetDirty( ivyBehavior.profileAsset );
                }
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();

            // plant root creation button
            var oldColor = GUI.color;
            GUI.color = isPlantingModeActive ? Color.yellow : oldColor;
            if (GUILayout.Button( !isPlantingModeActive ? "Start Painting Ivy": "Stop Painting Ivy" ) )
            {
                isPlantingModeActive = !isPlantingModeActive;
            }
            GUI.color = oldColor;

            ivyBehavior.generateMeshDuringGrowth = EditorGUILayout.Toggle("Generate Mesh During Growth", ivyBehavior.generateMeshDuringGrowth);

            if (ivyBehavior.ivyGraphs.Where( ivy => ivy.isGrowing ).Count() > 0) {
                if ( GUILayout.Button( "Force-Stop All Growing") ) {
                    IvyCore.ForceStopGrowing();
                }
            }

           // display existing ivy
            for ( int i=0; i< ivyBehavior.ivyGraphs.Count; i++ ) {
                var ivy = ivyBehavior.ivyGraphs[i];
                EditorGUILayout.BeginHorizontal( EditorStyles.helpBox );
                if (GUILayout.Button("x")) {
                    // stop growing and/or delete this ivy
                    if ( ivy.isGrowing ) {
                        ivy.isGrowing = false;
                    } else {
                        if ( ivy.rootGO != null) {
                            DestroyImmediate( ivy.rootGO );
                        }
                        ivyBehavior.ivyGraphs.Remove(ivy);
                        EditorGUILayout.EndHorizontal();
                        i--;
                        continue;
                    }
                }
                if ( ivy.rootGO != null ) {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField( ivy.rootGO, typeof(GameObject), true);
                    GUI.enabled = true;
                }
                string ivyLabel = string.Format(
                    "{1}{2} ivy", 
                    i+1, 
                    ivy.isGrowing ? "[Growing] " : "", 
                    ivy.roots.Count
                );
                GUILayout.Label(ivyLabel);
                
                if ( !ivy.isGrowing ) {
                    if (GUILayout.Button("Make Mesh", EditorStyles.miniButtonLeft))
                    {
                        IvyMesh.GenerateMesh(ivy, ivyProfile);
                        Repaint();
                    }
                    GUI.enabled = ivy.leafMesh != null && ivy.branchMesh != null;
                    if (GUILayout.Button("Save .OBJ", EditorStyles.miniButtonRight))
                    {
                        // TODO: export OBJ
                    }
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }


    }
}