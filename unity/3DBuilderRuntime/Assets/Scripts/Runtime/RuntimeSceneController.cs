using UnityEngine;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Builders;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Bootstraps the procedural scene generation pipeline for testing inside the Unity Editor.
    /// Attaching this to an empty GameObject will render a red cube at runtime.
    /// </summary>
    public class RuntimeSceneController : MonoBehaviour
    {
        private void Start()
        {
            // Define the test JSON scene string representing a procedural red cube
            string testJson = @"
            {
              ""schema_version"": ""1.0"",
              ""materials"": [
                {
                  ""id"": ""red"",
                  ""baseColor"": [1, 0, 0]
                }
              ],
              ""objects"": [
                {
                  ""id"": ""cube1"",
                  ""primitive"": ""cube"",
                  ""materialRef"": ""red"",
                  ""transform"": {
                    ""position"": [0, 0, 0],
                    ""rotation"": [0, 0, 0],
                    ""scale"": [1, 1, 1]
                  }
                }
              ]
            }";

            try
            {
                // Step 1: Parse JSON into our plain-data SceneModel mapping
                SceneInterpreter interpreter = new SceneInterpreter();
                SceneModel parsedScene = interpreter.ParseScene(testJson);

                if (parsedScene != null)
                {
                    Debug.Log($"Procedural Engine: Parsed JSON successfully. Found {parsedScene.objects.Count} objects.");

                    // Step 2: Use SceneBuilder to translate the model into Unity standard GameObjects
                    SceneBuilder builder = new SceneBuilder();
                    GameObject generatedRoot = builder.BuildScene(parsedScene);

                    if (generatedRoot != null)
                    {
                        // Attach the resulting hierarchy to this executing MonoBehaviour to keep the scene tidy
                        generatedRoot.transform.SetParent(this.transform);
                        Debug.Log("Procedural Engine: Scene built successfully!");
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Ensure failures during the parsing interpretation logic don't silently fail visually
                Debug.LogError($"Procedural Engine Error: Failed to bootstrap procedural scene: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
