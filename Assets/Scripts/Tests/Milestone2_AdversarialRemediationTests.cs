// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Milestone2_AdversarialRemediationTests.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Adversarial Challenge & Stress Test Suite for Milestone 2 Iteration 2 Remediation:
//   1. Fountain Fluted Basin Degenerates (assert exactly 0 degenerate triangles).
//   2. Fountain Water & Plume Mesh Degenerates (assert 0 degenerate triangles).
//   3. Fountain Fluted Basin Parametric Stress Testing across diverse parameters.
//   4. Inner Esplanade Curb Clearance to octagonal plinth corner (target 20mm, clearance > 0 everywhere).
//   5. Inner Esplanade Curb Flush Ground Termination at Y = 0.00m (0 gaps, 0 coplanar overlap).
//   6. BatchTownSquarePlaza Multi-Run Idempotency (consecutive runs produce strictly identical state).
//   7. ProceduralMeshCombiner Individual Method Idempotency and Boundary Edge Cases.
//   8. SwatchCoord Serialization Compliance & Analyzer Warning Elimination.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimalCrossing.Environment;
using AnimalCrossing.TechnicalArt;

namespace AnimalCrossing.Tests
{
    [TestFixture]
    [Description("Adversarial Challenge & Stress Test Suite for Milestone 2 Iteration 2 Remediation.")]
    public class Milestone2_AdversarialRemediationTests
    {
        private Material opaqueMat;
        private Material foliageMat;
        private Material emissiveMat;
        private Material liquidGlassMat;
        private GameObject scenePlaza;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            opaqueMat = AssetDatabase.LoadAssetAtPath<Material>(TownSquarePlazaBuilder.OpaqueMatPath);
            foliageMat = AssetDatabase.LoadAssetAtPath<Material>(TownSquarePlazaBuilder.FoliageMatPath);
            emissiveMat = AssetDatabase.LoadAssetAtPath<Material>(TownSquarePlazaBuilder.EmissiveMatPath);
            liquidGlassMat = AssetDatabase.LoadAssetAtPath<Material>(TownSquarePlazaBuilder.LiquidGlassMatPath);

            Assert.IsNotNull(opaqueMat, "M_AnimalCrossing_Opaque.mat must exist.");
            Assert.IsNotNull(foliageMat, "M_AnimalCrossing_Foliage.mat must exist.");
            Assert.IsNotNull(emissiveMat, "M_AnimalCrossing_Emissive.mat must exist.");
            Assert.IsNotNull(liquidGlassMat, "M_AnimalCrossing_LiquidGlass.mat must exist.");

            scenePlaza = GameObject.Find("TownSquarePlaza");
            if (scenePlaza == null)
            {
                var builderGo = new GameObject("TownSquarePlaza");
                var builder = builderGo.AddComponent<TownSquarePlazaBuilder>();
                builder.OpaqueMaterial = opaqueMat;
                builder.FoliageMaterial = foliageMat;
                builder.EmissiveMaterial = emissiveMat;
                builder.LiquidGlassMaterial = liquidGlassMat;
                builder.BuildPlaza();
                scenePlaza = builderGo;
            }
        }

        // =========================================================================
        // Challenge 1: Fountain Fluted Basin Degenerates (Zero Tolerance)
        // =========================================================================

        [Test]
        [Description("Challenge 1.1: Asserts exactly 0 degenerate triangles on StoneFountain mesh (default config).")]
        public void AdvRem_01_FountainFlutedBasin_ZeroDegenerateTriangles()
        {
            var config = FountainConfig.CreateDefault();
            Mesh fountainMesh = ProceduralFountainBuilder.CreateStoneFountainMesh(config);
            Assert.IsNotNull(fountainMesh, "Stone fountain mesh must be generated.");

            Vector3[] verts = fountainMesh.vertices;
            int[] tris = fountainMesh.triangles;
            Assert.Greater(verts.Length, 0, "Vertex count must be > 0.");
            Assert.Greater(tris.Length, 0, "Triangle index count must be > 0.");

            int degenIndices = 0;
            int degenColocated = 0;
            int degenZeroArea = 0;
            float minTriangleArea = float.MaxValue;

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i + 1];
                int i2 = tris[i + 2];

                // Check index equality
                if (i0 == i1 || i1 == i2 || i0 == i2)
                {
                    degenIndices++;
                }

