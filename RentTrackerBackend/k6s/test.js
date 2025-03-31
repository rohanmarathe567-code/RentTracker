import faker from "k6/x/faker"

export default function() {
    const createPropertyPayload = {
        address: faker.address.street(),
        city: faker.address.city(),
        state: faker.address.state(),
        zipCode: faker.address.zip(),
        bedrooms: faker.numbers.intRange(1,5),
        bathrooms: faker.numbers.intRange(1,5),
        monthlyRent: faker.payment.price(0,1000),
    };

    console.log(createPropertyPayload)
}
