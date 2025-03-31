import http from 'k6/http';
import { check, sleep } from 'k6';
import { faker } from '@faker-js/faker';

export const options = {
  stages: [
    { duration: '5s', target: 5 },
    { duration: '10s', target: 10 },
    { duration: '5s', target: 0 },
  ],
};

export default function () {
  // Create Payment Test
  const createPaymentPayload = {
    propertyId: faker.number.int({ min: 1, max: 100 }),
    tenantName: faker.person.fullName(),
    amount: faker.number.float({ min: 500, max: 5000, precision: 0.01 }),
    paymentDate: faker.date.recent().toISOString(),
    paymentMethod: faker.helpers.arrayElement(['Cash', 'Credit Card', 'Bank Transfer', 'Check']),
  };

  const createRes = http.post('http://localhost:7000/api/payments', JSON.stringify(createPaymentPayload), {
    headers: { 'Content-Type': 'application/json' },
  });

  check(createRes, {
    'create payment status is 201': (r) => r.status === 201,
  });

  // Get Payments Test
  const getRes = http.get('http://localhost:7000/api/payments');
  
  check(getRes, {
    'get payments status is 200': (r) => r.status === 200,
    'payments returned': (r) => JSON.parse(r.body).length > 0,
  });

  sleep(1);
}