# 🍃 Animal Crossing VR Village Market Square

> An abundantly stocked, stylized Animal Crossing-inspired PC VR & Desktop Village Market Scene built with **Unity 6 (6000.5.5f1)**, **Universal Render Pipeline (URP)**, and **XR Interaction Toolkit (XRI 3.5.1)**.

---

## 🌟 Highlights & Technical Art Contracts

* **100% In-Engine Procedural Geometry:**
  Strictly zero external 3D model files (`.fbx`, `.obj`, `.gltf`, `.blend`). Every stall beam, catenary awning, cobblestone plaza slab, fountain basin, fruit, pastry, mug, teapot, watering can, and basket is procedurally generated with micro-bevels via `ProceduralMeshBuilder` and `ProceduralCanopyBuilder`.
* **Single Color Atlas & 4 Master Materials:**
  Entire scene maps UV coordinates into a single $512\times 512$ 256-swatch color palette atlas (`AC_ColorAtlas_512.png`). Powered by 4 master materials (`Opaque`, `Foliage`, `LiquidGlass`, `Emissive`) with GPU Instancing enabled, maintaining a lightweight draw call budget ($\le 100$ draw calls).
* **100% Compound Primitive Colliders:**
  Zero dynamic `MeshCollider` components. All 31 interactable objects use compound Box, Sphere, and Capsule primitive colliders with continuous dynamic collision detection and sub-millimeter surface alignment ($< 2.0\,\text{mm}$ penetration tolerance).
* **Full Realistic Market Inventory (30 Items + Shopping Basket):**
  A thriving Animal Crossing-style market square featuring 30 distinct grabbable items across 3 interactive stalls, plus a physical carryable Shopping Basket.
* **Dual-Mode Locomotion & Desktop Controls:**
  Full out-of-the-box support for both **PC VR Headsets** (OpenXR 6DoF, smooth locomotion, snap turning, teleportation, direct/ray grab) and **Desktop Keyboard & Mouse** (WASD, first-person mouse look, contextual Animal Crossing bloom reticle, grab/drop/toss).
* **Forensic Geometric & Technical Art Audit Engine:**
  In-engine mathematical verification tool (`MarketGeometricAuditRunner`) executing 2D SAT polygon coplanar checks, downward raycast gap detection, UV gutter margin validation, and collider tightness audits.
* **105 Automated Tests (100% Pass Rate):**
  Comprehensive test suite covering unit, boundary, integration, and scenario tiers.

---

## 🏪 Market Stalls & Inventory Breakdown

```
                                  [ TOWN SQUARE PLAZA ]
                                            │
           ┌────────────────────────────────┼────────────────────────────────┐
           ▼                                ▼                                ▼
   [ Produce Stall ]                 [ Café Stall ]                 [ Crafting Stall ]
  (Fresh Harvest — 13 Props)        (Bakery & Brew — 10 Props)      (Garden Nursery — 7 Props)
  ├── 4x Apples (Tray trio + Crate) ├── 4x Flavored Donuts (Showcase)├── 1x Watering Can (Zinc)
  ├── 3x Oranges (Crate Bay)        ├── 3x Artisan Sliced Breads    ├── 3x Potted Saplings (Mat trio)
  ├── 3x Peaches (Crate Bay)        ├── 2x Coffee Mugs (Mint+Ivory) └── 3x Seed Packets (Variety row)
  └── 3x Turnips (Counter + Scale)  └── 1x Ceramic Teapot
```

### 1. 🍎 Produce Stall (13 Grabbable Items)
* **Apples (4 items):** 3 resting in an organic fruit trio on a woven wicker tray ($Y = 0.951\,\text{m}$), 1 bulk display specimen in the wooden crate tier ($Y = 1.090\,\text{m}$).
* **Oranges (3 items):** Golden citrus trio nestled inside the inclined display crate.
* **Peaches (3 items):** Velvet peach trio with leaf accents in the crate bay.
* **Turnips (3 items):** Fresh harvest bunch with leafy greens on the counter, plus a turnip resting in the brass weighing scale pan.

