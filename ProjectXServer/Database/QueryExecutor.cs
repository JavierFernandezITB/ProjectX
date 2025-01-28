using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Database
{
    internal class QueryExecutor
    {
        private static string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=pgsql;Database=projectx_db";

        private static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }

        // Executes SELECT queries and returns a list of mapped results
        public static async Task<List<T>> ExecuteQueryAsync<T>(string query, Dictionary<string, object> parameters, Func<NpgsqlDataReader, T> mapResult)
        {
            List<T> results = new List<T>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            T result = mapResult(reader);
                            results.Add(result);
                        }
                    }
                }
            }

            return results;
        }

        // Executes non-SELECT queries (INSERT, UPDATE, DELETE)
        public static async Task<bool> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
