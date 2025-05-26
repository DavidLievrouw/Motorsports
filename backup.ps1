# Get current date in YYYYMMDD format
$date = Get-Date -Format "yyyyMMdd"

# Backup
$password = "S3cr37_4321"
docker exec mariadb sh -c "exec mariadb-dump --user root --password=$password --single-transaction --routines --triggers --hex-blob --default-character-set=utf8mb4 motorsports" > "$PWD/backup/Backup$($date)_MariaDB.sql"

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
