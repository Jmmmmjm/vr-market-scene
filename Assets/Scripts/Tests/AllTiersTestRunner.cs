// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllTiersTestRunner.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Automated EditMode test harness executing all test tiers (Tiers 1-4) synchronously with reflection.
//   Provides immediate, deterministic pass/fail reporting for continuous validation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace AnimalCrossing.Tests
{
    public static class AllTiersTestRunner
    {
        public struct TestRunSummary
        {
            public int TotalTests;
            public int PassedTests;
            public int FailedTests;
            public int IgnoredTests;
            public List<string> FailureMessages;
        }

        [MenuItem("AnimalCrossing/Run All Tests (Tiers 1-4)", priority = 50)]
        public static void MenuItem_RunAllTests()
        {
            RunAllTests();
        }

        [MenuItem("AnimalCrossing/Run Tier 1 Tests", priority = 51)]
        public static void MenuItem_RunTier1() => RunSingleFixture(typeof(Tier1_FeatureCoverageTests));

        [MenuItem("AnimalCrossing/Run Tier 2 Tests", priority = 52)]
        public static void MenuItem_RunTier2() => RunSingleFixture(typeof(Tier2_BoundaryCornerTests));

        [MenuItem("AnimalCrossing/Run Tier 3 Tests", priority = 53)]
        public static void MenuItem_RunTier3() => RunSingleFixture(typeof(Tier3_CrossFeatureInteractions));

        [MenuItem("AnimalCrossing/Run Tier 4 Tests", priority = 54)]
        public static void MenuItem_RunTier4() => RunSingleFixture(typeof(Tier4_ApplicationScenarios));

        public static TestRunSummary RunSingleFixture(Type fixtureType)
        {
            var summary = new TestRunSummary
            {
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                IgnoredTests = 0,
                FailureMessages = new List<string>()
            };
            TestContracts.EnsureSceneContext();
            RunFixture(fixtureType, ref summary);
            if (summary.FailedTests == 0)
                Debug.Log($"[{fixtureType.Name}] ALL PASSED! Total: {summary.TotalTests}, Passed: {summary.PassedTests}, Failed: 0.");
            else
                Debug.LogError($"[{fixtureType.Name}] FAILED! Total: {summary.TotalTests}, Passed: {summary.PassedTests}, Failed: {summary.FailedTests}.\n" + string.Join("\n", summary.FailureMessages));
            return summary;
        }

        public static TestRunSummary RunAllTests()
        {
            var summary = new TestRunSummary
            {
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                IgnoredTests = 0,
                FailureMessages = new List<string>()
            };

            Debug.Log("[AllTiersTestRunner] ==================================================");
            Debug.Log("[AllTiersTestRunner] Starting Full Test Suite Execution (Tiers 1-4)");
            Debug.Log("[AllTiersTestRunner] ==================================================");

            TestContracts.EnsureSceneContext();

            Type[] targetFixtures = new Type[]
            {
                typeof(Tier1_FeatureCoverageTests),
                typeof(Tier2_BoundaryCornerTests),
                typeof(Tier3_CrossFeatureInteractions),
                typeof(Tier4_ApplicationScenarios)
            };

            foreach (var fixtureType in targetFixtures)
            {
                RunFixture(fixtureType, ref summary);
            }

            if (summary.FailedTests == 0)
            {
                Debug.Log($"[AllTiersTestRunner] FULL SUITE PASSED! Total: {summary.TotalTests}, Passed: {summary.PassedTests}, Failed: 0, Ignored: {summary.IgnoredTests}.");
            }
            else
            {
                Debug.LogError($"[AllTiersTestRunner] SUITE FAILED! Total: {summary.TotalTests}, Passed: {summary.PassedTests}, Failed: {summary.FailedTests}.\n" +
                               string.Join("\n", summary.FailureMessages));
            }

            return summary;
        }

        public static void RunFixture(Type fixtureType, ref TestRunSummary summary)
        {
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(fixtureType);
            }
            catch (Exception ex)
            {
                summary.FailedTests++;
                summary.FailureMessages.Add($"[Fixture Instantiation Failed] {fixtureType.Name}: {ex.Message}");
                return;
            }

            // Find lifecycle methods
            MethodInfo oneTimeSetUp = FindMethodWithAttribute(fixtureType, typeof(OneTimeSetUpAttribute));
            MethodInfo oneTimeTearDown = FindMethodWithAttribute(fixtureType, typeof(OneTimeTearDownAttribute));
            MethodInfo setUp = FindMethodWithAttribute(fixtureType, typeof(SetUpAttribute));
            MethodInfo tearDown = FindMethodWithAttribute(fixtureType, typeof(TearDownAttribute));

            // Execute OneTimeSetUp
            if (oneTimeSetUp != null)
            {
                try
                {
                    oneTimeSetUp.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    summary.FailedTests++;
                    summary.FailureMessages.Add($"[{fixtureType.Name} OneTimeSetUp Failed] {ex.InnerException?.Message ?? ex.Message}");
                    return;
                }
            }

            // Find all [Test] methods
            var methods = fixtureType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int fixturePassed = 0;
            int fixtureFailed = 0;

            foreach (var m in methods)
            {
                if (m.GetCustomAttribute<TestAttribute>() == null) continue;

                summary.TotalTests++;

                // SetUp
                if (setUp != null)
                {
                    try
                    {
                        setUp.Invoke(instance, null);
                    }
                    catch (Exception ex)
                    {
                        summary.FailedTests++;
                        fixtureFailed++;
                        summary.FailureMessages.Add($"[{fixtureType.Name}.{m.Name} SetUp Error]: {ex.InnerException?.Message ?? ex.Message}");
                        continue;
                    }
                }

                // Execute Test
                bool passed = false;
                try
                {
                    m.Invoke(instance, null);
                    passed = true;
                }
                catch (TargetInvocationException tie)
                {
                    Exception actual = tie.InnerException ?? tie;
                    if (actual.GetType().Name == "IgnoreException")
                    {
                        summary.IgnoredTests++;
                        passed = true;
                    }
                    else
                    {
                        summary.FailedTests++;
                        fixtureFailed++;
                        summary.FailureMessages.Add($"[{fixtureType.Name}.{m.Name} FAILED]: {actual.Message}");
                    }
                }
                catch (Exception ex)
                {
                    summary.FailedTests++;
                    fixtureFailed++;
                    summary.FailureMessages.Add($"[{fixtureType.Name}.{m.Name} ERROR]: {ex.Message}");
                }

                if (passed)
                {
                    summary.PassedTests++;
                    fixturePassed++;
                }

                // TearDown
                if (tearDown != null)
                {
                    try
                    {
                        tearDown.Invoke(instance, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[{fixtureType.Name}.{m.Name} TearDown Warning]: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }

            // OneTimeTearDown
            if (oneTimeTearDown != null)
            {
                try
                {
                    oneTimeTearDown.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{fixtureType.Name} OneTimeTearDown Warning]: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            Debug.Log($"[AllTiersTestRunner] {fixtureType.Name}: {fixturePassed} passed, {fixtureFailed} failed (Total: {fixturePassed + fixtureFailed})");
        }

        private static MethodInfo FindMethodWithAttribute(Type type, Type attributeType)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var m in methods)
            {
                if (m.GetCustomAttribute(attributeType) != null) return m;
            }
            return null;
        }
    }
}
