using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

// OBJ Export is based on:
// ExportOBJ from Unity wiki https://wiki.unity3d.com/index.php/ExportOBJ
// subsequent edits by Matt Rix https://gist.github.com/MattRix/0522c27ee44c0fbbdf76d65de123eeff
// main change here was to convert to a static utility class... -RY, 29 June 2019

namespace Hedera {
	public class ObjExport
	{
		private static int StartIndex = 0;
		
		public static string MeshToString(MeshFilter mf, Transform t) 
		{	
			Vector3 s 		= t.localScale;
			Vector3 p 		= t.localPosition;
			Quaternion r 	= t.localRotation;
			
			
			int numVertices = 0;
			Mesh m = mf.sharedMesh;
			if (!m)
			{
				return "####Error####";
			}
			Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
			
			StringBuilder sb = new StringBuilder();
			
			foreach(Vector3 vv in m.vertices)
			{
				Vector3 v = t.TransformPoint(vv);
				numVertices++;
				sb.Append(string.Format("v {0} {1} {2}\n",v.x,v.y,-v.z));
			}
			sb.Append("\n");
			foreach(Vector3 nn in m.normals) 
			{
				Vector3 v = r * nn;
				sb.Append(string.Format("vn {0} {1} {2}\n",-v.x,-v.y,v.z));
			}
			sb.Append("\n");
			foreach(Vector3 v in m.uv) 
			{
				sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
			}
			for (int material=0; material < m.subMeshCount; material ++) 
			{
				sb.Append("\n");
				sb.Append("usemtl ").Append(mats[material].name).Append("\n");
				sb.Append("usemap ").Append(mats[material].name).Append("\n");
				
				int[] triangles = m.GetTriangles(material);
				for (int i=0;i<triangles.Length;i+=3) {
					sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
											triangles[i]+1+StartIndex, triangles[i+1]+1+StartIndex, triangles[i+2]+1+StartIndex));
				}
			}
			
			StartIndex += numVertices;
			return sb.ToString();
		}

		[MenuItem("Hedera/Export Selected GameObjects to .OBJ...")]
		public static void DoObjExport () {
			GetObjData( Selection.gameObjects );
		}
		
		public static string GetObjData(GameObject[] gameObjects, bool recenter = true, bool makeSubmeshes = false)
		{
			if (gameObjects == null || gameObjects.Length == 0)
			{
				Debug.LogWarning("ObjExport: no game objects defined, nothing to export");
				return null;
			}
			
			string meshName = gameObjects[0].name;
			string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");
			
			// start
			StartIndex = 0;
			
			StringBuilder meshString = new StringBuilder();
			
			meshString.Append("#" + meshName + ".obj"
							+ "\n#" + System.DateTime.Now.ToLongDateString() 
							+ "\n#" + System.DateTime.Now.ToLongTimeString()
							+ "\n#-------" 
							+ "\n\n");

			// process all gameobjects, even the children (see ProcessTransform() )
			foreach ( var go in gameObjects ) {
				Transform t = go.transform;
				Vector3 originalPosition = t.position;
				if ( recenter ) {
					t.position = Vector3.zero;
				}
				if (!makeSubmeshes)
				{
					meshString.Append("g ").Append(t.name).Append("\n");
				}
				meshString.Append(ProcessTransform(t, makeSubmeshes));

				t.position = originalPosition;
			}
			
			WriteToFile(meshString.ToString(), fileName);
			
			// end
			StartIndex = 0;
			Debug.Log("ObjExport: saved .OBJ to " + fileName);
			return fileName;
		}
		
		static string ProcessTransform(Transform t, bool makeSubmeshes)
		{
			StringBuilder meshString = new StringBuilder();
			
			meshString.Append("#" + t.name
							+ "\n#-------" 
							+ "\n");
			
			if (makeSubmeshes)
			{
				meshString.Append("g ").Append(t.name).Append("\n");
			}
			
			MeshFilter mf = t.GetComponent<MeshFilter>();
			if (mf != null)
			{
				meshString.Append(ObjExport.MeshToString(mf, t));
			}
			
			for(int i = 0; i < t.childCount; i++)
			{
				meshString.Append(ProcessTransform(t.GetChild(i), makeSubmeshes));
			}
			
			return meshString.ToString();
		}
		
		static void WriteToFile(string s, string filename)
		{
			using (StreamWriter sw = new StreamWriter(filename)) 
			{
				sw.Write(s);
			}
		}
	}

}
