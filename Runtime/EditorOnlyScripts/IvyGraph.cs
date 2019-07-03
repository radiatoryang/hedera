using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hedera
{
	[System.Serializable]
    public class IvyProfile {
		#if UNITY_EDITOR

		public LayerMask collisionMask = -5;
        public Material branchMaterial, leafMaterial;

	    public float ivyStepDistance = 0.15f;
	    public float ivyLeafSize = 0.1f;
	    public float ivyBranchSize = 0.01f;

        /** maximum length of an ivy branch segment that is freely floating [0..1] */
	    public float maxFloatLength = 0.2f;

	    /** maximum distance for adhesion of scene object [0..1] */
	    public float maxAdhesionDistance = 0.25f;

		// force the ivy root to grow to at least this length
		public float minLength = 1f, maxLength = 5f;
		public int maxBranchesPerRoot = 6;

	    public float primaryWeight = 1f;
	    public float randomWeight = 0.5f;
	    public float gravityWeight = 1f;
	    public float adhesionWeight = 0.69f;

	    public float branchingProbability = 0.15f;
	    public float leafProbability = 0.5f;

		public IvyProfile() {
			ResetSettings();
		}

	    public void ResetSettings()
        {
	        primaryWeight = 1f;
	        randomWeight = 0.5f;
	        gravityWeight = 1f;
	        adhesionWeight = 0.69f;

	        branchingProbability = 0.15f;

	        ivyStepDistance = 0.15f;

	        ivyLeafSize = 0.1f;
	        ivyBranchSize = 0.01f;
			leafProbability = 0.5f;

	        maxFloatLength = 0.2f;
	        maxAdhesionDistance = 0.25f;
			maxBranchesPerRoot = 6;

			minLength = 1f;
			maxLength = 5f;

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
		public float lengthCumulative;

	    /** length at the last node that was climbing */
	    public float floatingLength;

	    public bool isClimbing;

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
		public Transform rootBehavior;
		public GameObject rootGO;
		public MeshFilter branchMF, leafMF;
		public Renderer branchR, leafR;

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