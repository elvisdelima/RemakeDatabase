using System.Collections.Generic;
using NUnit.Framework;

namespace RemakeDatabase.Test
{
    [TestFixture]
    public class DadaUmListaDeArgumentos
    {
        [Test]
        public void ShouldResolveDatabaseName()
        {
            var connectionResolver = new ConnectionResolver(new Dictionary<string, string> { { "database", "database" } });
            var database = connectionResolver.ResolveDatabase();

            Assert.That(database, Is.EqualTo("database"));
        }

        [Test]
        public void ShouldResolveUserName()
        {
            var connectionResolver = new ConnectionResolver(new Dictionary<string, string> { { "user", "user" }, { "password", "password" } });
            var database = connectionResolver.ResolveUser();

            Assert.That(database, Is.EqualTo("user"));
        }

        [Test]
        public void ShouldResolvePassword()
        {
            var connectionResolver = new ConnectionResolver(new Dictionary<string, string> { { "user", "user" },{ "password", "password" } });
            var database = connectionResolver.ResolvePassword();

            Assert.That(database, Is.EqualTo("password"));
        }

        [Test]
        public void ShouldResolveConnectionString()
        {
            var connectionResolver = new ConnectionResolver(new Dictionary<string, string> { { "user", "user" }, { "password", "password" } });
            var database = connectionResolver.ResolveConnectionString();

            Assert.That(database, Is.EqualTo(@"Data Source=.\sqlexpress;Initial Catalog=master;User ID=user;Password=password;"));
        }

        [Test]
        public void ShouldAssumeIntegratedSecurityInCaseUserAndPasswordWereNotGiven()
        {
            var connectionResolver = new ConnectionResolver(new Dictionary<string, string>());
            var database = connectionResolver.ResolveConnectionString();

            Assert.That(database, Is.EqualTo(@"Data Source=.\sqlexpress;Initial Catalog=master;Integrated Security=true;"));
        }
    }
}
