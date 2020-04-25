using NUnit.Framework;

namespace YAGNI.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var sut = new DummyClass();
            Assert.AreEqual(-4, sut.Add(2, 2));
        }
    }
}
