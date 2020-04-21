using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Polygonize;
using NUnit.Framework;

namespace NetTopologySuite.Index.Rbush.Test
{
    public class RbushTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test(Description = "constructor uses 9 max entries by default")]
        public void TestConstructor()
        {
            var tree = new Rbush<int>();
            tree.Load(new RbushTestUtility<int>(ItemValueGenerators.GetSomeValueInt32).SomeData(9));
            Assert.That(tree.Height, Is.EqualTo(1));

            tree = new Rbush<int>();
            tree.Load(new RbushTestUtility<int>(ItemValueGenerators.GetSomeValueInt32).SomeData(10));
            Assert.That(tree.Height, Is.EqualTo(2));
        }

    }
}
