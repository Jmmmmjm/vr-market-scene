using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace AnimalCrossing.Tests
{
    /// <summary>
    /// Contract and oracle helpers for the Animal Crossing VR Village Market Scene E2E test suite.
    /// Provides authoritative reference computations, reflection invocation, and scene verification helpers.
    /// </summary>
    public static class TestContracts
    {
        private static readonly string[] SearchNamespaces = new string[]
        {
            "",
            "AnimalCrossing",
            "AnimalCrossing.TechnicalArt",
            "AnimalCrossing.Environment",
            "AnimalCrossing.Interaction",
            "AnimalCrossing.Locomotion",
            "AnimalCrossing.Audit"
        };

        /// <summary>
        /// Locates a type by name across loaded assemblies and candidate namespaces.
        /// </summary>
        public static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            // Direct check
            var directType = Type.GetType(typeName);
            if (directType != null) return directType;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                // Try exact match in assembly
                var t = asm.GetType(typeName);
                if (t != null) return t;

                // Try candidate namespaces
                foreach (var ns in SearchNamespaces)
                {
                    var full = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
                    t = asm.GetType(full);
                    if (t != null) return t;
                }

                // Try matching by simple name ignoring namespace
                try
                {
                    var types = asm.GetTypes();
                    var match = types.FirstOrDefault(x => x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
                catch
                {
                    // Ignore ReflectionTypeLoadException from third-party assemblies
                }
            }

            return null;
        }

        /// <summary>
        /// Invokes a public or internal static method on a target type.
        /// </summary>
        public static T InvokeStatic<T>(string typeName, string methodName, params object[] args)
        {
            var type = FindType(typeName);
            Assert.IsNotNull(type, $"Target type '{typeName}' was not found in loaded assemblies.");

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var method = type.GetMethod(methodName, flags);
            Assert.IsNotNull(method, $"Method '{methodName}' was not found on type '{typeName}'.");

            var result = method.Invoke(null, args);
            return (T)result;
        }

        /// <summary>
        /// Invokes an instance method on a target object.
        /// </summary>
        public static T InvokeMethod<T>(object instance, string methodName, params object[] args)
        {
            Assert.IsNotNull(instance, "Target instance cannot be null.");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var method = instance.GetType().GetMethod(methodName, flags);
            Assert.IsNotNull(method, $"Method '{methodName}' was not found on instance of type '{instance.GetType().Name}'.");

            var result = method.Invoke(instance, args);
            return (T)result;
        }

        /// <summary>
        /// Retrieves a property value from a target object.
        /// </summary>
        public static T GetProperty<T>(object instance, string propertyName)
        {
            Assert.IsNotNull(instance, "Target instance cannot be null.");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Type targetType = (instance is Type t) ? t : instance.GetType();
            var prop = targetType.GetProperty(propertyName, flags);
            Assert.IsNotNull(prop, $"Property '{propertyName}' was not found on type '{targetType.Name}'.");
            return (T)prop.GetValue(instance is Type ? null : instance);
        }

        /// <summary>
        /// Retrieves a field value from a target object.
        /// </summary>
        public static T GetField<T>(object instance, string fieldName)
        {
            Assert.IsNotNull(instance, "Target instance cannot be null.");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Type targetType = (instance is Type t) ? t : instance.GetType();
            var field = targetType.GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found on type '{targetType.Name}'.");
            return (T)field.GetValue(instance is Type ? null : instance);
        }

        /// <summary>
        /// Loads an asset from the project at the specified asset path.
        /// </summary>
        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        /// <summary>
        /// Finds a GameObject by name or hierarchy path.
        /// </summary>
        public static GameObject FindSceneObject(string nameOrPath)
        {
            var go = GameObject.Find(nameOrPath);
            if (go != null) return go;

            // Search by root objects
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name.Equals(nameOrPath, StringComparison.OrdinalIgnoreCase))
                    return root;

                var child = FindChildRecursive(root.transform, nameOrPath);
                if (child != null) return child.gameObject;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return c;
                var found = FindChildRecursive(c, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Authoritative oracle: calculates expected swatch UV center for (col, row) on a 16x16 grid.
        /// Formula: ((col + 0.5) / 16, (row + 0.5) / 16)
        /// </summary>
        public static Vector2 CalculateExpectedSwatchUV(int col, int row)
        {
            return new Vector2((col + 0.5f) / 16f, (row + 0.5f) / 16f);
        }

        /// <summary>
        /// Authoritative oracle: verifies that a GameObject hierarchy contains ONLY primitive colliders
        /// (BoxCollider, SphereCollider, CapsuleCollider) and ZERO dynamic MeshColliders.
        /// </summary>
        public static bool VerifyCompoundPrimitiveCollidersOnly(GameObject root, out string violationMessage)
        {
            violationMessage = string.Empty;
            if (root == null)
            {
                violationMessage = "Root GameObject is null.";
                return false;
            }

            var rb = root.GetComponentInParent<Rigidbody>() ?? root.GetComponentInChildren<Rigidbody>();
            bool isDynamic = rb != null && !rb.isKinematic;

            var meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
            foreach (var mc in meshColliders)
            {
                if (isDynamic || (mc.attachedRigidbody != null && !mc.attachedRigidbody.isKinematic))
                {
                    violationMessage = $"Dynamic MeshCollider found on '{mc.gameObject.name}', which violates requirement R4/F13.";
                    return false;
                }
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                violationMessage = $"No colliders found on '{root.name}' or its children.";
                return false;
            }

            foreach (var col in colliders)
            {
                bool isPrimitive = col is BoxCollider || col is SphereCollider || col is CapsuleCollider;
                if (!isPrimitive)
                {
                    violationMessage = $"Non-primitive collider '{col.GetType().Name}' on '{col.gameObject.name}'. Allowed: BoxCollider, SphereCollider, CapsuleCollider.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Authoritative oracle: performs a 2D SAT overlap test between two axis-aligned or oriented bounding boxes on a plane.
        /// </summary>
        public static bool Check2DBoundsOverlap(Bounds b1, Bounds b2, float epsilon = 0.001f)
        {
            return (b1.min.x <= b2.max.x - epsilon && b1.max.x >= b2.min.x + epsilon) &&
                   (b1.min.z <= b2.max.z - epsilon && b1.max.z >= b2.min.z + epsilon);
        }

        /// <summary>
        /// Authoritative oracle: ensures the entire market scene environment is initialized and present in the scene context.
        /// Guaranteed to populate TownSquarePlaza, MarketStalls, ShoppingBasket, MarketProps (11 items), and XR Origin (VR).
        /// </summary>
        public static void EnsureSceneContext()
        {
            // 1. TownSquarePlaza
            var plaza = GameObject.Find("TownSquarePlaza");
            if (plaza == null)
            {
                var builderGo = new GameObject("TownSquarePlaza");
                var builder = builderGo.AddComponent<AnimalCrossing.Environment.TownSquarePlazaBuilder>();
                builder.BuildPlaza();
                plaza = builderGo;
            }

            // 2. MarketStalls
            var stalls = GameObject.Find("MarketStalls");
            if (stalls == null)
            {
                stalls = AnimalCrossing.Environment.MarketStallBuilder.BuildAllStalls(plaza.transform);
            }

            // 3. ShoppingBasket
            var basket = GameObject.Find("ShoppingBasket");
            if (basket == null)
            {
                AnimalCrossing.Interaction.ShoppingBasketBuilder.BuildShoppingBasket();
            }

            // 4. MarketProps (all 11 items)
            var grabs = UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsInactive.Include);
            if (grabs == null || grabs.Length < 11)
            {
                AnimalCrossing.Interaction.MarketPropsBuilder.BuildAllProps();
            }

            // 5. XR Origin (VR)
            var origin = UnityEngine.Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin == null)
            {
                AnimalCrossing.Interaction.VRMarketRigSetup.CreateOrConfigureRig();
            }
        }

        /// <summary>
        /// Locates a specific grabbable prop in the scene by name.
        /// </summary>
        public static GameObject FindProp(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;

            // Search under stalls and market props
            var candidateRoots = new string[] { "MarketProps", "ProduceStall", "CafeStall", "CraftingStall", "MarketStalls" };
            foreach (var rootName in candidateRoots)
            {
                var root = GameObject.Find(rootName);
                if (root != null)
                {
                    var child = FindChildRecursive(root.transform, name);
                    if (child != null) return child.gameObject;
                }
            }

            var allGrabs = UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in allGrabs)
            {
                if (grab.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return grab.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Counts distinct shared materials across all active MeshRenderers in the scene.
        /// </summary>
        public static int CountDistinctSceneMaterials()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var mats = new HashSet<Material>();
            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null) mats.Add(m);
                }
            }
            return mats.Count;
        }

        /// <summary>
        /// Authoritative oracle: checks whether two vertical bounds share a co-planar face with distance < tolerance.
        /// </summary>
        public static bool AreSurfacesCoplanar(float y1, float y2, float tolerance = 0.001f)
        {
            return Mathf.Abs(y1 - y2) <= tolerance;
        }
    }
}
