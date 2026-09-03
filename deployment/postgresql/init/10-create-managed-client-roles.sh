#!/usr/bin/env bash
set -Eeuo pipefail

readonly admin_user="${POSTGRES_USER:?POSTGRES_USER is required.}"
readonly database_name="${POSTGRES_DB:?POSTGRES_DB is required.}"

readonly migrator_user="${MANAGED_CLIENT_KEYS_DB_MIGRATOR_USER:-file_processing_migrator}"
readonly runtime_user="${MANAGED_CLIENT_KEYS_DB_RUNTIME_USER:-file_processing_runtime}"

readonly migrator_password_file="${MANAGED_CLIENT_KEYS_DB_MIGRATOR_PASSWORD_FILE:?MANAGED_CLIENT_KEYS_DB_MIGRATOR_PASSWORD_FILE is required.}"
readonly runtime_password_file="${MANAGED_CLIENT_KEYS_DB_RUNTIME_PASSWORD_FILE:?MANAGED_CLIENT_KEYS_DB_RUNTIME_PASSWORD_FILE is required.}"

read_secret()
{
    local file_path="$1"
    local secret_name="$2"

    if [[ ! -f "$file_path" ]]; then
        echo "$secret_name secret file does not exist." >&2
        return 1
    fi

    local secret_value
    secret_value="$(<"$file_path")"

    if [[ ${#secret_value} -lt 32 ]]; then
        echo "$secret_name must contain at least 32 characters." >&2
        return 1
    fi

    printf '%s' "$secret_value"
}

if [[ "$migrator_user" == "$runtime_user" ||
      "$migrator_user" == "$admin_user" ||
      "$runtime_user" == "$admin_user" ]]; then
    echo "Administrator, migrator, and runtime role names must be different." >&2
    exit 1
fi

export MC_DATABASE_NAME="$database_name"
export MC_MIGRATOR_USER="$migrator_user"
export MC_RUNTIME_USER="$runtime_user"
export MC_MIGRATOR_PASSWORD
export MC_RUNTIME_PASSWORD

MC_MIGRATOR_PASSWORD="$(read_secret "$migrator_password_file" "Migrator password")"
MC_RUNTIME_PASSWORD="$(read_secret "$runtime_password_file" "Runtime password")"

psql \
    --username "$admin_user" \
    --dbname "$database_name" \
    --set=ON_ERROR_STOP=1 <<'SQL'
\getenv database_name MC_DATABASE_NAME
\getenv migrator_user MC_MIGRATOR_USER
\getenv runtime_user MC_RUNTIME_USER
\getenv migrator_password MC_MIGRATOR_PASSWORD
\getenv runtime_password MC_RUNTIME_PASSWORD

SELECT format(
    'CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS',
    :'migrator_user',
    :'migrator_password')
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = :'migrator_user')
\gexec

SELECT format(
    'CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS',
    :'runtime_user',
    :'runtime_password')
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_roles
    WHERE rolname = :'runtime_user')
\gexec

REVOKE ALL ON DATABASE :"database_name" FROM PUBLIC;

GRANT CONNECT ON DATABASE :"database_name"
TO :"migrator_user", :"runtime_user";

REVOKE CREATE ON SCHEMA public FROM PUBLIC;

CREATE SCHEMA IF NOT EXISTS migration
AUTHORIZATION :"migrator_user";

CREATE SCHEMA IF NOT EXISTS security
AUTHORIZATION :"migrator_user";

ALTER SCHEMA migration OWNER TO :"migrator_user";
ALTER SCHEMA security OWNER TO :"migrator_user";

REVOKE ALL ON SCHEMA migration FROM PUBLIC;
REVOKE ALL ON SCHEMA security FROM PUBLIC;

GRANT USAGE ON SCHEMA security TO :"runtime_user";

ALTER DEFAULT PRIVILEGES
FOR ROLE :"migrator_user"
IN SCHEMA security
GRANT SELECT ON TABLES
TO :"runtime_user";
SQL

unset MC_MIGRATOR_PASSWORD
unset MC_RUNTIME_PASSWORD