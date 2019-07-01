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

        public static IvyGraph SeedNewIvyGraph(Vector3 seedPos, Vector3 primaryGrowDir, Vector3 adhesionVector = default(Vector3), bool generateMeshPreview=false)
        {
            var graph = new IvyGraph();
	        graph.ResetMeshData();
	        graph.roots.Clear();
			graph.seedPos = seedPos;
			graph.generateMeshDuringGrowth = generateMeshPreview;

	        IvyNode tmpNode = new IvyNode();
	        tmpNode.pos = seedPos;
	        tmpNode.primaryGrowDir = primaryGrowDir;
	        tmpNode.adhesionVector = adhesionVector;
	        tmpNode.length = 0.0f;
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

	    public static void GrowIvyStep(IvyGraph graph, IvyProfile ivyProfile)
        {
			// if there are no longer any live roots, then we're dead
			graph.isGrowing = graph.roots.Where( root => root.isAlive ).Count() > 0;
			if ( !graph.isGrowing ) {
				return;
			}

	        //lets grow
	        foreach (var root in graph.roots)
	        {
		        //process only roots that are alive
		        if (!root.isAlive) 
                    continue;

                IvyNode lastnode = root.nodes[root.nodes.Count-1];
		        //let the ivy die, if the maximum float length is reached
		        if (lastnode.floatingLength > ivyProfile.maxFloatLength) 
                    root.isAlive = false;

                //grow vectors: primary direction, random influence, and adhesion of scene objectss

                //primary vector = weighted sum of previous grow vectors
                Vector3 primaryVector = lastnode.primaryGrowDir;

                //random influence plus a little upright vector
                Vector3 randomVector = (Random.insideUnitSphere * 0.5f + Vector3.up * 0.25f).normalized;

                //adhesion influence to the nearest triangle = weighted sum of previous adhesion vectors
                Vector3 adhesionVector = ComputeAdhesion(lastnode.pos, ivyProfile);

                //compute grow vector
                Vector3 growVector = ivyProfile.ivySize * (
					primaryVector * ivyProfile.primaryWeight 
					+ randomVector * ivyProfile.randomWeight 
					+ adhesionVector * ivyProfile.adhesionWeight
				);

                //gravity influence
                Vector3 gravityVector = ivyProfile.ivySize * new Vector3(0.0f, -1.0f, 0.0f) * ivyProfile.gravityWeight;

                //gravity depends on the floating length
                gravityVector *= Mathf.Pow(lastnode.floatingLength / ivyProfile.maxFloatLength, 0.7f);


                //next possible ivy node

                //climbing state of that ivy node, will be set during collision detection
                bool climbing = false;

                //compute position of next ivy node
                Vector3 newPos = lastnode.pos + growVector + gravityVector;

                //combine alive state with result of the collision detection, e.g. let the ivy die in case of a collision detection problem
                root.isAlive = root.isAlive && ComputeCollision(lastnode.pos, ref newPos, ref climbing, ivyProfile.collisionMask);

                //update grow vector due to a changed newPos
                growVector = newPos - lastnode.pos - gravityVector;

				graph.debugLineSegmentsList.Add(lastnode.pos);
				graph.debugLineSegmentsList.Add(newPos);

                //create next ivy node
                IvyNode tmpNode = new IvyNode();

                tmpNode.pos = newPos;
                tmpNode.primaryGrowDir = (0.5f * lastnode.primaryGrowDir + 0.5f * growVector.normalized).normalized;
                tmpNode.adhesionVector = adhesionVector;
                tmpNode.length = lastnode.length + (newPos - lastnode.pos).magnitude;
                tmpNode.floatingLength = climbing ? 0.0f : lastnode.floatingLength + (newPos - lastnode.pos).magnitude;
                tmpNode.isClimbing = climbing;

		        root.nodes.Add( tmpNode );
	        }

	        //lets produce child ivys
	        foreach (var root in graph.roots)
	        {
		        //process only roots that are alive
		        if (!root.isAlive) 
                    continue;

		        //process only roots up to hierarchy level 3, results in a maximum hierarchy level of 4
		        if (root.parents > 3) {
                    continue;
				}

				// it should grow only if it has a few nodes at least
				if ( root.nodes.Count < 3) { 
					continue;
				}

				if ( root.childCount > 4 ) {
					root.isAlive = false;
					continue;
				}

		        //add child ivys on existing ivy nodes
		        foreach (var node in root.nodes)
		        {
			        //weight depending on ratio of node length to total length
			        float weight = 1.0f - ( Mathf.Cos( node.length / root.nodes[root.nodes.Count-1].length * 2.0f * Mathf.PI) * 0.5f + 0.5f );

			        //random influence
			        float probability = Random.value;

			        if (probability * weight > 1f - ivyProfile.branchingProbability)
			        {
				        //new ivy node
				        IvyNode tmpNode = new IvyNode();
				        tmpNode.pos = node.pos;
				        tmpNode.primaryGrowDir = new Vector3(0.0f, 1.0f, 0.0f);
				        tmpNode.adhesionVector = new Vector3(0.0f, 0.0f, 0.0f);
				        tmpNode.length = 0.0f;
				        tmpNode.floatingLength = node.floatingLength;
				        tmpNode.isClimbing = true;

				        //new ivy root
				        IvyRoot tmpRoot = new IvyRoot();
				        tmpRoot.nodes.Add( tmpNode );
				        tmpRoot.isAlive = true;
				        tmpRoot.parents = root.parents + 1;
				        graph.roots.Add( tmpRoot );

						root.childCount++;

				        //limit the branching to only one new root per iteration, so return
				        return;
			        }
		        }
	        }

			// cache line segments
			graph.debugLineSegmentsArray = graph.debugLineSegmentsList.ToArray();
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
        static bool ComputeCollision(Vector3 oldPos, ref Vector3 newPos, ref bool isClimbing, LayerMask collisionMask)
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
					//mirror newPos at triangle plane
					newPos += 2.0f * newRayHit.normal * newRayHit.distance;
					intersection = true;
					isClimbing = true;
				}

		        // abort climbing and growing if there was a collistion detection problem
		        if (deadlockCounter++ > 5)
		        {
			        return false;
		        }
  	        }
	        while (intersection);

	        return true;
        }


    }
}
