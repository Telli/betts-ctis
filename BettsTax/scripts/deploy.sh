#!/bin/bash

# BettsTax Production Deployment Script for Sierra Leone
# This script handles the complete deployment process

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
ENVIRONMENT="${1:-production}"
VERSION="${2:-latest}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Utility functions
check_dependencies() {
    log_info "Checking deployment dependencies..."
    
    local deps=("docker" "docker-compose" "curl" "jq")
    for dep in "${deps[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            log_error "$dep is required but not installed"
            exit 1
        fi
    done
    
    log_success "All dependencies are available"
}

check_environment() {
    log_info "Checking environment configuration..."
    
    if [[ ! -f "$PROJECT_ROOT/.env.${ENVIRONMENT}" ]]; then
        log_error "Environment file .env.${ENVIRONMENT} not found"
        log_info "Please create .env.${ENVIRONMENT} with required configuration"
        exit 1
    fi
    
    # Load environment variables
    export $(cat "$PROJECT_ROOT/.env.${ENVIRONMENT}" | xargs)
    
    # Check required variables
    local required_vars=(
        "DB_PASSWORD"
        "JWT_SECRET_KEY"
        "ENCRYPTION_MASTER_KEY"
        "REDIS_PASSWORD"
    )
    
    for var in "${required_vars[@]}"; do
        if [[ -z "${!var:-}" ]]; then
            log_error "Required environment variable $var is not set"
            exit 1
        fi
    done
    
    log_success "Environment configuration is valid"
}

backup_database() {
    log_info "Creating database backup before deployment..."
    
    local backup_dir="$PROJECT_ROOT/backups"
    local backup_file="$backup_dir/backup_$(date +%Y%m%d_%H%M%S).sql"
    
    mkdir -p "$backup_dir"
    
    if docker-compose -f "$PROJECT_ROOT/docker-compose.prod.yml" exec -T postgres pg_dump -U "${DB_USER}" "${DB_NAME}" > "$backup_file"; then
        log_success "Database backup created: $backup_file"
    else
        log_warning "Database backup failed, but continuing with deployment"
    fi
}

pre_deployment_checks() {
    log_info "Running pre-deployment checks..."
    
    # Check disk space
    local available_space=$(df "$PROJECT_ROOT" | tail -1 | awk '{print $4}')
    local required_space=1048576  # 1GB in KB
    
    if [[ $available_space -lt $required_space ]]; then
        log_error "Insufficient disk space. Required: 1GB, Available: $(($available_space / 1024))MB"
        exit 1
    fi
    
    # Check if ports are available
    local ports=("80" "443" "5432" "6379" "9090" "3001")
    for port in "${ports[@]}"; do
        if netstat -tuln | grep -q ":$port "; then
            log_warning "Port $port is already in use"
        fi
    done
    
    # Validate SSL certificates
    if [[ -f "$PROJECT_ROOT/ssl/fullchain.pem" ]] && [[ -f "$PROJECT_ROOT/ssl/privkey.pem" ]]; then
        log_success "SSL certificates found"
    else
        log_warning "SSL certificates not found. HTTPS will not work properly"
    fi
    
    log_success "Pre-deployment checks completed"
}

build_images() {
    log_info "Building Docker images..."
    
    cd "$PROJECT_ROOT"
    
    # Build API image
    log_info "Building BettsTax API image..."
    docker build -t "betts-tax-api:${VERSION}" .
    
    # Build frontend image (if Dockerfile exists)
    if [[ -f "sierra-leone-ctis/Dockerfile.prod" ]]; then
        log_info "Building BettsTax Frontend image..."
        docker build -t "betts-tax-frontend:${VERSION}" -f sierra-leone-ctis/Dockerfile.prod sierra-leone-ctis/
    fi
    
    log_success "Docker images built successfully"
}

deploy_services() {
    log_info "Deploying BettsTax services..."
    
    cd "$PROJECT_ROOT"
    
    # Pull latest images
    docker-compose -f docker-compose.prod.yml pull
    
    # Start services
    docker-compose -f docker-compose.prod.yml up -d --remove-orphans
    
    log_success "Services deployed successfully"
}

run_migrations() {
    log_info "Running database migrations..."
    
    # Wait for database to be ready
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if docker-compose -f "$PROJECT_ROOT/docker-compose.prod.yml" exec -T postgres pg_isready -U "${DB_USER}" -d "${DB_NAME}"; then
            break
        fi
        
        log_info "Waiting for database... (attempt $attempt/$max_attempts)"
        sleep 5
        ((attempt++))
    done
    
    if [[ $attempt -gt $max_attempts ]]; then
        log_error "Database connection timeout"
        exit 1
    fi
    
    # Run EF Core migrations
    docker-compose -f "$PROJECT_ROOT/docker-compose.prod.yml" exec -T betts-api dotnet ef database update --no-build
    
    log_success "Database migrations completed"
}

