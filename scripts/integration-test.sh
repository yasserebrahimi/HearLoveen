#!/bin/bash

# HearLoveen Integration Test Script
# Tests all services to ensure they're running correctly

set -e  # Exit on error

echo "========================================="
echo "HearLoveen Integration Test Suite"
echo "========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
PASSED=0
FAILED=0

# Function to test HTTP endpoint
test_endpoint() {
    local name=$1
    local url=$2
    local expected_code=${3:-200}

    echo -n "Testing $name... "

    response=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")

    if [ "$response" -eq "$expected_code" ]; then
        echo -e "${GREEN}✓ PASS${NC} (HTTP $response)"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAIL${NC} (Expected HTTP $expected_code, got $response)"
        ((FAILED++))
    fi
}

# Function to test service with health check
test_health() {
    local name=$1
    local url=$2

    echo -n "Testing $name health... "

    response=$(curl -s "$url" 2>/dev/null || echo "error")

    if [ "$response" != "error" ]; then
        echo -e "${GREEN}✓ PASS${NC}"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAIL${NC}"
        ((FAILED++))
    fi
}

# Function to check if service is listening on port
test_port() {
    local name=$1
    local port=$2

    echo -n "Testing $name port $port... "

    if nc -z localhost "$port" 2>/dev/null; then
        echo -e "${GREEN}✓ PASS${NC}"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAIL${NC}"
        ((FAILED++))
    fi
}

echo "1. Infrastructure Services"
echo "-------------------------"
test_port "PostgreSQL" 5432
test_port "Redis" 6379
test_port "RabbitMQ" 5672
test_port "RabbitMQ Management" 15672
test_port "Kafka" 9092
echo ""

echo "2. Core Microservices"
echo "--------------------"
test_endpoint "API Gateway" "http://localhost:5000/health" 200
test_endpoint "AudioService" "http://localhost:5001/health" 200
test_endpoint "AnalysisService" "http://localhost:5002/health" 200
test_endpoint "NotificationService" "http://localhost:5003/health" 200
test_endpoint "UserService" "http://localhost:5004/health" 200
test_endpoint "IoTService" "http://localhost:5005/health" 200
echo ""

echo "3. New Services"
echo "---------------"
test_endpoint "AnalysisProxy" "http://localhost:5100/health" 200
test_endpoint "Privacy.API" "http://localhost:5200/health" 200
test_endpoint "Analytics" "http://localhost:5300/health" 200
echo ""

echo "4. ML & Monitoring"
echo "------------------"
test_endpoint "ML API" "http://localhost:8000/health" 200
test_endpoint "Prometheus" "http://localhost:9090/-/healthy" 200
test_endpoint "Grafana" "http://localhost:3000/api/health" 200
echo ""

echo "5. Frontend Applications"
echo "------------------------"
test_endpoint "Therapist Dashboard" "http://localhost:5173" 200
echo ""

echo "6. Database Connectivity"
echo "------------------------"
echo -n "Testing PostgreSQL connection... "
if PGPASSWORD="${POSTGRES_PASSWORD:-postgres}" psql -h localhost -U postgres -d hearloveen -c "SELECT 1" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi
echo ""

echo "7. Redis Connectivity"
echo "---------------------"
echo -n "Testing Redis connection... "
if redis-cli -h localhost ping > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi
echo ""

echo "8. RabbitMQ Connectivity"
echo "------------------------"
echo -n "Testing RabbitMQ Management API... "
response=$(curl -s -u guest:guest "http://localhost:15672/api/overview" 2>/dev/null || echo "error")
if [ "$response" != "error" ]; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi
echo ""

echo "9. API Endpoint Tests"
echo "---------------------"
echo -n "Testing ML API docs... "
response=$(curl -s "http://localhost:8000/docs" 2>/dev/null || echo "error")
if [ "$response" != "error" ]; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi

echo -n "Testing API Gateway routing... "
response=$(curl -s "http://localhost:5000/api/health" 2>/dev/null || echo "error")
if [ "$response" != "error" ]; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi
echo ""

echo "========================================="
echo "Test Results Summary"
echo "========================================="
echo -e "Total Tests: $((PASSED + FAILED))"
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed! System is ready for production.${NC}"
    exit 0
else
    echo -e "${RED}⚠️  Some tests failed. Please review the errors above.${NC}"
    exit 1
fi
