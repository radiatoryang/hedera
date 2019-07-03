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

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Hedera
{
    public class IvyMesh
    {
        public static void GenerateMesh(IvyGraph ivyGraph, IvyProfile ivyProfile)
        {
            if ( !GenerateMeshData(ivyGraph, ivyProfile) ) {
                return;
            }

            if ( ivyGraph.rootGO == null ) {
                ivyGraph.rootGO = new GameObject("Ivy");
                ivyGraph.rootGO.isStatic = true;
                ivyGraph.rootGO.transform.SetParent( ivyGraph.rootBehavior );
            }
            ivyGraph.rootGO.name = string.Format("IvyMesh[{0}]_{1}", ivyGraph.roots.Count, ivyGraph.seedPos);

            // Branch mesh
            // Debug.Log( "branchVertices: " + ivyGraph.vertices.Count );
            // Debug.Log( "branchTris: " + string.Join(", ", ivyGraph.triangles.Select( tri => tri.ToString() ).ToArray()) );

            if ( ivyGraph.branchMesh == null) {
                ivyGraph.branchMesh = new Mesh();
            }
            if ( ivyGraph.branchMF == null || ivyGraph.branchR == null) {
                CreateIvyMeshObject(ivyGraph.rootGO, ivyGraph, ivyGraph.branchMesh, false);
            }
            ivyGraph.branchMesh.Clear();
            ivyGraph.branchMesh.name = "Ivy" + ivyGraph.seedPos + "_Branches";
            ivyGraph.branchMesh.SetVertices(ivyGraph.vertices);
            ivyGraph.branchMesh.SetUVs(0, ivyGraph.texCoords);
            ivyGraph.branchMesh.SetTriangles(ivyGraph.triangles, 0);
            ivyGraph.branchMesh.RecalculateBounds();
            ivyGraph.branchMesh.RecalculateNormals();
            ivyGraph.branchMesh.RecalculateTangents();
            ivyGraph.branchMF.sharedMesh = ivyGraph.branchMesh;
            ivyGraph.branchR.sharedMaterial = ivyProfile.branchMaterial;
            
            // Leaves mesh
            // Debug.Log( "leafVertices: " + ivyGraph.leafVertices.Count );
            // Debug.Log( "leafTris: " + string.Join(", ", ivyGraph.leafTriangles.Select( tri => tri.ToString() ).ToArray()) );

            if ( ivyGraph.leafMesh == null) {
                ivyGraph.leafMesh = new Mesh();
            }
            if ( ivyGraph.leafMF == null || ivyGraph.leafR == null) {
                CreateIvyMeshObject(ivyGraph.rootGO, ivyGraph, ivyGraph.leafMesh, true);
            } 
            ivyGraph.leafMesh.Clear();
            ivyGraph.leafMesh.name = "Ivy" + ivyGraph.seedPos + "_Leaves";
            if ( ivyProfile.ivyLeafSize > 0.0001f && ivyProfile.leafProbability > 0.0001f ) {
                ivyGraph.leafMesh.SetVertices(ivyGraph.leafVertices);
                ivyGraph.leafMesh.SetUVs(0, ivyGraph.leafUVs);
                ivyGraph.leafMesh.SetTriangles(ivyGraph.leafTriangles, 0);
                ivyGraph.leafMesh.RecalculateBounds();
                ivyGraph.leafMesh.RecalculateNormals();
                ivyGraph.leafMesh.RecalculateTangents();
                ivyGraph.leafMF.sharedMesh = ivyGraph.leafMesh;
                ivyGraph.leafR.sharedMaterial = ivyProfile.leafMaterial;
            } else if ( ivyGraph.leafMF != null ) {
                Object.DestroyImmediate( ivyGraph.leafMF.gameObject );
            }

        }

        static bool GenerateMeshData(IvyGraph ivyGraph, IvyProfile ivyProfile)
        {
            int nodeCount = 0;
            //evolve a gaussian filter over the adhesion vectors
            float[] gaussian = { 1.0f, 2.0f, 4.0f, 7.0f, 9.0f, 10.0f, 9.0f, 7.0f, 4.0f, 2.0f, 1.0f };

            foreach (var root in ivyGraph.roots)
            {
                for (int g = 0; g < 5; ++g)
                {
                    for (int node = 0; node < root.nodes.Count; node++)
                    {
                        nodeCount++;
                        Vector3 e = Vector3.zero;

                        for (int i = -5; i <= 5; ++i)
                        {
                            Vector3 tmpAdhesion = Vector3.zero;

                            if ((node + i) < 0) tmpAdhesion = root.nodes[0].adhesionVector;
                            if ((node + i) >= root.nodes.Count) tmpAdhesion = root.nodes[root.nodes.Count - 1].adhesionVector;
                            if (((node + i) >= 0) && ((node + i) < root.nodes.Count)) tmpAdhesion = root.nodes[node + i].adhesionVector;

                            e += tmpAdhesion * gaussian[i + 5];
                        }

                        root.nodes[node].smoothAdhesionVector = e / 56.0f;
                    }

                    foreach (var _node in root.nodes)
                    {
                        _node.adhesionVector = _node.smoothAdhesionVector;
                    }
                }
            }

            if ( nodeCount < 2 ) {
                return false;
            }

            var p = ivyProfile;

            //reset existing geometry
            ivyGraph.ResetMeshData();

            //branches
            foreach (var root in ivyGraph.roots)
            {
                //process only roots with more than one node
                if (root.nodes.Count == 1) continue;

                //branch diameter depends on number of parents
                float local_ivyBranchDiameter = 1.0f / (float)(root.parents + 1) + 0.75f;

                // generate simplified points for each root, to make it less wavy AND save tris
                var allPoints = root.nodes.Select( node => node.pos).ToList();
                var newPoints = allPoints.ToArray().ToList();
                if ( !root.isAlive ) {
                    LineUtility.Simplify( allPoints, 0.04f, newPoints);
                }
                // hack: make sure the last point is still there
                if ( Vector3.Distance(newPoints.Last(), allPoints.Last()) > 0.01f ) {
                    newPoints.Add( allPoints.Last() );
                }

                for (int node=0; node < newPoints.Count-1; node++)
                {
                    //weight depending on ratio of node length to total length
                    float weight = 1f * node / newPoints.Count;

                    //create trihedral vertices
                    Vector3 up = Vector3.down;
                    Vector3 basis = (newPoints[node + 1] - newPoints[node]).normalized;

                    Vector3 b0 = Vector3.Cross(up, basis).normalized * Mathf.Max(0.001f, local_ivyBranchDiameter * p.ivyBranchSize * 5f * (1f - weight) * weight) + newPoints[node];
                    Vector3 b1 = RotateAroundAxis(b0, newPoints[node], basis, 2.09f);
                    Vector3 b2 = RotateAroundAxis(b0, newPoints[node], basis, 4.18f);

                    //create vertices
                    ivyGraph.vertices.Add(b0);
                    ivyGraph.vertices.Add(b1);
                    ivyGraph.vertices.Add(b2);

                    //create texCoords
                    float texV = (node % 2 == 0 ? 1.0f : 0.0f);
                    ivyGraph.texCoords.Add(new Vector2(0.0f, texV));
                    ivyGraph.texCoords.Add(new Vector2(0.3f, texV));
                    ivyGraph.texCoords.Add(new Vector2(0.6f, texV));

                    if (node == 0) continue;

                    AddTriangle(ivyGraph, 4, 1, 5);
                    AddTriangle(ivyGraph, 5, 1, 2);
                    AddTriangle(ivyGraph, 5, 2, 6);
                    AddTriangle(ivyGraph, 6, 2, 3);
                    AddTriangle(ivyGraph, 6, 3, 1);
                    AddTriangle(ivyGraph, 6, 1, 4);
                }
            }

            if ( ivyProfile.ivyLeafSize <= 0.001f || ivyProfile.leafProbability <= 0.001f) {
                return true;
            }

            //create leafs
            List<Vector3> leafPoints = new List<Vector3>();
            foreach (var root in ivyGraph.roots)
            {
                // don't bother on small roots
                if ( root.nodes.Count <= 2 ) {
                    continue;
                }

                //simple multiplier, just to make it a more dense
                for (int i = 0; i < 1; ++i)
                {
                    //srand(i + (root - roots.begin()) * 10);

                    for(int n=0; n<root.nodes.Count; n++)
                    {
                        var node = root.nodes[n];
                        // do not generate a leaf on the first few or last few nodes
                        if ( n <= 1 || n >= root.nodes.Count-2) {
                            continue;
                        }

                        //test: the probability of leaves on the ground is increased
                        float groundedness = Mathf.Max(0.0f, -Vector3.Dot(new Vector3(0.0f, 1.0f, 0.0f), node.adhesionVector.normalized));
                        groundedness *= 0.25f * p.leafProbability;
                        // * Mathf.Pow(1.0f - node.length / back_node.length, 2.0f);

                        // don't spawn a leaf on top of another leaf
                        float leafSqrSize = p.ivyLeafSize * p.ivyLeafSize * Mathf.Clamp01(1f - p.leafProbability - groundedness);
                        if ( leafPoints.Where( leafPos => (leafPos - node.pos).sqrMagnitude < leafSqrSize).Count() > 0 ) {
                            continue;
                        }

                        IvyNode back_node = root.nodes[root.nodes.Count - 1];

                        if (Random.value + groundedness > 1f - p.leafProbability)
                        {
                            leafPoints.Add( node.pos );

                            //alignment weight depends on the adhesion "strength"
                            // float alignmentWeight = node.adhesionVector.magnitude;

                            // //horizontal angle (+ an epsilon vector, otherwise there's a problem at 0?and 90?.. mmmh)
                            // float phi = Vector2ToPolar(new Vector2(node.smoothAdhesionVector.z, node.smoothAdhesionVector.x).normalized + new Vector2(Vector2.kEpsilon, Vector2.kEpsilon)) - Mathf.PI * 0.5f;

                            // //vertical angle, trimmed by 0.5
                            // float theta = Vector3.Angle(node.smoothAdhesionVector, new Vector3(0.0f, -1.0f, 0.0f)) * 0.5f;

                            //center of leaf quad
                            Vector3 up = (node.pos - back_node.pos).normalized;
                            Vector3 right = Vector3.Cross( up, node.adhesionVector );
                            Vector3 center = node.pos - node.adhesionVector * 0.05f + (up * Random.Range(-1f, 1f) + right * Random.Range(-1f, 1f) ) * 0.25f * p.ivyLeafSize;

                            //size of leaf
                            float sizeWeight = 1.5f - ( Mathf.Abs(Mathf.Cos(2.0f * Mathf.PI)) * 0.5f + 0.5f);

                            //random influence for rotations (see AddLeafVertex)
                            // phi += Random.Range(-0.5f, 0.5f) * (1.3f - alignmentWeight);
                            // theta += Random.Range(-0.5f, 0.5f) * (1.1f - alignmentWeight);

                            float leafSize = p.ivyLeafSize * sizeWeight;
                            // AddLeafVertex(ivyGraph, center, new Vector3(-1f, 0f, 1f), leafSize, phi, theta);
                            // AddLeafVertex(ivyGraph, center, new Vector3(1f, 0f, 1f), leafSize, phi, theta);
                            // AddLeafVertex(ivyGraph, center, new Vector3(-1f, 0f, -1f), leafSize, phi, theta);
                            // AddLeafVertex(ivyGraph, center, new Vector3(1f, 0f, -1f), leafSize, phi, theta);

                            Quaternion facing = node.adhesionVector.sqrMagnitude < 0.001f ? Quaternion.identity : Quaternion.LookRotation( -node.adhesionVector, Random.onUnitSphere);
                            AddLeafVertex(ivyGraph, center, new Vector3(-1f, 1f, 0f), leafSize, facing);
                            AddLeafVertex(ivyGraph, center, new Vector3(1f, 1f, 0f), leafSize, facing);
                            AddLeafVertex(ivyGraph, center, new Vector3(-1f, -1f, 0f), leafSize, facing);
                            AddLeafVertex(ivyGraph, center, new Vector3(1f, -1f, 0f), leafSize, facing);

                            ivyGraph.leafUVs.Add(new Vector2(1.0f, 1.0f));
                            ivyGraph.leafUVs.Add(new Vector2(0.0f, 1.0f));
                            ivyGraph.leafUVs.Add(new Vector2(1.0f, 0.0f));
                            ivyGraph.leafUVs.Add(new Vector2(0.0f, 0.0f));

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
                                    AddLeafTriangle(ivyGraph, 1, 3, 4);
                                    AddLeafTriangle(ivyGraph, 4, 2, 1);
                            // }
                        }
                    }
                }
            }
            return true;
        }

        static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            // Find vectors corresponding to two of the sides of the triangle.
            Vector3 side1 = b - a;
            Vector3 side2 = c - a;

            // Cross the vectors to get a perpendicular vector, then normalize it.
            return Vector3.Cross(side1, side2).normalized;
        }

        // static void AddLeafVertex(IvyGraph ivyGraph, Vector3 center, Vector3 offsetScalar, float local_ivyLeafSize, float phi, float theta)
        // {
        //     var tmpVertex = Vector3.zero;
        //     tmpVertex = center + Vector3.Scale(new Vector3(local_ivyLeafSize, 0.0f, local_ivyLeafSize), offsetScalar);
        //     tmpVertex = RotateAroundAxis(tmpVertex, center, new Vector3(0.0f, 0.0f, 1.0f), theta);
        //     tmpVertex = RotateAroundAxis(tmpVertex, center, new Vector3(0.0f, 1.0f, 0.0f), phi);
        //     tmpVertex += Random.onUnitSphere * local_ivyLeafSize * 0.5f;
        //     ivyGraph.leafVertices.Add(tmpVertex);
        // }

        static void AddLeafVertex(IvyGraph ivyGraph, Vector3 center, Vector3 offsetScalar, float ivyLeafSize, Quaternion facing )
        {
            var tmpVertex = Vector3.zero;
            tmpVertex = center + ivyLeafSize * offsetScalar;
            tmpVertex = facing * (tmpVertex - center) + center; // thank you "The Pirate Duck" https://forum.unity.com/threads/rotate-vertices-around-pivot.124131/
            tmpVertex += Random.onUnitSphere * ivyLeafSize * 0.5f;
            ivyGraph.leafVertices.Add(tmpVertex);
        }

        static void AddLeafTriangle(IvyGraph ivyGraph, int offset1, int offset2, int offset3)
        {
            ivyGraph.leafTriangles.Add(ivyGraph.leafVertices.Count - offset1);
            ivyGraph.leafTriangles.Add(ivyGraph.leafVertices.Count - offset2);
            ivyGraph.leafTriangles.Add(ivyGraph.leafVertices.Count - offset3);
        }

        static void AddTriangle(IvyGraph ivyGraph, int offset1, int offset2, int offset3)
        {
            ivyGraph.triangles.Add(ivyGraph.vertices.Count - offset1);
            ivyGraph.triangles.Add(ivyGraph.vertices.Count - offset2);
            ivyGraph.triangles.Add(ivyGraph.vertices.Count - offset3);
        }

        static float Vector2ToPolar(Vector2 vector)
        {
            float phi = (vector.x == 0.0f) ? 0.0f : Mathf.Atan(vector.y / vector.x);

            if (vector.x < 0.0f)
            {
                phi += Mathf.PI;
            }
            else
            {
                if (vector.y < 0.0f)
                {
                    phi += 2.0f * Mathf.PI;
                }
            }

            return phi;
        }

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


        static void CreateIvyMeshObject(GameObject rootObj, IvyGraph graph, Mesh mesh, bool isLeaves=false)
        {
            var PartObj = new GameObject("Ivy" + (isLeaves ? "Leaves" : "Branches") );
            PartObj.transform.parent = rootObj.transform;
            PartObj.isStatic = true;

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

    }
}
