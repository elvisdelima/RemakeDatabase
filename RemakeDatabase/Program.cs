using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace RemakeDatabase
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args == null || !args.Any())
            {
                Console.WriteLine("Digite o nome da base de dados:");
                args = new[] { string.Format("database={0}", Console.ReadLine()) };
            }
            var parameters = args.ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);
            var connectionStringResolver = new ConnectionResolver(parameters);
            var dbName = connectionStringResolver.ResolveDatabase();

            var sqlCheckIfDatabaseExist = string.Format(@"SELECT COUNT(name) as total FROM master.dbo.sysdatabases dbs WHERE dbs.name = '{0}'", dbName);
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
                    var databaseExist = DatabaseExist(dbName, connection, sqlCheckIfDatabaseExist);
                    if (databaseExist)
                    {
                        Console.WriteLine("Database existe");
                        RemoveActiveConnections(dbName, connection, sqlCloseConnections);
                        Console.WriteLine();
                        DropDatabase(connection, dbName, sqlDropDatabase);
                    }
                    else
                    {
                        Console.WriteLine("Database não existe");
                    }
                    Console.WriteLine();
                    CreateDatabase(dbName, connection, sqlCreateDatabase);
                    if (parameters.ContainsKey("script"))
                    {
                        var path = parameters["script"];
                        using (var stream = new StreamReader(path))
                        {
                            Console.WriteLine("Rodando script: {0}", path);
                            var server = new Server(new ServerConnection(connection));
                            var sqlCommand = stream.ReadToEnd();
                            var totalStatements = Regex.Matches(sqlCommand, @"^GO\s?$", RegexOptions.Multiline).Count;
                            var executedStatements = 0;
                            var connectionContext = server.ConnectionContext;
                            var barSize = Console.WindowWidth - 20;
                            connectionContext.StatementExecuted += (sender, eventArgs) => DrawProgressBar(++executedStatements, totalStatements, barSize, '█');
                            Console.WriteLine("\nLinhas modificadas: {0}", connectionContext.ExecuteNonQuery(sqlCommand));
                            Console.WriteLine("Script executado!");
                        }
                    }
                }
                catch (Exception e)
                {
                    connection.Close();
                    ReportErrorAndCloseConnection(e, connection);
                    throw;
                }
            }

        }

        static bool DatabaseExist(string dbName, SqlConnection connection, string sqlCheckIfDatabaseExist)
        {
            using (var command = new SqlCommand(sqlCheckIfDatabaseExist, connection))
            {
                try
                {
                    int total;
                    ExecuteAndReportProcess(dbName, command, "Checando se banco de dados existe", "Existencia checada", out total);
                    return total > 0;
                }
                catch (Exception e)
                {
                    ReportErrorAndCloseConnection(e, connection);
                    throw;
                }
            }
        }

        static void CreateDatabase(string dbName, SqlConnection connection, string sqlCreateDatabase)
        {
            using (var command = new SqlCommand(sqlCreateDatabase, connection))
            {
                try
                {
                    ExecuteAndReportProcess(dbName, command, "Criando banco de dados", "OK");
                }
                catch (Exception e)
                {
                    ReportErrorAndCloseConnection(e, connection);
                    throw;
                }
            }
        }

        static void DropDatabase(SqlConnection connection, string dbName, string sqlDropDatabase)
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

        static void RemoveActiveConnections(string dbName, SqlConnection connection, string sqlCloseConnections)
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

        static void ExecuteAndReportProcess(string dbName, SqlCommand command, string beforeProgess, string afterProgess)
        {
            Console.WriteLine("{0} {1}", beforeProgess, dbName);
            command.ExecuteNonQuery();
            Console.WriteLine("{0}", afterProgess);
        }

        static void ExecuteAndReportProcess(string dbName, SqlCommand command, string beforeProgess, string afterProgess, out int executeNonQuery)
        {
            executeNonQuery = 0;
            Console.WriteLine("{0} {1}", beforeProgess, dbName);
            using (var sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.Read())
                    executeNonQuery = (int)sqlDataReader["total"];
            }
            Console.WriteLine("{0}", afterProgess);
        }

        static void ReportErrorAndCloseConnection(Exception e, SqlConnection connection)
        {
            Console.WriteLine(e.Message);
            connection.Close();
        }

        private static void DrawProgressBar(int complete, int maxVal, int barSize, char progressCharacter)
        {
            Console.CursorVisible = false;
            int left = Console.CursorLeft;
            decimal perc = (decimal)complete / (decimal)maxVal;
            int chars = (int)Math.Floor(perc / ((decimal)1 / (decimal)barSize));
            string p1 = String.Empty, p2 = String.Empty;

            for (int i = 0; i < chars; i++) p1 += progressCharacter;
            for (int i = 0; i < barSize - chars; i++) p2 += progressCharacter;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(p1);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(p2);

            Console.ResetColor();
            Console.Write(" {0}%", (perc * 100).ToString("N2"));
            Console.CursorLeft = left;
        }
    }
}
