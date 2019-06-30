using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// the only reason I'm doing this terrible hack is because Handles in OnSceneGUI seem to be broken in my Unity
public class IvyBehavior : MonoBehaviour {

	public List<Vector3> lineSegmentsToDraw = new List<Vector3>();
	public Vector3 cursorPos;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnDrawGizmosSelected () {
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere( cursorPos, 0.25f);
	}
}
