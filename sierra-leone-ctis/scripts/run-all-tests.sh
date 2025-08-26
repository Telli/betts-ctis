#!/bin/bash

# CTIS Comprehensive Test Suite Runner
# This script runs all integration tests, performance tests, and security tests

set -e

echo "🚀 Starting CTIS Comprehensive Test Suite"
echo "=========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if required dependencies are available
check_dependencies() {
    print_status "Checking dependencies..."
    
    if ! command -v node &> /dev/null; then
        print_error "Node.js is not installed"
        exit 1
    fi
    
    if ! command -v pnpm &> /dev/null; then
        print_error "pnpm is not installed"
        exit 1
    fi
    
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET is not installed"
        exit 1
    fi
    
    print_success "All dependencies are available"
}

# Start backend services
start_backend() {
    print_status "Starting backend services..."
    
    cd ../BettsTax/BettsTax.Web
    
    # Check if backend is already running
    if curl -s http://localhost:5000/health > /dev/null 2>&1; then
        print_warning "Backend is already running"
    else
        print_status "Starting .NET backend..."
        dotnet run --urls=http://localhost:5000 &
        BACKEND_PID=$!
        
        # Wait for backend to start
        for i in {1..30}; do
            if curl -s http://localhost:5000/health > /dev/null 2>&1; then
                print_success "Backend started successfully"
                break
            fi
            sleep 2
        done
        
        if ! curl -s http://localhost:5000/health > /dev/null 2>&1; then
            print_error "Failed to start backend"
            exit 1
        fi
    fi
    
    cd ../../sierra-leone-ctis
}

# Start frontend services
start_frontend() {
    print_status "Starting frontend services..."
    
    # Check if frontend is already running
    if curl -s http://localhost:3000 > /dev/null 2>&1; then
        print_warning "Frontend is already running"
    else
        print_status "Installing frontend dependencies..."
        pnpm install
        
        print_status "Starting Next.js frontend..."
        pnpm dev &
        FRONTEND_PID=$!
        
        # Wait for frontend to start
        for i in {1..30}; do
            if curl -s http://localhost:3000 > /dev/null 2>&1; then
                print_success "Frontend started successfully"
                break
            fi
            sleep 2
        done
        
        if ! curl -s http://localhost:3000 > /dev/null 2>&1; then
            print_error "Failed to start frontend"
            exit 1
        fi
    fi
}

# Run backend unit tests
run_backend_unit_tests() {
    print_status "Running backend unit tests..."
    
    cd ../BettsTax
    
    # Run Core tests
    print_status "Running Core service tests..."
    dotnet test BettsTax.Core.Tests --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
    
    # Run Web tests
    print_status "Running Web API tests..."
    dotnet test BettsTax.Web.Tests --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
    
    # Run Data tests
    print_status "Running Data layer tests..."
    dotnet test BettsTax.Data.Tests --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
    
    print_success "Backend unit tests completed"
    cd ../sierra-leone-ctis
}

# Run backend integration tests
run_backend_integration_tests() {
    print_status "Running backend integration tests..."
    
    cd ../BettsTax
    
    # Run integration tests with test database
    print_status "Running KPI service integration tests..."
    dotnet test BettsTax.Core.Tests --filter "Category=Integration" --logger "console;verbosity=normal"
    
    print_success "Backend integration tests completed"
    cd ../sierra-leone-ctis
}

# Run frontend end-to-end tests
run_e2e_tests() {
    print_status "Running end-to-end tests..."
    
    # Run Playwright tests
    print_status "Running authentication tests..."
    pnpm exec playwright test tests/e2e/auth.spec.ts --reporter=html
    
    print_status "Running client portal tests..."
    pnpm exec playwright test tests/e2e/client-portal.spec.ts --reporter=html
    
    print_status "Running admin interface tests..."
    pnpm exec playwright test tests/e2e/admin-interface.spec.ts --reporter=html
    
    print_status "Running KPI dashboard tests..."
    pnpm exec playwright test tests/e2e/kpi-dashboard.spec.ts --reporter=html
    
    print_status "Running reports integration tests..."
    pnpm exec playwright test tests/e2e/reports-integration.spec.ts --reporter=html
    
    print_status "Running payment gateway tests..."
    pnpm exec playwright test tests/e2e/payment-gateway-integration.spec.ts --reporter=html
    
    print_success "End-to-end tests completed"
}

# Run system integration tests
run_system_integration_tests() {
    print_status "Running full system integration tests..."
    
    pnpm exec playwright test tests/e2e/full-system-integration.spec.ts --reporter=html
    
    print_success "System integration tests completed"
}

# Run performance tests
run_performance_tests() {
    print_status "Running performance and load tests..."
    
    pnpm exec playwright test tests/performance/load-testing.spec.ts --reporter=html
    
    print_success "Performance tests completed"
}

# Run security tests
run_security_tests() {
    print_status "Running security and penetration tests..."
    
    pnpm exec playwright test tests/security/security-testing.spec.ts --reporter=html
    
    print_success "Security tests completed"
}

