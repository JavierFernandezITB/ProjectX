using BCrypt.Net;
using Npgsql;
using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        NpgsqlCommand command = new NpgsqlCommand(
                            "INSERT INTO accounts (username, password_hash, email) VALUES (@username, @hashedpassword, @email) RETURNING id", conn);
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@hashedpassword", hashedpasswd);
                        command.Parameters.AddWithValue("@email", email);

                        int accountId = (int)command.ExecuteScalar();

                        bool playerCreated = CreatePlayer(conn, accountId);

                        if (playerCreated)
                        {
                            transaction.Commit();
                            Console.WriteLine("[DB] Player and account created successfully.");
                            return true;
                        }
                        else
                        {
                            transaction.Rollback();
                            Console.WriteLine("[DB] Failed to create player.");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("[DB] Error during player registration: " + ex.Message);
                        return false;
                    }
                }
            }
        }

        public static bool CreatePlayer(NpgsqlConnection conn, int accountId)
        {
            NpgsqlCommand playerCommand = new NpgsqlCommand(
                "INSERT INTO players (account_id) VALUES (@account_id)", conn);
            playerCommand.Parameters.AddWithValue("@account_id", accountId);

            try
            {
                int playerResult = playerCommand.ExecuteNonQuery();
                return playerResult > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] Error creating player: " + ex.Message);
                return false;
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

        public static Player GetPlayerData(int accountId)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                // Query to fetch player data based on account_id
                string query = "SELECT id, account_id, light_points, prem_points, mastery_points, " +
                               "current_special_skill_charge, current_special_shield_charge FROM players WHERE account_id = @account_id";

                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                {
                    command.Parameters.AddWithValue("@account_id", accountId);

                    try
                    {
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Create a Player object and populate it with the data from the database
                                Player player = new Player
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    AccountId = reader.GetInt32(reader.GetOrdinal("account_id")),
                                    LightPoints = reader.GetInt32(reader.GetOrdinal("light_points")),
                                    PremPoints = reader.GetInt32(reader.GetOrdinal("prem_points")),
                                    MasteryPoints = reader.GetInt32(reader.GetOrdinal("mastery_points")),
                                    // Explicitly convert DOUBLE to FLOAT (Single)
                                    CurrentSpecialSkillCharge = reader.IsDBNull(reader.GetOrdinal("current_special_skill_charge"))
                                                               ? 0 : (float)reader.GetDouble(reader.GetOrdinal("current_special_skill_charge")),
                                    CurrentSpecialShieldCharge = reader.IsDBNull(reader.GetOrdinal("current_special_shield_charge"))
                                                               ? 0 : (float)reader.GetDouble(reader.GetOrdinal("current_special_shield_charge"))
                                };

                                return player;
                            }
                            else
                            {
                                Console.WriteLine("[DB] No player found for the given account ID.");
                                return null; // No player found
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[DB] Error fetching player data: " + ex.Message);
                        return null; // Return null in case of error
                    }
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
