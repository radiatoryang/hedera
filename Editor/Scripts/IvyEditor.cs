using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;

namespace Hedera
{
    [CustomEditor(typeof(IvyBehavior))]
    public class IvyEditor : Editor
    {
        private IvyGenerator gen;
        IvyBehavior ivyTarget;

        bool isUsing2DMode, isPlantingModeActive, showGrowingEditor, showBirthEditor;

        Vector3 cursorPos;

        // void OnEnable() {
        //     SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        // }

        // void OnDisable() {
        //     SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        // }

        public void OnSceneGUI()
        {
            //with creation mode enabled, place new root on keypress
            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint || Event.current.button != 0 || !isPlantingModeActive) {
                HandleUtility.Repaint();
                return;
            }

            // HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // ignore other input
             int controlId = GUIUtility.GetControlID(FocusType.Passive);
 
            //cast a ray against mouse position
            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo = new RaycastHit();
            Vector3 pos = Vector3.zero;
            
            if (Physics.Raycast(worldRay, out hitInfo)) {
                pos = hitInfo.point;
                ivyTarget.cursorPos = pos;
            }

            // preview in scene view
            // DrawWireCube( pos, Vector3.one * 0.5f, Color.yellow);
            // Debug.DrawRay( pos, hitInfo.normal * 0.5f, Color.yellow, 0.01f);

            // OnMouseUp because object selection happens on mouse up, so we should use it up
            if ( Event.current.type == EventType.MouseUp ) {
                isPlantingModeActive = false;
                var newIvy = new IvyGraph();

                PlaceRoot(newIvy, pos);
                gen.ivyGraphs.Add( newIvy );

                Debug.Log("new ivy at " + newIvy.seedPos.ToString() );
                HandleUtility.Repaint();
                Event.current.Use();
                HandleUtility.Repaint();
                GUIUtility.hotControl = -1;
            } else {
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
        }

        //called whenever the inspector gui gets rendered
        public override void OnInspectorGUI()
        {
            ivyTarget = (IvyBehavior)target;

            //get manager reference
            gen = EditorGUILayout.ObjectField( gen, typeof(IvyGenerator), false ) as IvyGenerator;

            if ( gen == null) {
                return;
            }



            if ( !IvyCore.ivyGenerators.Contains(gen) ) {
                IvyCore.ivyGenerators.Add(gen);
            }

            //get sceneview to auto-detect 2D mode
            SceneView view = GetSceneView();
            isUsing2DMode = view.in2DMode;

            //plant root creation button
            if (GUILayout.Button("Plant New Ivy", GUILayout.Height(40)))
            {
                //focus sceneview for placement
                view.Focus();
                isPlantingModeActive = true;
            }

            // display existing ivy
            for ( int i=0; i< gen.ivyGraphs.Count; i++ ) {
                var ivy = gen.ivyGraphs[i];
                EditorGUILayout.BeginHorizontal( EditorStyles.helpBox );
                if (GUILayout.Button("x")) {
                    // stop growing and/or delete this ivy
                    gen.ivyGraphs.Remove(ivy);
                    EditorGUILayout.EndHorizontal();
                    i--;
                    continue;
                }
                EditorGUILayout.LabelField(ivy.seedPos.ToString(), ivy.isGrowing ? "GROWING!" : "DONE" );
                if (GUILayout.Button("Instantiate"))
                {
                    gen.GenerateMesh(ivy);
                    Repaint();
                }
                if (GUILayout.Button("Export OBJ"))
                {
                    // TODO: export OBJ
                }
                EditorGUILayout.EndHorizontal();
            }

            //plant instructions
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Hint:\nPress 'Start Plant' to begin a new ivy root, then press"
                            + " 'p' on your keyboard to place new root in the SceneView. "
                            + "In 3D Mode you have to place root onto objects with colliders."
                            + " You can only place one root at the scene view position.", MessageType.Info);
            EditorGUILayout.Space();

