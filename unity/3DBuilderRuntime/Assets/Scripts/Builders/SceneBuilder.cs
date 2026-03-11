using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Geometry;
using ThreeDBuilder.Materials;
using ThreeDBuilder.Procedural;
using ThreeDBuilder.Runtime;

namespace ThreeDBuilder.Builders
{
    /// <summary>
    /// Constructs Unity GameObjects in the scene hierarchy from an internal SceneModel.
    ///
    /// Pipeline stages (in order):
    ///   1. SanitizeScene    – fill missing fields with safe defaults
    ///   2. BuildMaterials   – create PBR material instances and populate lookup
    ///   3. InstantiateObjects – spawn all primitives / procedural structures under root
    ///   4. RunPostBuildPasses – readability, camera framing, reflections, enhancements
    ///
    /// Guarantees:
    ///   • BuildScene NEVER returns null – a root is created before any fallible work.
    ///   • Every spawned GameObject is parented under the same ProceduralSceneRoot.
    ///   • Mesh and material lookups always fall back to safe values.
    ///   • All loop bounds are clamped so malformed JSON cannot hang the main thread.
    /// </summary>
    public class SceneBuilder
    {
        // ─────────────────────────────────────────────────────────────────
        // Constants
        // ─────────────────────────────────────────────────────────────────

        private const int MaxRepeatGrid   = 100;  // max objects per axis in a repeat grid
        private const int MaxObjectCount  = 2000; // total object safety cap
        private const string FallbackPrimitive  = "cube";
        private const string FallbackMaterialId = "default";