### 2. ☕ Café & Bakery Stall (10 Grabbable Items)
* **Glazed Donuts (4 items in Acrylic Pastry Showcase at $Y = 0.984\,\text{m}$):**
  * `Donut`: Strawberry glaze with rainbow sugar sprinkles.
  * `Donut_Choco`: Rich dark chocolate ganache glaze.
  * `Donut_Matcha`: Artisanal green tea matcha glaze.
  * `Donut_Vanilla`: Classic sweet white fondant glaze.
* **Artisan Sliced Bread (3 items):** Whole sourdough half-cylinder loaf + 2 thick golden-crust bread slices.
* **Coffee & Tea (3 items):**
  * `CoffeeMug`: Mint green ceramic mug snapped into `CoffeeSaucerSocket`.
  * `CoffeeMug_02`: Companion warm ivory porcelain mug.
  * `Teapot`: Handcrafted ceramic teapot with curved spout and lid.

### 3. 🌱 Crafting & Garden Stall (7 Grabbable Items)
* **Watering Can (1 item):** Galvanized zinc body with angled spout and curved carry handle.
* **Potted Saplings (3 items):** Terra cotta nursery pots with rich potting soil and green sprouts resting flush on the burlap potting mat ($Y = 0.931\,\text{m}$).
* **Seed Packets (3 items on workbench at $Y = 0.920\,\text{m}$):**
  * `SeedPacket`: Golden Sunflower / Marigold badge.
  * `SeedPacket_Tomato`: Heirloom Red Tomato badge.
  * `SeedPacket_Herbs`: Fresh Garden Herbs badge.

### 4. 🧺 Shopping Basket (XR Interactable)
* Woven wicker basket with twin handles, physical collision cavity, and `BasketContainmentZone` trigger for dynamic inertial retention of carried fruit and bakery items.

---

## 🎮 How to Play

### Option A: Desktop Mode (Keyboard & Mouse)
No VR headset needed! Run the project directly in the Unity Game view:

1. Open `Assets/Scenes/AnimalCrossingMarketSquare.unity` and click **Play ($\blacktriangleright$)**.
2. **Click inside the Game view** to lock the mouse into first-person view.
3. **Controls:**
   * **W / A / S / D:** Walk through the market square ($2.4\,\text{m/s}$ stroll speed).
   * **Mouse Move:** First-person look around.
   * **Escape:** Unlock mouse cursor.
   * **Aim / In-Range Indicator:**
     * Aim at empty space $\rightarrow$ Clean white aiming dot.
     * Aim at an item within reach ($\le 2.4\,\text{m}$) $\rightarrow$ Reticle blooms into a **bright green focus ring** with item badge (`🖐 [E / Click] Grab {Item Name}`).
     * Aim at an item out of reach ($> 2.4\,\text{m}$) $\rightarrow$ Amber warning ring with distance indicator.
   * **E or Left Click:** Pick up the hovered item. Item smoothly floats in front of you.
   * **E or Left Click (while holding):** Place / drop item gently.
   * **F or Space (or Right Click):** Toss / throw item forward.
   * **Snap to Socket:** Drop the `CoffeeMug` near the café saucer to watch it snap into place.

### Option B: PC VR Headset (Meta Quest Link / SteamVR / Index)
1. Connect your headset via Link Cable, Air Link, or SteamVR.
2. Ensure OpenXR is selected as the active runtime.
3. Click **Play ($\blacktriangleright$)** in Unity and put on the headset.
4. **VR Controls:**
   * **Left Thumbstick:** Smooth Locomotion.
   * **Right Thumbstick:** Snap Turning (45°) and Teleportation Ray onto plaza pavement.
   * **Grip Button:** Grab items and basket handles with dual direct/ray interactors.

---

## 🔍 Forensic Technical Art Audit Engine

Built-in mathematical QA audit engine accessible via Unity menu:  
`AnimalCrossing > Run Geometric QA Audit`

```
==================================================================
Starting Forensic Geometric & Technical Art Audit (5/5 Checks)
==================================================================
1. SAT 2D Coplanar Overlap Check:  0 violations (Zero Z-fighting)
2. Downward Raycast Gap Check:     0 violations (Zero gaps > 1.5mm)
3. Palette Atlas UV Bleed Check:   0 violations (Margin >= 1.5px)
4. Collider Penetration Check:     0 violations (Zero penetration > 2.0mm)
5. Master Material & Draw Calls:   100% compliant (<= 100 draw calls)
------------------------------------------------------------------
VERDICT: QA Audit PASSED! All 5/5 checks passed cleanly (IsClean: True)
==================================================================
```

