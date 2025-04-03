import http from 'k6/http';
import { check, sleep } from 'k6';
import faker from "k6/x/faker"

export const options = {
  vus: 1,
  iterations: 100
};

export default function () {

  const propertyId = "d571dd08-9db0-1a3d-8c1b-5cf18ebf9df2"; // Assuming you have a property with ID 1 for testing
  // Create Payment Test
  const createPaymentPayload = {
    amount: faker.payment.price(0,1000),
    paymentDate: faker.time.date("RFC3339"),
    paymentMethod: 'Bank Transfer',
    paymentReference: faker.word.word(),
    notes: faker.word.verbPhrase()
  };

  const createRes = http.post(
    `http://localhost:7000/api/properties/${propertyId}/payments`, 
    JSON.stringify(createPaymentPayload),
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );


  sleep(1);
}