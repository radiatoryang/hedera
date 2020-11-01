/**************************************************************************************
**
**  Copyright (crSpline[5]) 2006 Thomas Luft, University of Konstanz. All rights reserved.
**
**  This file was part of the Ivy Generator Tool.
**
**  This program is free software; you can redistribute it and/or modify it
**  under the terms of the GNU General Public License as published by the
**  Free Software Foundation; either version 2 of the License, or (at your
**  option) any later version.
**  This program is distributed in the hope that it will be useful, but
**  WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
**  or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
**  for more details.
**  You should have received a copy of the GNU General Public License along
**  with this program; if not, write to the Free Software Foundation,
**  Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110, USA 
**
***************************************************************************************/

// subsequent modifications:
// (crSpline[5]) 2016 Weng Xiao Yi https://github.com/phoenixzz/IvyGenerator
// (crSpline[5]) 2019 Robert Yang https://github.com/radiatoryang/hedera

using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Hedera.SimplifyCSharp;

namespace Hedera
{
    public class IvyMesh
    {
		static List<Vector3> verticesAll = new List<Vector3>(4096);
	    static List<Vector2> texCoordsAll = new List<Vector2>(4096);
	    static List<int> trianglesAll = new List<int>(16384);
		static List<Vector3> leafVerticesAll = new List<Vector3>(4096);
		static List<Vector2> leafUVsAll = new List<Vector2>(4096);
		static List<int> leafTrianglesAll = new List<int>(16384);
        static List<Color> leafColorsAll = new List<Color>(4096);
        static Mesh branchMesh, leafMesh;
        static Quaternion lastLeafFacing = Quaternion.identity;

        public static void InitOrRefreshRoot(IvyGraph ivyGraph, IvyProfile ivyProfile) {
            if ( ivyGraph.rootGO == null ) {
                ivyGraph.rootGO = new GameObject("HederaObject");
                ivyGraph.rootGO.transform.SetParent( ivyGraph.rootBehavior );
            }
            SetStaticEditorFlag( ivyGraph.rootGO, StaticEditorFlags.BatchingStatic, ivyProfile.markMeshAsStatic );
            #if UNITY_2019_2_OR_NEWER
            SetStaticEditorFlag( ivyGraph.rootGO, StaticEditorFlags.ContributeGI, ivyProfile.useLightmapping );
            #else
            SetStaticEditorFlag( ivyGraph.rootGO, StaticEditorFlags.LightmapStatic, ivyProfile.useLightmapping );
            #endif
            var rootTrans = ivyGraph.rootGO.transform;
            rootTrans.position = ivyGraph.seedPos;
            rootTrans.rotation = Quaternion.identity;
            rootTrans.localScale = Vector3.one;
            ivyGraph.rootGO.name = string.Format(ivyProfile.namePrefix, ivyGraph.roots.Count, ivyGraph.seedPos);
        }

