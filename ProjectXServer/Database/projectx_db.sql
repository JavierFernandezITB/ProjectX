CREATE DATABASE projectx_db;

\c projectx_db -- Connect to the new database.

CREATE TABLE player (
    id SERIAL PRIMARY KEY,               -- Auto-incrementing unique ID
    username VARCHAR(50) NOT NULL UNIQUE, -- Unique username
    password_hash VARCHAR(255) NOT NULL, -- Hashed password
    email VARCHAR(100) NOT NULL,          -- Email address
    created_at TIMESTAMP DEFAULT NOW()   -- Account creation timestamp
);

-- Create the auth_tokens table
CREATE TABLE auth_tokens (
    id SERIAL PRIMARY KEY,               -- Auto-incrementing unique ID for each token
    player_id INT NOT NULL,              -- Player ID associated with the token
    token VARCHAR(255) NOT NULL UNIQUE,  -- Auth token (could be JWT or random string)
    created_at TIMESTAMP DEFAULT NOW(), -- Timestamp when the token was created
    expires_at TIMESTAMP,               -- Expiration time of the token
    FOREIGN KEY (player_id) REFERENCES player(id) ON DELETE CASCADE  -- Foreign key linking to player table
);
