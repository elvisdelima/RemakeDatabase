using System;
using System.Linq;

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
            var remakeConfiguration = ArrayListConverter.Convert<RemakeConfiguration>(args);

            var remaker = new Remaker(remakeConfiguration);
            remaker.ReportProcess += Console.WriteLine;
            remaker.ReportScriptExecuting += (complete, maxVal, barSize) => DrawProgressBar(complete, maxVal, barSize, '█');
            remaker.Remake();
            Console.ReadKey();

            var dataSourceConnectionString = parameters.ContainsKey("src_server") ? connectionStringResolver.ResolveDataSourceConnectionString() : "";
            if (dataSourceConnectionString != "")
                GenerateScriptFromDataSource(dataSourceConnectionString, connectionStringResolver.ResolveSrcDatabase());
           
        }

        /*static void ExecuteAndReportProcess(string dbName, SqlCommand command, string beforeProgess, string afterProgess)
        {
            Console.WriteLine("{0} {1}", beforeProgess, dbName);
            command.ExecuteNonQuery();
            Console.WriteLine("{0}", afterProgess);
        }*/

 		static void GenerateScriptFromDataSource(string connectionString, string dataBaseName)
        {
            using (var sqlConn = new SqlConnection(connectionString))
            {
                var server = new Server(new ServerConnection(sqlConn));
                Console.WriteLine("Conectando Servidor de Origem... Aguarde...");
                var conectaDb = server.Databases[dataBaseName];
                Console.WriteLine("Transferindo Script do Banco de Dados");
                var transfer = new Transfer(conectaDb);
                transfer.Options.ToFileOnly = transfer.Options.ScriptBatchTerminator = true;
                transfer.Options.FileName = @"DatabaseSript_" + dataBaseName + ".sql";
                transfer.ScriptTransfer();
            }
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
