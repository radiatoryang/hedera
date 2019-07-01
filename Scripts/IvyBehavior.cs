using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedera {
	public class IvyBehavior : MonoBehaviour {
		// strip out ivy generator code upon compile
		#if UNITY_EDITOR
		public List<IvyGraph> ivyGraphs = new List<IvyGraph>();
		public IvyProfileAsset profileAsset;

		#endif
	}
}