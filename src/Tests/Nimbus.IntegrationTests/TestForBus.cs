﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nimbus.Configuration;
using Nimbus.Extensions;
using Nimbus.IntegrationTests.Extensions;
using Nimbus.Tests.Common.Extensions;
using Nimbus.Tests.Common.TestScenarioGeneration.ScenarioComposition;
using Nimbus.Tests.Common.TestUtilities;
using NUnit.Framework;

namespace Nimbus.IntegrationTests
{
    [TestFixture]
    [Timeout(TimeoutSeconds*1000)]
    public abstract class TestForBus
    {
        protected const int TimeoutSeconds = 60;
        protected readonly TimeSpan Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

        protected ScenarioInstance<BusBuilderConfiguration> Instance { get; private set; }
        protected Bus Bus { get; private set; }

        private bool _thenWasInvoked;

        protected virtual async Task Given(IConfigurationScenario<BusBuilderConfiguration> scenario)
        {
            MethodCallCounter.Clear();

            Instance = scenario.CreateInstance();
            Reconfigure();

            Bus = Instance.Configuration.Build();
            await Bus.Start();
        }

        protected virtual void Reconfigure()
        {
        }

        protected abstract Task When();

        protected async Task Then()
        {
            _thenWasInvoked = true;

            var assertionMethods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                            .Where(m => m.HasAttribute<ThenAttribute>())
                                            .ToArray();

            await assertionMethods
                .Select(m => (Task) m.Invoke(this, new object[0]))
                .WhenAll();
        }

        [SetUp]
        public void SetUp()
        {
            _thenWasInvoked = false;
            TestLoggingExtensions.LogTestStart();
        }

        [TearDown]
        public void TearDown()
        {
            var bus = Bus;
            Bus = null;

            bus?.Stop().Wait();
            bus?.Dispose();

            Instance?.Dispose();
            Instance = null;

            if (!_thenWasInvoked) Assert.Fail("It looks like we forgot to call our Then() method.");
            TestLoggingExtensions.LogTestResult();
        }
    }
}