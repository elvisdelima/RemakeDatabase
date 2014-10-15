using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace RemakeDatabase
{
    public class Remaker : IRemaker
    {
        private readonly string connectionString;
        private readonly RemakeConfiguration config;
        private readonly string sqlCheckIfDatabaseExist;
        private readonly string sqlCloseConnections;
        private readonly string sqlCreateDatabase;
        private readonly string sqlDropDatabase;
        private readonly Stopwatch stopwatch;

        public Remaker(RemakeConfiguration config)
        {
            this.config = config;
            sqlCheckIfDatabaseExist = string.Format(@"SELECT COUNT(name) as total FROM master.dbo.sysdatabases dbs WHERE dbs.name = '{0}'", config.Database);
            sqlCloseConnections = string.Format(@"ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", config.Database);
            sqlDropDatabase = string.Format(@"DROP DATABASE [{0}]", config.Database);
            sqlCreateDatabase = string.Format(@"CREATE DATABASE [{0}]", config.Database);
            connectionString = config.ServerConnectionString;
            stopwatch = new Stopwatch();
        }

        public event Action<string> ReportProcess;
        public event Action<int, int, int> ReportScriptExecuting;

        public void Remake()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    if (!String.IsNullOrEmpty(config.SrcServer))
                        config.Script = GenerateScriptFromDataSource(config.ConnectionStringBuilder, config.SrcDatabase, config.Database);

                    DoReportProcess("Abrindo conexão com banco de dados", true);
                    connection.Open();
                    DoReportProcess("Conexão aberta", false);
                    DoReportProcess("", false);
                    var databaseExist = DatabaseExist(connection);
                    if (databaseExist)
                    {
                        DoReportProcess("Database existe", false);
                        RemoveActiveConnections(connection);
                        DoReportProcess("", false);
                        DropDatabase(connection);
                    }
                    else
                    {
                        DoReportProcess("Database não existe", false);
                    }
                    DoReportProcess("", false);
                    CreateDatabase(connection);

                    if (!string.IsNullOrWhiteSpace(config.Script))
                    {
                        var path = config.Script;
                        DoReportProcess(string.Format("Rodando script: {0}", path), true);
                        var server = new Server(new ServerConnection(connection));
                        var sqlCommand = File.ReadAllText(path);
                        var totalStatements = Regex.Matches(sqlCommand, @"^GO\s?$", RegexOptions.Multiline).Count;
                        var executedStatements = 0;
                        var connectionContext = server.ConnectionContext;
                        var barSize = Console.WindowWidth - 20;
                        connectionContext.StatementExecuted += (sender, eventArgs) => ReportScriptExecuting(++executedStatements, totalStatements, barSize);
                        DoReportProcess(string.Format("\nLinhas modificadas: {0}", connectionContext.ExecuteNonQuery(sqlCommand)), false);
                        DoReportProcess("Script executado!", false);
                    }
                }
                catch (Exception e)
                {
                    connection.Close();
                    ReportErrorAndCloseConnection(e, connection);
                }
            }
        }

        private void DoReportProcess(string message, bool startStopwatch)
        {
            if (startStopwatch)
                stopwatch.Restart();
            else if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
                message = string.Format("{0} - Executado em {1}", message, stopwatch.Elapsed);
            }

            ReportProcess(message);
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

        private string GenerateScriptFromDataSource(string serverConnectionString, string dataBaseName, string database)
        {
            var filename = Path.Combine(Environment.CurrentDirectory, @"DatabaseSript_" + dataBaseName + ".sql");

            var assembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, @"Microsoft.SqlServer.Management.SqlScriptPublishModel.dll"));

            var sqlScriptPublishModelTypes = assembly.GetTypes();
            var typeSqlScriptPublishModel = sqlScriptPublishModelTypes.First(t => t.Name.Contains("SqlScriptPublishModel"));
            var typeScriptOutputOptions = sqlScriptPublishModelTypes.First(t => t.Name.Contains("ScriptOutputOptions"));

            var sqlScriptPublishModel = Activator.CreateInstance(typeSqlScriptPublishModel, new[] { serverConnectionString });
            var scriptOutputOptions = Activator.CreateInstance(typeScriptOutputOptions);

            scriptOutputOptions.Set("SaveFileName", filename);
            sqlScriptPublishModel.Set("ScriptAllObjects", true);
            //sqlScriptPublishModel.Set("SkipCreateDatabase", true);
            var advancedOptions = sqlScriptPublishModel.Get("AdvancedOptions");
            advancedOptions.Set("ConvertUDDTToBaseType", 1);
            advancedOptions.Set("ScriptUseDatabase", 0);
            advancedOptions.Set("ScriptLogins", 1);
            advancedOptions.Set("ScriptCreateDrop", 0);
            advancedOptions.Set("TypeOfDataToScript", 0);
            advancedOptions.Set("ScriptObjectLevelPermissions", 1);
            advancedOptions.Set("ScriptOwner", 1);
            advancedOptions.Set("GenerateScriptForDependentObjects", 0);
            advancedOptions.Set("IncludeDescriptiveHeaders", 0);
            advancedOptions.Set("IncludeVarDecimal", 1);
            advancedOptions.Set("Bindings", 1);
            advancedOptions.Set("ContinueScriptingOnError", 1);
            advancedOptions.Set("AppendToFile", 1);
            advancedOptions.Set("ScriptExtendedProperties", 1);
            advancedOptions.Set("ScriptStatistics", 0);
            advancedOptions.Set("ScriptDriIncludeSystemNames", 1);
            advancedOptions.Set("ScriptAnsiPadding", 0);
            advancedOptions.Set("SchemaQualify", 0);
            advancedOptions.Set("IncludeIfNotExists", 1);
            advancedOptions.Set("Collation", 0);
            advancedOptions.Set("Default", 0);
            advancedOptions.Set("ScriptCompatibilityOption", 1);
            advancedOptions.Set("TargetDatabaseEngineType", 0);
            advancedOptions.Set("IncludeUnsupportedStatements", 1);
            advancedOptions.Set("ScriptIndexes", 0);
            advancedOptions.Set("ScriptFullTextIndexes", 1);
            advancedOptions.Set("ScriptTriggers", 1);
            advancedOptions.Set("ScriptPrimaryKeys", 0);
            advancedOptions.Set("UniqueKeys", 0);
            advancedOptions.Set("ScriptForeignKeys", 0);
            advancedOptions.Set("ScriptChangeTracking", 1);
            advancedOptions.Set("ScriptDataCompressionOptions", 1);
            advancedOptions.Set("ScriptCheckConstraints", 0);
            DoReportProcess("Transferindo Script do Banco de Dados... Aguarde...", true);
            sqlScriptPublishModel.InvokeMethod("GenerateScript", scriptOutputOptions);
            DoReportProcess("Transferência de Script finalizada", false);

            var useDb = new[] { string.Format("USE [{0}]", database) };
            var script = string.Join(Environment.NewLine, useDb.Concat(File.ReadLines(filename).Skip(15)));
            script = Regex.Replace(script, string.Format(@"\[{0}\]", dataBaseName), "[" + database + "]");
            var pattern = string.Format(@"\/\*+[\s\w:]+\[(\w+)\][\s\w:/]+\*+\/{0}CREATE USER \[\1\] FOR LOGIN \[\1\] WITH DEFAULT_SCHEMA=\[dbo\]{0}GO{0}sys.sp_addrolemember @rolename = N'db_owner', @membername = N'\1'{0}GO{0}", Environment.NewLine);
            script = Regex.Replace(script, pattern, string.Empty);
            File.WriteAllText(filename, script);

            return filename;
        }

        private int ExecuteAndReportProcess(SqlCommand command, string beforeProgess, string afterProgess)
        {
            var executeNonQuery = 0;
            DoReportProcess(string.Format("{0} {1}", beforeProgess, config.Database), true);
            using (var sqlDataReader = command.ExecuteReader())
            {
                if (sqlDataReader.Read())
                    executeNonQuery = (int)sqlDataReader["total"];
            }
            DoReportProcess(afterProgess, false);
            return executeNonQuery;
        }

        private void ReportErrorAndCloseConnection(Exception e, SqlConnection connection)
        {
            DoReportProcess(e.Message, false);
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
                        DoReportProcess(string.Format("Banco de dados {0} não existe, continuado para processo de criação", config.Database), false);
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