"use strict";

describe('weights', () => {
  const baseUrl = 'http://localhost:5200/api';
  const validApiKey = 'apikey';

  const sampleData = {
    weightRecords: [
      { weightId: "955c82e8-124a-427b-9160-358db7e51e41", date: "2025-04-10T00:00:00Z", weight: 71.0 },
      { weightId: "5bf0a60a-58d9-4136-8b4c-85a82e34fb02", date: "2025-04-11T00:00:00Z", weight: 70.5 }
    ]
  };

  beforeAll(async () => {

    // put the system in a known state before testing
    const restoreResponse = await fetch(`${baseUrl}/backup/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': validApiKey
      },
      body: JSON.stringify(sampleData)
    });
    expect(restoreResponse.status).toBe(200);
  });


  it('should return all weights correctly with valid API key', async () => {

    const backupResponse = await fetch(`${baseUrl}/weights`, {
      headers: {
        'X-API-Key': validApiKey
      }
    });
    expect(backupResponse.status).toBe(200);

    const backupData = await backupResponse.json();
    expect(backupData).toEqual(sampleData);

  });

  it('should return 401 when calling backup endpoint without API key', async () => {
    const response = await fetch(`${baseUrl}/weights`);
    expect(response.status).toBe(401);
  });
});
