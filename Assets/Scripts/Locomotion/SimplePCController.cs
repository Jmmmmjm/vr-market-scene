// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimplePCController.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Dual Keyboard & Mouse / Desktop First-Person interaction and locomotion controller.
//   Enables full playability in Game view without a VR headset:
//   1. Smooth walking (WASD / Arrow Keys) with CharacterController.
//   2. Responsive first-person mouse look with seamless cursor locking (Click to focus, Esc to release).
//   3. Raycast hover detection with dynamic in-range indicators and stylized Animal Crossing HUD reticle.
//   4. Desktop grab (E / Left-Click), carry, drop, and toss (F / Space / Right-Click).
//   5. Native compatibility with XRGrabInteractable, XRSocketInteractor, and ShoppingBasket containment.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnimalCrossing.Locomotion
{
    public class SimplePCController : MonoBehaviour
    {
        [Header("Locomotion Settings")]
        public float moveSpeed = 2.4f;
        public float lookSpeed = 2.2f;
        public bool lockCursorInPlay = true;

        [Header("Interaction & Grab Settings")]
        public float grabRange = 2.4f;
        public float highlightRange = 4.0f;
        public Vector3 defaultHoldOffset = new Vector3(0.14f, -0.12f, 0.60f);
        public Vector3 basketHoldOffset = new Vector3(0f, -0.26f, 0.72f);
        public float tossForce = 4.2f;

        // Core Components
        private CharacterController characterController;
        private Transform cameraTransform;
        private Camera playerCamera;
        private float pitch = 0f;
        private bool isCursorLocked = false;

        // Hover & Grab State
        private XRGrabInteractable hoveredInteractable;
        private float hoveredDistance;
        private string hoveredItemDisplayName = "";
        private bool isHoveredInGrabRange = false;

        private XRGrabInteractable heldInteractable;
        private Rigidbody heldRigidbody;
        private bool originalUseGravity;
        private bool originalIsKinematic;
        private RigidbodyInterpolation originalInterpolation;
        private Vector3 currentHoldOffset;

        // GUI Assets & Styles
        private Texture2D whitePixel;
        private Texture2D circleTexture;
        private GUIStyle promptTitleStyle;
        private GUIStyle promptSubtitleStyle;
        private GUIStyle helpGuideStyle;
        private bool guiInitialized = false;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                cameraTransform = playerCamera.transform;
                pitch = cameraTransform.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }

            if (lockCursorInPlay && Application.isPlaying)
            {
                SetCursorLock(true);
            }
        }

        private void OnDisable()
        {
            if (heldInteractable != null)
            {
                DropItem(false);
            }
            SetCursorLock(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // 1. Cursor Lock Management
            HandleCursorLockInputs(keyboard, mouse);

            // 2. Mouse Look
            HandleMouseLook(mouse);

            // 3. Locomotion Movement (WASD)
            HandleMovement(keyboard);

            // 4. Raycast Hover Detection
            UpdateHoverState();

            // 5. Interaction Inputs (Grab / Drop / Toss)
            HandleInteractionInputs(keyboard, mouse);
        }

        private void LateUpdate()
        {
            // Smoothly carry held item in front of player
            if (heldInteractable != null && cameraTransform != null)
            {
                Vector3 targetPos = cameraTransform.TransformPoint(currentHoldOffset);
                Quaternion targetRot = cameraTransform.rotation;

                heldInteractable.transform.position = Vector3.Lerp(heldInteractable.transform.position, targetPos, Time.deltaTime * 24f);
                heldInteractable.transform.rotation = Quaternion.Slerp(heldInteractable.transform.rotation, targetRot, Time.deltaTime * 24f);
            }
        }

        // =========================================================================
        // Locomotion & View Controls
        // =========================================================================

        private void HandleCursorLockInputs(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLock(false);
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !isCursorLocked)
            {
                SetCursorLock(true);
            }
        }

        private void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook(Mouse mouse)
        {
            if (cameraTransform == null || mouse == null) return;

            bool canLook = isCursorLocked || mouse.rightButton.isPressed;
            if (canLook)
            {
                Vector2 delta = mouse.delta.ReadValue() * (lookSpeed * 0.10f);
                transform.Rotate(0f, delta.x, 0f, Space.World);

                pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
                Vector3 euler = cameraTransform.localEulerAngles;
                euler.x = pitch;
                cameraTransform.localEulerAngles = euler;
            }
        }

        private void HandleMovement(Keyboard keyboard)
        {
            Vector3 inputDir = Vector3.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDir += Vector3.forward;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDir += Vector3.back;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDir += Vector3.left;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDir += Vector3.right;
            }

            if (inputDir.sqrMagnitude > 0.001f)
            {
                inputDir.Normalize();
                Vector3 worldDir = transform.TransformDirection(inputDir);
                worldDir.y = 0f;
                worldDir.Normalize();

                Vector3 motion = worldDir * (moveSpeed * Time.deltaTime);
                if (characterController != null && characterController.enabled)
                {
                    characterController.Move(motion);
                }
                else
                {
                    transform.position += motion;
                }
            }
        }

        // =========================================================================
        // Raycast Hover & Interaction System
        // =========================================================================

        private void UpdateHoverState()
        {
            hoveredInteractable = null;
            hoveredItemDisplayName = "";
            hoveredDistance = 0f;
            isHoveredInGrabRange = false;

            if (cameraTransform == null || heldInteractable != null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, highlightRange);
            if (hits == null || hits.Length == 0) return;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.GetComponent<CharacterController>() != null) continue;

                var grab = hit.collider.GetComponentInParent<XRGrabInteractable>();
                if (grab != null && grab.enabled)
                {
                    hoveredInteractable = grab;
                    hoveredDistance = hit.distance;
                    isHoveredInGrabRange = hit.distance <= grabRange;
                    hoveredItemDisplayName = FormatDisplayName(grab.gameObject.name);
                    break;
                }
            }
        }

        private void HandleInteractionInputs(Keyboard keyboard, Mouse mouse)
        {
            bool grabDropPressed = false;
            bool tossPressed = false;

            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) grabDropPressed = true;
                if (keyboard.fKey.wasPressedThisFrame) tossPressed = true;
                if (keyboard.spaceKey.wasPressedThisFrame && heldInteractable != null) tossPressed = true;
            }

            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame && isCursorLocked)
                {
                    grabDropPressed = true;
                }
                if (mouse.rightButton.wasPressedThisFrame && heldInteractable != null && isCursorLocked)
                {
                    tossPressed = true;
                }
            }

            // Execute Grab / Drop / Toss
            if (grabDropPressed)
            {
                if (heldInteractable != null)
                {
                    DropItem(toss: false);
                }
                else if (hoveredInteractable != null && isHoveredInGrabRange)
                {
                    GrabItem(hoveredInteractable);
                }
            }
            else if (tossPressed && heldInteractable != null)
            {
                DropItem(toss: true);
            }
        }

        public void GrabItem(XRGrabInteractable grab)
        {
            if (grab == null) return;
            var rb = grab.GetComponent<Rigidbody>();
            if (rb == null) return;

            heldInteractable = grab;
            heldRigidbody = rb;
            originalUseGravity = rb.useGravity;
            originalIsKinematic = rb.isKinematic;
            originalInterpolation = rb.interpolation;

            // Release from XRSocketInteractor if snapped in a socket
            var socket = grab.transform.GetComponentInParent<XRSocketInteractor>();
            if (socket == null && grab.transform.parent != null)
            {
                socket = grab.transform.parent.GetComponent<XRSocketInteractor>();
            }
            if (socket != null && socket.interactionManager != null)
            {
                socket.interactionManager.SelectExit((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);
            }

            // Configure physics while held
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            bool isBasket = grab.name.Contains("ShoppingBasket");
            currentHoldOffset = isBasket ? basketHoldOffset : defaultHoldOffset;
        }

        public void DropItem(bool toss = false)
        {
            if (heldInteractable == null || heldRigidbody == null) return;

            heldRigidbody.isKinematic = false;
            heldRigidbody.useGravity = true;
            heldRigidbody.interpolation = originalInterpolation;

            Vector3 playerVel = characterController != null ? characterController.velocity : Vector3.zero;
            if (toss)
            {
                heldRigidbody.linearVelocity = playerVel + cameraTransform.forward * tossForce + Vector3.up * 1.2f;
                heldRigidbody.angularVelocity = UnityEngine.Random.insideUnitSphere * 2.5f;
            }
            else
            {
                // Gentle release forward
                heldRigidbody.linearVelocity = playerVel + cameraTransform.forward * 0.6f;
                heldRigidbody.angularVelocity = Vector3.zero;
            }

            heldInteractable = null;
            heldRigidbody = null;
        }

        // =========================================================================
        // High-Clarity Animal Crossing HUD & In-Range Reticle Indicator
        // =========================================================================

        private void InitGUI()
        {
            if (guiInitialized) return;

            whitePixel = new Texture2D(1, 1);
            whitePixel.SetPixel(0, 0, Color.white);
            whitePixel.Apply();

            // Procedural smooth anti-aliased circular reticle (32x32)
            int res = 32;
            circleTexture = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float r = res * 0.5f;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float alpha = Mathf.Clamp01((r - dist) * 1.8f);
                    circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            circleTexture.Apply();

            promptTitleStyle = new GUIStyle();
            promptTitleStyle.fontSize = 15;
            promptTitleStyle.fontStyle = FontStyle.Bold;
            promptTitleStyle.alignment = TextAnchor.MiddleCenter;
            promptTitleStyle.normal.textColor = Color.white;

            promptSubtitleStyle = new GUIStyle();
            promptSubtitleStyle.fontSize = 12;
            promptSubtitleStyle.fontStyle = FontStyle.Normal;
            promptSubtitleStyle.alignment = TextAnchor.MiddleCenter;
            promptSubtitleStyle.normal.textColor = new Color(0.92f, 0.98f, 0.92f, 0.90f);

            helpGuideStyle = new GUIStyle();
            helpGuideStyle.fontSize = 12;
            helpGuideStyle.fontStyle = FontStyle.Bold;
            helpGuideStyle.alignment = TextAnchor.MiddleLeft;
            helpGuideStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

            guiInitialized = true;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            InitGUI();

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (heldInteractable != null)
            {
                // 1. Currently Holding an Item
                DrawCircle(center, 10f, new Color(0.35f, 0.85f, 1f, 0.95f));
                DrawPillBadge(center.y + 32f,
                    $"🖐 Carrying: {FormatDisplayName(heldInteractable.name)}",
                    "[E / Click] Drop  •  [F / Space] Toss",
                    new Color(0.10f, 0.20f, 0.28f, 0.90f),
                    new Color(0.35f, 0.85f, 1f, 0.95f));
            }
            else if (hoveredInteractable != null)
            {
                if (isHoveredInGrabRange)
                {
                    // 2. Pointing at Grabbable Item IN RANGE (<= 2.4m)
                    // Bright Animal Crossing leafy green bloom reticle + ring
                    DrawCircle(center, 12f, new Color(0.48f, 0.88f, 0.22f, 1.0f));
                    DrawRing(center, 26f, 2.5f, new Color(0.48f, 0.88f, 0.22f, 0.85f));

                    DrawPillBadge(center.y + 36f,
                        $"🖐 [E / Click] Grab {hoveredItemDisplayName}",
                        $"Distance: {hoveredDistance:F2}m  •  Ready to Pick Up",
                        new Color(0.12f, 0.26f, 0.16f, 0.92f),
                        new Color(0.48f, 0.88f, 0.22f, 0.95f));
                }
                else
                {
                    // 3. Pointing at Grabbable Item OUT OF RANGE (2.4m - 4.0m)
                    // Warm amber reticle indicating presence but requiring proximity
                    DrawCircle(center, 8f, new Color(1.0f, 0.78f, 0.28f, 0.80f));

                    DrawPillBadge(center.y + 32f,
                        $"{hoveredItemDisplayName} (Too Far)",
                        $"Walk closer ({hoveredDistance:F2}m > {grabRange:F1}m)",
                        new Color(0.28f, 0.24f, 0.10f, 0.85f),
                        new Color(1.0f, 0.78f, 0.28f, 0.85f));
                }
            }
            else
            {
                // 4. Default Ambient Reticle (Looking at scenery)
                DrawCircle(center, 6f, new Color(1f, 1f, 1f, 0.50f));
            }

            // Top-Left Control Guide
            DrawHelpBar();
        }

        private void DrawCircle(Vector2 center, float diameter, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            float half = diameter * 0.5f;
            GUI.DrawTexture(new Rect(center.x - half, center.y - half, diameter, diameter), circleTexture);
            GUI.color = old;
        }

        private void DrawRing(Vector2 center, float outerDiameter, float thickness, Color color)
        {
            // Outer circle in color, punch hole inside with inner overlay
            DrawCircle(center, outerDiameter, color);
            DrawCircle(center, outerDiameter - thickness * 2f, new Color(0f, 0f, 0f, 0.4f));
        }

        private void DrawPillBadge(float topY, string title, string subtitle, Color bgCol, Color borderCol)
        {
            float w = Mathf.Max(300f, title.Length * 11f);
            float h = 48f;
            float x = (Screen.width - w) * 0.5f;
            Rect bgRect = new Rect(x, topY, w, h);

            Color old = GUI.color;

            // Border (1.5px)
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(bgRect.x - 1.5f, bgRect.y - 1.5f, bgRect.width + 3f, bgRect.height + 3f), whitePixel);

            // Fill
            GUI.color = bgCol;
            GUI.DrawTexture(bgRect, whitePixel);
            GUI.color = old;

            // Text
            GUI.Label(new Rect(bgRect.x, bgRect.y + 4f, bgRect.width, 22f), title, promptTitleStyle);
            GUI.Label(new Rect(bgRect.x, bgRect.y + 24f, bgRect.width, 18f), subtitle, promptSubtitleStyle);
        }

        private void DrawHelpBar()
        {
            Rect barRect = new Rect(16f, 14f, 440f, 26f);
            Color old = GUI.color;
            GUI.color = new Color(0.1f, 0.12f, 0.15f, 0.70f);
            GUI.DrawTexture(barRect, whitePixel);
            GUI.color = old;

            string help = isCursorLocked
                ? "WASD: Walk  |  Mouse: Look  |  E / Click: Grab  |  F: Toss  |  Esc: Free Mouse"
                : "Click Game Screen to Lock Mouse & Look Around  |  WASD: Walk";
            GUI.Label(new Rect(barRect.x + 8f, barRect.y + 3f, barRect.width - 16f, 20f), help, helpGuideStyle);
        }

        private static string FormatDisplayName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Market Item";
            if (rawName.StartsWith("Apple")) return "Fresh Apple";
            if (rawName.StartsWith("Orange")) return "Sweet Orange";
            if (rawName.StartsWith("Peach")) return "Ripe Peach";
            if (rawName.StartsWith("Turnip")) return "Farm Harvest Turnip";
            if (rawName.Contains("Donut_Choco")) return "Chocolate Ganache Donut";
            if (rawName.Contains("Donut_Matcha")) return "Matcha Green Tea Donut";
            if (rawName.Contains("Donut_Vanilla")) return "Vanilla Fondant Donut";
            if (rawName.StartsWith("Donut")) return "Strawberry Glazed Donut";
            if (rawName.StartsWith("CoffeeMug_02")) return "Ivory Porcelain Mug";
            if (rawName.StartsWith("CoffeeMug")) return "Mint Ceramic Mug";
            if (rawName.StartsWith("Teapot")) return "Ceramic Teapot";
            if (rawName.Contains("SlicedBread_Loaf")) return "Artisan Bread Loaf";
            if (rawName.StartsWith("SlicedBread")) return "Artisan Sliced Bread";
            if (rawName.StartsWith("WateringCan")) return "Garden Watering Can";
            if (rawName.StartsWith("PottedSapling")) return "Potted Sapling Plant";
            if (rawName.Contains("SeedPacket_Tomato")) return "Heirloom Tomato Seeds";
            if (rawName.Contains("SeedPacket_Herbs")) return "Garden Herb Seeds";
            if (rawName.StartsWith("SeedPacket")) return "Sunflower Flower Seeds";
            if (rawName.StartsWith("ShoppingBasket")) return "Shopping Basket";
            return rawName;
        }
    }
}
