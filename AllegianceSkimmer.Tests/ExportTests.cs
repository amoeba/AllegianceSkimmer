using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AllegianceSkimmer.Tests
{
    [TestFixture]
    public class ExportTests
    {
        private static JsonSerializerSettings Settings() =>
            new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Export.ScanItemJsonConverter() }
            };

        [Test]
        public void WriteJson_LeafNode_SerializesNameIsOnlineAndEmptyChildren()
        {
            var item = new ScanItem(1, "TestChar");

            var obj = JObject.Parse(JsonConvert.SerializeObject(item, Settings()));

            Assert.That((string)obj["name"], Is.EqualTo("TestChar"));
            Assert.That((bool)obj["is_online"], Is.False);
            Assert.That(obj["children"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(((JArray)obj["children"]).Count, Is.EqualTo(0));
        }

        [Test]
        public void WriteJson_OnlineNode_SerializesIsOnlineTrue()
        {
            var item = new ScanItem(1, "TestChar") { IsOnline = true };

            var obj = JObject.Parse(JsonConvert.SerializeObject(item, Settings()));

            Assert.That((bool)obj["is_online"], Is.True);
        }

        [Test]
        public void WriteJson_NodeWithChildren_SerializesChildren()
        {
            var parent = new ScanItem(1, "Parent");
            parent.Children.Add(new ScanItem(2, "Child1"));
            parent.Children.Add(new ScanItem(3, "Child2"));

            var obj = JObject.Parse(JsonConvert.SerializeObject(parent, Settings()));
            var children = (JArray)obj["children"];

            Assert.That(children.Count, Is.EqualTo(2));
            Assert.That((string)children[0]["name"], Is.EqualTo("Child1"));
            Assert.That((string)children[1]["name"], Is.EqualTo("Child2"));
        }

        [Test]
        public void WriteJson_NullChildren_SerializesEmptyArray()
        {
            var item = new ScanItem(); // default ctor leaves Children null

            var obj = JObject.Parse(JsonConvert.SerializeObject(item, Settings()));

            Assert.That(obj["children"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(((JArray)obj["children"]).Count, Is.EqualTo(0));
        }

        [Test]
        public void WriteJson_NestedChildren_SerializesRecursively()
        {
            var root = new ScanItem(1, "Root");
            var child = new ScanItem(2, "Child");
            child.Children.Add(new ScanItem(3, "Grandchild"));
            root.Children.Add(child);

            var obj = JObject.Parse(JsonConvert.SerializeObject(root, Settings()));
            var grandchild = obj["children"][0]["children"][0];

            Assert.That((string)grandchild["name"], Is.EqualTo("Grandchild"));
        }
    }
}
