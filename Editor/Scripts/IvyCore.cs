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

        // called on InitializeOnLoad
        static IvyCore()
        {
            if (Instance == null)
            {
                Instance = new IvyCore();
            }
            EditorApplication.update += Instance.OnEditorUpdate;
			ivyBehaviors.Clear();
        }

		// TODO:
		// - make branching happen later in root systems
		// - merge visible button
		// - ivy meshFilter / MR transform positions should be at seedPos + parented to the IvyBehavior
		// - show capacity for vertices and tris (for branches, count nodes?)... leaf mesh gen should account for full quota
		// - add undo?
		// - rope preset, cable preset, vine preset
		// - vertex color variation
		// - texture atlas mode for leaves

        void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup > lastRefreshTime + refreshInterval)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                foreach (var ivyB in ivyBehaviors) {
					foreach ( var ivy in ivyB.ivyGraphs) {
						if ( ivy.isGrowing ) {
							GrowIvyStep(ivy, ivyB.profileAsset.ivyProfile);
							if ( ivy.generateMeshDuringGrowth ) {
								IvyMesh.GenerateMesh(ivy, ivyB.profileAsset.ivyProfile);
							}
						}
					}
				}
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

        public static IvyGraph SeedNewIvyGraph(Vector3 seedPos, Vector3 primaryGrowDir, Vector3 adhesionVector, Transform root, bool generateMeshPreview=false)
        {
            var graph = new IvyGraph();
	        graph.ResetMeshData();
	        graph.roots.Clear();
			graph.seedPos = seedPos;
			graph.generateMeshDuringGrowth = generateMeshPreview;
			graph.rootBehavior = root;

	        IvyNode tmpNode = new IvyNode();
	        tmpNode.pos = seedPos;
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

            return graph;
        }

		public static void ForceIvyGrowth(IvyGraph graph, IvyProfile ivyProfile, Vector3 newPos, Vector3 newNormal) {
			// find the nearest root end node, and continue off of it
			var closestRoot = graph.roots.OrderBy( root => Vector3.Distance( newPos, root.nodes.Last().pos ) ).FirstOrDefault();
			if ( closestRoot == null ) {
				return;
			}

			var lastNode = closestRoot.nodes.Last();
			var growVector = newPos - lastNode.pos;

			var newNode = new IvyNode();

			newNode.pos = newPos;
			newNode.primaryGrowDir = (0.5f * lastNode.primaryGrowDir + 0.5f * growVector.normalized).normalized;
			newNode.adhesionVector = ComputeAdhesion( newPos, ivyProfile );
			newNode.length = lastNode.length + growVector.magnitude;
			newNode.lengthCumulative = lastNode.lengthCumulative + growVector.magnitude;
			newNode.floatingLength = 0f;
			newNode.isClimbing = true;

			closestRoot.nodes.Add( newNode );

			TryGrowIvyBranch( graph, ivyProfile, closestRoot, newNode );

			if ( graph.generateMeshDuringGrowth ) {
				IvyMesh.GenerateMesh( graph, ivyProfile );
			}
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

                //primary vector = weighted sum of previous grow vectors
                Vector3 primaryVector = lastNode.primaryGrowDir;

                //random influence plus a little upright vector
				Vector3 exploreVector = lastNode.pos - root.nodes[0].pos;
				if ( exploreVector.magnitude > 1f ) {
					exploreVector = exploreVector.normalized;
				}
				exploreVector *= lastNode.length;
                Vector3 randomVector = (Random.insideUnitSphere * 0.5f + Vector3.up * 0.5f + exploreVector).normalized;

                //adhesion influence to the nearest triangle = weighted sum of previous adhesion vectors
                Vector3 adhesionVector = ComputeAdhesion(lastNode.pos, ivyProfile);

                //compute grow vector
                Vector3 growVector = ivyProfile.ivyStepDistance * 
				Vector3.Normalize(
					primaryVector * ivyProfile.primaryWeight 
					+ randomVector * ivyProfile.randomWeight 
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
                Vector3 newPos = lastNode.pos + growVector + gravityVector;

                //combine alive state with result of the collision detection, e.g. let the ivy die in case of a collision detection problem
                root.isAlive = root.isAlive && ComputeCollision(0.069f, lastNode.pos, ref newPos, ref climbing, ivyProfile.collisionMask);

                //update grow vector due to a changed newPos
                growVector = newPos - lastNode.pos - gravityVector;

				graph.debugLineSegmentsList.Add(lastNode.pos);
				graph.debugLineSegmentsList.Add(newPos);

                //create next ivy node
                IvyNode newNode = new IvyNode();

                newNode.pos = newPos;
                newNode.primaryGrowDir = (0.5f * lastNode.primaryGrowDir + 0.5f * growVector.normalized).normalized;
                newNode.adhesionVector = adhesionVector;
                newNode.length = lastNode.length + (newPos - lastNode.pos).magnitude;
				newNode.lengthCumulative = lastNode.lengthCumulative + (newPos - lastNode.pos).magnitude;
                newNode.floatingLength = climbing ? 0.0f : lastNode.floatingLength + (newPos - lastNode.pos).magnitude;
                newNode.isClimbing = climbing;

		        root.nodes.Add( newNode );

	        	//lets produce child ivys
		        //process only roots that are alive
				//process only roots up to hierarchy level 3, results in a maximum hierarchy level of 4
				//and branch only if it has a few nodes at least
		        if ( root.parents > 3 || root.nodes.Count < 3) 
                    continue;

				if ( root.childCount >= ivyProfile.maxBranchesPerRoot ) {
					continue;
				}

				var randomNode = root.nodes[Random.Range(0, root.nodes.Count)];
				if ( TryGrowIvyBranch( graph, ivyProfile, root, randomNode ) ) {
					break;
				}
	        }

			// cache line segments
			graph.debugLineSegmentsArray = graph.debugLineSegmentsList.ToArray();
        }

		static bool TryGrowIvyBranch (IvyGraph graph, IvyProfile ivyProfile, IvyRoot root, IvyNode fromNode) {
			//weight depending on ratio of node length to total length
			float weight = 1f; //1.0f - ( Mathf.Cos( newNode.length / root.nodes[root.nodes.Count-1].length * 2.0f * Mathf.PI) * 0.5f + 0.5f );
			if (Random.value * weight > ivyProfile.branchingProbability)
			{
				return false;
			}

			//new ivy node
			IvyNode newRootNode = new IvyNode();
			newRootNode.pos = fromNode.pos;
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
			var nearbyColliders = Physics.OverlapSphere( pos, ivyProfile.maxAdhesionDistance);

			// find closest point on each collider
			foreach ( var col in nearbyColliders ) {
				Vector3 p0 = Vector3.zero;
				// ClosestPoint does not work on non-convex mesh colliders, so let's just pick the closest vertex
				if ( col is MeshCollider && !((MeshCollider)col).convex ) {
					p0 = ((MeshCollider)col).sharedMesh.vertices.OrderBy( vert => Vector3.Distance(pos, col.transform.TransformVector(vert)) ).FirstOrDefault();
				} else {
					p0 = col.ClosestPoint( pos );
				}

				// see if the distance is closer than the closest distance so far
				float distance = Vector3.Distance(pos, p0);
				if ( distance < minDistance ) {
					minDistance = distance;
					adhesionVector = (p0 - pos).normalized;
				    adhesionVector *= 1.0f - distance / ivyProfile.maxAdhesionDistance; //distance dependent adhesion vector
				}
			}

	        return adhesionVector;
        }

	    /** computes the collision detection for an ivy segment oldPos->newPos, newPos will be modified if necessary */
        static bool ComputeCollision(float stepDistance, Vector3 oldPos, ref Vector3 newPos, ref bool isClimbing, LayerMask collisionMask)
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
				if ( Physics.Raycast( oldPos, newPos - oldPos, out newRayHit, Vector3.Distance(oldPos,newPos), collisionMask) )
				{                    
					newPos += newRayHit.normal * stepDistance;
					intersection = true;
					isClimbing = true;
				}

		        // abort climbing and growing if there was a collistion detection problem
		        if (deadlockCounter++ > 10)
		        {
			        return false;
		        }
  	        }
	        while (intersection);

	        return true;
        }


    }
}
