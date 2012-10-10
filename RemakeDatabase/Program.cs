using System;
using System.Data.SqlClient;

namespace RemakeDatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionStringResolver = new ConnectionResolver(args);
            var dbName = connectionStringResolver.ResolveDatabase();

            var sqlCloseConnections = string.Format(@"ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", dbName);
            var sqlDropDatabase = string.Format(@"DROP DATABASE [{0}]", dbName);
            var sqlCreateDatabase = string.Format(@"CREATE DATABASE [{0}]", dbName);
            var connectionString = connectionStringResolver.ResolveConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Abrindo conexão com banco de dados");
                    connection.Open();
                    Console.WriteLine("Conexão aberta");
                    Console.WriteLine();
                    RemoveActiveConnections(dbName, connection, sqlCloseConnections);
                    Console.WriteLine();
                    DropDatabase(connection, dbName, sqlDropDatabase);
                    Console.WriteLine();
                    CreateDatabase(dbName, connection, sqlCreateDatabase);
                }
                catch (Exception e)
                {
                    connection.Close();
                    ReportErrorAndCloseConnection(e, connection);
                    throw;
                }
            }

        }

        private static void CreateDatabase(string dbName, SqlConnection connection, string sqlCreateDatabase)
        {
            using (var command = new SqlCommand(sqlCreateDatabase, connection))
            {
                try
                {
                    ExecuteAndReportProcess(dbName, command, "Criando banco de dados", "Banco de dados criado");
                }
                catch (Exception e)
                {
                    ReportErrorAndCloseConnection(e, connection);
                    throw;
                }
            }
        }

        private static void DropDatabase(SqlConnection connection, string dbName, string sqlDropDatabase)
        {
            using (var command = new SqlCommand(sqlDropDatabase, connection))
            {
                try
                {
                    ExecuteAndReportProcess(dbName, command, "Removendo banco de dados", "Banco de dados removido");
                }
                catch (SqlException e)
                {
                    if (e.Number == 3701)
                        Console.WriteLine("Banco de dados {0} não existe, continuado para processo de criação", dbName);
                    else
                    {
                        ReportErrorAndCloseConnection(e, connection);
                    }
                }
                catch (Exception e)
                {
                    ReportErrorAndCloseConnection(e, connection);
                    throw;
                }
            }
        }

        private static void RemoveActiveConnections(string dbName, SqlConnection connection, string sqlCloseConnections)
        {
            using (var command = new SqlCommand(sqlCloseConnections, connection))
            {
                try
                {
                    ExecuteAndReportProcess(dbName, command, "Removendo conexões existentes com o banco de dados",
                                            "Conexões removidas");
                }
                catch (Exception e)
                { }
            }
        }

        private static void ExecuteAndReportProcess(string dbName, SqlCommand command, string beforeProgess, string afterProgess)
        {
            Console.WriteLine("{0} {1}", beforeProgess, dbName);
            command.ExecuteNonQuery();
            Console.WriteLine("{0}", afterProgess);
        }

        private static void ReportErrorAndCloseConnection(Exception e, SqlConnection connection)
        {
            Console.WriteLine(e.Message);
            connection.Close();
        }
    }
}