            showGrowingEditor = EditorGUILayout.Foldout(showGrowingEditor, "Growth Settings");
            if (EditorGUILayout.BeginFadeGroup(showGrowingEditor ? 1 : 0))
            {
                GUIContent content = null;
                EditorGUILayout.Space();
                
                content = new GUIContent("Ivy Size : ", "adapts the ivy growing and geometry to the scene size and content");
                gen.ivyProfile.ivySize = EditorGUILayout.Slider(content, gen.ivyProfile.ivySize, 0, 0.05f);

                content = new GUIContent("Primary Weight : ", "defines the weight of the primary growing direction");
                gen.ivyProfile.primaryWeight = EditorGUILayout.Slider(content, gen.ivyProfile.primaryWeight, 0, 1f);

                content = new GUIContent("Random Weight : ", "defines the weight of a random growing direction");
                gen.ivyProfile.randomWeight = EditorGUILayout.Slider(content, gen.ivyProfile.randomWeight, 0, 1f);

                content = new GUIContent("Gravity Weight : ", "defines the weight of gravity");
                gen.ivyProfile.gravityWeight = EditorGUILayout.Slider(content, gen.ivyProfile.gravityWeight, 0, 2f);

                content = new GUIContent("Adhesion Weight : ", "defines the weight of adhesion towards attracting surfaces");
                gen.ivyProfile.adhesionWeight = EditorGUILayout.Slider(content, gen.ivyProfile.adhesionWeight, 0, 1f);

                content = new GUIContent("Branch Probability : ", "defines the density of branching structure during growing");
                gen.ivyProfile.branchingProbability = EditorGUILayout.Slider(content, gen.ivyProfile.branchingProbability, 0, 1f);
   
                content = new GUIContent("Max Float Length : ", "defines the length at which a freely floating branch will die");
                gen.ivyProfile.maxFloatLength = EditorGUILayout.Slider(content, gen.ivyProfile.maxFloatLength, 0, 1f);

                content = new GUIContent("Max Adhesion Dist : ", "defines the maximum distance to a surface at which the surface will attract the ivy");
                gen.ivyProfile.maxAdhesionDistance = EditorGUILayout.Slider(content, gen.ivyProfile.maxAdhesionDistance, 0, 1f);

                gen.ivyProfile.collisionMask = EditorGUILayout.MaskField("Collision Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(gen.ivyProfile.collisionMask), InternalEditorUtility.layers);
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.Separator();

            showBirthEditor = EditorGUILayout.Foldout(showBirthEditor, "Mesh Settings");
            if (EditorGUILayout.BeginFadeGroup(showBirthEditor ? 1 : 0))
            {
                GUIContent content = null;
                EditorGUILayout.Space();

                content = new GUIContent("Ivy Branch Diameter", "defines the diameter of the branch geometry relative to the ivy size");
                gen.ivyProfile.ivyBranchSize = EditorGUILayout.Slider(content, gen.ivyProfile.ivyBranchSize, 0, 0.5f);
    
                content = new GUIContent("Ivy Leaf Size", "defines the diameter of the leaf geometry relative to the ivy size");
                gen.ivyProfile.ivyLeafSize = EditorGUILayout.Slider(content, gen.ivyProfile.ivyLeafSize, 0, 2f);

                content = new GUIContent("Leaf Density", "defines the density of the leaves during geometry generation");
                gen.ivyProfile.leafProbability = EditorGUILayout.Slider(content, gen.ivyProfile.leafProbability, 0, 1f);

                EditorGUILayout.Space();

                gen.ivyProfile.branchMaterial = EditorGUILayout.ObjectField("Branch Material", gen.ivyProfile.branchMaterial, typeof(Material), false) as Material;
                gen.ivyProfile.leafMaterial = EditorGUILayout.ObjectField("Leaf Material", gen.ivyProfile.leafMaterial, typeof(Material), false) as Material;
            }
            EditorGUILayout.EndFadeGroup();
        }

        public static SceneView GetSceneView() {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
                view = EditorWindow.GetWindow<SceneView>();

            return view;
        }

        private void PlaceRoot(IvyGraph ivyGraph, Vector3 placePos) {
            ivyGraph.SeedRoot(placePos);
            Repaint();
        }


    }
}