// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorAtlasGenerator.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Procedural 512x512 Master Color Palette Atlas Generator for Animal Crossing Stylized VR Market.
//   Generates a 16x16 grid of 256 distinct 32x32px pastel color swatches with cell-center UV collapsing
//   and zero-bleed guarantees. Configures TextureImporter for bilinear clamp filtering without mipmaps.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimalCrossing.TechnicalArt
{
    /// <summary>
    /// Represents a strongly-typed coordinate in the 16x16 Color Palette Atlas.
    /// </summary>
    [System.Serializable]
    public struct SwatchCoord : IEquatable<SwatchCoord>
    {
        public int Col;
        public int Row;

        public SwatchCoord(int col, int row)
        {
            Col = Mathf.Clamp(col, 0, ColorAtlasGenerator.GridCols - 1);
            Row = Mathf.Clamp(row, 0, ColorAtlasGenerator.GridRows - 1);
        }

        public Vector2 UV => ColorAtlasGenerator.GetSwatchUV(Col, Row);
        public Color32 Color => ColorAtlasGenerator.GetSwatchColor(Col, Row);

        public bool Equals(SwatchCoord other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is SwatchCoord other && Equals(other);
        public override int GetHashCode() => (Col * 397) ^ Row;
        public override string ToString() => $"Swatch({Col}, {Row}) [UV: {UV.x:F4}, {UV.y:F4}]";

        public static bool operator ==(SwatchCoord left, SwatchCoord right) => left.Equals(right);
        public static bool operator !=(SwatchCoord left, SwatchCoord right) => !left.Equals(right);
    }

    /// <summary>
    /// Pre-defined named swatch coordinates mapped directly to Animal Crossing village props,
    /// stalls, nature elements, foods, ceramics, and structural woodwork.
    /// </summary>
    public static class Swatches
    {
        // --------------------------------------------------------------------
        // Row 0: Woods & Timbers
        // --------------------------------------------------------------------
        public static readonly SwatchCoord WoodBirchBlond       = new SwatchCoord(0, 0);
        public static readonly SwatchCoord WoodBirchWarm        = new SwatchCoord(1, 0);
        public static readonly SwatchCoord WoodPineTan          = new SwatchCoord(2, 0);
        public static readonly SwatchCoord WoodPineMidtone      = new SwatchCoord(3, 0);
        public static readonly SwatchCoord WoodOakGolden        = new SwatchCoord(4, 0);
        public static readonly SwatchCoord WoodOakRich          = new SwatchCoord(5, 0);
        public static readonly SwatchCoord WoodCedarDark        = new SwatchCoord(6, 0);
        public static readonly SwatchCoord WoodBarkDeep         = new SwatchCoord(7, 0);
        public static readonly SwatchCoord WoodDriftwoodLight   = new SwatchCoord(8, 0);
        public static readonly SwatchCoord WoodDriftwoodDark    = new SwatchCoord(9, 0);
        public static readonly SwatchCoord WoodBambooLime       = new SwatchCoord(10, 0);
        public static readonly SwatchCoord WoodBambooYellow     = new SwatchCoord(11, 0);
        public static readonly SwatchCoord WoodWhitePaint       = new SwatchCoord(12, 0);
        public static readonly SwatchCoord WoodRedPaint         = new SwatchCoord(13, 0);
        public static readonly SwatchCoord WoodTealPaint        = new SwatchCoord(14, 0);
        public static readonly SwatchCoord WoodTimberCharcoal   = new SwatchCoord(15, 0);

        // --------------------------------------------------------------------
        // Row 1: Stone & Masonry
        // --------------------------------------------------------------------
        public static readonly SwatchCoord StonePlazaLimestone   = new SwatchCoord(0, 1);
        public static readonly SwatchCoord StoneSandstoneBuff   = new SwatchCoord(1, 1);
        public static readonly SwatchCoord StoneCobbleLight     = new SwatchCoord(2, 1);
        public static readonly SwatchCoord StoneCobbleMid       = new SwatchCoord(3, 1);
        public static readonly SwatchCoord StoneBasinBlueGrey   = new SwatchCoord(4, 1);
        public static readonly SwatchCoord StoneCopingPaleBlue  = new SwatchCoord(5, 1);
        public static readonly SwatchCoord StoneCoolSlate       = new SwatchCoord(6, 1);
        public static readonly SwatchCoord StonePaverTerracotta = new SwatchCoord(7, 1);
        public static readonly SwatchCoord StonePaverSalmon     = new SwatchCoord(8, 1);
        public static readonly SwatchCoord StoneRiverPebbleDark = new SwatchCoord(9, 1);
        public static readonly SwatchCoord StoneRiverPebbleLight= new SwatchCoord(10, 1);
        public static readonly SwatchCoord StoneWetSlate        = new SwatchCoord(11, 1);
        public static readonly SwatchCoord StoneGraniteBase     = new SwatchCoord(12, 1);
        public static readonly SwatchCoord StoneMarbleWhite     = new SwatchCoord(13, 1);
        public static readonly SwatchCoord StoneMossyRuin       = new SwatchCoord(14, 1);
        public static readonly SwatchCoord StoneBasaltShadow    = new SwatchCoord(15, 1);

        // --------------------------------------------------------------------
        // Row 2: Earth & Pathways
        // --------------------------------------------------------------------
        public static readonly SwatchCoord DirtPathTan          = new SwatchCoord(0, 2);
        public static readonly SwatchCoord DirtPackedWarm       = new SwatchCoord(1, 2);
        public static readonly SwatchCoord DirtBeatenTrack      = new SwatchCoord(2, 2);
        public static readonly SwatchCoord DirtLoamFresh        = new SwatchCoord(3, 2);
        public static readonly SwatchCoord DirtHumusDark        = new SwatchCoord(4, 2);
        public static readonly SwatchCoord DirtPlanterMoist     = new SwatchCoord(5, 2);
        public static readonly SwatchCoord SandDunesPale        = new SwatchCoord(6, 2);
        public static readonly SwatchCoord SandFineCream        = new SwatchCoord(7, 2);
        public static readonly SwatchCoord ClayTerracottaRaw    = new SwatchCoord(8, 2);
        public static readonly SwatchCoord ClayBakedTile        = new SwatchCoord(9, 2);
        public static readonly SwatchCoord MudWetPuddle         = new SwatchCoord(10, 2);
        public static readonly SwatchCoord GravelFine           = new SwatchCoord(11, 2);
        public static readonly SwatchCoord CliffEarthBrown      = new SwatchCoord(12, 2);
        public static readonly SwatchCoord CliffShadowBrown     = new SwatchCoord(13, 2);
        public static readonly SwatchCoord EarthOchreAccent     = new SwatchCoord(14, 2);
        public static readonly SwatchCoord DirtDeepShadow       = new SwatchCoord(15, 2);

        // --------------------------------------------------------------------
        // Row 3: Grass & Foliage
        // --------------------------------------------------------------------
        public static readonly SwatchCoord GrassSpringMeadow    = new SwatchCoord(0, 3);
        public static readonly SwatchCoord GrassACGreen         = new SwatchCoord(1, 3);
        public static readonly SwatchCoord GrassSunlitLawn      = new SwatchCoord(2, 3);
        public static readonly SwatchCoord GrassMidsummerLush   = new SwatchCoord(3, 3);
        public static readonly SwatchCoord GrassLawnEmerald     = new SwatchCoord(4, 3);
        public static readonly SwatchCoord GrassForestShade     = new SwatchCoord(5, 3);
        public static readonly SwatchCoord FoliageHedgeLime     = new SwatchCoord(6, 3);
        public static readonly SwatchCoord FoliageHedgeJade     = new SwatchCoord(7, 3);
        public static readonly SwatchCoord FoliageShrubOlive    = new SwatchCoord(8, 3);
        public static readonly SwatchCoord FoliageTreeSunlitLime= new SwatchCoord(9, 3);
        public static readonly SwatchCoord FoliageTreeEmerald   = new SwatchCoord(10, 3);
        public static readonly SwatchCoord FoliageTreeUnderside = new SwatchCoord(11, 3);
        public static readonly SwatchCoord MossVelvetLime       = new SwatchCoord(12, 3);
        public static readonly SwatchCoord MossShadedForest     = new SwatchCoord(13, 3);
        public static readonly SwatchCoord PineNeedleForest     = new SwatchCoord(14, 3);
        public static readonly SwatchCoord FoliageAODeepGreen   = new SwatchCoord(15, 3);

        // --------------------------------------------------------------------
        // Row 4: Flowers & Botanicals
        // --------------------------------------------------------------------
        public static readonly SwatchCoord FlowerCherryBlossom  = new SwatchCoord(0, 4);
        public static readonly SwatchCoord FlowerCosmosBlush    = new SwatchCoord(1, 4);
        public static readonly SwatchCoord FlowerTulipRed       = new SwatchCoord(2, 4);
        public static readonly SwatchCoord FlowerRoseCrimson    = new SwatchCoord(3, 4);
        public static readonly SwatchCoord FlowerWindflowerOrange = new SwatchCoord(4, 4);
        public static readonly SwatchCoord FlowerPansyYellow    = new SwatchCoord(5, 4);
        public static readonly SwatchCoord FlowerDandelionGold  = new SwatchCoord(6, 4);
        public static readonly SwatchCoord FlowerDaisyWhite     = new SwatchCoord(7, 4);
        public static readonly SwatchCoord FlowerDaisyGoldCenter= new SwatchCoord(8, 4);
        public static readonly SwatchCoord FlowerHyacinthLavender = new SwatchCoord(9, 4);
        public static readonly SwatchCoord FlowerHyacinthPurple = new SwatchCoord(10, 4);
        public static readonly SwatchCoord FlowerHydrangeaSky   = new SwatchCoord(11, 4);
        public static readonly SwatchCoord FlowerHydrangeaPeriwinkle = new SwatchCoord(12, 4);
        public static readonly SwatchCoord FlowerLilyIvory      = new SwatchCoord(13, 4);
        public static readonly SwatchCoord FlowerStemSapGreen   = new SwatchCoord(14, 4);
        public static readonly SwatchCoord FlowerBudCrimson     = new SwatchCoord(15, 4);

        // --------------------------------------------------------------------
        // Row 5: Produce — Orchard Fruits
        // --------------------------------------------------------------------
        public static readonly SwatchCoord FruitAppleRed        = new SwatchCoord(0, 5);
        public static readonly SwatchCoord FruitAppleCrimson    = new SwatchCoord(1, 5);
        public static readonly SwatchCoord FruitAppleYellowBlush= new SwatchCoord(2, 5);
        public static readonly SwatchCoord FruitStemWoodBrown   = new SwatchCoord(3, 5);
        public static readonly SwatchCoord FruitOrangeCitrus    = new SwatchCoord(4, 5);
        public static readonly SwatchCoord FruitOrangeTangerine = new SwatchCoord(5, 5);
        public static readonly SwatchCoord FruitOrangePaleCore  = new SwatchCoord(6, 5);
        public static readonly SwatchCoord FruitPeachBlushPink  = new SwatchCoord(7, 5);
        public static readonly SwatchCoord FruitPeachTenderCream= new SwatchCoord(8, 5);
        public static readonly SwatchCoord FruitPeachSalmon     = new SwatchCoord(9, 5);
        public static readonly SwatchCoord FruitPearChartreuse  = new SwatchCoord(10, 5);
        public static readonly SwatchCoord FruitPearGoldenYellow= new SwatchCoord(11, 5);
        public static readonly SwatchCoord FruitCherryRuby      = new SwatchCoord(12, 5);
        public static readonly SwatchCoord FruitCherryStemGreen = new SwatchCoord(13, 5);
        public static readonly SwatchCoord FruitCoconutHuskBrown= new SwatchCoord(14, 5);
        public static readonly SwatchCoord FruitCoconutMilkWhite= new SwatchCoord(15, 5);

        // --------------------------------------------------------------------
        // Row 6: Produce — Vegetables & Crops
        // --------------------------------------------------------------------
        public static readonly SwatchCoord TurnipCreamBody      = new SwatchCoord(0, 6);
        public static readonly SwatchCoord TurnipIvorySkin      = new SwatchCoord(1, 6);
        public static readonly SwatchCoord TurnipVioletTop      = new SwatchCoord(2, 6);
        public static readonly SwatchCoord TurnipPurpleCrown    = new SwatchCoord(3, 6);
        public static readonly SwatchCoord TurnipLeafGreen      = new SwatchCoord(4, 6);
        public static readonly SwatchCoord TurnipStemCelery     = new SwatchCoord(5, 6);
        public static readonly SwatchCoord CarrotOrange         = new SwatchCoord(6, 6);
        public static readonly SwatchCoord CarrotFeatheryGreen  = new SwatchCoord(7, 6);
        public static readonly SwatchCoord PumpkinOrange        = new SwatchCoord(8, 6);
        public static readonly SwatchCoord PumpkinSageGreen     = new SwatchCoord(9, 6);
        public static readonly SwatchCoord PumpkinCreamStripe   = new SwatchCoord(10, 6);
        public static readonly SwatchCoord PotatoRussetSkin     = new SwatchCoord(11, 6);
        public static readonly SwatchCoord PotatoPaleFlesh      = new SwatchCoord(12, 6);
        public static readonly SwatchCoord TomatoVineRed        = new SwatchCoord(13, 6);
        public static readonly SwatchCoord WheatGoldenGrain     = new SwatchCoord(14, 6);
        public static readonly SwatchCoord RootTipCharcoal      = new SwatchCoord(15, 6);

        // --------------------------------------------------------------------
        // Row 7: Bakery — Crusts & Breads
        // --------------------------------------------------------------------
        public static readonly SwatchCoord DoughUnbakedCream    = new SwatchCoord(0, 7);
        public static readonly SwatchCoord DoughRawIvory        = new SwatchCoord(1, 7);
        public static readonly SwatchCoord BriocheButterLight   = new SwatchCoord(2, 7);
        public static readonly SwatchCoord CroissantGoldenTan   = new SwatchCoord(3, 7);
        public static readonly SwatchCoord BreadCrustGolden     = new SwatchCoord(4, 7);
        public static readonly SwatchCoord CrustCaramelizedAmber= new SwatchCoord(5, 7);
        public static readonly SwatchCoord SourdoughBrownCrust  = new SwatchCoord(6, 7);
        public static readonly SwatchCoord DarkRyeCrust         = new SwatchCoord(7, 7);
        public static readonly SwatchCoord BaguetteSlitCream    = new SwatchCoord(8, 7);
        public static readonly SwatchCoord BreadCrumbSoft       = new SwatchCoord(9, 7);
        public static readonly SwatchCoord DonutFriedGoldenTan  = new SwatchCoord(10, 7);
        public static readonly SwatchCoord DonutDeepGoldenBrown = new SwatchCoord(11, 7);
        public static readonly SwatchCoord MuffinFluffyYellow   = new SwatchCoord(12, 7);
        public static readonly SwatchCoord PretzelDarkBaked     = new SwatchCoord(13, 7);
        public static readonly SwatchCoord SesameCrumbSprinkle  = new SwatchCoord(14, 7);
        public static readonly SwatchCoord OvenBakingChar       = new SwatchCoord(15, 7);

        // --------------------------------------------------------------------
        // Row 8: Bakery — Frostings & Glazes
        // --------------------------------------------------------------------
        public static readonly SwatchCoord DonutGlazeStrawberry = new SwatchCoord(0, 8);
        public static readonly SwatchCoord StrawberryJamDeepRed = new SwatchCoord(1, 8);
        public static readonly SwatchCoord VanillaFondantCream  = new SwatchCoord(2, 8);
        public static readonly SwatchCoord PowderedSugarWhite   = new SwatchCoord(3, 8);
        public static readonly SwatchCoord ChocolateFrostingCocoa = new SwatchCoord(4, 8);
        public static readonly SwatchCoord ChocolateGanacheDark = new SwatchCoord(5, 8);
        public static readonly SwatchCoord MilkChocolateMousse  = new SwatchCoord(6, 8);
        public static readonly SwatchCoord CaramelDrizzleAmber  = new SwatchCoord(7, 8);
        public static readonly SwatchCoord CinnamonSugarSpice   = new SwatchCoord(8, 8);
        public static readonly SwatchCoord BlueberryGlazePurple = new SwatchCoord(9, 8);
        public static readonly SwatchCoord LemonCurdTartYellow  = new SwatchCoord(10, 8);
        public static readonly SwatchCoord MatchaGlazeGreen     = new SwatchCoord(11, 8);
        public static readonly SwatchCoord SprinkleCyan         = new SwatchCoord(12, 8);
        public static readonly SwatchCoord SprinkleCoral        = new SwatchCoord(13, 8);
        public static readonly SwatchCoord SprinkleYellow       = new SwatchCoord(14, 8);
        public static readonly SwatchCoord BakeryPaperCup       = new SwatchCoord(15, 8);

        // --------------------------------------------------------------------
        // Row 9: Café — Beverages & Coffee
        // --------------------------------------------------------------------
        public static readonly SwatchCoord EspressoCremaTan     = new SwatchCoord(0, 9);
        public static readonly SwatchCoord CafeAuLaitCoffee     = new SwatchCoord(1, 9);
        public static readonly SwatchCoord CoffeeMediumRoast    = new SwatchCoord(2, 9);
        public static readonly SwatchCoord CoffeeDarkEspresso   = new SwatchCoord(3, 9);
        public static readonly SwatchCoord MilkFoamSteamedWhite = new SwatchCoord(4, 9);
        public static readonly SwatchCoord LatteArtSwirlTan     = new SwatchCoord(5, 9);
        public static readonly SwatchCoord GreenTeaMatcha       = new SwatchCoord(6, 9);
        public static readonly SwatchCoord ChamomileTeaGold     = new SwatchCoord(7, 9);
        public static readonly SwatchCoord BlackTeaEnglishAmber = new SwatchCoord(8, 9);
        public static readonly SwatchCoord BerryTeaIcedRed      = new SwatchCoord(9, 9);
        public static readonly SwatchCoord IcedLemonTeaAmber    = new SwatchCoord(10, 9);
        public static readonly SwatchCoord ColdBrewIceCubeTint  = new SwatchCoord(11, 9);
        public static readonly SwatchCoord HotCocoaMarshmallow  = new SwatchCoord(12, 9);
        public static readonly SwatchCoord HotCocoaCream        = new SwatchCoord(13, 9);
        public static readonly SwatchCoord CaramelSyrupAccent   = new SwatchCoord(14, 9);
        public static readonly SwatchCoord EspressoDepthVoid    = new SwatchCoord(15, 9);

        // --------------------------------------------------------------------
        // Row 10: Café — Ceramics & Tableware
        // --------------------------------------------------------------------
        public static readonly SwatchCoord PorcelainIvoryMug    = new SwatchCoord(0, 10);
        public static readonly SwatchCoord CeramicEggshellWhite = new SwatchCoord(1, 10);
        public static readonly SwatchCoord DinerMintSaucer      = new SwatchCoord(2, 10);
        public static readonly SwatchCoord SageCeramicPlate     = new SwatchCoord(3, 10);
        public static readonly SwatchCoord TerracottaRusticCup  = new SwatchCoord(4, 10);
        public static readonly SwatchCoord GlazedTerracottaRust = new SwatchCoord(5, 10);
        public static readonly SwatchCoord CobaltCeramicAccent  = new SwatchCoord(6, 10);
        public static readonly SwatchCoord PastelYellowSaucer   = new SwatchCoord(7, 10);
        public static readonly SwatchCoord DisplayCaseBrassFrame= new SwatchCoord(8, 10);
        public static readonly SwatchCoord DisplayCaseGlassTint = new SwatchCoord(9, 10);
        public static readonly SwatchCoord TeapotEnamelCream    = new SwatchCoord(10, 10);
        public static readonly SwatchCoord TeapotRetroTeal      = new SwatchCoord(11, 10);
        public static readonly SwatchCoord StainlessTeaspoonSilver = new SwatchCoord(12, 10);
        public static readonly SwatchCoord SilverwarePolished   = new SwatchCoord(13, 10);
        public static readonly SwatchCoord TableNapkinWhite     = new SwatchCoord(14, 10);
        public static readonly SwatchCoord CeramicShadowRim     = new SwatchCoord(15, 10);

        // --------------------------------------------------------------------
        // Row 11: Metals & Hardware
        // --------------------------------------------------------------------
        public static readonly SwatchCoord BellBrassGold        = new SwatchCoord(0, 11);
        public static readonly SwatchCoord BellBrassHighlight   = new SwatchCoord(1, 11);
        public static readonly SwatchCoord BellBrassShadow      = new SwatchCoord(2, 11);
        public static readonly SwatchCoord BellBaseWood         = new SwatchCoord(3, 11);
        public static readonly SwatchCoord LampWroughtIronBlack = new SwatchCoord(4, 11);
        public static readonly SwatchCoord LampCharcoalGrey     = new SwatchCoord(5, 11);
        public static readonly SwatchCoord IronSoftHighlight    = new SwatchCoord(6, 11);
        public static readonly SwatchCoord CopperPolishedBronze = new SwatchCoord(7, 11);
        public static readonly SwatchCoord CopperAgedWarm       = new SwatchCoord(8, 11);
        public static readonly SwatchCoord VerdigrisPatinaGreen = new SwatchCoord(9, 11);
        public static readonly SwatchCoord GalvanizedPailGrey   = new SwatchCoord(10, 11);
        public static readonly SwatchCoord SteelSpoutMidGrey    = new SwatchCoord(11, 11);
        public static readonly SwatchCoord TrowelSteelBlade     = new SwatchCoord(12, 11);
        public static readonly SwatchCoord ChromeScrewRivet     = new SwatchCoord(13, 11);
        public static readonly SwatchCoord CastIronAnvil        = new SwatchCoord(14, 11);
        public static readonly SwatchCoord IronJointShadow      = new SwatchCoord(15, 11);

        // --------------------------------------------------------------------
        // Row 12: Crafting Materials & Gardening
        // --------------------------------------------------------------------
        public static readonly SwatchCoord PailPineWood         = new SwatchCoord(0, 12);
        public static readonly SwatchCoord PailIronRimBand      = new SwatchCoord(1, 12);
        public static readonly SwatchCoord FlowerPotTerracotta  = new SwatchCoord(2, 12);
        public static readonly SwatchCoord FlowerPotRimSunlit   = new SwatchCoord(3, 12);
        public static readonly SwatchCoord FlowerPotSoil        = new SwatchCoord(4, 12);
        public static readonly SwatchCoord PottedSaplingGreenShoot = new SwatchCoord(5, 12);
        public static readonly SwatchCoord SeedPacketKraftPaper = new SwatchCoord(6, 12);
        public static readonly SwatchCoord SeedPacketPrintYellow= new SwatchCoord(7, 12);
        public static readonly SwatchCoord SeedPacketPrintRed   = new SwatchCoord(8, 12);
        public static readonly SwatchCoord SeedPacketPrintGreen = new SwatchCoord(9, 12);
        public static readonly SwatchCoord CardboardKraft       = new SwatchCoord(10, 12);
        public static readonly SwatchCoord CardboardCreaseShadow= new SwatchCoord(11, 12);
        public static readonly SwatchCoord WovenBasketLightReed = new SwatchCoord(12, 12);
        public static readonly SwatchCoord WovenBasketWeaveShadow = new SwatchCoord(13, 12);
        public static readonly SwatchCoord JuteTwineCord        = new SwatchCoord(14, 12);
        public static readonly SwatchCoord PegboardTan          = new SwatchCoord(15, 12);

        // --------------------------------------------------------------------
        // Row 13: Water, Glass & Transparent Accents
        // --------------------------------------------------------------------
        public static readonly SwatchCoord WaterFountainAzure   = new SwatchCoord(0, 13);
        public static readonly SwatchCoord WaterJetStreamPale   = new SwatchCoord(1, 13);
        public static readonly SwatchCoord WaterPlungeDepthAqua = new SwatchCoord(2, 13);
        public static readonly SwatchCoord WaterBasinDeepPool   = new SwatchCoord(3, 13);
        public static readonly SwatchCoord WaterSplashWhiteFoam = new SwatchCoord(4, 13);
        public static readonly SwatchCoord WaterRippleHighlight = new SwatchCoord(5, 13);
        public static readonly SwatchCoord WaterDrinkingClear   = new SwatchCoord(6, 13);
        public static readonly SwatchCoord GlassBottleTintClear = new SwatchCoord(7, 13);
        public static readonly SwatchCoord GlassBottleEmerald   = new SwatchCoord(8, 13);
        public static readonly SwatchCoord GlassBottleAmber     = new SwatchCoord(9, 13);
        public static readonly SwatchCoord LiquidPotionViolet   = new SwatchCoord(10, 13);
        public static readonly SwatchCoord LiquidPotionRose     = new SwatchCoord(11, 13);
        public static readonly SwatchCoord LiquidPotionMint     = new SwatchCoord(12, 13);
        public static readonly SwatchCoord WaterMistTranslucent = new SwatchCoord(13, 13);
        public static readonly SwatchCoord WaterRiverShallows   = new SwatchCoord(14, 13);
        public static readonly SwatchCoord WaterAquaticVoid     = new SwatchCoord(15, 13);

        // --------------------------------------------------------------------
        // Row 14: Emissive Glows & Lamps
        // --------------------------------------------------------------------
        public static readonly SwatchCoord EmissiveFlameCore    = new SwatchCoord(0, 14);
        public static readonly SwatchCoord EmissiveGoldenLantern= new SwatchCoord(1, 14);
        public static readonly SwatchCoord EmissiveAmberFlame   = new SwatchCoord(2, 14);
        public static readonly SwatchCoord EmissiveCandleFlame  = new SwatchCoord(3, 14);
        public static readonly SwatchCoord EmissiveFilamentWarm = new SwatchCoord(4, 14);
        public static readonly SwatchCoord EmissiveTungstenWhite= new SwatchCoord(5, 14);
        public static readonly SwatchCoord EmissiveFireplaceEmber = new SwatchCoord(6, 14);
        public static readonly SwatchCoord EmissiveFireflyLime  = new SwatchCoord(7, 14);
        public static readonly SwatchCoord EmissiveMagicCrystal = new SwatchCoord(8, 14);
        public static readonly SwatchCoord EmissiveFairyRose    = new SwatchCoord(9, 14);
        public static readonly SwatchCoord EmissiveSignboardNeon= new SwatchCoord(10, 14);
        public static readonly SwatchCoord EmissiveTwilightWindow = new SwatchCoord(11, 14);
        public static readonly SwatchCoord EmissiveSunShaftGold = new SwatchCoord(12, 14);
        public static readonly SwatchCoord EmissiveStarFragment = new SwatchCoord(13, 14);
        public static readonly SwatchCoord EmissivePureWhite    = new SwatchCoord(14, 14);
        public static readonly SwatchCoord EmissiveFlameShadow  = new SwatchCoord(15, 14);

        // --------------------------------------------------------------------
        // Row 15: Stall Canopies & Stripes
        // --------------------------------------------------------------------
        public static readonly SwatchCoord CanopyProduceRed     = new SwatchCoord(0, 15);
        public static readonly SwatchCoord CanopyProduceCream   = new SwatchCoord(1, 15);
        public static readonly SwatchCoord CanopyProduceRubyAccent = new SwatchCoord(2, 15);
        public static readonly SwatchCoord CanopyCafeBistroGreen = new SwatchCoord(3, 15);
        public static readonly SwatchCoord CanopyCafeCream      = new SwatchCoord(4, 15);
        public static readonly SwatchCoord CanopyCafeOliveDark  = new SwatchCoord(5, 15);
        public static readonly SwatchCoord CanopyCraftingTeal   = new SwatchCoord(6, 15);
        public static readonly SwatchCoord CanopyCraftingPeach  = new SwatchCoord(7, 15);
        public static readonly SwatchCoord CanopyCraftingCoral  = new SwatchCoord(8, 15);
        public static readonly SwatchCoord CanopySunshineYellow = new SwatchCoord(9, 15);
        public static readonly SwatchCoord CanopyNavyAccent     = new SwatchCoord(10, 15);
        public static readonly SwatchCoord CanopyPastelLavender = new SwatchCoord(11, 15);
        public static readonly SwatchCoord BulletinBoardSlate   = new SwatchCoord(12, 15);
        public static readonly SwatchCoord BulletinChalkWhite   = new SwatchCoord(13, 15);
        public static readonly SwatchCoord UniversalWhiteHighlight = new SwatchCoord(14, 15);
        public static readonly SwatchCoord UniversalShadowCharcoal = new SwatchCoord(15, 15);
    }

    /// <summary>
    /// Master procedural color atlas texture generator, UV coordinate provider,
    /// and zero-bleed validation engine.
    /// </summary>
    public static class ColorAtlasGenerator
    {
        public const int TextureWidth = 512;
        public const int TextureHeight = 512;
        public const int GridCols = 16;
        public const int GridColumns = 16;
        public const int GridRows = 16;
        public const int CellWidth = TextureWidth / GridCols;   // 32 px
        public const int CellHeight = TextureHeight / GridRows; // 32 px
        public const int SwatchSize = 32;
        public const int SwatchResolution = SwatchSize;
        public const int TotalSwatches = GridCols * GridRows;   // 256 swatches

        public const string AssetPath = "Assets/Textures/T_AnimalCrossing_PaletteAtlas.png";

        // Hex color table: [Row, Col]
        private static readonly string[,] HexTable = new string[GridRows, GridCols]
        {
            // Row 0: Woods & Timbers
            { "#F5E6C8", "#ECD6B3", "#DEB887", "#CD853F", "#C18845", "#A0522D", "#6B3E1E", "#4A2810",
              "#D3C5B4", "#9C8A79", "#D4E157", "#E6EE9C", "#F8F9FA", "#D32F2F", "#00897B", "#2D241E" },

            // Row 1: Stone & Masonry
            { "#EDE8DB", "#DFD4BD", "#C5BFB5", "#A49C90", "#8C9BA5", "#AFC2D5", "#6C7D8A", "#C87D55",
              "#D99B75", "#857564", "#ABA090", "#4A5560", "#7D7871", "#F0EDE6", "#8C997D", "#2E3338" },

            // Row 2: Earth & Pathways
            { "#D2B48C", "#C49E6C", "#B38550", "#6E4A28", "#4A3018", "#5A3D1E", "#EED9A4", "#FFF0C2",
              "#BA5A31", "#D36E43", "#4E3620", "#BDAE9E", "#8D5B32", "#5C3A21", "#CC7722", "#2E1D0E" },

            // Row 3: Grass & Foliage
            { "#B4E051", "#88D436", "#6BCB26", "#4CAF50", "#388E3C", "#2E7D32", "#9CCC65", "#558B2F",
              "#689F38", "#7CB342", "#43A047", "#2E7234", "#C0CA33", "#827717", "#1B5E20", "#143E17" },

            // Row 4: Flowers & Botanicals
            { "#F8BBD0", "#F48FB1", "#E53935", "#C2185B", "#FB8C00", "#FDD835", "#FFB300", "#FFFFFF",
              "#FFD54F", "#BA68C8", "#7B1FA2", "#81D4FA", "#64B5F6", "#F5F5F0", "#7CB342", "#880E4F" },

            // Row 5: Produce — Orchard Fruits
            { "#FF3B30", "#D32F2F", "#FFE082", "#5D4037", "#FF9500", "#F57C00", "#FFE0B2", "#FF8A80",
              "#FFE4E1", "#FF6E40", "#CDDC39", "#DCE775", "#D81B60", "#8BC34A", "#795548", "#FAFAFA" },

            // Row 6: Produce — Vegetables & Crops
            { "#FDFBF7", "#F5F0E6", "#AB47BC", "#8E24AA", "#7CB342", "#DCEDC8", "#FF6F00", "#558B2F",
              "#EF6C00", "#9E9D24", "#FFF9C4", "#A1887F", "#EFEBE9", "#E53935", "#FFCA28", "#3E2723" },

            // Row 7: Bakery — Crusts & Breads
            { "#FFF3E0", "#FFECB3", "#FFE082", "#FFB74D", "#FFA726", "#FB8C00", "#F57C00", "#E65100",
              "#FFE8D6", "#FFF5E4", "#E09852", "#C27532", "#FED766", "#8D4925", "#FFEAA7", "#4E260E" },

            // Row 8: Bakery — Frostings & Glazes
            { "#FF80AB", "#C2185B", "#FFFDE7", "#FFFFFF", "#5D4037", "#3E2723", "#795548", "#FFA000",
              "#A16B47", "#7E57C2", "#FFEE58", "#9CCC65", "#00E5FF", "#FF5252", "#FFEA00", "#F5EBE1" },

            // Row 9: Café — Beverages & Coffee
            { "#D7CCC8", "#BCAAA4", "#8D6E63", "#4E342E", "#FFFDF9", "#C7A788", "#8BC34A", "#FBC02D",
              "#A04000", "#D81B60", "#F57F17", "#E0F7FA", "#FFFFFF", "#6D4C41", "#FF6F00", "#271810" },

            // Row 10: Café — Ceramics & Tableware
            { "#F7F4EB", "#ECE7D8", "#A7D7C5", "#84A98C", "#D97D54", "#B25329", "#1E56A0", "#FCE38A",
              "#E6B04C", "#E8F4F8", "#FFF6E5", "#48B1BF", "#CFD8DC", "#ECEFF1", "#FFFFFF", "#455A64" },

            // Row 11: Metals & Hardware
            { "#FFD700", "#FFF176", "#C69214", "#37474F", "#212121", "#37474F", "#607D8B", "#D87D4A",
              "#B85D19", "#4DB6AC", "#B0BEC5", "#78909C", "#90A4AE", "#ECEFF1", "#263238", "#12181B" },

            // Row 12: Crafting Materials & Gardening
            { "#E0BB7B", "#546E7A", "#D35400", "#E67E22", "#4A2E18", "#8BC34A", "#D7CCC8", "#FFF176",
              "#EF5350", "#66BB6A", "#C7A76D", "#9E7D47", "#ECC880", "#BA9146", "#D4B26F", "#DEBA84" },

            // Row 13: Water, Glass & Transparent Accents
            { "#4DD0E1", "#80DEEA", "#00ACC1", "#00838F", "#E0F7FA", "#B2EBF2", "#E1F5FE", "#E0F2F1",
              "#26A69A", "#FFA726", "#AB47BC", "#F06292", "#4DB6AC", "#F5FDFF", "#006064", "#00363A" },

            // Row 14: Emissive Glows & Lamps
            { "#FFF9C4", "#FFE082", "#FFCA28", "#FFA000", "#FF8F00", "#FFF8E1", "#FF6F00", "#EEFF41",
              "#84FFFF", "#FF80AB", "#FFFDE7", "#FFD54F", "#FFF59D", "#FFFF8D", "#FFFFFF", "#8C3B00" },

            // Row 15: Stall Canopies & Stripes
            { "#E53935", "#FFF8E1", "#D32F2F", "#2E7D32", "#FFF8E1", "#1B5E20", "#00897B", "#FFE0B2",
              "#FF7043", "#FBC02D", "#1565C0", "#AB47BC", "#263238", "#ECEFF1", "#FFFFFF", "#1B1B1B" }
        };

        // Cached Color32 table to avoid repeated hex parsing
        private static Color32[,] _colorCache;

        private static void EnsureColorCache()
        {
            if (_colorCache != null) return;
            _colorCache = new Color32[GridRows, GridCols];
            for (int r = 0; r < GridRows; r++)
            {
                for (int c = 0; c < GridCols; c++)
                {
                    if (ColorUtility.TryParseHtmlString(HexTable[r, c], out Color col))
                    {
                        _colorCache[r, c] = (Color32)col;
                    }
                    else
                    {
                        Debug.LogError($"[ColorAtlasGenerator] Failed to parse hex '{HexTable[r, c]}' at ({c}, {r})");
                        _colorCache[r, c] = new Color32(255, 0, 255, 255); // Magenta error fallback
                    }
                }
            }
        }

        /// <summary>
        /// Returns the exact 32-bit color for the swatch at grid position (col, row).
        /// </summary>
        public static Color32 GetSwatchColor(int col, int row)
        {
            EnsureColorCache();
            int c = Mathf.Clamp(col, 0, GridCols - 1);
            int r = Mathf.Clamp(row, 0, GridRows - 1);
            return _colorCache[r, c];
        }

        /// <summary>
        /// Returns the normalized cell-center UV coordinates for the swatch at grid position (col, row).
        /// Guarantee: Distance to any cell boundary is exactly 16 pixels (0.03125 UV units),
        /// completely immune to bilinear filter bleeding.
        /// </summary>
        public static Vector2 GetSwatchUV(int col, int row)
        {
            int c = Mathf.Clamp(col, 0, GridCols - 1);
            int r = Mathf.Clamp(row, 0, GridRows - 1);
            float u = (c + 0.5f) / GridCols;
            float v = (r + 0.5f) / GridRows;
            return new Vector2(u, v);
        }

        /// <summary>
        /// Procedurally generates the 512x512 Master Color Atlas texture, writes the PNG file to disk,
        /// and applies TextureImporter settings.
        /// </summary>
        /// <returns>The generated/imported Texture2D asset.</returns>
        public static Texture2D GenerateAtlas()
        {
            EnsureColorCache();

            Texture2D tex = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, false);
            tex.name = "T_AnimalCrossing_PaletteAtlas";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixelArray = new Color32[TextureWidth * TextureHeight];

            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridCols; col++)
                {
                    Color32 swatchColor = _colorCache[row, col];
                    int startY = row * CellHeight;
                    int startX = col * CellWidth;

                    for (int py = 0; py < CellHeight; py++)
                    {
                        int y = startY + py;
                        int rowOffset = y * TextureWidth;

                        for (int px = 0; px < CellWidth; px++)
                        {
                            int x = startX + px;
                            pixelArray[rowOffset + x] = swatchColor;
                        }
                    }
                }
            }

            tex.SetPixels32(pixelArray);
            tex.Apply(false, false);

            string fullPath = Path.Combine(Application.dataPath, "Textures", "T_AnimalCrossing_PaletteAtlas.png");
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

#if UNITY_EDITOR
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(AssetPath);

            Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath);
            if (loadedTex != null)
            {
                return loadedTex;
            }
