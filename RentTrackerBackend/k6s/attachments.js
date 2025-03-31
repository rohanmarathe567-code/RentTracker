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
  // Create Attachment Test
  const createAttachmentPayload = {
    propertyId: faker.number.int({ min: 1, max: 100 }),
    documentType: faker.helpers.arrayElement(['Lease', 'Invoice', 'Maintenance Report', 'Insurance']),
    documentName: `${faker.lorem.word()}_${faker.string.uuid()}.pdf`,
    uploadDate: faker.date.recent().toISOString(),
    description: faker.lorem.sentence(),
  };

  const createRes = http.post('http://localhost:7000/api/attachments', JSON.stringify(createAttachmentPayload), {
    headers: { 'Content-Type': 'application/json' },
  });

  check(createRes, {
    'create attachment status is 201': (r) => r.status === 201,
  });

  // Get Attachments Test
  const getRes = http.get('http://localhost:7000/api/attachments');
  
  check(getRes, {
    'get attachments status is 200': (r) => r.status === 200,
    'attachments returned': (r) => JSON.parse(r.body).length > 0,
  });

  sleep(1);
}