        // ─────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the full scene. Always returns a non-null root GameObject.
        /// On unrecoverable error the root will be named "ProceduralSceneRoot_Error"
        /// and may be empty, which is preferable to returning null and hanging Flutter.
        /// </summary>
        public GameObject BuildScene(SceneModel scene)
        {
            // Root is allocated *before* any fallible work so the catch block can
            // always return a valid object back to RuntimeManager.
            GameObject root = new GameObject("ProceduralSceneRoot");
            int totalObjectsCreated = 0;

            try
            {
                Debug.Log("[SceneBuilder] BuildScene started.");
                Debug.Log("[SceneBuilder] Scene contains " + scene.objects.Count + " objects.");

                if (scene == null)
                {
                    UnityEngine.Debug.LogError("[SceneBuilder] SceneModel is null. Returning empty root.");
                    return root;
                }

                // ── Stage 1: sanitize ──────────────────────────────────────
                SanitizeScene(scene);

                UnityEngine.Debug.Log($"[SceneBuilder] Stage 1 complete – {scene.objects.Count} objects, {scene.materials.Count} materials.");

                PerformanceMonitor.StartTimer("Total Scene Build");

                // Validate schema (non-fatal: log errors but continue)
                if (!SceneSchemaValidator.ValidateScene(scene, out List<string> validationErrors))
                {
                    foreach (var err in validationErrors)
                        UnityEngine.Debug.LogWarning($"[SceneBuilder] Validation: {err}");
                    // Not a hard failure – best-effort build continues.
                }

                // ── Stage 2: environment & materials ───────────────────────
                PerformanceMonitor.StartTimer("Environment Bootstrap");
                SceneConfig config = CreateSceneConfig(scene);
                SceneEnvironmentBootstrap.Setup(config);
                PerformanceMonitor.EndTimer("Environment Bootstrap");

                ProfessionalMaterialFactory materialFactory = new ProfessionalMaterialFactory();
                materialFactory.ClearCache();
                SceneReadabilityEnhancer.ClearCache();
                RuntimeDiagnostics.Reset();
                PerformanceMonitor.Reset();
                SceneCompositionHelper.ClearCache();

                Dictionary<string, Mesh>     meshCache     = new Dictionary<string, Mesh>();
                Dictionary<string, Material> materialLookup = new Dictionary<string, Material>();

                PerformanceMonitor.StartTimer("Material Creation");
                BuildMaterials(scene, materialFactory, materialLookup);
                PerformanceMonitor.EndTimer("Material Creation");

                UnityEngine.Debug.Log($"[SceneBuilder] Stage 2 complete – {materialLookup.Count} materials built.");

                // ── Stage 3: object instantiation ─────────────────────────
                StructureGenerator structureGenerator = new StructureGenerator(materialFactory, meshCache);

                // Template generation intercepted BEFORE instantiation (merges objects directly)
                if (scene.sceneTemplate != null && !string.IsNullOrEmpty(scene.sceneTemplate.type))
                {
                    PerformanceMonitor.StartTimer("Scene Template Expansion");
                    List<ObjectModel> templateObjects = SceneTemplateRegistry.GenerateObjects(scene.sceneTemplate.type, scene.sceneTemplate.parameters);
                    
                    if (scene.objects == null) scene.objects = new List<ObjectModel>();
                    scene.objects.AddRange(templateObjects);
                    
                    UnityEngine.Debug.Log($"[SceneBuilder] Expanded template '{scene.sceneTemplate.type}' into {templateObjects.Count} objects.");
                    PerformanceMonitor.EndTimer("Scene Template Expansion");
                }

                PerformanceMonitor.StartTimer("Object Instantiation");

                totalObjectsCreated = InstantiateObjects(
                    scene, root, materialFactory, materialLookup, meshCache, structureGenerator);

                if (totalObjectsCreated == 0)
                {
                    Debug.LogWarning("[SceneBuilder] No objects instantiated. Spawning debug cube for diagnostics.");

                    GameObject debugCube = new GameObject("debug_cube");
                    debugCube.transform.SetParent(root.transform, false);

                    debugCube.transform.position = new Vector3(0f, 1f, 0f);
                    debugCube.transform.rotation = Quaternion.identity;
                    debugCube.transform.localScale = new Vector3(2f, 2f, 2f);

                    MeshFilter mf = debugCube.AddComponent<MeshFilter>();
                    MeshRenderer mr = debugCube.AddComponent<MeshRenderer>();

                    mf.sharedMesh = MeshFactory.CreateMesh("cube");

                    // simple visible material
                    Material debugMat = new Material(Shader.Find("Standard"));
                    debugMat.color = Color.white;

                    mr.sharedMaterial = debugMat;
                    totalObjectsCreated = 1; // Ensure logs reflect the spawned cube
                }
                PerformanceMonitor.EndTimer("Object Instantiation");

                UnityEngine.Debug.Log($"[SceneBuilder] Stage 3 complete – {totalObjectsCreated} objects instantiated.");

                // ── Mobile safety guard ────────────────────────────────────────
                if (totalObjectsCreated > 1500)
                    UnityEngine.Debug.LogWarning($"[SceneBuilder] High object count ({totalObjectsCreated}) detected — " +
                        "this may cause performance issues on mobile GPUs. Consider reducing repeat.grid or structure.count.");

                // ── Stage 4: post-build passes ─────────────────────────────
                RunPostBuildPasses(root, materialLookup, config, scene.camera != null);

                PerformanceMonitor.EndTimer("Total Scene Build");
                RuntimeDiagnostics.LogSceneDiagnostics(root);
                PerformanceMonitor.LogSceneStats(totalObjectsCreated, materialFactory.CachedMaterialCount, meshCache.Count);

                UnityEngine.Debug.Log($"[SceneBuilder] Build complete. Root='{root.name}' Children={root.transform.childCount} TotalInstances={totalObjectsCreated}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[SceneBuilder] Unhandled exception – scene may be partial. {ex}");
                UnityEngine.Debug.LogError(ex.StackTrace);
                root.name = "ProceduralSceneRoot_Error";
            }

            Debug.Log("[SceneBuilder] Scene build complete. Returning root.");
            return root;
        }

