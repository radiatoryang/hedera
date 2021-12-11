using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;

#if CAN_EXPORT_FBX
using UnityEditor.Formats.Fbx.Exporter;
#endif

namespace Hedera {

    /// mostly just a wrapper for the FBX Exporter package
	public class FbxExport
	{
        public static bool foundFBXExporterPackage = false;

#if CAN_EXPORT_FBX
        [MenuItem("Hedera/Export Selected GameObjects to .FBX...")]
		public static void DoFbxExport () {
			// SaveObjFile( Selection.gameObjects, true );
		}
#endif
    }

    #if !FBX_EXPORTER
    public static class DetectFBXExporter
    {
        const string FBX_DEFINE = "FBX_EXPORTER";
        const string FBX_PACKAGE_ID = "com.unity.formats.fbx";

        [InitializeOnLoadMethod]
        static void Init()
        {
            // Get current defines
            var currentDefinesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Split at ;
            var defines = currentDefinesString.Split(';').ToList();

            // check if defines already exist given define
            if (!defines.Contains(FBX_DEFINE) && IsPackageInstalled(FBX_PACKAGE_ID))
            {
                // if not add it at the end with a leading ; separator
                currentDefinesString += $";{FBX_DEFINE}";

                // write the new defines back to the PlayerSettings
                // This will cause a recompilation of your scripts
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currentDefinesString);
            } 
        }

        /// simple way to detect if Unity package is installed with just a simple text search in Packages/manifest.json which should work in 99% of cases
		/// adapted from https://forum.unity.com/threads/detect-if-a-package-is-installed.1100338/#post-7095136
		public static bool IsPackageInstalled(string packageId)
		{
			if ( !File.Exists("Packages/manifest.json") )
				return false;

			string jsonText = File.ReadAllText("Packages/manifest.json");
			return jsonText.Contains( packageId );
		}
    }
    #endif
}