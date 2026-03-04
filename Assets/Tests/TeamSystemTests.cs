using NUnit.Framework;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Cadenza.Tests
{
    public class TeamSystemTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        private GameObject testObject;
        private TeamSystem TeamSystem;
        private Team testTeam;

        [SetUp]
        public void SetUp()
        {
            this.testObject = new GameObject(nameof(TeamSystemTests));
            this.TeamSystem = this.testObject.AddComponent<TeamSystem>();

            SetStaticField("singleton", this.TeamSystem);
        }

        [TearDown]
        public void TearDown()
        {
            SetStaticField("singleton", null);

            if (this.testObject != null)
                Object.DestroyImmediate(this.testObject);
        }

        [Test]
        public void AddTeamWithName_CompletesWhenTeamAddedWithName()
        {
            Team testTeam = TeamSystem.CreateTeam("test");
            Assert.AreEqual("test", testTeam.Name);
        }

        private static void SetStaticField(string fieldName, object value)
        {
            FieldInfo field = typeof(TeamSystem).GetField(fieldName, StaticPrivate);
            Assert.NotNull(field, $"Expected static field '{fieldName}' to exist.");
            field.SetValue(null, value);
        }

        private static void SetInstanceField(TeamSystem target, string fieldName, object value)
        {
            FieldInfo field = typeof(TeamSystem).GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static object GetInstanceField(TeamSystem target, string fieldName)
        {
            FieldInfo field = typeof(TeamSystem).GetField(fieldName, InstancePrivate);
            Assert.NotNull(field, $"Expected field '{fieldName}' to exist.");
            return field.GetValue(target);
        }

        private static void ClearStaticEvent(string eventName)
        {
            FieldInfo field = typeof(TeamSystem).GetField(eventName, StaticPrivate);
            if (field != null)
                field.SetValue(null, null);
        }
    }
}
