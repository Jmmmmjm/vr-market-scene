#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimalCrossing.TechnicalArt
{
    /// <summary>
    /// Utility and editor automation script responsible for generating, configuring, and verifying
    /// the 4 Master Material assets for the Animal Crossing VR Market Scene in URP 17.5.0:
    /// - M_AnimalCrossing_Opaque.mat
    /// - M_AnimalCrossing_Foliage.mat
    /// - M_AnimalCrossing_LiquidGlass.mat
    /// - M_AnimalCrossing_Emissive.mat
    /// Ensures SRP Batcher compatibility, GPU Instancing activation, and palette atlas binding.
    /// </summary>
    public static class MaterialAssetGenerator
    {
        public const string MaterialsFolderPath = "Assets/Materials";
        public const string PaletteAtlasPath = "Assets/Textures/T_AnimalCrossing_PaletteAtlas.png";

        public const string OpaqueShaderName = "AnimalCrossing/Opaque";
        public const string FoliageShaderName = "AnimalCrossing/Foliage";
        public const string LiquidGlassShaderName = "AnimalCrossing/LiquidGlass";
        public const string EmissiveShaderName = "AnimalCrossing/Emissive";

        public const string OpaqueMatPath = "Assets/Materials/M_AnimalCrossing_Opaque.mat";
        public const string FoliageMatPath = "Assets/Materials/M_AnimalCrossing_Foliage.mat";
        public const string LiquidGlassMatPath = "Assets/Materials/M_AnimalCrossing_LiquidGlass.mat";
        public const string EmissiveMatPath = "Assets/Materials/M_AnimalCrossing_Emissive.mat";

        [MenuItem("Animal Crossing/Generate Master Materials", priority = 10)]
        public static void GenerateAllMaterials()
        {
            Debug.Log("[MaterialAssetGenerator] Starting Master Materials Generation...");

            EnsureDirectoryExists(MaterialsFolderPath);

            Texture2D atlasTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PaletteAtlasPath);
            if (atlasTex == null)
            {
                Debug.LogWarning($"[MaterialAssetGenerator] Palette Atlas texture not found at '{PaletteAtlasPath}'. Materials will be generated with white placeholder texture.");
            }
            else
            {
                Debug.Log($"[MaterialAssetGenerator] Bound Palette Atlas '{PaletteAtlasPath}' ({atlasTex.width}x{atlasTex.height}).");
            }

            CreateOrConfigureOpaqueMaterial(atlasTex);
            CreateOrConfigureFoliageMaterial(atlasTex);
            CreateOrConfigureLiquidGlassMaterial(atlasTex);
            CreateOrConfigureEmissiveMaterial(atlasTex);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (VerifyAllMaterials(out string report))
            {
                Debug.Log($"[MaterialAssetGenerator] Master Materials Generation SUCCESSFUL!\n{report}");
            }
            else
            {
                Debug.LogError($"[MaterialAssetGenerator] Master Materials Generation Verification FAILED!\n{report}");
            }
        }

        public static void GenerateAllMaterialsFromCommandLine()
        {
            try
            {
                GenerateAllMaterials();
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MaterialAssetGenerator] Exception during command line generation: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static Material CreateOrConfigureOpaqueMaterial(Texture2D atlasTex)
        {
            Shader shader = Shader.Find(OpaqueShaderName);
            if (shader == null)
            {
                Debug.LogError($"[MaterialAssetGenerator] Could not find shader '{OpaqueShaderName}'!");
                return null;
            }

            Material mat = LoadOrCreateMaterial(OpaqueMatPath, shader);
            mat.shader = shader;
            mat.enableInstancing = true;

            if (atlasTex != null)
            {
                mat.SetTexture("_BaseMap", atlasTex);
                mat.mainTexture = atlasTex;
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_RampThreshold", 0.5f);
            mat.SetFloat("_RampSmoothness", 0.2f);
            mat.SetColor("_ShadowColor", new Color(0.72f, 0.68f, 0.76f, 1.0f));

            mat.SetColor("_RimColor", new Color(1.0f, 0.96f, 0.88f, 1.0f));
            mat.SetFloat("_RimPower", 3.0f);
            mat.SetFloat("_RimIntensity", 0.35f);
            mat.SetFloat("_Cutoff", 0.5f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrConfigureFoliageMaterial(Texture2D atlasTex)
        {
            Shader shader = Shader.Find(FoliageShaderName);
            if (shader == null)
            {
                Debug.LogError($"[MaterialAssetGenerator] Could not find shader '{FoliageShaderName}'!");
                return null;
            }

            Material mat = LoadOrCreateMaterial(FoliageMatPath, shader);
            mat.shader = shader;
            mat.enableInstancing = true;

            if (atlasTex != null)
            {
                mat.SetTexture("_BaseMap", atlasTex);
                mat.mainTexture = atlasTex;
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_RampThreshold", 0.45f);
            mat.SetFloat("_RampSmoothness", 0.25f);
            mat.SetColor("_ShadowColor", new Color(0.58f, 0.70f, 0.62f, 1.0f));

            mat.SetColor("_RimColor", new Color(0.92f, 1.0f, 0.82f, 1.0f));
            mat.SetFloat("_RimPower", 2.5f);
            mat.SetFloat("_RimIntensity", 0.45f);

            mat.SetVector("_CentroidOffsetOS", new Vector4(0f, 1.5f, 0f, 0f));
            mat.SetFloat("_NormalSoftening", 0.85f);

            mat.SetFloat("_WindSpeed", 1.8f);
            mat.SetVector("_WindFrequency", new Vector4(0.4f, 0.3f, 0.4f, 0f));
            mat.SetFloat("_WindAmplitude", 0.06f);
            mat.SetVector("_WindDirection", new Vector4(1.0f, 0.0f, 0.5f, 0f));
            mat.SetFloat("_WindHeightFactor", 0.35f);

            mat.SetColor("_TransmissionColor", new Color(0.85f, 0.95f, 0.45f, 1.0f));
            mat.SetFloat("_TransmissionStrength", 0.6f);
            mat.SetFloat("_TransmissionPower", 2.0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrConfigureLiquidGlassMaterial(Texture2D atlasTex)
        {
            Shader shader = Shader.Find(LiquidGlassShaderName);
            if (shader == null)
            {
                Debug.LogError($"[MaterialAssetGenerator] Could not find shader '{LiquidGlassShaderName}'!");
                return null;
            }

            Material mat = LoadOrCreateMaterial(LiquidGlassMatPath, shader);
            mat.shader = shader;
            mat.enableInstancing = true;

            if (atlasTex != null)
            {
                mat.SetTexture("_BaseMap", atlasTex);
                mat.mainTexture = atlasTex;
            }

            mat.SetColor("_BaseColor", new Color(0.35f, 0.72f, 0.88f, 0.65f));
            mat.SetFloat("_Mode", 1.0f); // Default to Water mode for plaza fountain
            mat.SetFloat("_BaseAlpha", 0.55f);
            mat.SetFloat("_FresnelAlpha", 0.88f);
            mat.SetFloat("_FresnelPower", 3.2f);
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetColor("_SpecularColor", Color.white);

            mat.SetFloat("_WaveSpeed", 1.5f);
            mat.SetFloat("_WaveFrequency", 3.5f);
            mat.SetFloat("_WaveAmplitude", 0.015f);
            mat.SetFloat("_RippleScale", 6.0f);
            mat.SetFloat("_RippleDistortion", 0.12f);

            mat.SetColor("_FoamColor", new Color(0.92f, 0.96f, 1.0f, 0.85f));
            mat.SetFloat("_FoamDistance", 0.06f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrConfigureEmissiveMaterial(Texture2D atlasTex)
        {
            Shader shader = Shader.Find(EmissiveShaderName);
            if (shader == null)
            {
                Debug.LogError($"[MaterialAssetGenerator] Could not find shader '{EmissiveShaderName}'!");
                return null;
            }

            Material mat = LoadOrCreateMaterial(EmissiveMatPath, shader);
            mat.shader = shader;
            mat.enableInstancing = true;

            if (atlasTex != null)
            {
                mat.SetTexture("_BaseMap", atlasTex);
                mat.mainTexture = atlasTex;
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_EmissionColor", new Color(1.8f, 1.35f, 0.75f, 1.0f));
            mat.SetFloat("_EmissionIntensity", 2.0f);
            mat.SetFloat("_FlickerSpeed", 3.5f);
            mat.SetFloat("_FlickerAmount", 0.08f);

            mat.SetFloat("_RampThreshold", 0.5f);
            mat.SetFloat("_RampSmoothness", 0.2f);
            mat.SetColor("_ShadowColor", new Color(0.72f, 0.68f, 0.76f, 1.0f));

            mat.SetColor("_RimColor", new Color(1.0f, 0.95f, 0.80f, 1.0f));
            mat.SetFloat("_RimPower", 3.0f);
            mat.SetFloat("_RimIntensity", 0.35f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string assetPath, Shader shader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
                Debug.Log($"[MaterialAssetGenerator] Created new Material asset at '{assetPath}'.");
            }
            return mat;
        }

        private static void EnsureDirectoryExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] parts = folderPath.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        public static bool VerifyAllMaterials(out string report)
        {
            var sb = new System.Text.StringBuilder();
            bool allValid = true;

            string[] paths = { OpaqueMatPath, FoliageMatPath, LiquidGlassMatPath, EmissiveMatPath };
            string[] expectedShaders = { OpaqueShaderName, FoliageShaderName, LiquidGlassShaderName, EmissiveShaderName };

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                string expectedShader = expectedShaders[i];
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat == null)
                {
                    sb.AppendLine($"[FAIL] Missing material asset at '{path}'.");
                    allValid = false;
                    continue;
                }

                if (mat.shader == null || mat.shader.name != expectedShader)
                {
                    sb.AppendLine($"[FAIL] Material '{path}' has shader '{(mat.shader != null ? mat.shader.name : "null")}', expected '{expectedShader}'.");
                    allValid = false;
                }
                else
                {
                    sb.AppendLine($"[PASS] Material '{path}' assigned to '{expectedShader}'.");
                }

                if (!mat.enableInstancing)
                {
                    sb.AppendLine($"[FAIL] Material '{path}' does not have GPU Instancing enabled.");
                    allValid = false;
                }
                else
                {
                    sb.AppendLine($"[PASS] Material '{path}' GPU Instancing is ON.");
                }
            }

            report = sb.ToString();
            return allValid;
        }
    }
}
#endif
