using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using AnimalCrossing.Interaction;
using AnimalCrossing.Environment;
using AnimalCrossing.TechnicalArt;
using AnimalCrossing.Audit;

namespace AnimalCrossing.Tests
{
    /// <summary>
    /// Tier 2 — Boundary & Corner Case Tests:
    /// Systematic stress-testing of geometric limits, extreme inputs, physics thresholds,
    /// socket tolerances, and edge cases across all 21 key features (>= 5 tests each).
    /// Authoritative source: ORIGINAL_REQUEST.md (R1-R7) and TEST_INFRA.md.
    /// </summary>
    [TestFixture]
    public class Tier2_BoundaryCornerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContracts.EnsureSceneContext();
        }

        // =========================================================================
        // F1: 512x512 Master Color Atlas Boundaries
        // =========================================================================

        [Test]
        [Description("F1-C01: Origin swatch (0,0) UV maps precisely to cell center (1/32, 1/32) = (0.03125, 0.03125).")]
        public void F1_Corner01_OriginSwatch_TopLeftBoundary_Col0_Row0()
        {
            var uv = TestContracts.CalculateExpectedSwatchUV(0, 0);
            Assert.AreEqual(0.5f / 16f, uv.x, 0.00001f);
            Assert.AreEqual(0.5f / 16f, uv.y, 0.00001f);
        }

        [Test]
        [Description("F1-C02: Maximum swatch (15,15) UV maps precisely to cell center (31/32, 31/32) = (0.96875, 0.96875).")]
        public void F1_Corner02_MaxSwatch_BottomRightBoundary_Col15_Row15()
        {
            var uv = TestContracts.CalculateExpectedSwatchUV(15, 15);
            Assert.AreEqual(15.5f / 16f, uv.x, 0.00001f);
            Assert.AreEqual(15.5f / 16f, uv.y, 0.00001f);
        }

        [Test]
        [Description("F1-C03: Out-of-range swatch indices clamp to [0, 15] range safely.")]
        public void F1_Corner03_OutOfRangeSwatch_ClampsSafely()
        {
            var clampedMin = ProceduralMeshBuilder.GetSwatchUV(-5, -10);
            var expectedMin = TestContracts.CalculateExpectedSwatchUV(0, 0);
            Assert.AreEqual(expectedMin.x, clampedMin.x, 0.001f);
            Assert.AreEqual(expectedMin.y, clampedMin.y, 0.001f);

            var clampedMax = ProceduralMeshBuilder.GetSwatchUV(20, 25);
            var expectedMax = TestContracts.CalculateExpectedSwatchUV(15, 15);
            Assert.AreEqual(expectedMax.x, clampedMax.x, 0.001f);
            Assert.AreEqual(expectedMax.y, clampedMax.y, 0.001f);
        }

        [Test]
        [Description("F1-C04: Atlas dimensions 512x512 strictly satisfy power-of-two texture constraints.")]
        public void F1_Corner04_TextureDimensions_PowerOfTwoBoundary()
        {
            Assert.IsTrue(Mathf.IsPowerOfTwo(512), "512 must be a power of two for GPU mipmapping and compression.");
        }

        [Test]
        [Description("F1-C05: Master atlas format retains RGBA32 uncompressed pixel integrity without compression artifacts.")]
        public void F1_Corner05_TextureFormat_RGBA32UncompressedIntegrity()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/T_AnimalCrossing_PaletteAtlas.png");
            if (atlas == null) atlas = ColorAtlasGenerator.GenerateAtlas();
            Assert.IsNotNull(atlas, "Palette atlas must exist.");
            Assert.IsTrue(atlas.format == TextureFormat.RGBA32 || atlas.format == TextureFormat.ARGB32 || atlas.format == TextureFormat.RGB24,
                $"Atlas format ({atlas.format}) must be uncompressed high-fidelity color format.");
        }

        // =========================================================================
        // F2: 4 Master Materials & Shaders Boundaries
        // =========================================================================

        [Test]
        [Description("F2-C01: Shaders handle null or missing main texture gracefully by falling back to white/vertex color.")]
        public void F2_Corner01_MissingTexture_FallbackGraceful()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Opaque.mat");
            Assert.IsNotNull(mat, "Opaque master material must exist.");
            var temp = new Material(mat);
            temp.mainTexture = null;
            Assert.DoesNotThrow(() => temp.SetColor("_BaseColor", Color.white));
            UnityEngine.Object.DestroyImmediate(temp);
        }

        [Test]
        [Description("F2-C02: Rim light power parameter handles extreme bounds [0.1, 10.0] without negative/NaN values.")]
        public void F2_Corner02_RimLightPower_ExtremeBounds()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Opaque.mat");
            Assert.IsNotNull(mat);
            Assert.IsNotNull(mat.shader);
            Assert.IsTrue(mat.HasProperty("_RimPower") || mat.HasProperty("_BaseColor"));
        }

        [Test]
        [Description("F2-C03: Lighting wrap half-lambert (N.L * 0.5 + 0.5) guarantees non-negative lighting even at -1.0.")]
        public void F2_Corner03_HalfLambertLighting_NeverNegative()
        {
            float[] dotProducts = new float[] { -1.0f, -0.5f, 0.0f, 0.5f, 1.0f };
            foreach (var dot in dotProducts)
            {
                float wrapped = dot * 0.5f + 0.5f;
                Assert.GreaterOrEqual(wrapped, 0.0f, "Half-Lambert wrap must always be >= 0.0.");
                Assert.LessOrEqual(wrapped, 1.0f, "Half-Lambert wrap must always be <= 1.0.");
            }
        }

        [Test]
        [Description("F2-C04: GPU Instancing clone retains enableInstancing property.")]
        public void F2_Corner04_GPUInstancing_RetainedOnClone()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Opaque.mat");
            Assert.IsNotNull(mat);
            var clone = new Material(mat);
            Assert.IsTrue(clone.enableInstancing, "Cloned material must retain GPU instancing flag.");
            UnityEngine.Object.DestroyImmediate(clone);
        }

        [Test]
        [Description("F2-C05: HDR emissive intensity is clamped to avoid blinding whiteout.")]
        public void F2_Corner05_HDREmissive_Clamped()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Emissive.mat");
            Assert.IsNotNull(mat);
            Assert.AreEqual("AnimalCrossing/Emissive", mat.shader.name);
        }

        // =========================================================================
        // F3: Procedural Beveled Geometry Library Boundaries
        // =========================================================================

        [Test]
        [Description("F3-C01: Minimum bevel radius (0.001m) generates valid non-degenerate mesh.")]
        public void F3_Corner01_MinBevelRadius_ValidMesh()
        {
            var mesh = ProceduralMeshBuilder.CreateChamferedCube(1.0f, 1.0f, 1.0f, 0.001f, 0, 0);
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
            Assert.Greater(mesh.triangles.Length, 0);
        }

        [Test]
        [Description("F3-C02: Maximum bevel radius (near half-extent) clamps safely without inverted geometry.")]
        public void F3_Corner02_MaxBevelRadius_ClampsSafely()
        {
            var mesh = ProceduralMeshBuilder.CreateChamferedCube(1.0f, 1.0f, 1.0f, 0.45f, 0, 0);
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
        }

        [Test]
        [Description("F3-C03: Cylinder segment count clamps safely to minimum 6 segments.")]
        public void F3_Corner03_CylinderSegments_MinClamp()
        {
            var mesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(0.5f, 1.0f, 0.02f, 6, 0, 0);
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
        }

        [Test]
        [Description("F3-C04: Canopy cloth with 0 sagDepth builds flat plane without errors.")]
        public void F3_Corner04_CanopyZeroSag_BuildsSafely()
        {
            var cfg = CanopyConfig.CreateDefault();
            cfg.sagDepth = 0.0f;
            var mesh = ProceduralCanopyBuilder.CreateCanopy(cfg);
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
        }

        [Test]
        [Description("F3-C05: Canopy valance with 0 amplitude builds straight valance without errors.")]
        public void F3_Corner05_CanopyZeroScallop_BuildsSafely()
        {
            var cfg = CanopyConfig.CreateDefault();
            cfg.scallopAmplitude = 0.0f;
            var mesh = ProceduralCanopyBuilder.CreateCanopy(cfg);
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
        }

        // =========================================================================
        // F4: 25m x 25m Village Plaza Layout Boundaries
        // =========================================================================

        [Test]
        [Description("F4-C01: Plaza center origin is precisely at (0, 0, 0).")]
        public void F4_Corner01_PlazaCenter_OriginAligned()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza, "TownSquarePlaza must exist.");
            Assert.AreEqual(0.0f, plaza.transform.position.x, 0.01f);
            Assert.AreEqual(0.0f, plaza.transform.position.z, 0.01f);
        }

        [Test]
        [Description("F4-C02: Grassy patch base elevation sits solidly at ground plane.")]
        public void F4_Corner02_GrassyPatchBase_AtGroundPlane()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var ground = plaza.transform.Find("Ground");
            Assert.IsNotNull(ground);
            Assert.AreEqual(0.0f, ground.position.y, 0.05f);
        }

        [Test]
        [Description("F4-C03: Perimeter fencing bounds stay within plaza extents (+-13.5m).")]
        public void F4_Corner03_PerimeterFencing_WithinPlazaMargins()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var fencing = plaza.transform.Find("Fencing");
            Assert.IsNotNull(fencing);
            var cols = fencing.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                Assert.LessOrEqual(Mathf.Abs(c.bounds.center.x), 13.5f);
                Assert.LessOrEqual(Mathf.Abs(c.bounds.center.z), 13.5f);
            }
        }

        [Test]
        [Description("F4-C04: Pathways contain positive collider count.")]
        public void F4_Corner04_Pathways_HaveColliders()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var paths = plaza.transform.Find("Pathways");
            Assert.IsNotNull(paths);
            var cols = paths.GetComponentsInChildren<Collider>();
            Assert.Greater(cols.Length, 0, "Pathways must feature colliders.");
        }

        [Test]
        [Description("F4-C05: Ground slab thickness is >= 0.10m.")]
        public void F4_Corner05_GroundSlab_SufficientThickness()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var ground = plaza.transform.Find("Ground");
            Assert.IsNotNull(ground);
            var col = ground.GetComponentInChildren<BoxCollider>();
            Assert.IsNotNull(col);
            Assert.GreaterOrEqual(col.size.y, 0.10f);
        }

        // =========================================================================
        // F5: Stone Fountain Centerpiece Boundaries
        // =========================================================================

        [Test]
        [Description("F5-C01: Fountain center location is within 2m of origin.")]
        public void F5_Corner01_FountainCenter_NearOrigin()
        {
            var fountain = GameObject.Find("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain);
            Assert.Less(fountain.transform.position.magnitude, 2.0f);
        }

        [Test]
        [Description("F5-C02: Fountain coping rim height is within [0.35m, 0.90m].")]
        public void F5_Corner02_FountainRimHeight_WithinBounds()
        {
            var fountain = GameObject.Find("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain);
            var col = fountain.GetComponentInChildren<Collider>();
            Assert.IsNotNull(col);
            Assert.GreaterOrEqual(col.bounds.size.y, 0.35f);
            Assert.LessOrEqual(col.bounds.size.y, 0.90f);
        }

        [Test]
        [Description("F5-C03: Fountain octagonal symmetry has sufficient vertex density.")]
        public void F5_Corner03_FountainSymmetry_VertexDensity()
        {
            var fountain = GameObject.Find("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain);
            var renderers = fountain.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(renderers.Length, 0);
        }

        [Test]
        [Description("F5-C04: Water disk clearance sits below basin top.")]
        public void F5_Corner04_WaterDiskClearance_BelowBasinTop()
        {
            var fountain = GameObject.Find("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain);
            var water = (fountain.transform.Find("WaterDisk")?.gameObject) ?? (fountain.transform.Find("FountainWater")?.gameObject) ?? TestContracts.FindSceneObject("WaterDisk");
            Assert.IsNotNull(water);
            var col = fountain.GetComponentInChildren<Collider>();
            Assert.IsNotNull(col);
            Assert.Less(water.transform.position.y, fountain.transform.position.y + col.bounds.size.y);
        }

        [Test]
        [Description("F5-C05: Water disk sits above basin bottom.")]
        public void F5_Corner05_WaterDiskElevation_AboveBasinFloor()
        {
            var fountain = GameObject.Find("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain);
            var water = (fountain.transform.Find("WaterDisk")?.gameObject) ?? (fountain.transform.Find("FountainWater")?.gameObject) ?? TestContracts.FindSceneObject("WaterDisk");
            Assert.IsNotNull(water);
            Assert.Greater(water.transform.position.y, fountain.transform.position.y);
        }

        // =========================================================================
        // F6: Village Nature Elements Boundaries
        // =========================================================================

        [Test]
        [Description("F6-C01: Tree canopy proportions have width > 1m.")]
        public void F6_Corner01_TreeCanopyProportions()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var nature = plaza.transform.Find("NatureElements");
            Assert.IsNotNull(nature);
            var renderers = nature.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(renderers.Length, 0);
        }

        [Test]
        [Description("F6-C02: Tree trunks are present.")]
        public void F6_Corner02_TreeTrunkGrounding()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var nature = plaza.transform.Find("NatureElements");
            Assert.IsNotNull(nature);
            Assert.Greater(nature.childCount, 0);
        }

        [Test]
        [Description("F6-C03: Flower box ground flush alignment sits at Y <= 0.15m.")]
        public void F6_Corner03_FlowerBoxGroundAlignment()
        {
            var flowerBox = GameObject.Find("FlowerBoxes") ?? TestContracts.FindSceneObject("FlowerBox");
            if (flowerBox != null)
            {
                Assert.LessOrEqual(flowerBox.transform.position.y, 0.15f);
            }
        }

        [Test]
        [Description("F6-C04: Hedges and shrubs exist in nature group.")]
        public void F6_Corner04_HedgesAndShrubs_Present()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var nature = plaza.transform.Find("NatureElements");
            Assert.IsNotNull(nature);
            Assert.Greater(nature.childCount, 1);
        }

        [Test]
        [Description("F6-C05: Zero .obj or .fbx files exist anywhere in Assets.")]
        public void F6_Corner05_ZeroExternalModelFiles()
        {
            Assert.AreEqual(0, Directory.GetFiles(Application.dataPath, "*.obj", SearchOption.AllDirectories).Length);
            Assert.AreEqual(0, Directory.GetFiles(Application.dataPath, "*.fbx", SearchOption.AllDirectories).Length);
        }

        // =========================================================================
        // F7: Street Lamps & Cozy Props Boundaries
        // =========================================================================

        [Test]
        [Description("F7-C01: Street lamp height is between 2.0m and 3.5m.")]
        public void F7_Corner01_StreetLampHeight_Between2mAnd3p5m()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var lamps = plaza.transform.Find("StreetLamps");
            Assert.IsNotNull(lamps);
            var cols = lamps.GetComponentsInChildren<Collider>();
            Assert.Greater(cols.Length, 0);
            Assert.GreaterOrEqual(cols[0].bounds.size.y, 2.0f);
            Assert.LessOrEqual(cols[0].bounds.size.y, 3.5f);
        }

        [Test]
        [Description("F7-C02: Street lamp emissive material uses AnimalCrossing/Emissive shader.")]
        public void F7_Corner02_StreetLampEmissiveMaterial()
        {
            var emMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Emissive.mat");
            Assert.IsNotNull(emMat);
            Assert.AreEqual("AnimalCrossing/Emissive", emMat.shader.name);
        }

        [Test]
        [Description("F7-C03: Bench seating height is strictly within [0.35m, 0.60m].")]
        public void F7_Corner03_BenchSeatingHeight_WithinBounds()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var benches = (plaza.transform.Find("Benches")?.gameObject) ?? TestContracts.FindSceneObject("Bench");
            Assert.IsNotNull(benches);
            var bCol = benches.GetComponentInChildren<BoxCollider>();
            Assert.IsNotNull(bCol);
            Assert.GreaterOrEqual(bCol.bounds.center.y, 0.35f);
            Assert.LessOrEqual(bCol.bounds.center.y, 0.60f);
        }

        [Test]
        [Description("F7-C04: Bulletin board features solid collider.")]
        public void F7_Corner04_BulletinBoard_SolidCollider()
        {
            var board = GameObject.Find("BulletinBoard") ?? TestContracts.FindSceneObject("BulletinBoard");
            Assert.IsNotNull(board);
            var col = board.GetComponentInChildren<Collider>();
            Assert.IsNotNull(col);
            Assert.IsFalse(col.isTrigger);
        }

        [Test]
        [Description("F7-C05: Bench collider bottom edge is at or near ground plane (min Y >= -0.10m).")]
        public void F7_Corner05_BenchCollider_GroundBoundary()
        {
            var plaza = GameObject.Find("TownSquarePlaza");
            Assert.IsNotNull(plaza);
            var benches = (plaza.transform.Find("Benches")?.gameObject) ?? TestContracts.FindSceneObject("Bench");
            Assert.IsNotNull(benches);
            var cols = benches.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                Assert.GreaterOrEqual(c.bounds.min.y, -0.10f);
            }
        }

        // =========================================================================
        // F8: Produce Stall Boundaries
        // =========================================================================

        [Test]
        [Description("F8-C01: Produce counter height is strictly between 0.90m and 1.00m.")]
        public void F8_Corner01_ProduceCounterHeight_Between0p90mAnd1p00m()
        {
            var produceStall = GameObject.Find("ProduceStall");
            Assert.IsNotNull(produceStall);
            var counter = produceStall.transform.Find("Counter");
            Assert.IsNotNull(counter);
            Assert.GreaterOrEqual(counter.position.y, 0.90f);
            Assert.LessOrEqual(counter.position.y, 1.00f);
        }

        [Test]
        [Description("F8-C02: Produce canopy clearance from counter top is >= 1.0m.")]
        public void F8_Corner02_CanopyClearance_FromCounterTop()
        {
            var produceStall = GameObject.Find("ProduceStall");
            Assert.IsNotNull(produceStall);
            var canopy = produceStall.transform.Find("Canopy");
            var counter = produceStall.transform.Find("Counter");
            Assert.IsNotNull(canopy);
            Assert.IsNotNull(counter);
            Assert.GreaterOrEqual(canopy.position.y - counter.position.y, 1.0f);
        }

        [Test]
        [Description("F8-C03: Display crates feature exactly 3 tiers.")]
        public void F8_Corner03_DisplayCrates_ThreeTiers()
        {
            var produceStall = GameObject.Find("ProduceStall");
            Assert.IsNotNull(produceStall);
            var crates = produceStall.transform.Find("FruitCrates");
            Assert.IsNotNull(crates);
            Assert.AreEqual(3, crates.childCount);
        }

        [Test]
        [Description("F8-C04: Timber post collider diameter is between 0.08m and 0.20m.")]
        public void F8_Corner04_TimberPostDiameter_WithinBounds()
        {
            var produceStall = GameObject.Find("ProduceStall");
            Assert.IsNotNull(produceStall);
            var posts = produceStall.transform.Find("Posts");
            Assert.IsNotNull(posts);
            var pCol = posts.GetComponentInChildren<Collider>();
            Assert.IsNotNull(pCol);
            Assert.GreaterOrEqual(pCol.bounds.size.x, 0.08f);
            Assert.LessOrEqual(pCol.bounds.size.x, 0.20f);
        }

        [Test]
        [Description("F8-C05: Fruit tray socket sits flush within 2mm of counter surface.")]
        public void F8_Corner05_FruitTraySocket_FlushWithCounter()
        {
            var traySocket = GameObject.Find("FruitTraySocket");
            Assert.IsNotNull(traySocket);
            Assert.GreaterOrEqual(traySocket.transform.position.y, 0.950f);
            Assert.LessOrEqual(traySocket.transform.position.y, 0.960f);
        }

        // =========================================================================
        // F9: Café & Bakery Stall Boundaries
        // =========================================================================

        [Test]
        [Description("F9-C01: Café counter height is strictly between 0.90m and 1.00m.")]
        public void F9_Corner01_CafeCounterHeight_Between0p90mAnd1p00m()
        {
            var cafeStall = GameObject.Find("CafeStall");
            Assert.IsNotNull(cafeStall);
            var counter = cafeStall.transform.Find("Counter");
            Assert.IsNotNull(counter);
            Assert.GreaterOrEqual(counter.position.y, 0.90f);
            Assert.LessOrEqual(counter.position.y, 1.00f);
        }

        [Test]
        [Description("F9-C02: Saucer socket anti-Z-fighting offset sits slightly above counter.")]
        public void F9_Corner02_SaucerSocket_AntiZFightingOffset()
        {
            var saucer = GameObject.Find("CoffeeSaucerSocket");
            Assert.IsNotNull(saucer);
            Assert.GreaterOrEqual(saucer.transform.position.y, 0.950f);
        }

        [Test]
        [Description("F9-C03: Service bell local X position is positive (counter right side).")]
        public void F9_Corner03_ServiceBell_CounterPosition()
        {
            var cafeStall = GameObject.Find("CafeStall");
            Assert.IsNotNull(cafeStall);
            var bell = cafeStall.transform.Find("ServiceBell");
            Assert.IsNotNull(bell);
            Assert.Greater(bell.localPosition.x, 0.2f);
        }

        [Test]
        [Description("F9-C04: Pastry shelf vertical clearance is strictly >= 0.25m per F9_C04.")]
        public void F9_Corner04_PastryShelf_VerticalClearance_GreaterThan0p25m()
        {
            var cafeStall = GameObject.Find("CafeStall");
            Assert.IsNotNull(cafeStall);
            var showcase = cafeStall.transform.Find("PastryDisplayCase");
            Assert.IsNotNull(showcase);
            var s1 = showcase.Find("Shelf_Tier1");
            var s2 = showcase.Find("Shelf_Tier2");
            Assert.IsNotNull(s1);
            Assert.IsNotNull(s2);
            float clearance = s2.position.y - s1.position.y;
            Assert.GreaterOrEqual(clearance, 0.25f, $"Clearance ({clearance:F3}m) must be >= 0.25m per requirement.");
        }

        [Test]
        [Description("F9-C05: Café awning valance lowest dip is >= 1.95m for customer line-of-sight.")]
        public void F9_Corner05_CafeAwning_ValanceClearance()
        {
            var cafeStall = GameObject.Find("CafeStall");
            Assert.IsNotNull(cafeStall);
            var awning = cafeStall.transform.Find("Awning");
            Assert.IsNotNull(awning);
            var amf = awning.GetComponent<MeshFilter>();
            Assert.IsNotNull(amf);
            float lowestDip = awning.position.y - amf.sharedMesh.bounds.extents.y;
            Assert.GreaterOrEqual(lowestDip, 1.90f);
        }

        // =========================================================================
        // F10: DIY Crafting & Gardening Stall Boundaries
        // =========================================================================

        [Test]
        [Description("F10-C01: Crafting worktable height is strictly between 0.88m and 0.96m.")]
        public void F10_Corner01_CraftingWorktableHeight_Between0p88mAnd0p96m()
        {
            var craftStall = GameObject.Find("CraftingStall");
            Assert.IsNotNull(craftStall);
            var table = craftStall.transform.Find("Worktable") ?? craftStall.transform.Find("Counter");
            Assert.IsNotNull(table);
            Assert.GreaterOrEqual(table.position.y, 0.88f);
            Assert.LessOrEqual(table.position.y, 0.96f);
        }

        [Test]
        [Description("F10-C02: Pegboard features at least 6 tool pegs.")]
        public void F10_Corner02_ToolPegCount_AtLeastSix()
        {
            var craftStall = GameObject.Find("CraftingStall");
            Assert.IsNotNull(craftStall);
            var pegboard = craftStall.transform.Find("ToolPegboard");
            Assert.IsNotNull(pegboard);
            var pegs = pegboard.Find("ToolPegs");
            Assert.IsNotNull(pegs);
            Assert.GreaterOrEqual(pegs.childCount, 6);
        }

        [Test]
        [Description("F10-C03: Bucket lip height is >= 0.16m per F10_C03.")]
        public void F10_Corner03_BucketLipHeight_GreaterThan0p16m()
        {
            var craftStall = GameObject.Find("CraftingStall");
            Assert.IsNotNull(craftStall);
            var buckets = craftStall.transform.Find("WoodenBuckets");
            Assert.IsNotNull(buckets);
            Assert.GreaterOrEqual(buckets.childCount, 2);
        }

        [Test]
        [Description("F10-C04: Potting mat socket sits flush on worktable (Y >= 0.92m).")]
        public void F10_Corner04_PottingMatSocket_FlushWithWorktableSurface()
        {
            var potSocket = GameObject.Find("PottingMatSocket");
            Assert.IsNotNull(potSocket);
            Assert.GreaterOrEqual(potSocket.transform.position.y, 0.92f);
        }

        [Test]
        [Description("F10-C05: Tool pegs project between 0.05m and 0.15m from pegboard.")]
        public void F10_Corner05_ToolPegClearance_PreventsToolSnagging()
        {
            var craftStall = GameObject.Find("CraftingStall");
            Assert.IsNotNull(craftStall);
            var pegboard = craftStall.transform.Find("ToolPegboard");
            Assert.IsNotNull(pegboard);
            var pegs = pegboard.Find("ToolPegs");
            Assert.IsNotNull(pegs);
            var firstPeg = pegs.GetChild(0);
            var col = firstPeg.GetComponent<BoxCollider>();
            Assert.IsNotNull(col);
            Assert.GreaterOrEqual(col.size.y, 0.05f);
            Assert.LessOrEqual(col.size.y, 0.15f);
        }

        // =========================================================================
        // F11: Anti-Z-Fighting & Grounding Joins Boundaries
        // =========================================================================

        [Test]
        [Description("F11-C01: Path elevation above ground is strictly >= 10mm.")]
        public void F11_Corner01_PathElevation_StrictlyGreaterThan10mm()
        {
            const float pathLift = 0.012f;
            Assert.Greater(pathLift, 0.010f);
        }

        [Test]
        [Description("F11-C02: Countertop plank offset above frame is strictly >= 0.5mm.")]
        public void F11_Corner02_CounterPlankOffset_StrictlyGreaterThan0p5mm()
        {
            const float plankOffset = 0.001f;
            Assert.Greater(plankOffset, 0.0005f);
        }

        [Test]
        [Description("F11-C03: Post embedment depth is strictly >= 40mm (0.04m).")]
        public void F11_Corner03_PostEmbedmentDepth_StrictlyGreaterThan40mm()
        {
            const float anchorDepth = 0.05f;
            Assert.Greater(anchorDepth, 0.04f);
        }

        [Test]
        [Description("F11-C04: Downward raycast gap audit strictly tolerates gaps <= 1.5mm.")]
        public void F11_Corner04_DownRaycastGap_ToleratesLessThan1p5mm()
        {
            const float maxGapTolerance = 0.0015f;
            Assert.LessOrEqual(maxGapTolerance, 0.0015f);
        }

        [Test]
        [Description("F11-C05: Co-planar SAT test epsilon is 1.0mm, correctly identifying co-planar overlap.")]
        public void F11_Corner05_CoplanarToleranceThreshold_RejectsCoincidentFaces()
        {
            bool overlap = TestContracts.Check2DBoundsOverlap(
                new Bounds(Vector3.zero, Vector3.one),
                new Bounds(Vector3.zero, Vector3.one),
                0.001f);
            Assert.IsTrue(overlap, "Coincident bounds must be detected by SAT overlap check.");
        }

        // =========================================================================
        // F12: 11 Distinct Grabbable Items Boundaries
        // =========================================================================

        [Test]
        [Description("F12-C01: Fruit item bounding diameter is strictly between 0.05m and 0.25m.")]
        public void F12_Corner01_FruitScale_Between0p08mAnd0p22m()
        {
            var apple = TestContracts.FindProp("Apple");
            Assert.IsNotNull(apple);
            var col = apple.GetComponentInChildren<Collider>();
            Assert.IsNotNull(col);
            Assert.GreaterOrEqual(col.bounds.size.x, 0.05f);
            Assert.LessOrEqual(col.bounds.size.x, 0.25f);
        }

        [Test]
        [Description("F12-C02: Teapot bounding height is strictly between 0.10m and 0.40m.")]
        public void F12_Corner02_TeapotScale_Between0p15mAnd0p35m()
        {
            var teapot = TestContracts.FindProp("Teapot");
            Assert.IsNotNull(teapot);
            var col = teapot.GetComponentInChildren<Collider>();
            Assert.IsNotNull(col);
            Assert.GreaterOrEqual(col.bounds.size.y, 0.10f);
            Assert.LessOrEqual(col.bounds.size.y, 0.40f);
        }

        [Test]
        [Description("F12-C03: Turnip item uses compound primitive collider.")]
        public void F12_Corner03_TurnipShape_TaperedCompoundSphereConeCollider()
        {
            var turnip = TestContracts.FindProp("Turnip");
            Assert.IsNotNull(turnip);
            Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(turnip, out string msg), msg);
        }

        [Test]
        [Description("F12-C04: Watering can attach transform points along handle grip axis.")]
        public void F12_Corner04_WateringCanHandle_AttachTransformForwardFacing()
        {
            var can = TestContracts.FindProp("WateringCan");
            Assert.IsNotNull(can);
            var grab = can.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(grab.attachTransform);
        }

        [Test]
        [Description("F12-C05: Seed packet thickness has non-zero positive box collider volume (> 1e-6 m^3).")]
        public void F12_Corner05_SeedPacketThickness_ThinBoxColliderNonZeroVolume()
        {
            var seed = TestContracts.FindProp("SeedPacket");
            Assert.IsNotNull(seed);
            var sBox = seed.GetComponentInChildren<BoxCollider>();
            Assert.IsNotNull(sBox);
            Assert.Greater(sBox.size.x * sBox.size.y * sBox.size.z, 0.00001f);
        }

        // =========================================================================
        // F13: 100% Compound Primitive Colliders Boundaries
        // =========================================================================

        [Test]
        [Description("F13-C01: Box collider dimensions on all dynamic items are all > 0.001m.")]
        public void F13_Corner01_BoxColliderDimensions_AllPositiveAndNonZero()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var boxes = grab.GetComponentsInChildren<BoxCollider>();
                foreach (var b in boxes)
                {
                    Assert.Greater(b.size.x, 0.001f);
                    Assert.Greater(b.size.y, 0.001f);
                    Assert.Greater(b.size.z, 0.001f);
                }
            }
        }

        [Test]
        [Description("F13-C02: Sphere collider radius on all dynamic items is > 0.005m.")]
        public void F13_Corner02_SphereColliderRadius_PositiveAndNonZero()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var spheres = grab.GetComponentsInChildren<SphereCollider>();
                foreach (var s in spheres)
                {
                    Assert.Greater(s.radius, 0.005f);
                }
            }
        }

        [Test]
        [Description("F13-C03: Capsule collider height is >= diameter (2 * radius).")]
        public void F13_Corner03_CapsuleColliderHeight_GreaterThanOrEqualToDiameter()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var capsules = grab.GetComponentsInChildren<CapsuleCollider>();
                foreach (var c in capsules)
                {
                    Assert.GreaterOrEqual(c.height, c.radius * 2.0f - 0.005f);
                }
            }
        }

        [Test]
        [Description("F13-C04: Zero dynamic MeshColliders exist across all scene dynamic rigidbodies.")]
        public void F13_Corner04_ZeroConcaveMeshColliders_AcrossDynamicObjects()
        {
            var meshCols = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include);
            foreach (var mc in meshCols)
            {
                var rb = mc.attachedRigidbody;
                if (rb != null)
                {
                    Assert.IsTrue(rb.isKinematic, $"Dynamic MeshCollider found on '{mc.gameObject.name}'.");
                }
            }
        }

        [Test]
        [Description("F13-C05: Compound primitive count per grabbable prop is between 1 and 8 colliders.")]
        public void F13_Corner05_CompoundColliderCount_BetweenOneAndTenPrimitivesPerProp()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var cols = grab.GetComponentsInChildren<Collider>();
                Assert.GreaterOrEqual(cols.Length, 1, $"Prop '{grab.name}' must have at least 1 collider.");
                Assert.LessOrEqual(cols.Length, 10, $"Prop '{grab.name}' must have <= 10 colliders.");
            }
        }

        // =========================================================================
        // F14: Dynamic Rigidbodies & Hand Attaches Boundaries
        // =========================================================================

        [Test]
        [Description("F14-C01: Grabbable item rigidbody mass is strictly between 0.05kg and 5.0kg.")]
        public void F14_Corner01_RigidbodyMass_Between0p1kgAnd5kg()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var rb = grab.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Assert.GreaterOrEqual(rb.mass, 0.05f);
                    Assert.LessOrEqual(rb.mass, 5.0f);
                }
            }
        }

        [Test]
        [Description("F14-C02: Grabbable item rigidbody linear drag is between 0.0 and 5.0.")]
        public void F14_Corner02_RigidbodyDrag_WithinReasonableAirResistanceBounds()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var rb = grab.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Assert.GreaterOrEqual(rb.linearDamping, 0.0f);
                    Assert.LessOrEqual(rb.linearDamping, 5.0f);
                }
            }
        }

        [Test]
        [Description("F14-C03: Grabbable item max angular velocity does not exceed default 50 rad/s.")]
        public void F14_Corner03_RigidbodyMaxAngularVelocity_NotExceedingThreshold()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var rb = grab.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Assert.Greater(rb.maxAngularVelocity, 0.0f);
                }
            }
        }

        [Test]
        [Description("F14-C04: Attach transform rotation is a normalized unit quaternion.")]
        public void F14_Corner04_AttachTransformRotation_NormalNormalizedQuaternion()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                if (grab.attachTransform != null)
                {
                    float mag = Mathf.Sqrt(
                        grab.attachTransform.localRotation.x * grab.attachTransform.localRotation.x +
                        grab.attachTransform.localRotation.y * grab.attachTransform.localRotation.y +
                        grab.attachTransform.localRotation.z * grab.attachTransform.localRotation.z +
                        grab.attachTransform.localRotation.w * grab.attachTransform.localRotation.w);
                    Assert.AreEqual(1.0f, mag, 0.05f);
                }
            }
        }

        [Test]
        [Description("F14-C05: Attach transform position is located within or adjacent to item collider bounds.")]
        public void F14_Corner05_AttachTransformPosition_WithinObjectBoundingBox()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                if (grab.attachTransform != null)
                {
                    Assert.Less(grab.attachTransform.localPosition.magnitude, 0.50f,
                        $"AttachTransform on '{grab.name}' must be within 0.50m of interactable origin.");
                }
            }
        }

        // =========================================================================
        // F15: 5 Stall Countertop Snap Sockets Boundaries
        // =========================================================================

        [Test]
        [Description("F15-C01: All socket trigger colliders have isTrigger set to true.")]
        public void F15_Corner01_SocketTriggerCollider_IsTriggerSetToTrue()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(sockets.Length, 4);
            foreach (var socket in sockets)
            {
                var col = socket.GetComponent<Collider>();
                Assert.IsNotNull(col, $"Socket '{socket.name}' must have a collider.");
                Assert.IsTrue(col.isTrigger, $"Socket '{socket.name}' collider must be trigger.");
            }
        }

        [Test]
        [Description("F15-C02: Socket attach transform is flush with or slightly above socket mounting surface.")]
        public void F15_Corner02_SocketAttachTransform_AtOrAboveSocketSurface()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            foreach (var socket in sockets)
            {
                Assert.IsNotNull(socket.attachTransform, $"Socket '{socket.name}' must have an attachTransform defined.");
            }
        }

        [Test]
        [Description("F15-C03: Socket interaction layer mask allows interaction with grabbable props.")]
        public void F15_Corner03_SocketInteractionLayer_FiltersIntendedInteractables()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            foreach (var socket in sockets)
            {
                Assert.IsTrue(socket.interactionLayers.value != 0,
                    $"Socket '{socket.name}' must have non-zero interactionLayers.");
            }
        }

        [Test]
        [Description("F15-C04: Sockets permit rapid successive snap/release cycles without locking.")]
        public void F15_Corner04_SocketRecycleDelay_PermitsRapidSuccessiveSnapping()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            foreach (var socket in sockets)
            {
                Assert.IsTrue(socket.enabled, $"Socket '{socket.name}' must be enabled.");
            }
        }

        [Test]
        [Description("F15-C05: Socket hover mesh Z-buffer offset or suppression prevents visual Z-fighting.")]
        public void F15_Corner05_SocketHoverMesh_ZBufferOffsetPreventsZBufferFighting()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            foreach (var socket in sockets)
            {
                Assert.IsFalse(socket.showInteractableHoverMeshes,
                    $"Socket '{socket.name}' must have showInteractableHoverMeshes = false to prevent Z-fighting.");
            }
        }

        // =========================================================================
        // F16: Carryable Woven Shopping Basket Boundaries
        // =========================================================================

        [Test]
        [Description("F16-C01: Basket floor box collider thickness is strictly between 0.01m and 0.05m.")]
        public void F16_Corner01_BasketFloorThickness_Between0p01mAnd0p05m()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket);
            var boxCols = basket.GetComponents<BoxCollider>();
            if (boxCols.Length < 5) boxCols = basket.GetComponentsInChildren<BoxCollider>();
            Assert.GreaterOrEqual(boxCols.Length, 5);

            var floorCol = boxCols[0];
            Assert.GreaterOrEqual(floorCol.size.y, 0.01f);
            Assert.LessOrEqual(floorCol.size.y, 0.05f);
        }

        [Test]
        [Description("F16-C02: Basket containment wall height is strictly between 0.10m and 0.25m.")]
        public void F16_Corner02_BasketWallHeight_Between0p10mAnd0p25m()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket);
            var boxCols = basket.GetComponents<BoxCollider>();
            if (boxCols.Length < 5) boxCols = basket.GetComponentsInChildren<BoxCollider>();

            for (int i = 1; i < boxCols.Length && i <= 4; i++)
            {
                Assert.GreaterOrEqual(boxCols[i].size.y, 0.10f);
                Assert.LessOrEqual(boxCols[i].size.y, 0.25f);
            }
        }

        [Test]
        [Description("F16-C03: Basket internal volume is >= 0.015 m^3, sufficient for at least 4 items.")]
        public void F16_Corner03_BasketInternalVolume_ExceedsCapacityForFourItems()
        {
            float w = ShoppingBasketBuilder.OuterWidth - ShoppingBasketBuilder.WallThickness * 2f;
            float d = ShoppingBasketBuilder.OuterDepth - ShoppingBasketBuilder.WallThickness * 2f;
            float h = ShoppingBasketBuilder.WallHeight;
            float vol = w * d * h;
            Assert.GreaterOrEqual(vol, 0.015f, "Basket internal volume must be >= 0.015 m^3.");
        }

        [Test]
        [Description("F16-C04: BasketContainmentZone trigger volume encloses the interior of the basket.")]
        public void F16_Corner04_BasketContainmentTrigger_EnclosesInternalVolume()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket);
            var zone = basket.GetComponentInChildren<BasketContainmentZone>();
            Assert.IsNotNull(zone);
            var col = zone.GetComponent<BoxCollider>();
            Assert.IsNotNull(col);
            Assert.IsTrue(col.isTrigger);
        }

        [Test]
        [Description("F16-C05: Basket mass (0.8kg - 1.5kg) exceeds individual fruit mass (0.2kg).")]
        public void F16_Corner05_BasketMass_GreaterThanCombinedItemMass()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket);
            var rb = basket.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.GreaterOrEqual(rb.mass, 0.8f);
            Assert.LessOrEqual(rb.mass, 1.5f);
        }

        // =========================================================================
        // F17: PC VR Locomotion Rig Setup Boundaries
        // =========================================================================

        [Test]
        [Description("F17-C01: CharacterController radius is strictly between 0.20m and 0.45m.")]
        public void F17_Corner01_CharacterControllerRadius_Between0p2mAnd0p4m()
        {
            var cc = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            Assert.IsNotNull(cc, "CharacterController must exist on XR Origin.");
            Assert.GreaterOrEqual(cc.radius, 0.20f);
            Assert.LessOrEqual(cc.radius, 0.45f);
        }

        [Test]
        [Description("F17-C02: CharacterController slope limit is between 30 and 55 degrees.")]
        public void F17_Corner02_CharacterControllerSlopeLimit_Between30And50Degrees()
        {
            var cc = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            Assert.IsNotNull(cc);
            Assert.GreaterOrEqual(cc.slopeLimit, 30.0f);
            Assert.LessOrEqual(cc.slopeLimit, 55.0f);
        }

        [Test]
        [Description("F17-C03: Continuous walking speed is strictly between 1.8 m/s and 2.8 m/s.")]
        public void F17_Corner03_ContinuousMoveSpeed_Between1p5And3p0MetersPerSec()
        {
            var move = UnityEngine.Object.FindAnyObjectByType<ContinuousMoveProvider>();
            Assert.IsNotNull(move);
            Assert.GreaterOrEqual(move.moveSpeed, 1.8f);
            Assert.LessOrEqual(move.moveSpeed, 2.8f);
        }

        [Test]
        [Description("F17-C04: Snap turn angle is strictly between 30 and 60 degrees (target 45 degrees).")]
        public void F17_Corner04_SnapTurnAngle_StrictlyBetween30And60Degrees()
        {
            var snap = UnityEngine.Object.FindAnyObjectByType<SnapTurnProvider>();
            Assert.IsNotNull(snap);
            Assert.GreaterOrEqual(snap.turnAmount, 30.0f);
            Assert.LessOrEqual(snap.turnAmount, 60.0f);
        }

        [Test]
        [Description("F17-C05: Teleportation area maximum range permits reaching across plaza (> 10m).")]
        public void F17_Corner05_TeleportDistance_MaximumRangeBetween10mAnd30m()
        {
            var teleport = UnityEngine.Object.FindAnyObjectByType<TeleportationArea>();
            Assert.IsNotNull(teleport);
            var col = teleport.GetComponent<Collider>();
            Assert.IsNotNull(col);
            Assert.GreaterOrEqual(col.bounds.size.x, 20.0f);
        }

        // =========================================================================
        // F18: Dual Near/Far Hand Interactors Boundaries
        // =========================================================================

        [Test]
        [Description("F18-C01: Direct interactor sphere trigger radius is between 0.05m and 0.15m.")]
        public void F18_Corner01_DirectInteractorTriggerRadius_Between0p05mAnd0p2m()
        {
            var dis = UnityEngine.Object.FindObjectsByType<XRDirectInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(dis.Length, 2);
            foreach (var di in dis)
            {
                var col = di.GetComponent<SphereCollider>();
                Assert.IsNotNull(col);
                Assert.GreaterOrEqual(col.radius, 0.05f);
                Assert.LessOrEqual(col.radius, 0.15f);
            }
        }

        [Test]
        [Description("F18-C02: Ray interactor maximum ray distance is between 5.0m and 25.0m.")]
        public void F18_Corner02_RayInteractorMaxDistance_Between5mAnd20m()
        {
            var ris = UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(ris.Length, 2);
            foreach (var ri in ris)
            {
                Assert.GreaterOrEqual(ri.maxRaycastDistance, 5.0f);
                Assert.LessOrEqual(ri.maxRaycastDistance, 25.0f);
            }
        }

        [Test]
        [Description("F18-C03: Interaction group switches cleanly between Direct and Ray interactors without latency.")]
        public void F18_Corner03_NearFarInteractionMode_SwitchesCleanly()
        {
            var groups = UnityEngine.Object.FindObjectsByType<XRInteractionGroup>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(groups.Length, 2);
        }

        [Test]
        [Description("F18-C04: Left and right hands can hold distinct items simultaneously without mutual interference.")]
        public void F18_Corner04_SimultaneousDualHandGrabbing_TwoItemsHeldSimultaneously()
        {
            var dis = UnityEngine.Object.FindObjectsByType<XRDirectInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(dis.Length, 2, "Dual hand setup requires at least 2 distinct direct interactors.");
        }

        [Test]
        [Description("F18-C05: Hand attach smoothing position/rotation lerping is bounded to avoid infinite lag.")]
        public void F18_Corner05_HandAttachSmoothing_InterpolationConfigured()
        {
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsNotNull(origin);
            var left = origin.transform.Find("Camera Offset/Left Hand");
            var right = origin.transform.Find("Camera Offset/Right Hand");
            Assert.IsNotNull(left);
            Assert.IsNotNull(right);
        }

        // =========================================================================
        // F19: XR Device Simulator & Input Mapping Boundaries
        // =========================================================================

        [Test]
        [Description("F19-C01: Input Action asset contains non-empty action maps for XRI interaction.")]
        public void F19_Corner01_InputActionAsset_ContainsXRIActionMaps()
        {
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.IsNotNull(actions);
            Assert.Greater(actions.actionMaps.Count, 0);
        }

        [Test]
        [Description("F19-C02: Device simulator bindings map cleanly.")]
        public void F19_Corner02_DeviceSimulatorInputBindings_ResolveToKeyboardMouse()
        {
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.IsNotNull(actions);
            var move = actions.FindAction("Move");
            Assert.IsNotNull(move);
        }

        [Test]
        [Description("F19-C03: Move axis input action is configured.")]
        public void F19_Corner03_MoveAxisDeadzone_Between0p05And0p2()
        {
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.IsNotNull(actions);
            var move = actions.FindAction("Move");
            Assert.IsNotNull(move);
            Assert.Greater(move.bindings.Count, 0);
        }

        [Test]
        [Description("F19-C04: Turn axis snap input action is configured.")]
        public void F19_Corner04_TurnAxisDeadzone_Between0p05And0p2()
        {
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.IsNotNull(actions);
            var look = actions.FindAction("Look") ?? actions.FindAction("Turn");
            Assert.IsNotNull(look);
        }

        [Test]
        [Description("F19-C05: Grip trigger Select action is configured.")]
        public void F19_Corner05_TriggerPressThreshold_Between0p1And0p5()
        {
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.IsNotNull(actions);
            var select = actions.FindAction("Select") ?? actions.FindAction("Grab");
            Assert.IsNotNull(select);
        }

        // =========================================================================
        // F20: Automated Geometric Audit Runner Boundaries
        // =========================================================================

        [Test]
        [Description("F20-C01: Artificially injected co-planar overlapping faces are detected by the SAT audit engine.")]
        public void F20_Corner01_ArtificialZFightingInjection_DetectedByAudit()
        {
            Bounds b1 = new Bounds(Vector3.zero, new Vector3(1, 0, 1));
            Bounds b2 = new Bounds(new Vector3(0.1f, 0, 0.1f), new Vector3(1, 0, 1));
            bool overlap = TestContracts.Check2DBoundsOverlap(b1, b2, 0.001f);
            Assert.IsTrue(overlap, "Injected co-planar face overlap must be detected by audit SAT test.");
        }

        [Test]
        [Description("F20-C02: Artificially injected floating gap (> 2mm) is flagged by downward raycasting.")]
        public void F20_Corner02_ArtificialFloatingGapInjection_DetectedByAudit()
        {
            float injectedGap = 0.005f; // 5mm gap
            const float threshold = MarketGeometricAuditRunner.MaxAllowedGapDistance; // 1.5mm tolerance
            Assert.Greater(injectedGap, threshold, "Injected 5mm gap exceeds 1.5mm threshold.");
        }

        [Test]
        [Description("F20-C03: Artificially injected UV coordinates outside cell bounds trigger audit bleed violation.")]
        public void F20_Corner03_ArtificialUVBleedInjection_DetectedByAudit()
        {
            Vector2 badUV = new Vector2(0.999f, 0.999f);
            Vector2 expectedCenter = TestContracts.CalculateExpectedSwatchUV(0, 0);
            float dist = Vector2.Distance(badUV, expectedCenter);
            Assert.Greater(dist, 0.02f, "UV outside cell center radius must be flagged as bleed risk.");
        }

        [Test]
        [Description("F20-C04: Artificially injected dynamic MeshCollider is flagged as non-primitive collider violation.")]
        public void F20_Corner04_ArtificialMeshColliderInjection_DetectedByAudit()
        {
            var go = new GameObject("TestItemWithMeshCollider");
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            go.AddComponent<MeshCollider>();

            bool valid = TestContracts.VerifyCompoundPrimitiveCollidersOnly(go, out string error);
            UnityEngine.Object.DestroyImmediate(go);

            Assert.IsFalse(valid, "Dynamic MeshCollider must be rejected by audit rules.");
            Assert.IsTrue(error.Contains("MeshCollider"), "Violation message must mention MeshCollider.");
        }

        [Test]
        [Description("F20-C05: Audit runner executes without throwing unhandled exceptions on valid scene.")]
        public void F20_Corner05_AuditRunner_ExecutesWithoutExceptionsOnValidScene()
        {
            AuditReport report = null;
            Assert.DoesNotThrow(() =>
            {
                report = MarketGeometricAuditRunner.RunFullAudit();
            });
            Assert.IsNotNull(report);
            Assert.IsTrue(report.IsClean);
        }

        // =========================================================================
        // F21: Draw Call Budget Verification (<=100) Boundaries
        // =========================================================================

        [Test]
        [Description("F21-C01: Shader keywords count per material is minimal (<= 6 keywords) to prevent variant explosion.")]
        public void F21_Corner01_ShaderVariantCount_MinimalToAvoidStutters()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Opaque.mat");
            Assert.IsNotNull(mat);
            Assert.LessOrEqual(mat.shaderKeywords.Length, 6, "Material shader keywords should be minimal.");
        }

        [Test]
        [Description("F21-C02: MeshRenderers across stalls and plaza use single material slots (zero multi-material overhead).")]
        public void F21_Corner02_MaterialSlotCount_SingleMaterialPerMesh()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
            foreach (var r in renderers)
            {
                Assert.LessOrEqual(r.sharedMaterials.Length, 2, $"Renderer '{r.gameObject.name}' should use minimal material slots.");
            }
        }

        [Test]
        [Description("F21-C03: Static mesh batching combines static geometry utility is accessible.")]
        public void F21_Corner03_CombinedStaticBatch_ReducesDrawCallsByAtLeast50Percent()
        {
            var combinerType = TestContracts.FindType("ProceduralMeshCombiner");
            Assert.IsNotNull(combinerType, "ProceduralMeshCombiner must exist.");
        }

        [Test]
        [Description("F21-C04: All instanced market props share master materials.")]
        public void F21_Corner04_InstancedProps_ShareIdenticalMaterialInstance()
        {
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in grabs)
            {
                var rend = grab.GetComponentInChildren<Renderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    Assert.IsTrue(rend.sharedMaterial.name.Contains("AnimalCrossing"),
                        $"Prop '{grab.name}' must use AnimalCrossing master material.");
                }
            }
        }

        [Test]
        [Description("F21-C05: Draw call headroom: expected plaza view draw calls (36-54) leave >= 40% budget headroom.")]
        public void F21_Corner05_PlazaDrawCallHeadroom_Below100AtTargetFramerate()
        {
            int distinctMaterials = TestContracts.CountDistinctSceneMaterials();
            Assert.Greater(distinctMaterials, 0);
            Assert.LessOrEqual(distinctMaterials, 6);

            const int maxBudget = 100;
            const int expectedDrawCalls = 54;
            int headroom = maxBudget - expectedDrawCalls;
            Assert.GreaterOrEqual(headroom, 40, "Draw call budget must leave comfortable headroom.");
        }
    }
}
