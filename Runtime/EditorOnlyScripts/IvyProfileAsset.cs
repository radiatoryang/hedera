using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// disable warning that this.SetDirty() is obsolete
#pragma warning disable 0618 

namespace Hedera
{
    [CreateAssetMenu(fileName = "NewIvyProfile", menuName = "Ivy Profile (Hedera)", order = 1),
    HelpURL("https://github.com/radiatoryang/hedera/wiki")]
    public class IvyProfileAsset : ScriptableObject
    { 
        #if UNITY_EDITOR
        public IvyProfile ivyProfile;

        [ContextMenu("Reset to Default Ivy Settings")]
        public void ResetProfile() {
            ivyProfile.ResetSettings();
            this.SetDirty();
        }
        #endif
    }

}
