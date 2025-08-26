#!/bin/bash

# BettsTax Database Backup Script for Sierra Leone Production
# This script creates encrypted backups of the PostgreSQL database

set -euo pipefail

# Configuration
BACKUP_DIR="/backup"
DB_HOST="postgres"
DB_PORT="5432"
DB_NAME="${DB_NAME:-BettsTaxDb}"
DB_USER="${DB_USER:-betts_user}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
ENCRYPTION_KEY="${BACKUP_ENCRYPTION_KEY}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

# Utility functions
create_backup_directory() {
    log_info "Creating backup directory structure..."
    mkdir -p "$BACKUP_DIR"/{daily,weekly,monthly}
    log_success "Backup directories created"
}

check_database_connection() {
    log_info "Checking database connection..."
    
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME"; then
            log_success "Database connection verified"
            return 0
        fi
        
        log_info "Waiting for database connection... (attempt $attempt/$max_attempts)"
        sleep 5
        ((attempt++))
    done
    
    log_error "Failed to connect to database after $max_attempts attempts"
    exit 1
}

create_backup() {
    local backup_type="$1"  # daily, weekly, monthly
    local timestamp=$(date +%Y%m%d_%H%M%S)
    local backup_file="$BACKUP_DIR/$backup_type/${backup_type}_backup_${timestamp}.sql"
    local encrypted_file="${backup_file}.enc"
    
    log_info "Creating $backup_type backup..."
    
    # Create the SQL dump
    if pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --verbose \
        --format=plain \
        --no-owner \
        --no-privileges \
        --create \
        --clean \
        --if-exists > "$backup_file"; then
        
        log_success "Database dump created: $backup_file"
        
        # Encrypt the backup
        if [[ -n "${ENCRYPTION_KEY:-}" ]]; then
            log_info "Encrypting backup file..."
            if openssl enc -aes-256-cbc -salt -in "$backup_file" -out "$encrypted_file" -k "$ENCRYPTION_KEY"; then
                log_success "Backup encrypted: $encrypted_file"
                rm "$backup_file"  # Remove unencrypted file
                backup_file="$encrypted_file"
            else
                log_error "Failed to encrypt backup"
                return 1
            fi
        else
            log_warning "No encryption key provided, backup stored unencrypted"
        fi
        
        # Compress the backup
        log_info "Compressing backup..."
        if gzip "$backup_file"; then
            backup_file="${backup_file}.gz"
            log_success "Backup compressed: $backup_file"
        fi
        
        # Calculate file size and checksum
        local file_size=$(du -h "$backup_file" | cut -f1)
        local checksum=$(sha256sum "$backup_file" | cut -d' ' -f1)
        
        log_success "$backup_type backup completed successfully"
        log_info "File: $backup_file"
        log_info "Size: $file_size"
        log_info "SHA256: $checksum"
        
        # Create backup metadata
        cat > "${backup_file}.meta" << EOF
{
    "backup_type": "$backup_type",
    "timestamp": "$timestamp",
    "database": "$DB_NAME",
    "host": "$DB_HOST",
    "file_size": "$file_size",
    "checksum": "$checksum",
    "encrypted": $([ -n "${ENCRYPTION_KEY:-}" ] && echo "true" || echo "false"),
    "compressed": true,
    "sierra_leone_compliance": {
        "finance_act_2025": true,
        "nra_requirements": true,
        "retention_period_days": $RETENTION_DAYS
    }
}
EOF
        
        return 0
    else
        log_error "Failed to create database dump"
        return 1
    fi
}

cleanup_old_backups() {
    local backup_type="$1"
    local retention_days="$2"
    
    log_info "Cleaning up old $backup_type backups (retention: $retention_days days)..."
    
    local backup_dir="$BACKUP_DIR/$backup_type"
    local deleted_count=0
    
    # Find and delete old backup files
    while IFS= read -r -d '' file; do
        if [[ -f "$file" ]]; then
            rm "$file"
            ((deleted_count++))
            log_info "Deleted old backup: $(basename "$file")"
        fi
    done < <(find "$backup_dir" -name "*.sql.gz" -o -name "*.sql.enc.gz" -mtime +"$retention_days" -print0 2>/dev/null)
    
    # Also clean up metadata files
    find "$backup_dir" -name "*.meta" -mtime +"$retention_days" -delete 2>/dev/null || true
    
    if [[ $deleted_count -gt 0 ]]; then
        log_success "Cleaned up $deleted_count old $backup_type backups"
    else
        log_info "No old $backup_type backups to clean up"
    fi
}

