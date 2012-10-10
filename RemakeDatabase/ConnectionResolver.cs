using System;
using System.Linq;

namespace RemakeDatabase
{
    public class ConnectionResolver
    {
        private const string USER_CONNECTION_STRING =
            @"Data Source=.\sqlexpress;Initial Catalog=master;User ID={0};Password={1}";
        private const string INTEGRATED_SECURITY_CONNECTION_STRING =
            @"Data Source=.\sqlexpress;Initial Catalog=master;Integrated Security=true";

        private readonly string[] _args;

        public ConnectionResolver(string[] args)
        {
            _args = args;
        }

        public string ResolveDatabase()
        {
            if(_args.Length == 0)
                throw new InvalidOperationException("Database not defined");

            return _args.First();
        }

        public string ResolveUser()
        {
            if (_args.Length > 1)
                return _args[1];

            throw new InvalidOperationException("User not defined");
        }

        public string ResolvePassword()
        {
            if (_args.Length > 2)
                return _args[2];

            throw new InvalidOperationException("Password not defined");
        }

        public string ResolveConnectionString()
        {
            if (_args.Length == 3)
                return string.Format(USER_CONNECTION_STRING,
                                     ResolveUser(),
                                     ResolvePassword());

            return string.Format(INTEGRATED_SECURITY_CONNECTION_STRING);
        }
    }
}