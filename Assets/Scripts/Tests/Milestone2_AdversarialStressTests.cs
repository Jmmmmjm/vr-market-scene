// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Milestone2_AdversarialStressTests.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Empirical adversarial stress harness for Milestone 2:
//   - ProceduralMeshCombiner vertex chunking (<= 65534), primitive collider preservation, Rigidbody exclusion.
//   - Draw call budget verification (<= 100 draw calls, >= 40 headroom).
//   - Street lamps & props physical metrics (height in [2.5, 3.8], emissive luminance > 0, flush colliders).
//   - Benches (seat height in [0.40, 0.50]), Bulletin Board (+Z facing).
//   - Nature elements (canopy radius in [1.2, 2.5], flower boxes flush at Y=0 +- 2mm, hedge height >= 0.8m).
//   - Zero MeshColliders.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using AnimalCrossing.Environment;
using AnimalCrossing.TechnicalArt;

namespace AnimalCrossing.Tests
{
    [TestFixture]
    [Description("Empirical adversarial stress harness for Milestone 2: Static Batching, Nature Elements, Street Lamps, and Draw Call Budget.")]
    public class Milestone2_AdversarialStressTests
    {
        private Material opaqueMat;
        private Material foliageMat;
        private Material emissiveMat;
        private Material liquidGlassMat;
        private GameObject testPlaza;

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

            testPlaza = GameObject.Find("TownSquarePlaza");
            if (testPlaza == null)
            {
                var builderGo = new GameObject("TownSquarePlaza");
                var builder = builderGo.AddComponent<TownSquarePlazaBuilder>();
                builder.OpaqueMaterial = opaqueMat;
                builder.FoliageMaterial = foliageMat;
                builder.EmissiveMaterial = emissiveMat;
                builder.LiquidGlassMaterial = liquidGlassMat;
                builder.BuildPlaza();
                builder.BatchPlaza();
                testPlaza = builderGo;
            }
        }

        // =========================================================================
        // Task 1: ProceduralMeshCombiner Chunking, Colliders, & Rigidbody Exclusion
        // =========================================================================

