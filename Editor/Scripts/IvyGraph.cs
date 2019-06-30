/**************************************************************************************
**
**  Copyright (C) 2006 Thomas Luft, University of Konstanz. All rights reserved.
**
**  This file is part of the Ivy Generator Tool.
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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hedera
{
	[System.Serializable]
    public class IvyProfile {
		public LayerMask collisionMask;
        public Material branchMaterial, leafMaterial;

         /** the ivy size factor, influences the grow behaviour [0..0,1] */
	    public float ivySize;

	    /** leaf size factor [0..0,1] */
	    public float ivyLeafSize;

	    /** branch size factor [0..0,1] */
	    public float ivyBranchSize;

        /** maximum length of an ivy branch segment that is freely floating [0..1] */
	    public float maxFloatLength;

	    /** maximum distance for adhesion of scene object [0..1] */
	    public float maxAdhesionDistance;

	    /** weight for the primary grow vector [0..1] */
	    public float primaryWeight;

	    /** weight for the random influence vector [0..1] */
	    public float randomWeight;

	    /** weight for the gravity vector [0..1] */
	    public float gravityWeight;

	    /** weight for the adhesion vector [0..1] */
	    public float adhesionWeight;

	    /** the probability of producing a new ivy root per iteration [0..1]*/
	    public float branchingProbability;

	    /** the probability of creating a new ivy leaf [0..1] */
	    public float leafProbability;

		public IvyProfile() {
			ResetSettings();
		}

	    public void ResetSettings()
        {
	        primaryWeight = 0.5f;
	        randomWeight = 0.2f;
	        gravityWeight = 1.0f;
	        adhesionWeight = 0.1f;

	        branchingProbability = 0.95f;
	        leafProbability = 0.7f;

	        ivySize = 0.05f;

	        ivyLeafSize = 1.5f;
	        ivyBranchSize = 0.15f;

	        maxFloatLength = 0.1f;
	        maxAdhesionDistance = 0.1f;

			collisionMask = Physics.DefaultRaycastLayers;
			branchMaterial = null;
			leafMaterial = null;
        }

    }

	[System.Serializable]
    public class IvyNode
    {
	    public Vector3 pos;			

	    /** primary grow direction, a weighted sum of the previous directions */
	    public Vector3 primaryGrowDir;

	    public Vector3 adhesionVector;

	    /** a smoothed adhesion vector computed and used during the birth phase,
	       since the ivy leaves are align by the adhesion vector, this smoothed vector
	       allows for smooth transitions of leaf alignment */
	    public Vector3 smoothAdhesionVector;

	    public float length;

	    /** length at the last node that was climbing */
	    public float floatingLength;

	    public bool isClimbing;

        public IvyNode()
        {
            isClimbing = false;
            length = 0;
            floatingLength = 0;
        }
    }

	[System.Serializable]
    public class IvyRoot
    {
	    public List<IvyNode> nodes = new List<IvyNode>();
	    public bool isAlive;

	    /** number of parents, represents the level in the root hierarchy */
	    public int parents;
    }

	[System.Serializable]
    public class IvyGraph
    {
		public bool isGrowing = false;
		public Vector3 seedPos;

		public void DrawDebug(Color debugColor = default(Color)) {
			if ( debugColor == default(Color)) {
				debugColor = Color.yellow;
			}

			foreach (var root in roots)
            {
                for (int node = 0; node < root.nodes.Count - 1; node++)
                    Debug.DrawLine(root.nodes[node].pos, root.nodes[node + 1].pos, debugColor);
            }
		}

		public void ResetMeshData()
        {
	        vertices.Clear();
            texCoords.Clear();
            triangles.Clear();
        }

	    public void SeedRoot(Vector3 seedPos)
        {
	        ResetMeshData();
	        roots.Clear();
			this.seedPos = seedPos;

	        IvyNode tmpNode = new IvyNode();
	        tmpNode.pos = seedPos;
	        tmpNode.primaryGrowDir = new Vector3(0.0f, 1.0f, 0.0f);
	        tmpNode.adhesionVector = new Vector3(0.0f, 0.0f, 0.0f);
	        tmpNode.length = 0.0f;
	        tmpNode.floatingLength = 0.0f;
	        tmpNode.isClimbing = true;

	        IvyRoot tmpRoot = new IvyRoot();
	        tmpRoot.nodes.Add( tmpNode );
	        tmpRoot.isAlive = true;
			isGrowing = true;
	        tmpRoot.parents = 0;
	        roots.Add( tmpRoot );
        }

	    public void GrowIvyStep(IvyProfile ivyProfile)
        {
			// if there are no longer any live roots, then we're dead
			isGrowing = roots.Where( root => root.isAlive ).Count() > 0;
			if ( !isGrowing ) {
				return;
			}

	        //lets grow
	        foreach (var root in roots)
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
	        foreach (var root in roots)
	        {
		        //process only roots that are alive
		        if (!root.isAlive)
                    continue;

		        //process only roots up to hierarchy level 3, results in a maximum hierarchy level of 4
		        if (root.parents > 3) 
                    continue;

		        //add child ivys on existing ivy nodes
		        foreach (var node in root.nodes)
		        {
			        //weight depending on ratio of node length to total length
			        float weight = 1.0f - ( Mathf.Cos( node.length / root.nodes[root.nodes.Count-1].length * 2.0f * Mathf.PI) * 0.5f + 0.5f );

			        //random influence
			        float probability = Random.value;

			        if (probability * weight > ivyProfile.branchingProbability)
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
				        roots.Add( tmpRoot );

				        //limit the branching to only one new root per iteration, so return
				        return;
			        }
		        }
	        }

			DrawDebug();
        }

	    /** compute the adhesion of scene objects at a point pos*/
	    public Vector3 ComputeAdhesion(Vector3 pos, IvyProfile ivyProfile)
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
        public bool ComputeCollision(Vector3 oldPos, ref Vector3 newPos, ref bool isClimbing, LayerMask collisionMask)
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

	    // ivy roots
	    [HideInInspector] public List<IvyRoot> roots = new List<IvyRoot>();	

		// ivy mesh data
		public List<Vector3> vertices = new List<Vector3>();
	    public List<Vector2> texCoords = new List<Vector2>();
	    public List<int> triangles = new List<int>();

		public List<Vector3> leafVertices = new List<Vector3>();
		public List<Vector2> leafUVs = new List<Vector2>();
		public List<int> leafTriangles = new List<int>();

		public Mesh branchMesh, leafMesh;
    }


}