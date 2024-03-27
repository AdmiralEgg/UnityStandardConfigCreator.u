using UnityEditor;

namespace AdmiralEgg.Tools
{
    public class BuildStandardSetupPackages
    {
        [MenuItem("Tools/Build Standard Setup Packages")]
        public static void Initialize()
        {
            const string PACKAGE_PATH = "Assets/Editor/AEGStandardProjectSetup";
            const string PACKAGE_NAME = "AEGStandardProjectSetup";

            // Export packages
            AssetDatabase.ExportPackage
            (
                PACKAGE_PATH,
                //"Assets/_AEGStandardProjectSetup", 
                $"{PACKAGE_NAME}.unitypackage",
                ExportPackageOptions.Recurse | ExportPackageOptions.Interactive
            );
        }
    }
}