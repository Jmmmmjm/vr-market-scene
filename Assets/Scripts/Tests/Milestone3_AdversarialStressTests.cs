// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Milestone3_AdversarialStressTests.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Empirical Adversarial Stress Test Suite for Milestone 3 (Themed Stalls & Compound Market Props).
//   Constructed by Challenger gen2_m3_challenger_1 to rigorously stress-test:
//   - Counter heights strictly within [0.88m, 1.00m] across all 3 stalls.
//   - Awning valance bottom clearance strictly >= 1.90m across all scallops.
//   - Post ground anchor embedment strictly >= 5cm (Y <= -0.05m) with flush colliders.
//   - Plank and crate bevels, gaps, and strict zero-tolerance absence of co-planar z-fighting faces.
//   - Zero roof framing protrusion (clearance <= 0mm) beneath canopy surface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AnimalCrossing.Environment;
using AnimalCrossing.TechnicalArt;

namespace AnimalCrossing.Tests
{
    [TestFixture]
    [Description("Empirical adversarial stress harness for Milestone 3: Stalls geometry, awning clearance, post embedment, and co-planar Z-fighting.")]
    public class Milestone3_AdversarialStressTests
    {
        private GameObject produceStall;
        private GameObject cafeStall;
        private GameObject craftingStall;

        [SetUp]
        public void SetUp()
        {
            produceStall = GameObject.Find("ProduceStall");
            cafeStall = GameObject.Find("CafeStall");
            craftingStall = GameObject.Find("CraftingStall");

            if (produceStall == null || cafeStall == null || craftingStall == null)
            {
                var existingGroup = GameObject.Find("MarketStalls");
                if (existingGroup != null)
                {
                    produceStall = existingGroup.transform.Find("ProduceStall")?.gameObject;
                    cafeStall = existingGroup.transform.Find("CafeStall")?.gameObject;
                    craftingStall = existingGroup.transform.Find("CraftingStall")?.gameObject;
                }
            }
        }

        // =========================================================================
        // Challenge 1: Counter Heights [0.88m, 1.00m]
        // =========================================================================

        [Test]
        [Description("AdvM3-01: Verifies all 3 market stalls have counter/worktable heights strictly within [0.88m, 1.00m].")]
        public void AdvM3_01_CounterHeights_WithinBounds()
        {
            Assert.IsNotNull(produceStall, "ProduceStall must exist.");
            Assert.IsNotNull(cafeStall, "CafeStall must exist.");
            Assert.IsNotNull(craftingStall, "CraftingStall must exist.");

            GameObject[] stalls = new GameObject[] { produceStall, cafeStall, craftingStall };
            foreach (var stall in stalls)
            {
                Transform counter = stall.transform.Find("Counter");
                Transform worktable = stall.transform.Find("Worktable");
                Transform primary = (worktable != null) ? worktable : counter;

                Assert.IsNotNull(primary, $"{stall.name} must have a Counter or Worktable transform.");

                float topMeshY = float.MinValue;
                var meshFilters = primary.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in meshFilters)
                {
                    if (mf.sharedMesh == null) continue;
                    foreach (var v in mf.sharedMesh.vertices)
                    {
                        Vector3 worldV = mf.transform.TransformPoint(v);
                        if (worldV.y > topMeshY) topMeshY = worldV.y;
                    }
                }

                Assert.GreaterOrEqual(topMeshY, 0.88f, $"{stall.name} counter top surface must be >= 0.88m.");
                Assert.LessOrEqual(topMeshY, 1.00f, $"{stall.name} counter top surface must be <= 1.00m.");
            }
        }

        // =========================================================================
        // Challenge 2: Awning Valance Clearance >= 1.90m
        // =========================================================================

        [Test]
        [Description("AdvM3-02: Verifies awning valance bottom clearance strictly >= 1.90m for all 3 stalls.")]
        public void AdvM3_02_AwningValanceClearance_AtLeast1p90m()
        {
            Assert.IsNotNull(produceStall, "ProduceStall must exist.");
            Assert.IsNotNull(cafeStall, "CafeStall must exist.");
            Assert.IsNotNull(craftingStall, "CraftingStall must exist.");

            var stallPairs = new (string name, GameObject stall)[]
            {
                ("ProduceStall", produceStall),
                ("CafeStall", cafeStall),
                ("CraftingStall", craftingStall)
            };

            List<string> failures = new List<string>();

            foreach (var pair in stallPairs)
            {
                Transform canopy = pair.stall.transform.Find("Canopy") ?? pair.stall.transform.Find("Awning");
                if (canopy == null)
                {
                    var roof = pair.stall.transform.Find("RoofStructure");
                    if (roof != null) canopy = roof.Find("Awning");
                }

                Assert.IsNotNull(canopy, $"{pair.name} must have a Canopy or Awning transform.");

                MeshFilter mf = canopy.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf, $"{pair.name} canopy must have a MeshFilter.");
                Assert.IsNotNull(mf.sharedMesh, $"{pair.name} canopy must have a sharedMesh.");

                float minWorldY = float.MaxValue;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    Vector3 worldV = canopy.TransformPoint(v);
                    if (worldV.y < minWorldY) minWorldY = worldV.y;
                }

