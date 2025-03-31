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
  // Create Property Test
  const createPropertyPayload = {
    address: faker.location.streetAddress(),
    city: faker.location.city(),
    state: faker.location.state(),
    zipCode: faker.location.zipCode(),
    bedrooms: faker.number.int({ min: 1, max: 5 }),
    bathrooms: faker.number.int({ min: 1, max: 3 }),
    monthlyRent: faker.number.float({ min: 1000, max: 5000, precision: 0.01 }),
  };

  const createRes = http.post('http://localhost:7000/properties', JSON.stringify(createPropertyPayload), {
    headers: { 'Content-Type': 'application/json' },
  });

  check(createRes, {
    'create property status is 201': (r) => r.status === 201,
  });

  // Get Properties Test
  const getRes = http.get('http://localhost:7000/properties');
  
  check(getRes, {
    'get properties status is 200': (r) => r.status === 200,
    'properties returned': (r) => JSON.parse(r.body).length > 0,
  });

  sleep(1);
}