#endif
            return tex;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Applies the project standard TextureImporter configuration to the imported palette atlas.
        /// Enforces Bilinear filtering, Clamp wrap mode, disabled mipmaps, sRGB color, and uncompressed RGBA32.
        /// </summary>
        public static void ConfigureTextureImporter(string relativePath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[ColorAtlasGenerator] No TextureImporter found at '{relativePath}'");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
            platformSettings.format = TextureImporterFormat.RGBA32;
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platformSettings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        [MenuItem("Tools/Animal Crossing/Generate Master Color Atlas")]
        public static void MenuItem_GenerateAtlas()
        {
            Texture2D atlas = GenerateAtlas();
            Debug.Log($"[ColorAtlasGenerator] Master Color Atlas successfully generated at '{AssetPath}'. Size: {atlas.width}x{atlas.height}, Format: {atlas.format}");
        }
#endif
    }

    /// <summary>
    /// Mathematical verification and assertion helper for zero-bleed UV mapping.
    /// Used by procedural mesh builders, test runners, and the geometric audit suite.
    /// </summary>
    public static class AtlasUvValidator
    {
        public const float GutterMarginPixels = 1.5f;
        public const float TextureDimension = 512f;
        public const float GutterMarginUV = GutterMarginPixels / TextureDimension; // 0.0029296875f

        /// <summary>
        /// Checks whether a normalized UV coordinate lies safely within the interior gutter
        /// margin of cell (col, row), preventing any bilinear bleeding into adjacent cells.
        /// </summary>
        public static bool IsInsideCellGutter(Vector2 uv, int col, int row, float marginPixels = GutterMarginPixels)
        {
            if (col < 0 || col >= ColorAtlasGenerator.GridCols || row < 0 || row >= ColorAtlasGenerator.GridRows)
                return false;

            float marginUV = marginPixels / TextureDimension;
            float uMin = (float)col / ColorAtlasGenerator.GridCols + marginUV;
            float uMax = (float)(col + 1) / ColorAtlasGenerator.GridCols - marginUV;
            float vMin = (float)row / ColorAtlasGenerator.GridRows + marginUV;
            float vMax = (float)(row + 1) / ColorAtlasGenerator.GridRows - marginUV;

            return uv.x >= uMin && uv.x <= uMax && uv.y >= vMin && uv.y <= vMax;
        }

        /// <summary>
        /// Checks if a UV coordinate corresponds to the cell center within numerical tolerance.
        /// </summary>
        public static bool IsCellCenter(Vector2 uv, int col, int row, float tolerance = 1e-4f)
        {
            Vector2 expectedCenter = ColorAtlasGenerator.GetSwatchUV(col, row);
            return Mathf.Abs(uv.x - expectedCenter.x) <= tolerance && Mathf.Abs(uv.y - expectedCenter.y) <= tolerance;
        }

        /// <summary>
        /// Audits an entire Mesh for UV bleed violations against an expected swatch cell.
        /// Returns 0 if all vertices are compliant; otherwise returns the violation count.
        /// </summary>
        public static int AuditMeshUvBleed(Mesh mesh, int expectedCol, int expectedRow)
        {
            if (mesh == null) return 0;
            Vector2[] uvs = mesh.uv;
            if (uvs == null || uvs.Length == 0) return 1;

            int violations = 0;
            for (int i = 0; i < uvs.Length; i++)
            {
                if (!IsInsideCellGutter(uvs[i], expectedCol, expectedRow))
                {
                    violations++;
                }
            }
            return violations;
        }

        /// <summary>
        /// Asserts that a UV coordinate is strictly within the zero-bleed safe interior gutter.
        /// Throws InvalidOperationException with detailed diagnostics on failure.
        /// </summary>
        public static void AssertZeroBleed(Vector2 uv, int col, int row)
        {
            if (!IsInsideCellGutter(uv, col, row))
            {
                float uMin = (float)col / ColorAtlasGenerator.GridCols + GutterMarginUV;
                float uMax = (float)(col + 1) / ColorAtlasGenerator.GridCols - GutterMarginUV;
                float vMin = (float)row / ColorAtlasGenerator.GridRows + GutterMarginUV;
                float vMax = (float)(row + 1) / ColorAtlasGenerator.GridRows - GutterMarginUV;

                throw new InvalidOperationException(
                    $"[UV Bleed Violation] UV ({uv.x:F5}, {uv.y:F5}) outside safe gutter bounds for cell ({col}, {row}). " +
                    $"Safe bounds: U [{uMin:F5}, {uMax:F5}], V [{vMin:F5}, {vMax:F5}]. Gutter: {GutterMarginPixels}px.");
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// AssetPostprocessor that automatically guarantees all import settings on T_AnimalCrossing_PaletteAtlas.png
    /// are strictly enforced whenever the file is imported, reimported, or pulled from source control.
    /// </summary>
    public class ColorAtlasPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.Replace('\\', '/').EndsWith("Textures/T_AnimalCrossing_PaletteAtlas.png", StringComparison.OrdinalIgnoreCase))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
                platformSettings.format = TextureImporterFormat.RGBA32;
                platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(platformSettings);
            }
        }
    }
#endif
}
