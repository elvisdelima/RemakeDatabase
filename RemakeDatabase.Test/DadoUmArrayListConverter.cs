using NUnit.Framework;

namespace RemakeDatabase.Test
{
    [TestFixture]
    public class DadoUmArrayListConverter
    {
        [Test]
        public void ConverteEmRemakeConfiguration()
        {
            var config = new[] { "database=Nash", @"server=.\sqlexpress" };
            var remakeConfiguration = ArrayListConverter.Convert<RemakeConfiguration>(config);
            Assert.IsNotNull(remakeConfiguration);
            Assert.AreEqual("Nash", remakeConfiguration.Database);
            Assert.AreEqual(@".\sqlexpress", remakeConfiguration.Server);
        }
    }
}