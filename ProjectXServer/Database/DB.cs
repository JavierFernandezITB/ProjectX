using BCrypt.Net;
using Npgsql;
using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
                "INSERT INTO players (account_id) VALUES (@account_id) RETURNING id", conn);
            playerCommand.Parameters.AddWithValue("@account_id", accountId);

            try
            {
                int playerId = (int)playerCommand.ExecuteScalar();

                if (playerId > 0)
                {
                    bool towersCreated = CreateLightTowers(conn, playerId);

                    if (towersCreated)
                    {
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("[DB] Failed to create towers.");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("[DB] Failed to create player.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] Error creating player or towers: " + ex.Message);
                return false;
            }
        }

        public static bool CreateLightTowers(NpgsqlConnection conn, int playerId)
        {
            try
            {
                NpgsqlCommand towerCommand = new NpgsqlCommand(
                    "INSERT INTO light_towers (player_id, tower_num, multiplier, base_amount) " +
                    "VALUES (@player_id, @tower_num, @multiplier, @base_amount)", conn);

                towerCommand.Parameters.AddWithValue("@player_id", playerId);
                towerCommand.Parameters.AddWithValue("@tower_num", 1);  // Initial state: not unlocked
                towerCommand.Parameters.AddWithValue("@multiplier", 1.0f); // Default multiplier
                towerCommand.Parameters.AddWithValue("@base_amount", 10);  // Default base amount

                towerCommand.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] Error creating towers: " + ex.Message);
                return false;
            }
        }

        public static List<LightTower> GetLightTowersByPlayer(int playerId)
        {
            try
            {
                List<LightTower> lightTowers = new List<LightTower>();

                using (NpgsqlConnection conn = GetConnection())
                {
                    conn.Open();

                    string query = "SELECT player_id, tower_num, init_date, multiplier, base_amount FROM light_towers WHERE player_id = @player_id";

                    NpgsqlCommand command = new NpgsqlCommand(query, conn);
                    command.Parameters.AddWithValue("@player_id", playerId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            LightTower tower = new LightTower(
                                reader.GetInt32(0),          // player_id
                                reader.GetInt32(1),          // tower_num
                                reader.GetDateTime(2),       // init_date
                                (float)reader.GetDouble(3),  // multiplier (cast double to float)
                                reader.GetInt32(4)           // base_amount
                            );

                            lightTowers.Add(tower);
                        }
                    }
                }

                return lightTowers;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] Error retrieving light towers: " + ex.Message);
                return null;
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

        public static void SaveTowerData(Player playerObject)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();
                foreach (LightTower tower in playerObject.unlockedLightTowers)
                {
                    string query = @"
                        UPDATE light_towers 
                        SET init_date = @init_date, 
                            multiplier = @multiplier, 
                            base_amount = @base_amount 
                        WHERE player_id = @player_id AND tower_num = @tower_num";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("player_id", tower.PlayerId);
                        cmd.Parameters.AddWithValue("tower_num", tower.TowerNum);
                        cmd.Parameters.AddWithValue("init_date", tower.InitDate);
                        cmd.Parameters.AddWithValue("multiplier", tower.Multiplier);
                        cmd.Parameters.AddWithValue("base_amount", tower.BaseAmount);

                        int rowsAffected = cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void SavePlayerData(Player playerObject)
        {
            using (NpgsqlConnection conn = GetConnection())
            {
                conn.Open();

                string query = @"
                            UPDATE players 
                            SET account_id = @account_id, 
                                light_points = @light_points, 
                                prem_points = @prem_points, 
                                mastery_points = @mastery_points, 
                                current_special_skill_charge = @current_special_skill_charge, 
                                current_special_shield_charge = @current_special_shield_charge 
                            WHERE id = @id";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("id", playerObject.Id);
                    cmd.Parameters.AddWithValue("account_id", playerObject.AccountId);
                    cmd.Parameters.AddWithValue("light_points", playerObject.LightPoints);
                    cmd.Parameters.AddWithValue("prem_points", playerObject.PremPoints);
                    cmd.Parameters.AddWithValue("mastery_points", playerObject.MasteryPoints);
                    cmd.Parameters.AddWithValue("current_special_skill_charge", playerObject.CurrentSpecialSkillCharge);
                    cmd.Parameters.AddWithValue("current_special_shield_charge", playerObject.CurrentSpecialShieldCharge);

                    int rowsAffected = cmd.ExecuteNonQuery();
                }
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
