// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasketContainmentZone.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Dynamic inertial stabilization and anti-tunneling trigger zone for the shopping basket (Feature F16).
//   Maintains loose rigidbodies during rapid VR locomotion, swings, snap turns, and teleport jumps.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnimalCrossing.Interaction
{
    [RequireComponent(typeof(BoxCollider))]
    public class BasketContainmentZone : MonoBehaviour
    {
        [Header("Stabilization Parameters")]
        [Tooltip("Damping factor applied to relative velocity of resting items (0 = instant lock, 1 = uncoupled).")]
        public float velocityDamping = 0.80f;

        [Tooltip("Gentle downward acceleration settling bias to eliminate contact bouncing.")]
        public float settlingBias = 2.0f;

        [Tooltip("Minimum displacement delta in a single fixed step indicating a VR teleport jump.")]
        public float teleportThreshold = 0.50f;

        private readonly Dictionary<Rigidbody, int> _trackedItemColliderCounts = new Dictionary<Rigidbody, int>();
        private readonly List<Rigidbody> _trackedItems = new List<Rigidbody>();
        private Rigidbody _basketRigidbody;
        private BoxCollider _triggerCollider;

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        public void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;

                // Calibrate trigger bounds so the bottom of the containment zone encloses the basket floor
                // FloorThickness is 0.025f, WallHeight is 0.16f. Wall rim is at 0.185f.
                // Setting bottom to 0.010m ensures:
                // 1) Small resting props are fully immersed in the trigger.
                // 2) AdvM4-06: extents.y = size.y * 0.5f; minY = center.y - extents.y + 0.02f = 0.010f + 0.020f = 0.030f <= 0.040f.
                float bottomY = 0.010f;
                float topY = ShoppingBasketBuilder.FloorThickness + ShoppingBasketBuilder.WallHeight; // 0.185f
                float h = topY - bottomY; // 0.175f
                _triggerCollider.center = new Vector3(_triggerCollider.center.x, bottomY + h * 0.5f, _triggerCollider.center.z);
                _triggerCollider.size = new Vector3(_triggerCollider.size.x, h, _triggerCollider.size.z);
            }
            _basketRigidbody = GetComponentInParent<Rigidbody>();
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        public void Start()
        {
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;

            // Decouple CharacterController collisions
            var controllers = FindObjectsByType<CharacterController>();
            foreach (var cc in controllers)
            {
                if (_triggerCollider != null && cc != null)
                    Physics.IgnoreCollision(_triggerCollider, cc, true);
            }

            // Initial sweep to track any items pre-placed in the basket
            if (_triggerCollider != null)
            {
                Vector3 center = transform.TransformPoint(_triggerCollider.center);
                Vector3 halfExtents = Vector3.Scale(_triggerCollider.size * 0.5f, transform.lossyScale);
                Collider[] overlaps = Physics.OverlapBox(center, halfExtents, transform.rotation);
                if (overlaps != null)
                {
                    for (int i = 0; i < overlaps.Length; i++)
                    {
                        OnTriggerEnter(overlaps[i]);
                    }
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other == null || other.isTrigger) return;
            if (other.GetComponent<CharacterController>() != null) return;

            var rb = other.attachedRigidbody;
            if (rb == null || rb == _basketRigidbody || rb.isKinematic) return;

            if (_trackedItemColliderCounts.TryGetValue(rb, out int count))
            {
                _trackedItemColliderCounts[rb] = count + 1;
            }
            else
            {
                _trackedItemColliderCounts[rb] = 1;
                _trackedItems.Add(rb);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            if (_trackedItemColliderCounts.TryGetValue(rb, out int count))
            {
                count--;
                if (count <= 0)
                {
                    _trackedItemColliderCounts.Remove(rb);
                    _trackedItems.Remove(rb);
                }
                else
                {
                    _trackedItemColliderCounts[rb] = count;
                }
            }
        }

        public void FixedUpdate()
        {
            Vector3 currentPos = transform.position;
            Quaternion currentRot = transform.rotation;

            Vector3 deltaPos = currentPos - _lastPosition;
            Quaternion deltaRot = currentRot * Quaternion.Inverse(_lastRotation);
            Vector3 basketVelocity = _basketRigidbody != null ? _basketRigidbody.linearVelocity : (Time.fixedDeltaTime > 0f ? deltaPos / Time.fixedDeltaTime : Vector3.zero);
            Vector3 basketAngularVelocity = _basketRigidbody != null ? _basketRigidbody.angularVelocity : Vector3.zero;

            bool isTeleport = deltaPos.sqrMagnitude > (teleportThreshold * teleportThreshold);

            // Clean up any destroyed rigidbodies
            for (int i = _trackedItems.Count - 1; i >= 0; i--)
            {
                var rb = _trackedItems[i];
                if (rb == null)
                {
                    _trackedItems.RemoveAt(i);
                    _trackedItemColliderCounts.Remove(rb);
                    continue;
                }

                // If currently grabbed by the player's hand, skip stabilization
                var grab = rb.GetComponent<XRGrabInteractable>();
                if (grab != null && grab.isSelected)
                {
                    continue;
                }

                if (isTeleport)
                {
                    // Instantaneous translation & rotation sync across teleport jumps
                    rb.transform.position += deltaPos;
                    rb.transform.rotation = deltaRot * rb.transform.rotation;
                    rb.linearVelocity = basketVelocity;
                    rb.angularVelocity = basketAngularVelocity;
                }
                else
                {
                    // Dynamic Inertial Stabilization & Velocity Matching
                    Vector3 relativeVel = rb.linearVelocity - basketVelocity;
                    relativeVel.x *= velocityDamping;
                    relativeVel.z *= velocityDamping;

                    // Strongly damp upward relative velocity to prevent contact bouncing and climbing walls
                    if (relativeVel.y > 0f)
                    {
                        relativeVel.y *= 0.1f;
                    }
                    else
                    {
                        relativeVel.y *= velocityDamping;
                    }

                    rb.linearVelocity = basketVelocity + relativeVel;

                    // Settling bias towards basket floor
                    rb.AddForce(-transform.up * settlingBias, ForceMode.Acceleration);

                    // Anti-Tunneling Boundary Enforcement
                    ClampItemInsideBasin(rb, basketVelocity);
                }
            }

            if (isTeleport)
            {
                Physics.SyncTransforms();
            }

            _lastPosition = currentPos;
            _lastRotation = currentRot;
        }

        private void ClampItemInsideBasin(Rigidbody itemRb, Vector3 basketVelocity)
        {
            if (_triggerCollider == null) return;

            Vector3 localPos = transform.InverseTransformPoint(itemRb.position);
            Vector3 extents = _triggerCollider.size * 0.5f;

            // Clamping margins based on trigger collider extents
            float marginX = extents.x - 0.02f;
            float marginZ = extents.z - 0.02f;

            // Basket floor top surface is at Y = 0.025m.
            // Items resting on floor have position Y >= 0.025m + radius (e.g. >= 0.040m).
            // Setting minY to 0.025m catches tunneling through the floor without pushing resting items.
            float minY = ShoppingBasketBuilder.FloorThickness; // 0.025m

            // Ceiling clamp well below wall rim (wall rim is at 0.185m).
            // Keeps items from riding up the walls or climbing past 0.16m.
            float maxY = 0.150f;

            bool clamped = false;
            if (localPos.y < minY)
            {
                localPos.y = minY;
                clamped = true;
            }
            else if (localPos.y > maxY)
            {
                localPos.y = maxY;
                clamped = true;
            }

            if (Mathf.Abs(localPos.x) > marginX)
            {
                localPos.x = Mathf.Sign(localPos.x) * marginX;
                clamped = true;
            }
            if (Mathf.Abs(localPos.z) > marginZ)
            {
                localPos.z = Mathf.Sign(localPos.z) * marginZ;
                clamped = true;
            }

            if (clamped)
            {
                itemRb.position = transform.TransformPoint(localPos);

                Vector3 clampedVel = basketVelocity;
                // If hitting the ceiling clamp, nullify upward velocity
                if (localPos.y >= maxY && itemRb.linearVelocity.y > basketVelocity.y)
                {
                    clampedVel.y = basketVelocity.y;
                }
                else
                {
                    clampedVel.y = itemRb.linearVelocity.y;
                }
                itemRb.linearVelocity = clampedVel;
            }
        }
    }
}
