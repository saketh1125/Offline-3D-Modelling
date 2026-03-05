using System.Collections.Generic;
using UnityEngine;
using ThreeDBuilder.Scene;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Processes high-level CityModel schemas into arrays of explicit ObjectModels.
    /// Does not instantiate GameObjects, merely inflates JSON abstractions into scene data.
    /// </summary>
    public static class CityGenerator
    {
        public static List<ObjectModel> GenerateCity(CityModel cityConfig)
        {
            List<ObjectModel> cityObjects = new List<ObjectModel>();

            if (cityConfig == null || cityConfig.grid_size == null || cityConfig.grid_size.Length < 2)
            {
                return cityObjects;
            }

            int cols = cityConfig.grid_size[0];
            int rows = cityConfig.grid_size[1];
            float spacing = cityConfig.block_spacing;
            float bWidth = cityConfig.building_width;
            float roadWidth = cityConfig.road_width;

            float minHeight = (cityConfig.building_height_range != null && cityConfig.building_height_range.Length > 0) ? cityConfig.building_height_range[0] : 1f;
            float maxHeight = (cityConfig.building_height_range != null && cityConfig.building_height_range.Length > 1) ? cityConfig.building_height_range[1] : 5f;

            // Center the grid around origin
            float startX = -((cols - 1) * spacing) / 2f;
            float startZ = -((rows - 1) * spacing) / 2f;

            for (int x = 0; x < cols; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    float px = startX + x * spacing;
                    float pz = startZ + z * spacing;

                    // Generate a random building block for this grid cell
                    float bHeight = UnityEngine.Random.Range(minHeight, maxHeight);

                    ObjectModel building = new ObjectModel
                    {
                        id = $"city_bldg_{x}_{z}",
                        primitive = "cube",
                        materialRef = "", // Leave empty for fallback default material assignment in SceneBuilder
                        transform = new TransformModel
                        {
                            position = new float[] { px, bHeight / 2f, pz },
                            rotation = new float[] { 0f, 0f, 0f },
                            scale = new float[] { bWidth, bHeight, bWidth }
                        }
                    };
                    cityObjects.Add(building);

                    // Procedural planes map a flat network representing roads intersecting the building blocks
                    if (roadWidth > 0f)
                    {
                        // To represent the road footprint for this block, we use a single plane centered under the building
                        // Scale it up to cover the block_spacing area. The height is miniscule so it rests on floor.
                        ObjectModel road = new ObjectModel
                        {
                            id = $"city_road_{x}_{z}",
                            primitive = "plane",
                            materialRef = "",
                            transform = new TransformModel
                            {
                                position = new float[] { px, 0.01f, pz },
                                rotation = new float[] { 0f, 0f, 0f },
                                scale = new float[] { spacing, 1f, spacing }
                            }
                        };
                        cityObjects.Add(road);
                    }
                }
            }

            return cityObjects;
        }
    }
}
