# RentTracker Performance Tests

## Prerequisites
- k6 installed
- Node.js and npm
- @faker-js/faker package

## Setup
1. Install dependencies:
```bash
npm install
```

## Running Tests
To run individual test scripts:
```bash
k6 run health.js
k6 run properties.js
k6 run payments.js
k6 run attachments.js
```

## Test Descriptions
- `health.js`: Tests the health endpoint for basic server responsiveness
- `properties.js`: Performance tests for property creation and retrieval
- `payments.js`: Performance tests for payment creation and retrieval
- `attachments.js`: Performance tests for document attachment creation and retrieval

## Performance Test Configuration
Each script includes:
- Virtual User (VU) scaling
- Duration configuration
- Faker.js for realistic test data generation

## Notes
- Ensure the backend server is running on localhost:7000 (HTTP) or localhost:7001 (HTTPS)
- Adjust endpoint URLs if your server configuration differs