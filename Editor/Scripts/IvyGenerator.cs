using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Hedera
{
    [CreateAssetMenu(fileName = "NewIvyGenerator", menuName = "Ivy Generator (Hedera)", order = 1)]
    public class IvyGenerator : ScriptableObject
    {
        //public IvyGraph ivyGraph = new IvyGraph();

        public List<IvyGraph> ivyGraphs = new List<IvyGraph>();

        // materials
        public IvyProfile ivyProfile;

        public void GenerateMesh(IvyGraph ivyGraph)
        {
            GenerateMeshData(ivyGraph);

            GameObject rootObj = new GameObject("IvyGenObject");

            // Branch mesh
            Mesh _branchMesh = CreateIvyMeshObject(rootObj, "IvyBranch_", 0, ivyProfile.branchMaterial);
            _branchMesh.SetVertices( ivyGraph.vertices );
            _branchMesh.SetUVs( 0, ivyGraph.texCoords );
            _branchMesh.SetTriangles( ivyGraph.triangles, 0);
            _branchMesh.RecalculateBounds();
            _branchMesh.RecalculateNormals();
            _branchMesh.RecalculateTangents();
            
            // Leaves mesh
            Mesh _leavesMesh = CreateIvyMeshObject(rootObj, "IvyLeaves_", 0, ivyProfile.leafMaterial);
            _leavesMesh.SetVertices( ivyGraph.leafVertices );
            _leavesMesh.SetUVs( 0, ivyGraph.leafUVs );
            _leavesMesh.SetTriangles( ivyGraph.leafTriangles, 0);
            _leavesMesh.RecalculateBounds();
            _leavesMesh.RecalculateNormals();
            _leavesMesh.RecalculateTangents();
        }

        void GenerateMeshData(IvyGraph ivyGraph) {
	        //evolve a gaussian filter over the adhesion vectors
	        float [] gaussian = {1.0f, 2.0f, 4.0f, 7.0f, 9.0f, 10.0f, 9.0f, 7.0f, 4.0f, 2.0f, 1.0f }; 
	
	        foreach (var root in ivyGraph.roots) {
		        for (int g = 0; g < 5; ++g) {
			        for (int node = 0; node < root.nodes.Count; node++) {
				        Vector3 e = Vector3.zero;

				        for (int i = -5; i <= 5; ++i) {
					        Vector3 tmpAdhesion = Vector3.zero;

					        if ((node + i) < 0) tmpAdhesion = root.nodes[0].adhesionVector;
					        if ((node + i) >= root.nodes.Count) tmpAdhesion = root.nodes[root.nodes.Count-1].adhesionVector;
					        if (((node + i) >= 0) && ((node + i) < root.nodes.Count)) tmpAdhesion = root.nodes[node + i].adhesionVector;

					        e += tmpAdhesion * gaussian[i+5];
				        }

				       root.nodes[node].smoothAdhesionVector = e / 56.0f;
			        }

			        foreach (var _node in root.nodes) {
				        _node.adhesionVector = _node.smoothAdhesionVector;
			        }
		        }
	        }

            var p = ivyProfile;

	        //reset existing geometry
	        ivyGraph.ResetMeshData();

	        //create leafs
	        foreach (var root in ivyGraph.roots)
	        {
		        //simple multiplier, just to make it a more dense
		        for (int i = 0; i < 10; ++i)
		        {
			        //srand(i + (root - roots.begin()) * 10);

			        foreach (var node in root.nodes)
			        {
                        IvyNode back_node = root.nodes[root.nodes.Count - 1];
				        //weight depending on ratio of node length to total length
				        float weight = Mathf.Pow(node.length / back_node.length, 0.7f);

				        //test: the probability of leaves on the ground is increased
				        float groundIvy = Mathf.Max(0.0f, -Vector3.Dot( new Vector3(0.0f, 1.0f, 0.0f), node.adhesionVector.normalized ));
				        weight += groundIvy * Mathf.Pow(1.0f - node.length / back_node.length, 2.0f);
				
				        //random influence
				        float probability = Random.value;

				        if (probability * weight > p.leafProbability)
				        {
					        //alignment weight depends on the adhesion "strength"
					        float alignmentWeight = node.adhesionVector.magnitude;

					        //horizontal angle (+ an epsilon vector, otherwise there's a problem at 0?and 90?.. mmmh)
					        float phi = Vector2ToPolar( new Vector2(node.adhesionVector.z, node.adhesionVector.x).normalized  + new Vector2(Vector2.kEpsilon, Vector2.kEpsilon) ) - Mathf.PI * 0.5f;

					        //vertical angle, trimmed by 0.5
					        float theta = Vector3.Angle( node.adhesionVector, new Vector3(0.0f, -1.0f, 0.0f) ) * 0.5f;

					        //center of leaf quad
					        Vector3 center = node.pos + Random.onUnitSphere * p.ivySize * p.ivyLeafSize;

					        //size of leaf
					        float sizeWeight = 1.5f - (Mathf.Cos(weight * 2.0f * Mathf.PI) * 0.5f + 0.5f);

					        //random influence
					        phi += Random.Range(-0.5f, 0.5f) * (1.3f - alignmentWeight);
					        theta += Random.Range(-0.5f, 0.5f) * (1.1f - alignmentWeight);

					        //create vertices
                            float leafSize = p.ivySize * p.ivyLeafSize * sizeWeight;
							AddLeafVertex(ivyGraph, center, new Vector3(-1f, 0f, 1f), leafSize, phi, theta);
							AddLeafVertex(ivyGraph, center, new Vector3(1f, 0f, 1f), leafSize, phi, theta);
							AddLeafVertex(ivyGraph, center, new Vector3(-1f, 0f, -1f), leafSize, phi, theta);
							AddLeafVertex(ivyGraph, center, new Vector3(1f, 0f, -1f), leafSize, phi, theta);

					        //create texCoords
							ivyGraph.leafUVs.Add( new Vector2( 0.0f, 1.0f) );
					        ivyGraph.leafUVs.Add( new Vector2( 1.0f, 1.0f) );
					        ivyGraph.leafUVs.Add( new Vector2( 0.0f, 0.0f) );
					        ivyGraph.leafUVs.Add( new Vector2( 1.0f, 0.0f) );

					        //create triangle
					        // BasicTriangle tmpTriangle = new BasicTriangle();
					        // tmpTriangle.matid = 1;

					        // float _probability = Random.value;
					        // if (_probability * weight > leafProbability) tmpTriangle.matid = 2;

					        AddTriangle(ivyGraph, 1,3,2, ivyGraph.leafTriangles);
					        AddTriangle(ivyGraph, 2,0,1, ivyGraph.leafTriangles);
				        }
			        }
		        }
	        }

	        //branches
            float local_ivyBranchSize = p.ivySize * p.ivyBranchSize;
	        foreach (var root in ivyGraph.roots)
	        {
		        //process only roots with more than one node
		        if (root.nodes.Count == 1) continue;

		        //branch diameter depends on number of parents
		        float local_ivyBranchDiameter = 1.0f / (float)(root.parents + 1) + 1.0f;

                for (int node = 0; node < root.nodes.Count - 1; node++)
		        {
			        //weight depending on ratio of node length to total length
			        float weight = root.nodes[node].length / root.nodes[root.nodes.Count-1].length;

			        //create trihedral vertices
			        Vector3 up = new Vector3(0.0f, -1.0f, 0.0f);
			        Vector3 basis = (root.nodes[node + 1].pos - root.nodes[node].pos).normalized;

			        Vector3 b0 = Vector3.Cross(up, basis).normalized * local_ivyBranchDiameter * local_ivyBranchSize * (1.3f - weight) + root.nodes[node].pos;
			        Vector3 b1 = RotateAroundAxis(b0, root.nodes[node].pos, basis, 2.09f);
			        Vector3 b2 = RotateAroundAxis(b0, root.nodes[node].pos, basis, 4.18f);

			        //create vertices
			        ivyGraph.vertices.Add( b0 );
			        ivyGraph.vertices.Add( b1 );
			        ivyGraph.vertices.Add( b2 );

			        //create texCoords
			        float texV = (node % 2 == 0 ? 1.0f : 0.0f);
			        ivyGraph.texCoords.Add( new Vector2(0.0f, texV) );
			        ivyGraph.texCoords.Add( new Vector2(0.3f, texV) );
			        ivyGraph.texCoords.Add( new Vector2(0.6f, texV) );

			        if (node == 0) continue;

			        AddTriangle(ivyGraph, 3,0,4);
                    AddTriangle(ivyGraph, 4,0,1);
                    AddTriangle(ivyGraph, 4,1,5);
                    AddTriangle(ivyGraph, 5,1,2);
                    AddTriangle(ivyGraph, 5,2,0);
                    AddTriangle(ivyGraph, 5,0,3);
		        }
	        }

        }

		void AddLeafVertex(IvyGraph ivyGraph, Vector3 center, Vector3 offsetScalar, float local_ivyLeafSize, float phi, float theta) {
			var tmpVertex = Vector3.zero;
			tmpVertex = center + Vector3.Scale( new Vector3(local_ivyLeafSize, 0.0f, local_ivyLeafSize), offsetScalar);
			tmpVertex = RotateAroundAxis(tmpVertex, center, new Vector3(0.0f, 0.0f, 1.0f), theta);					
			tmpVertex = RotateAroundAxis(tmpVertex, center, new Vector3(0.0f, 1.0f, 0.0f), phi);
			tmpVertex += Random.onUnitSphere * local_ivyLeafSize * 0.5f;
			ivyGraph.leafVertices.Add( tmpVertex );
		}

        void AddTriangle(IvyGraph ivyGraph, int offset1, int offset2, int offset3, List<int> triangleList = null) {
            if ( triangleList == null ) {
                triangleList = ivyGraph.triangles;
            }
            triangleList.Add(ivyGraph.vertices.Count - offset1);
            triangleList.Add(ivyGraph.vertices.Count - offset2);
            triangleList.Add(ivyGraph.vertices.Count - offset3);
        }

	    private float Vector2ToPolar( Vector2 vector )
	    {
		    float phi = (vector.x == 0.0f) ? 0.0f : Mathf.Atan( vector.y / vector.x );

		    if ( vector.x < 0.0f )
		    {
			    phi += Mathf.PI;
		    }
		    else
		    {
			    if ( vector.y < 0.0f )
			    {
				    phi += 2.0f * Mathf.PI;
			    }
		    }

		    return phi;
	    }

	    private Vector3 RotateAroundAxis( Vector3 vector, Vector3 axisPosition, Vector3 axis, float angle )
	    {
		    //determining the sinus and cosinus of the rotation angle
		    float cosTheta = Mathf.Cos(angle);
            float sinTheta = Mathf.Sin(angle);

		    //Vector3 from the given axis point to the initial point
		    Vector3 direction = vector - axisPosition;

		    //new vector which will hold the direction from the given axis point to the new rotated point 
		    Vector3 newDirection = Vector3.zero;

		    //x-component of the direction from the given axis point to the rotated point
		    newDirection.x = ( cosTheta + ( 1 - cosTheta ) * axis.x * axis.x ) * direction.x +
						     ( ( 1 - cosTheta ) * axis.x * axis.y - axis.z * sinTheta ) * direction.y +
						     ( ( 1 - cosTheta ) * axis.x * axis.z + axis.y * sinTheta ) * direction.z;

		    //y-component of the direction from the given axis point to the rotated point
		    newDirection.y = ( ( 1 - cosTheta ) * axis.x * axis.y + axis.z * sinTheta ) * direction.x +
						     ( cosTheta + ( 1 - cosTheta ) * axis.y * axis.y ) * direction.y +
						     ( ( 1 - cosTheta ) * axis.y * axis.z - axis.x * sinTheta ) * direction.z;

		    //z-component of the direction from the given axis point to the rotated point
		    newDirection.z = ( ( 1 - cosTheta ) * axis.x * axis.z - axis.y * sinTheta ) * direction.x +
						     ( ( 1 - cosTheta ) * axis.y * axis.z + axis.x * sinTheta ) * direction.y +
						     ( cosTheta + ( 1 - cosTheta ) * axis.z * axis.z) * direction.z;

		    //returning the result by addind the new direction vector to the given axis point
		    return axisPosition + newDirection;
	    }


        Mesh CreateIvyMeshObject(GameObject rootObj, string ObjName, int ObjIdx, Material mat)
        {
            var PartObj = new GameObject(ObjName + ObjIdx.ToString());
            PartObj.transform.parent = rootObj.transform;

            var mf = PartObj.AddComponent<MeshFilter>();
            var mr = PartObj.AddComponent<MeshRenderer>();

            Mesh _PartMesh = new Mesh();
            _PartMesh.name = ObjName + ObjIdx.ToString();
            mf.mesh = _PartMesh;

            mr.material = mat;

            return _PartMesh;
        }


    }


}
