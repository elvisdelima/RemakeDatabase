using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RemakeDatabase
{
    static class Program
    {
        static void Main(string[] args)
        {

            /*var assembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, @"Microsoft.SqlServer.Management.SqlScriptPublishModel.dll"));

            var typeSqlScriptPublishModel = assembly.GetTypes().FirstOrDefault(t => t.Name.Contains("SqlScriptOptions"));

            foreach (var propertyInfo in typeSqlScriptPublishModel.GetProperties())
            {
                Console.WriteLine("{0} => {1}", propertyInfo.Name, propertyInfo.PropertyType.Name);
            }
            return;*/
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
