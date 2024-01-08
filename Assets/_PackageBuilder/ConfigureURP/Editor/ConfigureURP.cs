using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Threading;
using UnityEditor.UIElements;
using UnityEditor.ShaderKeywordFilter;
// Namespaces below require packages to be installed.
#if (URP_EXISTS && POSTPROCESSING_EXISTS)
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.PostProcessing;
#endif

namespace AdmiralEgg.Tools.URP
{
    public class ConfigureURP
    {
        static string settingsPath = "Assets/_Project/Settings";
        static string materialsPath = "Assets/_Project/MaterialsAndUV";
        
        // Asset Names
        static string skyboxMaterialName = "DefaultSkybox.mat";
        static string lightingAssetName = "DefaultLightingSettings.asset";
        static string urdAssetName = "DefaultURPRenderData.asset";
        static string urpaAssetName = "DefaultURPRenderPipelineAsset.asset";
        static string postProcessName = "DefaultPostProcessProfile.asset";
        static string currentURPGlobalSettingsName = "UniversalRenderPipelineGlobalSettings.asset";
        static string updatedURPGlobalSettingsName = "DefaultURPGlobalSettings.asset";

        // GameObject Names
        static string postProcessVolumeName = "PostProcessVolume";

        [MenuItem("Tools/Initial Setup Tools/Configure URP Rendering",false,2)]
        public static void Initialize()
       {
            // Initial refresh to make sure we're working with current asset data
            AssetDatabase.Refresh();

#if (URP_EXISTS && POSTPROCESSING_EXISTS)
            UniversalRendererData urd = ConfigureRendererData(); // This is derived from ScriptableRendererData.

            UniversalRenderPipelineAsset urpa = ConfigureRenderPipelineAsset(urd); // Derived from RenderPipelineAsset
                 
            // Line below sets the default pipeline and creates a Global Settings asset in 'Assets'
            GraphicsSettings.defaultRenderPipeline = urpa;
#endif

            // Refresh database so we can apply new pipeline assets to the GraphicsSettings
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ForceReserializeAssets();

            // TODO: Fix having to run the script multiple times to move the Global Settings. Why isn't it being found?
            //Thread.Sleep(10000); //Problem isn't time dependent.
#if (URP_EXISTS && POSTPROCESSING_EXISTS)
            ConfigureRenderPipelineGlobalSettings();
#endif

            ConfigureSkyboxMaterial();

            // BUG: Rerunning removes all the Components in this.
#if (URP_EXISTS && POSTPROCESSING_EXISTS)
            PostProcessProfile ppp = ConfigurePostProcessing();
            ConfigurePostProcessVolume(ppp);
#endif

            ConfigureLightingSettings();
        }

#if (URP_EXISTS && POSTPROCESSING_EXISTS)
        private static void ConfigurePostProcessVolume(PostProcessProfile ppp)
        {
            // If a Post Process Volume Game Object already exists, do not create a new one.
            GameObject existingVolume = GameObject.Find(postProcessVolumeName);

            if (existingVolume != null)
            {
                Debug.LogWarning($"Post Process Volume of name [{postProcessVolumeName}] already exists. Deleting.");
                GameObject.DestroyImmediate(existingVolume);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // If the PostProcessProfile hasn't been passed, check if one already exists and use that.
            if (ppp == null)
            {
                string[] guid = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(postProcessName));

                if (guid.Length != 0)
                {
                    string assetPath = Path.Combine(settingsPath, postProcessName);
                    
                    ppp = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(assetPath);
                }
            }

            GameObject pppGameObject = new GameObject(postProcessVolumeName);

            pppGameObject.layer = LayerMask.NameToLayer("PostProcessing");

            PostProcessVolume volume = pppGameObject.AddComponent<PostProcessVolume>();

            volume.profile = ppp;
            volume.isGlobal = true;

            // Move under parent object
            GameObject parentGameObject = GameObject.Find("--- ENVIRONMENT ---");

            if (parentGameObject == null)
            {
                Debug.Log($"Skipping parenting of the {postProcessVolumeName} game object");
            }
            else
            {
                pppGameObject.transform.SetParent(parentGameObject.transform);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif

        private static void ConfigureLightingSettings()
        {
            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(lightingAssetName), new string[] { settingsPath }).Length != 0)
            {
                Debug.LogWarning($"Asset named [{lightingAssetName}] already exists in Settings directory. Skipping.");
                return;
            }

            LightingSettings ls = new LightingSettings();
            
            AssetDatabase.CreateAsset(ls, Path.Combine(settingsPath, $"{lightingAssetName}"));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Get the new lighting settings asset and apply to current scene.
            Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(Path.Combine(settingsPath, $"{lightingAssetName}"));
        }

