using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdmiralEgg.Tools
{
    /// <summary>
    /// Modified version of the UnityLauncherPro initilization script.
    /// </summary>
    public class ConfigureProject
    {   
        // Add a _Project root
        static string[] folders = new string[] 
        {
            "Imports",
            "Documentation",
            "_Project",
            "_Project/Materials",
            "_Project/MeshAndUV",
            "_Project/Prefabs",
            "_Project/Scripts",
            "_Project/Scripts/Managers",
            "_Project/Scripts/UI",
            "_Project/Scripts/ScriptableObjects",
            "_Project/Shaders",
            "_Project/Audio",
            "_Project/Textures",
            "_Project/Data",
            "_Project/Settings"
        };

        // Scene folder is separated, as the initial scene is moved here.
        static string sceneFolder = "_Project/Scenes";
        static string defaultSceneName = "Main.unity";

        // Dictionary of assemblies and paths
        static Dictionary<string, AssemblyDefinitionType> addAssemblies = new Dictionary<string, AssemblyDefinitionType>()
        {
            { "_Project/Scripts", 
                new AssemblyDefinitionType()
                {
                    name = "Core"
                } 
            },
            { "_Project/Scripts/Managers", 
                new AssemblyDefinitionType()
                {
                    name = "Managers"
                } 
            },
            { "_Project/Scripts/UI",
                new AssemblyDefinitionType()
                {
                    name = "UI"
                }
            },
            { "_Project/Scripts/ScriptableObjects",
                new AssemblyDefinitionType()
                {
                    name = "ScriptableObjects"
                }
            }
        };

        // Gist Data

        // Settings
        static string projectName = "Project";
        static string companyName = "AdmiralEgg";

        // GameObjects
        static string[] headerGameObjects = new string[]
        {
            "--- MANAGERS ---",
            "--- CAMERA ---",
            "--- ENVIRONMENT ---",
            "--- UI ---",
            "--- MISC ---"
        };

        // Assets?
        static string assetsFolder;

        [MenuItem("Tools/Initial Setup Tools/Initialize Project",false,1)]
        public static void Initialize()
        {
            assetsFolder = Application.dataPath;

            // TODO: Don't run if it's already run? Create a setup log and check for that?
            CreateFolders();
            SetupAssemblyDefinions();
            SetupProject();
            ConfigureEditor();
            ConfigureCamera();
            SetupGameObjects();
            UpdatePackageManifestWithGist("AdmiralEgg", "8bb745d9fc52a23a9116b233db939200");

            // Refresh all
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Initial Setup Tools/Update Script Template (Admin Required)",false,3)]
        public static void UpdateScriptTemplate()
        {
            UpdateDefaultScriptWithGist("AdmiralEgg", "85ca2c52e669e42e679a32f606b5c2e1");

            // Refresh all
            AssetDatabase.Refresh();
        }

        static void SetupProject()
        {
            PlayerSettings.companyName = companyName;
            PlayerSettings.productName = projectName;

            // TODO: What does this do?
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Skybox off from lighting settings
            RenderSettings.skybox = null;

            // Disable unity splash
            PlayerSettings.SplashScreen.show = false;

            // TODO: Build Settings?

            // TODO: Lighting Settings
            //https://docs.unity3d.com/Manual/lighting-window.html
        }

        static void SetupGameObjects()
        {
            foreach (string gameObject in headerGameObjects)
            {
                // check whether the game object exists, and continue if it does...
                if (GameObject.Find(gameObject) != null)
                {
                    UnityEngine.Debug.Log($"GameObject {gameObject} already exists, skipping creation...");
                    continue;
                }
                
                GameObject go = new GameObject(gameObject);
                go.tag = "EditorOnly";
            }
        }

        static void ConfigureCamera()
        {
            // Skybox off from camera
            Camera.main.clearFlags = CameraClearFlags.SolidColor;

            // Reset camera position
            Camera.main.transform.position = Vector3.zero;
        }

        static void ConfigureEditor()
        {
            // Disable editor camera easing and acceleration
#if UNITY_2019_1_OR_NEWER
            SceneView.lastActiveSceneView.cameraSettings.easingEnabled = false;
            SceneView.lastActiveSceneView.cameraSettings.accelerationEnabled = false;
#endif

            // GIZMOS
            // Set sceneview gizmos size https://github.com/unity3d-kr/GizmoHotkeys/blob/05516ebfc3ce1655cbefb150d328e2b66e03646d/Editor/SelectionGizmo.cs
            Assembly asm = Assembly.GetAssembly(typeof(Editor));
            Type type = asm.GetType("UnityEditor.AnnotationUtility");
            if (type != null)
            {
                PropertyInfo iconSizeProperty = type.GetProperty("iconSize", BindingFlags.Static | BindingFlags.NonPublic);
                if (iconSizeProperty != null)
                {
                    iconSizeProperty.SetValue(asm, 0.01f, null);
                }
            }
        }

        /// <summary>
        /// Best method.
        /// Tried using Client.AddAndRemovePackages, but has poor error handling and is not idempotent.
        /// </summary>
        static async void UpdatePackageManifestWithGist(string githubUser, string id)
        {
            string url = $"https://gist.github.com/{githubUser}/{id}/raw";

            var content = await GetGistContents(url);

            // replace package file
            var existingManifest = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            File.WriteAllText(existingManifest, content);

            // Refresh the package manager
            Client.Resolve();
        }

        /// <summary>
        /// Manual jobs to do once the setup has finished.
        /// </summary>
        private static void SetupSummary()
        {
            UnityEngine.Debug.LogWarning("Please manually configure the TagManager and EditorSetting in Project Settings using the provided .preset files");
        }

        /// <summary>
        /// Downloads default script content from gist.
        /// Creates a new file in the users temp folder, then copies the file to ScriptTemplates with cmd.exe using Admin permissions.
        /// </summary>
        static async void UpdateDefaultScriptWithGist(string githubUser, string id)
        {
            string url = $"https://gist.github.com/{githubUser}/{id}/raw";

            var content = await GetGistContents(url);

            // Find the NewBehaviourScript in the Unity Editor script templates
            UnityEngine.Debug.Log($"App path: {EditorApplication.applicationContentsPath}");

            string defaultTemplatePath = Path.Combine(EditorApplication.applicationContentsPath, "Resources\\ScriptTemplates");
            string[] filePaths = Directory.GetFiles(defaultTemplatePath);

            string defaultTemplateName = "";

            // Find the name and path of the new file to create
            foreach (string file in filePaths)
            {
                if (file.Contains("NewBehaviourScript.cs")) 
                {  
                    defaultTemplateName = Path.GetFileName(file);
                }
            }

            // Write file to temporary location
            string temporaryFilePath = Path.Combine(System.IO.Path.GetTempPath(), defaultTemplateName);

            File.WriteAllText(temporaryFilePath, content);

            // Run a copy as admin
            var psi = new ProcessStartInfo();

            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c copy \"{temporaryFilePath}\" \"{defaultTemplatePath}\"";
            psi.Verb = "runas";

            var process = new Process();
            process.StartInfo = psi;
            process.Start();
            process.WaitForExit();
        }

        static async Task<string> GetGistContents(string url)
        {
            // download manifest from gist
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }

        /// <summary>
        /// Create folders and migrate scene to the new folder structure.
        /// </summary>
        static void CreateFolders()
        {
            // Create a folder if it doesn't exist. Add assembly definition if required.
            foreach (string folder in folders)
            {
                if (!Directory.Exists(assetsFolder + " / " + folder))
                {
                    Directory.CreateDirectory(assetsFolder + "/" + folder);
                }
            }

            // Create a scene folder
            if (!Directory.Exists(assetsFolder + " / " + sceneFolder))
            {
                Directory.CreateDirectory(assetsFolder + "/" + sceneFolder);
            }

            string scenePath = $"{assetsFolder}/{sceneFolder}/{defaultSceneName}";

            // Move Scene
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);

            // add scene to build settings
            List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
            if (!string.IsNullOrEmpty(scenePath)) editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }

        class AssemblyDefinitionType
        {
            public string name;
            public List<string> references;
            public List<string> includePlatforms;
            public List<string> excludePlatforms;
        }

        static void SetupAssemblyDefinions()
        {
            foreach (KeyValuePair<string, AssemblyDefinitionType> definition in addAssemblies)
            {
                // TODO: IF these definitions exist, skip (doesn't seem to matter?).
                var newObject = JsonUtility.ToJson(definition.Value, true);

                var p = Path.Combine(assetsFolder, definition.Key);

                File.WriteAllText(Path.Combine(p, definition.Value.name + ".asmdef"), newObject);
            }
        }
    }

    /// <summary>
    /// Modified version of the UnityLauncherPro initilization script.
    /// </summary>
    public class OfflineManifestUpdate
    {
        // Packages Offline Data (requies fixed version)
        static Dictionary<string, string> addPackages = new Dictionary<string, string>()
        {
            { "com.unity.ide.visualstudio", "2.0.22" },
            { "com.unity.cinemachine", "2.9.7" },
            { "com.unity.textmeshpro", "3.0.6" },
            { "com.unity.render-pipelines.universal", "14.0.7" },
            { "com.unity.postprocessing", "3.2.2" },
            { "com.unity.polybrush", "1.1.5" }
        };

        static string[] removePackages = new string[]
        {
            "com.unity.modules.unityanalytics",
            "com.unity.modules.director",
            "com.unity.collab-proxy",
            "com.unity.ide.rider",
            "com.unity.ide.vscode",
            "com.unity.test-framework",
            "com.unity.timeline",
            "com.unity.visualscripting",
            "com.unity.2d.tilemap.extras"
        };

        [MenuItem("Tools/Initial Setup Tools/Update Package Manifest (Offline)", false, 4)]
        public static void Initialize()
        {
            string manifestPath = GetManifestPath();
            string jsonText = ReadManifestFile(manifestPath);
            DependenciesManifest dm = DeserializeManifest(jsonText);

            DependenciesManifest updatedManifestObject = UpdateManifestObject(dm, addPackages, removePackages);

            SerializeUpdatedManifest(manifestPath, updatedManifestObject);

            // Refresh all
            AssetDatabase.Refresh();
        }

        private static string GetManifestPath()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\"));
            var packagesPath = Path.Combine(projectRoot, "Packages\\manifest.json");

            return packagesPath;
        }

        private static void SerializeUpdatedManifest(string manifestPath, DependenciesManifest dm)
        {
            var jsonConvertType = Assembly.Load("Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");

            IJsonSerializer jsonSerializer = new NewtonsoftJsonSerializer(jsonConvertType);

            var toJson = jsonSerializer.Serialize(dm);

            File.WriteAllText(manifestPath, toJson);
        }

        private static string ReadManifestFile(string packagesPath)
        {
            UnityEngine.Debug.Log($"Fetching package from [{packagesPath}]");

            // check if packages.json exists
            if (File.Exists(packagesPath) == false)
            {
                UnityEngine.Debug.LogError($"manifest.json does not exist in {packagesPath}. Unable to update the file.");
                return null;
            }

            // Read manifest file.
            var jsonText = File.ReadAllText(packagesPath, Encoding.UTF8);

            return jsonText;
        }

        private static DependenciesManifest DeserializeManifest(string manifestFile)
        {
            var jsonConvertType = Assembly.Load("Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");

            IJsonSerializer jsonSerializer = new NewtonsoftJsonSerializer(jsonConvertType);

            var fromJson = jsonSerializer.Deserialize<DependenciesManifest>(manifestFile);

            return fromJson;
        }

        private static DependenciesManifest UpdateManifestObject(DependenciesManifest dm, Dictionary<string, string> addPackages, string[] removePackages)
        {
            // iterate through all dependencies and removes ones on the list
            foreach (string package in removePackages)
            {
                if (dm.dependencies.ContainsKey(package))
                {
                    UnityEngine.Debug.Log($"Package [{package}] found in manifest, removing.");
                    dm.dependencies.Remove(package);
                }
            }

            // add anything on the list that doesn't already exist
            foreach (KeyValuePair<string, string> package in addPackages)
            {
                if (dm.dependencies.ContainsKey(package.Key) == false)
                {
                    UnityEngine.Debug.Log($"Required package [{package}] missing from manifest, adding.");
                    dm.dependencies.Add(package.Key, package.Value);
                }
            }

            return dm;
        }

        // manifest.json
        public class DependenciesManifest
        {
            public Dictionary<string, string> dependencies { get; set; }
        }

        public interface IJsonSerializer
        {
            string Serialize(object obj);
            T Deserialize<T>(string json);
        }

        public class NewtonsoftJsonSerializer : IJsonSerializer
        {
            private readonly Type jsonConvertType;

            public NewtonsoftJsonSerializer(Type jsonConvertType)
            {
                this.jsonConvertType = jsonConvertType;
            }

            public string Serialize(object obj)
            {
                var serializeMethod = jsonConvertType.GetMethod("SerializeObject", new Type[] { typeof(object) });
                return (string)serializeMethod.Invoke(null, new object[] { obj });
            }

            public T Deserialize<T>(string json)
            {
                var deserializeMethod = jsonConvertType.GetMethod("DeserializeObject", new Type[] { typeof(string), typeof(Type) });
                return (T)deserializeMethod.Invoke(null, new object[] { json, typeof(T) });
            }
        }
    }

}
