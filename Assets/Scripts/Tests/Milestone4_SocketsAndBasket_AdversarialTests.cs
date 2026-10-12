// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Milestone4_SocketsAndBasket_AdversarialTests.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Empirical Adversarial Stress Test Suite for Milestone 4 (Sockets & Shopping Basket - Features F15, F16).
//   Constructed by Challenger gen2_m4_challenger_2 to rigorously stress-test:
//   - Sockets: Verify all 5 countertop snap sockets cleanly snap items with showInteractableHoverMeshes = false and valid trigger volumes.
//   - Basket: Verify composite 5-box containment colliders and carry handle collider.
//   - Basket Dynamic Containment: Empirically stress-test dynamic item retention, compound collider de-registration,
//     minY floor collision fight, and wall climbing escape under locomotion.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AnimalCrossing.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnimalCrossing.Tests
{
    [TestFixture]
    [Description("Empirical adversarial stress harness for Milestone 4: Sockets and Shopping Basket (F15, F16).")]
    public class Milestone4_SocketsAndBasket_AdversarialTests
    {
        // =========================================================================
        // Feature F15: Countertop Snap Sockets Challenges
        // =========================================================================

        [Test]
        [Description("AdvM4-01: Verifies all 5 required countertop snap sockets exist with showInteractableHoverMeshes = false and valid trigger volumes.")]
        public void AdvM4_01_Sockets_AllFiveExistWithHoverMeshesFalseAndValidTriggers()
        {
            string[] requiredSockets = new string[]
            {
                "FruitTraySocket",
                "TurnipScaleSocket",
                "CoffeeSaucerSocket",
                "PastryBoardSocket",
                "PottingMatSocket"
            };

            var allSockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            var socketMap = new Dictionary<string, XRSocketInteractor>();
            foreach (var s in allSockets)
            {
                socketMap[s.name] = s;
            }

            foreach (var req in requiredSockets)
            {
                Assert.IsTrue(socketMap.ContainsKey(req), $"Required socket '{req}' must exist in the scene.");
                var socket = socketMap[req];

                // Hover meshes must be false to prevent co-planar Z-fighting
                Assert.IsFalse(socket.showInteractableHoverMeshes,
                    $"Socket '{req}' must have showInteractableHoverMeshes = false to prevent Z-fighting.");

                // Trigger collider must be a SphereCollider with isTrigger = true
                var col = socket.GetComponent<SphereCollider>();
                Assert.IsNotNull(col, $"Socket '{req}' must have a SphereCollider component.");
                Assert.IsTrue(col.isTrigger, $"Socket '{req}' SphereCollider must have isTrigger = true.");
                Assert.GreaterOrEqual(col.radius, 0.10f, $"Socket '{req}' trigger radius must be >= 0.10m.");
                Assert.LessOrEqual(col.radius, 0.25f, $"Socket '{req}' trigger radius must be <= 0.25m.");

                // AttachTransform must exist
                Assert.IsNotNull(socket.attachTransform, $"Socket '{req}' must have an attachTransform defined.");

                // Recycle delay must be rapid (< 1s, >= 0.1s)
                Assert.GreaterOrEqual(socket.recycleDelayTime, 0.1f, $"Socket '{req}' recycleDelayTime must be >= 0.1s.");
                Assert.LessOrEqual(socket.recycleDelayTime, 1.0f, $"Socket '{req}' recycleDelayTime must be <= 1.0s.");
            }
        }

        [Test]
        [Description("AdvM4-02: Verifies all 5 countertop sockets have valid interaction layers accepting grabbable market props.")]
        public void AdvM4_02_Sockets_AcceptAllGrabbableProps()
        {
            var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include);
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude);

            Assert.GreaterOrEqual(sockets.Length, 5, "At least 5 sockets must exist.");

            foreach (var s in sockets)
            {
                int acceptableProps = 0;
                foreach (var g in grabs)
                {
                    if (g.name.Contains("Basket")) continue;
                    if (s.CanHover((IXRHoverInteractable)g) && s.CanSelect((IXRSelectInteractable)g))
                    {
                        acceptableProps++;
                    }
                }
                Assert.GreaterOrEqual(acceptableProps, 8,
                    $"Socket '{s.name}' must accept grabbable market props through its interaction layer mask.");
            }
        }

        [Test]
        [Description("AdvM4-03: Verifies that pre-socketed props in the scene are positioned within their socket trigger volumes.")]
        public void AdvM4_03_Sockets_ScenePreSocketedItemsInsideTriggers()
        {
            var pairs = new (string SocketPath, string ItemPath)[]
            {
                ("MarketStalls/ProduceStall/Sockets/FruitTraySocket", "MarketStalls/ProduceStall/Apple"),
                ("MarketStalls/ProduceStall/Sockets/TurnipScaleSocket", "MarketStalls/ProduceStall/Turnip"),
                ("MarketStalls/CafeStall/Sockets/CoffeeSaucerSocket", "MarketStalls/CafeStall/CoffeeMug"),
                ("MarketStalls/CafeStall/Sockets/PastryBoardSocket", "MarketStalls/CafeStall/Donut"),
                ("MarketStalls/CraftingStall/Sockets/PottingMat/PottingMatSocket", "MarketStalls/CraftingStall/PottedSapling")
            };

            foreach (var p in pairs)
            {
                var sGo = GameObject.Find(p.SocketPath);
                var iGo = GameObject.Find(p.ItemPath);

                Assert.IsNotNull(sGo, $"Socket at '{p.SocketPath}' must exist.");
                Assert.IsNotNull(iGo, $"Item at '{p.ItemPath}' must exist.");

                var trigCol = sGo.GetComponent<SphereCollider>();
                Vector3 trigCenter = sGo.transform.TransformPoint(trigCol.center);
                float trigRadius = trigCol.radius * sGo.transform.lossyScale.x;

                var itemColliders = iGo.GetComponentsInChildren<Collider>();
                bool inside = false;
                foreach (var col in itemColliders)
                {
                    if (col.isTrigger) continue;
                    Vector3 closest = col.ClosestPoint(trigCenter);
                    if (Vector3.Distance(closest, trigCenter) <= trigRadius)
                    {
                        inside = true;
                        break;
                    }
                }

                Assert.IsTrue(inside, $"Pre-socketed prop '{iGo.name}' must be inside trigger volume of '{sGo.name}'.");
            }
        }

        // =========================================================================
        // Feature F16: Shopping Basket Challenges
        // =========================================================================

        [Test]
        [Description("AdvM4-04: Verifies basket has composite 5-box containment colliders + separate handle grab collider.")]
        public void AdvM4_04_Basket_CompositeFiveBoxContainmentAndHandleColliders()
        {
            var basket = GameObject.Find("ShoppingBasket");
            Assert.IsNotNull(basket, "ShoppingBasket must exist in scene.");

            var colliders = basket.GetComponentsInChildren<BoxCollider>();
            Assert.GreaterOrEqual(colliders.Length, 6,
                "ShoppingBasket requires at least 6 BoxColliders (1 floor + 4 walls + 1 handle).");

            // Verify floor collider
            bool foundFloor = false;
            int wallCount = 0;
            bool foundHandle = false;

            foreach (var bc in colliders)
            {
                if (bc.isTrigger) continue; // skip containment zone trigger

                if (bc.transform == basket.transform)
                {
                    if (bc.size.y <= 0.05f && bc.size.x > 0.30f && bc.size.z > 0.20f)
                    {
                        foundFloor = true;
                        Assert.GreaterOrEqual(bc.size.y, 0.01f, "Floor thickness must be >= 0.01m.");
                        Assert.LessOrEqual(bc.size.y, 0.05f, "Floor thickness must be <= 0.05m.");
                    }
                    else if (bc.size.y >= 0.12f && bc.size.y <= 0.25f)
                    {
                        wallCount++;
                    }
                }
                else if (bc.transform.name.Contains("Handle"))
                {
                    foundHandle = true;
                }
            }

            Assert.IsTrue(foundFloor, "Basket must have a containment floor BoxCollider.");
            Assert.AreEqual(4, wallCount, "Basket root must have 4 side wall BoxColliders.");
            Assert.IsTrue(foundHandle, "Basket must have a child HandleCollider BoxCollider.");
        }

        [Test]
        [Description("AdvM4-05: Stress-tests compound collider items in BasketContainmentZone to ensure partial collider exit does NOT drop the item.")]
        public void AdvM4_05_Basket_CompoundCollider_DoesNotDeregisterOnPartialExit()
        {
            var basketGo = ShoppingBasketBuilder.BuildShoppingBasket(null, Vector3.zero, Quaternion.identity);
            var zone = basketGo.GetComponentInChildren<BasketContainmentZone>();
            zone.Awake();
            zone.Start();

            // Create compound item with 2 colliders (body + stem)
            var itemGo = new GameObject("TestCompoundItem");
            var itemRb = itemGo.AddComponent<Rigidbody>();
            itemRb.mass = 0.2f;

            var bodyCol = itemGo.AddComponent<SphereCollider>();
            bodyCol.radius = 0.04f;
            bodyCol.center = Vector3.zero;

            var stemColObj = new GameObject("Stem");
            stemColObj.transform.SetParent(itemGo.transform, false);
            stemColObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            var stemCol = stemColObj.AddComponent<BoxCollider>();
            stemCol.size = new Vector3(0.01f, 0.04f, 0.01f);

            itemGo.transform.position = new Vector3(0f, 0.06f, 0f);

            // Both colliders enter zone
            zone.OnTriggerEnter(bodyCol);
            zone.OnTriggerEnter(stemCol);

            // Simulate stem collider poking out and exiting trigger
            zone.OnTriggerExit(stemCol);

            // Now move basket by teleport jump of 2.0m
            basketGo.transform.position += new Vector3(2.0f, 0f, 0f);
            zone.FixedUpdate();

            float distanceAfterTeleport = Vector3.Distance(itemGo.transform.position, basketGo.transform.position);

            UnityEngine.Object.DestroyImmediate(basketGo);
            UnityEngine.Object.DestroyImmediate(itemGo);

            Assert.Less(distanceAfterTeleport, 0.5f,
                "Compound collider item must NOT be dropped by BasketContainmentZone when a child collider exits while body remains inside.");
        }

        [Test]
        [Description("AdvM4-06: Verifies that BasketContainmentZone minY clamp does NOT exceed resting height of items on basket floor.")]
        public void AdvM4_06_Basket_MinYClampingDoesNotExceedFloorRestingHeight()
        {
            var basketGo = ShoppingBasketBuilder.BuildShoppingBasket(null, Vector3.zero, Quaternion.identity);
            var zone = basketGo.GetComponentInChildren<BasketContainmentZone>();
            var zoneCol = zone.GetComponent<BoxCollider>();

            Vector3 extents = zoneCol.size * 0.5f;
            float minY = zoneCol.center.y - extents.y + 0.02f;
            float floorTopY = ShoppingBasketBuilder.FloorThickness; // 0.025m

            // Small props (Donut half-thickness 0.017m, small Peach) have COM at floorTopY + 0.015m ~ 0.040m
            float restingSmallItemComY = floorTopY + 0.015f;

            UnityEngine.Object.DestroyImmediate(basketGo);

            Assert.LessOrEqual(minY, restingSmallItemComY,
                $"BasketContainmentZone minY ({minY:F4}m) must be <= resting COM height ({restingSmallItemComY:F4}m) to prevent vertical collision trampoline fighting.");
        }

        [Test]
        [Description("AdvM4-07: Empirically stress-tests multiple items in shopping basket under 2.4 m/s locomotion to verify zero escape.")]
        public void AdvM4_07_Basket_DynamicLocomotionContainmentRetainsItems()
        {
            var prevMode = Physics.simulationMode;
            GameObject testRoot = null;

            try
            {
                Physics.simulationMode = SimulationMode.Script;

                testRoot = new GameObject("TestRoot_LocomotionContainment");
                testRoot.transform.position = new Vector3(60f, 1f, 60f);

                var basketGo = ShoppingBasketBuilder.BuildShoppingBasket(testRoot.transform, testRoot.transform.position, Quaternion.identity);
                var basketRb = basketGo.GetComponent<Rigidbody>();
                basketRb.useGravity = false;
                basketRb.isKinematic = false;

                var zone = basketGo.GetComponentInChildren<BasketContainmentZone>();
                zone.Awake();
                zone.Start();

                var itemRbs = new List<Rigidbody>();
                Vector3[] offsets = new Vector3[]
                {
                    new Vector3(-0.06f, 0.06f, -0.05f),
                    new Vector3( 0.06f, 0.06f, -0.05f),
                    new Vector3(-0.06f, 0.06f,  0.05f),
                    new Vector3( 0.06f, 0.06f,  0.05f)
                };

                for (int i = 0; i < 4; i++)
                {
                    var itemGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    itemGo.name = "Fruit_" + i;
                    itemGo.transform.SetParent(testRoot.transform, false);
                    itemGo.transform.localScale = Vector3.one * 0.07f;
                    itemGo.transform.position = basketGo.transform.TransformPoint(offsets[i]);

                    var rb = itemGo.AddComponent<Rigidbody>();
                    rb.mass = 0.2f;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;

                    itemRbs.Add(rb);
                    zone.OnTriggerEnter(itemGo.GetComponent<Collider>());
                }

                Physics.SyncTransforms();

                // Initial step
                zone.FixedUpdate();
                Physics.Simulate(0.02f);

                Vector3 walkVelocity = new Vector3(2.4f, 0f, 0f);
                basketRb.linearVelocity = walkVelocity;
                float dt = 0.02f;
                int escapeCount = 0;

                for (int step = 0; step < 80; step++)
                {
                    basketGo.transform.position += walkVelocity * dt;
                    zone.FixedUpdate();
                    Physics.Simulate(dt);

                    foreach (var rb in itemRbs)
                    {
                        Vector3 localPos = basketGo.transform.InverseTransformPoint(rb.position);
                        if (localPos.y > 0.18f || Mathf.Abs(localPos.x) > 0.25f || Mathf.Abs(localPos.z) > 0.19f)
                        {
                            escapeCount++;
                            break;
                        }
                    }

                    if (escapeCount > 0) break;
                }

                Assert.AreEqual(0, escapeCount,
                    "Items must remain stably contained inside the shopping basket basin during 2.4 m/s locomotion without escaping or climbing over walls.");
            }
            finally
            {
                Physics.simulationMode = prevMode;
                if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }
    }
}
