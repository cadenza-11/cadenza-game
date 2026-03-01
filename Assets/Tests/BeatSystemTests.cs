using NUnit.Framework;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Cadenza.Tests
{
    public class BeatSystemTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        private GameObject testObject;
        private BeatSystem beatSystem;

        [SetUp]
        public void SetUp()
        {
            this.testObject = new GameObject(nameof(BeatSystemTests));
            this.beatSystem = this.testObject.AddComponent<BeatSystem>();

            SetStaticField("singleton", this.beatSystem);
            SetInstanceField(this.beatSystem, "beatPeriod", 0.5d);
            SetInstanceField(this.beatSystem, "timelineInfo", new TimelineInfo());
        }

        [TearDown]
        public void TearDown()
        {
            ClearStaticEvent("BeatPlayed");
            ClearStaticEvent("MeasurePassed");
            ClearStaticEvent("MarkerPassed");
            ClearStaticEvent("UpBeatPlayed");
            ClearStaticEvent("TempoChanged");
            ClearStaticEvent("OffsetChanged");
            SetStaticField("singleton", null);

            if (this.testObject != null)
                Object.DestroyImmediate(this.testObject);
        }

        [Test]
        public void GetClosestBeat_RoundsToNearestBeatBoundary()
        {
            Assert.AreEqual(0, BeatSystem.GetClosestBeat(0.24d));
            Assert.AreEqual(1, BeatSystem.GetClosestBeat(0.26d));
            Assert.AreEqual(2, BeatSystem.GetClosestBeat(1.24d));
            Assert.AreEqual(3, BeatSystem.GetClosestBeat(1.26d));
        }

        [Test]
        public void WaitForNextBeatAsync_CompletesWhenBeatIsPlayed()
        {
            this.SetCurrentBeat(2);

            Task waitTask = BeatSystem.WaitForNextBeatAsync();

            this.InvokePrivateMethod("OnFixedBeat");

            Assert.IsTrue(waitTask.IsCompleted);
        }

        [Test]
        public void WaitForNextMeasureAsync_CompletesWhenMeasureBoundaryPasses()
        {
            this.SetCurrentBeat(1);

            Task waitTask = BeatSystem.WaitForNextMeasureAsync();

            this.InvokePrivateMethod("OnFixedBeat");

            Assert.IsTrue(waitTask.IsCompleted);
        }

        [Test]
        public void WaitForMarkerAsync_OnlyCompletesForMatchingMarker()
        {
            Task waitTask = BeatSystem.WaitForMarkerAsync("chorus");

            RaiseMarkerPassed("verse");
            Assert.IsFalse(waitTask.IsCompleted);

            RaiseMarkerPassed("chorus");
            Assert.IsTrue(waitTask.IsCompleted);
        }

        private void SetCurrentBeat(int beat)
        {
            var info = (TimelineInfo)GetInstanceField(this.beatSystem, "timelineInfo");
            info.currentBeat = beat;
        }

        private void InvokePrivateMethod(string methodName)
        {
            MethodInfo method = typeof(BeatSystem).GetMethod(methodName, InstancePrivate);
            Assert.NotNull(method, $"Expected method '{methodName}' to exist.");
            method.Invoke(this.beatSystem, null);
        }

        private static void RaiseMarkerPassed(string markerName)
        {
            var markerField = typeof(BeatSystem).GetField("MarkerPassed", StaticPrivate);
            Assert.NotNull(markerField, "Expected MarkerPassed event backing field.");

            var markerDelegate = markerField.GetValue(null) as BeatSystem.MarkerListenerDelegate;
            markerDelegate?.Invoke(markerName);
        }

        private static void SetStaticField(string fieldName, object value)
        {
            FieldInfo field = typeof(BeatSystem).GetField(fieldName, StaticPrivate);
            Assert.NotNull(field, $"Expected static field '{fieldName}' to exist.");
            field.SetValue(null, value);
        }

        private static void SetInstanceField(BeatSystem target, string fieldName, object value)
        {
            FieldInfo field = typeof(BeatSystem).GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static object GetInstanceField(BeatSystem target, string fieldName)
        {
            FieldInfo field = typeof(BeatSystem).GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            return field.GetValue(target);
        }

        private static void ClearStaticEvent(string eventName)
        {
            FieldInfo field = typeof(BeatSystem).GetField(eventName, StaticPrivate);
            if (field != null)
                field.SetValue(null, null);
        }
    }
}
