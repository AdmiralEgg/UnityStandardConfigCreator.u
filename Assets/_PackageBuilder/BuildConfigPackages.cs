using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace AdmiralEgg.Tools
{
    public class BuildConfigPackages
    {
        static string[] packagesToBuild = new string[]
        {
            "ConfigureProject",
            "ConfigureURP"
        };
        
        // TODO: Add support for editing the Package.json file with the Package Manifest viewer in Unity.
        // Package Manifest Viewer will only a appear when a package is opened under the Packages folder usually.

        [MenuItem("Tools/Custom Packages/Build Packages")]
        public static void BuildPackages()
        {            
            // Export packages
            foreach (string packageName in packagesToBuild)
            {
                AssetDatabase.ExportPackage($"Assets/_PackageBuilder/{packageName}", $"{packageName}.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
            }
        }
    }
}