---

## 🧪 Automated Test Suite

Run the full EditMode / PlayMode test harness via Unity menu:  
`AnimalCrossing > Run All Tests (Tiers 1-4)` or `AnimalCrossing > Run Tier 1 Tests`

* **Tier 1 (Feature Coverage):** 105 tests verifying scene contracts, stall geometries, prop colliders, rigidbodies, and audit rules.
* **Tier 2 (Boundary & Corner Cases):** Socket detachments, high-speed throw trajectories, rapid pickup cycling.
* **Tier 3 (Cross-Feature Interactions):** Basket containment during locomotion, saucer snapping, fruit crate settling.
* **Tier 4 (Application Scenarios):** Full village shopping cycle from plaza spawn to basket checkout.

```
[Tier1_FeatureCoverageTests] ALL PASSED! Total: 105, Passed: 105, Failed: 0.
```

---

## 📂 Project Structure

```
Assets/
├── Materials/
│   ├── M_AnimalCrossing_Opaque.mat        # Master opaque material (GPU instanced)
│   ├── M_AnimalCrossing_Foliage.mat       # Foliage master material
│   ├── M_AnimalCrossing_LiquidGlass.mat   # Fountain water & acrylic showcase glass
│   └── M_AnimalCrossing_Emissive.mat      # Street lanterns & glowing elements
├── Textures/
│   └── AC_ColorAtlas_512.png              # 512x512 256-swatch palette texture
├── Scenes/
│   ├── AnimalCrossingMarketSquare.unity   # Primary village market square scene
│   └── SampleScene.unity                  # Secondary synchronized scene
└── Scripts/
    ├── Environment/
    │   ├── TownSquarePlazaBuilder.cs      # Radial cobblestone plaza & fountain builder
    │   └── MarketStallBuilder.cs          # Produce, Café, and Crafting stalls builder
    ├── Interaction/
    │   ├── MarketPropsBuilder.cs          # All 30 procedural grabbable props builder
    │   ├── ShoppingBasketBuilder.cs       # Physical Shopping Basket & handles builder
    │   ├── BasketContainmentZone.cs       # Anti-tunneling inertial retention trigger
    │   └── VRMarketRigSetup.cs            # Complete PC VR & Desktop player rig setup
    ├── Locomotion/
    │   └── SimplePCController.cs          # Desktop WASD + Mouse look + HUD & Grab controller
    ├── TechnicalArt/
    │   ├── ProceduralMeshBuilder.cs       # Beveled cubes, cylinders, spheres, cones, swatches
    │   └── ProceduralCanopyBuilder.cs     # Striped catenary awnings with scallops
    ├── Audit/
    │   └── MarketGeometricAuditRunner.cs  # 5-check mathematical forensic audit engine
    └── Tests/
        ├── TestContracts.cs               # Authoritative test oracles & assertions
        ├── Tier1_FeatureCoverageTests.cs  # 105 Tier 1 regression tests
        └── AllTiersTestRunner.cs          # Synchronous reflection test harness
```

---

## ⚙️ Requirements & Compatibility

* **Unity Version:** Unity 6 (`6000.5.5f1` or later)
* **Render Pipeline:** Universal Render Pipeline (URP)
* **Target Platforms:** PC Standalone (Windows 10 / 11 64-bit)
* **VR Support:** OpenXR, Meta Quest (Link / Air Link), SteamVR, HTC Vive, Valve Index
* **Packages:**
  * `com.unity.xr.interaction.toolkit` (3.5.1)
  * `com.unity.xr.core-utils` (2.4.0)
  * `com.unity.inputsystem` (1.14.0)
  * `com.unity.render-pipelines.universal` (17.5.0)

---

## 📜 License

Created for educational, portfolio, and VR development purposes. Animal Crossing is a registered trademark of Nintendo Co., Ltd. Stylized aesthetic inspired by Animal Crossing: New Horizons.
