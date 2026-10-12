// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProceduralFountainBuilder.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Procedural Stone Fountain Centerpiece Generator (Milestone 2 / Feature F5).
//   Constructs an 8-sided beveled stone basin (~4.5m diameter) with raised coping rim,
//   stone pedestal baluster column, fluted upper basin tier, animated water pool disks,
//   upright central water jet plume, 5cm ground embedment (Y = -0.05m), and 100% compound primitive colliders.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using AnimalCrossing.TechnicalArt;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimalCrossing.Environment
{
    /// <summary>
    /// Configuration data for procedural stone fountain centerpiece generation.
    /// </summary>
    [System.Serializable]
    public struct FountainConfig
    {
        // --------------------------------------------------------------------
        // Basin Dimensions
        // --------------------------------------------------------------------
        public float basinOuterDiameter;       // 4.50m (outer radius 2.25m)
        public float basinWallThickness;       // 0.30m (inner radius 1.95m)
        public float groundAnchorDepth;        // 0.05m (recessed into ground, Y = -0.05m)
        public float footingPlinthStep;        // 0.05m (footing radius = 2.30m)
        public float copingRimElevation;       // +0.50m (top of coping rim)
        public float copingBevelRadius;        // 0.04m (45-degree chamfer bevel)
        public float basinFloorElevation;      // +0.10m (interior basin stone floor)

        // --------------------------------------------------------------------
        // Center Pedestal & Column
        // --------------------------------------------------------------------
        public float pedestalBaseRadius;       // 0.50m (stepped plinth base)
        public float pedestalBaseHeight;       // 0.22m (from Y = 0.10m to Y = 0.32m)
        public float columnBaseRadius;         // 0.35m
        public float columnWaistRadius;        // 0.26m (Animal Crossing curved baluster entasis)
        public float columnNeckRadius;         // 0.33m
        public float columnCapitalElevation;   // 1.25m (flared bell supporting upper basin)
        public float columnCapitalRadius;      // 0.65m

        // --------------------------------------------------------------------
        // Fluted Upper Basin
        // --------------------------------------------------------------------
        public float upperBasinOuterRadius;    // 0.95m (~1.90m diameter)
        public float upperBasinWallThickness;  // 0.18m (inner radius 0.77m)
        public float upperBasinBottomElevation;// 1.25m
        public float upperBasinCopingElevation;// 1.48m
        public float upperBasinFloorElevation; // 1.34m
        public int upperBasinFluteCount;        // 16 fluted lobes
        public float upperBasinFluteAmplitude; // 0.02m scalloped flute depth

        // --------------------------------------------------------------------
        // Central Finial & Spout
        // --------------------------------------------------------------------
        public float finialBaseRadius;         // 0.20m
        public float finialNozzleElevation;    // 1.65m
        public float nozzleOrificeRadius;      // 0.08m

        // --------------------------------------------------------------------
        // Water Surfaces
        // --------------------------------------------------------------------
        public float lowerWaterElevation;      // +0.38m (12cm below rim, 28cm above floor)
        public float upperWaterElevation;      // +1.42m (6cm below upper rim, 8cm above floor)
        public int waterRadialSegments;        // 16 segments
        public int waterConcentricRings;       // 4 rings for vertex wave bobbing

        // --------------------------------------------------------------------
        // Water Jet Plume
        // --------------------------------------------------------------------
        public float plumeHeight;              // 0.63m (from Y = 1.65m to Y = 2.28m)
        public float plumeMaxRadius;           // 0.22m bubbling crown
        public int plumeRadialSegments;        // 12 segments

        // --------------------------------------------------------------------
        // Color Swatches
        // --------------------------------------------------------------------
        public SwatchCoord swatchBasinOuter;
        public SwatchCoord swatchCopingRim;
        public SwatchCoord swatchBasinFloor;
        public SwatchCoord swatchPedestal;
        public SwatchCoord swatchUpperBasin;
        public SwatchCoord swatchWaterLower;
        public SwatchCoord swatchWaterUpper;
        public SwatchCoord swatchWaterPlume;

        public static FountainConfig CreateDefault()
        {
            return new FountainConfig
            {
                basinOuterDiameter = 4.50f,
                basinWallThickness = 0.30f,
                groundAnchorDepth = 0.05f,
                footingPlinthStep = 0.05f,
                copingRimElevation = 0.50f,
                copingBevelRadius = 0.04f,
                basinFloorElevation = 0.10f,

                pedestalBaseRadius = 0.50f,
                pedestalBaseHeight = 0.22f,
                columnBaseRadius = 0.35f,
                columnWaistRadius = 0.26f,
                columnNeckRadius = 0.33f,
                columnCapitalElevation = 1.25f,
                columnCapitalRadius = 0.65f,

                upperBasinOuterRadius = 0.95f,
                upperBasinWallThickness = 0.18f,
                upperBasinBottomElevation = 1.25f,
                upperBasinCopingElevation = 1.48f,
                upperBasinFloorElevation = 1.34f,
                upperBasinFluteCount = 16,
                upperBasinFluteAmplitude = 0.02f,

                finialBaseRadius = 0.20f,
                finialNozzleElevation = 1.65f,
                nozzleOrificeRadius = 0.08f,

                lowerWaterElevation = 0.38f,
                upperWaterElevation = 1.42f,
                waterRadialSegments = 16,
                waterConcentricRings = 4,

                plumeHeight = 0.63f,
                plumeMaxRadius = 0.22f,
                plumeRadialSegments = 12,

                swatchBasinOuter = Swatches.StoneBasinBlueGrey,
                swatchCopingRim = Swatches.StoneCopingPaleBlue,
                swatchBasinFloor = Swatches.StoneGraniteBase,
                swatchPedestal = Swatches.StonePlazaLimestone,
                swatchUpperBasin = Swatches.StonePlazaLimestone,
                swatchWaterLower = Swatches.WaterFountainAzure,
                swatchWaterUpper = Swatches.WaterFountainAzure,
                swatchWaterPlume = Swatches.WaterJetStreamPale
            };
        }
    }

    /// <summary>
    /// Master procedural generator for the Stone Fountain Centerpiece.
    /// Conforms strictly to requirements R1, R4, R7 and test suite F5, T3-07, T3-14.
    /// </summary>
    public static class ProceduralFountainBuilder
    {
        public const string OpaqueMatPath = "Assets/Materials/M_AnimalCrossing_Opaque.mat";
        public const string LiquidGlassMatPath = "Assets/Materials/M_AnimalCrossing_LiquidGlass.mat";

        /// <summary>
        /// Builds the complete Stone Fountain Centerpiece GameObject hierarchy at origin.
        /// </summary>
        public static GameObject CreateFountain(Transform parent = null)
        {
            FountainConfig config = FountainConfig.CreateDefault();

            Material stoneMat = null;
            Material liquidGlassMat = null;

#if UNITY_EDITOR
            stoneMat = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMatPath);
            liquidGlassMat = AssetDatabase.LoadAssetAtPath<Material>(LiquidGlassMatPath);
#endif

            return BuildFountainGameObject(parent, config, stoneMat, liquidGlassMat);
        }

        /// <summary>
        /// Assembles the complete StoneFountain GameObject hierarchy with meshes, materials, and colliders.
        /// </summary>
        public static GameObject BuildFountainGameObject(Transform parent, FountainConfig config, Material stoneMat, Material liquidGlassMat)
        {
            GameObject fountainGo = new GameObject("StoneFountain");
            if (parent != null)
            {
                fountainGo.transform.SetParent(parent, false);
            }
            fountainGo.transform.localPosition = Vector3.zero;
            fountainGo.transform.localRotation = Quaternion.identity;

            // 1. Stone Masonry Mesh (Basin, Pedestal, Upper Tier, Finial)
            Mesh stoneMesh = CreateStoneFountainMesh(config);
            GameObject stoneGo = new GameObject("FountainStone");
            stoneGo.transform.SetParent(fountainGo.transform, false);
            stoneGo.transform.localPosition = Vector3.zero;
            var mfStone = stoneGo.AddComponent<MeshFilter>();
            mfStone.sharedMesh = stoneMesh;
            var mrStone = stoneGo.AddComponent<MeshRenderer>();
            mrStone.sharedMaterial = stoneMat;

            // Center Pedestal Anchor (F5-03)
            GameObject pedestalGo = new GameObject("Pedestal");
            pedestalGo.transform.SetParent(fountainGo.transform, false);
            pedestalGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            // 2. Animated Water Surface Mesh (Lower Pool Disk + Upper Pool Disk)
            Mesh waterMesh = CreateFountainWaterMesh(config);
            GameObject waterGo = new GameObject("WaterDisk");
            waterGo.transform.SetParent(fountainGo.transform, false);
            waterGo.transform.localPosition = new Vector3(0f, config.lowerWaterElevation, 0f);
            var mfWater = waterGo.AddComponent<MeshFilter>();
            mfWater.sharedMesh = waterMesh;
            var mrWater = waterGo.AddComponent<MeshRenderer>();
            mrWater.sharedMaterial = liquidGlassMat;
            GameObject waterAlias = new GameObject("FountainWater");
            waterAlias.transform.SetParent(fountainGo.transform, false);
            waterAlias.transform.localPosition = new Vector3(0f, config.lowerWaterElevation, 0f);

            // 3. Central Water Jet Plume Mesh (Upright Bubbling Sprout & Splash Skirt)
            Mesh plumeMesh = CreateWaterJetPlumeMesh(config);
            GameObject plumeGo = new GameObject("FountainPlume");
            plumeGo.transform.SetParent(fountainGo.transform, false);
            plumeGo.transform.localPosition = Vector3.zero;
            var mfPlume = plumeGo.AddComponent<MeshFilter>();
            mfPlume.sharedMesh = plumeMesh;
            var mrPlume = plumeGo.AddComponent<MeshRenderer>();
            mrPlume.sharedMaterial = liquidGlassMat;

            // 4. Compound Primitive Colliders (100% Box & Capsule, 0% MeshCollider)
            BuildFountainColliders(fountainGo.transform, config);

            Debug.Log("[ProceduralFountainBuilder] Stone Fountain Centerpiece successfully assembled at plaza center.");
            return fountainGo;
        }

        // --------------------------------------------------------------------
        // 1. Stone Masonry Procedural Mesh Generator
        // --------------------------------------------------------------------

        public static Mesh CreateStoneFountainMesh(FountainConfig config)
        {
            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();

            Vector2 uvOuter  = ProceduralMeshBuilder.GetSwatchUV(config.swatchBasinOuter.Col, config.swatchBasinOuter.Row);
            Vector2 uvCoping = ProceduralMeshBuilder.GetSwatchUV(config.swatchCopingRim.Col, config.swatchCopingRim.Row);
            Vector2 uvFloor  = ProceduralMeshBuilder.GetSwatchUV(config.swatchBasinFloor.Col, config.swatchBasinFloor.Row);
            Vector2 uvPed    = ProceduralMeshBuilder.GetSwatchUV(config.swatchPedestal.Col, config.swatchPedestal.Row);
            Vector2 uvUpper  = ProceduralMeshBuilder.GetSwatchUV(config.swatchUpperBasin.Col, config.swatchUpperBasin.Row);

            float rOut = config.basinOuterDiameter * 0.5f;          // 2.25m
            float rIn = rOut - config.basinWallThickness;           // 1.95m
            float rFoot = rOut + config.footingPlinthStep;          // 2.30m
            float b = config.copingBevelRadius;                     // 0.04m

            float yFootBot = -config.groundAnchorDepth;             // -0.05m (recessed into ground)
            float yFootTop = 0.05f;                                  // +0.05m
            float yChamferBot = config.copingRimElevation - b;       // 0.46m
            float yRimTop = config.copingRimElevation;               // 0.50m
            float yFloor = config.basinFloorElevation;               // 0.10m
            float rPedBase = config.pedestalBaseRadius;              // 0.50m

            // Precompute 8 Octagonal Radial Slices (Exact Analytical Symmetry)
            int octSegs = 8;
            Vector3[] dir = new Vector3[octSegs + 1];
            for (int i = 0; i <= octSegs; i++)
            {
                float angle = (i % octSegs) * (Mathf.PI * 2f / octSegs);
                dir[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }

            // A. Octagonal Outer Basin Walls & Coping Rim
            for (int i = 0; i < octSegs; i++)
            {
                Vector3 d0 = dir[i];
                Vector3 d1 = dir[i + 1];
                Vector3 midNormal = ((d0 + d1) * 0.5f).normalized;

                // 1. Footing Plinth Vertical Skirt (Y = -0.05m to Y = +0.05m at R = 2.30m)
                Vector3 vFootBot0 = d0 * rFoot + Vector3.up * yFootBot;
                Vector3 vFootBot1 = d1 * rFoot + Vector3.up * yFootBot;
                Vector3 vFootTop0 = d0 * rFoot + Vector3.up * yFootTop;
                Vector3 vFootTop1 = d1 * rFoot + Vector3.up * yFootTop;
                buffer.AddQuad(vFootBot0, vFootBot1, vFootTop1, vFootTop0, midNormal, uvFloor);

                // 2. Footing Ledge (R = 2.30m to R = 2.25m at Y = +0.05m)
                Vector3 vWallBot0 = d0 * rOut + Vector3.up * yFootTop;
                Vector3 vWallBot1 = d1 * rOut + Vector3.up * yFootTop;
                buffer.AddQuad(vFootTop0, vFootTop1, vWallBot1, vWallBot0, Vector3.up, uvFloor);

                // 3. Outer Wall Face (Y = +0.05m to Y = 0.46m at R = 2.25m)
                Vector3 vChamOut0 = d0 * rOut + Vector3.up * yChamferBot;
                Vector3 vChamOut1 = d1 * rOut + Vector3.up * yChamferBot;
                buffer.AddQuad(vWallBot0, vWallBot1, vChamOut1, vChamOut0, midNormal, uvOuter);

                // 4. Outer Coping Chamfer (45-degree bevel up-inward to Y = 0.50m, R = 2.21m)
                Vector3 vRimOut0 = d0 * (rOut - b) + Vector3.up * yRimTop;
                Vector3 vRimOut1 = d1 * (rOut - b) + Vector3.up * yRimTop;
                Vector3 nChamOut = (midNormal + Vector3.up).normalized;
                buffer.AddQuad(vChamOut0, vChamOut1, vRimOut1, vRimOut0, nChamOut, uvCoping);

                // 5. Coping Top Crown (Flat crowning from R = 2.21m to R = 1.99m at Y = 0.50m)
                Vector3 vRimIn0 = d0 * (rIn + b) + Vector3.up * yRimTop;
                Vector3 vRimIn1 = d1 * (rIn + b) + Vector3.up * yRimTop;
                buffer.AddQuad(vRimOut0, vRimOut1, vRimIn1, vRimIn0, Vector3.up, uvCoping);

                // 6. Inner Coping Chamfer (45-degree bevel down-inward to Y = 0.46m, R = 1.95m)
                Vector3 vChamIn0 = d0 * rIn + Vector3.up * yChamferBot;
                Vector3 vChamIn1 = d1 * rIn + Vector3.up * yChamferBot;
                Vector3 nChamIn = (-midNormal + Vector3.up).normalized;
                buffer.AddQuad(vRimIn0, vRimIn1, vChamIn1, vChamIn0, nChamIn, uvCoping);

                // 7. Inner Wall Face (Y = 0.46m down to Y = 0.10m at R = 1.95m)
                Vector3 vFlrOut0 = d0 * rIn + Vector3.up * yFloor;
                Vector3 vFlrOut1 = d1 * rIn + Vector3.up * yFloor;
                buffer.AddQuad(vChamIn0, vChamIn1, vFlrOut1, vFlrOut0, -midNormal, uvOuter);

                // 8. Basin Floor (From inner wall R = 1.95m inward to pedestal base R = 0.50m at Y = 0.10m)
                Vector3 vFlrIn0 = d0 * rPedBase + Vector3.up * yFloor;
                Vector3 vFlrIn1 = d1 * rPedBase + Vector3.up * yFloor;
                buffer.AddQuad(vFlrOut0, vFlrOut1, vFlrIn1, vFlrIn0, Vector3.up, uvFloor);
            }

            // B. Center Pedestal Baluster Column (16 radial segments)
            int colSegs = 16;
            Vector3[] colDir = new Vector3[colSegs + 1];
            for (int j = 0; j <= colSegs; j++)
            {
                float angle = (j % colSegs) * (Mathf.PI * 2f / colSegs);
                colDir[j] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }

            // 1. Pedestal Plinth Base: stepped ring from Y = 0.10m to Y = 0.32m
            AddCylinderSection(buffer, colDir, colSegs, yFloor, 0.22f, rPedBase, rPedBase * 0.90f, uvFloor);
            AddCylinderSection(buffer, colDir, colSegs, 0.22f, 0.32f, rPedBase * 0.90f, config.columnBaseRadius, uvPed);

            // 2. Baluster Shaft: entasis hourglass curvature from Y = 0.32m to Y = 1.12m
            int balusterSteps = 5;
            float yStartBal = 0.32f;
            float yEndBal = 1.12f;
            float balSpan = yEndBal - yStartBal;

            for (int s = 0; s < balusterSteps; s++)
            {
                float t0 = (float)s / balusterSteps;
                float t1 = (float)(s + 1) / balusterSteps;

                float y0 = yStartBal + t0 * balSpan;
                float y1 = yStartBal + t1 * balSpan;

                float r0 = EvalBalusterRadius(t0, config.columnBaseRadius, config.columnWaistRadius, config.columnNeckRadius);
                float r1 = EvalBalusterRadius(t1, config.columnBaseRadius, config.columnWaistRadius, config.columnNeckRadius);

                AddCylinderSection(buffer, colDir, colSegs, y0, y1, r0, r1, uvPed);
            }

            // 3. Column Capital: flared bell from Y = 1.12m to Y = 1.25m supporting upper basin
            AddCylinderSection(buffer, colDir, colSegs, 1.12f, config.columnCapitalElevation, config.columnNeckRadius, config.columnCapitalRadius, uvPed);

            // C. Fluted Upper Water Basin Tier (16 scalloped lobes)
            int fluteSegs = 16;
            float rUpOut = config.upperBasinOuterRadius;             // 0.95m
            float rUpIn  = rUpOut - config.upperBasinWallThickness;  // 0.77m
            float yUpBot = config.upperBasinBottomElevation;         // 1.25m
            float yUpRim = config.upperBasinCopingElevation;         // 1.48m
            float yUpFlr = config.upperBasinFloorElevation;          // 1.34m
            float fAmp   = config.upperBasinFluteAmplitude;          // 0.02m

            for (int j = 0; j < fluteSegs; j++)
            {
                float a0 = (j % fluteSegs) * (Mathf.PI * 2f / fluteSegs);
                float a1 = ((j + 1) % fluteSegs) * (Mathf.PI * 2f / fluteSegs);

                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 midN = ((d0 + d1) * 0.5f).normalized;

                float rFlute0 = rUpOut + fAmp * Mathf.Cos(j * Mathf.PI * 2f);
                float rFlute1 = rUpOut + fAmp * Mathf.Cos((j + 1) * Mathf.PI * 2f);

                // Upper Basin Outer Bowl curved shell
                Vector3 vBowlBot0 = d0 * config.columnCapitalRadius + Vector3.up * yUpBot;
                Vector3 vBowlBot1 = d1 * config.columnCapitalRadius + Vector3.up * yUpBot;
                Vector3 vBowlRim0 = d0 * rFlute0 + Vector3.up * yUpRim;
                Vector3 vBowlRim1 = d1 * rFlute1 + Vector3.up * yUpRim;
                buffer.AddQuad(vBowlBot0, vBowlBot1, vBowlRim1, vBowlRim0, (midN + Vector3.up * 0.3f).normalized, uvUpper);

                // Upper Basin Coping Rim top
                Vector3 vBowlInRim0 = d0 * rUpIn + Vector3.up * (yUpRim - 0.04f);
                Vector3 vBowlInRim1 = d1 * rUpIn + Vector3.up * (yUpRim - 0.04f);
                buffer.AddQuad(vBowlRim0, vBowlRim1, vBowlInRim1, vBowlInRim0, Vector3.up, uvCoping);

                // Upper Basin Interior Wall
                Vector3 vBowlFlr0 = d0 * rUpIn + Vector3.up * yUpFlr;
                Vector3 vBowlFlr1 = d1 * rUpIn + Vector3.up * yUpFlr;
                buffer.AddQuad(vBowlInRim0, vBowlInRim1, vBowlFlr1, vBowlFlr0, -midN, uvUpper);

                // Upper Basin Interior Floor (inward to central finial urn base)
                Vector3 vFinBot0 = d0 * config.finialBaseRadius + Vector3.up * yUpFlr;
                Vector3 vFinBot1 = d1 * config.finialBaseRadius + Vector3.up * yUpFlr;
                buffer.AddQuad(vBowlFlr0, vBowlFlr1, vFinBot1, vFinBot0, Vector3.up, uvFloor);
            }

            // D. Central Finial Nozzle Urn
            AddCylinderSection(buffer, colDir, colSegs, yUpFlr, 1.45f, config.finialBaseRadius, 0.16f, uvPed);
            AddCylinderSection(buffer, colDir, colSegs, 1.45f, 1.58f, 0.16f, 0.12f, uvPed);
            AddCylinderSection(buffer, colDir, colSegs, 1.58f, config.finialNozzleElevation, 0.12f, config.nozzleOrificeRadius, uvCoping);

            return buffer.Build("Mesh_StoneFountain_Opaque");
        }

        private static float EvalBalusterRadius(float t, float rBase, float rWaist, float rNeck)
        {
            float mid = 0.5f;
            float dt = (t - mid) / mid;
            float curve = dt * dt;
            float endR = (t < 0.5f) ? rBase : rNeck;
            return Mathf.Lerp(rWaist, endR, curve);
        }

        private static void AddCylinderSection(ProceduralMeshBuilder.MeshBuffer buffer,
            Vector3[] dirs, int segs, float y0, float y1, float r0, float r1, Vector2 uv)
        {
            for (int i = 0; i < segs; i++)
            {
                Vector3 d0 = dirs[i];
                Vector3 d1 = dirs[i + 1];

                Vector3 v00 = d0 * r0 + Vector3.up * y0;
                Vector3 v01 = d1 * r0 + Vector3.up * y0;
                Vector3 v11 = d1 * r1 + Vector3.up * y1;
                Vector3 v10 = d0 * r1 + Vector3.up * y1;

                Vector3 n0 = d0;
                Vector3 n1 = d1;
                buffer.AddQuad(v00, v01, v11, v10, n0, n1, n1, n0, uv);
            }
        }

        // --------------------------------------------------------------------
        // 2. Animated Water Pool Disks Generator
        // --------------------------------------------------------------------

        public static Mesh CreateFountainWaterMesh(FountainConfig config)
        {
            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 waterUV = ProceduralMeshBuilder.GetSwatchUV(config.swatchWaterLower.Col, config.swatchWaterLower.Row);

            int segs = config.waterRadialSegments;
            int rings = config.waterConcentricRings;

            float[] cosAngles = new float[segs + 1];
            float[] sinAngles = new float[segs + 1];
            for (int i = 0; i <= segs; i++)
            {
                float angle = (i % segs) * (Mathf.PI * 2f / segs);
                cosAngles[i] = Mathf.Cos(angle);
                sinAngles[i] = Mathf.Sin(angle);
            }

            // A. Lower Basin Pool Disk (Annular concentric rings at WaterDisk root Y = +0.38m)
            float rLowerIn  = 0.32f;
            float rLowerOut = config.basinOuterDiameter * 0.5f - config.basinWallThickness; // 1.95m
            float yLower    = 0.0f;                                                         // Root sits at +0.38m

            for (int r = 0; r < rings; r++)
            {
                float rad0 = Mathf.Lerp(rLowerIn, rLowerOut, (float)r / rings);
                float rad1 = Mathf.Lerp(rLowerIn, rLowerOut, (float)(r + 1) / rings);

                for (int i = 0; i < segs; i++)
                {
                    Vector3 v00 = new Vector3(rad0 * cosAngles[i], yLower, rad0 * sinAngles[i]);
                    Vector3 v01 = new Vector3(rad0 * cosAngles[i + 1], yLower, rad0 * sinAngles[i + 1]);
                    Vector3 v11 = new Vector3(rad1 * cosAngles[i + 1], yLower, rad1 * sinAngles[i + 1]);
                    Vector3 v10 = new Vector3(rad1 * cosAngles[i], yLower, rad1 * sinAngles[i]);

                    buffer.AddQuad(v00, v01, v11, v10, Vector3.up, waterUV);
                }
            }

            // B. Upper Basin Pool Disk (Concentric rings at Y = +1.42m -> local +1.04m)
            float rUpperIn  = config.finialBaseRadius;                                          // 0.20m
            float rUpperOut = config.upperBasinOuterRadius - config.upperBasinWallThickness;  // 0.77m
            float yUpper    = config.upperWaterElevation - config.lowerWaterElevation;        // +1.04m
            int upperRings = 2;

            for (int r = 0; r < upperRings; r++)
            {
                float rad0 = Mathf.Lerp(rUpperIn, rUpperOut, (float)r / upperRings);
                float rad1 = Mathf.Lerp(rUpperIn, rUpperOut, (float)(r + 1) / upperRings);

                for (int i = 0; i < segs; i++)
                {
                    Vector3 v00 = new Vector3(rad0 * cosAngles[i], yUpper, rad0 * sinAngles[i]);
                    Vector3 v01 = new Vector3(rad0 * cosAngles[i + 1], yUpper, rad0 * sinAngles[i + 1]);
                    Vector3 v11 = new Vector3(rad1 * cosAngles[i + 1], yUpper, rad1 * sinAngles[i + 1]);
                    Vector3 v10 = new Vector3(rad1 * cosAngles[i], yUpper, rad1 * sinAngles[i]);

                    buffer.AddQuad(v00, v01, v11, v10, Vector3.up, waterUV);
                }
            }

            return buffer.Build("Mesh_FountainWater_Disks");
        }

        // --------------------------------------------------------------------
        // 3. Central Water Jet Plume & Bubbling Sprout Generator
        // --------------------------------------------------------------------

        public static Mesh CreateWaterJetPlumeMesh(FountainConfig config)
        {
            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 plumeUV = ProceduralMeshBuilder.GetSwatchUV(config.swatchWaterPlume.Col, config.swatchWaterPlume.Row);

            int segs = config.plumeRadialSegments;
            float yBase = config.finialNozzleElevation; // 1.65m

            float[] cosA = new float[segs + 1];
            float[] sinA = new float[segs + 1];
            for (int i = 0; i <= segs; i++)
            {
                float a = (i % segs) * (Mathf.PI * 2f / segs);
                cosA[i] = Mathf.Cos(a);
                sinA[i] = Mathf.Sin(a);
            }

            // Height and Radius profile for upright bubbling fountain sprout
            float[] sliceY = new float[] { 0.0f,  0.15f, 0.33f, 0.45f, 0.57f, 0.63f };
            float[] sliceR = new float[] { 0.08f, 0.13f, 0.19f, 0.16f, 0.22f, 0.00f };
            int sliceCount = sliceY.Length;

            for (int s = 0; s < sliceCount - 1; s++)
            {
                float y0 = yBase + sliceY[s];
                float y1 = yBase + sliceY[s + 1];
                float r0 = sliceR[s];
                float r1 = sliceR[s + 1];

                for (int i = 0; i < segs; i++)
                {
                    Vector3 v00 = new Vector3(r0 * cosA[i], y0, r0 * sinA[i]);
                    Vector3 v01 = new Vector3(r0 * cosA[i + 1], y0, r0 * sinA[i + 1]);
                    Vector3 v11 = new Vector3(r1 * cosA[i + 1], y1, r1 * sinA[i + 1]);
                    Vector3 v10 = new Vector3(r1 * cosA[i], y1, r1 * sinA[i]);

                    Vector3 n00 = (new Vector3(cosA[i], 0.2f, sinA[i])).normalized;
                    Vector3 n01 = (new Vector3(cosA[i + 1], 0.2f, sinA[i + 1])).normalized;

                    if (s == sliceCount - 2)
                    {
                        Vector3 vApex = new Vector3(0f, y1, 0f);
                        buffer.AddTriangle(v00, v01, vApex, n00, n01, Vector3.up, plumeUV);
                    }
                    else
                    {
                        buffer.AddQuad(v00, v01, v11, v10, n00, n01, n01, n00, plumeUV);
                    }
                }
            }

            // Cascading Splash Skirt (Flared water veil spilling from mid-plume)
            float ySkirtTop = yBase + 0.33f; // 1.98m
            float ySkirtBot = yBase + 0.18f; // 1.83m
            float rSkirtTop = 0.19f;
            float rSkirtBot = 0.28f;

            for (int i = 0; i < segs; i++)
            {
                Vector3 v00 = new Vector3(rSkirtTop * cosA[i], ySkirtTop, rSkirtTop * sinA[i]);
                Vector3 v01 = new Vector3(rSkirtTop * cosA[i + 1], ySkirtTop, rSkirtTop * sinA[i + 1]);
                Vector3 v11 = new Vector3(rSkirtBot * cosA[i + 1], ySkirtBot, rSkirtBot * sinA[i + 1]);
                Vector3 v10 = new Vector3(rSkirtBot * cosA[i], ySkirtBot, rSkirtBot * sinA[i]);

                Vector3 nSkirt = (new Vector3(cosA[i], -0.3f, sinA[i])).normalized;
                buffer.AddQuad(v00, v01, v11, v10, nSkirt, plumeUV);
            }

            return buffer.Build("Mesh_FountainWater_Plume");
        }

        // --------------------------------------------------------------------
        // 4. Primitive Colliders Assembly (100% Box & Capsule)
        // --------------------------------------------------------------------

        private static void BuildFountainColliders(Transform parent, FountainConfig config)
        {
            GameObject colGroup = new GameObject("Colliders");
            colGroup.transform.SetParent(parent, false);
            colGroup.transform.localPosition = Vector3.zero;

            float rOut = config.basinOuterDiameter * 0.5f;          // 2.25m
            float rIn  = rOut - config.basinWallThickness;          // 1.95m
            float rMid = (rOut + rIn) * 0.5f;                       // 2.10m
            float wallThick = config.basinWallThickness + 0.04f;    // 0.34m
            float wallHeight = config.copingRimElevation + 0.05f;   // 0.55m (from Y = 0.0m to 0.55m)
            float wallCenterY = wallHeight * 0.5f;                  // 0.275m

            float chordLen = 2f * rMid * Mathf.Tan(Mathf.PI / 8f);  // ~1.74m
            float boxLen = chordLen + 0.02f;                        // 1.76m

            // A. 8 Octagonal Perimeter Basin Wall Colliders
            for (int k = 0; k < 8; k++)
            {
                float midAngle = (k + 0.5f) * (Mathf.PI * 2f / 8f);
                float posX = rMid * Mathf.Cos(midAngle);
                float posZ = rMid * Mathf.Sin(midAngle);

                GameObject wallColGo = new GameObject($"WallCollider_{k}");
                wallColGo.transform.SetParent(colGroup.transform, false);
                wallColGo.transform.localPosition = new Vector3(posX, wallCenterY, posZ);

                float rotY = -(midAngle * Mathf.Rad2Deg) + 90f;
                wallColGo.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

                var box = wallColGo.AddComponent<BoxCollider>();
                box.size = new Vector3(boxLen, wallHeight, wallThick);
            }

            // B. Center Pedestal Column Collider (CapsuleCollider)
            GameObject colColGo = new GameObject("ColumnCollider");
            colColGo.transform.SetParent(colGroup.transform, false);
            colColGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            var capsule = colColGo.AddComponent<CapsuleCollider>();
            capsule.radius = 0.35f;
            capsule.height = 1.50f;
            capsule.direction = 1;

            // C. Basin Floor Collider (BoxCollider)
            GameObject flrColGo = new GameObject("BasinFloorCollider");
            flrColGo.transform.SetParent(colGroup.transform, false);
            flrColGo.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            var flrBox = flrColGo.AddComponent<BoxCollider>();
            flrBox.size = new Vector3(4.0f, 0.15f, 4.0f);

            // D. Upper Basin Support Collider (CapsuleCollider)
            GameObject upColGo = new GameObject("UpperBasinCollider");
            upColGo.transform.SetParent(colGroup.transform, false);
            upColGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            var upCapsule = upColGo.AddComponent<CapsuleCollider>();
            upCapsule.radius = 0.95f;
            upCapsule.height = 0.30f;
            upCapsule.direction = 1;
        }

#if UNITY_EDITOR
        [MenuItem("AnimalCrossing/Build Stone Fountain", priority = 21)]
        public static void MenuItem_BuildFountain()
        {
            GameObject existing = GameObject.Find("StoneFountain");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject fountain = CreateFountain();
            Undo.RegisterCreatedObjectUndo(fountain, "Build Stone Fountain");
            Selection.activeGameObject = fountain;
        }
#endif
    }
}