# Run accessibility tests
run_accessibility_tests() {
    print_status "Running accessibility tests..."
    
    pnpm exec playwright test tests/e2e/accessibility.spec.ts --reporter=html
    
    print_success "Accessibility tests completed"
}

# Generate test coverage report
generate_coverage_report() {
    print_status "Generating test coverage report..."
    
    # Backend coverage
    cd ../BettsTax
    dotnet tool install --global dotnet-reportgenerator-globaltool 2>/dev/null || true
    reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"../sierra-leone-ctis/test-results/coverage-backend" -reporttypes:Html
    
    cd ../sierra-leone-ctis
    
    # Frontend coverage (if available)
    if [ -d "coverage" ]; then
        cp -r coverage test-results/coverage-frontend
    fi
    
    print_success "Coverage reports generated"
}

# Cleanup function
cleanup() {
    print_status "Cleaning up..."
    
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null || true
    fi
    
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null || true
    fi
    
    # Kill any remaining processes
    pkill -f "dotnet.*BettsTax.Web" 2>/dev/null || true
    pkill -f "next-server" 2>/dev/null || true
    
    print_success "Cleanup completed"
}

# Set trap for cleanup on exit
trap cleanup EXIT

# Main execution
main() {
    print_status "CTIS Test Suite - $(date)"
    
    # Parse command line arguments
    RUN_UNIT=true
    RUN_INTEGRATION=true
    RUN_E2E=true
    RUN_PERFORMANCE=false
    RUN_SECURITY=false
    RUN_ACCESSIBILITY=false
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --unit-only)
                RUN_INTEGRATION=false
                RUN_E2E=false
                shift
                ;;
            --integration-only)
                RUN_UNIT=false
                RUN_E2E=false
                shift
                ;;
            --e2e-only)
                RUN_UNIT=false
                RUN_INTEGRATION=false
                shift
                ;;
            --include-performance)
                RUN_PERFORMANCE=true
                shift
                ;;
            --include-security)
                RUN_SECURITY=true
                shift
                ;;
            --include-accessibility)
                RUN_ACCESSIBILITY=true
                shift
                ;;
            --all)
                RUN_PERFORMANCE=true
                RUN_SECURITY=true
                RUN_ACCESSIBILITY=true
                shift
                ;;
            *)
                print_error "Unknown option: $1"
                echo "Usage: $0 [--unit-only|--integration-only|--e2e-only] [--include-performance] [--include-security] [--include-accessibility] [--all]"
                exit 1
                ;;
        esac
    done
    
    # Create test results directory
    mkdir -p test-results
    
    # Check dependencies
    check_dependencies
    
    # Start services if running integration or e2e tests
    if [ "$RUN_INTEGRATION" = true ] || [ "$RUN_E2E" = true ]; then
        start_backend
        start_frontend
        
        # Wait for services to be fully ready
        sleep 5
    fi
    
    # Run test suites based on parameters
    if [ "$RUN_UNIT" = true ]; then
        run_backend_unit_tests
    fi
    
    if [ "$RUN_INTEGRATION" = true ]; then
        run_backend_integration_tests
    fi
    
    if [ "$RUN_E2E" = true ]; then
        run_e2e_tests
        run_system_integration_tests
    fi
    
    if [ "$RUN_PERFORMANCE" = true ]; then
        run_performance_tests
    fi
    
    if [ "$RUN_SECURITY" = true ]; then
        run_security_tests
    fi
    
    if [ "$RUN_ACCESSIBILITY" = true ]; then
        run_accessibility_tests
    fi
    
    # Generate coverage reports
    generate_coverage_report
    
    print_success "🎉 All test suites completed successfully!"
    print_status "Test reports available in:"
    print_status "  - Playwright HTML reports: playwright-report/"
    print_status "  - Backend coverage: test-results/coverage-backend/"
    print_status "  - Frontend coverage: test-results/coverage-frontend/"
    
    # Summary
    echo ""
    echo "📊 Test Suite Summary"
    echo "===================="
    echo "✅ Backend Unit Tests: $([ "$RUN_UNIT" = true ] && echo "Completed" || echo "Skipped")"
    echo "✅ Backend Integration Tests: $([ "$RUN_INTEGRATION" = true ] && echo "Completed" || echo "Skipped")"
    echo "✅ End-to-End Tests: $([ "$RUN_E2E" = true ] && echo "Completed" || echo "Skipped")"
    echo "✅ System Integration Tests: $([ "$RUN_E2E" = true ] && echo "Completed" || echo "Skipped")"
    echo "🚀 Performance Tests: $([ "$RUN_PERFORMANCE" = true ] && echo "Completed" || echo "Skipped")"
    echo "🔒 Security Tests: $([ "$RUN_SECURITY" = true ] && echo "Completed" || echo "Skipped")"
    echo "♿ Accessibility Tests: $([ "$RUN_ACCESSIBILITY" = true ] && echo "Completed" || echo "Skipped")"
}

# Run main function
main "$@"