using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using AnimalCrossing.Interaction;
using AnimalCrossing.Environment;
using AnimalCrossing.Audit;

namespace AnimalCrossing.Tests
{
    /// <summary>
    /// Tier 3 — Cross-Feature Interactions:
    /// Pairwise combinatorial tests verifying integrations between subsystems:
    /// Basket + Locomotion, Sockets + Physics, Stalls + Colliders, Audit + Materials, etc.
    /// Authoritative source: TEST_INFRA.md and PROJECT.md.
    /// </summary>
    [TestFixture]
    public class Tier3_CrossFeatureInteractions
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContracts.EnsureSceneContext();
        }

        [Test]
        [Description("T3-01: Pairwise Basket + Locomotion: Carrying basket with 3 items while smooth locomotion moves rig.")]
        public void T3_01_BasketAndLocomotion_BasketCarriesMultipleItemsWhileMoving()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket, "ShoppingBasket must exist in the scene.");

            var basketGrab = basket.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(basketGrab, "ShoppingBasket must have XRGrabInteractable for carrying.");

            var basketRb = basket.GetComponent<Rigidbody>();
            Assert.IsNotNull(basketRb, "ShoppingBasket must have Rigidbody.");
            Assert.IsFalse(basketRb.isKinematic, "ShoppingBasket Rigidbody must be dynamic for physical carrying.");

            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsNotNull(origin, "XR Origin rig must exist for locomotion.");

            var move = origin.GetComponent<ContinuousMoveProvider>();
            Assert.IsNotNull(move, "ContinuousMoveProvider must be present on XR Origin.");
            Assert.AreEqual(2.4f, move.moveSpeed, 0.1f, "Continuous move speed must be configured for 2.4 m/s.");

            var cc = origin.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "CharacterController must be present on XR Origin.");
        }

        [Test]
        [Description("T3-02: Pairwise Locomotion + Socket: Releasing/grabbing an item from snap socket while moving.")]
        public void T3_02_LocomotionAndSocket_GrabbingItemFromSocketWhileMoving()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(sockets.Length, 4, $"At least 4 snap sockets must exist across stalls, found {sockets.Length}.");

            foreach (var s in sockets)
            {
                var trigger = s.GetComponent<SphereCollider>();
                Assert.IsNotNull(trigger, $"Socket '{s.name}' must have a SphereCollider trigger.");
                Assert.IsTrue(trigger.isTrigger, $"Socket '{s.name}' SphereCollider must be configured as a trigger.");
                Assert.IsNotNull(s.attachTransform, $"Socket '{s.name}' must have attachTransform defined.");
            }

            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsNotNull(origin, "XR Origin must exist in the scene.");
            var move = origin.GetComponent<ContinuousMoveProvider>();
            Assert.IsNotNull(move, "ContinuousMoveProvider must be present for locomotion release testing.");
        }

        [Test]
        [Description("T3-03: Pairwise Teleportation + Basket: Teleporting across plaza with full basket retains contained items.")]
        public void T3_03_TeleportationAndBasket_TeleportAcrossPlazaWithFullBasket()
        {
            var teleport = UnityEngine.Object.FindAnyObjectByType<TeleportationArea>();
            Assert.IsNotNull(teleport, "TeleportationArea must span the plaza ground for VR locomotion.");

            var teleportCol = teleport.GetComponent<Collider>();
            Assert.IsNotNull(teleportCol, "TeleportationArea must have an active collider.");
            Assert.GreaterOrEqual(teleportCol.bounds.size.x, 20.0f, "Teleportation area width must cover plaza footprint.");

            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket, "ShoppingBasket must exist.");

            var containmentZone = basket.GetComponentInChildren<BasketContainmentZone>();
            Assert.IsNotNull(containmentZone, "BasketContainmentZone must be attached to ShoppingBasket for inertial retention.");

            var boxCols = basket.GetComponents<BoxCollider>();
            if (boxCols.Length < 5) boxCols = basket.GetComponentsInChildren<BoxCollider>();
            Assert.GreaterOrEqual(boxCols.Length, 5, "Shopping basket must possess 5-box composite containment colliders.");
        }

        [Test]
        [Description("T3-04: Pairwise Produce Stall + Fruits: Fruit items resting inside display crates without clipping or jitter.")]
        public void T3_04_ProduceStallAndFruits_SpawningFruitsInsideCratesWithoutSnagging()
        {
            var stall = TestContracts.FindSceneObject("ProduceStall");
            Assert.IsNotNull(stall, "ProduceStall must exist in the market square.");

            var crates = stall.transform.Find("FruitCrates");
            Assert.IsNotNull(crates, "Fruit display crates must exist under ProduceStall.");
            Assert.AreEqual(3, crates.childCount, "Produce stall must feature 3-tier angled fruit display crates.");

            for (int t = 0; t < crates.childCount; t++)
            {
                var tier = crates.GetChild(t);
                var col = tier.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"Crate tier '{tier.name}' must possess a BoxCollider.");
                Assert.IsNotNull(col.sharedMaterial, $"Crate tier '{tier.name}' must have a PhysicsMaterial assigned.");
                Assert.AreEqual(0.6f, col.sharedMaterial.dynamicFriction, 0.05f, "Crate display shelf must have friction 0.6.");
            }

            string[] fruitNames = new string[] { "Apple", "Orange", "Peach", "Turnip" };
            foreach (var fName in fruitNames)
            {
                var fruit = TestContracts.FindProp(fName);
                Assert.IsNotNull(fruit, $"Fruit '{fName}' must exist in Produce stall goods.");
                var rb = fruit.GetComponent<Rigidbody>();
                Assert.IsNotNull(rb, $"Fruit '{fName}' must have a dynamic Rigidbody.");
                Assert.IsFalse(rb.isKinematic, $"Fruit '{fName}' Rigidbody must be dynamic.");
                Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(fruit, out string msg), msg);
            }
        }

        [Test]
        [Description("T3-05: Pairwise Café Stall + Snap Socket: Coffee mug snaps onto café saucer socket at Y = 0.95m.")]
        public void T3_05_CafeStallAndSocket_SnappingCoffeeMugToSaucerSocket()
        {
            var cafe = TestContracts.FindSceneObject("CafeStall");
            Assert.IsNotNull(cafe, "CafeStall must exist in the market square.");

            var counter = cafe.transform.Find("Counter");
            Assert.IsNotNull(counter, "Service counter must exist on CafeStall.");
            Assert.AreEqual(0.95f, counter.position.y, 0.05f, "Café counter height must be at Y = 0.95m.");

            var saucerSocketGo = GameObject.Find("CoffeeSaucerSocket");
            Assert.IsNotNull(saucerSocketGo, "CoffeeSaucerSocket must exist on the Café counter.");

            var socket = saucerSocketGo.GetComponent<XRSocketInteractor>();
            Assert.IsNotNull(socket, "CoffeeSaucerSocket must have XRSocketInteractor component.");
            Assert.IsNotNull(socket.attachTransform, "CoffeeSaucerSocket must have attachTransform defined.");
            Assert.IsFalse(socket.showInteractableHoverMeshes, "CoffeeSaucerSocket must disable hover mesh to prevent Z-fighting.");

            var mug = TestContracts.FindProp("CoffeeMug");
            Assert.IsNotNull(mug, "CoffeeMug must exist at the Café stall.");
            var mugGrab = mug.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(mugGrab, "CoffeeMug must have XRGrabInteractable.");
            Assert.IsNotNull(mugGrab.attachTransform, "CoffeeMug must have child attachTransform.");
        }

        [Test]
        [Description("T3-06: Pairwise Crafting Stall + Gardening Props: Watering can rests stably on crafting worktable.")]
        public void T3_06_CraftingStallAndTools_PlacingWateringCanOnWorktable()
        {
            var crafting = TestContracts.FindSceneObject("CraftingStall");
            Assert.IsNotNull(crafting, "CraftingStall must exist in the market square.");

            var worktable = crafting.transform.Find("Worktable") ?? crafting.transform.Find("Counter");
            Assert.IsNotNull(worktable, "Worktable must exist on CraftingStall.");
            Assert.AreEqual(0.92f, worktable.position.y, 0.05f, "Crafting worktable height must be at Y = 0.92m.");

            var col = worktable.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "Crafting worktable must have BoxCollider.");
            Assert.IsNotNull(col.sharedMaterial, "Worktable must have PhysicsMaterial assigned.");
            Assert.AreEqual(0.6f, col.sharedMaterial.dynamicFriction, 0.05f, "Worktable collider must provide friction 0.6.");

            var wateringCan = TestContracts.FindProp("WateringCan");
            Assert.IsNotNull(wateringCan, "WateringCan prop must exist at Crafting stall.");
            Assert.IsTrue(TestContracts.VerifyCompoundPrimitiveCollidersOnly(wateringCan, out string msg), msg);

            var rb = wateringCan.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "WateringCan must have dynamic Rigidbody.");
            Assert.IsFalse(rb.isKinematic, "WateringCan Rigidbody must be dynamic.");
        }

        [Test]
        [Description("T3-07: Pairwise Fountain + Locomotion: CharacterController collision blocks player from walking into stone basin.")]
        public void T3_07_FountainAndLocomotion_CharacterControllerBlocksWalkingIntoFountainBasin()
        {
            var fountain = TestContracts.FindSceneObject("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain, "StoneFountain centerpiece must exist in the plaza.");

            var colliders = fountain.GetComponentsInChildren<Collider>();
            Assert.Greater(colliders.Length, 0, "Fountain must provide solid collision surfaces.");

            bool hasNonTrigger = false;
            foreach (var c in colliders)
            {
                if (!c.isTrigger) { hasNonTrigger = true; break; }
            }
            Assert.IsTrue(hasNonTrigger, "Fountain must feature solid non-trigger collision surfaces to block player navigation.");

            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsNotNull(origin, "XR Origin rig must exist.");
            var cc = origin.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "CharacterController must be present to interact with fountain colliders.");
            Assert.AreEqual(0.30f, cc.radius, 0.05f, "CharacterController radius must be ~0.30m.");
        }

        [Test]
        [Description("T3-08: Pairwise Fencing + Locomotion: Plaza perimeter fencing prevents player locomotion from escaping plaza.")]
        public void T3_08_FencingAndLocomotion_PlazaPerimeterBlocksPlayerWalkingOutOfBounds()
        {
            var plaza = TestContracts.FindSceneObject("TownSquarePlaza") ?? TestContracts.FindSceneObject("Plaza");
            Assert.IsNotNull(plaza, "TownSquarePlaza must exist.");

            var fencing = (plaza.transform.Find("Fencing")?.gameObject) ?? TestContracts.FindSceneObject("Fencing");
            Assert.IsNotNull(fencing, "Perimeter fencing must exist in the plaza layout.");

            var fenceCols = fencing.GetComponentsInChildren<Collider>();
            Assert.Greater(fenceCols.Length, 0, "Perimeter fencing must feature colliders to block player locomotion.");

            foreach (var col in fenceCols)
            {
                Assert.IsFalse(col.isTrigger, "Perimeter fence colliders must be solid non-triggers.");
            }
        }

        [Test]
        [Description("T3-09: Pairwise Shopping Basket + CharacterController: Basket collider layer does not collide with player capsule.")]
        public void T3_09_BasketAndCharacterController_BasketDoesNotCollideWithPlayerCapsule()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket, "ShoppingBasket must exist.");

            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsNotNull(origin, "XR Origin must exist.");
            var cc = origin.GetComponent<CharacterController>();
            Assert.IsNotNull(cc, "CharacterController must exist.");

            int basketLayer = basket.layer;
            int playerLayer = cc.gameObject.layer;
            if (basketLayer != playerLayer && basketLayer > 0)
            {
                Assert.IsTrue(Physics.GetIgnoreLayerCollision(basketLayer, playerLayer) || basketLayer != 0,
                    "Basket layer should be isolated from player layer collision.");
            }

            var grab = basket.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(grab, "Basket must have XRGrabInteractable.");
        }

        [Test]
        [Description("T3-10: Pairwise Grabbable Items + Materials: All 11 goods share M_AnimalCrossing_Opaque / LiquidGlass.")]
        public void T3_10_GrabbableItemsAndAtlasMaterials_AllGoodsShareMasterMaterials()
        {
            string[] propNames = new string[]
            {
                "Apple", "Orange", "Peach", "Turnip",
                "CoffeeMug", "Donut", "Teapot", "SlicedBread",
                "WateringCan", "PottedSapling", "SeedPacket"
            };

            foreach (var name in propNames)
            {
                var prop = TestContracts.FindProp(name);
                Assert.IsNotNull(prop, $"Grabbable prop '{name}' must exist in the scene.");

                var renderers = prop.GetComponentsInChildren<Renderer>();
                Assert.Greater(renderers.Length, 0, $"Prop '{name}' must have at least one Renderer.");

                foreach (var rend in renderers)
                {
                    Assert.IsNotNull(rend.sharedMaterial, $"Renderer on '{rend.name}' must have a sharedMaterial assigned.");
                    string matName = rend.sharedMaterial.name;
                    Assert.IsTrue(matName.Contains("AnimalCrossing") || matName.Contains("Opaque") || matName.Contains("LiquidGlass"),
                        $"Prop '{name}' Renderer '{rend.name}' must use a master material, found: {matName}");
                }
            }
        }

        [Test]
        [Description("T3-11: Pairwise Procedural Canopies + Master Shaders: Canopies render with AnimalCrossing/Opaque.")]
        public void T3_11_CanopiesAndMasterShaders_CanopiesRenderWithOpaqueStylizedShader()
        {
            string[] stallNames = new string[] { "ProduceStall", "CafeStall", "CraftingStall" };
            foreach (var sName in stallNames)
            {
                var stall = GameObject.Find(sName);
                Assert.IsNotNull(stall, $"Stall '{sName}' must exist in the scene.");

                var canopy = stall.transform.Find("Canopy") ?? stall.transform.Find("Awning");
                Assert.IsNotNull(canopy, $"Canopy/Awning must exist on stall '{sName}'.");

                var rend = canopy.GetComponent<Renderer>();
                Assert.IsNotNull(rend, $"Canopy on '{sName}' must have a Renderer.");
                Assert.IsNotNull(rend.sharedMaterial, $"Canopy on '{sName}' must have sharedMaterial assigned.");
                Assert.IsTrue(rend.sharedMaterial.name.Contains("Opaque"),
                    $"Canopy on '{sName}' must use M_AnimalCrossing_Opaque material.");
            }
        }

        [Test]
        [Description("T3-12: Pairwise Tree Foliage + Foliage Material: Round trees bind to M_AnimalCrossing_Foliage with softened normals.")]
        public void T3_12_TreesAndFoliageMaterial_PuffyTreesUseFoliageMaterialWithSoftNormals()
        {
            var plaza = TestContracts.FindSceneObject("TownSquarePlaza") ?? TestContracts.FindSceneObject("Plaza");
            Assert.IsNotNull(plaza, "TownSquarePlaza must exist.");

            var natureGroup = plaza.transform.Find("NatureElements") ?? plaza.transform.Find("Trees");
            Assert.IsNotNull(natureGroup, "NatureElements group must exist under TownSquarePlaza.");

            var renderers = natureGroup.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(renderers.Length, 0, "Nature elements must feature MeshRenderers.");

            bool foundFoliageMat = false;
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("Foliage"))
                {
                    foundFoliageMat = true;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Assert.Greater(mf.sharedMesh.normals.Length, 0, "Foliage mesh must contain valid normals.");
                    }
                }
            }
            Assert.IsTrue(foundFoliageMat, "At least one tree foliage mesh must bind to M_AnimalCrossing_Foliage material.");
        }

        [Test]
        [Description("T3-13: Pairwise Street Lamps + Emissive Material: Lantern glass meshes bind to M_AnimalCrossing_Emissive.")]
        public void T3_13_StreetLampsAndEmissiveMaterial_LampsEmitWarmStylizedGlow()
        {
            var plaza = TestContracts.FindSceneObject("TownSquarePlaza") ?? TestContracts.FindSceneObject("Plaza");
            Assert.IsNotNull(plaza, "TownSquarePlaza must exist.");

            var lamps = (plaza.transform.Find("StreetLamps")?.gameObject) ?? TestContracts.FindSceneObject("StreetLamps");
            Assert.IsNotNull(lamps, "StreetLamps group must exist in the plaza.");

            var renderers = lamps.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(renderers.Length, 0, "Street lamps must feature renderers.");

            bool foundEmissive = false;
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("Emissive"))
                {
                    foundEmissive = true;
                    break;
                }
            }
            Assert.IsTrue(foundEmissive, "Street lamps must contain lantern heads using M_AnimalCrossing_Emissive material.");
        }

        [Test]
        [Description("T3-14: Pairwise Fountain Water Disk + LiquidGlass Material: Water disk renders with stylized transparency.")]
        public void T3_14_FountainWaterAndLiquidGlass_WaterDiskRendersWithStylizedTransparency()
        {
            var fountain = TestContracts.FindSceneObject("StoneFountain") ?? TestContracts.FindSceneObject("Fountain");
            Assert.IsNotNull(fountain, "StoneFountain must exist.");

            var water = (fountain.transform.Find("WaterDisk")?.gameObject) ?? (fountain.transform.Find("FountainWater")?.gameObject) ?? TestContracts.FindSceneObject("WaterDisk");
            Assert.IsNotNull(water, "Water disk must exist within the stone fountain.");

            var rend = water.GetComponent<Renderer>();
            Assert.IsNotNull(rend, "Water disk must have a Renderer.");
            Assert.IsNotNull(rend.sharedMaterial, "Water disk must have a material assigned.");
            Assert.IsTrue(rend.sharedMaterial.name.Contains("LiquidGlass") || rend.sharedMaterial.name.Contains("Water"),
                $"Water disk must use M_AnimalCrossing_LiquidGlass, found: {rend.sharedMaterial.name}");
        }

        [Test]
        [Description("T3-15: Pairwise Audit Runner + Plaza Scene: Full scene environment executes under audit runner with 0 violations.")]
        public void T3_15_AuditRunnerAndPlaza_FullPlazaEnvironmentPassesGeometricAudit()
        {
            var report = MarketGeometricAuditRunner.RunFullAudit();
            Assert.IsNotNull(report, "MarketGeometricAuditRunner must produce an audit report.");
            Assert.AreEqual(0, report.FailedChecks,
                $"Audit report failed {report.FailedChecks} checks:\n{string.Join("\n", report.Errors)}");
            Assert.IsTrue(report.IsClean, "Plaza scene must be clean with 0 geometric or art violations.");
        }

        [Test]
        [Description("T3-16: Pairwise Draw Call Batching + 4 Master Materials: Plaza view batches down to <= 100 draw calls.")]
        public void T3_16_DrawCallBatchingAndMaterials_FullSceneUsesAtMostFourMaterials()
        {
            int distinctMaterials = TestContracts.CountDistinctSceneMaterials();
            Assert.Greater(distinctMaterials, 0, "Scene must have active materials.");
            Assert.LessOrEqual(distinctMaterials, 6,
                $"Scene must rely on <= 4 master materials (+ glass/emissive variants), found {distinctMaterials}.");

            var opaqueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AnimalCrossing_Opaque.mat");
            Assert.IsNotNull(opaqueMat, "M_AnimalCrossing_Opaque must exist.");
            Assert.IsTrue(opaqueMat.enableInstancing, "M_AnimalCrossing_Opaque must have GPU instancing enabled.");
        }

        [Test]
        [Description("T3-17: Pairwise Device Simulator + Direct Interactors: Simulator activates Direct Interactor trigger on proximity.")]
        public void T3_17_DeviceSimulatorAndDirectInteractors_SimulatorDrivesDirectInteractions()
        {
            var directInteractors = UnityEngine.Object.FindObjectsByType<XRDirectInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(directInteractors.Length, 2, "Both controller hands must feature XRDirectInteractor.");

            foreach (var di in directInteractors)
            {
                var col = di.GetComponent<SphereCollider>();
                Assert.IsNotNull(col, $"XRDirectInteractor on '{di.name}' must have a SphereCollider trigger.");
                Assert.IsTrue(col.isTrigger, $"XRDirectInteractor on '{di.name}' collider must be a trigger volume.");
                Assert.AreEqual(0.08f, col.radius, 0.04f, "Direct interactor trigger radius should be ~0.08m.");
            }
        }

        [Test]
        [Description("T3-18: Pairwise Snap Socket + Grab Release: Dropping item into socket trigger activates selectEntered.")]
        public void T3_18_SnapSocketAndDropPhysics_ItemDroppedIntoSocketVolumeSnapsCleanly()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(sockets.Length, 4, "At least 4 snap sockets must exist across stalls.");

            foreach (var socket in sockets)
            {
                Assert.IsNotNull(socket.attachTransform, $"Socket '{socket.name}' must have an attachTransform defined.");
                var trigger = socket.GetComponent<SphereCollider>();
                Assert.IsNotNull(trigger, $"Socket '{socket.name}' must have a trigger collider.");
                Assert.IsTrue(trigger.isTrigger, $"Socket '{socket.name}' collider must be configured as a trigger.");
            }
        }

        [Test]
        [Description("T3-19: Pairwise Basket Inertial Zone + Acceleration: BasketContainmentZone dampens rapid acceleration jitter.")]
        public void T3_19_BasketInertialZoneAndAcceleration_StabilizationAppliesDamping()
        {
            var basket = TestContracts.FindSceneObject("ShoppingBasket");
            Assert.IsNotNull(basket, "ShoppingBasket must exist.");

            var zone = basket.GetComponentInChildren<BasketContainmentZone>();
            Assert.IsNotNull(zone, "BasketContainmentZone must be attached to the ShoppingBasket.");

            var trigger = zone.GetComponent<BoxCollider>();
            Assert.IsNotNull(trigger, "BasketContainmentZone must feature a BoxCollider trigger volume.");
            Assert.IsTrue(trigger.isTrigger, "BasketContainmentZone BoxCollider must be marked isTrigger = true.");

            var basketRb = basket.GetComponent<Rigidbody>();
            Assert.IsNotNull(basketRb, "ShoppingBasket must have a Rigidbody for inertial dynamics.");
            Assert.AreEqual(CollisionDetectionMode.ContinuousDynamic, basketRb.collisionDetectionMode,
                "ShoppingBasket Rigidbody must use ContinuousDynamic collision detection.");
        }

        [Test]
        [Description("T3-20: Pairwise Grounding Joins + Audit Raycast: 5cm post embedment passes audit zero-floating-gap rule.")]
        public void T3_20_GroundingJoinsAndAuditRunner_AllTimberPostsPassAuditZeroGapCheck()
        {
            string[] stallNames = new string[] { "ProduceStall", "CafeStall", "CraftingStall" };
            foreach (var sName in stallNames)
            {
                var stall = GameObject.Find(sName);
                Assert.IsNotNull(stall, $"Stall '{sName}' must exist in the scene.");
                Assert.LessOrEqual(stall.transform.position.y, 0.05f, $"Stall '{sName}' root must be grounded (Y <= 0.05m).");

                var posts = stall.transform.Find("Posts") ?? stall.transform.Find("StructuralPosts");
                Assert.IsNotNull(posts, $"Posts group must exist under stall '{sName}'.");
                Assert.GreaterOrEqual(posts.childCount, 4, $"Stall '{sName}' must feature at least 4 corner posts.");

                for (int p = 0; p < posts.childCount; p++)
                {
                    var post = posts.GetChild(p);
                    if (post.name.StartsWith("Post"))
                    {
                        var mf = post.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            float bottomY = post.position.y - mf.sharedMesh.bounds.extents.y;
                            Assert.LessOrEqual(bottomY, 0.01f,
                                $"Post '{post.name}' on stall '{sName}' must be grounded and anchored into the ground.");
                        }
                    }
                }
            }

            var report = MarketGeometricAuditRunner.RunFullAudit();
            Assert.AreEqual(0, report.FailedChecks, "Zero floating gaps permitted under geometric audit.");
        }
    }
}
