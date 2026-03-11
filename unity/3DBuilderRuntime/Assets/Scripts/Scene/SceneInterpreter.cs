using System;
using UnityEngine; // Required for JsonUtility

namespace ThreeDBuilder.Scene
{
    /// <summary>
    /// Pure JSON parsing layer. Converts incoming Scene JSON into a strongly typed SceneModel.
    /// Does not create GameObjects or generate meshes.
    /// </summary>
    public class SceneInterpreter
    {
        /// <summary>
        /// Parses a raw JSON string into a SceneModel and validates required fields.
        /// </summary>
        /// <param name="json">The raw JSON string representing the scene.</param>
        /// <returns>A validated SceneModel instance.</returns>
        public SceneModel ParseScene(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("ParseScene: JSON payload is null or empty.");
            }

            SceneModel model;
            try
            {
                model = JsonUtility.FromJson<SceneModel>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"ParseScene: Failed to deserialize JSON into SceneModel: {ex.Message}", ex);
            }

            if (model == null)
            {
                throw new Exception("ParseScene: Deserialized SceneModel is null.");
            }

            // Validation: schema_version
            if (string.IsNullOrWhiteSpace(model.schema_version))
            {
                throw new Exception("ParseScene: Validation failed - 'schema_version' is missing or empty.");
            }

            // Validation: materials list
            if (model.materials == null || model.materials.Count == 0)
            {
                model.materials = new System.Collections.Generic.List<MaterialModel>();
                model.materials.Add(new MaterialModel{
                    id = "default",
                    baseColor = new float[]{0.8f,0.8f,0.8f}
                });
            }

            // Validation: objects list
            if (model.objects == null)
            {
                throw new Exception("ParseScene: Validation failed - 'objects' list is null.");
            }

            // Validation: Each ObjectModel references a materialRef
            foreach (var obj in model.objects)
            {
                if (string.IsNullOrWhiteSpace(obj.materialRef))
                {
                    obj.materialRef = "default";
                }
            }

            // Procedural macro-expansion: If a city schema was passed, inflate it into explicit standard objects
            if (model.city != null)
            {
                var cityObjects = ThreeDBuilder.Procedural.CityGenerator.GenerateCity(model.city);
                model.objects.AddRange(cityObjects);
            }

            return model;
        }
    }
}
