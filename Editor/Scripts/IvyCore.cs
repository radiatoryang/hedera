/**************************************************************************************
**
**  Copyright (C) 2006 Thomas Luft, University of Konstanz. All rights reserved.
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
// (C) 2016 Weng Xiao Yi https://github.com/phoenixzz/IvyGenerator
// (C) 2019 Robert Yang https://github.com/radiatoryang/hedera

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

		public static List<IvyBehavior> ivyBehaviors = new List<IvyBehavior>();

		const int TERRAIN_SEARCH_COUNT = 64;
		static Dictionary<TerrainCollider, Terrain> colliderToTerrain = new Dictionary<TerrainCollider, Terrain>();
		static Vector3[] terrainSearchDisc = new Vector3[TERRAIN_SEARCH_COUNT];

        // called on InitializeOnLoad
        static IvyCore()
        {
            if (Instance == null)
            {
                Instance = new IvyCore();
				colliderToTerrain = new Dictionary<TerrainCollider, Terrain>();
				terrainSearchDisc = new Vector3[TERRAIN_SEARCH_COUNT];
            }
            EditorApplication.update += Instance.OnEditorUpdate;
			ivyBehaviors.Clear();
        }

		// TODO:
		// x ivy meshFilter / MR transform positions should be at seedPos
		// x fix local/world conversions with seedPos
		// x make sure roots have better probability LATER for branching on strokes
		// - let user convert mesh to asset, move mesh data out of scene
		// x test lightmapping
		// x mesh children names not getting updated
		// - profile and optimize
		// - cartoon brush, cable brush
		// - test build out
		// - optimize ComputeAdhesion.GetVertices GC
		// - optimise GrowIvyStep.ToArray?
		// - GenerateMeshData.ToList
		// - GenerateMeshData.ToArray
		// - GenerateMeshData.AddList.EnsureCapacity
		// - GenerateMeshData.AddTriangles

        void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup > lastRefreshTime + refreshInterval)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
				CacheTerrainColliderStuff();
                foreach (var ivyB in ivyBehaviors) {
					foreach ( var ivy in ivyB.ivyGraphs) {
						if ( ivy.isGrowing ) {
							GrowIvyStep(ivy, ivyB.profileAsset.ivyProfile);
							if ( ivy.generateMeshDuringGrowth ) {
								IvyMesh.GenerateMesh(ivy, ivyB.profileAsset.ivyProfile);
							}
						}
						if ( !ivy.isGrowing && ivyB.profileAsset.ivyProfile.useLightmapping && ivy.generateMeshDuringGrowth && ivy.dirtyUV2s ) {
							IvyMesh.GenerateMesh( ivy, ivyB.profileAsset.ivyProfile, true);
						}
					}
				}
            }
			
        }

		static void CacheTerrainColliderStuff () {
			colliderToTerrain.Clear();
			foreach ( var terrain in Terrain.activeTerrains ) {
				colliderToTerrain.Add( terrain.GetComponent<TerrainCollider>(), terrain);
			}

			for ( int i=0; i<TERRAIN_SEARCH_COUNT; i++) {
				terrainSearchDisc[i] = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.value;
			}
		}

        [MenuItem("Hedera/Create New Ivy Generator...")]
        public static void NewAssetFromHederaMenu()
        {
            CreateNewAsset("");
        }

		public static IvyProfileAsset CreateNewAsset(string path = "Assets/NewIvyGenerator.asset") {
			if ( path == "") {
				path = EditorUtility.SaveFilePanelInProject("Hedera: Create New Ivy Generator .asset file...", "NewIvyGenerator.asset", "asset", "Choose where in your project to save the new ivy generator asset file.");
			}

			IvyProfileAsset asset = ScriptableObject.CreateInstance<IvyProfileAsset>();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
			return asset;
		}

		[MenuItem("Hedera/Force-Stop All Ivy Growing")]
        public static void ForceStopGrowing()
        {
            foreach ( var gen in ivyBehaviors ) {
				foreach (var ivy in gen.ivyGraphs ) {
					ivy.isGrowing = false;
				}
			}
        }

        public static IvyGraph SeedNewIvyGraph(IvyProfile ivyProfile, Vector3 seedPos, Vector3 primaryGrowDir, Vector3 adhesionVector, Transform root, bool generateMeshPreview=false)
        {
            var graph = new IvyGraph();
	        graph.ResetMeshData();
	        graph.roots.Clear();
			graph.seedPos = seedPos;
			graph.generateMeshDuringGrowth = generateMeshPreview;
			graph.rootBehavior = root;

	        IvyNode tmpNode = new IvyNode();
	        tmpNode.localPos = Vector3.zero; //seedPos;
	        tmpNode.primaryGrowDir = primaryGrowDir;
	        tmpNode.adhesionVector = adhesionVector;
	        tmpNode.length = 0.0f;
			tmpNode.lengthCumulative = 0f;
	        tmpNode.floatingLength = 0.0f;
	        tmpNode.isClimbing = true;

	        IvyRoot tmpRoot = new IvyRoot();
	        tmpRoot.nodes.Add( tmpNode );
	        tmpRoot.isAlive = true;
			graph.isGrowing = true;
	        tmpRoot.parents = 0;
	        graph.roots.Add( tmpRoot );

			if ( graph.generateMeshDuringGrowth ) {
				IvyMesh.GenerateMesh( graph, ivyProfile );
				Undo.RegisterCreatedObjectUndo( graph.rootGO, "Hedera > Paint Ivy");
			}

            return graph;
        }

		public static void ForceIvyGrowth(IvyGraph graph, IvyProfile ivyProfile, Vector3 newPos, Vector3 newNormal) {
			newPos -= graph.seedPos; // convert to local space

			// find the nearest root end node, and continue off of it
			var closestRoot = graph.roots.OrderBy( root => Vector3.Distance( newPos, root.nodes.Last().localPos ) ).FirstOrDefault();
			if ( closestRoot == null ) {
				return;
			}

			var lastNode = closestRoot.nodes.Last();
			var growVector = newPos - lastNode.localPos;

			var newNode = new IvyNode();

			newNode.localPos = newPos;
			newNode.primaryGrowDir = (0.5f * lastNode.primaryGrowDir + 0.5f * growVector.normalized).normalized;
			//newNode.adhesionVector = ComputeAdhesion( newPos, ivyProfile );
			//if ( newNode.adhesionVector.sqrMagnitude < 0.01f ) {
				newNode.adhesionVector = -newNormal;
			//}
			newNode.length = lastNode.length + growVector.magnitude;
			newNode.lengthCumulative = lastNode.lengthCumulative + growVector.magnitude;
			newNode.floatingLength = 0f;
			newNode.isClimbing = true;

			closestRoot.nodes.Add( newNode );
			// TryGrowIvyBranch( graph, ivyProfile, closestRoot, newNode );

			if ( graph.generateMeshDuringGrowth ) {
				IvyMesh.GenerateMesh( graph, ivyProfile );
			}
		}

		public static void ForceRandomIvyBranch ( IvyGraph graph, IvyProfile ivyProfile ) {
			var randomRoot = graph.roots[0];
			var randomNode = randomRoot.nodes[Random.Range(0, randomRoot.nodes.Count)];
			TryGrowIvyBranch( graph, ivyProfile, randomRoot, randomNode, true );
		}

	    public static void GrowIvyStep(IvyGraph graph, IvyProfile ivyProfile)
        {
			// if there are no longer any live roots, then we're dead
			if ( graph.isGrowing ) {
				graph.isGrowing = graph.roots.Where( root => root.isAlive ).Count() > 0;
			}
			if ( !graph.isGrowing ) {
				return;
			}

	        //lets grow
	        foreach (var root in graph.roots)
	        {
		        //process only roots that are alive
		        if (!root.isAlive) 
                    continue;

                IvyNode lastNode = root.nodes[root.nodes.Count-1];

		        //let the ivy die, if the maximum float length is reached
				if ( lastNode.lengthCumulative > ivyProfile.maxLength || (lastNode.lengthCumulative > ivyProfile.minLength && lastNode.floatingLength > ivyProfile.maxFloatLength) ) {
                    // Debug.LogFormat("root death! cum dist: {0:F2}, floatLength {1:F2}", lastNode.lengthCumulative, lastNode.floatingLength);
					root.isAlive = false;
					continue;
				}

                //grow vectors: primary direction, random influence, and adhesion of scene objectss

                //primary vector = weighted sum of previous grow vectors plus a little bit upwards
                Vector3 primaryVector = Vector3.Normalize(lastNode.primaryGrowDir * 2f + Vector3.up * 0.5f);

                //random influence plus a little upright vector
				Vector3 exploreVector = lastNode.localPos - root.nodes[0].localPos;
				if ( exploreVector.magnitude > 1f ) {
					exploreVector = exploreVector.normalized;
				}
				exploreVector *= lastNode.lengthCumulative / ivyProfile.maxLength;
                Vector3 randomVector = (Random.insideUnitSphere * 0.5f + exploreVector).normalized;

                //adhesion influence to the nearest triangle = weighted sum of previous adhesion vectors
                Vector3 adhesionVector = ComputeAdhesion(lastNode.localPos + graph.seedPos, ivyProfile);
				if ( adhesionVector.sqrMagnitude <= 0.01f) {
					adhesionVector = lastNode.adhesionVector;
				}

                //compute grow vector
                Vector3 growVector = ivyProfile.ivyStepDistance * 
				Vector3.Normalize(
					primaryVector * ivyProfile.primaryWeight 
					+ randomVector * Mathf.Max(0.01f, ivyProfile.randomWeight) 
					+ adhesionVector * ivyProfile.adhesionWeight
				);

                //gravity influence
                Vector3 gravityVector = ivyProfile.ivyStepDistance * Vector3.down * ivyProfile.gravityWeight;
                //gravity depends on the floating length
                gravityVector *= Mathf.Pow(lastNode.floatingLength / ivyProfile.maxFloatLength, 0.7f);

                //next possible ivy node

                //climbing state of that ivy node, will be set during collision detection
                bool climbing = false;

                //compute position of next ivy node
                Vector3 newPos = lastNode.localPos + growVector + gravityVector;

                //combine alive state with result of the collision detection, e.g. let the ivy die in case of a collision detection problem
                Vector3 adhesionFromRaycast = adhesionVector;

				// convert newPos to world position, just for the collision calc
				newPos += graph.seedPos;
				root.isAlive = root.isAlive && ComputeCollision( 0.01f, lastNode.localPos + graph.seedPos, ref newPos, ref climbing, ref adhesionFromRaycast, ivyProfile.collisionMask);
				newPos -= graph.seedPos;

                //update grow vector due to a changed newPos
                growVector = newPos - lastNode.localPos - gravityVector;

				// +graph.seedPos to convert back to world space
				graph.debugLineSegmentsList.Add(lastNode.localPos + graph.seedPos);
				graph.debugLineSegmentsList.Add(newPos + graph.seedPos);

                //create next ivy node
                IvyNode newNode = new IvyNode();

                newNode.localPos = newPos;
                newNode.primaryGrowDir = (0.5f * lastNode.primaryGrowDir + 0.5f * growVector.normalized).normalized;
                newNode.adhesionVector = adhesionVector; //Vector3.Lerp(adhesionVector, adhesionFromRaycast, 0.5f);
                newNode.length = lastNode.length + (newPos - lastNode.localPos).magnitude;
				newNode.lengthCumulative = lastNode.lengthCumulative + (newPos - lastNode.localPos).magnitude;
                newNode.floatingLength = climbing ? 0.0f : lastNode.floatingLength + (newPos - lastNode.localPos).magnitude;
                newNode.isClimbing = climbing;

		        root.nodes.Add( newNode );

				var randomNode = root.nodes[Random.Range(0, root.nodes.Count)];
				if ( TryGrowIvyBranch( graph, ivyProfile, root, randomNode ) ) {
					break;
				}
	        }

			// cache line segments
			graph.debugLineSegmentsArray = graph.debugLineSegmentsList.ToArray();
        }

		static bool TryGrowIvyBranch (IvyGraph graph, IvyProfile ivyProfile, IvyRoot root, IvyNode fromNode, bool forceBranch=false) {
			//weight depending on ratio of node length to total length
			float weight = 1f; //Mathf.PerlinNoise( fromNode.localPos.x + fromNode.lengthCumulative, fromNode.length + fromNode.localPos.y + fromNode.localPos.z); // - ( Mathf.Cos( fromNode.length / root.nodes[root.nodes.Count-1].length * 2.0f * Mathf.PI) * 0.5f + 0.5f );
			var nearbyRootCount = graph.roots.Where( r => (r.nodes[0].localPos - fromNode.localPos).sqrMagnitude < ivyProfile.ivyStepDistance ).Count();
			if (!forceBranch ) {
				if ( graph.roots.Count >= ivyProfile.maxBranchesTotal 
					|| nearbyRootCount > ivyProfile.branchingProbability * 2f
					|| root.childCount > ivyProfile.branchingProbability * 3f
					|| root.nodes.Count < 3
					|| root.parents > ivyProfile.branchingProbability * 8f
					|| ivyProfile.maxLength - fromNode.lengthCumulative < ivyProfile.minLength 
					|| Random.value * Mathf.Clamp(weight, 0f, 1f - ivyProfile.branchingProbability) > Mathf.Pow(ivyProfile.branchingProbability, 1.5f )
				) {
					return false;
				}
			}

			//new ivy node
			IvyNode newRootNode = new IvyNode();
			newRootNode.localPos = fromNode.localPos;
			newRootNode.primaryGrowDir = Vector3.Lerp( fromNode.primaryGrowDir, Vector3.up, 0.5f).normalized;
			newRootNode.adhesionVector = fromNode.adhesionVector;
			newRootNode.length = 0.0f;
			newRootNode.lengthCumulative = fromNode.lengthCumulative;
			newRootNode.floatingLength = fromNode.floatingLength;
			newRootNode.isClimbing = true;

			//new ivy root
			IvyRoot newRoot = new IvyRoot();
			newRoot.nodes.Add( newRootNode );
			newRoot.isAlive = true;
			newRoot.parents = root.parents + 1;
			
			graph.roots.Add( newRoot );
			root.childCount++;

			return true;
		}

	    /** compute the adhesion of scene objects at a point pos*/
	    static Vector3 ComputeAdhesion(Vector3 pos, IvyProfile ivyProfile)
        {
	        Vector3 adhesionVector = Vector3.zero;

	        float minDistance = ivyProfile.maxAdhesionDistance;

			// find nearest colliders
			var nearbyColliders = Physics.OverlapSphere( pos, ivyProfile.maxAdhesionDistance, ivyProfile.collisionMask, QueryTriggerInteraction.Ignore);

			// find closest point on each collider
			foreach ( var col in nearbyColliders ) {
				Vector3 closestPoint = Vector3.zero;
				// ClosestPoint does not work on non-convex mesh colliders so let's just pick the closest vertex
				if ( col is MeshCollider && !((MeshCollider)col).convex ) {
					// I don't have time to do anything more robust, sorry
					closestPoint = col.transform.TransformPoint( ((MeshCollider)col).sharedMesh.vertices.OrderBy( vert => Vector3.SqrMagnitude(pos - col.transform.TransformPoint(vert)) ).FirstOrDefault() );
				
					// try to get surface normal at nearest vertex
					var meshColliderHit = new RaycastHit();
					if ( col.Raycast( new Ray(pos, closestPoint - pos), out meshColliderHit, ivyProfile.maxAdhesionDistance) ) {
						closestPoint = pos - meshColliderHit.normal * meshColliderHit.distance;
					}
				} // ClosestPoint doesn't work on TerrainColliders either...
				else if ( col is TerrainCollider ) {
					// based on cache of TerrainColliders, search surrounding points until we find a close enough position
					var terrain = colliderToTerrain[(TerrainCollider)col];
					closestPoint = pos;
					closestPoint.y = terrain.SampleHeight( closestPoint );
					Vector3 closestSearchPoint = closestPoint;
					Vector3 currentSearchPoint = Vector3.zero;

					for ( int i=0; i<terrainSearchDisc.Length; i++) {
						currentSearchPoint = closestPoint + terrainSearchDisc[i] * ivyProfile.maxAdhesionDistance;
						currentSearchPoint.y = terrain.SampleHeight( currentSearchPoint );
						if ( Vector3.SqrMagnitude(pos - currentSearchPoint) < Vector3.SqrMagnitude(pos - closestSearchPoint) ) {
							closestSearchPoint = currentSearchPoint;
							// close enough, early out
							if ( Vector3.SqrMagnitude(pos - currentSearchPoint) < ivyProfile.ivyStepDistance * ivyProfile.ivyStepDistance ) {
								break;
							}
						}
					}
					
					currentSearchPoint = closestSearchPoint + Vector3.down * 0.25f;
					var terrainRayHit = new RaycastHit();
					if ( Physics.Raycast( pos, currentSearchPoint - pos, out terrainRayHit, minDistance, ivyProfile.collisionMask, QueryTriggerInteraction.Ignore) ) {
						closestPoint = pos - terrainRayHit.normal * Vector3.Distance(closestSearchPoint, pos);
					}
				} else {
					closestPoint = col.ClosestPoint( pos );
				}

				// see if the distance is closer than the closest distance so far
				float distance = Vector3.Distance(pos, closestPoint);
				if ( distance < minDistance ) {
					minDistance = distance;
					adhesionVector = (closestPoint - pos).normalized;
				    adhesionVector *= 1.0f - distance / ivyProfile.maxAdhesionDistance; //distance dependent adhesion vector
				}
			}
	        return adhesionVector;
        }

	    /** computes the collision detection for an ivy segment oldPos->newPos, newPos will be modified if necessary */
        static bool ComputeCollision(float stepDistance, Vector3 oldPos, ref Vector3 newPos, ref bool isClimbing, ref Vector3 adhesionVector, LayerMask collisionMask)
        {
	        //reset climbing state
	        isClimbing = false;
	        bool intersection;
	        int deadlockCounter = 0;

	        do
	        {
		        intersection = false;

				// new raycast collision test
				RaycastHit newRayHit = new RaycastHit();
				if ( Physics.Raycast( oldPos, newPos - oldPos, out newRayHit, Vector3.Distance(oldPos,newPos), collisionMask, QueryTriggerInteraction.Ignore) )
				{                    
					newPos += newRayHit.normal * stepDistance;
					adhesionVector = -newRayHit.normal;
					intersection = true;
					isClimbing = true;
				}

		        // abort climbing and growing if the root is stuck in a crack or something
		        if (deadlockCounter++ > 16)
		        {
			        return false;
		        }
  	        }
	        while (intersection);

	        return true;
        }

		public static IvyGraph MergeIvyGraphs (List<IvyGraph> graphsToMerge, IvyProfile ivyProfile, bool rebuildMesh = true) {
			var mainGraph = graphsToMerge[0];
			graphsToMerge.Remove(mainGraph);

			foreach ( var graph in graphsToMerge ) {
				// convert merged graph's localPos to mainGraph's localPos
				foreach ( var root in graph.roots ) {
					foreach ( var node in root.nodes ) {
						node.localPos += graph.seedPos - mainGraph.seedPos;
					}
				}
				mainGraph.roots.AddRange( graph.roots );
				mainGraph.debugLineSegmentsList.AddRange( graph.debugLineSegmentsList );
				if ( graph.rootGO != null) {
					Undo.DestroyObjectImmediate( graph.rootGO );
				}
			}
			mainGraph.debugLineSegmentsArray = mainGraph.debugLineSegmentsList.ToArray();

			if ( rebuildMesh ) {
				Undo.RegisterFullObjectHierarchyUndo( mainGraph.rootGO, "Hedera > Merge Visible");
				IvyMesh.GenerateMesh( mainGraph, ivyProfile, ivyProfile.useLightmapping );
			}

			return mainGraph;
		}


    }
}
