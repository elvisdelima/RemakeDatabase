using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public event Action<int, int, int> ReportScriptCopying;

        public void Remake()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    if (!String.IsNullOrEmpty(remakeConfiguration.SrcServer))
                        remakeConfiguration.Script = GenerateScriptFromDataSource(remakeConfiguration.ConnectionStringBuilder, remakeConfiguration.SrcDatabase);

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

        private string GenerateScriptFromDataSource(string connectionString, string dataBaseName)
        {
            var filename = @"DatabaseSript_" + dataBaseName + ".sql";

            using (var sqlConn = new SqlConnection(connectionString))
            {
                try
                {
                    var server = new Server(new ServerConnection(sqlConn));
                    ReportProcess("Conectando Servidor de Origem... Aguarde...");
                    var conectaDb = server.Databases[dataBaseName];
                    ReportProcess("Transferindo Script do Banco de Dados");
                    var scriptingOptions = new ScriptingOptions
                        {
                            ScriptData = true,
                            ScriptDrops = false,
                            FileName = filename,
                            EnforceScriptingOptions = true,
                            ScriptSchema = true,
                            IncludeHeaders = true,
                            AppendToFile = true,
                            Indexes = true,
                            WithDependencies = true,
                            //ToFileOnly = true,
                            //ScriptBatchTerminator = true,
                            //FileName = filename,
                            //AnsiPadding = true,
                            //ContinueScriptingOnError = false,
                            //ConvertUserDefinedDataTypesToBaseType = false,
                            //WithDependencies = true,
                            //DdlHeaderOnly = true,
                            //IncludeIfNotExists = false,
                            //DriAllConstraints = false,
                            //SchemaQualify = true,
                            //Bindings = false,
                            //NoCollation = false,
                            //Default = true,
                            //ScriptDrops = false,
                            //ExtendedProperties = true,
                            //TargetServerVersion = SqlServerVersion.Version110,
                            //TargetDatabaseEngineType = DatabaseEngineType.Standalone,
                            //LoginSid = false,
                            //Permissions = false,
                            //Statistics = false,
                            //ScriptData = true,
                            //ChangeTracking = false,
                            //DriChecks = true,
                            ScriptDataCompression = true,
                            //DriForeignKeys = true,
                            //FullTextIndexes = false,
                            //Indexes = true,
                            //DriPrimaryKey = true,
                            //Triggers = false,
                            //DriUniqueKeys = true,
                            //IncludeDatabaseContext = true
                        };


                    int currentTable = 0, totalTables = conectaDb.Tables.Count, barSize = Console.WindowWidth - 20;
                    File.AppendAllText(filename, string.Format("USE [{0}] \n GO \n", dataBaseName));
                    var lines = new List<string>();
                    foreach (Table tbl in conectaDb.Tables)
                    {
                        lines.AddRange(tbl.EnumScript(scriptingOptions));
                        ReportScriptCopying(currentTable++, totalTables, barSize);
                    }

                    ReportProcess("");
                    ReportProcess("Transferência de Script finalizada");
                }
                finally
                {
                    sqlConn.Close();
                }
            }
            return filename;
        }

        private int ExecuteAndReportProcess(SqlCommand command, string beforeProgess, string afterProgess)
        {
            var executeNonQuery = 0;
            ReportProcess(string.Format("{0} {1}", beforeProgess, remakeConfiguration.Database));
            using (var sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.Read())
                    executeNonQuery = (int)sqlDataReader["total"];
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