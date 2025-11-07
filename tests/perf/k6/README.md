# k6 Load Testing

## Installation

```bash
# macOS
brew install k6

# Linux
wget https://github.com/grafana/k6/releases/download/v0.48.0/k6-v0.48.0-linux-amd64.tar.gz
tar -xzf k6-v0.48.0-linux-amd64.tar.gz
sudo mv k6 /usr/local/bin/

# Windows (via Chocolatey)
choco install k6
```

## Running Tests

```bash
# Basic test
k6 run api-load-test.js

# With custom URL
k6 run --env BASE_URL=https://api.hearloveen.com api-load-test.js

# With authentication
k6 run --env AUTH_TOKEN=your_token_here api-load-test.js

# Save results to JSON
k6 run --out json=test-results.json api-load-test.js
```

## Test Scenarios

- **api-load-test.js**: General API load testing with gradual ramp-up

## Thresholds

- 95th percentile response time < 500ms
- Error rate < 5%

## Results

Test results are saved to `summary.json` and displayed in the console.
