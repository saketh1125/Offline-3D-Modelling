using UnityEngine;
using ThreeDBuilder.Scene;
using System.Collections.Generic;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Validates JSON scene schema before generation.
    /// Provides clear error messages for invalid fields.
    /// </summary>
    public static class SceneSchemaValidator
    {
        public static bool ValidateScene(SceneModel scene, out List<string> errors)
        {
            errors = new List<string>();
            bool isValid = true;

            // Check schema version
            if (string.IsNullOrEmpty(scene.schema_version))
            {
                errors.Add("Missing required field: schema_version");
                isValid = false;
            }

            // Validate environment if present
            if (scene.environment != null)
            {
                ValidateEnvironment(scene.environment, errors);
            }

            // Validate lighting if present
            if (scene.lighting != null)
            {
                ValidateLighting(scene.lighting, errors);
            }

            // Validate camera if present
            if (scene.camera != null)
            {
                ValidateCamera(scene.camera, errors);
            }

            // Validate scene template if present
            if (scene.sceneTemplate != null)
            {
                ValidateSceneTemplate(scene.sceneTemplate, errors);
            }

            // Validate materials if present
            if (scene.materials != null)
            {
                ValidateMaterials(scene.materials, errors);
            }

            // Validate objects - this is required for a meaningful scene
            if (scene.objects == null || scene.objects.Count == 0)
            {
                if (scene.sceneTemplate == null)
                {
                    errors.Add("Scene must contain either 'objects' array or 'sceneTemplate'");
                    isValid = false;
                }
            }
            else
            {
                ValidateObjects(scene.objects, errors);
            }

            return isValid;
        }

        private static void ValidateEnvironment(EnvironmentModel environment, List<string> errors)
        {
            // Validate skybox
            if (environment.skybox != null)
            {
                if (!string.IsNullOrEmpty(environment.skybox.type))
                {
                    string[] validTypes = { "procedural", "color", "none" };
                    if (System.Array.IndexOf(validTypes, environment.skybox.type) == -1)
                    {
                        errors.Add($"Invalid skybox type: {environment.skybox.type}. Valid types: procedural, color, none");
                    }
                }
            }

            // Validate fog
            if (environment.fog != null)
            {
                if (environment.fog.enabled)
                {
                    if (!string.IsNullOrEmpty(environment.fog.mode))
                    {
                        string[] validModes = { "linear", "exp", "exp2" };
                        if (System.Array.IndexOf(validModes, environment.fog.mode) == -1)
                        {
                            errors.Add($"Invalid fog mode: {environment.fog.mode}. Valid modes: linear, exp, exp2");
                        }
                    }

                    if (environment.fog.mode == "linear")
                    {
                        if (environment.fog.start >= environment.fog.end)
                        {
                            errors.Add("Fog start distance must be less than end distance");
                        }
                    }
                }
            }

            // Validate ground
            if (environment.ground != null && environment.ground.enabled)
            {
                if (environment.ground.size == null || environment.ground.size.Length < 3)
                {
                    errors.Add("Ground size must be an array of 3 values [x, y, z]");
                }
            }
        }

        private static void ValidateLighting(LightingModel lighting, List<string> errors)
        {
            // Validate preset if specified
            if (!string.IsNullOrEmpty(lighting.preset))
            {
                string[] validPresets = { "studio", "sunset", "outdoor", "museum" };
                if (System.Array.IndexOf(validPresets, lighting.preset) == -1)
                {
                    errors.Add($"Invalid lighting preset: {lighting.preset}. Valid presets: studio, sunset, outdoor, museum");
                }
            }

            // Validate type
            if (!string.IsNullOrEmpty(lighting.type))
            {
                string[] validTypes = { "directional", "point", "spot" };
                if (System.Array.IndexOf(validTypes, lighting.type) == -1)
                {
                    errors.Add($"Invalid lighting type: {lighting.type}. Valid types: directional, point, spot");
                }
            }

            // Validate intensity
            if (lighting.intensity < 0)
            {
                errors.Add("Light intensity cannot be negative");
            }
        }

        private static void ValidateCamera(CameraModel camera, List<string> errors)
        {
            // Validate mode
            if (!string.IsNullOrEmpty(camera.mode))
            {
                string[] validModes = { "orbit", "static" };
                if (System.Array.IndexOf(validModes, camera.mode) == -1)
                {
                    errors.Add($"Invalid camera mode: {camera.mode}. Valid modes: orbit, static");
                }
            }

            // Validate distance for orbit mode
            if (camera.mode == "orbit" && camera.distance <= 0)
            {
                errors.Add("Camera distance must be positive for orbit mode");
            }

            // Validate position for static mode
            if (camera.mode == "static")
            {
                if (camera.position == null || camera.position.Length < 3)
                {
                    errors.Add("Camera position must be an array of 3 values [x, y, z] for static mode");
                }
            }
        }

        private static void ValidateSceneTemplate(SceneTemplateModel template, List<string> errors)
        {
            // Validate template type
            if (string.IsNullOrEmpty(template.type))
            {
                errors.Add("Scene template type is required");
                return;
            }

            string[] validTypes = { "temple", "solar_system", "neural_network", "dna_helix", "city_grid" };
            if (System.Array.IndexOf(validTypes, template.type) == -1)
            {
                errors.Add($"Invalid scene template type: {template.type}. Valid types: temple, solar_system, neural_network, dna_helix, city_grid");
            }

            // Validate parameters based on type
            if (template.parameters != null)
            {
                switch (template.type.ToLower())
                {
                    case "temple":
                        if (template.parameters.radius <= 0)
                            errors.Add("Temple radius must be positive");
                        if (template.parameters.pillars < 4)
                            errors.Add("Temple must have at least 4 pillars");
                        break;
                    case "solar_system":
                        if (template.parameters.orbitRadius <= 0)
                            errors.Add("Solar system orbit radius must be positive");
                        if (template.parameters.planets < 1)
                            errors.Add("Solar system must have at least 1 planet");
                        break;
                    case "neural_network":
                        if (template.parameters.layers < 2)
                            errors.Add("Neural network must have at least 2 layers");
                        if (template.parameters.neuronsPerLayer < 1)
                            errors.Add("Neural network must have at least 1 neuron per layer");
                        break;
                    case "dna_helix":
                        if (template.parameters.height <= 0)
                            errors.Add("DNA helix height must be positive");
                        if (template.parameters.radius <= 0)
                            errors.Add("DNA helix radius must be positive");
                        if (template.parameters.pairs < 2)
                            errors.Add("DNA helix must have at least 2 base pairs");
                        break;
                    case "city_grid":
                        if (template.parameters.gridSize < 2)
                            errors.Add("City grid size must be at least 2");
                        if (template.parameters.blockSpacing <= 0)
                            errors.Add("City block spacing must be positive");
                        break;
                }
            }
        }

        private static void ValidateMaterials(List<MaterialModel> materials, List<string> errors)
        {
            var materialIds = new HashSet<string>();

            foreach (var material in materials)
            {
                // Check for duplicate IDs
                if (!string.IsNullOrEmpty(material.id))
                {
                    if (materialIds.Contains(material.id))
                    {
                        errors.Add($"Duplicate material ID: {material.id}");
                    }
                    materialIds.Add(material.id);
                }

                // Validate baseColor
                if (material.baseColor == null || material.baseColor.Length < 3)
                {
                    errors.Add($"Material {material.id ?? "unnamed"} must have baseColor as array of 3 values [r, g, b]");
                }
                else if (material.baseColor.Length > 3)
                {
                    errors.Add($"Material {material.id ?? "unnamed"} baseColor should only have 3 values [r, g, b]");
                }
            }
        }

        private static void ValidateObjects(List<ObjectModel> objects, List<string> errors)
        {
            var objectIds = new HashSet<string>();

            foreach (var obj in objects)
            {
                // Check for duplicate IDs
                if (!string.IsNullOrEmpty(obj.id))
                {
                    if (objectIds.Contains(obj.id))
                    {
                        errors.Add($"Duplicate object ID: {obj.id}");
                    }
                    objectIds.Add(obj.id);
                }

                // Validate primitive
                if (string.IsNullOrEmpty(obj.primitive))
                {
                    errors.Add($"Object {obj.id ?? "unnamed"} must specify a primitive type");
                }
                else
                {
                    string[] validPrimitives = { "cube", "sphere", "cylinder", "cone", "plane" };
                    if (System.Array.IndexOf(validPrimitives, obj.primitive) == -1)
                    {
                        errors.Add($"Invalid primitive type '{obj.primitive}' in object {obj.id ?? "unnamed"}");
                    }
                }

                // Validate structure if present
                if (obj.structure != null)
                {
                    ValidateStructure(obj.structure, obj.id, errors);
                }

                // Validate repeat if present
                if (obj.repeat != null)
                {
                    if (obj.repeat.grid == null || obj.repeat.grid.Length < 2)
                    {
                        errors.Add($"Object {obj.id ?? "unnamed"} repeat.grid must be an array of 2 values [columns, rows]");
                    }
                    else
                    {
                        if (obj.repeat.grid[0] < 1 || obj.repeat.grid[1] < 1)
                        {
                            errors.Add($"Object {obj.id ?? "unnamed"} repeat.grid values must be positive");
                        }
                    }
                }
            }
        }

        private static void ValidateStructure(StructureModel structure, string objectId, List<string> errors)
        {
            if (string.IsNullOrEmpty(structure.type))
            {
                errors.Add($"Object {objectId ?? "unnamed"} structure must specify a type");
                return;
            }

            string[] validTypes = { "grid", "circle", "radial", "line", "spiral" };
            if (System.Array.IndexOf(validTypes, structure.type) == -1)
            {
                errors.Add($"Invalid structure type '{structure.type}' in object {objectId ?? "unnamed"}");
            }

            // Validate parameters based on type
            switch (structure.type.ToLower())
            {
                case "grid":
                    if (structure.columns < 1 || structure.rows < 1)
                        errors.Add($"Object {objectId ?? "unnamed"} grid structure requires positive columns and rows");
                    break;
                case "circle":
                case "radial":
                    if (structure.count < 1)
                        errors.Add($"Object {objectId ?? "unnamed"} {structure.type} structure requires positive count");
                    if (structure.radius <= 0)
                        errors.Add($"Object {objectId ?? "unnamed"} {structure.type} structure requires positive radius");
                    break;
                case "line":
                    if (structure.spacing <= 0)
                        errors.Add($"Object {objectId ?? "unnamed"} line structure requires positive spacing");
                    break;
                case "spiral":
                    if (structure.height <= 0)
                        errors.Add($"Object {objectId ?? "unnamed"} spiral structure requires positive height");
                    break;
            }
        }
    }
}