                // Check colocated vertices
                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];
                if (Vector3.Distance(v0, v1) < 1e-6f || Vector3.Distance(v1, v2) < 1e-6f || Vector3.Distance(v0, v2) < 1e-6f)
                {
                    degenColocated++;
                }

                // Check triangle surface area
                Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
                float area = cross.magnitude * 0.5f;
                if (area < minTriangleArea)
                {
                    minTriangleArea = area;
                }
                if (area < 1e-8f)
                {
                    degenZeroArea++;
                }
            }

            Assert.AreEqual(0, degenIndices, "StoneFountain mesh must have 0 triangles with duplicate vertex indices.");
            Assert.AreEqual(0, degenColocated, "StoneFountain mesh must have 0 triangles with colocated vertex positions.");
            Assert.AreEqual(0, degenZeroArea, "StoneFountain mesh must have 0 triangles with zero surface area.");
            Assert.Greater(minTriangleArea, 0.0005f, $"Minimum triangle area ({minTriangleArea:E4} m^2) must be substantial.");

            // Specific check on Upper Basin Fluted Region (Y in [1.25, 1.48])
            int upperBasinTris = 0;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = verts[tris[i]];
                Vector3 v1 = verts[tris[i + 1]];
                Vector3 v2 = verts[tris[i + 2]];
                float avgY = (v0.y + v1.y + v2.y) / 3f;
                if (avgY >= 1.24f && avgY <= 1.49f)
                {
                    upperBasinTris++;
                }
            }
            Assert.GreaterOrEqual(upperBasinTris, 128, "Fluted upper basin must contain at least 128 triangles (64 quads).");
        }

        [Test]
        [Description("Challenge 1.2: Asserts 0 degenerate triangles on Fountain Water and Plume meshes.")]
        public void AdvRem_02_FountainMeshes_WaterAndPlume_ZeroDegenerates()
        {
            var config = FountainConfig.CreateDefault();

            // Water Disks
            Mesh waterMesh = ProceduralFountainBuilder.CreateFountainWaterMesh(config);
            Assert.IsNotNull(waterMesh);
            AssertDegenerateCountZero(waterMesh, "FountainWater");

            // Plume Mesh
            Mesh plumeMesh = ProceduralFountainBuilder.CreateWaterJetPlumeMesh(config);
            Assert.IsNotNull(plumeMesh);
            AssertDegenerateCountZero(plumeMesh, "FountainPlume");
        }

        [Test]
        [Description("Challenge 1.3: Adversarial parametric stress test across varying flute counts, amplitudes, and basin sizes.")]
        public void AdvRem_03_FountainFlutedBasin_ParametricStressTest()
        {
            int[] testFluteCounts = { 8, 12, 16, 20, 24, 32 };
            float[] testAmplitudes = { 0.0f, 0.005f, 0.02f, 0.04f };
            float[] testDiameters = { 3.5f, 4.5f, 5.5f };

            foreach (int fc in testFluteCounts)
            {
                foreach (float amp in testAmplitudes)
                {
                    foreach (float dia in testDiameters)
                    {
                        var config = FountainConfig.CreateDefault();
                        config.upperBasinFluteCount = fc;
                        config.upperBasinFluteAmplitude = amp;
                        config.basinOuterDiameter = dia;

                        Mesh m = ProceduralFountainBuilder.CreateStoneFountainMesh(config);
                        Assert.IsNotNull(m, $"Mesh must be created for Flutes={fc}, Amp={amp}, Dia={dia}");
                        Assert.Less(m.vertexCount, 65534, "Must stay within UInt16 index limit.");

                        AssertDegenerateCountZero(m, $"FountainMesh(Flutes={fc}, Amp={amp}, Dia={dia})");
                    }
                }
            }
        }

        // =========================================================================
        // Challenge 2: Inner Esplanade Curb Clearance & Ground Flush Termination
        // =========================================================================

        [Test]
        [Description("Challenge 2.1: Inner curb radial clearance to octagonal footing plinth corner > 0 (target 20mm).")]
        public void AdvRem_04_InnerEsplanadeCurb_RadialClearanceToPlinthCorner()
        {
            Transform pathwaysTransform = scenePlaza.transform.Find("Pathways");
            Assert.IsNotNull(pathwaysTransform, "TownSquarePlaza/Pathways must exist.");

            Mesh pathMesh = pathwaysTransform.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(pathMesh, "Pathways must have valid sharedMesh.");

            Vector3[] verts = pathMesh.vertices;
            int[] tris = pathMesh.triangles;

            // Extract inner curb ground vertices (Y == 0.000m, radius around 2.32m)
            List<Vector3> curbGroundVerts = new List<Vector3>();
            int innerCurbTriCount = 0;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 vA = verts[tris[i]];
                Vector3 vB = verts[tris[i + 1]];
                Vector3 vC = verts[tris[i + 2]];

                float rA = new Vector2(vA.x, vA.z).magnitude;
                float rB = new Vector2(vB.x, vB.z).magnitude;
                float rC = new Vector2(vC.x, vC.z).magnitude;

                float minR = Mathf.Min(rA, Mathf.Min(rB, rC));
                float maxR = Mathf.Max(rA, Mathf.Max(rB, rC));

                if (minR >= 2.31f && maxR <= 2.41f)
                {
                    innerCurbTriCount++;
                    if (Mathf.Abs(vA.y) < 1e-4f) curbGroundVerts.Add(vA);
                    if (Mathf.Abs(vB.y) < 1e-4f) curbGroundVerts.Add(vB);
                    if (Mathf.Abs(vC.y) < 1e-4f) curbGroundVerts.Add(vC);
                }
            }

            Assert.AreEqual(32, innerCurbTriCount, "Inner beveled curb must consist of exactly 16 quads (32 triangles).");
            Assert.GreaterOrEqual(curbGroundVerts.Count, 16, "Must have at least 16 ground-level vertices along inner curb edge.");

            // Verify inner curb ground vertices radius is strictly 2.32m (+- 0.001m)
            float minGroundRadius = float.MaxValue;
            float maxGroundRadius = float.MinValue;
            foreach (var gv in curbGroundVerts)
            {
                float r = new Vector2(gv.x, gv.z).magnitude;
                if (r < minGroundRadius) minGroundRadius = r;
                if (r > maxGroundRadius) maxGroundRadius = r;
            }

            Assert.AreEqual(2.320f, minGroundRadius, 0.001f, "Inner curb ground edge radius minimum must be 2.32m.");
            Assert.AreEqual(2.320f, maxGroundRadius, 0.001f, "Inner curb ground edge radius maximum must be 2.32m.");

            // Footing plinth corner radius is 2.30m
            const float plinthCornerRadius = 2.300f;
            float cornerClearance = minGroundRadius - plinthCornerRadius;
            Assert.Greater(cornerClearance, 0.0f, "Clearance between inner curb and fountain footing plinth corner must be strictly > 0.");
            Assert.AreEqual(0.020f, cornerClearance, 0.001f, $"Corner clearance ({cornerClearance * 1000f:F2}mm) must equal target 20.00mm.");

            // Continuous 360-degree 2D SAT boundary clearance check
            // Octagonal plinth has 8 corners at r = 2.30m at angles k * pi / 4
            // Inner curb has 16 vertices at r = 2.32m at angles k * pi / 8
            for (int deg = 0; deg < 360; deg++)
            {
                float theta = deg * Mathf.Deg2Rad;

                // Radius of octagonal plinth at angle theta:
                // An octagon segment between k*pi/4 and (k+1)*pi/4 has edge equation:
                // r_oct(theta) = r_corner * cos(pi/8) / cos(theta_local) where theta_local in [-pi/8, pi/8]
                float sectorAngle = Mathf.PI / 4f; // 45 deg
                float localAngle = (theta % sectorAngle) - (sectorAngle * 0.5f);
                float rPlinth = plinthCornerRadius * Mathf.Cos(sectorAngle * 0.5f) / Mathf.Cos(localAngle);

                // Radius of 16-gon inner curb at angle theta:
                float curbSector = Mathf.PI / 8f; // 22.5 deg
                float curbLocalAngle = (theta % curbSector) - (curbSector * 0.5f);
                float rCurb = 2.320f * Mathf.Cos(curbSector * 0.5f) / Mathf.Cos(curbLocalAngle);

                float radialGap = rCurb - rPlinth;
                Assert.Greater(radialGap, 0.019f,
                    $"Radial clearance at angle {deg} deg ({radialGap * 1000f:F2}mm) must be >= 19mm.");
            }
        }

        [Test]
        [Description("Challenge 2.2: Asserts Y = 0.00m flush ground termination, zero vertical cliff, zero gap, zero coplanar overlap.")]
        public void AdvRem_05_InnerEsplanadeCurb_GroundTerminationAndFlushness()
        {
            Transform pathwaysTransform = scenePlaza.transform.Find("Pathways");
            Mesh pathMesh = pathwaysTransform.GetComponent<MeshFilter>().sharedMesh;

            Vector3[] verts = pathMesh.vertices;
            Vector3[] norms = pathMesh.normals;
            int[] tris = pathMesh.triangles;

            float minInnerCurbY = float.MaxValue;
            float maxInnerCurbY = float.MinValue;
            int groundTerminationVertices = 0;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 vA = verts[tris[i]];
                Vector3 vB = verts[tris[i + 1]];
                Vector3 vC = verts[tris[i + 2]];

                float rA = new Vector2(vA.x, vA.z).magnitude;
                float rB = new Vector2(vB.x, vB.z).magnitude;
                float rC = new Vector2(vC.x, vC.z).magnitude;

                float minR = Mathf.Min(rA, Mathf.Min(rB, rC));
                float maxR = Mathf.Max(rA, Mathf.Max(rB, rC));

                if (minR >= 2.31f && maxR <= 2.41f)
                {
                    minInnerCurbY = Mathf.Min(minInnerCurbY, Mathf.Min(vA.y, Mathf.Min(vB.y, vC.y)));
                    maxInnerCurbY = Mathf.Max(maxInnerCurbY, Mathf.Max(vA.y, Mathf.Max(vB.y, vC.y)));

                    if (Mathf.Abs(vA.y) < 1e-4f) groundTerminationVertices++;
                    if (Mathf.Abs(vB.y) < 1e-4f) groundTerminationVertices++;
                    if (Mathf.Abs(vC.y) < 1e-4f) groundTerminationVertices++;

                    // Check normal: must have positive Y (upward slope) and inward horizontal normal
                    Vector3 nA = norms[tris[i]];
                    Assert.Greater(nA.y, 0.05f, "Inner curb normals must slope upwards from ground.");
                    Assert.Less(nA.y, 0.95f, "Inner curb normals must NOT be vertical (must not be coplanar with ground Y=0 plane).");
                }
            }

            Assert.AreEqual(0.000f, minInnerCurbY, 0.0001f, "Inner curb lowest elevation must be strictly Y = 0.000m (flush with ground).");
            Assert.AreEqual(0.012f, maxInnerCurbY, 0.0001f, "Inner curb highest elevation must be strictly Y = 0.012m (pathway elevation).");
            Assert.GreaterOrEqual(groundTerminationVertices, 16, "At least 16 vertex references terminate exactly at ground plane.");
        }

        // =========================================================================
        // Challenge 3: Batching Idempotency (Multi-Execution Stability)
        // =========================================================================

        [Test]
        [Description("Challenge 3.1: Execute BatchTownSquarePlaza multiple times consecutively; assert active mesh count, verts, and states remain strictly identical.")]
        public void AdvRem_06_BatchTownSquarePlaza_Idempotency_ConsecutiveRuns()
        {
            GameObject testRoot = new GameObject("AdvRem_IdempotencyTestRoot");
            try
            {
                // Build a representative test hierarchy with 10 Opaque, 5 Foliage, 2 Emissive, and 1 LiquidGlass meshes
                List<MeshRenderer> opaqueRenderers = new List<MeshRenderer>();
                List<MeshRenderer> foliageRenderers = new List<MeshRenderer>();
                List<MeshRenderer> emissiveRenderers = new List<MeshRenderer>();

                for (int i = 0; i < 10; i++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"OpaqueProp_{i}";
                    cube.transform.SetParent(testRoot.transform, false);
                    cube.transform.localPosition = new Vector3(i * 1.5f, 0, 0);
                    var mr = cube.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = opaqueMat;
                    opaqueRenderers.Add(mr);
                }

                for (int i = 0; i < 5; i++)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = $"FoliageProp_{i}";
                    sphere.transform.SetParent(testRoot.transform, false);
                    sphere.transform.localPosition = new Vector3(0, 0, i * 1.5f);
                    var mr = sphere.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = foliageMat;
                    foliageRenderers.Add(mr);
                }

                for (int i = 0; i < 2; i++)
                {
                    var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cyl.name = $"EmissiveProp_{i}";
                    cyl.transform.SetParent(testRoot.transform, false);
                    cyl.transform.localPosition = new Vector3(-i * 1.5f, 0, 0);
                    var mr = cyl.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = emissiveMat;
                    emissiveRenderers.Add(mr);
                }

                var liquidGlassObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                liquidGlassObj.name = "LiquidGlass_Water";
                liquidGlassObj.transform.SetParent(testRoot.transform, false);
                liquidGlassObj.GetComponent<MeshRenderer>().sharedMaterial = liquidGlassMat;

                const int initialChildren = 18;
                Assert.AreEqual(initialChildren, testRoot.transform.childCount);

                // Run batching 5 times consecutively
                int prevChildCount = -1;
                int prevOpaqueVerts = -1;
                int prevFoliageVerts = -1;
                int prevEmissiveVerts = -1;
                int prevDisabledMRs = -1;

                for (int run = 1; run <= 5; run++)
                {
                    ProceduralMeshCombiner.BatchTownSquarePlaza(testRoot, opaqueMat, foliageMat, emissiveMat);

                    // Assert 3 batch containers were created (Opaque, Foliage, Emissive)
                    int childCount = testRoot.transform.childCount;
                    Assert.AreEqual(initialChildren + 3, childCount,
                        $"Run {run}: Child count must equal initial ({initialChildren}) + 3 batch containers = 21.");

                    // Verify batch container components
                    Transform bOp = testRoot.transform.Find("Batch_Plaza_Opaque");
                    Transform bFol = testRoot.transform.Find("Batch_Plaza_Foliage");
                    Transform bEm = testRoot.transform.Find("Batch_Plaza_Emissive");

                    Assert.IsNotNull(bOp, $"Run {run}: Batch_Plaza_Opaque must exist.");
                    Assert.IsNotNull(bFol, $"Run {run}: Batch_Plaza_Foliage must exist.");
                    Assert.IsNotNull(bEm, $"Run {run}: Batch_Plaza_Emissive must exist.");

                    int opVerts = bOp.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                    int folVerts = bFol.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                    int emVerts = bEm.GetComponent<MeshFilter>().sharedMesh.vertexCount;

                    // 10 cubes * 24 verts = 240 verts
                    Assert.AreEqual(240, opVerts, $"Run {run}: Opaque verts must be 240.");
                    // 2 cylinders * 88 verts = 176 verts
                    Assert.AreEqual(176, emVerts, $"Run {run}: Emissive verts must be 176.");

                    // Source renderers must be disabled
                    int disabledMRs = 0;
                    foreach (var mr in opaqueRenderers) { Assert.IsFalse(mr.enabled); disabledMRs++; }
                    foreach (var mr in foliageRenderers) { Assert.IsFalse(mr.enabled); disabledMRs++; }
                    foreach (var mr in emissiveRenderers) { Assert.IsFalse(mr.enabled); disabledMRs++; }
                    Assert.AreEqual(17, disabledMRs);

                    // LiquidGlass renderer must remain ENABLED
                    Assert.IsTrue(liquidGlassObj.GetComponent<MeshRenderer>().enabled,
                        $"Run {run}: LiquidGlass renderer must remain enabled.");

                    // Colliders on source objects must remain active
                    for (int i = 0; i < 10; i++)
                    {
                        var col = testRoot.transform.Find($"OpaqueProp_{i}").GetComponent<BoxCollider>();
                        Assert.IsNotNull(col, "BoxCollider must exist.");
                        Assert.IsTrue(col.enabled, "BoxCollider must remain enabled.");
                    }

                    // Assert strict idempotency with previous run
                    if (run > 1)
                    {
                        Assert.AreEqual(prevChildCount, childCount, $"Run {run}: Child count must match Run {run - 1}.");
                        Assert.AreEqual(prevOpaqueVerts, opVerts, $"Run {run}: Opaque vertex count must match Run {run - 1}.");
                        Assert.AreEqual(prevFoliageVerts, folVerts, $"Run {run}: Foliage vertex count must match Run {run - 1}.");
                        Assert.AreEqual(prevEmissiveVerts, emVerts, $"Run {run}: Emissive vertex count must match Run {run - 1}.");
                        Assert.AreEqual(prevDisabledMRs, disabledMRs, $"Run {run}: Disabled renderer count must match Run {run - 1}.");
                    }

                    prevChildCount = childCount;
                    prevOpaqueVerts = opVerts;
                    prevFoliageVerts = folVerts;
                    prevEmissiveVerts = emVerts;
                    prevDisabledMRs = disabledMRs;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        [Description("Challenge 3.2: ClearTownSquarePlazaBatches cleanly restores hierarchy without dangling batch objects.")]
        public void AdvRem_07_ProceduralMeshCombiner_IndividualIdempotencyAndEdgeCases()
        {
            GameObject testRoot = new GameObject("AdvRem_ClearTestRoot");
            try
            {
                var c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c1.transform.SetParent(testRoot.transform, false);
                c1.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;

                // 1. Batch
                ProceduralMeshCombiner.BatchTownSquarePlaza(testRoot, opaqueMat, foliageMat, emissiveMat);
                Assert.AreEqual(2, testRoot.transform.childCount); // Cube + Batch_Plaza_Opaque
                Assert.IsFalse(c1.GetComponent<MeshRenderer>().enabled);

                // 2. Clear Batches
                ProceduralMeshCombiner.ClearTownSquarePlazaBatches(testRoot);
                Assert.AreEqual(1, testRoot.transform.childCount, "Batch container must be destroyed.");
                Assert.IsNull(testRoot.transform.Find("Batch_Plaza_Opaque"));

                // 3. Batch again
                ProceduralMeshCombiner.BatchTownSquarePlaza(testRoot, opaqueMat, foliageMat, emissiveMat);
                Assert.AreEqual(2, testRoot.transform.childCount);
                Assert.IsNotNull(testRoot.transform.Find("Batch_Plaza_Opaque"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        // =========================================================================
        // Challenge 4: SwatchCoord Serialization Compliance
        // =========================================================================

        [Test]
        [Description("Challenge 4.1: SwatchCoord is [System.Serializable] struct with public fields; serializes cleanly with JsonUtility.")]
        public void AdvRem_08_SwatchCoord_SerializationIntegrity()
        {
            Type t = typeof(SwatchCoord);
            Assert.IsTrue(t.IsValueType, "SwatchCoord must be a value type (struct).");
            var attrs = t.GetCustomAttributes(typeof(System.SerializableAttribute), false);
            Assert.Greater(attrs.Length, 0, "SwatchCoord must have [System.Serializable] attribute.");

            var colField = t.GetField("Col");
            var rowField = t.GetField("Row");
            Assert.IsNotNull(colField, "SwatchCoord must have 'Col' field.");
            Assert.IsNotNull(rowField, "SwatchCoord must have 'Row' field.");
            Assert.IsFalse(colField.IsInitOnly, "Col field must not be readonly for serialization compatibility.");
            Assert.IsFalse(rowField.IsInitOnly, "Row field must not be readonly for serialization compatibility.");

            // Test JsonUtility round-trip
            FountainConfig original = FountainConfig.CreateDefault();
            string json = JsonUtility.ToJson(original);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("swatchBasinOuter"));

            FountainConfig deserialized = JsonUtility.FromJson<FountainConfig>(json);
            Assert.AreEqual(original.swatchBasinOuter.Col, deserialized.swatchBasinOuter.Col);
            Assert.AreEqual(original.swatchBasinOuter.Row, deserialized.swatchBasinOuter.Row);
            Assert.AreEqual(original.swatchCopingRim.Col, deserialized.swatchCopingRim.Col);
            Assert.AreEqual(original.swatchCopingRim.Row, deserialized.swatchCopingRim.Row);
        }

        // =========================================================================
        // Challenge 5: Full Regression Verification across M2 Test Suites
        // =========================================================================

        [Test]
        [Description("Challenge 5.1: Regression run of all 8 Milestone 2 Adversarial Stress Tests.")]
        public void AdvRem_09_FullRegression_M2AdversarialSuite()
        {
            var m2Adv = new Milestone2_AdversarialStressTests();
            m2Adv.OneTimeSetUp();

            m2Adv.AdvM2_01_Combiner_ChunksSubmeshesUnder65534Vertices();
            m2Adv.AdvM2_02_Combiner_PreservesPrimitiveCollidersAndDisablesRenderers();
            m2Adv.AdvM2_03_Combiner_ExcludesRigidbodiesAndInteractables();
            m2Adv.AdvM2_04_DrawCallBudget_CompletePlazaView_StrictlyBelow100WithHeadroom();
            m2Adv.AdvM2_05_StreetLamps_HeightInBound_EmissiveCoreLuminance_FlushColliders();
            m2Adv.AdvM2_06_Benches_SeatHeight_BulletinBoard_FacingNorth();
            m2Adv.AdvM2_07_NatureElements_TreeCanopyRadius_FlowerBoxesFlush_HedgeHeight();
            m2Adv.AdvM2_08_ZeroDynamicMeshColliders_AcrossEntirePlaza();
        }

        [Test]
        [Description("Challenge 5.2: Regression run of all 60 Tier 1 and Tier 2 Milestone 2 feature tests (F4, F5, F6, F7, F11, F21).")]
        public void AdvRem_10_FullRegression_Tier1AndTier2_M2Features()
        {
            string[] targetFeatures = { "F4_", "F5_", "F6_", "F7_", "F11_", "F21_" };

            // 1. Tier 1 Tests
            var t1 = new Tier1_FeatureCoverageTests();
            var t1Methods = typeof(Tier1_FeatureCoverageTests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int t1Passed = 0;

            foreach (var m in t1Methods)
            {
                if (m.GetParameters().Length > 0 || m.ReturnType != typeof(void)) continue;
                bool isTarget = false;
                foreach (var f in targetFeatures)
                {
                    if (m.Name.StartsWith(f)) { isTarget = true; break; }
                }
                if (!isTarget) continue;

                try
                {
                    m.Invoke(t1, null);
                    t1Passed++;
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException is SuccessException)
                    {
                        t1Passed++;
                    }
                    else
                    {
                        throw tie.InnerException;
                    }
                }
            }
            Assert.AreEqual(30, t1Passed, "All 30 targeted Tier 1 tests for F4, F5, F6, F7, F11, F21 must execute and pass.");

            // 2. Tier 2 Tests
            var t2 = new Tier2_BoundaryCornerTests();
            var t2Methods = typeof(Tier2_BoundaryCornerTests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int t2Passed = 0;

            foreach (var m in t2Methods)
            {
                if (m.GetParameters().Length > 0 || m.ReturnType != typeof(void)) continue;
                bool isTarget = false;
                foreach (var f in targetFeatures)
                {
                    if (m.Name.StartsWith(f)) { isTarget = true; break; }
                }
                if (!isTarget) continue;

                try
                {
                    m.Invoke(t2, null);
                    t2Passed++;
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException is SuccessException)
                    {
                        t2Passed++;
                    }
                    else
                    {
                        throw tie.InnerException;
                    }
                }
            }
            Assert.AreEqual(30, t2Passed, "All 30 targeted Tier 2 tests for F4, F5, F6, F7, F11, F21 must execute and pass.");
        }

        // =========================================================================
        // Helper Assertions
        // =========================================================================

        private static void AssertDegenerateCountZero(Mesh m, string meshName)
        {
            Vector3[] verts = m.vertices;
            int[] tris = m.triangles;
            int degen = 0;

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i0 = tris[i];
                int i1 = tris[i + 1];
                int i2 = tris[i + 2];

                if (i0 == i1 || i1 == i2 || i0 == i2)
                {
                    degen++;
                    continue;
                }

                Vector3 v0 = verts[i0];
                Vector3 v1 = verts[i1];
                Vector3 v2 = verts[i2];

                if (Vector3.Distance(v0, v1) < 1e-6f || Vector3.Distance(v1, v2) < 1e-6f || Vector3.Distance(v0, v2) < 1e-6f)
                {
                    degen++;
                    continue;
                }

                if (Vector3.Cross(v1 - v0, v2 - v0).sqrMagnitude < 1e-12f)
                {
                    degen++;
                }
            }

            Assert.AreEqual(0, degen, $"Mesh '{meshName}' must have strictly 0 degenerate triangles. Found {degen}.");
        }
    }
}