        [Test]
        [Description("Task 1.1: Adversarial verification that ProceduralMeshCombiner chunks submeshes to strictly <= 65534 vertices with UInt16 index format.")]
        public void AdvM2_01_Combiner_ChunksSubmeshesUnder65534Vertices()
        {
            GameObject testRoot = new GameObject("AdvTest_ChunkRoot");
            try
            {
                // Generate 4 meshes with 24,000 vertices each (total = 96,000 vertices > 65534)
                const int vertsPerMesh = 24000;
                const int meshCount = 4;

                for (int i = 0; i < meshCount; i++)
                {
                    GameObject child = new GameObject($"SubMesh_{i}");
                    child.transform.SetParent(testRoot.transform, false);
                    child.transform.localPosition = new Vector3(i * 2f, 0f, 0f);

                    Mesh m = new Mesh();
                    Vector3[] verts = new Vector3[vertsPerMesh];
                    int[] tris = new int[vertsPerMesh];
                    for (int v = 0; v < vertsPerMesh; v++)
                    {
                        verts[v] = new Vector3(v % 100, v / 100, 0f);
                        tris[v] = v;
                    }
                    m.vertices = verts;
                    m.triangles = tris;

                    var mf = child.AddComponent<MeshFilter>();
                    mf.sharedMesh = m;
                    var mr = child.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = opaqueMat;
                }

                GameObject combined = ProceduralMeshCombiner.CombineStaticMeshes(testRoot, opaqueMat, "AdvTest_CombinedChunks");
                Assert.IsNotNull(combined, "CombineStaticMeshes must succeed and return container.");

                var combinedFilters = combined.GetComponentsInChildren<MeshFilter>();
                Assert.Greater(combinedFilters.Length, 1, "Must chunk into > 1 combined submeshes due to vertex limit.");

                int totalVertsCombined = 0;
                foreach (var mf in combinedFilters)
                {
                    int vCount = mf.sharedMesh.vertexCount;
                    totalVertsCombined += vCount;
                    Assert.LessOrEqual(vCount, ProceduralMeshCombiner.MaxVerticesPerSubmesh,
                        $"Chunk {mf.gameObject.name} vertex count ({vCount}) must be <= {ProceduralMeshCombiner.MaxVerticesPerSubmesh}.");
                    Assert.AreEqual(UnityEngine.Rendering.IndexFormat.UInt16, mf.sharedMesh.indexFormat,
                        $"Chunk {mf.gameObject.name} must strictly use UInt16 index format.");
                }

                Assert.AreEqual(meshCount * vertsPerMesh, totalVertsCombined,
                    "Total combined vertex count must preserve all source vertices without data loss.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        [Description("Task 1.2: Adversarial verification that ProceduralMeshCombiner preserves all primitive colliders and deactivates source renderers.")]
        public void AdvM2_02_Combiner_PreservesPrimitiveCollidersAndDisablesRenderers()
        {
            GameObject testRoot = new GameObject("AdvTest_ColliderRoot");
            try
            {
                // BoxCollider
                GameObject goBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                goBox.name = "BoxProp";
                goBox.transform.SetParent(testRoot.transform, false);
                goBox.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;
                var boxCol = goBox.GetComponent<BoxCollider>();

                // SphereCollider
                GameObject goSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                goSphere.name = "SphereProp";
                goSphere.transform.SetParent(testRoot.transform, false);
                goSphere.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;
                var sphereCol = goSphere.GetComponent<SphereCollider>();

                // CapsuleCollider
                GameObject goCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                goCapsule.name = "CapsuleProp";
                goCapsule.transform.SetParent(testRoot.transform, false);
                goCapsule.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;
                var capsuleCol = goCapsule.GetComponent<CapsuleCollider>();

                GameObject combined = ProceduralMeshCombiner.CombineStaticMeshes(testRoot, opaqueMat, "AdvTest_CombinedColliders");
                Assert.IsNotNull(combined, "CombineStaticMeshes must succeed.");

                // Colliders must still exist, be active, and retain their primitive types
                Assert.IsNotNull(boxCol, "BoxCollider must not be destroyed.");
                Assert.IsTrue(boxCol.enabled, "BoxCollider must remain enabled.");

                Assert.IsNotNull(sphereCol, "SphereCollider must not be destroyed.");
                Assert.IsTrue(sphereCol.enabled, "SphereCollider must remain enabled.");

                Assert.IsNotNull(capsuleCol, "CapsuleCollider must not be destroyed.");
                Assert.IsTrue(capsuleCol.enabled, "CapsuleCollider must remain enabled.");

                // Source renderers must be disabled
                Assert.IsFalse(goBox.GetComponent<MeshRenderer>().enabled, "Source Box MeshRenderer must be disabled.");
                Assert.IsFalse(goSphere.GetComponent<MeshRenderer>().enabled, "Source Sphere MeshRenderer must be disabled.");
                Assert.IsFalse(goCapsule.GetComponent<MeshRenderer>().enabled, "Source Capsule MeshRenderer must be disabled.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        [Description("Task 1.3: Adversarial verification that ProceduralMeshCombiner strictly excludes dynamic Rigidbodies and interactables.")]
        public void AdvM2_03_Combiner_ExcludesRigidbodiesAndInteractables()
        {
            GameObject testRoot = new GameObject("AdvTest_DynamicRoot");
            try
            {
                // 1. Static object (should be combined)
                GameObject staticObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                staticObj.name = "Static_Obj";
                staticObj.transform.SetParent(testRoot.transform, false);
                staticObj.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;

                // 2. Object with Rigidbody on self (must NOT be combined)
                GameObject rbObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rbObj.name = "Rigidbody_Direct";
                rbObj.transform.SetParent(testRoot.transform, false);
                rbObj.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;
                rbObj.AddComponent<Rigidbody>();

                // 3. Object with Rigidbody on parent (must NOT be combined)
                GameObject parentRb = new GameObject("Parent_With_RB");
                parentRb.transform.SetParent(testRoot.transform, false);
                parentRb.AddComponent<Rigidbody>();
                GameObject childRbObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                childRbObj.name = "Child_Of_RB";
                childRbObj.transform.SetParent(parentRb.transform, false);
                childRbObj.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;

                // 4. Object tagged "Interactable" (must NOT be combined)
                GameObject interactableObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                interactableObj.name = "Tagged_Interactable";
                interactableObj.transform.SetParent(testRoot.transform, false);
                interactableObj.GetComponent<MeshRenderer>().sharedMaterial = opaqueMat;
                try { interactableObj.tag = "Interactable"; } catch { }

                GameObject combined = ProceduralMeshCombiner.CombineStaticMeshes(testRoot, opaqueMat, "AdvTest_CombinedDynamic");
                Assert.IsNotNull(combined, "CombineStaticMeshes must combine the static object.");

                Assert.IsFalse(staticObj.GetComponent<MeshRenderer>().enabled, "Static object renderer must be disabled.");
                Assert.IsTrue(rbObj.GetComponent<MeshRenderer>().enabled, "Direct Rigidbody object renderer must remain enabled.");
                Assert.IsTrue(childRbObj.GetComponent<MeshRenderer>().enabled, "Child of Rigidbody object renderer must remain enabled.");
                Assert.IsTrue(interactableObj.GetComponent<MeshRenderer>().enabled, "Interactable tagged object renderer must remain enabled.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        // =========================================================================
        // Task 2: Draw Call Budget Stress-Testing (<= 100 Draw Calls, >= 40 Headroom)
        // =========================================================================

        [Test]
        [Description("Task 2: Empirically measures draw calls across full plaza viewpoints; strictly <= 100 with >= 40 headroom.")]
        public void AdvM2_04_DrawCallBudget_CompletePlazaView_StrictlyBelow100WithHeadroom()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.IsNotNull(cam, "Camera must exist in the scene to measure draw calls.");

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;

            var viewpoints = new (string name, Vector3 pos, Vector3 lookAt)[]
            {
                ("Complete Overhead Overview", new Vector3(0f, 22f, -22f), new Vector3(0f, 0f, 0f)),
                ("Nadir Top-Down View", new Vector3(0f, 25f, 0f), new Vector3(0f, 0f, 0.01f)),
                ("SW Corner Eye Level (1.7m)", new Vector3(-11f, 1.7f, -11f), new Vector3(0f, 1f, 0f)),
                ("SE Corner Eye Level (1.7m)", new Vector3(11f, 1.7f, -11f), new Vector3(0f, 1f, 0f)),
                ("NW Corner Eye Level (1.7m)", new Vector3(-11f, 1.7f, 11f), new Vector3(0f, 1f, 0f)),
                ("NE Corner Eye Level (1.7m)", new Vector3(11f, 1.7f, 11f), new Vector3(0f, 1f, 0f)),
                ("Center Fountain Panoramic", new Vector3(0f, 1.7f, 0f), new Vector3(0f, 1.7f, 12f))
            };

            const int maxDrawCallBudget = 100;
            const int minHeadroomRequired = 40;

            try
            {
                foreach (var vp in viewpoints)
                {
                    cam.transform.position = vp.pos;
                    cam.transform.LookAt(vp.lookAt);

                    RenderTexture rt = new RenderTexture(1920, 1080, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(rt);

                    int actualDrawCalls = UnityEditor.UnityStats.drawCalls;
                    int headroom = maxDrawCallBudget - actualDrawCalls;

                    Assert.LessOrEqual(actualDrawCalls, maxDrawCallBudget,
                        $"Vantage point '{vp.name}' produced {actualDrawCalls} draw calls, exceeding budget of {maxDrawCallBudget}.");
                    Assert.GreaterOrEqual(headroom, minHeadroomRequired,
                        $"Vantage point '{vp.name}' has {headroom} headroom, which is below required headroom of {minHeadroomRequired}.");
                }
            }
            finally
            {
                cam.transform.position = origPos;
                cam.transform.rotation = origRot;
            }
        }

        // =========================================================================
        // Task 3: Street Lamps & Props Empirical Verification
        // =========================================================================

        [Test]
        [Description("Task 3.1: Verifies street lamp height in [2.5m, 3.8m], emissive core luminance > 0, colliders flush at Y >= 0.")]
        public void AdvM2_05_StreetLamps_HeightInBound_EmissiveCoreLuminance_FlushColliders()
        {
            var lampsRoot = testPlaza.transform.Find("StreetLamps");
            Assert.IsNotNull(lampsRoot, "StreetLamps container must exist under TownSquarePlaza.");
            Assert.AreEqual(4, lampsRoot.childCount, "Must have exactly 4 street lamps framing plaza corridors.");

            for (int i = 0; i < lampsRoot.childCount; i++)
            {
                var lamp = lampsRoot.GetChild(i);
                var renderers = lamp.GetComponentsInChildren<MeshRenderer>(true);

                Bounds totalBounds = new Bounds(lamp.position, Vector3.zero);
                bool hasBounds = false;
                foreach (var mr in renderers)
                {
                    if (mr.GetComponent<MeshFilter>()?.sharedMesh != null)
                    {
                        if (!hasBounds) { totalBounds = mr.bounds; hasBounds = true; }
                        else { totalBounds.Encapsulate(mr.bounds); }
                    }
                }

                float lampHeight = totalBounds.size.y;
                Assert.GreaterOrEqual(lampHeight, 2.5f, $"Lamp '{lamp.name}' height ({lampHeight:F3}m) must be >= 2.5m.");
                Assert.LessOrEqual(lampHeight, 3.8f, $"Lamp '{lamp.name}' height ({lampHeight:F3}m) must be <= 3.8m.");

                // Emissive core luminance
                var core = lamp.Find("LanternCore");
                Assert.IsNotNull(core, $"Lamp '{lamp.name}' must have a LanternCore child.");
                var coreMr = core.GetComponent<MeshRenderer>();
                Assert.IsNotNull(coreMr, "LanternCore must have MeshRenderer.");
                Assert.IsNotNull(coreMr.sharedMaterial, "LanternCore must have sharedMaterial.");
                Color emColor = coreMr.sharedMaterial.GetColor("_EmissionColor");
                float luminance = emColor.r * 0.2126f + emColor.g * 0.7152f + emColor.b * 0.0722f;
                Assert.Greater(luminance, 0f, $"LanternCore emissive luminance ({luminance:F3}) must be strictly > 0.");

                // Collider flush with ground (zero penetration deeper than ground anchor Y = -0.05m)
                var col = lamp.GetComponent<CapsuleCollider>();
                Assert.IsNotNull(col, $"Lamp '{lamp.name}' must have CapsuleCollider.");
                float colBottomY = lamp.position.y + col.center.y - col.height * 0.5f;
                Assert.GreaterOrEqual(colBottomY, -0.001f,
                    $"Lamp '{lamp.name}' collider bottom ({colBottomY:F4}m) must sit flush at Y >= 0 (zero penetration into ground).");
            }
        }

        [Test]
        [Description("Task 3.2: Verifies bench seat height in [0.40m, 0.50m], and bulletin board facing North (+Z).")]
        public void AdvM2_06_Benches_SeatHeight_BulletinBoard_FacingNorth()
        {
            // Benches
            var benchesRoot = testPlaza.transform.Find("Benches");
            Assert.IsNotNull(benchesRoot, "Benches container must exist under TownSquarePlaza.");
            Assert.GreaterOrEqual(benchesRoot.childCount, 2, "Must have at least 2 benches.");

            for (int i = 0; i < benchesRoot.childCount; i++)
            {
                var bench = benchesRoot.GetChild(i);
                var seat = bench.Find("SeatSlat_0");
                Assert.IsNotNull(seat, $"Bench '{bench.name}' must have SeatSlat_0.");
                float seatMaxY = seat.GetComponent<MeshRenderer>().bounds.max.y;

                Assert.GreaterOrEqual(seatMaxY, 0.40f, $"Bench '{bench.name}' seat top surface ({seatMaxY:F4}m) must be >= 0.40m.");
                Assert.LessOrEqual(seatMaxY, 0.50f, $"Bench '{bench.name}' seat top surface ({seatMaxY:F4}m) must be <= 0.50m.");
            }

            // Bulletin Board
            var bb = testPlaza.transform.Find("BulletinBoard");
            Assert.IsNotNull(bb, "BulletinBoard must exist under TownSquarePlaza.");
            float dotZ = Vector3.Dot(bb.forward, Vector3.forward);
            Assert.Greater(dotZ, 0.999f,
                $"BulletinBoard forward vector ({bb.forward}) must face North (+Z) toward main walkway corridor.");
        }

        [Test]
        [Description("Task 3.3: Verifies tree canopy radius in [1.2m, 2.5m], flower boxes flush at Y = 0 +- 2mm, hedge height >= 0.8m.")]
        public void AdvM2_07_NatureElements_TreeCanopyRadius_FlowerBoxesFlush_HedgeHeight()
        {
            var natureRoot = testPlaza.transform.Find("NatureElements");
            Assert.IsNotNull(natureRoot, "NatureElements container must exist under TownSquarePlaza.");

            // 1. Trees
            int treeCount = 0;
            for (int i = 0; i < natureRoot.childCount; i++)
            {
                var child = natureRoot.GetChild(i);
                if (child.name.StartsWith("Tree_"))
                {
                    treeCount++;
                    var canopy = child.Find("CanopyCrown");
                    Assert.IsNotNull(canopy, $"Tree '{child.name}' must have CanopyCrown child.");
                    var mr = canopy.GetComponent<MeshRenderer>();
                    Assert.IsNotNull(mr, $"CanopyCrown of '{child.name}' must have MeshRenderer.");

                    Bounds cb = mr.bounds;
                    float horizRadius = Mathf.Max(cb.extents.x, cb.extents.z);
                    Assert.GreaterOrEqual(horizRadius, 1.2f, $"Tree '{child.name}' canopy horizontal radius ({horizRadius:F3}m) must be >= 1.2m.");
                    Assert.LessOrEqual(horizRadius, 2.5f, $"Tree '{child.name}' canopy horizontal radius ({horizRadius:F3}m) must be <= 2.5m.");
                }
            }
            Assert.GreaterOrEqual(treeCount, 4, "Must have at least 4 puffy trees around plaza perimeter.");

            // 2. Flower Boxes
            var flowerBoxes = natureRoot.Find("FlowerBoxes");
            Assert.IsNotNull(flowerBoxes, "FlowerBoxes container must exist.");
            Assert.GreaterOrEqual(flowerBoxes.childCount, 2, "Must have at least 2 flower boxes.");

            for (int i = 0; i < flowerBoxes.childCount; i++)
            {
                var fb = flowerBoxes.GetChild(i);
                var frame = fb.Find("PlanterFrame");
                Assert.IsNotNull(frame, $"FlowerBox '{fb.name}' must have PlanterFrame.");
                Bounds b = frame.GetComponent<MeshRenderer>().bounds;
                float lowestY = b.min.y;
                Assert.AreEqual(0.000f, lowestY, 0.002f,
                    $"FlowerBox '{fb.name}' lowest Y ({lowestY:F4}m) must sit flush on ground at Y = 0 +- 2mm.");
            }

            // 3. Hedges
            var hedges = natureRoot.Find("Hedges");
            Assert.IsNotNull(hedges, "Hedges container must exist.");
            Assert.GreaterOrEqual(hedges.childCount, 2, "Must have at least 2 hedges.");

            for (int i = 0; i < hedges.childCount; i++)
            {
                var hedge = hedges.GetChild(i);
                var meshChild = hedge.Find("HedgeMesh");
                Assert.IsNotNull(meshChild, $"Hedge '{hedge.name}' must have HedgeMesh.");
                Bounds hb = meshChild.GetComponent<MeshRenderer>().bounds;
                float hedgeHeight = hb.size.y;
                Assert.GreaterOrEqual(hedgeHeight, 0.8f,
                    $"Hedge '{hedge.name}' height ({hedgeHeight:F3}m) must be >= 0.8m to prevent player walking through.");

                var col = hedge.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"Hedge '{hedge.name}' must have BoxCollider.");
                Assert.GreaterOrEqual(col.size.y, 0.8f,
                    $"Hedge '{hedge.name}' BoxCollider height ({col.size.y:F3}m) must be >= 0.8m.");
            }
        }

        [Test]
        [Description("Task 3.4: Strict zero dynamic MeshCollider audit across entire TownSquarePlaza hierarchy.")]
        public void AdvM2_08_ZeroDynamicMeshColliders_AcrossEntirePlaza()
        {
            var meshColliders = testPlaza.GetComponentsInChildren<MeshCollider>(true);
            Assert.AreEqual(0, meshColliders.Length,
                "TownSquarePlaza must strictly contain ZERO MeshColliders (100% compound primitive colliders).");
        }
    }
}
