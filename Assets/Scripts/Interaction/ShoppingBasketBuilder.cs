// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShoppingBasketBuilder.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Procedural builder for the carryable woven shopping basket (Feature F16).
//   Constructs beveled woven basin, curved carry handle, 5-box containment colliders,
//   XRGrabInteractable, and BasketContainmentZone dynamic stabilization.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using UnityEngine;
using AnimalCrossing.TechnicalArt;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimalCrossing.Interaction
{
    public static class ShoppingBasketBuilder
    {
        public const string OpaqueMatPath = "Assets/Materials/M_AnimalCrossing_Opaque.mat";

        public const float OuterWidth = 0.46f;
        public const float OuterDepth = 0.34f;
        public const float BasinHeight = 0.185f;
        public const float WallThickness = 0.03f;
        public const float FloorThickness = 0.025f; // F16-C01: 0.01m to 0.05m
        public const float WallHeight = 0.16f;      // F16-C02: 0.12m to 0.25m
        public const float BasketMass = 1.2f;       // F16-C05: 0.8kg to 1.5kg

        private static Material _cachedOpaqueMat;
        public static Material OpaqueMaterial
        {
            get
            {
                if (_cachedOpaqueMat == null)
                {
#if UNITY_EDITOR
                    _cachedOpaqueMat = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMatPath);
#endif
                }
                return _cachedOpaqueMat;
            }
            set => _cachedOpaqueMat = value;
        }

        public static GameObject BuildShoppingBasket(Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
        {
            Vector3 pos = position ?? new Vector3(2.6f, 0.012f, 0.0f);
            Quaternion rot = rotation ?? Quaternion.identity;
            Material mat = OpaqueMaterial;

            GameObject root = new GameObject("ShoppingBasket");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = rot;

            // Set layer if defined
            int basketLayer = LayerMask.NameToLayer("Basket");
            if (basketLayer >= 0) root.layer = basketLayer;

            // 1. Dynamic Rigidbody
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = BasketMass;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.0f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // 2. Composite PhysicsMaterial
            PhysicsMaterial basketPhysMat = new PhysicsMaterial("PM_BasketContainment")
            {
                dynamicFriction = 0.7f,
                staticFriction = 0.8f,
                bounciness = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            // 3. Five Composite Containment BoxColliders (Floor + 4 Walls)
            // Floor Collider (Thickness = 0.025m)
            var colFloor = root.AddComponent<BoxCollider>();
            colFloor.center = new Vector3(0f, FloorThickness * 0.5f, 0f);
            colFloor.size = new Vector3(OuterWidth, FloorThickness, OuterDepth);
            colFloor.material = basketPhysMat;

            // Front Wall (+Z)
            var colFront = root.AddComponent<BoxCollider>();
            colFront.center = new Vector3(0f, FloorThickness + WallHeight * 0.5f, OuterDepth * 0.5f - WallThickness * 0.5f);
            colFront.size = new Vector3(OuterWidth, WallHeight, WallThickness);
            colFront.material = basketPhysMat;

            // Back Wall (-Z)
            var colBack = root.AddComponent<BoxCollider>();
            colBack.center = new Vector3(0f, FloorThickness + WallHeight * 0.5f, -OuterDepth * 0.5f + WallThickness * 0.5f);
            colBack.size = new Vector3(OuterWidth, WallHeight, WallThickness);
            colBack.material = basketPhysMat;

            // Left Wall (-X)
            float innerDepth = OuterDepth - WallThickness * 2f;
            var colLeft = root.AddComponent<BoxCollider>();
            colLeft.center = new Vector3(-OuterWidth * 0.5f + WallThickness * 0.5f, FloorThickness + WallHeight * 0.5f, 0f);
            colLeft.size = new Vector3(WallThickness, WallHeight, innerDepth);
            colLeft.material = basketPhysMat;

            // Right Wall (+X)
            var colRight = root.AddComponent<BoxCollider>();
            colRight.center = new Vector3(OuterWidth * 0.5f - WallThickness * 0.5f, FloorThickness + WallHeight * 0.5f, 0f);
            colRight.size = new Vector3(WallThickness, WallHeight, innerDepth);
            colRight.material = basketPhysMat;

            // 4. Handle Collider (6th BoxCollider, for grabbing)
            GameObject handleColObj = new GameObject("HandleCollider");
            handleColObj.transform.SetParent(root.transform, false);
            if (basketLayer >= 0) handleColObj.layer = basketLayer;
            var colHandle = handleColObj.AddComponent<BoxCollider>();
            colHandle.center = new Vector3(0f, 0.37f, 0f);
            colHandle.size = new Vector3(0.18f, 0.04f, 0.04f);
            colHandle.material = basketPhysMat;

            // 5. Procedural Visual Meshes
            BuildVisualGeometry(root.transform, mat);

            // 6. Handle Attach Transform
            GameObject attachObj = new GameObject("HandleAttachTransform");
            attachObj.transform.SetParent(root.transform, false);
            attachObj.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            attachObj.transform.localRotation = Quaternion.identity;

            // 7. XRGrabInteractable
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.attachTransform = attachObj.transform;
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.throwVelocityScale = 1.5f;
            grab.throwAngularVelocityScale = 1.0f;

            // 8. Containment Zone Dynamic Trigger
            GameObject zoneObj = new GameObject("ContainmentZone");
            zoneObj.transform.SetParent(root.transform, false);
            if (basketLayer >= 0) zoneObj.layer = basketLayer;
            var zoneTrigger = zoneObj.AddComponent<BoxCollider>();
            zoneTrigger.isTrigger = true;
            zoneTrigger.center = new Vector3(0f, FloorThickness + WallHeight * 0.5f, 0f);
            zoneTrigger.size = new Vector3(OuterWidth - WallThickness * 2f - 0.01f, WallHeight - 0.005f, OuterDepth - WallThickness * 2f - 0.01f);
            zoneObj.AddComponent<BasketContainmentZone>();

            // 9. CharacterController Collision Decoupling
            IgnoreCharacterControllerCollisions(root);

            return root;
        }

        private static void BuildVisualGeometry(Transform root, Material mat)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root, false);

            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh basketMesh = GenerateWovenBasketMesh();
            mf.sharedMesh = basketMesh;
        }

        public static Mesh GenerateWovenBasketMesh()
        {
            var buffer = new ProceduralMeshBuilder.MeshBuffer();

            Vector2 uvReed = ProceduralMeshBuilder.GetSwatchUV(Swatches.WovenBasketLightReed.Col, Swatches.WovenBasketLightReed.Row);
            Vector2 uvShadow = ProceduralMeshBuilder.GetSwatchUV(Swatches.WovenBasketWeaveShadow.Col, Swatches.WovenBasketWeaveShadow.Row);
            Vector2 uvTwine = ProceduralMeshBuilder.GetSwatchUV(Swatches.JuteTwineCord.Col, Swatches.JuteTwineCord.Row);
            Vector2 uvRim = ProceduralMeshBuilder.GetSwatchUV(Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);
            Vector2 uvHandle = ProceduralMeshBuilder.GetSwatchUV(Swatches.WoodBirchWarm.Col, Swatches.WoodBirchWarm.Row);

            float hw = OuterWidth * 0.5f;
            float hd = OuterDepth * 0.5f;
            float ft = FloorThickness;
            float wh = WallHeight;
            float wt = WallThickness;

            // Floor Bottom (-Y) and Floor Interior (+Y)
            buffer.AddQuad(new Vector3(-hw, 0f, hd), new Vector3(hw, 0f, hd), new Vector3(hw, 0f, -hd), new Vector3(-hw, 0f, -hd), Vector3.down, uvShadow);
            buffer.AddQuad(new Vector3(-hw + wt, ft, -hd + wt), new Vector3(hw - wt, ft, -hd + wt), new Vector3(hw - wt, ft, hd - wt), new Vector3(-hw + wt, ft, hd - wt), Vector3.up, uvReed);

            // 4 Woven Bands on Outer Walls
            int bands = 4;
            float bandH = wh / bands;
            for (int b = 0; b < bands; b++)
            {
                float y0 = ft + b * bandH;
                float y1 = y0 + bandH;
                Vector2 bandUV = (b % 2 == 0) ? uvReed : uvShadow;

                // Front (+Z)
                buffer.AddQuad(new Vector3(-hw, y0, hd), new Vector3(hw, y0, hd), new Vector3(hw, y1, hd), new Vector3(-hw, y1, hd), Vector3.forward, bandUV);
                // Back (-Z)
                buffer.AddQuad(new Vector3(hw, y0, -hd), new Vector3(-hw, y0, -hd), new Vector3(-hw, y1, -hd), new Vector3(hw, y1, -hd), Vector3.back, bandUV);
                // Left (-X)
                buffer.AddQuad(new Vector3(-hw, y0, -hd), new Vector3(-hw, y0, hd), new Vector3(-hw, y1, hd), new Vector3(-hw, y1, -hd), Vector3.left, bandUV);
                // Right (+X)
                buffer.AddQuad(new Vector3(hw, y0, hd), new Vector3(hw, y0, -hd), new Vector3(hw, y1, -hd), new Vector3(hw, y1, hd), Vector3.right, bandUV);
            }

            // Inner Wall Surfaces
            float inY0 = ft;
            float inY1 = ft + wh;
            float inHW = hw - wt;
            float inHD = hd - wt;
            buffer.AddQuad(new Vector3(inHW, inY0, inHD), new Vector3(-inHW, inY0, inHD), new Vector3(-inHW, inY1, inHD), new Vector3(inHW, inY1, inHD), Vector3.back, uvShadow);
            buffer.AddQuad(new Vector3(-inHW, inY0, -inHD), new Vector3(inHW, inY0, -inHD), new Vector3(inHW, inY1, -inHD), new Vector3(-inHW, inY1, -inHD), Vector3.forward, uvShadow);
            buffer.AddQuad(new Vector3(-inHW, inY0, inHD), new Vector3(-inHW, inY0, -inHD), new Vector3(-inHW, inY1, -inHD), new Vector3(-inHW, inY1, inHD), Vector3.right, uvShadow);
            buffer.AddQuad(new Vector3(inHW, inY0, -inHD), new Vector3(inHW, inY0, inHD), new Vector3(inHW, inY1, inHD), new Vector3(inHW, inY1, -inHD), Vector3.left, uvShadow);

            // Beveled Crown Rim
            float rimY = ft + wh;
            float rimThick = 0.015f;
            buffer.AddQuad(new Vector3(-hw, rimY, -hd), new Vector3(hw, rimY, -hd), new Vector3(hw, rimY + rimThick, -hd), new Vector3(-hw, rimY + rimThick, -hd), Vector3.up, uvRim);
            buffer.AddQuad(new Vector3(-hw, rimY + rimThick, -hd), new Vector3(hw, rimY + rimThick, -hd), new Vector3(hw, rimY + rimThick, hd), new Vector3(-hw, rimY + rimThick, hd), Vector3.up, uvRim);

            // Curved Tubular Carry Handle Arch (Sweep across X from -0.21 to +0.21)
            int arcSegments = 16;
            int tubeSegments = 8;
            float arcRadius = 0.21f;
            float tubeRadius = 0.013f;
            float handleCenterY = rimY + 0.01f;

            for (int i = 0; i < arcSegments; i++)
            {
                float t0 = (float)i / arcSegments;
                float t1 = (float)(i + 1) / arcSegments;
                float angle0 = Mathf.PI * (1f - t0);
                float angle1 = Mathf.PI * (1f - t1);

                Vector3 p0 = new Vector3(Mathf.Cos(angle0) * arcRadius, handleCenterY + Mathf.Sin(angle0) * arcRadius, 0f);
                Vector3 p1 = new Vector3(Mathf.Cos(angle1) * arcRadius, handleCenterY + Mathf.Sin(angle1) * arcRadius, 0f);
                Vector3 dir = (p1 - p0).normalized;
                Vector3 up = Vector3.Cross(dir, Vector3.forward).normalized;

                Vector2 handleUV = (i >= arcSegments * 3 / 8 && i <= arcSegments * 5 / 8) ? uvTwine : uvHandle;

                for (int j = 0; j < tubeSegments; j++)
                {
                    float u0 = (float)j / tubeSegments * Mathf.PI * 2f;
                    float u1 = (float)(j + 1) / tubeSegments * Mathf.PI * 2f;

                    Vector3 n0 = (Mathf.Cos(u0) * Vector3.forward + Mathf.Sin(u0) * up).normalized;
                    Vector3 n1 = (Mathf.Cos(u1) * Vector3.forward + Mathf.Sin(u1) * up).normalized;

                    Vector3 v00 = p0 + n0 * tubeRadius;
                    Vector3 v01 = p0 + n1 * tubeRadius;
                    Vector3 v11 = p1 + n1 * tubeRadius;
                    Vector3 v10 = p1 + n0 * tubeRadius;

                    buffer.AddQuad(v00, v01, v11, v10, n0, n1, n1, n0, handleUV);
                }
            }

            return buffer.Build("ProceduralWovenShoppingBasket");
        }

        public static void IgnoreCharacterControllerCollisions(GameObject basket)
        {
            int basketLayer = LayerMask.NameToLayer("Basket");
            int playerLayer = LayerMask.NameToLayer("Player");
            if (basketLayer >= 0 && playerLayer >= 0)
            {
                Physics.IgnoreLayerCollision(basketLayer, playerLayer, true);
            }

            var controllers = UnityEngine.Object.FindObjectsByType<CharacterController>();
            var basketCols = basket.GetComponentsInChildren<Collider>();
            foreach (var cc in controllers)
            {
                foreach (var col in basketCols)
                {
                    Physics.IgnoreCollision(col, cc, true);
                }
            }
        }

#if UNITY_EDITOR
        [MenuItem("AnimalCrossing/Build Shopping Basket", priority = 31)]
        public static void MenuItem_BuildShoppingBasket()
        {
            var existing = GameObject.Find("ShoppingBasket");
            if (existing != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(existing);
                else
                    UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject basket = BuildShoppingBasket();
            Selection.activeGameObject = basket;
            EditorUtility.SetDirty(basket);
            Debug.Log("[ShoppingBasketBuilder] Shopping Basket successfully created in active scene.");
        }
#endif
    }
}
