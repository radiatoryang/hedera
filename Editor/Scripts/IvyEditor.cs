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
        IvyGraph currentIvyGraph;

        bool isPlantingModeActive;

        private Vector3 lastPos, mousePos, mouseNormal, mouseDirection;
        double lastEditorTime, deltaTime;
        // private Quaternion mouseRot;

        Texture iconVisOn, iconVisOff, iconLeaf, iconMesh, iconExport, iconTrash, iconPaint;

        void OnEnable() {
            iconVisOn = EditorGUIUtility.IconContent("animationvisibilitytoggleon").image;
            iconVisOff = EditorGUIUtility.IconContent("animationvisibilitytoggleoff").image;
            iconLeaf = EditorGUIUtility.IconContent("tree_icon_leaf").image;
            iconMesh = EditorGUIUtility.IconContent("MeshRenderer Icon").image;
            iconExport = EditorGUIUtility.IconContent("PrefabModel Icon").image;
            iconTrash = EditorGUIUtility.IconContent("TreeEditor.Trash").image;
            iconPaint = EditorGUIUtility.IconContent("ClothInspector.PaintTool").image;
        }

        // got working painter code from https://github.com/marmitoTH/Unity-Prefab-Placement-Editor
        private void OnSceneGUI()
        {
            if ( ivyBehavior == null) {
                ivyBehavior = (IvyBehavior)target;
            }

            foreach ( var graph in ivyBehavior.ivyGraphs) {
                if ( graph.isVisible && (graph.branchMF == null || graph.branchR == null || graph.branchMesh == null) ) {
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
                    if (current.type == EventType.MouseDown) {
                        ivyBehavior.transform.localScale = Vector3.one;
                        Undo.SetCurrentGroupName( "Hedera > Paint Ivy");
                        Undo.RegisterCompleteObjectUndo( ivyBehavior, "Hedera > Paint Ivy" );
                        currentIvyGraph = IvyCore.SeedNewIvyGraph(ivyBehavior.profileAsset.ivyProfile, lastPos, Vector3.up, -mouseNormal, ivyBehavior.transform, ivyBehavior.generateMeshDuringGrowth);
                        currentIvyGraph.isGrowing = false;
                        ivyBehavior.ivyGraphs.Add( currentIvyGraph );
                    } else {
                        IvyCore.ForceIvyGrowth( currentIvyGraph, ivyBehavior.profileAsset.ivyProfile, lastPos, mouseNormal );
                    }
                } 
                // else if (current.button == 0 && current.shift)
                // { // erase 
                //     lastPos = mousePos;
                //     if (current.type == EventType.MouseDown) {
                //         Undo.SetCurrentGroupName( "Hedera > Erase Ivy");
                //     } else {

                //     }
                // }
            }

            if (current.type == EventType.MouseUp) {
                ivyBehavior.transform.localScale = Vector3.one;
                lastPos = Vector3.zero;
                if ( currentIvyGraph != null) {
                    currentIvyGraph.isGrowing = ivyBehavior.enableGrowthSim;
                    if ( currentIvyGraph.isGrowing ) {
                        float branchPercentage = Mathf.Clamp(currentIvyGraph.roots[0].nodes.Last().lengthCumulative / ivyBehavior.profileAsset.ivyProfile.maxLength, 0f, 0.38f);
                        int branchCount = Mathf.CeilToInt(ivyBehavior.profileAsset.ivyProfile.maxBranchesTotal * branchPercentage * Mathf.Pow(ivyBehavior.profileAsset.ivyProfile.branchingProbability, 2f));
                        for( int b=0; b<branchCount; b++) {
                            IvyCore.ForceRandomIvyBranch( currentIvyGraph, ivyBehavior.profileAsset.ivyProfile );
                        }
                    }
                    currentIvyGraph = null;
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            SceneView.RepaintAll();
        }

        public bool CanDraw ()
        { 
            float dist = Vector3.Distance(mousePos, lastPos);

            if (dist >= Mathf.Max(0.05f, ivyBehavior.profileAsset.ivyProfile.ivyStepDistance) )
                return true;
            else
                return false;
        }

        public void MousePosition ()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, ivyBehavior.profileAsset.ivyProfile.collisionMask)) 
            {
                mousePos = hit.point + hit.normal * 0.05f;
                mouseNormal = hit.normal;
                // mouseRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Handles.color = Color.blue;
                Handles.DrawWireDisc(mousePos, hit.normal, Mathf.Max(0.1f, ivyBehavior.profileAsset.ivyProfile.ivyStepDistance) - 0.01f );
                Handles.DrawWireDisc(mousePos, hit.normal, Mathf.Max(0.1f, ivyBehavior.profileAsset.ivyProfile.ivyStepDistance) );
                Handles.DrawWireDisc(mousePos, hit.normal, Mathf.Max(0.1f, ivyBehavior.profileAsset.ivyProfile.ivyStepDistance) + 0.01f );
                Handles.DrawLine(mousePos, mousePos + hit.normal * 0.25f);
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

            
            GUIContent content = null;
            EditorGUI.indentLevel++;
            ivyBehavior.showProfileFoldout = EditorGUILayout.Foldout(ivyBehavior.showProfileFoldout, "Ivy Profile Settings", true);
            EditorGUI.indentLevel--;
            if (EditorGUILayout.BeginFadeGroup(ivyBehavior.showProfileFoldout ? 1 : 0))
            {
                EditorGUILayout.HelpBox("Hover over each label to learn more.\nIf you mess up, click Reset To Defaults.", MessageType.Info);

                if ( GUILayout.Button("Reset to Defaults", EditorStyles.miniButton) ) {
                    if ( EditorUtility.DisplayDialog("Hedera: Reset Ivy Profile to Default Settings", "Are you sure you want to reset this ivy profile back to default settings?", "Yes, reset!", "Cancel") )
                    {
                        Undo.RegisterCompleteObjectUndo(ivyBehavior.profileAsset, "Hedera > Reset Settings" );
                        ivyProfile.ResetSettings();
                        EditorUtility.SetDirty( ivyBehavior.profileAsset );
                    }
                }

                EditorGUILayout.Separator();
                GUI.changed = false;
                EditorGUI.BeginChangeCheck();

                EditorGUI.indentLevel++;
                content = new GUIContent("Growth Sim", "When you paint ivy, it will try to grow on nearby surfaces. These settings control how much it should grow automatically.");
                ivyBehavior.showGrowthFoldout = EditorGUILayout.Foldout(ivyBehavior.showGrowthFoldout, content, true);

                if ( ivyBehavior.showGrowthFoldout ) {
                    content = new GUIContent("Ivy Step Distance", "How far ivy tries to move per frame. (default: 0.1)\nSmaller = more curvy, smoother, more polygons.\nLarger = more angular and fewer polygons.");
                    ivyProfile.ivyStepDistance = EditorGUILayout.Slider(content, ivyProfile.ivyStepDistance, 0.01f, 0.5f);

                    content = new GUIContent("Length", "Force branches to grow within this min / max range.\n(default: 0.5-5.0)");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( content, GUILayout.MaxWidth(60) );
                    EditorGUI.indentLevel--;
                    ivyProfile.minLength = Mathf.Clamp(EditorGUILayout.DelayedFloatField( ivyProfile.minLength, GUILayout.MaxWidth(28) ), 0.01f, ivyProfile.maxLength-0.01f);
                    EditorGUILayout.MinMaxSlider(ref ivyProfile.minLength, ref ivyProfile.maxLength, 0.01f, 10f);
                    ivyProfile.maxLength = Mathf.Clamp(EditorGUILayout.DelayedFloatField( ivyProfile.maxLength, GUILayout.MaxWidth(28) ), ivyProfile.minLength+0.01f, 10f);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.EndHorizontal();

                    content = new GUIContent("Branch Limit", "Maximum branches total per plant. (Hint: If you want large plants to go specific places, then paint multiple plants and merge them later.)\n(default: 64)");
                    ivyProfile.maxBranchesTotal = EditorGUILayout.IntSlider(content, ivyProfile.maxBranchesTotal, 1, 128);

                    // content = new GUIContent("Branch Per Branch", "How many times a branch can branch.\n(default: 1)");
                    // ivyProfile.maxBranchesPerRoot = EditorGUILayout.IntSlider(content, ivyProfile.maxBranchesPerRoot, 0, 8);

                    content = new GUIContent("Branch Probability", "Higher values = many more branches. At 100%, almost all plants will reach their Branch Limit.\n(default: 10%");
                    ivyProfile.branchingProbability = EditorGUILayout.Slider(content, ivyProfile.branchingProbability, 0f, 1f);
    
                    content = new GUIContent("Max Float Length", "How far ivy can 'float' with no surface to cling on to.\n(default: 1)");
                    ivyProfile.maxFloatLength = EditorGUILayout.Slider(content, ivyProfile.maxFloatLength, 0.001f, 2f);

                    content = new GUIContent("Max Cling Distance", "How far ivy can detect surfaces to cling on to. Larger values make ivy 'smarter', but simulation will be slower and more expensive.\n(default: 1.0)");
                    ivyProfile.maxAdhesionDistance = EditorGUILayout.Slider(content, ivyProfile.maxAdhesionDistance, 0.01f, 2f);

                    content = new GUIContent("Collision Mask", "Which layers the ivy should collide with / cling to. Also determines which collision layers you can paint on.\n(default: Everything except Ignore Raycast)");
                    ivyProfile.collisionMask = EditorGUILayout.MaskField(content, InternalEditorUtility.LayerMaskToConcatenatedLayersMask(ivyProfile.collisionMask), InternalEditorUtility.layers);

                    if ( ivyProfile.collisionMask == 0) {
                        EditorGUILayout.HelpBox("Collision Mask shouldn't be Nothing. That means you can't paint on anything, and ivy can't cling or climb.", MessageType.Warning);
                    }
                }

                // =========

                content = new GUIContent("Growth AI", "When you simulate ivy, it will calculate growth directions using these influence settings.");
                ivyBehavior.showAIFoldout = EditorGUILayout.Foldout(ivyBehavior.showAIFoldout, content, true);

                if ( ivyBehavior.showAIFoldout ) {
                    content = new GUIContent("Plant Follow %", "How much to maintain current path and grow upwards.\n(default: 50%)");
                    ivyProfile.primaryWeight = EditorGUILayout.Slider(content, ivyProfile.primaryWeight, 0, 2f);

                    content = new GUIContent("Random Spread %", "How much to randomly roam outwards in different directions. At 100%, ivy will spread out to cover more of an area.\n(default: 50%)");
                    ivyProfile.randomWeight = EditorGUILayout.Slider(content, ivyProfile.randomWeight, 0, 2f);

                    content = new GUIContent("Gravity Weight %", "How much gravity should pull down floating branches.\n(default: 300%)");
                    ivyProfile.gravityWeight = EditorGUILayout.Slider(content, ivyProfile.gravityWeight, 0, 5f);

                    content = new GUIContent("Surface Cling %", "How much to cling / adhere to nearby surfaces.\n(default: 100%)");
                    ivyProfile.adhesionWeight = EditorGUILayout.Slider(content, ivyProfile.adhesionWeight, 0, 2f);
                }

                // =========

                content = new GUIContent("3D Mesh Settings", "After ivy is done growing, you can make a 3D mesh model based on its path. These settings control how the ivy 3D model will look.");
                ivyBehavior.showMeshFoldout = EditorGUILayout.Foldout(ivyBehavior.showMeshFoldout, content, true);

                if ( ivyBehavior.showMeshFoldout ) {
                    content = new GUIContent("Name Format", "All 3D ivy object names use this template, with numbered placeholders to fill-in data.\n{0}: branch count\n{1}: seed position\n(default: 'Ivy[{0}]{1}')");
                    ivyProfile.namePrefix = EditorGUILayout.DelayedTextField(content, ivyProfile.namePrefix);

                    content = new GUIContent("Batching Static", "Set ivy meshes to 'batch' draw calls for huge performance gains. Always enable this unless you're moving / rotating / scaling the ivy during the game.\n(default: true)");
                    ivyProfile.markMeshAsStatic = EditorGUILayout.Toggle( content, ivyProfile.markMeshAsStatic );

                    content = new GUIContent("Lighting Static", "Set ivy meshes to use lightmapping AND generate lightmap UV2s for the ivy.\n- Make sure your lightmap luxel resolution is high enough, or else it'll probably look very spotty.\n- Also make sure your lightmap atlas size is big enough, or else the lightmapped ivy won't batch.\n(default: false)");
                    ivyProfile.useLightmapping = EditorGUILayout.Toggle( content, ivyProfile.useLightmapping );

                    content = new GUIContent("Branch Thickness", "Width of the branch meshes in world units. (default: 0.05)");
                    ivyProfile.ivyBranchSize = EditorGUILayout.Slider(content, ivyProfile.ivyBranchSize, 0.01f, 0.5f);

                    content = new GUIContent("Branch Smooth", "How many Catmull-Rom spline subdivisions to add to each branch to smooth out the line. For example, a value of 2 doubles your branch vert count. Make sure you use Branch Optimize with this! A value of 1 means don't do any smoothing.\n(default: 2)");
                    ivyProfile.branchSmooth = EditorGUILayout.IntSlider(content, ivyProfile.branchSmooth, 1, 4);

                    content = new GUIContent("Branch Optimize %", "How much to simplify branches. High percentages will lower verts and polycounts for branches, but might look jagged or lose small details. 0% means no optimization will happen.\n(default: 60%)");
                    ivyProfile.branchOptimize = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.branchOptimize * 100f), 0, 100) * 0.01f;

                    content = new GUIContent("Leaf Size", "Size of leaves in world units. Smaller leaves = more leaves (to maintain the same % of coverage) which means more polygons, so bigger leaves are usually better for performance.\n(default: 0.15)");
                    ivyProfile.ivyLeafSize = EditorGUILayout.Slider(content, ivyProfile.ivyLeafSize, 0.05f, 1f);

                    if ( ivyProfile.ivyLeafSize == 0f ) {
                        EditorGUILayout.HelpBox("No leaf mesh when Leaf Size = 0!", MessageType.Warning);
                    }

                    content = new GUIContent("Leaf Density %", "How many leaves on each branch. 0% means no leaves, 100% is very bushy.\n(default: 50%)");
                    ivyProfile.leafProbability = EditorGUILayout.Slider(content, ivyProfile.leafProbability, 0, 1f);

                    if ( ivyProfile.leafProbability == 0f ) {
                        EditorGUILayout.HelpBox("No leaf mesh when Leaf Density = 0!", MessageType.Warning);
                    }

                    content = new GUIContent("Leaf Sunlight %", "Approximates how ivy wants to be in the sun. More leaves on floors / roofs, fewer leaves on ceilings. 0% means leaves will spawn evenly regardless of surface.\n(default: 100%)");
                    ivyProfile.leafSunlightBonus = EditorGUILayout.Slider(content, ivyProfile.leafSunlightBonus, 0, 1f);

                    content = new GUIContent("Branch Material", "Unity material to use for branches. It doesn't need to be very high resolution or detailed, unless you set your Branch Width to be very thick. Example materials can be found in /Hedera/Runtime/Materials/");
                    ivyProfile.branchMaterial = EditorGUILayout.ObjectField(content, ivyProfile.branchMaterial, typeof(Material), false) as Material;
                    
                    if ( ivyProfile.branchMaterial == null) {
                        EditorGUILayout.HelpBox("Branch Material is undefined. Branch meshes will use default material.", MessageType.Warning);
                    }
                    
                    content = new GUIContent("Leaf Material", "Unity material to use for leaves. Example materials are in /Hedera/Runtime/Materials/");
                    ivyProfile.leafMaterial = EditorGUILayout.ObjectField(content, ivyProfile.leafMaterial, typeof(Material), false) as Material;

                    if ( ivyProfile.leafMaterial == null) {
                        EditorGUILayout.HelpBox("Leaf Material is undefined. Leaf meshes will use default material.", MessageType.Warning);
                    }
                }

                if ( EditorGUI.EndChangeCheck() ) {
                    Undo.RegisterCompleteObjectUndo(ivyBehavior.profileAsset, "Hedera > Edit Ivy Settings" );
                    EditorUtility.SetDirty( ivyBehavior.profileAsset );
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();

            GUILayout.Label("Ivy Painter", EditorStyles.boldLabel);

            // plant root creation button
            var oldColor = GUI.color;
            GUI.color = isPlantingModeActive ? Color.yellow : Color.Lerp(Color.yellow, oldColor, 0.69f);
            content = new GUIContent(!isPlantingModeActive ? "  Start Painting Ivy": "  Stop Painting Ivy", iconPaint, "while painting, left-click and drag in the Scene view on any collider");
            if (GUILayout.Button( content, GUILayout.Height(20) ) )
            {
                isPlantingModeActive = !isPlantingModeActive;
            }
            GUI.color = oldColor;

            content = new GUIContent( " Enable Growth Sim AI", "If disabled, then you can just paint ivy without simulation or AI, which is useful when you want small strokes or full control." );
            ivyBehavior.enableGrowthSim = EditorGUILayout.ToggleLeft(content, ivyBehavior.enableGrowthSim);

            content = new GUIContent( " Make Mesh During Painting / Growth", "Generate 3D ivy mesh during painting and growth. Very cool, but very processing intensive. If your computer gets very slow while painting, then disable this." );
            ivyBehavior.generateMeshDuringGrowth = EditorGUILayout.ToggleLeft(content, ivyBehavior.generateMeshDuringGrowth);

            int visibleIvy = ivyBehavior.ivyGraphs.Where( ivy => ivy.rootGO != null && ivy.isVisible ).Count();
            GUI.enabled = visibleIvy > 0;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            content = new GUIContent(" Re-mesh Visible", iconMesh, "Remake meshes for all visible ivy, all at once. Useful when you change your ivy profile settings, and want to see the new changes.");
            if ( GUILayout.Button(content, EditorStyles.miniButtonLeft, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.45f), GUILayout.Height(16)) ) {
                if ( EditorUtility.DisplayDialog("Hedera: Remake All Visible Meshes", string.Format("Are you sure you want to remake {0} meshes all at once? It also might be very slow or crash your editor.", visibleIvy), "YES!", "Maybe not...")) {
                    foreach ( var ivy in ivyBehavior.ivyGraphs ) {
                        if ( !ivy.isVisible) {
                            continue;
                        }
                        Undo.RegisterFullObjectHierarchyUndo( ivy.rootGO, "Hedera > Re-mesh Visible" );
                        IvyMesh.GenerateMesh(ivy, ivyProfile, ivyProfile.useLightmapping, true);
                    }
                }
            }

            content = new GUIContent(" Merge Visible", iconLeaf, "Merge all visible ivy into a single ivy / single mesh. This is (usually) good for optimizing the 3D performance of your scene, especially if you have a lot of ivy everywhere.");
            if ( GUILayout.Button(content, EditorStyles.miniButtonRight, GUILayout.Height(16)) ) {
                if ( EditorUtility.DisplayDialog("Hedera: Merge All Visible Ivy Strokes", string.Format("Are you sure you want to merge {0} ivy plants into one?", visibleIvy), "YES!", "Maybe not...")) {
                    Undo.RegisterCompleteObjectUndo( ivyBehavior, "Hedera > Merge Visible" );
                    Undo.SetCurrentGroupName( "Hedera > Merge Visible" );

                    //var rootIvyB = IvyCore.StartDestructiveEdit(ivyBehavior, applyAllOverrides:true );
                    IvyCore.MergeVisibleIvyGraphs(ivyBehavior, ivyProfile );
                    // IvyCore.CommitDestructiveEdit();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            if ( ivyBehavior.ivyGraphs.Count == 0) {
                EditorGUILayout.HelpBox("To paint Ivy, first click [Start Painting Ivy]... then hold down [Left Mouse Button] on a collider in the Scene view, and drag.", MessageType.Info);
            }

            var oldBGColor = GUI.backgroundColor;
            var pulseColor = Color.Lerp( oldBGColor, Color.yellow, Mathf.PingPong( System.Convert.ToSingle(EditorApplication.timeSinceStartup) * 2f, 1f ) );
            for ( int i=0; i< ivyBehavior.ivyGraphs.Count; i++ ) {
                var ivy = ivyBehavior.ivyGraphs[i];
                if ( ivy.isGrowing ) {
                    GUI.backgroundColor = pulseColor;
                }
                EditorGUILayout.BeginHorizontal( EditorStyles.helpBox );
                GUI.backgroundColor = oldBGColor;

                GUI.color = ivy.isVisible ? oldColor : Color.gray;
                var eyeIcon = ivy.isVisible ? iconVisOn : iconVisOff;
                content = new GUIContent( eyeIcon, "Click to toggle visibility for this ivy plant.\n(Enable / disable the game object.)");
                if ( GUILayout.Button(content, GUILayout.Width(24) )) {
                    ivy.isVisible = !ivy.isVisible;
                    ivy.rootGO.SetActive( ivy.isVisible );
                }
                GUI.color = oldColor;

                if ( ivy.rootGO != null ) {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField( ivy.rootGO, typeof(GameObject), true);
                    GUI.enabled = true;
                } else {
                    string ivyLabel = string.Format(
                        "(no mesh) {0} ivy", 
                        ivy.roots.Count,
                        ivy.seedPos
                    );
                    GUILayout.Label(ivyLabel, EditorStyles.miniLabel);
                }
                
                if ( !ivy.isGrowing ) {
                    content = new GUIContent( iconMesh, "Make (or remake) the 3D mesh for this ivy");
                    if (GUILayout.Button(content, EditorStyles.miniButtonLeft, GUILayout.Width(24), GUILayout.Height(16)))
                    {
                        if ( ivy.rootGO != null ) {
                            Undo.RegisterFullObjectHierarchyUndo( ivy.rootGO, "Hedera > Make Mesh" );
                        } else {
                            IvyMesh.InitOrRefreshRoot( ivy, ivyProfile );
                            Undo.RegisterCreatedObjectUndo( ivy.rootGO, "Hedera > Make Mesh");
                        }
                        IvyMesh.GenerateMesh(ivy, ivyProfile, ivyProfile.useLightmapping, true);
                        Repaint();
                    }
                    GUI.enabled = ivy.leafMesh != null && ivy.branchMesh != null;
                    content = new GUIContent( "OBJ", iconExport, "Export ivy mesh to .OBJ file\n(Note: .OBJs only support one UV channel so they cannot have lightmap UVs, Unity must unwrap them upon import)");
                    if (GUILayout.Button(content, EditorStyles.miniButtonMid, GUILayout.Width(24), GUILayout.Height(16)))
                    {
                        ObjExport.SaveObjFile( new GameObject[] { ivy.rootGO }, false, true );
                    }
                    GUI.enabled = true;
                    content = new GUIContent( iconTrash, "Delete this ivy as well as its mesh objects.");
                    if (GUILayout.Button(content, EditorStyles.miniButtonRight, GUILayout.Width(24), GUILayout.Height(16))) {
                        if ( ivy.rootGO != null) {
                            IvyCore.DestroyObject( ivy.rootGO );
                        }
                        Undo.RegisterCompleteObjectUndo( ivyBehavior, "Hedera > Delete Ivy" );
                        ivyBehavior.ivyGraphs.Remove(ivy);
                        EditorGUILayout.EndHorizontal();
                        i--;
                        continue;
                    }
                } else {
                    if (GUILayout.Button("Stop Growing", EditorStyles.miniButton)) {
                        ivy.isGrowing = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (ivyBehavior.ivyGraphs.Where( ivy => ivy.isGrowing ).Count() > 0 ) {
                EditorGUILayout.Space();
                GUI.color = pulseColor;
                if ( GUILayout.Button( "Force-Stop All Growing") ) {
                    IvyCore.ForceStopGrowing();
                }
                GUI.color = oldColor;
            }
            
            EditorGUILayout.Space();
        }


    }
}