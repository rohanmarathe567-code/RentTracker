import http from 'k6/http';
import { check, sleep } from 'k6';
import faker from "k6/x/faker"

export const options = {
  vus: 1,
  iterations: 6
};

export default function () {
  // Create Property Test
  const createPropertyPayload = {
    address: faker.address.street(),
    city: faker.address.city(),
    state: faker.address.state(),
    zipCode: faker.address.zip(),
    bedrooms: faker.numbers.intRange(1,5),
    bathrooms: faker.numbers.intRange(1,5),
    monthlyRent: faker.payment.price(0,1000),
  };

  const createRes = http.post('http://localhost:7000/api/properties', JSON.stringify(createPropertyPayload), {
    headers: { 'Content-Type': 'application/json' },
  });

  check(createRes, {
    'create property status is 201': (r) => r.status === 201,
  });

  // Get Properties Test
  const getRes = http.get('http://localhost:7000/api/properties');
  
  check(getRes, {
    'get properties status is 200': (r) => r.status === 200,
    'properties returned': (r) => JSON.parse(r.body).length > 0,
  });

  sleep(1);
}
