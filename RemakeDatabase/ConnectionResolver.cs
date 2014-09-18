using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemakeDatabase
{
    public class ConnectionResolver
    {
        private const string DEFAULT_SQL_INSTANCE = @".\sqlexpress";

        private readonly Dictionary<string, string> args;

        public ConnectionResolver(Dictionary<string, string> args)
        {
            this.args = args;
        }

        public string ResolveSrcServer()
        {
            if (!args.ContainsKey("src_server"))
                throw new InvalidOperationException("Source Server not defined");

            return args["src_server"];
        }

        public string ResolveSrcDatabase()
        {
            if(!args.ContainsKey("src_database"))
                throw new InvalidOperationException("Source Database not defined");

            return args["src_database"];
        }

        public string ResolveSrcUser()
        {
            if(!args.ContainsKey("src_user"))
                throw new InvalidOperationException("Source User not defined");

            return args["src_user"];
        }

        public string ResolveSrcPassword()
        {
            if(!args.ContainsKey("src_password"))
                throw new InvalidOperationException("Source Password not defined");

            return args["src_password"];
        }

        public string ResolveServer()
        {
            return !args.ContainsKey("server") ? DEFAULT_SQL_INSTANCE : args["server"];
        }

        public string ResolveDatabase()
        {
            if (!args.ContainsKey("database"))
                throw new InvalidOperationException("Database not defined");

            return args["database"];
        }

        public string ResolveUser()
        {
            if (args.ContainsKey("user") && args.ContainsKey("password"))
                return args["user"];

            throw new InvalidOperationException("User not defined");
        }

        public string ResolvePassword()
        {
            if (args.ContainsKey("user") && args.ContainsKey("password"))
                return args["password"];

            throw new InvalidOperationException("Password not defined");
        }

        public string ResolveConnectionString()
        {
            var server = ResolveServer();
            var database = args.ContainsKey("database") ? ResolveDatabase() : "";
            var user = args.ContainsKey("user") ? ResolveUser() : "";
            var password = args.ContainsKey("password") ? ResolvePassword() : "";

            return ConnectionStringBuilder(server, database, user, password);
        }

        public string ResolveDataSourceConnectionString()
        {
            var server = ResolveSrcServer();
            var database = args.ContainsKey("src_database") ? ResolveSrcDatabase() : "";
            var user = args.ContainsKey("src_user") ? ResolveSrcUser() : "";
            var password = args.ContainsKey("src_password") ? ResolveSrcPassword() : "";

            return ConnectionStringBuilder(server, database, user, password);
        }

        private string ConnectionStringBuilder(string server, string database, string user, string password)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Data Source={0};", server);
            
            if (database != "")
                sb.AppendFormat("Initial Catalog={0};", database);
            else
                sb.Append("Initial Catalog=master;");

            if (user != "" && password != "")
            {
                sb.AppendFormat("User ID={0};", user);
                sb.AppendFormat("Password={0};", password);
            }
            else
            {
                sb.Append("Integrated Security=true;");
            }
            return sb.ToString();
        }
    }
}