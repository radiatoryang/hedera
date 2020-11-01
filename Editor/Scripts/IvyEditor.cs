using UnityEngine;
using UnityEngine.SceneManagement;
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
        IvyGraph currentIvyGraph, currentIvyGraphMove;
        Editor ivyProfileEditor;

        bool isPlantingModeActive;

        bool wasPartOfPrefab;
        List<long> lastMeshIDs = new List<long>();
        IvyDataAsset lastDataAsset;

        private Vector3 lastPos, mousePos, mouseNormal, mouseDirection;
        double lastEditorTime, deltaTime;
        // private Quaternion mouseRot;

        Texture iconVisOn, iconVisOff, iconLeaf, iconMesh, iconExport, iconTrash, iconPaint, iconMove;

        Tool LastTool = Tool.None;

        void OnEnable() {
            currentIvyGraph = null;
            currentIvyGraphMove = null;
            iconVisOn = EditorGUIUtility.IconContent("animationvisibilitytoggleon").image;
            iconVisOff = EditorGUIUtility.IconContent("animationvisibilitytoggleoff").image;
            iconLeaf = EditorGUIUtility.IconContent("tree_icon_leaf").image;
            iconMesh = EditorGUIUtility.IconContent("MeshRenderer Icon").image;
            iconExport = EditorGUIUtility.IconContent("PrefabModel Icon").image;
            iconTrash = EditorGUIUtility.IconContent("TreeEditor.Trash").image;
            iconPaint = EditorGUIUtility.IconContent("ClothInspector.PaintTool").image;
            iconMove = EditorGUIUtility.IconContent("MoveTool").image;
        }

        void OnDisable() {
            Tools.hidden = false;
        }

        // got working painter code from https://github.com/marmitoTH/Unity-Prefab-Placement-Editor
        private void OnSceneGUI()
        {
            if ( ivyBehavior == null) {
                ivyBehavior = (IvyBehavior)target;
            }

            Handles.color = ivyBehavior.debugColor;
            foreach ( var graph in ivyBehavior.ivyGraphs) {
                if ( graph.isVisible && (graph.branchMF == null || graph.branchR == null || (graph.rootGO != null && Vector3.SqrMagnitude(graph.rootGO.transform.position - graph.seedPos) > 0.001f)) ) {
                    DrawThiccDisc( graph.seedPos, graph.seedNormal, 0.05f );
                    DrawDebugIvy( graph );
                }
            }

            Event current = Event.current;

            // change current editor tool for painting and positioning
            if ( (isPlantingModeActive || currentIvyGraphMove != null) ) {
                if ( Tools.current != Tool.None ) {
                    LastTool = Tools.current;
                    Tools.current = Tool.None;
                    Tools.hidden = true;
                }
            } else if ( LastTool != Tool.None ) {
                Tools.current = LastTool;
                LastTool = Tool.None;
                Tools.hidden = false;
            }

            if ( currentIvyGraphMove != null ) {
                if (current.type == EventType.MouseDrag) {
                    Undo.RegisterCompleteObjectUndo( ivyBehavior, "Hedera > Move Ivy Seed Position" );
                    if ( currentIvyGraphMove.rootGO != null) {
                        Undo.RegisterCompleteObjectUndo( currentIvyGraphMove.rootGO, "Hedera > Move Ivy Seed Position" );
                    }
                }
                isPlantingModeActive = false;
                currentIvyGraphMove.seedPos = Handles.PositionHandle(currentIvyGraphMove.seedPos, Quaternion.identity);
                if ( currentIvyGraphMove.rootGO != null) {
                    currentIvyGraphMove.rootGO.transform.position = currentIvyGraphMove.seedPos;
                }
                Handles.Label( currentIvyGraphMove.seedPos, currentIvyGraphMove.seedPos.ToString() );
            }

            if ( !isPlantingModeActive ) {
                return;
            }


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
                    } else if ( currentIvyGraph != null) {
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
                    if ( currentIvyGraph.isGrowing && currentIvyGraph.roots.Count > 0 ) {
                        float branchPercentage = Mathf.Clamp(currentIvyGraph.roots[0].nodes.Last().cS / ivyBehavior.profileAsset.ivyProfile.maxLength, 0f, 0.38f);
                        int branchCount = Mathf.FloorToInt(ivyBehavior.profileAsset.ivyProfile.maxBranchesTotal * branchPercentage * ivyBehavior.profileAsset.ivyProfile.branchingProbability);
                        for( int b=0; b<branchCount; b++) {
                            IvyCore.ForceRandomIvyBranch( currentIvyGraph, ivyBehavior.profileAsset.ivyProfile );
                        }
                    } else {
                        IvyCore.needToSaveAssets = true;
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

            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, ivyBehavior.profileAsset.ivyProfile.collisionMask, QueryTriggerInteraction.Ignore)) 
            {
                mousePos = hit.point + hit.normal * 0.05f;
                mouseNormal = hit.normal;
                // mouseRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Handles.color = Color.blue;
                DrawThiccDisc(mousePos, hit.normal, Mathf.Max(0.1f, ivyBehavior.profileAsset.ivyProfile.ivyStepDistance) );
                Handles.DrawLine(mousePos, mousePos + hit.normal * 0.25f);
            }
        }

        void DrawThiccDisc(Vector3 mousePos, Vector3 normal, float radius) {
            var originalColor = Handles.color;
            Handles.color = new Color( originalColor.r, originalColor.g, originalColor.b, 0.4f);
            Handles.DrawSolidDisc( mousePos, normal, radius);
            Handles.color = originalColor;
            Handles.DrawWireDisc(mousePos, normal, radius - 0.01f );
            Handles.DrawWireDisc(mousePos, normal, radius );
            Handles.DrawWireDisc(mousePos, normal, radius + 0.01f );
        }

    	public void DrawDebugIvy(IvyGraph graph) {
            // Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            foreach ( var root in graph.roots ) {
                if ( root.nodes.Count < 2) { continue; }
                if ( root.debugLineSegmentsArray == null || root.debugLineSegmentsArray.Length != (root.nodes.Count-1)*2 || Vector3.SqrMagnitude(root.debugLineSegmentsArray[0] - (root.nodes[0].p+graph.seedPos)) > 0.01f ) {
                    IvyCore.RegenerateDebugLines(graph.seedPos, root );
                    // Debug.LogFormat("regenerting {0}", Vector3.SqrMagnitude(root.debugLineSegmentsArray[0] - (root.nodes[0].p+graph.seedPos)) );
                }
                if ( root.debugLineSegmentsArray != null ) {
                    Handles.DrawLines( root.debugLineSegmentsArray );
                }
            }
		}

        //called whenever the inspector gui gets rendered
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            if ( ivyBehavior == null) {
                ivyBehavior = (IvyBehavior)target;
            }
            wasPartOfPrefab = IvyCore.IsPartOfPrefab( ivyBehavior.gameObject );

            bool isInARealScene = !string.IsNullOrEmpty(ivyBehavior.gameObject.scene.path) && ivyBehavior.gameObject.activeInHierarchy;
            if ( isInARealScene ) {
                lastDataAsset = IvyCore.GetDataAsset( ivyBehavior.gameObject );
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUI.BeginChangeCheck();
            ivyBehavior.profileAsset = EditorGUILayout.ObjectField( ivyBehavior.profileAsset, typeof(IvyProfileAsset), false ) as IvyProfileAsset;
            if ( EditorGUI.EndChangeCheck() || (ivyProfileEditor == null && ivyBehavior.profileAsset != null)) {
                ivyProfileEditor = Editor.CreateEditor( ivyBehavior.profileAsset );
                ((IvyProfileEditor)ivyProfileEditor).viewedFromMonobehavior = true;
            }

            // destroy old editor / cleanup
            if ( ivyBehavior.profileAsset == null && ivyProfileEditor != null) {
                DestroyImmediate( ivyProfileEditor );
            }

            if ( ivyBehavior.profileAsset == null || ivyProfileEditor == null) {
                EditorGUILayout.HelpBox("Please assign an Ivy Profile Asset.", MessageType.Warning);
                if ( GUILayout.Button("Create new Ivy Profile Asset...") ) {
                    var newAsset = IvyCore.CreateNewAsset("");
                    if ( newAsset != null) {
                        ivyBehavior.profileAsset = newAsset;
                        ivyBehavior.showProfileFoldout = true;
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
            
            GUIContent content = null;
            EditorGUI.indentLevel++;
            ivyBehavior.showProfileFoldout = EditorGUILayout.Foldout(ivyBehavior.showProfileFoldout, "Ivy Profile Settings", true);
            EditorGUI.indentLevel--;
            if (EditorGUILayout.BeginFadeGroup(ivyBehavior.showProfileFoldout ? 1 : 0))
            {
                ivyProfileEditor.OnInspectorGUI();
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();

            DrawUILine();
            GUILayout.Label("Ivy Painter", EditorStyles.boldLabel);

            if ( !isInARealScene ) {
                EditorGUILayout.HelpBox("Painting / mesh generation only works in saved scenes on active game objects.\n- Save the scene?\n- Put this game object in a saved scene?\n- Make sure it is active?", MessageType.Error);
                GUI.enabled = false;
            }

            // if Gizmos aren't drawn in scene view, then we can't paint anything since OnSceneGUI() is no longer called... but this warning is only supported in Unity 2019.1 or newer
            // see issue: https://github.com/radiatoryang/hedera/issues/6
            #if UNITY_2019_1_OR_NEWER
            if ( SceneView.lastActiveSceneView.drawGizmos == false) {
                GUI.enabled = false;
                EditorGUILayout.HelpBox("Gizmos are disabled in the Scene View, which breaks OnSceneGUI(), so ivy painting is disabled.", MessageType.Error);
            }
            #endif

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

            int visibleIvy = ivyBehavior.ivyGraphs.Where( ivy => ivy.isVisible ).Count();
            GUI.enabled = isInARealScene && visibleIvy > 0;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            content = new GUIContent(" Re-mesh Visible", iconMesh, "Remake meshes for all visible ivy, all at once. Useful when you change your ivy profile settings, and want to see the new changes.");
            if ( GUILayout.Button(content, EditorStyles.miniButtonLeft, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth * 0.45f), GUILayout.Height(16)) ) {
                if ( EditorUtility.DisplayDialog("Hedera: Remake All Visible Meshes", string.Format("Are you sure you want to remake {0} meshes all at once? It also might be very slow or crash your editor.", visibleIvy), "YES!", "Maybe not...")) {
                    foreach ( var ivy in ivyBehavior.ivyGraphs ) {
                        if ( !ivy.isVisible) {
                            continue;
                        }
                        if ( ivy.rootGO != null) {
                            Undo.RegisterFullObjectHierarchyUndo( ivy.rootGO, "Hedera > Re-mesh Visible" );
                        } else {
                            IvyMesh.InitOrRefreshRoot( ivy, ivyProfile );
                            Undo.RegisterCreatedObjectUndo( ivy.rootGO, "Hedera > Re-mesh Visible");
                        }
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
            GUI.enabled = isInARealScene;

            if ( ivyBehavior.ivyGraphs.Count == 0) {
                EditorGUILayout.HelpBox("To paint Ivy, first click [Start Painting Ivy]... then hold down [Left Mouse Button] on a collider in the Scene view, and drag.", MessageType.Info);
            }

            lastMeshIDs.Clear();
            IvyGraph ivyGraphObjJob = null; // used to pull .OBJ export out of the for() loop
            var oldBGColor = GUI.backgroundColor;
            var pulseColor = Color.Lerp( oldBGColor, Color.yellow, Mathf.PingPong( System.Convert.ToSingle(EditorApplication.timeSinceStartup) * 2f, 1f ) );
            for ( int i=0; i< ivyBehavior.ivyGraphs.Count; i++ ) {
                GUI.enabled = isInARealScene;
                var ivy = ivyBehavior.ivyGraphs[i];
                if ( ivy.isGrowing ) {
                    GUI.backgroundColor = pulseColor;
                }
                lastMeshIDs.Add( ivy.leafMeshID );
                lastMeshIDs.Add( ivy.branchMeshID );
                EditorGUILayout.BeginHorizontal( EditorStyles.helpBox );
                GUI.backgroundColor = oldBGColor;

                GUI.color = ivy.isVisible ? oldColor : Color.gray;
                var eyeIcon = ivy.isVisible ? iconVisOn : iconVisOff;
                content = new GUIContent( eyeIcon, "Click to toggle visibility for this ivy plant.\n(Enable / disable the game object.)");
                if ( GUILayout.Button(content, EditorStyles.miniButtonLeft, GUILayout.Height(16), GUILayout.Width(24) )) {
                    ivy.isVisible = !ivy.isVisible;
                    if ( ivy.rootGO != null) {
                        ivy.rootGO.SetActive( ivy.isVisible );
                    }
                }
                GUI.color = oldColor;

                GUI.color = ivy != currentIvyGraphMove ? oldColor : Color.gray;
                content = new GUIContent( iconMove, "Click to start moving the seed position for this ivy plant.");
                if ( GUILayout.Button(content, EditorStyles.miniButtonRight, GUILayout.Height(16), GUILayout.Width(24) ) ) {
                    if ( ivy.rootGO != null) {
                        ivy.rootGO.transform.position = ivy.seedPos;
                    }
                    currentIvyGraphMove = ivy == currentIvyGraphMove ? null : ivy;
                }
                GUI.color = oldColor;

                if ( ivy.rootGO != null ) {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField( ivy.rootGO, typeof(GameObject), true);
                    GUI.enabled = isInARealScene;
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
                    GUI.enabled = ivy.branchMF != null || ivy.leafMF != null;
                    content = new GUIContent( "OBJ", iconExport, "Export ivy mesh to .OBJ file\n(Note: .OBJs only support one UV channel so they cannot have lightmap UVs, Unity must unwrap them upon import)");
                    if (GUILayout.Button(content, EditorStyles.miniButtonMid, GUILayout.Width(24), GUILayout.Height(16)))
                    {
                        ivyGraphObjJob = ivy;
                    }
                    GUI.enabled = isInARealScene;
                    content = new GUIContent( iconTrash, "Delete this ivy as well as its mesh objects.");
                    if (GUILayout.Button(content, EditorStyles.miniButtonRight, GUILayout.Width(24), GUILayout.Height(16))) {
                        if ( ivy.rootGO != null) {
                            IvyCore.DestroyObject( ivy.rootGO );
                        }
                        IvyCore.TryToDestroyMeshes( ivyBehavior, ivy);
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
                if ( GUILayout.Button( "Stop All Growing") ) {
                    IvyCore.ForceStopGrowing();
                }
                GUI.color = oldColor;
            }

            GUI.enabled = true;
            EditorGUILayout.Space();
            content = new GUIContent("Debug Color", "When ivy doesn't have a mesh, Hedera will visualize the ivy structure as a debug wireframe with this color in the Scene view.");
            ivyBehavior.debugColor = EditorGUILayout.ColorField( content, ivyBehavior.debugColor );
            EditorGUILayout.Space();

            // was getting GUI errors doing OBJ export inline, so let's do it outside of the for() loop
            if ( ivyGraphObjJob != null) {
                var filename = ObjExport.SaveObjFile( new GameObject[] { ivyGraphObjJob.rootGO }, true );
                if ( isInARealScene
                    && !string.IsNullOrEmpty(filename) 
                    && filename.StartsWith(Application.dataPath) 
                    && AssetDatabase.IsMainAssetAtPathLoaded("Assets" + filename.Substring( Application.dataPath.Length ))
                ) {
                    int choice = EditorUtility.DisplayDialogComplex("Hedera: Instantiate .OBJ into scene?", "You just exported ivy into a .OBJ into your project.\nDo you want to replace the ivy with the .OBJ?", "Yes, and delete old ivy", "No, don't instantiate", "Yes, and hide old ivy");
                   
                    if ( choice == 0 || choice == 2) {
                        var prefab = AssetDatabase.LoadAssetAtPath<Object>( "Assets" + filename.Substring( Application.dataPath.Length ) );
                        var newObj = (GameObject)PrefabUtility.InstantiatePrefab( prefab );
                        Undo.RegisterCreatedObjectUndo( newObj, "Hedera > Instantiate OBJ" );
                        newObj.transform.SetParent( ivyBehavior.transform );
                        newObj.transform.position = ivyGraphObjJob.seedPos;

                        var renders = newObj.GetComponentsInChildren<Renderer>();
                        renders[0].material = ivyProfile.branchMaterial;
                        if ( renders.Length > 1) { renders[1].material = ivyProfile.leafMaterial; }

                        if ( choice == 0 ) { // remove old ivy
                            if ( ivyGraphObjJob.rootGO != null) {
                                IvyCore.DestroyObject( ivyGraphObjJob.rootGO );
                            }
                            IvyCore.TryToDestroyMeshes( ivyBehavior, ivyGraphObjJob);
                            Undo.RegisterCompleteObjectUndo( ivyBehavior, "Hedera > Instantiate OBJ" );
                            ivyBehavior.ivyGraphs.Remove(ivyGraphObjJob);
                        } else { // just turn off old ivy
                            Undo.RegisterCompleteObjectUndo( ivyBehavior, "Hedera > Instantiate OBJ" );
                            ivyGraphObjJob.isVisible = false;
                            if ( ivyGraphObjJob.rootGO != null) {
                                ivyGraphObjJob.rootGO.SetActive( false );
                            }
                        }
                    }
                }
            }

        }

        public static void DrawUILine(Color color = default(Color), int thickness = 1, int padding = 4)
        {
            if ( color == default(Color)) {
                color = Color.white * 0.68f;
            }
            // Rect r = GUILayoutUtility.GetLastRect();
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding+thickness));
            r.height = thickness;
            r.y+=padding/2;
            r.x-=2;
            r.width +=6;
            // EditorGUILayout.LabelField("", GUILayout.Height(thickness+padding));
            EditorGUI.DrawRect(r, color);
        }

        // does this actually work? I feel like it doesn't
        public void OnDestroy()
        {
            if ( Application.isEditor )
            {
                if( (IvyBehavior)target == null) {
                    // clean up meshes as well
                    if ( wasPartOfPrefab && !IvyCore.ConfirmDestroyMeshes() ) {
                        return;
                    }

                    if ( lastDataAsset != null ) {
                        foreach ( var meshID in lastMeshIDs ) {
                            IvyCore.TryDestroyMesh( meshID, lastDataAsset, false );
                        }
                        IvyCore.needToSaveAssets = true;
                    }

                }
            }
        }


    }
}