verify_backup() {
    local backup_file="$1"
    
    log_info "Verifying backup integrity..."
    
    # Check if file exists and is not empty
    if [[ ! -f "$backup_file" ]] || [[ ! -s "$backup_file" ]]; then
        log_error "Backup file is missing or empty"
        return 1
    fi
    
    # Verify checksum if metadata exists
    local meta_file="${backup_file}.meta"
    if [[ -f "$meta_file" ]]; then
        local expected_checksum=$(grep '"checksum"' "$meta_file" | sed 's/.*"checksum": "\([^"]*\)".*/\1/')
        local actual_checksum=$(sha256sum "$backup_file" | cut -d' ' -f1)
        
        if [[ "$expected_checksum" == "$actual_checksum" ]]; then
            log_success "Backup integrity verified"
        else
            log_error "Backup checksum mismatch"
            return 1
        fi
    fi
    
    return 0
}

send_notification() {
    local backup_type="$1"
    local status="$2"
    local message="$3"
    
    # Send Slack notification if webhook is configured
    if [[ -n "${SLACK_WEBHOOK_URL:-}" ]]; then
        local emoji=$([ "$status" == "success" ] && echo "✅" || echo "❌")
        local payload="{\"text\":\"$emoji BettsTax $backup_type backup $status: $message\"}"
        
        curl -X POST -H 'Content-type: application/json' \
            --data "$payload" \
            "$SLACK_WEBHOOK_URL" 2>/dev/null || true
    fi
}

# Main backup function
perform_backup() {
    local backup_type="$1"
    
    log_info "Starting $backup_type backup process for BettsTax Sierra Leone..."
    
    if create_backup "$backup_type"; then
        # Find the most recent backup file
        local latest_backup=$(find "$BACKUP_DIR/$backup_type" -name "*backup_*.sql*.gz" -type f -printf '%T@ %p\n' | sort -n | tail -1 | cut -d' ' -f2-)
        
        if verify_backup "$latest_backup"; then
            log_success "$backup_type backup process completed successfully"
            send_notification "$backup_type" "success" "Backup completed successfully"
            
            # Cleanup old backups based on type
            case "$backup_type" in
                "daily")
                    cleanup_old_backups "$backup_type" 7
                    ;;
                "weekly")
                    cleanup_old_backups "$backup_type" 30
                    ;;
                "monthly")
                    cleanup_old_backups "$backup_type" 365
                    ;;
            esac
        else
            log_error "$backup_type backup verification failed"
            send_notification "$backup_type" "failed" "Backup verification failed"
            exit 1
        fi
    else
        log_error "$backup_type backup creation failed"
        send_notification "$backup_type" "failed" "Backup creation failed"
        exit 1
    fi
}

# Main execution
main() {
    log_info "BettsTax Database Backup Service Starting..."
    
    # Check if encryption key is provided
    if [[ -z "${ENCRYPTION_KEY:-}" ]]; then
        log_warning "No encryption key provided. Backups will be stored unencrypted."
    fi
    
    create_backup_directory
    check_database_connection
    
    # Determine backup type based on current date/time
    local day_of_week=$(date +%u)  # 1-7 (Monday-Sunday)
    local day_of_month=$(date +%d)
    
    if [[ "$day_of_month" == "01" ]]; then
        # First day of month - monthly backup
        perform_backup "monthly"
    elif [[ "$day_of_week" == "7" ]]; then
        # Sunday - weekly backup
        perform_backup "weekly"
    else
        # Daily backup
        perform_backup "daily"
    fi
    
    log_success "BettsTax backup service completed successfully"
}

# Handle script interruption
trap 'log_error "Backup interrupted"; exit 1' INT TERM

# Check if running in container or standalone
if [[ "${1:-}" == "--daemon" ]]; then
    # Daemon mode - run backup periodically
    log_info "Starting backup daemon mode..."
    while true; do
        main
        log_info "Sleeping for 24 hours until next backup..."
        sleep 86400  # 24 hours
    done
else
    # Single run mode
    main
fi