        public static void GenerateMesh(IvyGraph ivyGraph, IvyProfile ivyProfile, bool doUV2s=false, bool forceGeneration = false)
        {
            // avoid GC allocations by reusing static lists
            verticesAll.Clear();
            texCoordsAll.Clear();
            trianglesAll.Clear();
            leafVerticesAll.Clear();
            leafUVsAll.Clear();
            leafTrianglesAll.Clear();
            leafColorsAll.Clear();

            // the main mesh generation actually happens here; if it can't generate a mesh, then stop
            if ( !GenerateMeshData(ivyGraph, ivyProfile, forceGeneration) ) {
                return;
            }
            ivyGraph.dirtyUV2s = !doUV2s;

            InitOrRefreshRoot( ivyGraph, ivyProfile );
            var myAsset = IvyCore.GetDataAsset( ivyGraph.rootGO );

            // Branch mesh debug
            // Debug.Log( "branchVertices: " + verticesAll.Count );
            // Debug.Log( "branchTris: " + string.Join(", ", trianglesAll.Select( tri => tri.ToString() ).ToArray()) );
            // foreach ( var vert in verticesAll ) {
            //     Debug.DrawRay( vert + ivyGraph.seedPos, Vector3.up, Color.cyan, 1f, false );
            // }

            if ( ivyProfile.ivyBranchSize < 0.0001f ) {
                if ( ivyGraph.branchMF != null ) {
                    IvyCore.DestroyObject( ivyGraph.branchMF.gameObject );
                }
                IvyCore.TryDestroyMesh( ivyGraph.branchMeshID, myAsset, true);
            } else {
                CheckMeshDataAsset( ref ivyGraph.branchMeshID, myAsset, ivyProfile.meshCompress);
                branchMesh = myAsset.meshList[ivyGraph.branchMeshID];
            
                if ( ivyGraph.branchMF == null || ivyGraph.branchR == null) {
                    CreateIvyMeshObject(ivyGraph, ivyProfile, branchMesh, false);
                }
                RefreshMeshObject( ivyGraph.branchMF, ivyProfile );

                branchMesh.Clear();
                ivyGraph.branchMF.name = ivyGraph.rootGO.name + "_Branches";
                ivyGraph.branchR.shadowCastingMode = ivyProfile.castShadows;
                ivyGraph.branchR.receiveShadows = ivyProfile.receiveShadows;
                branchMesh.name = ivyGraph.branchMF.name;
                branchMesh.SetVertices( verticesAll);
                branchMesh.SetUVs(0, texCoordsAll);
                if ( ivyProfile.useLightmapping && doUV2s ) {
                    PackBranchUV2s(ivyGraph);
                }
                branchMesh.SetTriangles(trianglesAll, 0);
                branchMesh.RecalculateBounds();
                branchMesh.RecalculateNormals();
                branchMesh.RecalculateTangents();
                #if UNITY_2017_1_OR_NEWER
                MeshUtility.Optimize( branchMesh );
                #endif
                ivyGraph.branchMF.sharedMesh = branchMesh;
                ivyGraph.branchR.sharedMaterial = ivyProfile.branchMaterial != null ? ivyProfile.branchMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            }

            // Leaves mesh debug
            // Debug.Log( "leafVertices: " + ivyGraph.leafVertices.Count );
            // Debug.Log( "leafTris: " + string.Join(", ", ivyGraph.leafTriangles.Select( tri => tri.ToString() ).ToArray()) );
            
            // don't do leaf mesh if it's unnecessary
            if ( ivyProfile.leafProbability < 0.001f) {
                if ( ivyGraph.leafMF != null ) {
                    IvyCore.DestroyObject( ivyGraph.leafMF.gameObject );
                }
                IvyCore.TryDestroyMesh( ivyGraph.leafMeshID, myAsset, true);
            } else {
                CheckMeshDataAsset(ref ivyGraph.leafMeshID, myAsset, ivyProfile.meshCompress);
                leafMesh = myAsset.meshList[ivyGraph.leafMeshID];

                if ( ivyGraph.leafMF == null || ivyGraph.leafR == null) {
                    CreateIvyMeshObject(ivyGraph, ivyProfile, leafMesh, true);
                } 
                RefreshMeshObject( ivyGraph.leafMF, ivyProfile);

                leafMesh.Clear();
                ivyGraph.leafMF.name = ivyGraph.rootGO.name + "_Leaves";
                ivyGraph.leafR.shadowCastingMode = ivyProfile.castShadows;
                ivyGraph.leafR.receiveShadows = ivyProfile.receiveShadows;
                leafMesh.name = ivyGraph.leafMF.name;
                leafMesh.SetVertices(leafVerticesAll);
                leafMesh.SetUVs(0, leafUVsAll);
                if ( ivyProfile.useLightmapping && doUV2s ) {
                    PackLeafUV2s( ivyGraph );
                }
                leafMesh.SetTriangles(leafTrianglesAll, 0);
                if ( ivyProfile.useVertexColors ) {
                    leafMesh.SetColors( leafColorsAll );
                }   
                leafMesh.RecalculateBounds();
                leafMesh.RecalculateNormals();
                leafMesh.RecalculateTangents();
                #if UNITY_2017_1_OR_NEWER
                MeshUtility.Optimize(leafMesh);
                #endif
                ivyGraph.leafMF.sharedMesh = leafMesh;
                ivyGraph.leafR.sharedMaterial = ivyProfile.leafMaterial != null ? ivyProfile.leafMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            }
            // EditorUtility.SetDirty( myAsset );
            // AssetDatabase.SaveAssets();
            // AssetDatabase.ImportAsset( AssetDatabase.GetAssetPath(myAsset) );
        }

        static void CheckMeshDataAsset(ref long meshID, IvyDataAsset myAsset, IvyProfile.MeshCompression meshCompress) {
            if ( meshID == 0 ) {
                meshID = IvyRoot.GetRandomLong();
            }
            if ( !myAsset.meshList.ContainsKey( meshID ) || myAsset.meshList[meshID]==null ) {
                var newMesh = new Mesh();
                MeshUtility.SetMeshCompression(newMesh, (ModelImporterMeshCompression)meshCompress );
                AssetDatabase.AddObjectToAsset(newMesh, AssetDatabase.GetAssetPath(myAsset));

                if ( myAsset.meshList.ContainsKey(meshID) && myAsset.meshList[meshID]==null) {
                    myAsset.meshList[meshID] = newMesh;
                } else {
                    myAsset.meshList.Add(meshID, newMesh);
                }
            }
        }

