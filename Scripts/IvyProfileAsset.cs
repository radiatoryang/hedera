using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Hedera
{
    [CreateAssetMenu(fileName = "NewIvyProfile", menuName = "Ivy Profile (Hedera)", order = 1)]
    public class IvyProfileAsset : ScriptableObject
    { 
        public IvyProfile ivyProfile;
    }

}
