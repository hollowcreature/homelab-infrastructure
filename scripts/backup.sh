#!/bin/bash

BACKUP_DIR="/var/lib/docker/volumes/nextcloud_nc-data/_data/data/YOUR_USERNAME/files/homelab-backups"
DATE=$(date +%Y-%m-%d_%H-%M)
LOG_FILE="YOUR-LOG-FILE-PATH"

# webhook to notify you for example through discord
WEBHOOK_URL="YOUR-WEBHOOK-LINK"

notify() {
    curl -s -H "Content-Type: application/json" -d "{\"content\": \"$1\"}" "$WEBHOOK_URL" > /dev/null
}

echo "[$DATE] Starting backup" >> "$LOG_FILE"

mkdir -p "$BACKUP_DIR/$DATE"

tar -czf "$BACKUP_DIR/$DATE/vaultwarden.tar.gz" -C /home/YOUR_USERNAME/vaultwarden data
if [ $? -ne 0 ]; then
	echo "[$DATE] ERROR: Vaultwarden backup failed" >> "$LOG_FILE"
	notify "⚠️ Homelab backup FAILED: Vaultwarden step ($DATE)"
	exit 1
fi

tar -czf "$BACKUP_DIR/$DATE/gitea.tar.gz" -C /home/YOUR_USERNAME/gitea data
if [ $? -ne 0 ]; then
	echo "[$DATE] ERROR: Gitea backup failed" >> "$LOG_FILE"
	notify "⚠️ Homelab backup FAILED: Gitea step ($DATE)"
	exit 1
fi

find "$BACKUP_DIR" -maxdepth 1 -type d -mtime +14 -exec rm -rf {} \;

echo "[$DATE] Backup finished successfully" >> "$LOG_FILE"
notify "✅ Homelab backup completed successfully ($DATE)"