        static void RefreshMeshObject(MeshFilter mf, IvyProfile ivyProfile) {
            SetStaticEditorFlag( mf.gameObject, StaticEditorFlags.BatchingStatic, ivyProfile.markMeshAsStatic );
            #if UNITY_2019_2_OR_NEWER
            SetStaticEditorFlag( mf.gameObject, StaticEditorFlags.ContributeGI, ivyProfile.useLightmapping );
            #else
            SetStaticEditorFlag( mf.gameObject, StaticEditorFlags.LightmapStatic, ivyProfile.useLightmapping );
            #endif
            var branchTrans = mf.transform;
            branchTrans.localPosition = Vector3.zero;
            branchTrans.localRotation = Quaternion.identity;
            branchTrans.localScale = Vector3.one;
        }

        static List<Vector3> allLeafPoints = new List<Vector3>(1024);
        static List<Vector3> allPoints = new List<Vector3>(512);
        static List<Vector3> smoothPoints = new List<Vector3>(512);
        static List<Vector3> newPoints = new List<Vector3>(512);
        static List<int> combinedTriangleIndices = new List<int>(1024);
        static Vector3[] branchVertBasis = new Vector3[MAX_BRANCH_SIDES]; // 6 is maximum number of sides allowed
        const int MAX_BRANCH_SIDES = 6;
        static bool GenerateMeshData(IvyGraph ivyGraph, IvyProfile ivyProfile, bool forceGeneration = false)
        {
            var p = ivyProfile;

            //branches
            foreach (var root in ivyGraph.roots)
            {
                var cache = IvyRoot.GetMeshCacheFor( root );
                if ( root.useCachedBranchData && !forceGeneration ) {
                    combinedTriangleIndices.Clear();
                    cache.triangles.ForEach( localIndex => combinedTriangleIndices.Add( localIndex + verticesAll.Count) );
                    trianglesAll.AddRange ( combinedTriangleIndices );

                    verticesAll.AddRange( cache.vertices );
                    texCoordsAll.AddRange( cache.texCoords );
                    continue;
                }
                root.useCachedBranchData = true;

                //process only roots with more than one node
                if (root.nodes.Count < 2) continue;

                cache.vertices.Clear();
                cache.texCoords.Clear();
                cache.triangles.Clear();

                //branch diameter depends on number of parents AND branch taper
                float local_ivyBranchDiameter = 1.0f / Mathf.Lerp(1f, 1f + root.parents, ivyProfile.branchTaper);

                // smooth the line... which increases points a lot
                allPoints = root.nodes.Select( node => node.p).ToList();
                var useThesePoints = allPoints;
                if ( ivyProfile.branchSmooth > 1 ) {
                    SmoothLineCatmullRomNonAlloc( allPoints, smoothPoints, ivyProfile.branchSmooth);
                    useThesePoints = smoothPoints;
                }

                // generate simplified points for each root, to make it less wavy AND save tris
                if ( !root.isAlive && ivyProfile.branchOptimize > 0f ) {
                    newPoints.Clear();
                    newPoints.AddRange( SimplificationHelpers.Simplify<Vector3>( 
                        useThesePoints, 
                        (vec1, vec2) => vec1 == vec2,
                        (vec) => vec.x, 
                        (vec) => vec.y,
                        (vec) => vec.z,
                        ivyProfile.branchOptimize * ivyProfile.ivyStepDistance * 0.5f,
                        false
                    ) );
                    useThesePoints = newPoints;
                } 

                // I'm not sure why there's this bug when we use Catmull Rom + line simplify, but let's do this hacky fix
                // if ( ivyProfile.branchSmooth > 1 && ivyProfile.branchOptimize > 0f ) {
                //     useThesePoints.ForEach( delegate(Vector3 point) {
                //         if ( float.IsInfinity(point.x) ) {point.x = 0f;}
                //         if ( float.IsInfinity(point.y) ) {point.y = 0f;}
                //         if ( float.IsInfinity(point.z) ) {point.z = 0f;}
                //     } );
                // }

                for (int n=0; n < useThesePoints.Count; n++)
                {
                    if ( verticesAll.Count >= 65531 ) {
                        Debug.LogWarning("Hedera: ending branch generation early, reached ~65536 vertex limit on mesh " + ivyGraph.seedPos + "... but this could technically be solved in Unity 2017.3+ or later with 32-bit index formats for meshes? The exercise is left to the reader.");
                        break;
                    }
                    cache.meshSegments = n+1;

                    //weight depending on ratio of node length to total length
                    float taper = 1f * n / useThesePoints.Count;
                    taper = Mathf.Lerp(1f, (1f - taper) * taper, ivyProfile.branchTaper);

                    //create trihedral vertices... TODO: let user specify how many sides?
                    Vector3 up = Vector3.down;
                    Vector3 basis = Vector3.Normalize( n < useThesePoints.Count-1 ? useThesePoints[n+1] - useThesePoints[n] : -(useThesePoints[n] - useThesePoints[n-1]) );
                    // Debug.DrawLine( newPoints[node+1] + ivyGraph.seedPos, newPoints[node] + ivyGraph.seedPos, Color.cyan, 5f, false);
                    
                    int edges = 3; // TODO: finish this, make it configurable
                    float texV = (n % 2 == 0 ? 1f : 0.0f); // vertical UV tiling
                    for(int b=0; b<edges; b++) {
                        // generate vertices
                        if ( b == 0) {
                            branchVertBasis[b] = Vector3.Cross(up, basis).normalized * Mathf.Max(0.001f, local_ivyBranchDiameter * p.ivyBranchSize * taper) + useThesePoints[n];
                        } else {
                            branchVertBasis[b] = RotateAroundAxis(branchVertBasis[0], useThesePoints[n], basis, 6.283f * b / edges);
                        }
                        cache.vertices.Add( branchVertBasis[b] );

                        // generate UVs
                        cache.texCoords.Add( new Vector2( 1f * b / (edges-1), texV) );

                        // add triangles
                        // AddTriangle(root, 4, 1, 5);
                        // AddTriangle(root, 5, 1, 2);

                        // TODO: finish this
                    }

                    if (n == 0) { // start cap
                        if ( taper > 0f) {
                            AddTriangle( cache, 1, 2, 3);
                        }
                        continue;
                    }

                    AddTriangle(cache, 4, 1, 5);
                    AddTriangle(cache, 5, 1, 2);

                    AddTriangle(cache, 5, 2, 6);
                    AddTriangle(cache, 6, 2, 3);

                    AddTriangle(cache, 6, 3, 1);
                    AddTriangle(cache, 6, 1, 4);

                    if (n==useThesePoints.Count-1 && taper > 0f) { // end cap
                        AddTriangle( cache, 3, 2, 1 );
                    }
                    
                }
                
                combinedTriangleIndices.Clear();
                cache.triangles.ForEach( localIndex => combinedTriangleIndices.Add( localIndex + verticesAll.Count) );
                trianglesAll.AddRange ( combinedTriangleIndices );

                verticesAll.AddRange ( cache.vertices );
                texCoordsAll.AddRange( cache.texCoords );
            }

            if ( ivyProfile.ivyLeafSize <= 0.001f || ivyProfile.leafProbability <= 0.001f) {
                return true;
            }

            //create leafs
            allLeafPoints.Clear();
            foreach (var root in ivyGraph.roots)
            {
                // don't bother on small roots
                if ( root.nodes.Count <= 2 ) {
                    root.useCachedLeafData = false;
                    continue;
                }
                var cache = IvyRoot.GetMeshCacheFor(root);

                // use cached mesh data for leaves only if (a) we're supposed to, and (b) if not using vertex colors OR vertex colors seem valid
                if ( root.useCachedLeafData && !forceGeneration && (!ivyProfile.useVertexColors || cache.leafVertices.Count == cache.leafVertexColors.Count) ) {
                    combinedTriangleIndices.Clear();
                    cache.leafTriangles.ForEach( index => combinedTriangleIndices.Add(index + leafVerticesAll.Count));
                    leafTrianglesAll.AddRange( combinedTriangleIndices );

                    allLeafPoints.AddRange( cache.leafPoints );
                    leafVerticesAll.AddRange ( cache.leafVertices );
                    leafUVsAll.AddRange( cache.leafUVs );
                    if (ivyProfile.useVertexColors) {
                        leafColorsAll.AddRange ( cache.leafVertexColors );
                    }
                    continue;
                }
                root.useCachedLeafData = true;
                cache.leafPoints.Clear();
                cache.leafVertices.Clear();
                cache.leafUVs.Clear();
                cache.leafTriangles.Clear();
                cache.leafVertexColors.Clear();

                // simple multiplier, just to make it a more dense
                for (int i = 0; i < 1; ++i)
                {
                    var leafPositions = GetAllSamplePosAlongRoot( root, p.ivyLeafSize * Mathf.Clamp(1f - p.leafSunlightBonus, 0.69f, 1f) * Mathf.Clamp(1f - p.leafProbability, 0.69f, 1f) );

                    // for(int n=0; n<root.nodes.Count; n++)
                    foreach ( var kvp in leafPositions ) 
                    {
                        if ( leafVerticesAll.Count >= 65530 ) {
                            Debug.LogWarning("Hedera: ending leaf generation early, reached ~65536 vertex limit on mesh " + ivyGraph.seedPos + "... but this could technically be solved in Unity 2017.3+ or later with 32-bit index formats for meshes? The exercise is left to the reader.");
                            break;
                        }

                        int n = kvp.Value;
                        Vector3 newLeafPos = kvp.Key;
                        var node = root.nodes[n];

                        // // do not generate a leaf on the first few nodes
                        // if ( n <= 1 ) { // || n >= root.nodes.Count
                        //     continue;
                        // }

                        // probability of leaves on the ground is increased
                        float groundedness = Vector3.Dot(Vector3.down, node.c.normalized);
                        if ( groundedness < -0.02f ) {
                            groundedness -= 0.1f;
                            groundedness *= 3f;
                        } else {
                            groundedness = (groundedness - 0.25f) * 0.5f;
                        }
                        groundedness *= ivyProfile.leafSunlightBonus * p.leafProbability;

                        // don't spawn a leaf on top of another leaf
                        bool badLeafPos = false;
                        float leafSqrSize = p.ivyLeafSize * p.ivyLeafSize * Mathf.Clamp(1f - p.leafProbability - groundedness, 0.01f, 2f);
                        for(int f=0; f<allLeafPoints.Count; f++) {
                            if ( Vector3.SqrMagnitude(allLeafPoints[f] - newLeafPos) < leafSqrSize ) {
                                badLeafPos = true;
                                break;
                            }
                        }
                        if ( badLeafPos ) {
                            continue;
                        }

                        IvyNode previousNode = root.nodes[Mathf.Max(0, n - 1)];

                        // randomize leaf probability // guarantee a leaf on the first or last node
                        if ( (Random.value + groundedness > 1f - p.leafProbability) || n <= 1 || n == root.nodes.Count-1 )
                        {
                            cache.leafPoints.Add( node.p );
                            allLeafPoints.Add( node.p );

                            //center of leaf quad
                            Vector3 up = (newLeafPos - previousNode.p).normalized;
                            Vector3 right = Vector3.Cross( up, node.c );
                            // Vector3 center = newLeafPos - node.c.normalized * 0.05f + (up * Random.Range(-1f, 1f) + right * Random.Range(-1f, 1f) ) * randomSpreadHack * p.ivyLeafSize;
                            Vector3 center = newLeafPos - node.c.normalized * 0.05f;

                            //size of leaf
                            float sizeWeight = 1.5f - ( Mathf.Abs(Mathf.Cos(2.0f * Mathf.PI)) * 0.5f + 0.5f);
                            float leafSize = p.ivyLeafSize * sizeWeight + Random.Range(-p.ivyLeafSize, p.ivyLeafSize) * 0.1f + (p.ivyLeafSize * groundedness);
                            leafSize = Mathf.Max( 0.01f, leafSize);

                            // Quaternion facing = node.c.sqrMagnitude < 0.001f ? Quaternion.identity : Quaternion.LookRotation( Vector3.Lerp(-node.c, Vector3.up, Mathf.Clamp01(0.68f - Mathf.Abs(groundedness)) * ivyProfile.leafSunlightBonus), Random.onUnitSphere);
                            lastLeafFacing = node.c.sqrMagnitude < 0.001f ? lastLeafFacing : Quaternion.LookRotation( Vector3.Lerp(-node.c, Vector3.up, Mathf.Clamp01(0.68f - Mathf.Abs(groundedness)) * ivyProfile.leafSunlightBonus));
                            AddLeafVertex(cache, center, new Vector3(-1f, 1f, 0f), leafSize, lastLeafFacing);
                            AddLeafVertex(cache, center, new Vector3(1f, 1f, 0f), leafSize, lastLeafFacing);
                            AddLeafVertex(cache, center, new Vector3(-1f, -1f, 0f), leafSize, lastLeafFacing);
                            AddLeafVertex(cache, center, new Vector3(1f, -1f, 0f), leafSize, lastLeafFacing);

                            cache.leafUVs.Add(new Vector2(1.0f, 1.0f));
                            cache.leafUVs.Add(new Vector2(0.0f, 1.0f));
                            cache.leafUVs.Add(new Vector2(1.0f, 0.0f));
                            cache.leafUVs.Add(new Vector2(0.0f, 0.0f));

                            if ( ivyProfile.useVertexColors ) {
                                var randomColor = ivyProfile.leafVertexColors.Evaluate( Random.value );
                                cache.leafVertexColors.Add( randomColor );
                                cache.leafVertexColors.Add( randomColor );
                                cache.leafVertexColors.Add( randomColor );
                                cache.leafVertexColors.Add( randomColor );
                            }

                            // calculate normal of the leaf tri, and make it face outwards
                            // var normal = GetNormal(
                            //     ivyGraph.leafVertices[ivyGraph.leafVertices.Count - 2],
                            //     ivyGraph.leafVertices[ivyGraph.leafVertices.Count - 4],
                            //     ivyGraph.leafVertices[ivyGraph.leafVertices.Count - 3]
                            // );
                            // if ( Vector3.Dot( normal, node.adhesionVector) < 0f) {
                            //    AddLeafTriangle(ivyGraph, 2, 4, 3);
                            //    AddLeafTriangle(ivyGraph, 3, 1, 2);
                            // } else {
                                    AddLeafTriangle(cache, 1, 3, 4);
                                    AddLeafTriangle(cache, 4, 2, 1);
                            // }
                        }
                    }
                    combinedTriangleIndices.Clear();
                    cache.leafTriangles.ForEach( index => combinedTriangleIndices.Add(index + leafVerticesAll.Count));
                    leafTrianglesAll.AddRange( combinedTriangleIndices );

                    leafVerticesAll.AddRange ( cache.leafVertices );
                    leafUVsAll.AddRange( cache.leafUVs );
                    if ( ivyProfile.useVertexColors ) {
                        leafColorsAll.AddRange( cache.leafVertexColors );
                    }
                }
            }
            return true;
        }

