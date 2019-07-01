using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hedera
{
	[System.Serializable]
    public class IvyProfile {
		#if UNITY_EDITOR

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

	        branchingProbability = 0.15f;

	        ivySize = 0.2f;

	        ivyLeafSize = 0.1f;
	        ivyBranchSize = 0.01f;
			leafProbability = 0.5f;

	        maxFloatLength = 0.1f;
	        maxAdhesionDistance = 0.1f;

			collisionMask = Physics.DefaultRaycastLayers;
			branchMaterial = null;
			leafMaterial = null;
        }

		#endif
    }

	[System.Serializable]
    public class IvyNode
    {
		
		#if UNITY_EDITOR
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

		#endif
    }

	[System.Serializable]
    public class IvyRoot
    {
		#if UNITY_EDITOR

	    public List<IvyNode> nodes = new List<IvyNode>();
	    public bool isAlive;

	    /** number of parents, represents the level in the root hierarchy */
	    public int parents;
		
		// there's a big flaw in the old algorithm or port, where roots will keep getting generated infinitely... childCount helps us kill off roots with too many children
		public int childCount;

		#endif
    }

	[System.Serializable]
    public class IvyGraph
    {
		#if UNITY_EDITOR

		public bool isGrowing = false;
		public Vector3 seedPos;
		public bool generateMeshDuringGrowth = false;
		
		public List<Vector3> debugLineSegmentsList = new List<Vector3>();
		public Vector3[] debugLineSegmentsArray;

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
		public GameObject rootGO;
		public MeshFilter branchMF, leafMF;

		public void ResetMeshData()
        {
	        vertices.Clear();
            texCoords.Clear();
            triangles.Clear();
			leafVertices.Clear();
			leafUVs.Clear();
			leafTriangles.Clear();
			branchMesh = null;
			leafMesh = null;
        }

		#endif
    }


}