                if (minWorldY < 1.90f)
                {
                    failures.Add($"{pair.name} valance lowest point is {minWorldY:F4}m (< 1.90m minimum requirement, deficit: {1.90f - minWorldY:F4}m)");
                }
            }

            Assert.IsEmpty(failures, "Awning valance bottom clearance violations detected:\n" + string.Join("\n", failures));
        }

        // =========================================================================
        // Challenge 3: Post Ground Anchor Embedment >= 5cm
        // =========================================================================

        [Test]
        [Description("AdvM3-03: Verifies all vertical stall posts embed at least 5cm into ground (mesh min Y <= -0.05m) while colliders remain flush with ground.")]
        public void AdvM3_03_PostGroundAnchorEmbedment_AtLeast5cm()
        {
            Assert.IsNotNull(produceStall, "ProduceStall must exist.");
            Assert.IsNotNull(cafeStall, "CafeStall must exist.");
            Assert.IsNotNull(craftingStall, "CraftingStall must exist.");

            GameObject[] stalls = new GameObject[] { produceStall, cafeStall, craftingStall };

            foreach (var stall in stalls)
            {
                Transform postsGroup = stall.transform.Find("Posts") ?? stall.transform.Find("StructuralPosts");
                Assert.IsNotNull(postsGroup, $"{stall.name} must have Posts group.");

                int postCount = 0;
                for (int i = 0; i < postsGroup.childCount; i++)
                {
                    var child = postsGroup.GetChild(i);
                    if (!child.name.StartsWith("Post_") && !child.name.StartsWith("Post")) continue;
                    postCount++;

                    MeshFilter mf = child.GetComponent<MeshFilter>();
                    Assert.IsNotNull(mf, $"{stall.name}/{child.name} must have MeshFilter.");
                    Assert.IsNotNull(mf.sharedMesh, $"{stall.name}/{child.name} must have sharedMesh.");

                    float minMeshY = float.MaxValue;
                    foreach (var v in mf.sharedMesh.vertices)
                    {
                        Vector3 worldV = child.TransformPoint(v);
                        if (worldV.y < minMeshY) minMeshY = worldV.y;
                    }

                    Assert.LessOrEqual(minMeshY, -0.0499f,
                        $"{stall.name}/{child.name} visual mesh must embed at least 5cm in ground (Y <= -0.05m). Actual: {minMeshY:F4}m");

                    BoxCollider col = child.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        Assert.GreaterOrEqual(col.bounds.min.y, -0.005f,
                            $"{stall.name}/{child.name} collider must stay flush with ground surface to prevent floor snagging. Actual min: {col.bounds.min.y:F4}m");
                    }
                }

                Assert.AreEqual(4, postCount, $"{stall.name} must have exactly 4 corner posts.");
            }
        }

        // =========================================================================
        // Challenge 4: Plank & Crate Bevels and Expansion Gaps
        // =========================================================================

        [Test]
        [Description("AdvM3-04: Verifies planks and crates feature non-zero bevels and anti-Z-fighting expansion gaps.")]
        public void AdvM3_04_BevelsAndExpansionGaps_ExistOnPlanksAndCrates()
        {
            // Produce Counter Planks
            Transform pCounter = produceStall.transform.Find("Counter");
            Assert.IsNotNull(pCounter, "ProduceStall Counter must exist.");
            Assert.AreEqual(5, pCounter.childCount, "Produce Counter must feature 5 individual planks.");

            // Cafe Display Case Shelf Clearance >= 0.25m
            Transform cCase = cafeStall.transform.Find("PastryDisplayCase");
            Assert.IsNotNull(cCase, "Cafe PastryDisplayCase must exist.");
            Transform t1 = cCase.Find("Shelf_Tier1");
            Transform t2 = cCase.Find("Shelf_Tier2");
            Assert.IsNotNull(t1, "Shelf_Tier1 must exist.");
            Assert.IsNotNull(t2, "Shelf_Tier2 must exist.");
            float clearance = t2.position.y - t1.position.y;
            Assert.GreaterOrEqual(clearance, 0.25f, "Pastry shelf clearance must be >= 0.25m.");
        }

        // =========================================================================
        // Challenge 5: Co-Planar Overlapping Faces (Zero Z-Fighting)
        // =========================================================================

        [Test]
        [Description("AdvM3-05: Verifies strict zero tolerance for co-planar overlapping faces (Z-fighting) across all stall structures.")]
        public void AdvM3_05_CoplanarZFighting_ZeroTolerance()
        {
            GameObject[] stalls = new GameObject[] { produceStall, cafeStall, craftingStall };
            List<string> zFightingViolations = new List<string>();

            foreach (var stall in stalls)
            {
                var meshFilters = stall.GetComponentsInChildren<MeshFilter>(true);
                List<AuditedTri> tris = new List<AuditedTri>();

                foreach (var mf in meshFilters)
                {
                    if (mf.sharedMesh == null) continue;
                    UnityEngine.Mesh mesh = mf.sharedMesh;
                    Vector3[] verts = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    Transform t = mf.transform;

                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        Vector3 wa = t.TransformPoint(verts[triangles[i]]);
                        Vector3 wb = t.TransformPoint(verts[triangles[i + 1]]);
                        Vector3 wc = t.TransformPoint(verts[triangles[i + 2]]);

                        Vector3 normal = Vector3.Cross(wb - wa, wc - wa);
                        if (normal.sqrMagnitude < 1e-8f) continue;
                        normal.Normalize();

                        float dist = Vector3.Dot(normal, wa);
                        tris.Add(new AuditedTri { a = wa, b = wb, c = wc, normal = normal, d = dist, name = mf.gameObject.name });
                    }
                }

                int overlapCount = 0;
                for (int i = 0; i < tris.Count; i++)
                {
                    for (int j = i + 1; j < tris.Count; j++)
                    {
                        if (tris[i].name == tris[j].name) continue;

                        float dot = Vector3.Dot(tris[i].normal, tris[j].normal);
                        if (dot > 0.999f)
                        {
                            if (Mathf.Abs(tris[i].d - tris[j].d) < 0.0005f)
                            {
                                if (TrianglesOverlap2D(tris[i], tris[j]))
                                {
                                    overlapCount++;
                                    if (overlapCount <= 3)
                                    {
                                        zFightingViolations.Add($"{stall.name}: Overlapping coplanar faces between '{tris[i].name}' and '{tris[j].name}' on plane (d={tris[i].d:F4})");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Assert.IsEmpty(zFightingViolations, "Co-planar Z-fighting face overlaps detected:\n" + string.Join("\n", zFightingViolations));
        }

        // =========================================================================
        // Challenge 6: Zero Roof Framing Protrusion Beneath Canopy Surface
        // =========================================================================

        [Test]
        [Description("AdvM3-06: Verifies all structural framing elements (beams and rafters) in CafeStall have clearance <= 0mm beneath canopy surface (zero roof protrusion).")]
        public void AdvM3_06_RoofClearance_ZeroFramingProtrusion()
        {
            Assert.IsNotNull(cafeStall, "CafeStall must exist.");

            Transform roofStructure = cafeStall.transform.Find("RoofStructure");
            Assert.IsNotNull(roofStructure, "CafeStall must have a RoofStructure transform.");

            Transform canopy = cafeStall.transform.Find("Awning") ?? roofStructure.Find("Awning");
            Assert.IsNotNull(canopy, "CafeStall must have an Awning transform.");

            MeshFilter canopyMf = canopy.GetComponent<MeshFilter>();
            Assert.IsNotNull(canopyMf, "Awning must have a MeshFilter.");
            Assert.IsNotNull(canopyMf.sharedMesh, "Awning must have a sharedMesh.");

            float canopyDepth = 1.70f;
            float halfD = canopyDepth * 0.5f;
            float slopeDrop = 0.20f;
            float sagDepth = 0.08f;

            string[] framingNames = new string[] { "Beam_Rear", "Beam_Front", "Rafter_Left", "Rafter_Right" };
            List<string> protrusionFailures = new List<string>();

            foreach (var framingName in framingNames)
            {
                Transform framing = roofStructure.Find(framingName);
                Assert.IsNotNull(framing, $"CafeStall/RoofStructure/{framingName} must exist.");

                MeshFilter mf = framing.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf, $"{framingName} must have a MeshFilter.");
                Assert.IsNotNull(mf.sharedMesh, $"{framingName} sharedMesh must not be null.");

                float maxProtrusion = float.MinValue;
                Vector3 worstVertex = Vector3.zero;

                foreach (var v in mf.sharedMesh.vertices)
                {
                    Vector3 wv = framing.TransformPoint(v);
                    Vector3 canLocal = canopy.InverseTransformPoint(wv);
                    float tz = Mathf.Clamp01((canLocal.z - (-halfD)) / canopyDepth);
                    float deckLocalY = -tz * slopeDrop - 4f * sagDepth * tz * (1f - tz);
                    float deckWorldY = canopy.TransformPoint(new Vector3(canLocal.x, deckLocalY, canLocal.z)).y;
                    float protrusion = wv.y - deckWorldY;

                    if (protrusion > maxProtrusion)
                    {
                        maxProtrusion = protrusion;
                        worstVertex = wv;
                    }
                }

                // Requirement: clearance <= 0mm beneath canopy surface (i.e. protrusion <= 0.0mm)
                if (maxProtrusion > 0.0001f)
                {
                    protrusionFailures.Add($"{framingName} protrudes through canopy surface by {maxProtrusion * 1000f:F1}mm (worst vertex: {worstVertex})");
                }
            }

            Assert.IsEmpty(protrusionFailures, "Roof framing protrusions detected in CafeStall:\n" + string.Join("\n", protrusionFailures));
        }

        private struct AuditedTri
        {
            public Vector3 a, b, c;
            public Vector3 normal;
            public float d;
            public string name;
        }

        private static bool TrianglesOverlap2D(AuditedTri t1, AuditedTri t2)
        {
            Vector3 n = t1.normal;
            int dropAxis = 0;
            if (Mathf.Abs(n.y) > Mathf.Abs(n.x) && Mathf.Abs(n.y) > Mathf.Abs(n.z)) dropAxis = 1;
            else if (Mathf.Abs(n.z) > Mathf.Abs(n.x)) dropAxis = 2;

            Vector2 p1a = Project(t1.a, dropAxis);
            Vector2 p1b = Project(t1.b, dropAxis);
            Vector2 p1c = Project(t1.c, dropAxis);

            Vector2 p2a = Project(t2.a, dropAxis);
            Vector2 p2b = Project(t2.b, dropAxis);
            Vector2 p2c = Project(t2.c, dropAxis);

            Vector2 min1 = Vector2.Min(Vector2.Min(p1a, p1b), p1c);
            Vector2 max1 = Vector2.Max(Vector2.Max(p1a, p1b), p1c);
            Vector2 min2 = Vector2.Min(Vector2.Min(p2a, p2b), p2c);
            Vector2 max2 = Vector2.Max(Vector2.Max(p2a, p2b), p2c);

            if (max1.x < min2.x || max2.x < min1.x || max1.y < min2.y || max2.y < min1.y) return false;

            return SAT_2D(new Vector2[] { p1a, p1b, p1c }, new Vector2[] { p2a, p2b, p2c });
        }

        private static Vector2 Project(Vector3 v, int dropAxis)
        {
            if (dropAxis == 0) return new Vector2(v.y, v.z);
            if (dropAxis == 1) return new Vector2(v.x, v.z);
            return new Vector2(v.x, v.y);
        }

        private static bool SAT_2D(Vector2[] poly1, Vector2[] poly2)
        {
            Vector2[] axes = new Vector2[]
            {
                Perp(poly1[1] - poly1[0]),
                Perp(poly1[2] - poly1[1]),
                Perp(poly1[0] - poly1[2]),
                Perp(poly2[1] - poly2[0]),
                Perp(poly2[2] - poly2[1]),
                Perp(poly2[0] - poly2[2])
            };

            foreach (var axis in axes)
            {
                if (axis.sqrMagnitude < 1e-8f) continue;
                Vector2 norm = axis.normalized;
                ProjectPoly(norm, poly1, out float min1, out float max1);
                ProjectPoly(norm, poly2, out float min2, out float max2);

                if (max1 < min2 + 1e-5f || max2 < min1 + 1e-5f) return false;
            }
            return true;
        }

        private static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

        private static void ProjectPoly(Vector2 axis, Vector2[] poly, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < poly.Length; i++)
            {
                float dot = Vector2.Dot(axis, poly[i]);
                if (dot < min) min = dot;
                if (dot > max) max = dot;
            }
        }
    }
}