        static void PackLeafUV2s(IvyGraph graph) {
            // remember: this can only happen AFTER vertices and UV1s are generated, we're packing them into a grid
            int leafCount = leafUVsAll.Count / 4;
            int gridSize = Mathf.CeilToInt( Mathf.Sqrt(leafCount));
            float gridIncrement = 1f / gridSize;
            leafUVsAll.Clear(); // reuse uv0 list

            int uvCounter = 0;
            Vector2 gridPointer = Vector2.zero;

            // TODO: implement pack margin... but probably not a big deal since leaf textures are already transparent along edges
            for( int v=0; v<gridSize; v++) {
                for (int u=0; u<gridSize; u++) {
                    gridPointer = new Vector2(u,v);
                    for( int i=0; i<4 && uvCounter<leafUVsAll.Count; i++) {
                        leafUVsAll[uvCounter+i] = (gridPointer + leafUVsAll[uvCounter+i]) * gridIncrement;
                    }
                    uvCounter += 4;
                }
            }

            leafMesh.SetUVs(1, leafUVsAll);
        }

        static float branchUV2packMargin = 0.01f;
        static void PackBranchUV2s(IvyGraph graph) {
            // remember: this can only happen AFTER vertices and UV1s are generated, we're packing them into columns
            var rootsWithUVs = graph.roots.Where (root => root.meshSegments > 0).ToArray();
            int branchCount = rootsWithUVs.Length;
            int meshSegmentCount = 0; // placeholder, will depend on root
            float gridIncrementX = 1f / branchCount;
            float gridIncrementY = 0f; // placeholder, will depend on root
            texCoordsAll.Clear(); // reuse uv0 list

            int uvCounter = 0;
            Vector2 gridPointer = Vector2.zero;
            Vector2 gridIncrement = Vector2.zero;

            for( int u=0; u<branchCount; u++) {
                meshSegmentCount = rootsWithUVs[u].meshSegments;
                gridIncrementY = 1f / (meshSegmentCount-1); // segmentRow.y is always 0f, so that's why we -1 here
                gridIncrement = new Vector2( gridIncrementX, gridIncrementY);
                for (int v=0; v<meshSegmentCount; v++) {
                    gridPointer = new Vector2(u, v);
                    for( int i=0; i<3 && uvCounter<texCoordsAll.Count; i++) {
                        texCoordsAll[uvCounter+i] = Vector2.Scale(gridPointer, gridIncrement) + new Vector2( (0.5f * i) * (gridIncrementX - branchUV2packMargin), 0f);
                    }
                    uvCounter += 3;
                }
            }
            branchMesh.SetUVs(1, texCoordsAll );
        }

