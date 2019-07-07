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
        Editor ivyProfileEditor;

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
                        int branchCount = Mathf.FloorToInt(ivyBehavior.profileAsset.ivyProfile.maxBranchesTotal * branchPercentage * ivyBehavior.profileAsset.ivyProfile.branchingProbability);
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
            EditorGUI.BeginChangeCheck();
            ivyBehavior.profileAsset = EditorGUILayout.ObjectField( ivyBehavior.profileAsset, typeof(IvyProfileAsset), false ) as IvyProfileAsset;
            if ( EditorGUI.EndChangeCheck() || (ivyProfileEditor == null && ivyBehavior.profileAsset != null)) {
                ivyProfileEditor = Editor.CreateEditor( ivyBehavior.profileAsset );
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
                    GUI.enabled = ivy.branchMesh != null;
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

    }
}