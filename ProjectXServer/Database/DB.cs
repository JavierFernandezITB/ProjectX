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

                NpgsqlCommand command = new NpgsqlCommand("INSERT INTO player (username, password_hash, email) VALUES (@username, @hashedpassword, @email)", conn);
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

        public static (string, Player) LoginPlayer(string username, string rawpasswd)
        {
            using (NpgsqlConnection conn = GetConnection())
            { 
                conn.Open();

                NpgsqlCommand command = new NpgsqlCommand("SELECT id, password_hash FROM player WHERE username = :username", conn);
                command.Parameters.AddWithValue("username", username);
                NpgsqlDataReader dataReader = command.ExecuteReader();
                bool isHashValid = false;
                int playerId = 0;

                if (dataReader.Read())
                {
                    playerId = dataReader.GetInt32(0);
                    string storedHash = dataReader.GetString(1);

                    isHashValid = VerifyPassword(rawpasswd, storedHash);
                }

                dataReader.Close();

                if (isHashValid)
                {
                    Player playerObj = GeneratePlayerObject(playerId);
                    string authToken = CreateAuthToken(playerId);
                    return (authToken, playerObj);
                }
                else
                {
                    return (null,null);
                }
            }
        }

        public static Player LoginWithAuthToken(string token)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                NpgsqlCommand command = new NpgsqlCommand("SELECT player_id, expires_at FROM auth_tokens WHERE token = @token", conn);
                command.Parameters.AddWithValue("@token", token);
                NpgsqlDataReader dataReader = command.ExecuteReader();

                if (dataReader.Read())
                {
                    int playerId = dataReader.GetInt32(0);
                    DateTime expirationDate = dataReader.GetDateTime(1);

                    dataReader.Close();
                    if (expirationDate > DateTime.UtcNow)
                    {
                        return GeneratePlayerObject(playerId);
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

                NpgsqlCommand command = new NpgsqlCommand("INSERT INTO auth_tokens (player_id, token, expires_at) VALUES (@playerid, @token, @expdate)", conn);
                command.Parameters.AddWithValue("@playerid", playerId);
                command.Parameters.AddWithValue("@token", randomGeneratedToken);
                command.Parameters.AddWithValue("@expdate", Globals.passwordExpiresAt);
                command.ExecuteNonQuery();

                return randomGeneratedToken;
            }
        }

        public static Player GeneratePlayerObject(int playerId)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                NpgsqlCommand command = new NpgsqlCommand("SELECT username, email, created_at FROM player WHERE id = @playerid", conn);
                command.Parameters.AddWithValue("@playerid", playerId);
                NpgsqlDataReader dataReader = command.ExecuteReader();

                if (dataReader.Read())
                {
                    string playerUsername = dataReader.GetString(0);
                    string playerEmail = dataReader.GetString(1);
                    DateTime playerCreatedDate = dataReader.GetDateTime(2);
                    dataReader.Close();

                    Player playerObj = new Player(playerId, playerUsername, playerEmail, playerCreatedDate);
                    return playerObj;
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
