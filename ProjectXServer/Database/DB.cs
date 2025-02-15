using BCrypt.Net;
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
        // Register player method, now using QueryExecutor for DB operations
        public static async Task<bool> RegisterPlayer(string username, string rawpasswd, string email)
        {
            string hashedpasswd = HashPassword(rawpasswd);

            // Parameters for the insert query
            var parameters = new Dictionary<string, object>
            {
                { "@username", username },
                { "@hashedpassword", hashedpasswd },
                { "@email", email }
            };

            string query = "INSERT INTO accounts (username, password_hash, email) VALUES (@username, @hashedpassword, @email) RETURNING id";

            var accountId = await QueryExecutor.ExecuteQueryAsync<int>(query, parameters, reader => reader.GetInt32(0));
            if (accountId.Any())
            {
                bool playerCreated = await CreatePlayer(accountId.First());
                return playerCreated;
            }
            return false;
        }

        // Create player method
        public static async Task<bool> CreatePlayer(int accountId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@account_id", accountId }
            };

            string query = "INSERT INTO players (account_id) VALUES (@account_id) RETURNING id";

            var playerId = await QueryExecutor.ExecuteQueryAsync<int>(query, parameters, reader => reader.GetInt32(0));
            if (playerId.Any())
            {
                bool towersCreated = await CreateLightTowers(playerId.First());
                return towersCreated;
            }
            return false;
        }

        // Create light towers method
        public static async Task<bool> CreateLightTowers(int playerId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@player_id", playerId },
                { "@tower_num", 1 },
                { "@multiplier", 1.0f },
                { "@base_amount", 10 }
            };

            string query = "INSERT INTO light_towers (player_id, tower_num, multiplier, base_amount) VALUES (@player_id, @tower_num, @multiplier, @base_amount)";

            return await QueryExecutor.ExecuteNonQueryAsync(query, parameters);
        }

        // Login player method
        public static async Task<(string, Account)> LoginPlayer(string username, string rawpasswd)
        {
            string query = "SELECT id, password_hash FROM accounts WHERE username = @username";
            var parameters = new Dictionary<string, object>
            {
                { "@username", username }
            };

            var accountData = await QueryExecutor.ExecuteQueryAsync<(int, string)>(query, parameters, reader => (reader.GetInt32(0), reader.GetString(1)));

            if (accountData.Any())
            {
                var (accountId, storedHash) = accountData.First();
                bool isHashValid = VerifyPassword(rawpasswd, storedHash);

                if (isHashValid)
                {
                    Account accountObj = await GenerateAccountObject(accountId);
                    string authToken = await CreateAuthToken(accountId);
                    return (authToken, accountObj);
                }
            }
            return (null, null);
        }

        // Create authentication token
        public static async Task<string> CreateAuthToken(int playerId)
        {
            string randomGeneratedToken = "PXAT_" + GenerateRandomString(64);

            var parameters = new Dictionary<string, object>
            {
                { "@playerid", playerId },
                { "@token", randomGeneratedToken },
                { "@expdate", Globals.passwordExpiresAt }
            };

            string query = "INSERT INTO auth_tokens (account_id, token, expires_at) VALUES (@playerid, @token, @expdate)";

            bool success = await QueryExecutor.ExecuteNonQueryAsync(query, parameters);
            return success ? randomGeneratedToken : null;
        }

        // Generate a account object
        public static async Task<Account> GenerateAccountObject(int accountId)
        {
            string query = "SELECT username, email, created_at FROM accounts WHERE id = @accountid";
            var parameters = new Dictionary<string, object>
            {
                { "@accountid", accountId }
            };

            var accountData = await QueryExecutor.ExecuteQueryAsync<(string, string, DateTime)>(query, parameters, reader => (reader.GetString(0), reader.GetString(1), reader.GetDateTime(2)));

            if (accountData.Any())
            {
                var (username, email, createdDate) = accountData.First();
                return new Account(accountId, username, email, createdDate);
            }
            return null;
        }

        // Get player data
        public static async Task<Player> GetPlayerData(int accountId)
        {
            string query = "SELECT id, account_id, light_points, prem_points, mastery_points, current_special_skill_charge, current_special_shield_charge FROM players WHERE account_id = @account_id";
            var parameters = new Dictionary<string, object>
            {
                { "@account_id", accountId }
            };

            var playerData = await QueryExecutor.ExecuteQueryAsync<Player>(query, parameters, reader =>
            {
                return new Player
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    AccountId = reader.GetInt32(reader.GetOrdinal("account_id")),
                    LightPoints = reader.GetInt32(reader.GetOrdinal("light_points")),
                    PremPoints = reader.GetInt32(reader.GetOrdinal("prem_points")),
                    MasteryPoints = reader.GetInt32(reader.GetOrdinal("mastery_points")),
                    CurrentSpecialSkillCharge = reader.IsDBNull(reader.GetOrdinal("current_special_skill_charge"))
                        ? 0 : (float)reader.GetDouble(reader.GetOrdinal("current_special_skill_charge")),
                    CurrentSpecialShieldCharge = reader.IsDBNull(reader.GetOrdinal("current_special_shield_charge"))
                        ? 0 : (float)reader.GetDouble(reader.GetOrdinal("current_special_shield_charge"))
                };
            });

            return playerData.FirstOrDefault();
        }

        // Get light towers by player
        public static async Task<List<LightTower>> GetLightTowersByPlayer(int playerId)
        {
            string query = "SELECT player_id, tower_num, init_date, multiplier, base_amount FROM light_towers WHERE player_id = @player_id";
            var parameters = new Dictionary<string, object>
            {
                { "@player_id", playerId }
            };

            var lightTowers = await QueryExecutor.ExecuteQueryAsync<LightTower>(query, parameters, reader =>
            {
                return new LightTower(
                    reader.GetInt32(0),          // player_id
                    reader.GetInt32(1),          // tower_num
                    reader.GetDateTime(2),       // init_date
                    (float)reader.GetDouble(3),  // multiplier
                    reader.GetInt32(4)           // base_amount
                );
            });

            return lightTowers;
        }

        // Login with auth token
        public static async Task<Account> LoginWithAuthToken(string token)
        {
            string query = "SELECT account_id, expires_at FROM auth_tokens WHERE token = @token";
            var parameters = new Dictionary<string, object>
            {
                { "@token", token }
            };

            var authData = await QueryExecutor.ExecuteQueryAsync<(int, DateTime)>(query, parameters, reader => (reader.GetInt32(0), reader.GetDateTime(1)));

            if (authData.Any())
            {
                var (accountId, expirationDate) = authData.First();
                if (expirationDate > DateTime.UtcNow)
                {
                    return await GenerateAccountObject(accountId);
                }
            }

            return null;
        }

        // Save tower data
        public static async Task SaveTowerData(Player playerObject)
        {
            foreach (var tower in playerObject.unlockedLightTowers)
            {
                string query = @"
                    UPDATE light_towers 
                    SET init_date = @init_date, 
                        multiplier = @multiplier, 
                        base_amount = @base_amount 
                    WHERE player_id = @player_id AND tower_num = @tower_num";

                var parameters = new Dictionary<string, object>
                {
                    { "@player_id", tower.PlayerId },
                    { "@tower_num", tower.TowerNum },
                    { "@init_date", tower.InitDate },
                    { "@multiplier", tower.Multiplier },
                    { "@base_amount", tower.BaseAmount }
                };

                await QueryExecutor.ExecuteNonQueryAsync(query, parameters);
            }
        }

        // Save account data with friends as INT[] array
        public static async Task SaveAccountData(Account accountObject)
        {
            string query = @"
                UPDATE accounts 
                SET username = @username, 
                    password_hash = @password_hash, 
                    email = @email, 
                    friends = @friends
                WHERE id = @id";

            var parameters = new Dictionary<string, object>
            {
                { "@id", accountObject.Id },
                { "@username", accountObject.Username },
                { "@password_hash", accountObject.PasswordHash },
                { "@email", accountObject.Email },
                { "@friends", accountObject.Friends.ToArray() } // Convert List<int> to int[] for PostgreSQL
            };

            await QueryExecutor.ExecuteNonQueryAsync(query, parameters);
        }


        // Save player data
        public static async Task SavePlayerData(Player playerObject)
        {
            string query = @"
                        UPDATE players 
                        SET light_points = @light_points, 
                            prem_points = @prem_points, 
                            mastery_points = @mastery_points, 
                            current_special_skill_charge = @current_special_skill_charge, 
                            current_special_shield_charge = @current_special_shield_charge 
                        WHERE id = @id";

            var parameters = new Dictionary<string, object>
            {
                { "@id", playerObject.Id },
                { "@account_id", playerObject.AccountId },
                { "@light_points", playerObject.LightPoints },
                { "@prem_points", playerObject.PremPoints },
                { "@mastery_points", playerObject.MasteryPoints },
                { "@current_special_skill_charge", playerObject.CurrentSpecialSkillCharge },
                { "@current_special_shield_charge", playerObject.CurrentSpecialShieldCharge }
            };

            await QueryExecutor.ExecuteNonQueryAsync(query, parameters);
        }

        // Helper methods for password hashing and verification
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        // Method for generating a random string (used for auth tokens)
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
    }
}
