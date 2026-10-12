// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Milestone1_AdversarialStressTests.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Empirical Adversarial Stress Test Suite for Milestone 1 (Procedural Geometry & Color Atlas UV Mappings).
//   Constructed by Challenger m1_challenger_1 to stress-test extreme dimensions, degenerate shapes,
//   zero/excessive bevels, odd segment counts, bounds invariants, and strict cell-center zero-bleed UVs.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AnimalCrossing.TechnicalArt;

namespace AnimalCrossing.Tests
{
    [TestFixture]
    public class Milestone1_AdversarialStressTests
    {
        private const float GutterMarginTolerance = 1.5f / 512f;

        private void AssertMeshValid(string testName, Mesh mesh, bool checkNormalsNonZero = true)
        {
            Assert.IsNotNull(mesh, $"{testName}: Mesh is null.");
            int vCount = mesh.vertexCount;
            int tCount = mesh.triangles.Length;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            var triangles = mesh.triangles;

            Assert.Greater(vCount, 0, $"{testName}: Mesh has zero vertices.");
            Assert.Greater(tCount, 0, $"{testName}: Mesh has zero triangles.");
            Assert.AreEqual(0, tCount % 3, $"{testName}: Triangle indices not divisible by 3.");

            // Verify triangle indices
            for (int i = 0; i < tCount; i++)
            {
                int idx = triangles[i];
                Assert.IsTrue(idx >= 0 && idx < vCount, $"{testName}: Triangle index {idx} out of range [0, {vCount}).");
            }

            // Verify vertices: non-NaN, non-Inf
            for (int i = 0; i < vCount; i++)
            {
                Vector3 v = vertices[i];
                Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z), $"{testName}: Vertex {i} contains NaN.");
                Assert.IsFalse(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z), $"{testName}: Vertex {i} contains Infinity.");
            }

            // Verify normals: non-NaN, non-Inf, and non-zero
            if (normals != null && normals.Length > 0)
            {
                Assert.AreEqual(vCount, normals.Length, $"{testName}: Normals count mismatch.");
                for (int i = 0; i < vCount; i++)
                {
                    Vector3 n = normals[i];
                    Assert.IsFalse(float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z), $"{testName}: Normal {i} contains NaN.");
                    Assert.IsFalse(float.IsInfinity(n.x) || float.IsInfinity(n.y) || float.IsInfinity(n.z), $"{testName}: Normal {i} contains Infinity.");
                    if (checkNormalsNonZero)
                    {
                        Assert.Greater(n.sqrMagnitude, 0.0001f, $"{testName}: Normal {i} is near zero.");
                    }
                }
            }

            // Verify UVs: strictly within [0, 1] and exactly on cell centers
            if (uvs != null && uvs.Length > 0)
            {
                Assert.AreEqual(vCount, uvs.Length, $"{testName}: UV count mismatch.");
                for (int i = 0; i < vCount; i++)
                {
                    Vector2 uv = uvs[i];
                    Assert.IsFalse(float.IsNaN(uv.x) || float.IsNaN(uv.y), $"{testName}: UV {i} contains NaN.");
                    Assert.IsTrue(uv.x >= 0f && uv.x <= 1f, $"{testName}: UV.x {uv.x} out of [0, 1].");
                    Assert.IsTrue(uv.y >= 0f && uv.y <= 1f, $"{testName}: UV.y {uv.y} out of [0, 1].");

                    float colF = uv.x * 16f - 0.5f;
                    float rowF = uv.y * 16f - 0.5f;
                    int col = Mathf.RoundToInt(colF);
                    int row = Mathf.RoundToInt(rowF);

                    Assert.AreEqual(col, colF, 0.0001f, $"{testName}: UV.x not at cell center: {uv.x}");
                    Assert.AreEqual(row, rowF, 0.0001f, $"{testName}: UV.y not at cell center: {uv.y}");
                    Assert.IsTrue(col >= 0 && col <= 15, $"{testName}: Swatch col {col} out of range [0, 15].");
                    Assert.IsTrue(row >= 0 && row <= 15, $"{testName}: Swatch row {row} out of range [0, 15].");

                    // Gutter margin test
                    float cellMinX = col / 16f;
                    float cellMaxX = (col + 1) / 16f;
                    float cellMinY = row / 16f;
                    float cellMaxY = (row + 1) / 16f;
                    float distToEdgeX = Mathf.Min(uv.x - cellMinX, cellMaxX - uv.x);
                    float distToEdgeY = Mathf.Min(uv.y - cellMinY, cellMaxY - uv.y);
                    float minDist = Mathf.Min(distToEdgeX, distToEdgeY);
                    Assert.GreaterOrEqual(minDist, GutterMarginTolerance, $"{testName}: UV violates gutter margin: {minDist} < {GutterMarginTolerance}");
                }
            }

            // Verify bounds non-NaN
            var bounds = mesh.bounds;
            Assert.IsFalse(float.IsNaN(bounds.size.x) || float.IsNaN(bounds.size.y) || float.IsNaN(bounds.size.z), $"{testName}: Bounds size is NaN.");
        }

        private float ComputeSignedVolume(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            double vol = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 p1 = vertices[triangles[i]];
                Vector3 p2 = vertices[triangles[i + 1]];
                Vector3 p3 = vertices[triangles[i + 2]];
                vol += Vector3.Dot(p1, Vector3.Cross(p2, p3));
            }
            return (float)(vol / 6.0);
        }

        #region 1. ProceduralMeshBuilder Stress Tests

        [Test]
        [Description("Stress-test BeveledCube with extreme dimensions: zero, negative, tiny, huge, and extreme aspect ratio.")]
        public void PMB_01_BeveledCube_ExtremeDimensions()
        {
            AssertMeshValid("BeveledCube_Standard", ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, 0.1f, 1, 0, 0));
            AssertMeshValid("BeveledCube_ZeroSize", ProceduralMeshBuilder.CreateBeveledCube(Vector3.zero, 0.1f, 1, 0, 0));
            AssertMeshValid("BeveledCube_NegativeSize", ProceduralMeshBuilder.CreateBeveledCube(new Vector3(-5f, -2f, -1f), 0.1f, 1, 0, 0));
            AssertMeshValid("BeveledCube_TinySize", ProceduralMeshBuilder.CreateBeveledCube(new Vector3(1e-5f, 1e-5f, 1e-5f), 0.00001f, 1, 0, 0));
            AssertMeshValid("BeveledCube_HugeSize", ProceduralMeshBuilder.CreateBeveledCube(new Vector3(1000f, 500f, 2000f), 10f, 1, 0, 0));
            AssertMeshValid("BeveledCube_ExtremeAspect", ProceduralMeshBuilder.CreateBeveledCube(new Vector3(100f, 0.01f, 0.01f), 0.002f, 1, 0, 0));
        }

        [Test]
        [Description("Stress-test BeveledCube with zero, negative, and microscopic bevel radii.")]
        public void PMB_02_BeveledCube_ZeroAndNegativeBevel()
        {
            var meshZero = ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, 0.0f, 0, 0, 0);
            AssertMeshValid("BeveledCube_ZeroBevel", meshZero);
            Assert.AreEqual(36, meshZero.vertexCount, "Zero bevel must generate sharp cube with 36 vertices (6 quads).");

            var meshNeg = ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, -0.5f, 0, 0, 0);
            AssertMeshValid("BeveledCube_NegativeBevel", meshNeg);
            Assert.AreEqual(36, meshNeg.vertexCount, "Negative bevel must fallback to sharp cube with 36 vertices.");

            var meshTiny = ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, 0.00005f, 0, 0, 0);
            AssertMeshValid("BeveledCube_TinyBevel", meshTiny);
            Assert.AreEqual(36, meshTiny.vertexCount, "Bevel below 0.0001f must fallback to sharp cube.");
        }

        [Test]
        [Description("Stress-test BeveledCube with maximum and excessive bevel radii.")]
        public void PMB_03_BeveledCube_MaxAndExcessiveBevel()
        {
            var meshHalf = ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, 0.5f, 1, 0, 0);
            AssertMeshValid("BeveledCube_MaxBevel_Half", meshHalf);

            var mesh2x = ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, 1.0f, 1, 0, 0);
            AssertMeshValid("BeveledCube_ExcessiveBevel_2x", mesh2x);

            var mesh100x = ProceduralMeshBuilder.CreateBeveledCube(Vector3.one, 100.0f, 1, 0, 0);
            AssertMeshValid("BeveledCube_ExcessiveBevel_100x", mesh100x);
        }

        [Test]
        [Description("Stress-test BeveledCylinder with odd segment counts: 3, 5, 7, 9, 11, 13, 23, 31, 47.")]
        public void PMB_04_BeveledCylinder_OddSegmentCounts()
        {
            int[] oddSegments = { 3, 5, 7, 9, 11, 13, 17, 23, 31, 47 };
            foreach (int oddSeg in oddSegments)
            {
                var mesh = ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 0.05f, oddSeg, 2, 0, 0);
                AssertMeshValid($"Cylinder_OddSeg_{oddSeg}", mesh);
            }
        }

        [Test]
        [Description("Stress-test BeveledCylinder with segment counts below minimum (clamps to 3).")]
        public void PMB_05_BeveledCylinder_UnderMinimumSegments()
        {
            var mesh1 = ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 0.05f, 1, 1, 0, 0);
            AssertMeshValid("Cylinder_Seg_1", mesh1);

            var meshNeg = ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 0.05f, -5, 1, 0, 0);
            AssertMeshValid("Cylinder_Seg_Negative", meshNeg);
        }

        [Test]
        [Description("Stress-test BeveledCylinder with fillet segments from 1 to 8, zero, and excessive.")]
        public void PMB_06_BeveledCylinder_FilletSegmentsRange()
        {
            for (int fSeg = 1; fSeg <= 8; fSeg++)
            {
                var mesh = ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 0.05f, 12, fSeg, 0, 0);
                AssertMeshValid($"Cylinder_FilletSeg_{fSeg}", mesh);
            }

            var mesh0 = ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 0.05f, 12, 0, 0, 0);
            AssertMeshValid("Cylinder_FilletSeg_0", mesh0);

            var mesh20 = ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 0.05f, 12, 20, 0, 0);
            AssertMeshValid("Cylinder_FilletSeg_20", mesh20);
        }

        [Test]
        [Description("Stress-test BeveledCylinder with extreme dimensions and excessive fillet radius.")]
        public void PMB_07_BeveledCylinder_ExtremeDimensions()
        {
            AssertMeshValid("Cylinder_ZeroDims", ProceduralMeshBuilder.CreateBeveledCylinder(0f, 0f, 0f, 8, 2, 0, 0));
            AssertMeshValid("Cylinder_NegativeDims", ProceduralMeshBuilder.CreateBeveledCylinder(-1f, -2f, -0.5f, 8, 2, 0, 0));
            AssertMeshValid("Cylinder_HugeDims", ProceduralMeshBuilder.CreateBeveledCylinder(100f, 500f, 10f, 16, 2, 0, 0));
            AssertMeshValid("Cylinder_ExcessiveFillet", ProceduralMeshBuilder.CreateBeveledCylinder(0.5f, 1f, 50f, 16, 2, 0, 0));
        }

        [Test]
        [Description("Stress-test StylizedSphere with zero/negative radius, odd segments, and below-minimum segments.")]
        public void PMB_08_StylizedSphere_Stress()
        {
            AssertMeshValid("Sphere_ZeroRadius", ProceduralMeshBuilder.CreateStylizedSphere(0f, 8, 8, 0, 0));
            AssertMeshValid("Sphere_NegativeRadius", ProceduralMeshBuilder.CreateStylizedSphere(-2f, 8, 8, 0, 0));
            AssertMeshValid("Sphere_UnderMinSegments", ProceduralMeshBuilder.CreateStylizedSphere(0.5f, 1, 1, 0, 0));
            AssertMeshValid("Sphere_OddSegments", ProceduralMeshBuilder.CreateStylizedSphere(0.5f, 7, 9, 0, 0));
        }

        [Test]
        [Description("Stress-test BeveledTorus with zero major radius, excessive minor radius, odd segments, and below-minimum segments.")]
        public void PMB_09_BeveledTorus_Stress()
        {
            AssertMeshValid("Torus_ZeroMajor", ProceduralMeshBuilder.CreateBeveledTorus(0f, 0.1f, 12, 6, 0, 0));
            AssertMeshValid("Torus_ExcessiveMinor", ProceduralMeshBuilder.CreateBeveledTorus(1f, 2f, 12, 6, 0, 0));
            AssertMeshValid("Torus_UnderMinSegments", ProceduralMeshBuilder.CreateBeveledTorus(1f, 0.2f, 2, 1, 0, 0));
            AssertMeshValid("Torus_OddSegments", ProceduralMeshBuilder.CreateBeveledTorus(1f, 0.2f, 7, 5, 0, 0));
        }

        #endregion

        #region 2. Strict UV Mapping Stress Tests

        [Test]
        [Description("Verify all 256 swatches in the 16x16 grid evaluate exactly to cell centers with zero bleed.")]
        public void UV_01_All256Swatches_ExactCellCenters()
        {
            for (int col = 0; col < 16; col++)
            {
                for (int row = 0; row < 16; row++)
                {
                    Vector2 uv = ProceduralMeshBuilder.GetSwatchUV(col, row);
                    float expectedX = (col + 0.5f) / 16f;
                    float expectedY = (row + 0.5f) / 16f;

                    Assert.AreEqual(expectedX, uv.x, 1e-6f, $"Swatch ({col}, {row}) UV.x mismatch.");
                    Assert.AreEqual(expectedY, uv.y, 1e-6f, $"Swatch ({col}, {row}) UV.y mismatch.");

                    float minEdgeDist = Mathf.Min(uv.x - col / 16f, (col + 1) / 16f - uv.x);
                    Assert.GreaterOrEqual(minEdgeDist, GutterMarginTolerance, $"Swatch ({col}, {row}) gutter margin violation.");
                }
            }
        }

        [Test]
        [Description("Verify out-of-range swatch coordinates clamp safely to [0, 15].")]
        public void UV_02_OutOfRangeSwatches_ClampSafely()
        {
            Vector2 uvNeg = ProceduralMeshBuilder.GetSwatchUV(-10, -50);
            Vector2 uvPos = ProceduralMeshBuilder.GetSwatchUV(100, 500);
            Vector2 uv00 = ProceduralMeshBuilder.GetSwatchUV(0, 0);
            Vector2 uv1515 = ProceduralMeshBuilder.GetSwatchUV(15, 15);

            Assert.AreEqual(uv00, uvNeg, "Negative swatch coordinates must clamp to (0, 0).");
            Assert.AreEqual(uv1515, uvPos, "Excessive swatch coordinates must clamp to (15, 15).");
        }

        #endregion

        #region 3. ProceduralCanopyBuilder Stress Tests

        [Test]
        [Description("Verify all standard canopy presets (Produce, Cafe, Gardening) produce valid geometry.")]
        public void PCB_01_StandardPresets_Valid()
        {
            AssertMeshValid("ProduceStallCanopy", ProceduralCanopyBuilder.CreateProduceStallCanopy());
            AssertMeshValid("CafeStallCanopy", ProceduralCanopyBuilder.CreateCafeStallCanopy());
            AssertMeshValid("GardeningStallCanopy", ProceduralCanopyBuilder.CreateGardeningStallCanopy());
        }

        [Test]
        [Description("Verify CreateCanopyMesh with zero sag produces bounds.size.y == 0.")]
        public void PCB_02_CanopyMesh_ZeroSag_ProducesFlatAwning()
        {
            var mesh = ProceduralCanopyBuilder.CreateCanopyMesh(2.2f, 1.8f, 0.0f, 8, 12, 0, 4);
            AssertMeshValid("CanopyMesh_ZeroSag", mesh);
            Assert.AreEqual(0.0f, mesh.bounds.size.y, 0.0001f, "Zero sag must produce deck height bounds size Y = 0.");
        }

        [Test]
        [Description("Verify CreateCanopyMesh with large sag depth matches bounds size Y within tolerance.")]
        public void PCB_03_CanopyMesh_LargeSag_MatchesBounds()
        {
            float[] sags = { 0.5f, 1.0f, 2.5f, 5.0f };
            foreach (float sag in sags)
            {
                var mesh = ProceduralCanopyBuilder.CreateCanopyMesh(2.0f, 2.0f, sag, 8, 16, 0, 0);
                AssertMeshValid($"CanopyMesh_Sag_{sag}", mesh);
                Assert.AreEqual(sag, mesh.bounds.size.y, 0.02f, $"Canopy mesh sag {sag} must match bounds size Y.");
            }
        }

        [Test]
        [Description("Verify CreateScallopedValance with zero scallops clamps to 1 scallop.")]
        public void PCB_04_ScallopedValance_ZeroScallops_ClampsTo1()
        {
            var mesh0 = ProceduralCanopyBuilder.CreateScallopedValance(2.0f, 0.2f, 0, 0.05f, 0, 0);
            AssertMeshValid("Valance_0Scallops", mesh0);

            var mesh1 = ProceduralCanopyBuilder.CreateScallopedValance(2.0f, 0.2f, 1, 0.05f, 0, 0);
            Assert.AreEqual(mesh1.vertexCount, mesh0.vertexCount, "Zero scallops must clamp to 1 scallop vertex count.");
        }

        [Test]
        [Description("Verify CreateScallopedValance with 10 scallops produces exactly 480 vertices.")]
        public void PCB_05_ScallopedValance_10Scallops_VertexCount()
        {
            var mesh10 = ProceduralCanopyBuilder.CreateScallopedValance(2.0f, 0.2f, 10, 0.05f, 0, 0);
            AssertMeshValid("Valance_10Scallops", mesh10);
            // 10 scallops * 8 steps * 6 vertices per quad = 480 vertices
            Assert.AreEqual(480, mesh10.vertexCount, "10 scallops with amplitude > 0 must generate exactly 480 vertices.");
        }

        [Test]
        [Description("Verify CreateScallopedValance with zero amplitude produces straight edge (1 step per lobe = 60 vertices).")]
        public void PCB_06_ScallopedValance_ZeroAmplitude_StraightEdge()
        {
            var mesh = ProceduralCanopyBuilder.CreateScallopedValance(2.0f, 0.2f, 10, 0.0f, 0, 0);
            AssertMeshValid("Valance_10Scallops_ZeroAmp", mesh);
            // 10 lobes * 1 step * 6 vertices = 60 vertices
            Assert.AreEqual(60, mesh.vertexCount, "10 scallops with zero amplitude must generate exactly 60 vertices.");
        }

        [Test]
        [Description("Verify CreateCanopy with full configuration combinations: zero/large sag, 0/10 scallops, odd stripes.")]
        public void PCB_07_Canopy_ConfigPermutations()
        {
            var cfg = CanopyConfig.CreateDefault();

            // Zero sag
            cfg.sagDepth = 0f;
            AssertMeshValid("Canopy_ZeroSag", ProceduralCanopyBuilder.CreateCanopy(cfg));

            // Large sag
            cfg.sagDepth = 3.0f;
            AssertMeshValid("Canopy_LargeSag_3m", ProceduralCanopyBuilder.CreateCanopy(cfg));

            // Scallops per stripe = 0 (clamps to 1)
            cfg.scallopsPerStripe = 0;
            AssertMeshValid("Canopy_0ScallopsPerStripe", ProceduralCanopyBuilder.CreateCanopy(cfg));

            // Scallops per stripe = 10
            cfg.scallopsPerStripe = 10;
            AssertMeshValid("Canopy_10ScallopsPerStripe", ProceduralCanopyBuilder.CreateCanopy(cfg));

            // Scallop amplitude = 0
            cfg.scallopAmplitude = 0f;
            AssertMeshValid("Canopy_0Amp", ProceduralCanopyBuilder.CreateCanopy(cfg));

            // Odd stripes
            int[] oddStripes = { 3, 5, 7, 9, 11 };
            foreach (int s in oddStripes)
            {
                cfg.stripeCount = s;
                AssertMeshValid($"Canopy_OddStripes_{s}", ProceduralCanopyBuilder.CreateCanopy(cfg));
            }
        }

        #endregion

        #region 4. Signed Volume Oracle Tests

        [Test]
        [Description("Oracle: Gauss divergence theorem signed volume must be positive on all closed procedural meshes.")]
        public void Oracle_ClosedMeshes_SignedVolumePositive()
        {
            var sharp = ProceduralMeshBuilder.CreateSharpCube(2f, 3f, 4f, 0, 0);
            float volSharp = ComputeSignedVolume(sharp);
            Assert.AreEqual(24.0f, volSharp, 0.01f, "SharpCube volume must equal w*h*d = 24.0.");

            var chamf = ProceduralMeshBuilder.CreateChamferedCube(2f, 2f, 2f, 0.1f, 0, 0);
            float volChamf = ComputeSignedVolume(chamf);
            Assert.Greater(volChamf, 7.5f, "ChamferedCube volume must be > 7.5.");
            Assert.Less(volChamf, 8.0f, "ChamferedCube volume must be < 8.0.");

            var cyl = ProceduralMeshBuilder.CreateBeveledCylinder(1f, 2f, 0.05f, 32, 4, 0, 0);
            float volCyl = ComputeSignedVolume(cyl);
            Assert.Greater(volCyl, 5.5f, "BeveledCylinder volume must be positive and close to pi*r^2*h.");

            var sphere = ProceduralMeshBuilder.CreateStylizedSphere(1f, 24, 24, 0, 0);
            float volSphere = ComputeSignedVolume(sphere);
            Assert.Greater(volSphere, 3.8f, "StylizedSphere volume must be positive and close to 4/3*pi*r^3.");

            var torus = ProceduralMeshBuilder.CreateBeveledTorus(2f, 0.5f, 32, 16, 0, 0);
            float volTorus = ComputeSignedVolume(torus);
            Assert.Greater(volTorus, 8.0f, "BeveledTorus volume must be positive and close to 2*pi^2*R*r^2.");
        }

        #endregion
    }
}
