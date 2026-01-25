"use strict";

describe('backup users', () => {
  const baseUrl = 'http://localhost:5200/api';
  const validApiKey = 'apikey';

  const sampleData = {
    users: [
      { userId: "ApiUser", heightInCm: 150 }
    ]
  };

  it('should restore and backup data correctly with valid API key', async () => {

    // Restore data
    const restoreResponse = await fetch(`${baseUrl}/backup/users/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-Key': validApiKey
      },
      body: JSON.stringify(sampleData)
    });
    expect(restoreResponse.status).toBe(200);

    // Get backup
    const backupResponse = await fetch(`${baseUrl}/backup/users`, {
      headers: {
        'X-API-Key': validApiKey
      }
    });
    expect(backupResponse.status).toBe(200);

    const backupData = await backupResponse.json();
    expect(backupData).toEqual(sampleData);   
    
  });

  it('should return 401 when calling backup endpoint without API key', async () => {
    const response = await fetch(`${baseUrl}/backup/users`);
    expect(response.status).toBe(401);
  });

  it('should return 401 when calling restore endpoint without API key', async () => {
    const response = await fetch(`${baseUrl}/backup/users/restore`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(sampleData)
    });
    expect(response.status).toBe(401);
  });
});
