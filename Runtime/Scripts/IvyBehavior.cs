using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedera {
	[HelpURL("https://github.com/radiatoryang/hedera/wiki")]
	public class IvyBehavior : MonoBehaviour {
		// strip out ivy generator code upon compile
		#if UNITY_EDITOR
		public List<IvyGraph> ivyGraphs = new List<IvyGraph>();
		public bool generateMeshDuringGrowth = true, enableGrowthSim = true;
		public bool showProfileFoldout;
		public IvyProfileAsset profileAsset;
		public Color debugColor = Color.yellow;


		#endif
	}
}