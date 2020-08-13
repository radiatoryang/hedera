using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Hedera {
    [CustomEditor(typeof(IvyProfileAsset))]
    public class IvyProfileEditor : Editor
    {
        public bool viewedFromMonobehavior = false;
        public override void OnInspectorGUI () {
            var profileAsset = (IvyProfileAsset)target;
            var ivyProfile = profileAsset.ivyProfile;

            var content = new GUIContent();
            if( !viewedFromMonobehavior ) {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("This is an Ivy Profile, it controls how your ivy will look. But to paint anything, you must use an Ivy Behavior on a game object.", MessageType.Info);
                if ( GUILayout.Button("Create new Ivy Game Object in Scene")) {
                    IvyCore.CreateNewIvyGameObject(profileAsset);
                }
                EditorGUILayout.EndVertical();
                IvyEditor.DrawUILine();
            }

            if ( !ivyProfile.showAdvanced ) {
                EditorGUILayout.HelpBox("Hover over each label to learn more.\nIf you mess up, click Reset To Defaults.", MessageType.Info);
            }

            content = new GUIContent( "Reset to Defaults", "This will set almost all ivy settings back to their default settings, which has been tested to work OK.");
            if ( GUILayout.Button(content, EditorStyles.miniButton) ) {
                if ( EditorUtility.DisplayDialog("Hedera: Reset Ivy Profile to Default Settings", "Are you sure you want to reset this ivy profile back to default settings?", "Yes, reset!", "Cancel") )
                {
                    Undo.RegisterCompleteObjectUndo(profileAsset, "Hedera > Reset Settings" );
                    ivyProfile.ResetSettings();
                    ivyProfile.branchMaterial = IvyCore.TryToFindDefaultBranchMaterial();
                    ivyProfile.leafMaterial = IvyCore.TryToFindDefaultLeafMaterial();
                    EditorUtility.SetDirty( profileAsset );
                }
            }
            EditorGUILayout.Space();
            content = new GUIContent(" Show Extra Settings", "If enabled, exposes ALL the ivy settings for customization, which can be ovewhelming at first.\n(default: false)");
            ivyProfile.showAdvanced = EditorGUILayout.ToggleLeft( content, ivyProfile.showAdvanced );

            EditorGUILayout.Space();
            GUI.changed = false;
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            content = new GUIContent("Growth Sim", "When you paint ivy, it will try to grow on nearby surfaces. These settings control how much it should grow automatically.");
            ivyProfile.showGrowthFoldout = EditorGUILayout.Foldout(ivyProfile.showGrowthFoldout, content, true);

            if ( ivyProfile.showGrowthFoldout ) {
                content = new GUIContent("Length", "Force branches to grow within this min / max range.\n(default: 1.0 - 3.0)");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField( content, GUILayout.MaxWidth(60) );
                EditorGUI.indentLevel--;
                ivyProfile.minLength = Mathf.Clamp(EditorGUILayout.DelayedFloatField( ivyProfile.minLength, GUILayout.MaxWidth(28) ), 0.01f, ivyProfile.maxLength-0.01f);
                EditorGUILayout.MinMaxSlider(ref ivyProfile.minLength, ref ivyProfile.maxLength, 0.01f, 10f);
                ivyProfile.maxLength = Mathf.Clamp(EditorGUILayout.DelayedFloatField( ivyProfile.maxLength, GUILayout.MaxWidth(28) ), ivyProfile.minLength+0.01f, 10f);
                EditorGUI.indentLevel++;
                EditorGUILayout.EndHorizontal();

                content = new GUIContent("Branch Chance %", "The probability a branch can make another branch. Higher values = many more branches. At 100%, most plants will reach their Max Branch Limit.\n(default: 25%");
                ivyProfile.branchingProbability = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.branchingProbability * 100f), 0, 100) * 0.01f;

                if ( ivyProfile.showAdvanced ) {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("EXTRA GROW OPTIONS", EditorStyles.miniLabel);

                    content = new GUIContent("Max Branches", "Maximum total branches allowed per plant, basically how much the plant will spread. (Hint: If you want ivy to go specific places, then paint multiple plants and merge them.)\n(default: 64)");
                    ivyProfile.maxBranchesTotal = EditorGUILayout.IntSlider(content, ivyProfile.maxBranchesTotal, 1, 256);

                    content = new GUIContent("Step Distance", "How far ivy tries to move per frame, the simulation resolution. (default: 0.1)\nSmaller = more curvy, smoother, more polygons.\nLarger = more angular and fewer polygons.");
                    ivyProfile.ivyStepDistance = EditorGUILayout.Slider(content, ivyProfile.ivyStepDistance, 0.01f, 0.5f);

                    content = new GUIContent("Float Length", "How far ivy can 'float' with no surface to cling on to.\n(default: 1)");
                    ivyProfile.maxFloatLength = EditorGUILayout.Slider(content, ivyProfile.maxFloatLength, 0.001f, 2f);

                    content = new GUIContent("Cling Distance", "How far ivy can detect surfaces to cling on to. Larger values make ivy 'smarter', but simulation will be slower and more expensive.\n(default: 1.0)");
                    ivyProfile.maxAdhesionDistance = EditorGUILayout.Slider(content, ivyProfile.maxAdhesionDistance, 0.01f, 2f);

                    content = new GUIContent("Collision Mask", "Which layers the ivy should collide with / cling to. Also determines which collision layers you can paint on.\n(default: Everything except Ignore Raycast)");
                    ivyProfile.collisionMask = EditorGUILayout.MaskField(content, InternalEditorUtility.LayerMaskToConcatenatedLayersMask(ivyProfile.collisionMask), InternalEditorUtility.layers);

                    if ( ivyProfile.collisionMask == 0) {
                        EditorGUILayout.HelpBox("Collision Mask shouldn't be Nothing. That means you can't paint on anything, and ivy can't cling or climb.", MessageType.Warning);
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }

            IvyEditor.DrawUILine();
            // =========

            content = new GUIContent("Growth AI", "When you simulate ivy, it will try new growth directions using these influence settings.");
            ivyProfile.showAIFoldout = EditorGUILayout.Foldout(ivyProfile.showAIFoldout, content, true);

            if ( ivyProfile.showAIFoldout ) {
                content = new GUIContent("Random Spread %", "How much to randomly roam outwards in different directions. At 100%, ivy will spread out to cover more of an area.\n(default: 100%)");
                ivyProfile.randomWeight = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.randomWeight * 100f), 0, 400) * 0.01f;

                if ( ivyProfile.showAdvanced ) {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("EXTRA AI OPTIONS", EditorStyles.miniLabel);

                    content = new GUIContent("Plant Follow %", "How much to maintain current path and/or grow upwards toward the sun.\n(default: 50%)");
                    ivyProfile.primaryWeight = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.primaryWeight * 100f), 0, 400) * 0.01f;

                    content = new GUIContent("Gravity Weight %", "How much gravity should pull down floating branches.\n(default: 300%)");
                    ivyProfile.gravityWeight = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.gravityWeight * 100f), 0, 400) * 0.01f;

                    content = new GUIContent("Surface Cling %", "How much to cling / adhere to nearby surfaces.\n(default: 100%)");
                    ivyProfile.adhesionWeight = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.adhesionWeight * 100f), 0, 400) * 0.01f;
                
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }

            IvyEditor.DrawUILine();
            // =========

            content = new GUIContent("3D Mesh Settings", "After ivy is done growing, you can make a 3D mesh model based on its path. These settings control how the ivy 3D model will look.");
            ivyProfile.showMeshFoldout = EditorGUILayout.Foldout(ivyProfile.showMeshFoldout, content, true);

            if ( ivyProfile.showMeshFoldout ) {
                content = new GUIContent("Branch Thickness", "Width (diameter) of the branch meshes in world units. (default: 0.05)");
                ivyProfile.ivyBranchSize = EditorGUILayout.Slider(content, ivyProfile.ivyBranchSize, 0f, 0.5f);

                if ( ivyProfile.ivyBranchSize == 0f ) {
                    EditorGUILayout.HelpBox("No branches when Thickness is 0", MessageType.Warning);
                }

                content = new GUIContent("Leaf Size Radius", "Radial size of leaves in world units. Smaller leaves = more leaves (to maintain the same % of coverage) which means more polygons, so bigger leaves are usually better for performance.\n(default: 0.15)");
                ivyProfile.ivyLeafSize = EditorGUILayout.Slider(content, ivyProfile.ivyLeafSize, 0.05f, 1f);

                content = new GUIContent("Leaf Density %", "How many leaves on each branch. 0% means no leaves, 100% is very bushy.\n(default: 50%)");
                ivyProfile.leafProbability = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.leafProbability * 100f), 0, 100) * 0.01f;

                if ( ivyProfile.leafProbability == 0f ) {
                    EditorGUILayout.HelpBox("No leaves when Leaf Density is 0%", MessageType.Warning);
                }

                content = new GUIContent("Vertex Colors", "Randomize leaf mesh's vertex colors, based on a gradient. Make sure your leaf material's shader supports vertex colors, and the default Hedera foliage shader supports vertex colors. If disabled then no vertex colors will be generated, which saves memory.\n(default: true)");
                ivyProfile.useVertexColors = EditorGUILayout.Toggle(content, ivyProfile.useVertexColors );
                if ( ivyProfile.useVertexColors ) {
                    content = new GUIContent("Leaf Colors", "Leaves will pick random vertex colors anywhere along this gradient.\n(default: white > green > yellow)");
                    #if UNITY_2017_1_OR_NEWER
                    EditorGUI.indentLevel++;
                    ivyProfile.leafVertexColors = EditorGUILayout.GradientField( content, ivyProfile.leafVertexColors );
                    EditorGUI.indentLevel--; 
                    #else
                    EditorGUILayout.HelpBox("Can't display gradient editor in Unity 5.6 or earlier, for boring reasons. You'll have to edit the gradient in debug inspector.", MessageType.Error);
                    // ivyProfile.leafVertexColors.colorKeys[0].color = EditorGUILayout.ColorField( content, ivyProfile.leafVertexColors.colorKeys[0].color );
                    #endif
                }

                content = new GUIContent("Branch Material", "Unity material to use for branches. It doesn't need to be very high resolution or detailed, unless you set your Branch Thickness to be very wide. Example materials can be found in /Hedera/Runtime/Materials/");
                ivyProfile.branchMaterial = EditorGUILayout.ObjectField(content, ivyProfile.branchMaterial, typeof(Material), false) as Material;
                
                if ( ivyProfile.branchMaterial == null) {
                    EditorGUILayout.HelpBox("Branch Material is undefined. Branch meshes will use default material.", MessageType.Warning);
                }
                
                content = new GUIContent("Leaf Material", "Unity material to use for leaves. Example materials are in /Hedera/Runtime/Materials/");
                ivyProfile.leafMaterial = EditorGUILayout.ObjectField(content, ivyProfile.leafMaterial, typeof(Material), false) as Material;

                if ( ivyProfile.leafMaterial == null) {
                    EditorGUILayout.HelpBox("Leaf Material is undefined. Leaf meshes will use default material.", MessageType.Warning);
                }

                if ( ivyProfile.showAdvanced ) {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("EXTRA MESH OPTIONS", EditorStyles.miniLabel);

                    content = new GUIContent("Name Format", "All 3D ivy object names use this template, with numbered placeholders to fill-in data.\n{0}: branch count\n{1}: seed position\n(default: 'Ivy[{0}]{1}')");
                    ivyProfile.namePrefix = EditorGUILayout.DelayedTextField(content, ivyProfile.namePrefix);

                    content = new GUIContent("Batching Static", "Set ivy meshes to 'batch' draw calls for huge performance gains. Always enable this unless you're moving / rotating / scaling the ivy during the game.\n(default: true)");
                    ivyProfile.markMeshAsStatic = EditorGUILayout.Toggle( content, ivyProfile.markMeshAsStatic );

                    content = new GUIContent("Lighting Static", "Set ivy meshes to use lightmapping AND generate lightmap UV2s for the ivy.\n- Make sure your lightmap luxel resolution is high enough, or else it'll probably look very spotty.\n- Also make sure your lightmap atlas size is big enough, or else the lightmapped ivy won't batch.\n- If disabled, meshes won't have UV2s, which saves memory.\n(default: false)");
                    ivyProfile.useLightmapping = EditorGUILayout.Toggle( content, ivyProfile.useLightmapping );

                    content = new GUIContent("Mesh Compress", "How much to compress ivy meshes for a smaller file size in the build. However, higher compression can introduce small flaws or glitches in the mesh.\n(default: Low)");
                    ivyProfile.meshCompress = (IvyProfile.MeshCompression)EditorGUILayout.EnumPopup(content, ivyProfile.meshCompress );

                    content = new GUIContent("Cast Shadows", "The Cast Shadows setting on the ivy mesh renderers.\n(default: On)");
                    ivyProfile.castShadows = (UnityEngine.Rendering.ShadowCastingMode)EditorGUILayout.EnumPopup(content, ivyProfile.castShadows );

                    content = new GUIContent("Receive Shadow", "The Receive Shadows setting on the ivy mesh renderers.\n(default: True)" );
                    ivyProfile.receiveShadows = EditorGUILayout.Toggle( content, ivyProfile.receiveShadows );

                    content = new GUIContent("Smooth Count", "How many Catmull-Rom spline subdivisions to add to each branch to smooth out the line. For example, a value of 2 doubles your branch vert count.\n- Don't set it too high, and make sure you set Simplify above 0%.\n- value of 1 means no smoothing.\n(default: 2)");
                    ivyProfile.branchSmooth = EditorGUILayout.IntSlider(content, ivyProfile.branchSmooth, 1, 4);

                    content = new GUIContent("Simplify %", "How much to optimize branch meshes, to try to retain the main shape with fewer segments. High percentages will lower verts and polycounts for branches, but might look jagged or lose small details. 0% means no optimization will happen.\n(default: 50%)");
                    ivyProfile.branchOptimize = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.branchOptimize * 100f), 0, 100) * 0.01f;

                    content = new GUIContent("Taper %", "How much to taper / sharpen branch mesh ends. Looks great for organic objects. But for ropes, cables, or pipes, you should set to 0% to maintain a constant thickness.\n(default: 100%)");
                    ivyProfile.branchTaper = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.branchTaper * 100f), 0, 100) * 0.01f;

                    content = new GUIContent("Leaf Sunlight %", "Approximates how ivy wants to be in the sun. Leaves will face upwards more. Ivy will grow more leaves on floors / roofs, and fewer leaves on ceilings. 0% means leaves will spawn evenly regardless of surface, and align leaves with surface.\n(default: 100%)");
                    ivyProfile.leafSunlightBonus = EditorGUILayout.IntSlider(content, Mathf.RoundToInt(ivyProfile.leafSunlightBonus * 100f), 0, 100) * 0.01f;

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }

            EditorGUI.indentLevel--;

            if ( EditorGUI.EndChangeCheck() ) {
                Undo.RegisterCompleteObjectUndo(profileAsset, "Hedera > Edit Ivy Settings" );
                EditorUtility.SetDirty(profileAsset );
            }
    
            EditorGUILayout.Space();
        }
    }
}