        static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            // Find vectors corresponding to two of the sides of the triangle.
            Vector3 side1 = b - a;
            Vector3 side2 = c - a;

            // Cross the vectors to get a perpendicular vector, then normalize it.
            return Vector3.Cross(side1, side2).normalized;
        }

        static Dictionary<Vector3, int> leafList = new Dictionary<Vector3, int>(1024);
        static Dictionary<Vector3, int> GetAllSamplePosAlongRoot(IvyRoot root, float leafSize) {
            leafList.Clear();
            float rootEndLength = root.nodes[root.nodes.Count-1].s + Mathf.Epsilon;
            for( float pointer = leafSize; pointer <= rootEndLength; pointer += leafSize ) {
                AddLeafPosAlongRoot( root, pointer );
            }
            return leafList;
        }

        static void AddLeafPosAlongRoot(IvyRoot ivyRoot, float distance) {
            int startNodeIndex = 0, endNodeIndex = -1;
            for (int i=0; i<ivyRoot.nodes.Count; i++) {
                if ( ivyRoot.nodes[i].s <= distance + Mathf.Epsilon ) {
                    startNodeIndex = i;
                }
                if ( endNodeIndex < 0 && ivyRoot.nodes[i].s >= distance - Mathf.Epsilon ) {
                    endNodeIndex = i;
                }
            }

            float t = Mathf.InverseLerp( ivyRoot.nodes[startNodeIndex].s, ivyRoot.nodes[endNodeIndex].s, distance);
            leafList.Add( 
                Vector3.Lerp( ivyRoot.nodes[startNodeIndex].p, ivyRoot.nodes[endNodeIndex].p, t), 
                startNodeIndex 
            );
        }

