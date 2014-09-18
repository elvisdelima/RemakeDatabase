using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace RemakeDatabase
{
    public class Remaker : IRemaker
    {
        private readonly string connectionString;
        private readonly RemakeConfiguration remakeConfiguration;
        private readonly string sqlCheckIfDatabaseExist;
        private readonly string sqlCloseConnections;
        private readonly string sqlCreateDatabase;
        private readonly string sqlDropDatabase;

        public Remaker(RemakeConfiguration remakeConfiguration)
        {
            this.remakeConfiguration = remakeConfiguration;
            sqlCheckIfDatabaseExist = string.Format(@"SELECT COUNT(name) as total FROM master.dbo.sysdatabases dbs WHERE dbs.name = '{0}'", remakeConfiguration.Database);
            sqlCloseConnections = string.Format(@"ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", remakeConfiguration.Database);
            sqlDropDatabase = string.Format(@"DROP DATABASE [{0}]", remakeConfiguration.Database);
            sqlCreateDatabase = string.Format(@"CREATE DATABASE [{0}]", remakeConfiguration.Database);
            connectionString = remakeConfiguration.ServerConnectionString;
        }

        public event Action<string> ReportProcess;
        public event Action<int, int, int> ReportScriptExecuting;

        public void Remake()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    ReportProcess("Abrindo conexão com banco de dados");
                    connection.Open();
                    ReportProcess("Conexão aberta");
                    ReportProcess("");
                    var databaseExist = DatabaseExist(connection);
                    if (databaseExist)
                    {
                        ReportProcess("Database existe");
                        RemoveActiveConnections(connection);
                        ReportProcess("");
                        DropDatabase(connection);
                    }
                    else
                    {
                        ReportProcess("Database não existe");
                    }
                    ReportProcess("");
                    CreateDatabase(connection);
                    if (!string.IsNullOrWhiteSpace(remakeConfiguration.Script))
                    {
                        var path = remakeConfiguration.Script;
                        using (var stream = new StreamReader(path))
                        {
                            ReportProcess(string.Format("Rodando script: {0}", path));
                            var server = new Server(new ServerConnection(connection));
                            var sqlCommand = stream.ReadToEnd();
                            var totalStatements = Regex.Matches(sqlCommand, @"^GO\s?$", RegexOptions.Multiline).Count;
                            var executedStatements = 0;
                            var connectionContext = server.ConnectionContext;
                            var barSize = Console.WindowWidth - 20;
                            connectionContext.StatementExecuted += (sender, eventArgs) => ReportScriptExecuting(++executedStatements, totalStatements, barSize);
                            ReportProcess(string.Format("\nLinhas modificadas: {0}", connectionContext.ExecuteNonQuery(sqlCommand)));
                            ReportProcess("Script executado!");
                        }
                    }
                }
                catch (Exception e)
                {
                    connection.Close();
                    ReportErrorAndCloseConnection(e, connection);
                }
            }
        }

        private bool DatabaseExist(SqlConnection connection)
        {
            using (var command = new SqlCommand(sqlCheckIfDatabaseExist, connection))
            {
                try
                {
                    var total = ExecuteAndReportProcess(command, "Checando se banco de dados existe", "Existencia checada");
                    return total > 0;
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        private int ExecuteAndReportProcess(SqlCommand command, string beforeProgess, string afterProgess)
        {
            var executeNonQuery = 0;
            ReportProcess(string.Format("{0} {1}", beforeProgess, remakeConfiguration.Database));
            using (var sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.Read())
                    executeNonQuery = (int) sqlDataReader["total"];
            }
            ReportProcess(afterProgess);
            return executeNonQuery;
        }

        private void ReportErrorAndCloseConnection(Exception e, SqlConnection connection)
        {
            ReportProcess(e.Message);
            connection.Close();
        }

        private void CreateDatabase(SqlConnection connection)
        {
            using (var command = new SqlCommand(sqlCreateDatabase, connection))
            {
                try
                {
                    ExecuteAndReportProcess(command, "Criando banco de dados", "OK");
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        private void DropDatabase(SqlConnection connection)
        {
            using (var command = new SqlCommand(sqlDropDatabase, connection))
            {
                try
                {
                    ExecuteAndReportProcess(command, "Removendo banco de dados", "Banco de dados removido");
                }
                catch (SqlException e)
                {
                    if (e.Number == 3701)
                        ReportProcess(string.Format("Banco de dados {0} não existe, continuado para processo de criação", remakeConfiguration.Database));
                    else
                        throw;
                }
                finally
                {
                    command.Dispose();
                }
            }
        }

        private void RemoveActiveConnections(SqlConnection connection)
        {
            using (var command = new SqlCommand(sqlCloseConnections, connection))
            {
                try
                {
                    ExecuteAndReportProcess(command, "Removendo conexões existentes com o banco de dados", "Conexões removidas");
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}