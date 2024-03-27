using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdmiralEgg.Tools
{
    public static class ConfigureDefaultSpinningCube
    {
        const string SCRIPTS_PATH = "Assets/_Project/Scripts";
        const string PACKAGE_PATH = "Assets/Editor/AEGStandardProjectSetup/AddSpinningCube";

        [MenuItem("Tools/Initial Setup Tools/[5] Add Spinning Cube", false, 4)]
        public static void Initialize()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Move scripts, then add as components
            MoveScript("SpinCube", SCRIPTS_PATH);
            MoveScript("PressEscapeToExit", SCRIPTS_PATH);

            cube.AddComponent<SpinCube>();
            cube.AddComponent<PressEscapeToExit>();

            // Copy scripts back to their original locations so we avoid assembly reference issues.
            CopyScript("SpinCube", PACKAGE_PATH);
            CopyScript("PressEscapeToExit", PACKAGE_PATH);
        }

        private static void MoveScript(string scriptName, string movePath)
        {
            string scriptGUID = AssetDatabase.FindAssets($"{scriptName} t:Script").FirstOrDefault();

            AssetDatabase.MoveAsset(AssetDatabase.GUIDToAssetPath(scriptGUID), Path.Join(movePath, $"{scriptName}.cs"));
            AssetDatabase.Refresh();
        }

        private static void CopyScript(string scriptName, string copyPath)
        {
            string scriptGUID = AssetDatabase.FindAssets($"{scriptName} t:Script").FirstOrDefault();

            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(scriptGUID), Path.Join(copyPath, $"{scriptName}.cs"));
            AssetDatabase.Refresh();
        }
    }
}