        static void AddLeafVertex(IvyRootMeshCache ivyRoot, Vector3 center, Vector3 offsetScalar, float ivyLeafSize, Quaternion facing )
        {
            var tmpVertex = Vector3.zero;
            tmpVertex = center + ivyLeafSize * offsetScalar;
            tmpVertex = facing * (tmpVertex - center) + center; // thank you "The Pirate Duck" https://forum.unity.com/threads/rotate-vertices-around-pivot.124131/
            tmpVertex += Random.onUnitSphere * ivyLeafSize * 0.5f;
            ivyRoot.leafVertices.Add(tmpVertex);
        }

        static void AddLeafTriangle(IvyRootMeshCache ivyRoot, int offset1, int offset2, int offset3)
        {
            ivyRoot.leafTriangles.Add( ivyRoot.leafVertices.Count - offset1);
            ivyRoot.leafTriangles.Add( ivyRoot.leafVertices.Count - offset2);
            ivyRoot.leafTriangles.Add( ivyRoot.leafVertices.Count - offset3);
        }

        static void AddTriangle(IvyRootMeshCache ivyRoot, int offset1, int offset2, int offset3)
        {
            ivyRoot.triangles.Add( ivyRoot.vertices.Count - offset1);
            ivyRoot.triangles.Add( ivyRoot.vertices.Count - offset2);
            ivyRoot.triangles.Add( ivyRoot.vertices.Count - offset3);
        }

