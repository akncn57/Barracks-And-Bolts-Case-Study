using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BarracksAndBolts.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BarracksAndBolts.Tests.EditMode.Core
{
    public class EventBusTests
    {
        private readonly struct TestEvent : IGameEvent
        {
            public TestEvent(int value) => Value = value;
            public int Value { get; }
        }

        private readonly struct OtherEvent : IGameEvent
        {
        }

        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [Test]
        public void Publish_DeliversEventToSubscriber()
        {
            int received = 0;
            _bus.Subscribe<TestEvent>(e => received = e.Value);

            _bus.Publish(new TestEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent(1)));
        }

        [Test]
        public void Publish_OnlyReachesSubscribersOfThatEventType()
        {
            bool otherReceived = false;
            _bus.Subscribe<OtherEvent>(_ => otherReceived = true);

            _bus.Publish(new TestEvent(1));

            Assert.IsFalse(otherReceived);
        }

        [Test]
        public void Publish_ReachesHandlersInSubscriptionOrder()
        {
            var order = new List<int>();
            _bus.Subscribe<TestEvent>(_ => order.Add(1));
            _bus.Subscribe<TestEvent>(_ => order.Add(2));

            _bus.Publish(new TestEvent(0));

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
        }

        [Test]
        public void Dispose_StopsDelivery()
        {
            int count = 0;
            IDisposable subscription = _bus.Subscribe<TestEvent>(_ => count++);

            subscription.Dispose();
            _bus.Publish(new TestEvent(1));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            IDisposable subscription = _bus.Subscribe<TestEvent>(_ => { });

            subscription.Dispose();

            Assert.DoesNotThrow(subscription.Dispose);
        }

        [Test]
        public void Publish_HandlerDisposedByEarlierHandler_IsSkipped()
        {
            IDisposable second = null;
            bool secondReceived = false;
            _bus.Subscribe<TestEvent>(_ => second.Dispose());
            second = _bus.Subscribe<TestEvent>(_ => secondReceived = true);

            _bus.Publish(new TestEvent(1));

            Assert.IsFalse(secondReceived);
        }

        [Test]
        public void Publish_HandlerSubscribedDuringPublish_ReceivesOnlyNextEvent()
        {
            int lateCount = 0;
            bool hasSubscribed = false;
            _bus.Subscribe<TestEvent>(_ =>
            {
                if (hasSubscribed)
                    return;

                hasSubscribed = true;
                _bus.Subscribe<TestEvent>(lateEvent => lateCount++);
            });

            _bus.Publish(new TestEvent(1));
            Assert.AreEqual(0, lateCount);

            _bus.Publish(new TestEvent(2));
            Assert.AreEqual(1, lateCount);
        }

        [Test]
        public void Publish_HandlerThrows_OtherHandlersStillRun()
        {
            bool secondReceived = false;
            _bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
            _bus.Subscribe<TestEvent>(_ => secondReceived = true);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            _bus.Publish(new TestEvent(1));

            Assert.IsTrue(secondReceived);
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe<TestEvent>(null));
        }
    }
}
