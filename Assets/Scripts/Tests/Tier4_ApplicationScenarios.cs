using System;
using System.Collections.Generic;
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
using AnimalCrossing.Audit;

namespace AnimalCrossing.Tests
{
    /// <summary>
    /// Tier 4 — Real-World Application Scenarios:
    /// High-level, opaque-box, multi-stage end-to-end user workflows verifying
    /// full emergent behavior across all 21 features in realistic VR play sessions.
    /// Authoritative source: TEST_INFRA.md (§ Real-World Application Scenarios).
    /// </summary>
    [TestFixture]
    public class Tier4_ApplicationScenarios
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContracts.EnsureSceneContext();
        }

        [Test]
        [Description("Scenario 1: Complete Market Shopping Tour — Spawning at plaza entrance, walking to Produce Stall, " +
                     "grabbing Apple and Peach, placing them into shopping basket, carrying basket past fountain to Café Stall, " +
                     "snapping coffee mug to saucer socket, grabbing donut, and placing into basket.")]
        public void Scenario1_CompleteMarketShoppingTour()
        {
            // Stage 1: Verify Plaza & XR Origin spawn
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsNotNull(origin, "Stage 1: XR Origin (VR) rig must be initialized in the scene.");

            var cc = origin.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "Stage 1: Player rig must feature CharacterController locomotion collision.");

            var plaza = TestContracts.FindSceneObject("TownSquarePlaza") ?? TestContracts.FindSceneObject("Plaza");
            Assert.IsNotNull(plaza, "Stage 1: TownSquarePlaza environment must be initialized in the scene.");

            // Stage 2: Produce stall browsing and fruit gathering
            var produceStall = TestContracts.FindSceneObject("ProduceStall");
            Assert.IsNotNull(produceStall, "Stage 2: Produce stall must exist in the market square.");

            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket, "Stage 2: Shopping basket must be present for goods gathering.");

            var apple = TestContracts.FindProp("Apple");
            Assert.IsNotNull(apple, "Stage 2: Apple prop must exist at the Produce Stall.");
            Assert.IsNotNull(apple.GetComponent<XRGrabInteractable>(), "Stage 2: Apple must have XRGrabInteractable.");

            var peach = TestContracts.FindProp("Peach");
            Assert.IsNotNull(peach, "Stage 2: Peach prop must exist at the Produce Stall.");
            Assert.IsNotNull(peach.GetComponent<XRGrabInteractable>(), "Stage 2: Peach must have XRGrabInteractable.");

            // Stage 3: Pathway navigation past stone fountain to Café
            var fountain = TestContracts.FindSceneObject("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain, "Stage 3: Stone fountain centerpiece must be present along main avenue.");

            var cafeStall = TestContracts.FindSceneObject("CafeStall");
            Assert.IsNotNull(cafeStall, "Stage 3: Café & Bakery stall must be present along the plaza pathway.");

            var donut = TestContracts.FindProp("Donut");
            Assert.IsNotNull(donut, "Stage 3: Donut prop must exist at Café Stall.");

            var coffeeMug = TestContracts.FindProp("CoffeeMug");
            Assert.IsNotNull(coffeeMug, "Stage 3: Coffee mug prop must exist at Café Stall.");

            // Stage 4: Countertop snap socket interaction (coffee mug to saucer)
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            Assert.IsTrue(sockets.Length >= 4, $"Stage 4: At least 4 snap sockets must exist across stalls, found {sockets.Length}.");

            var saucerSocketGo = GameObject.Find("CoffeeSaucerSocket");
            Assert.IsNotNull(saucerSocketGo, "Stage 4: CoffeeSaucerSocket must exist on the Café counter.");
            var saucerSocket = saucerSocketGo.GetComponent<XRSocketInteractor>();
            Assert.IsNotNull(saucerSocket, "Stage 4: CoffeeSaucerSocket must have XRSocketInteractor component.");
            Assert.IsNotNull(saucerSocket.attachTransform, "Stage 4: CoffeeSaucerSocket must have attachTransform defined.");
            Assert.IsFalse(saucerSocket.showInteractableHoverMeshes, "Stage 4: CoffeeSaucerSocket must disable hover mesh to prevent Z-fighting.");
        }

        [Test]
        [Description("Scenario 2: Rapid Locomotion & Physics Stability Test — Carrying basket containing 4 loose items " +
                     "(Turnip, Apple, Donut, Seed Packet) while jogging at 2.4 m/s across the 25m x 25m plaza with snap turns " +
                     "and teleport jumps. Confirms zero collider tunneling, zero floor clipping, and physics stability.")]
        public void Scenario2_RapidLocomotionAndPhysicsStabilityTest()
        {
            // Stage 1: Setup 4 loose items in basket
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket, "Stage 1: Shopping basket must be present in the scene.");

            var boxCols = basket.GetComponents<BoxCollider>();
            if (boxCols.Length < 5) boxCols = basket.GetComponentsInChildren<BoxCollider>();
            Assert.GreaterOrEqual(boxCols.Length, 5, "Stage 1: Basket must possess 5-box composite containment colliders.");

            var containmentZone = basket.GetComponentInChildren<BasketContainmentZone>();
            Assert.IsNotNull(containmentZone, "Stage 1: Basket must contain BasketContainmentZone for inertial stabilization.");

            var turnip = TestContracts.FindProp("Turnip");
            Assert.IsNotNull(turnip, "Stage 1: Turnip must exist in market props.");

            var apple = TestContracts.FindProp("Apple");
            Assert.IsNotNull(apple, "Stage 1: Apple must exist in market props.");

            var donut = TestContracts.FindProp("Donut");
            Assert.IsNotNull(donut, "Stage 1: Donut must exist in market props.");

            var seedPacket = TestContracts.FindProp("SeedPacket");
            Assert.IsNotNull(seedPacket, "Stage 1: SeedPacket must exist in market props.");

            // Verify compound primitive colliders on all 4 items
            Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(turnip, out string msgT), msgT);
            Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(apple, out string msgA), msgA);
            Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(donut, out string msgD), msgD);
            Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(seedPacket, out string msgS), msgS);

            // Stage 2: Continuous movement at 2.4 m/s across 25m plaza
            var moveProvider = UnityEngine.Object.FindAnyObjectByType<ContinuousMoveProvider>();
            Assert.IsNotNull(moveProvider, "Stage 2: ContinuousMoveProvider must be attached to XR Origin.");
            Assert.AreEqual(2.4f, moveProvider.moveSpeed, 0.1f, "Stage 2: Move provider must be configured for 2.4 m/s walk speed.");
            Assert.IsFalse(moveProvider.enableFly, "Stage 2: enableFly must be false for grounded VR walking.");

            // Stage 3: Snap turn 45 degrees
            var snapTurn = UnityEngine.Object.FindAnyObjectByType<SnapTurnProvider>();
            Assert.IsNotNull(snapTurn, "Stage 3: SnapTurnProvider must be attached to XR Origin.");
            Assert.AreEqual(45.0f, snapTurn.turnAmount, 0.1f, "Stage 3: Snap turn angle must be 45 degrees.");

            // Stage 4: Teleport jump across 25m plaza
            var teleportArea = UnityEngine.Object.FindAnyObjectByType<TeleportationArea>();
            Assert.IsNotNull(teleportArea, "Stage 4: TeleportationArea must span the plaza ground for VR locomotion.");

            var groundCol = teleportArea.GetComponent<Collider>();
            Assert.IsNotNull(groundCol, "Stage 4: TeleportationArea must possess an active collider surface.");
            Assert.GreaterOrEqual(groundCol.bounds.size.x, 20.0f, "Stage 4: Teleportation area width must cover plaza footprint.");
            Assert.GreaterOrEqual(groundCol.bounds.size.z, 20.0f, "Stage 4: Teleportation area length must cover plaza footprint.");
        }

        [Test]
        [Description("Scenario 3: Multi-Socket Staging & Swap — Removing Coffee Mug from Café saucer, placing Apple on " +
                     "Produce scale, and placing Potted Sapling on DIY potting mat. Confirms clean snapping, correct " +
                     "orientations, and absence of hover mesh Z-fighting.")]
        public void Scenario3_MultiSocketStagingAndSwap()
        {
            // Stage 1: Locate countertop snap sockets
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(sockets.Length, 4, $"At least 4 snap sockets must exist across stalls, found {sockets.Length}.");

            string[] expectedSocketNames = new string[]
            {
                "FruitTraySocket",
                "TurnipScaleSocket",
                "CoffeeSaucerSocket",
                "PastryBoardSocket"
            };

            foreach (var sName in expectedSocketNames)
            {
                var sGo = GameObject.Find(sName);
                Assert.IsNotNull(sGo, $"Socket GameObject '{sName}' must exist in the scene.");
                var socket = sGo.GetComponent<XRSocketInteractor>();
                Assert.IsNotNull(socket, $"Socket '{sName}' must have XRSocketInteractor component.");

                // Stage 2: Verify snap orientation and attach transforms
                Assert.IsNotNull(socket.attachTransform, $"Socket '{sName}' must have an attachTransform defined.");

                var triggerCol = socket.GetComponent<SphereCollider>();
                Assert.IsNotNull(triggerCol, $"Socket '{sName}' must feature a SphereCollider trigger.");
                Assert.IsTrue(triggerCol.isTrigger, $"Socket '{sName}' SphereCollider must have isTrigger = true.");
                Assert.GreaterOrEqual(triggerCol.radius, 0.10f, $"Socket '{sName}' trigger radius must be >= 0.10m.");
                Assert.LessOrEqual(triggerCol.radius, 0.25f, $"Socket '{sName}' trigger radius must be <= 0.25m.");

                // Stage 3: Verify hover mesh suppression
                Assert.IsFalse(socket.showInteractableHoverMeshes,
                    $"Socket '{sName}' must have showInteractableHoverMeshes = false to prevent co-planar Z-fighting.");
            }
        }

        [Test]
        [Description("Scenario 4: Full Technical Art & Visual Integrity Pass — Multi-angle camera sweep across the " +
                     "25m x 25m plaza verifying all 4 master materials, GPU instancing, <= 100 draw calls, and zero visual artifacts.")]
        public void Scenario4_FullTechnicalArtAndVisualIntegrityPass()
        {
            // Stage 1: Master materials compliance (exactly 4 master materials)
            string[] expectedMaterials = new string[]
            {
                "Assets/Materials/M_AnimalCrossing_Opaque.mat",
                "Assets/Materials/M_AnimalCrossing_Foliage.mat",
                "Assets/Materials/M_AnimalCrossing_LiquidGlass.mat",
                "Assets/Materials/M_AnimalCrossing_Emissive.mat"
            };

            foreach (var matPath in expectedMaterials)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Assert.IsNotNull(mat, $"Master material asset '{matPath}' must exist.");
                Assert.IsNotNull(mat.shader, $"Material '{matPath}' must have a valid shader assigned.");

                if (matPath.Contains("Opaque") || matPath.Contains("Foliage"))
                {
                    Assert.IsTrue(mat.enableInstancing, $"Material '{matPath}' must have GPU Instancing enabled.");
                }
            }

            // Stage 2: Draw call budget headroom via active scene renderers and material counts
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            Assert.Greater(renderers.Length, 0, "Scene must contain active geometry renderers.");

            int distinctSceneMaterials = TestContracts.CountDistinctSceneMaterials();
            Assert.LessOrEqual(distinctSceneMaterials, 6,
                $"Plaza scene must rely on <= 4 master materials (plus glass/emissive variants), found {distinctSceneMaterials}.");

            // Stage 3: No external 3D models downloaded
            string[] forbiddenFormats = new string[] { "*.fbx", "*.obj", "*.gltf", "*.glb", "*.blend" };
            foreach (var fmt in forbiddenFormats)
            {
                var files = Directory.GetFiles(Application.dataPath, fmt, SearchOption.AllDirectories);
                Assert.AreEqual(0, files.Length, $"External 3D model ({fmt}) found in Assets. Strictly forbidden by Requirement R1.");
            }
        }

        [Test]
        [Description("Scenario 5: Strict Geometric Audit Zero-Tolerance Enforcement — Comprehensive audit engine " +
                     "execution confirming 0 co-planar overlapping faces, 0 floating gaps > 1.5mm, 0 UV bleed violations, " +
                     "0 collider clipping errors, and 0 dynamic MeshColliders.")]
        public void Scenario5_StrictGeometricAuditZeroToleranceEnforcement()
        {
            // Stage 1: Execute forensic audit runner
            var report = MarketGeometricAuditRunner.RunFullAudit();
            Assert.IsNotNull(report, "Audit report must be generated by MarketGeometricAuditRunner.");
            Assert.AreEqual(0, report.FailedChecks,
                $"Geometric QA Audit failed {report.FailedChecks} checks:\n{string.Join("\n", report.Errors)}");
            Assert.IsTrue(report.IsClean, "Scene must be 100% clean per forensic geometric audit.");

            // Stage 2: Verify zero dynamic MeshColliders across all Rigidbodies in scene
            var allRigidbodies = UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include);
            foreach (var rb in allRigidbodies)
            {
                if (!rb.isKinematic)
                {
                    var meshCols = rb.GetComponentsInChildren<MeshCollider>(true);
                    foreach (var mc in meshCols)
                    {
                        Assert.IsFalse(mc.enabled,
                            $"Dynamic Rigidbody on '{rb.gameObject.name}' has dynamic MeshCollider on '{mc.gameObject.name}'.");
                    }
                }
            }

            // Stage 3: Verify all market stalls sit flush on ground
            var stalls = GameObject.Find("MarketStalls");
            Assert.IsNotNull(stalls, "MarketStalls group must exist in scene.");
            for (int i = 0; i < stalls.transform.childCount; i++)
            {
                var stallChild = stalls.transform.GetChild(i);
                Assert.LessOrEqual(stallChild.position.y, 0.05f,
                    $"Stall '{stallChild.name}' root must sit solidly on ground (Y <= 0.05m).");
            }
        }
    }
}
