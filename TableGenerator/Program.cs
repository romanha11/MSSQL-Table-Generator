using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Dapper;

namespace TableGenerator
{
    public class Program
    {
        public readonly Dictionary<string, string> SqlToCSharpMapping = new Dictionary<string, string>
        {
            { "float", "float" },
            { "smallint", "short" },
            { "int", "int" },
            { "bitint", "double" },
            { "varchar", "string" },
            { "char", "string" },
            { "nvarchar", "string" },
            { "nchar", "string" },
            { "text", "string" },
            { "ntext", "string" },
            { "datetime", "DateTime" },
            { "smalldatetime", "DateTime" }
        };

        static void Main(string[] args) => new Program().Start(args);

        private void Start(string[] args)
        {
            // Build the SQL connection
            var connection = BuildSqlConnection();

            while (true)
            {
                // Clear console
                Console.Clear();

                // Get the table columns
                var tableColumns = GetTableInformation(connection).ToList();
                Console.WriteLine($"Table read successfully, found {tableColumns.Count} column/s!");

                // Build and return the class
                var builtClass = BuildClass(tableColumns);

                Console.WriteLine("------ Your Generated Class: ------");
                Console.WriteLine("-----------------------------------\n");

                Console.Write(builtClass.ToString());

                Console.WriteLine("\n-----------------------------------");

                Console.Write("Do you want to map another class? (Y/n)");
                var result = Console.ReadLine()?.ToLower();

                if (result != "y")
                    break;
            }

            Console.WriteLine("Closing connection...");
            connection.Close();
            connection.Dispose();
        }

        public StringBuilder BuildClass(List<TableInfo> tableColumns)
        {
            // Get the class name
            Console.Write("Name for generated class: ");
            var className = Console.ReadLine();

            Console.WriteLine("Starting to generate class and map columns. This may take a while...");

            var builder = new StringBuilder();
            builder.Append(
                "using System;\n\n" +
                "namespace TableGenerator\n" +
                "{\n" +
                $"    public class {className}\n" +
                "    {\n"
            );

            foreach (var col in tableColumns)
            {
                var colTypeStrip = col.system_type_name;

                if (col.system_type_name.Contains("("))
                    colTypeStrip = col.system_type_name.Substring(0, col.system_type_name.IndexOf("("));

                if (!SqlToCSharpMapping.TryGetValue(colTypeStrip.ToLower(), out string cSharpColname))
                {
                    var normalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("ERROR: UNSUPPORTED COLUMN: " + colTypeStrip);
                    Console.ForegroundColor = normalColor;

                    // ignore column
                    continue;
                }

                builder.AppendLine($"        public {cSharpColname} {col.name} {{ get; set; }}");
            }

            builder.Append(
                "    }\n" +
                "}"
            );

            return builder;
        }

        /// <summary>
        /// Get the table information, (list of column names and types
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private IEnumerable<TableInfo> GetTableInformation(SqlConnection connection)
        {
            Console.Write("\nEnter full table name: ");
            var tablename = Console.ReadLine();

            try
            {
                Console.WriteLine("Finding and reading table...");
                return connection.Query<TableInfo>(
                    "SELECT name, system_type_name FROM sys.dm_exec_describe_first_result_set_for_object(OBJECT_ID(@TableName), NULL);", new
                    {
                        TableName = tablename
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured while reading the table. Are you sure this table exists?\nError: " + e.Message + "\n\nPress any key to try again...");

                Console.ReadLine();
                return GetTableInformation(connection);
            }
        }

        /// <summary>
        /// Builds a valid SQL connection using user provided information
        /// </summary>
        /// <returns></returns>
        private SqlConnection BuildSqlConnection()
        {
            Console.Clear();
            Console.WriteLine("Welcome to C# Table Generator. This program generates c# classes from a MS SQL Server Database.");
            Console.WriteLine("Supports Microsoft SQL Server and Dapper");
            Console.WriteLine("Note: This program only currently supports trusted connection.");
            Console.WriteLine("Source code located at: https://github.com/DominicMaas/TableGenerator");
            Console.WriteLine("-----------------------------------------------------------------------------------------------\n");

            Console.Write("Enter MS SQL Server: ");
            var server = Console.ReadLine();

            Console.Write("Enter Database Name: ");
            var dbName = Console.ReadLine();

            try
            {
                Console.WriteLine("Connecting...");
                var connection = new SqlConnection($"Server={server};Database={dbName};Trusted_Connection=True;");
                connection.Open();

                Console.WriteLine("Successfully Connected!");
                return connection;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured while trying to connect to the server. Make sure you typed the correct information.\nPress any key to try again...");

                // Wait for user input and then try build the connection again
                Console.ReadLine();
                return BuildSqlConnection();
            }
        }
    }
}