        // ─────────────────────────────────────────────────────────────────
        // Stage 1 – Sanitize
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures the SceneModel has no null collections or missing required fields.
        /// Safe to call on any model; never throws.
        /// </summary>
        private void SanitizeScene(SceneModel scene)
        {
            if (scene.materials == null) scene.materials = new List<MaterialModel>();
            if (scene.objects   == null) scene.objects   = new List<ObjectModel>();

            // Guarantee a fallback material so every object resolves to something.
            bool hasDefault = false;
            foreach (var m in scene.materials)
                if (m.id == FallbackMaterialId) { hasDefault = true; break; }
            if (!hasDefault)
                scene.materials.Add(new MaterialModel
                    { id = FallbackMaterialId, baseColor = new float[] { 0.8f, 0.8f, 0.8f } });

            // Cap total objects to prevent memory exhaustion.
            if (scene.objects.Count > MaxObjectCount)
            {
                UnityEngine.Debug.LogWarning($"[SceneBuilder] Object count ({scene.objects.Count}) exceeds cap ({MaxObjectCount}). Truncating.");
                scene.objects.RemoveRange(MaxObjectCount, scene.objects.Count - MaxObjectCount);
            }

            foreach (var obj in scene.objects)
            {
                if (obj == null) continue;

                // Ensure transform model exists and has valid arrays.
                if (obj.transform == null) obj.transform = new TransformModel();
                if (obj.transform.position == null || obj.transform.position.Length < 3)
                    obj.transform.position = new float[] { 0f, 0f, 0f };
                if (obj.transform.rotation == null || obj.transform.rotation.Length < 3)
                    obj.transform.rotation = new float[] { 0f, 0f, 0f };
                if (obj.transform.scale    == null || obj.transform.scale.Length    < 3)
                    obj.transform.scale    = new float[] { 1f, 1f, 1f };

                // Ensure primitive and materialRef have safe defaults.
                if (string.IsNullOrEmpty(obj.primitive))   obj.primitive   = FallbackPrimitive;
                if (string.IsNullOrEmpty(obj.materialRef)) obj.materialRef = FallbackMaterialId;
                if (string.IsNullOrEmpty(obj.id))          obj.id          = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Stage 2 – Build Materials
        // ─────────────────────────────────────────────────────────────────

        private void BuildMaterials(
            SceneModel scene,
            ProfessionalMaterialFactory factory,
            Dictionary<string, Material> lookup)
        {
            foreach (var matModel in scene.materials)
            {
                if (matModel == null) continue;
                try
                {
                    Material mat = factory.CreateMaterial(matModel);
                    if (mat == null)
                    {
                        UnityEngine.Debug.LogWarning($"[SceneBuilder] Material factory returned null for id='{matModel.id}'. Skipping.");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(matModel.id))
                        lookup[matModel.id] = mat;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[SceneBuilder] Failed to create material id='{matModel.id}': {ex.Message}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Stage 3 – Instantiate Objects
        // ─────────────────────────────────────────────────────────────────

        private int InstantiateObjects(
            SceneModel scene,
            GameObject root,
            ProfessionalMaterialFactory materialFactory,
            Dictionary<string, Material> materialLookup,
            Dictionary<string, Mesh>     meshCache,
            StructureGenerator           structureGenerator)
        {
            int totalCreated = 0;

            foreach (ObjectModel objModel in scene.objects)
            {
                if (objModel == null) continue;
                Debug.Log("[SceneBuilder] Creating object: " + objModel.id + " primitive: " + objModel.primitive);

                try
                {
                    // ── Procedural structure (grid / circle / spiral / …) ──
                    if (objModel.structure != null && !string.IsNullOrEmpty(objModel.structure.type))
                    {
                        List<GameObject> structureObjects =
                            structureGenerator.GenerateStructure(objModel, root, materialLookup);

                        foreach (var obj in structureObjects)
                        {
                            if (obj == null) continue;
                            // Guarantee parenting under root (StructureGenerator already does this,
                            // but this is a defensive re-parent in case a future change breaks that).
                            if (obj.transform.parent != root.transform)
                                obj.transform.SetParent(root.transform, true);

                            var rend = obj.GetComponent<MeshRenderer>();
                            if (rend != null) ProfessionalMaterialFactory.ConfigureRenderer(rend);
                        }

                        totalCreated += structureObjects.Count;
                        UnityEngine.Debug.Log($"[SceneBuilder] Structure '{objModel.id}' → {structureObjects.Count} objects.");
                        continue;
                    }

                    // ── Primitive object (optionally repeated on a grid) ───
                    int   gridX  = 1;
                    int   gridZ  = 1;
                    float spaceX = 0f;
                    float spaceZ = 0f;

                    if (objModel.repeat != null)
                    {
                        if (objModel.repeat.grid != null && objModel.repeat.grid.Length >= 2)
                        {
                            gridX = Mathf.Clamp(objModel.repeat.grid[0], 1, MaxRepeatGrid);
                            gridZ = Mathf.Clamp(objModel.repeat.grid[1], 1, MaxRepeatGrid);
                        }
                        if (objModel.repeat.spacing != null && objModel.repeat.spacing.Length >= 2)
                        {
                            spaceX = objModel.repeat.spacing[0];
                            spaceZ = objModel.repeat.spacing[1];
                        }
                    }

                    Debug.Log("[SceneBuilder Diagnostic] Object ID: " + objModel.id);
                    Debug.Log("[SceneBuilder Diagnostic] Primitive: " + objModel.primitive);
                    Debug.Log("[SceneBuilder Diagnostic] MaterialRef: " + objModel.materialRef);

                    if(objModel.transform != null)
                    {
                        Debug.Log("[SceneBuilder Diagnostic] Position: " + objModel.transform.position[0] + "," +
                                  objModel.transform.position[1] + "," +
                                  objModel.transform.position[2]);
                    }

                    // Resolve mesh once, outside the inner loop.
                    Mesh sharedMesh = GetOrFallbackMesh(objModel.primitive, meshCache);

                    // Resolve material once, outside the inner loop.
                    Material sharedMat = GetOrFallbackMaterial(objModel.materialRef, materialLookup, materialFactory);

                    for (int x = 0; x < gridX; x++)
                    {
                        for (int z = 0; z < gridZ; z++)
                        {
                            string instanceName = (gridX == 1 && gridZ == 1)
                                ? objModel.id
                                : $"{objModel.id}_{x}_{z}";

                            GameObject objInstance = new GameObject(instanceName);
                            // Parent FIRST – localPosition is relative to parent space.
                            objInstance.transform.SetParent(root.transform, false);

                            // Apply transform from JSON.
                            ApplyTransform(objInstance.transform, objModel.transform);

                            // Zero Scale Protection: ensure objects are always visible.
                            if(objInstance.transform.localScale == Vector3.zero)
                            {
                                Debug.LogWarning("[SceneBuilder] Object had zero scale. Fixing.");
                                objInstance.transform.localScale = Vector3.one;
                            }

                            // Apply grid offset (additive, in local space).
                            objInstance.transform.localPosition += new Vector3(x * spaceX, 0f, z * spaceZ);

                            // Attach MeshFilter + MeshRenderer.
                            MeshFilter   mf = objInstance.AddComponent<MeshFilter>();
                            MeshRenderer mr = objInstance.AddComponent<MeshRenderer>();

                            mf.sharedMesh         = sharedMesh;  // may be null if even cube failed
                            mr.sharedMaterial     = sharedMat;

                            // GPU instancing + shadow config.
                            ProfessionalMaterialFactory.ConfigureRenderer(mr);

                            // Subtle procedural variation.
                            ProceduralVariationSystem.Apply(objInstance, mr);

                            totalCreated++;
                        }
                    }

                    UnityEngine.Debug.Log($"[SceneBuilder] Object '{objModel.id}' → {gridX * gridZ} instance(s).");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[SceneBuilder] Failed to instantiate object '{objModel?.id}': {ex.Message}");
                    // Continue with next object rather than aborting the whole build.
                }
            }

            return totalCreated;
        }



        // ─────────────────────────────────────────────────────────────────
        // Stage 4 – Post-Build Passes
        // ─────────────────────────────────────────────────────────────────

        private void RunPostBuildPasses(
            GameObject root,
            Dictionary<string, Material> materialLookup,
            SceneConfig config,
            bool hasExplicitCamera)
        {
            // Pass 1: Readability (adjacent-color contrast + brightness clamping).
            PerformanceMonitor.StartTimer("Readability Enhancement");
            try { SceneReadabilityEnhancer.Enhance(root); }
            catch (System.Exception ex) { UnityEngine.Debug.LogError($"[SceneBuilder] ReadabilityEnhancer: {ex.Message}"); }
            PerformanceMonitor.EndTimer("Readability Enhancement");

            // Pass 2: Auto-frame camera when JSON supplied no explicit position.
            if (!hasExplicitCamera)
            {
                PerformanceMonitor.StartTimer("Camera Framing");
                try
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null) SceneBoundsFramer.FrameCamera(mainCam, root);
                }
                catch (System.Exception ex) { UnityEngine.Debug.LogError($"[SceneBuilder] CameraFramer: {ex.Message}"); }
                PerformanceMonitor.EndTimer("Camera Framing");
            }

            // Pass 3: Refresh reflection probe.
            PerformanceMonitor.StartTimer("Reflection Probe Refresh");
            try { ReflectionProbeBootstrap.Refresh(); }
            catch (System.Exception ex) { UnityEngine.Debug.LogError($"[SceneBuilder] ReflectionProbe: {ex.Message}"); }
            PerformanceMonitor.EndTimer("Reflection Probe Refresh");

            // Pass 4: Visual enhancements.
            PerformanceMonitor.StartTimer("Visual Enhancements");
            try
            {
                ApplyVisualEnhancements(root, materialLookup);
                SceneVisualEnhancer.Enhance(root);
            }
            catch (System.Exception ex) { UnityEngine.Debug.LogError($"[SceneBuilder] VisualEnhancer: {ex.Message}"); }
            PerformanceMonitor.EndTimer("Visual Enhancements");
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a mesh from the cache, or creates it via MeshFactory.
        /// Falls back to cube if the requested primitive fails.
        /// Returns null only if even the cube fallback fails (should never happen).
        /// </summary>
        private Mesh GetOrFallbackMesh(string primitive, Dictionary<string, Mesh> meshCache)
        {
            // 1. Primitive Key Sanitization
            string primitiveKey = (primitive ?? FallbackPrimitive)
                                    .Trim()
                                    .ToLowerInvariant();

            if (meshCache.TryGetValue(primitiveKey, out Mesh cached)) return cached;

            // 2. MeshFactory Primary Lookup
            Mesh meshAsset = MeshFactory.CreateMesh(primitiveKey);

            // 3. MeshFactory Fallback to Cube
            if(meshAsset == null)
            {
                Debug.LogWarning("[SceneBuilder] Primitive not recognized: " + primitiveKey + " — falling back to cube.");
                meshAsset = MeshFactory.CreateMesh(FallbackPrimitive);
            }

            // 4. Unity Primitive Safety Fallback
            if(meshAsset == null)
            {
                Debug.LogWarning("[SceneBuilder] MeshFactory failed. Using Unity primitive cube mesh.");
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshAsset = fallback.GetComponent<MeshFilter>().sharedMesh;
                GameObject.Destroy(fallback);
            }

            if (meshAsset != null)
            {
                meshCache[primitiveKey] = meshAsset;
                return meshAsset;
            }

            UnityEngine.Debug.LogError($"[SceneBuilder] Critical failure: Mesh resolution for '{primitiveKey}' failed entirely.");
            return null;
        }

        /// <summary>
        /// Resolves a material from the lookup by ID.
        /// Falls back to the factory's default material if the ID is missing.
        /// Never returns null.
        /// </summary>
        private Material GetOrFallbackMaterial(
            string materialRef,
            Dictionary<string, Material> materialLookup,
            ProfessionalMaterialFactory  factory)
        {
            if (!string.IsNullOrEmpty(materialRef) &&
                materialLookup.TryGetValue(materialRef, out Material resolved))
                return resolved;

            // Try the explicit default key.
            if (materialLookup.TryGetValue(FallbackMaterialId, out Material defaultMat))
                return defaultMat;

            UnityEngine.Debug.LogWarning($"[SceneBuilder] Could not resolve material '{materialRef}' – creating ad-hoc default.");
            return factory.CreateMaterial(null);
        }

        /// <summary>
        /// Applies a JSON TransformModel to a Unity Transform with full null safety.
        /// </summary>
        private void ApplyTransform(Transform unityTransform, TransformModel modelTransform)
        {
            if (unityTransform == null || modelTransform == null) return;

            try
            {
                if (modelTransform.position != null && modelTransform.position.Length >= 3)
                    unityTransform.localPosition = new Vector3(
                        modelTransform.position[0], modelTransform.position[1], modelTransform.position[2]);

                if (modelTransform.rotation != null && modelTransform.rotation.Length >= 3)
                    unityTransform.localRotation = Quaternion.Euler(
                        modelTransform.rotation[0], modelTransform.rotation[1], modelTransform.rotation[2]);

                if (modelTransform.scale != null && modelTransform.scale.Length >= 3)
                    unityTransform.localScale = new Vector3(
                        modelTransform.scale[0], modelTransform.scale[1], modelTransform.scale[2]);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[SceneBuilder] ApplyTransform exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies visual enhancements to the generated scene.
        /// </summary>
        private void ApplyVisualEnhancements(GameObject sceneRoot, Dictionary<string, Material> materialLookup)
        {
            // Locate ground plane.
            GameObject ground = null;
            var renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("ground") || n.Contains("floor") ||
                    (r.transform.localScale.y < 1f &&
                     r.transform.localScale.x > 20f &&
                     r.transform.localScale.z > 20f))
                {
                    ground = r.gameObject;
                    break;
                }
            }

            if (ground != null && materialLookup.TryGetValue("ground", out Material groundMat))
                SceneVisualEnhancer.EnhanceGround(ground, groundMat);

            // Add a focal object for near-empty scenes.
            if (sceneRoot.transform.childCount < 5 &&
                materialLookup.TryGetValue("stone", out Material focalMat))
                SceneVisualEnhancer.CreateCenterFocalObject(sceneRoot.transform, focalMat);
        }

        // ─────────────────────────────────────────────────────────────────
        // CreateSceneConfig (unchanged)
        // ─────────────────────────────────────────────────────────────────

        private SceneConfig CreateSceneConfig(SceneModel scene)
        {
            try
            {
                SceneConfig config = SceneConfig.GetDefaults(); // Start with hardcoded professional defaults
                
                if (scene.environment != null)
                {
                    UnityEngine.Debug.Log("[SceneBuilder] Applying JSON environment overrides.");
                    
                    // Support both backgroundColor and skyColor
                    if (scene.environment.skyColor != null && scene.environment.skyColor.Length >= 3)
                        config.environment.backgroundColor = scene.environment.skyColor;
                    else if (scene.environment.backgroundColor != null && scene.environment.backgroundColor.Length >= 3)
                        config.environment.backgroundColor = scene.environment.backgroundColor;

                    if (scene.environment.skybox != null)
                    {
                        config.environment.skybox.type = scene.environment.skybox.type ?? config.environment.skybox.type;
                        if (scene.environment.skybox.sunSize > 0) 
                            config.environment.skybox.sunSize = Mathf.Clamp(scene.environment.skybox.sunSize, 0.01f, 0.5f);
                        if (scene.environment.skybox.sunSizeConvergence > 0)
                            config.environment.skybox.sunSizeConvergence = Mathf.Clamp(scene.environment.skybox.sunSizeConvergence, 1f, 20f);
                        if (scene.environment.skybox.atmosphereThickness > 0)
                            config.environment.skybox.atmosphereThickness = Mathf.Clamp(scene.environment.skybox.atmosphereThickness, 0f, 5f);
                        if (scene.environment.skybox.exposure > 0)
                            config.environment.skybox.exposure = Mathf.Clamp(scene.environment.skybox.exposure, 0f, 8f);
                        
                        if (scene.environment.skybox.color != null) config.environment.skybox.color = scene.environment.skybox.color;
                        if (scene.environment.skybox.skyTint != null) config.environment.skybox.skyTint = scene.environment.skybox.skyTint;
                        if (scene.environment.skybox.groundColor != null) config.environment.skybox.groundColor = scene.environment.skybox.groundColor;
                    }

                    if (scene.environment.fog != null)
                    {
                        config.environment.fog.enabled = scene.environment.fog.enabled;
                        config.environment.fog.mode = scene.environment.fog.mode ?? config.environment.fog.mode;
                        if (scene.environment.fog.density > 0)
                            config.environment.fog.density = Mathf.Clamp(scene.environment.fog.density, 0f, 0.1f);
                        
                        // User requested fogStart 5–200, fogEnd 10–400
                        if (scene.environment.fog.start > 0) 
                            config.environment.fog.start = Mathf.Clamp(scene.environment.fog.start, 5f, 200f);
                        if (scene.environment.fog.end > 0) 
                            config.environment.fog.end = Mathf.Clamp(scene.environment.fog.end, 10f, 400f);
                        
                        if (scene.environment.fog.color != null) config.environment.fog.color = scene.environment.fog.color;
                    }

                    if (scene.environment.ground != null)
                    {
                        config.environment.ground.enabled = scene.environment.ground.enabled;
                        if (scene.environment.ground.size != null) config.environment.ground.size = scene.environment.ground.size;
                        if (scene.environment.ground.color != null) config.environment.ground.color = scene.environment.ground.color;
                        config.environment.ground.metallic = Mathf.Clamp01(scene.environment.ground.metallic);
                        
                        // Support both glossiness and smoothness
                        float g = scene.environment.ground.smoothness > 0 ? scene.environment.ground.smoothness : scene.environment.ground.glossiness;
                        config.environment.ground.glossiness = Mathf.Clamp01(g);
                        config.environment.ground.smoothness = config.environment.ground.glossiness;
                    }

                    // User requested lighting inside environment
                    if (scene.environment.lighting != null)
                        ApplyLightingToConfig(scene.environment.lighting, config.lighting);
                }

                if (scene.lighting != null)
                    ApplyLightingToConfig(scene.lighting, config.lighting);

                if (scene.camera != null)
                {
                    UnityEngine.Debug.Log("[SceneBuilder] Applying JSON camera overrides.");
                    config.camera.mode = scene.camera.mode ?? config.camera.mode;
                    if (scene.camera.target != null) config.camera.target = scene.camera.target;
                    if (scene.camera.position != null) config.camera.position = scene.camera.position;
                    if (scene.camera.rotation != null) config.camera.rotation = scene.camera.rotation;
                    if (scene.camera.lookAt != null) config.camera.lookAt = scene.camera.lookAt;
                    
                    // User requested cameraDistance 5–80
                    if (scene.camera.distance > 0)
                        config.camera.distance = Mathf.Clamp(scene.camera.distance, 5f, 80f);
                    if (scene.camera.elevation != 0) // elevation can be negative, 0 is default
                        config.camera.elevation = Mathf.Clamp(scene.camera.elevation, -89f, 89f);
                }

                return config;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[SceneBuilder] CreateSceneConfig exception (using defaults): {ex.Message}");
                return SceneConfig.GetDefaults();
            }
        }

        private void ApplyLightingToConfig(LightingModel source, LightingConfig target)
        {
            UnityEngine.Debug.Log("[SceneBuilder] Applying JSON lighting overrides.");
            target.preset = source.preset ?? target.preset;
            target.type = source.type ?? target.type;
            if (source.rotation != null) target.rotation = source.rotation;
            if (source.direction != null) target.direction = source.direction;
            if (source.color != null) target.color = source.color;
            
            // User requested lightIntensity 0.1–5
            if (source.intensity > 0)
                target.intensity = Mathf.Clamp(source.intensity, 0.1f, 5f);
            
            target.shadows = source.shadows;
            target.shadowStrength = Mathf.Clamp01(source.shadowStrength);
            target.shadowBias = Mathf.Clamp(source.shadowBias, 0f, 2f);
            target.shadowNormalBias = Mathf.Clamp(source.shadowNormalBias, 0f, 3f);
            target.shadowNearPlane = Mathf.Clamp(source.shadowNearPlane, 0.1f, 10f);
        }
    }
}
