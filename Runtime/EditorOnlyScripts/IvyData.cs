using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Hedera
{
	[System.Serializable]
    public class IvyProfile {
		#if UNITY_EDITOR

		public bool showAdvanced, showGrowthFoldout=true, showAIFoldout=true, showMeshFoldout=true;

		public LayerMask collisionMask = -5;
        public Material branchMaterial, leafMaterial;

	    public float ivyStepDistance = 0.1f;
	    public float ivyLeafSize = 0.15f;
	    public float ivyBranchSize = 0.05f;

        /** maximum length of an ivy branch segment that is freely floating [0..1] */
	    public float maxFloatLength = 1;

	    /** maximum distance for adhesion of scene object [0..1] */
	    public float maxAdhesionDistance = 1f;

		// force the ivy root to grow to at least this length
		public float minLength = 0.5f, maxLength = 5f;
		// public int maxBranchesPerRoot = 2;
		public int maxBranchesTotal = 64;

	    public float primaryWeight = 0.5f;
	    public float randomWeight = 1f;
	    public float gravityWeight = 3;
	    public float adhesionWeight = 1f;

	    public float branchingProbability = 0.25f;
	    public float leafProbability = 0.5f;
		public float leafSunlightBonus = 1f;
		public float branchOptimize = 0.5f;
		public int branchSmooth = 2;

		public float branchTaper = 1f;

		public string namePrefix = "Ivy[{0}]{1}";
		public bool markMeshAsStatic = true;
		public bool useLightmapping = false;
		public enum MeshCompression {None, Low, Medium, High} // ModelImporterMeshCompression is in editor space...
		public MeshCompression meshCompress;
		public UnityEngine.Rendering.ShadowCastingMode castShadows = UnityEngine.Rendering.ShadowCastingMode.On;
		public bool receiveShadows = true;

		public bool useVertexColors = true;
		public Gradient leafVertexColors = new Gradient();

		public IvyProfile() {
			ResetSettings();
		}

	    public void ResetSettings()
        {
	        primaryWeight = 0.5f;
	        randomWeight = 1f;
	        gravityWeight = 3f;
	        adhesionWeight = 1f;

	        branchingProbability = 0.25f;

	        ivyStepDistance = 0.1f;

	        ivyLeafSize = 0.15f;
	        ivyBranchSize = 0.05f;
			leafProbability = 0.5f;
			leafSunlightBonus = 1f;
	
			branchOptimize = 0.5f;
			branchSmooth = 2;
			branchTaper = 1f;

	        maxFloatLength = 1f;
	        maxAdhesionDistance = 1f;
			// maxBranchesPerRoot = 2;
			maxBranchesTotal = 64;

			minLength = 1f;
			maxLength = 3f;

			namePrefix = "Ivy[{0}]{1}";
			markMeshAsStatic = true;
			useLightmapping = false;
			meshCompress = MeshCompression.Low;
			collisionMask = Physics.DefaultRaycastLayers;
			castShadows = UnityEngine.Rendering.ShadowCastingMode.On;
			receiveShadows = true;

			useVertexColors = true;
			leafVertexColors = new Gradient();
			leafVertexColors.SetKeys( new GradientColorKey[] { 
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.green, 0.68f),
				new GradientColorKey(Color.yellow, 1f )
			}, 
			new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f)} );

			// branchMaterial = null;
			//leafMaterial = null;
        }

		#endif
    }

	[System.Serializable]
    public class IvyNode
    {
		#if UNITY_EDITOR
		// renamed variables to be very short, to save on serialization file size
		// sorry in advance

		/// <summary>node's local position, relative to graph's seedPos</summary>
	    public Vector3 p;			

	    /// <summary>primary grow direction, weighted sum of the previous directions</summary>
	    public Vector3 g;

		/// <summary>"cling", adhesion vector, points inwards towards clinging surface</summary>
	    public Vector3 c;

		/// <summary>local length on this root so far</summary>
	    public float s;

		/// <summary>cumulative length at this node, across all roots... but note that when painting a stroke, even later branches will start with cumulative length of 0</summary>
		public float cS;

	    /// <summary>float length at the last node that was climbing</summary>
	    public float fS;

		/// <summary>is node climbing / clinging?</summary>
	    public bool cl;

		#endif
    }

	[System.Serializable]
    public class IvyRoot
    {
		#if UNITY_EDITOR

		// mesh cache for each root
        static Dictionary<long, IvyRootMeshCache> meshCache = new Dictionary<long, IvyRootMeshCache>(64);

        public static IvyRootMeshCache Get( long cacheID ) {
            if ( meshCache.ContainsKey(cacheID) ) {
                return meshCache[cacheID];
            } else {
                var newCache = new IvyRootMeshCache();
                meshCache.Add( cacheID, newCache );
                return newCache;
            }
        }

		public static IvyRootMeshCache GetMeshCacheFor( IvyRoot root ) {
			return Get( root.cacheID );
		}

	    public List<IvyNode> nodes = new List<IvyNode>();
	    public bool isAlive;

	    /** number of parents, represents the level in the root hierarchy */
	    public int parents;
		
		// there's a big flaw in the old algorithm or port, where roots will keep getting generated infinitely... childCount helps us kill off roots with too many children
		public int childCount;

		public float forceMinLength = -1f;

		public long cacheID;

		// public int meshSegments;
		// public List<Vector3> leafPoints = new List<Vector3>(64);
		// public bool useCachedBranchData = false, useCachedLeafData = false;

		// public List<Vector3> vertices = new List<Vector3>(128);
	    // public List<Vector2> texCoords = new List<Vector2>(128);
	    // public List<int> triangles = new List<int>(1024);

		// public List<Vector3> leafVertices = new List<Vector3>(128);
		// public List<Vector2> leafUVs = new List<Vector2>(128);
		// public List<int> leafTriangles = new List<int>(512);
		// public List<Color> leafVertexColors = new List<Color>(128);

		public int meshSegments {
			get { return Get(cacheID).meshSegments; }
			set { Get(cacheID).meshSegments = value; }
		}
		// public List<Vector3> leafPoints {
		// 	get { return Get(cacheID).leafPoints; }
		// 	// set { Get(this).leafPoints = value; }
		// }

		public bool useCachedBranchData = false, useCachedLeafData = false;

		// public List<Vector3> vertices {
		// 	get { return Get(cacheID).vertices; }
		// 	// set { Get(this).vertices = value; }
		// }
	    // public List<Vector2> texCoords {
		// 	get { return Get(cacheID).texCoords; }
		// 	// set { Get(this).texCoords = value; }
		// }
	    // public List<int> triangles {
		// 	get { return Get(cacheID).triangles; }
		// }

		// public List<Vector3> leafVertices {
		// 	get { return Get(cacheID).leafVertices; }
		// }

		// public List<Vector2> leafUVs {
		// 	get { return Get(cacheID).leafUVs; }
		// }
		// public List<int> leafTriangles {
		// 	get { return Get(cacheID).leafTriangles; }
		// }
		// public List<Color> leafVertexColors {
		// 	get { return Get(cacheID).leafVertexColors; }
		// }

		// public List<Vector3> debugLineSegmentsList {
		// 	get { return Get(cacheID).debugLineSegmentsList; }
		// }

		public Vector3[] debugLineSegmentsArray {
			get { return Get(cacheID).debugLineSegmentsArray; }
			set { Get(cacheID).debugLineSegmentsArray = value; }
		}

		static System.Random rand = new System.Random();
		public static long GetRandomLong() {
			byte[] buffer = new byte[8];
      		rand.NextBytes (buffer);
			return System.BitConverter.ToInt64(buffer, 0);
		}

		public IvyRoot() {
      		cacheID = GetRandomLong();
		}

		#endif
    }

	public class IvyRootMeshCache {
		#if UNITY_EDITOR
		public int meshSegments;
		public List<Vector3> leafPoints = new List<Vector3>(64);
		// public bool useCachedBranchData = false, useCachedLeafData = false;

		public List<Vector3> vertices = new List<Vector3>(128);
	    public List<Vector2> texCoords = new List<Vector2>(128);
	    public List<int> triangles = new List<int>(1024);

		public List<Vector3> leafVertices = new List<Vector3>(128);
		public List<Vector2> leafUVs = new List<Vector2>(128);
		public List<int> leafTriangles = new List<int>(512);
		public List<Color> leafVertexColors = new List<Color>(128);

		public List<Vector3> debugLineSegmentsList = new List<Vector3>(512);
		public Vector3[] debugLineSegmentsArray;

		#endif
	}

	[System.Serializable]
    public class IvyGraph
    {
		#if UNITY_EDITOR

		public bool isGrowing = false;
		public bool isVisible = true;
		public bool dirtyUV2s = false;
		public Vector3 seedPos, seedNormal = Vector3.up;
		public bool generateMeshDuringGrowth = false;
		
		 // ivy roots
	    [HideInInspector] public List<IvyRoot> roots = new List<IvyRoot>(8);	

		public long branchMeshID = 0, leafMeshID = 0;
		public Transform rootBehavior;
		public GameObject rootGO;
		public MeshFilter branchMF, leafMF;
		public Renderer branchR, leafR;

		public IvyGraph () {
			branchMeshID = IvyRoot.GetRandomLong();
			leafMeshID = IvyRoot.GetRandomLong();
		}

		#endif
    }


}