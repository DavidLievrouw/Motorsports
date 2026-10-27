# Get current date in YYYYMMDD format
$date = Get-Date -Format "yyyyMMdd"

# Backup
$password = "S3cr37_4321"

# Exclude ASP.NET Data Protection key rows: Development and historical deployments may store them unencrypted,
# allowing cookie forgery when combined with the Identity data in a dump. Production now encrypts keys with an
# external certificate; omitting them also prevents restoring historical plaintext keys. Only the table structure
# is dumped (second command); the application generates a new key ring after a clean restore (users log in again).
$dumpOptions = "--user root --single-transaction --default-character-set=utf8mb4"
$dataDump = "mariadb-dump $dumpOptions --routines --triggers --hex-blob --ignore-table=motorsports.DataProtectionKeys motorsports"
$keyTableSchemaDump = "mariadb-dump $dumpOptions --no-data motorsports DataProtectionKeys"
# Password goes via MYSQL_PWD env var: avoids shell-quoting of special characters (& #) in the password.
docker exec -e "MYSQL_PWD=$password" mariadb sh -c "$dataDump && $keyTableSchemaDump" > "$PWD/backup/Backup$($date)_MariaDB.sql"

# Restore - option 1:
# - Run MariaDB stack
# - Open phpmyadmin, connect to "db" with user "root"
# - Import data from sql file
# - Restart motorsports core container and login

# Restore - option 2:
# - Run MariaDB stack
# - Open dbeaver, and open the backup script
# - Create new database called "motorsports"
# - Run the script on the new database
# - Restart motorsports core container and login