        // static float Vector2ToPolar(Vector2 vector)
        // {
        //     float phi = (vector.x == 0.0f) ? 0.0f : Mathf.Atan(vector.y / vector.x);

        //     if (vector.x < 0.0f)
        //     {
        //         phi += Mathf.PI;
        //     }
        //     else
        //     {
        //         if (vector.y < 0.0f)
        //         {
        //             phi += 2.0f * Mathf.PI;
        //         }
        //     }

        //     return phi;
        // }

        static Vector3 RotateAroundAxis(Vector3 vector, Vector3 axisPosition, Vector3 axis, float angle)
        {
            //determining the sinus and cosinus of the rotation angle
            float cosTheta = Mathf.Cos(angle);
            float sinTheta = Mathf.Sin(angle);

            //Vector3 from the given axis point to the initial point
            Vector3 direction = vector - axisPosition;

            //new vector which will hold the direction from the given axis point to the new rotated point 
            Vector3 newDirection = Vector3.zero;

            //x-component of the direction from the given axis point to the rotated point
            newDirection.x = (cosTheta + (1 - cosTheta) * axis.x * axis.x) * direction.x +
                             ((1 - cosTheta) * axis.x * axis.y - axis.z * sinTheta) * direction.y +
                             ((1 - cosTheta) * axis.x * axis.z + axis.y * sinTheta) * direction.z;

            //y-component of the direction from the given axis point to the rotated point
            newDirection.y = ((1 - cosTheta) * axis.x * axis.y + axis.z * sinTheta) * direction.x +
                             (cosTheta + (1 - cosTheta) * axis.y * axis.y) * direction.y +
                             ((1 - cosTheta) * axis.y * axis.z - axis.x * sinTheta) * direction.z;

            //z-component of the direction from the given axis point to the rotated point
            newDirection.z = ((1 - cosTheta) * axis.x * axis.z - axis.y * sinTheta) * direction.x +
                             ((1 - cosTheta) * axis.y * axis.z + axis.x * sinTheta) * direction.y +
                             (cosTheta + (1 - cosTheta) * axis.z * axis.z) * direction.z;

            //returning the result by addind the new direction vector to the given axis point
            return axisPosition + newDirection;
        }


