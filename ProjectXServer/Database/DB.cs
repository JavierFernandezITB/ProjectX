using BCrypt.Net;
using Npgsql;
using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Database
{
    internal class DB
    {
        private static string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=pgsql;Database=projectx_db";

        private static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }

        public static bool RegisterPlayer(string username, string rawpasswd, string email)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                string hashedpasswd = HashPassword(rawpasswd);

                NpgsqlCommand command = new NpgsqlCommand("INSERT INTO accounts (username, password_hash, email) VALUES (@username, @hashedpassword, @email)", conn);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@hashedpassword", hashedpasswd);
                command.Parameters.AddWithValue("@email", email);
                int result = 0;
                try
                {
                    result = command.ExecuteNonQuery();
                }
                catch (Exception ex) {}
                Console.WriteLine($"[DB] Login operation affected {result} rows.");
                return result > 0;
            }
        }

        public static (string, Account) LoginPlayer(string username, string rawpasswd)
        {
            using (NpgsqlConnection conn = GetConnection())
            { 
                conn.Open();

                NpgsqlCommand command = new NpgsqlCommand("SELECT id, password_hash FROM accounts WHERE username = :username", conn);
                command.Parameters.AddWithValue("username", username);
                NpgsqlDataReader dataReader = command.ExecuteReader();
                bool isHashValid = false;
                int accountId = 0;

                if (dataReader.Read())
                {
                    accountId = dataReader.GetInt32(0);
                    string storedHash = dataReader.GetString(1);

                    isHashValid = VerifyPassword(rawpasswd, storedHash);
                }

                dataReader.Close();

                if (isHashValid)
                {
                    Account accountObj = GeneratePlayerObject(accountId);
                    string authToken = CreateAuthToken(accountId);
                    return (authToken, accountObj);
                }
                else
                {
                    return (null,null);
                }
            }
        }

        public static Account LoginWithAuthToken(string token)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                NpgsqlCommand command = new NpgsqlCommand("SELECT account_id, expires_at FROM auth_tokens WHERE token = @token", conn);
                command.Parameters.AddWithValue("@token", token);
                NpgsqlDataReader dataReader = command.ExecuteReader();

                if (dataReader.Read())
                {
                    int accountId = dataReader.GetInt32(0);
                    DateTime expirationDate = dataReader.GetDateTime(1);

                    dataReader.Close();
                    if (expirationDate > DateTime.UtcNow)
                    {
                        return GeneratePlayerObject(accountId);
                    }
                }
                dataReader.Close();
                return null;
            }
        }

        private static string CreateAuthToken(int playerId)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                string randomGeneratedToken = "PXAT_" + GenerateRandomString(64);

                NpgsqlCommand command = new NpgsqlCommand("INSERT INTO auth_tokens (account_id, token, expires_at) VALUES (@playerid, @token, @expdate)", conn);
                command.Parameters.AddWithValue("@playerid", playerId);
                command.Parameters.AddWithValue("@token", randomGeneratedToken);
                command.Parameters.AddWithValue("@expdate", Globals.passwordExpiresAt);
                command.ExecuteNonQuery();

                return randomGeneratedToken;
            }
        }

        public static Account GeneratePlayerObject(int accountId)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                NpgsqlCommand command = new NpgsqlCommand("SELECT username, email, created_at FROM accounts WHERE id = @accountid", conn);
                command.Parameters.AddWithValue("@accountid", accountId);
                NpgsqlDataReader dataReader = command.ExecuteReader();

                if (dataReader.Read())
                {
                    string accountUsername = dataReader.GetString(0);
                    string accountEmail = dataReader.GetString(1);
                    DateTime accountCreatedDate = dataReader.GetDateTime(2);
                    dataReader.Close();

                    Account accountObj = new Account(accountId, accountUsername, accountEmail, accountCreatedDate);
                    return accountObj;
                }

                dataReader.Close();
                return null;
            }
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var output = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                output.Append(chars[random.Next(chars.Length)]);
            }

            return output.ToString();
        }

        public static string HashPassword(string password)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            return hashedPassword;
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