health_checks() {
    log_info "Running health checks..."
    
    local services=("betts-api:8080" "betts-frontend:3000")
    local max_attempts=30
    
    for service in "${services[@]}"; do
        local attempt=1
        local service_name=$(echo "$service" | cut -d':' -f1)
        local port=$(echo "$service" | cut -d':' -f2)
        
        log_info "Checking health of $service_name..."
        
        while [[ $attempt -le $max_attempts ]]; do
            if curl -f -s "http://localhost:$port/health" > /dev/null 2>&1; then
                log_success "$service_name is healthy"
                break
            fi
            
            log_info "Waiting for $service_name... (attempt $attempt/$max_attempts)"
            sleep 10
            ((attempt++))
        done
        
        if [[ $attempt -gt $max_attempts ]]; then
            log_error "$service_name health check failed"
            exit 1
        fi
    done
    
    log_success "All health checks passed"
}

setup_monitoring() {
    log_info "Setting up monitoring and alerting..."
    
    # Ensure Prometheus is collecting metrics
    if curl -f -s "http://localhost:9090/api/v1/targets" | jq -r '.data.activeTargets[].health' | grep -q "up"; then
        log_success "Prometheus is collecting metrics"
    else
        log_warning "Prometheus may not be collecting all metrics properly"
    fi
    
    # Check Grafana
    if curl -f -s "http://localhost:3001/api/health" > /dev/null 2>&1; then
        log_success "Grafana dashboard is available"
    else
        log_warning "Grafana dashboard may not be accessible"
    fi
    
    log_success "Monitoring setup completed"
}

setup_ssl() {
    log_info "Setting up SSL certificates..."
    
    local ssl_dir="$PROJECT_ROOT/ssl"
    mkdir -p "$ssl_dir"
    
    if [[ "${ENVIRONMENT}" == "production" ]] && [[ ! -f "$ssl_dir/fullchain.pem" ]]; then
        log_info "Generating SSL certificates with Let's Encrypt..."
        
        # This would typically use certbot
        log_warning "SSL certificate generation requires manual setup with certbot"
        log_info "Run: certbot certonly --webroot -w /var/www/certbot -d betts.sl -d www.betts.sl"
    fi
}

post_deployment_tasks() {
    log_info "Running post-deployment tasks..."
    
    # Clear caches
    docker-compose -f "$PROJECT_ROOT/docker-compose.prod.yml" exec -T redis redis-cli flushall
    
    # Seed initial data if needed
    docker-compose -f "$PROJECT_ROOT/docker-compose.prod.yml" exec -T betts-api dotnet run --project BettsTax.Data --seed
    
    # Send deployment notification (if configured)
    if [[ -n "${SLACK_WEBHOOK_URL:-}" ]]; then
        curl -X POST -H 'Content-type: application/json' \
            --data "{\"text\":\"🚀 BettsTax deployed successfully to ${ENVIRONMENT} environment (version: ${VERSION})\"}" \
            "${SLACK_WEBHOOK_URL}"
    fi
    
    log_success "Post-deployment tasks completed"
}

cleanup() {
    log_info "Cleaning up old images and containers..."
    
    # Remove unused images
    docker image prune -f
    
    # Remove old backups (keep last 7 days)
    find "$PROJECT_ROOT/backups" -name "backup_*.sql" -mtime +7 -delete 2>/dev/null || true
    
    log_success "Cleanup completed"
}

show_deployment_info() {
    log_success "🎉 BettsTax deployment completed successfully!"
    echo
    echo "Deployment Information:"
    echo "======================"
    echo "Environment: ${ENVIRONMENT}"
    echo "Version: ${VERSION}"
    echo "Timestamp: $(date)"
    echo
    echo "Service URLs:"
    echo "============="
    echo "Application: https://betts.sl"
    echo "API: https://betts.sl/api"
    echo "Monitoring: http://localhost:3001 (Grafana)"
    echo "Metrics: http://localhost:9090 (Prometheus)"
    echo
    echo "Next Steps:"
    echo "==========="
    echo "1. Verify all services are running: docker-compose -f docker-compose.prod.yml ps"
    echo "2. Check logs: docker-compose -f docker-compose.prod.yml logs -f"
    echo "3. Monitor system health in Grafana dashboard"
    echo "4. Set up SSL certificates if not already done"
    echo "5. Configure DNS to point to your server"
    echo
}

# Main deployment flow
main() {
    log_info "🚀 Starting BettsTax deployment for Sierra Leone..."
    log_info "Environment: ${ENVIRONMENT}, Version: ${VERSION}"
    
    check_dependencies
    check_environment
    pre_deployment_checks
    setup_ssl
    backup_database
    build_images
    deploy_services
    run_migrations
    health_checks
    setup_monitoring
    post_deployment_tasks
    cleanup
    show_deployment_info
    
    log_success "🎉 Deployment completed successfully!"
}

# Handle script interruption
trap 'log_error "Deployment interrupted"; exit 1' INT TERM

# Run main function
main "$@"