        static void CreateIvyMeshObject(IvyGraph graph, IvyProfile profile, Mesh mesh, bool isLeaves=false)
        {
            var PartObj = new GameObject("HederaMesh");
            PartObj.transform.parent = graph.rootGO.transform;
            PartObj.transform.localPosition = Vector3.zero;

            if (!isLeaves) {
                graph.branchMF = PartObj.AddComponent<MeshFilter>();
                graph.branchMF.sharedMesh = mesh;
                graph.branchR = PartObj.AddComponent<MeshRenderer>();
            } else {
                graph.leafMF = PartObj.AddComponent<MeshFilter>();
                graph.leafMF.sharedMesh = mesh;
                graph.leafR = PartObj.AddComponent<MeshRenderer>();
            }
        }

        // from https://forum.unity.com/threads/how-to-set-use-staticeditorflags-cant-seem-to-set-them-from-script.137024/#post-3721513
        // thanks, hungrybelome!
        static void SetStaticEditorFlag(GameObject obj, StaticEditorFlags flag, bool shouldEnable)
        {
            var currentFlags = GameObjectUtility.GetStaticEditorFlags(obj);
 
            if (shouldEnable)
            {
                currentFlags |= flag;
            }
            else
            {
                currentFlags &= ~flag;
            }
           
            GameObjectUtility.SetStaticEditorFlags(obj, currentFlags);
        }

        // catmull-rom spline code from https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline
        static Vector3[] crPoints = new Vector3[4];
        static float[] crTan = new float[4];
        static Vector3[] crSpline = new Vector3[6];
        static bool crPointsTooClose = false;
        static void SmoothLineCatmullRomNonAlloc( List<Vector3> points, List<Vector3> splinePoints, int splineSubdivisons=2, float minimumSquareDistance = 0.0001f ) {
            splinePoints.Clear();

            for (int i=0; i<points.Count; i++) {
                crPointsTooClose = false;
                crPoints[0] = points[Mathf.Max(0, i-1) ]; 
                crPoints[1] = points[i];
                crPoints[2] = points[Mathf.Min(points.Count-1, i+1)];
                crPoints[3] = points[Mathf.Min(points.Count-1, i+2)];

                // check minimum distance
                for( int x=0; x<3; x++) {
                    if ( Vector3.SqrMagnitude(crPoints[x]-crPoints[x+1]) < minimumSquareDistance ) {
                        crPointsTooClose = true;
                        break;
                    }
                }
                if ( crPointsTooClose ) {
                    continue;
                }

                crTan[0] = 0.0f;
                crTan[1] = GetT(crTan[0], crPoints[0], crPoints[1]);
                crTan[2] = GetT(crTan[1], crPoints[1], crPoints[2]);
                crTan[3] = GetT(crTan[2], crPoints[2], crPoints[3]);

                for(float t=crTan[1]; t<crTan[2]; t+=((crTan[2]-crTan[1])/splineSubdivisons))
                {
                    crSpline[0] = (crTan[1]-t)/(crTan[1]-crTan[0])*crPoints[0] + (t-crTan[0])/(crTan[1]-crTan[0])*crPoints[1];
                    crSpline[1] = (crTan[2]-t)/(crTan[2]-crTan[1])*crPoints[1] + (t-crTan[1])/(crTan[2]-crTan[1])*crPoints[2];
                    crSpline[2] = (crTan[3]-t)/(crTan[3]-crTan[2])*crPoints[2] + (t-crTan[2])/(crTan[3]-crTan[2])*crPoints[3];
                    
                    crSpline[3] = (crTan[2]-t)/(crTan[2]-crTan[0])*crSpline[0] + (t-crTan[0])/(crTan[2]-crTan[0])*crSpline[1];
                    crSpline[4] = (crTan[3]-t)/(crTan[3]-crTan[1])*crSpline[1] + (t-crTan[1])/(crTan[3]-crTan[1])*crSpline[2];
                    
                    crSpline[5] = (crTan[2]-t)/(crTan[2]-crTan[1])*crSpline[3] + (t-crTan[1])/(crTan[2]-crTan[1])*crSpline[4];
                    
                    splinePoints.Add(crSpline[5]);
                }
            }
        }

        static float alpha = 0.5f;
        static float crA, crB, crC;
        static float GetT(float t, Vector3 pointA, Vector3 pointB)
        {
            crA = Mathf.Pow(pointB.x-pointA.x, 2.0f) + Mathf.Pow(pointB.y-pointA.y, 2.0f) + Mathf.Pow(pointB.z-pointA.z, 2f);
            crB = Mathf.Pow(crA, 0.5f);
            crC = Mathf.Pow(crB, alpha);
        
            return (crC + t);
        }


    }
}
