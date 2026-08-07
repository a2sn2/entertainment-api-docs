#!/usr/bin/env bash
set -euo pipefail

if [[ "${ATHAR_ALLOW_RESTORE_DRILL:-}" != "true" ]]; then
  echo "Restore drill refused. Set ATHAR_ALLOW_RESTORE_DRILL=true only for an isolated test environment." >&2
  exit 2
fi

compose=(docker compose -f deploy/athar-compose.yml)
backup_path="/var/opt/mssql/backup/athar-restore-drill.bak"
restore_db="AtharRestoreDrill"
restore_data="/var/opt/mssql/data/AtharRestoreDrill.mdf"
restore_log="/var/opt/mssql/data/AtharRestoreDrill_log.ldf"

sql_exec() {
  local query="$1"
  "${compose[@]}" exec -T athar-sqlserver bash -lc '
    set -e
    if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
      SQLCMD=/opt/mssql-tools18/bin/sqlcmd
    else
      SQLCMD=/opt/mssql-tools/bin/sqlcmd
    fi
    "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b "$@"
  ' -- -Q "$query"
}

cleanup() {
  set +e
  sql_exec "IF DB_ID(N'$restore_db') IS NOT NULL BEGIN ALTER DATABASE [$restore_db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$restore_db]; END" >/dev/null 2>&1
  "${compose[@]}" exec -T athar-sqlserver bash -lc "rm -f '$backup_path'" >/dev/null 2>&1
}
trap cleanup EXIT

"${compose[@]}" exec -T athar-sqlserver bash -lc "mkdir -p /var/opt/mssql/backup && rm -f '$backup_path'"

sql_exec "BACKUP DATABASE [AtharDb] TO DISK=N'$backup_path' WITH COPY_ONLY, INIT, CHECKSUM"
sql_exec "RESTORE VERIFYONLY FROM DISK=N'$backup_path' WITH CHECKSUM"

file_list="$(${compose[@]} exec -T athar-sqlserver bash -lc '
  if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then SQLCMD=/opt/mssql-tools18/bin/sqlcmd; else SQLCMD=/opt/mssql-tools/bin/sqlcmd; fi
  "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -h -1 -W -s "|" "$@"
' -- -Q "RESTORE FILELISTONLY FROM DISK=N'$backup_path'")"

data_logical="$(printf '%s\n' "$file_list" | awk -F'|' '$3 ~ /^D/ {gsub(/^ +| +$/, "", $1); print $1; exit}')"
log_logical="$(printf '%s\n' "$file_list" | awk -F'|' '$3 ~ /^L/ {gsub(/^ +| +$/, "", $1); print $1; exit}')"

if [[ -z "$data_logical" || -z "$log_logical" ]]; then
  echo "Could not determine logical SQL Server file names from RESTORE FILELISTONLY." >&2
  exit 1
fi

safe_data_logical="${data_logical//\'/\'\'}"
safe_log_logical="${log_logical//\'/\'\'}"

sql_exec "IF DB_ID(N'$restore_db') IS NOT NULL BEGIN ALTER DATABASE [$restore_db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$restore_db]; END; RESTORE DATABASE [$restore_db] FROM DISK=N'$backup_path' WITH MOVE N'$safe_data_logical' TO N'$restore_data', MOVE N'$safe_log_logical' TO N'$restore_log', RECOVERY, REPLACE"

validation="$(${compose[@]} exec -T athar-sqlserver bash -lc '
  if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then SQLCMD=/opt/mssql-tools18/bin/sqlcmd; else SQLCMD=/opt/mssql-tools/bin/sqlcmd; fi
  "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -h -1 -W "$@"
' -- -Q "SET NOCOUNT ON; USE [$restore_db]; SELECT CONCAT('users=', COUNT_BIG(*)) FROM dbo.AspNetUsers; SELECT CONCAT('initiatives=', COUNT_BIG(*)) FROM dbo.Initiatives; SELECT CONCAT('reviews=', COUNT_BIG(*)) FROM dbo.InitiativeReviews; SELECT CONCAT('audit=', COUNT_BIG(*)) FROM dbo.AuditEntries;")"

printf '%s\n' "$validation"
grep -q '^users=' <<< "$validation"
grep -q '^initiatives=' <<< "$validation"
grep -q '^reviews=' <<< "$validation"
grep -q '^audit=' <<< "$validation"

echo "Athar isolated backup restore drill passed: backup checksum verified, database restored, and core tables queried successfully."
