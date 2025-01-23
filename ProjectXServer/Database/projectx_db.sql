CREATE DATABASE projectx_db;

\c projectx_db -- Connect to the new database.

CREATE TABLE accounts (
    id SERIAL PRIMARY KEY,               -- Auto-incrementing unique ID
    username VARCHAR(50) NOT NULL UNIQUE, -- Unique username
    password_hash VARCHAR(255) NOT NULL, -- Hashed password
    email VARCHAR(100) NOT NULL,          -- Email address
    friends INT[],
    created_at TIMESTAMP DEFAULT NOW()   -- Account creation timestamp
);

CREATE TABLE players (
    id SERIAL PRIMARY KEY,
    account_id INT,
    light_points INT NOT NULL DEFAULT 100,
    prem_points INT NOT NULL DEFAULT 100,
    mastery_points INT NOT NULL DEFAULT 0,
    current_special_skill_charge FLOAT DEFAULT 0,
    current_special_shield_charge FLOAT DEFAULT 0,
    FOREIGN KEY (account_id) REFERENCES accounts(id)
);

CREATE TABLE light_towers (
    player_id INT,
    tower_num INT,
    init_date TIMESTAMP DEFAULT NOW(),
    multiplier FLOAT,
    base_amount INT,
    FOREIGN KEY (player_id) REFERENCES players(id)
);

CREATE TABLE monsters (
    id SERIAL PRIMARY KEY,
    name VARCHAR(32),
    description VARCHAR (255),
    type INT,
    base_level INT DEFAULT 1,
    base_level_multiplier FLOAT DEFAULT 1.25,
    base_damage FLOAT DEFAULT 0,
    base_purification FLOAT DEFAULT 0,
    base_max_purification FLOAT DEFAULT 50,
    base_purification_regen FLOAT DEFAULT 0.5,
    base_health FLOAT DEFAULT 20,
    base_max_health FLOAT DEFAULT 20,
    base_light_collection INT DEFAULT 5,
    base_item_multiplier FLOAT DEFAULT 1
);

CREATE TABLE owned_monsters (
    id SERIAL PRIMARY KEY,
    monster_id INT,
    level INT,
    current_purification FLOAT,
    current_health FLOAT
);

-- Create the auth_tokens table
CREATE TABLE auth_tokens (
    id SERIAL PRIMARY KEY,               -- Auto-incrementing unique ID for each token
    account_id INT NOT NULL,              -- Player ID associated with the token
    token VARCHAR(255) NOT NULL UNIQUE,  -- Auth token (could be JWT or random string)
    created_at TIMESTAMP DEFAULT NOW(), -- Timestamp when the token was created
    expires_at TIMESTAMP,               -- Expiration time of the token
    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE  -- Foreign key linking to player table
);