        /// <summary>
        /// Configures the RenderPipelineGlobalSettings asset, renames it, and moves it to the Settings folder.
        /// </summary>
        private static void ConfigureRenderPipelineGlobalSettings()
        {
            AssetDatabase.Refresh();

            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(updatedURPGlobalSettingsName), new string[] { settingsPath }).Length != 0)
            {
                Debug.LogWarning($"Asset named [{updatedURPGlobalSettingsName}] already exists in Settings directory. Skipping.");
                return;
            }

            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(currentURPGlobalSettingsName), new string[] { "Assets" }).Length == 0)
            {
                Debug.LogWarning($"Cannot find [{Path.GetFileNameWithoutExtension(currentURPGlobalSettingsName)}] in the Assets folder. Nothing to move, so skipping.");
                return;
            }
            else
            {
                AssetDatabase.MoveAsset(
                    Path.Combine("Assets", currentURPGlobalSettingsName),
                    Path.Combine(settingsPath, updatedURPGlobalSettingsName));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

#if (URP_EXISTS && POSTPROCESSING_EXISTS)
        private static UniversalRendererData ConfigureRendererData()
        {
            // Only do this if we don't already have an asset of the same name under Assets
            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(urdAssetName), new string[] { settingsPath }).Length != 0)
            {
                Debug.LogWarning($"Renderer Data asset named [{urdAssetName}] already exists in this project, skipping creation.");

                return null;
            }

            var urdAsset = ScriptableObject.CreateInstance<UniversalRendererData>();

            AssetDatabase.CreateAsset(urdAsset, Path.Combine(settingsPath, urdAssetName));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return urdAsset;
        }
#endif

#if (URP_EXISTS && POSTPROCESSING_EXISTS)
        private static UniversalRenderPipelineAsset ConfigureRenderPipelineAsset(UniversalRendererData urd)
        {
            if (urd == null)
            {
                Debug.LogWarning($"A null value UniversalRendererData object was passed to the script, skipping creation.");

                return null;
            }

            // Only do this if we don't already have an asset of the same name under Assets
            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(urpaAssetName), new string[] { settingsPath }).Length != 0)
            {
                Debug.LogWarning($"Renderer Data asset named [{urpaAssetName}] already exists in this project, skipping creation.");

                return null;
            }

            // Create rendering objects
            var urpaAsset = UniversalRenderPipelineAsset.Create(urd);

            // Configure
            urpaAsset.supportsHDR = true;
            urpaAsset.msaaSampleCount = 4;

            AssetDatabase.CreateAsset(urpaAsset, Path.Combine(settingsPath, urpaAssetName));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return urpaAsset;
        }

        private static PostProcessProfile ConfigurePostProcessing()
        {
            PostProcessProfile postProcessProfile = null;

            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(postProcessName), new string[] { settingsPath }).Length != 0)
            {
                Debug.LogWarning($"Post Process Profile asset named [{postProcessName}] already exists in this project, skipping creation.");
            }
            else
            {
                string assetPath = Path.Combine(settingsPath, postProcessName);

                // New Post Process Profile
                var postProcessingAsset = ScriptableObject.CreateInstance<PostProcessProfile>();

                AssetDatabase.CreateAsset(postProcessingAsset, assetPath);
                
                postProcessProfile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(assetPath);

                var colorGrading = ScriptableObject.CreateInstance<ColorGrading>();
                var ssr = ScriptableObject.CreateInstance<ScreenSpaceReflections>();

                postProcessProfile.settings.Add(colorGrading);
                postProcessProfile.settings.Add(ssr);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Configure the main camera

            GameObject mainCamera = GameObject.Find(Camera.main.name);

            var postProcessLayer = mainCamera.AddComponent<PostProcessLayer>();

            if (postProcessLayer != null)
            {
                // TODO: Fix this stupid layermask
                postProcessLayer.volumeLayer.value = LayerMask.NameToLayer("PostProcessing") + 3;
            }

            return postProcessProfile;
        }
#endif

        private static void ConfigureSkyboxMaterial()
        {
            if (AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(skyboxMaterialName), new string[] { materialsPath }).Length != 0)
            {
                Debug.LogWarning($"Skybox material asset named [{skyboxMaterialName}] already exists in this project, skipping creation.");
            }
            else
            {
                // New material, make skybox
                Material skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));

                AssetDatabase.CreateAsset(skyboxMaterial, Path.Combine(materialsPath, skyboxMaterialName));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // TODO: Set the Skybox material in the Lighting panel
        }
    }
}