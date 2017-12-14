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

                Console.WriteLine("\n------ Your Generated Class: ------");
                Console.WriteLine("-----------------------------------\n");

                Console.Write(builtClass.ToString());

                Console.WriteLine("\n\n-----------------------------------");

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
            Console.Write("\nEnter full object name (table, stored procedure etc.): ");
            var tablename = Console.ReadLine();


            // Get the type

            //SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('somethng')

            try
            {
                Console.WriteLine("Finding and reading stored procedure...");
                return connection.Query<TableInfo>(
                    "SELECT name, system_type_name FROM sys.dm_exec_describe_first_result_set_for_object(OBJECT_ID(@TableName), NULL);", new
                    {
                        TableName = tablename
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured while reading the object. Are you sure this item exists?\nError: " + e.Message + "\n\nPress any key to try again...");

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
            Console.WriteLine("Welcome to MSSQL to C# Mapper. This program generates C# classes from a MSSQL Server Database.");
            Console.WriteLine("This program is designed to be used with Dapper connecting to MSSQL databases.");
            Console.WriteLine("Note: This program only currently supports trusted connection.");
            Console.WriteLine("Code and documentation is located at: https://github.com/DominicMaas/MSSQL-Table-Generator");
            Console.WriteLine("-----------------------------------------------------------------------------------------------\n");

            Console.Write("Enter MSSQL Server Host: ");
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