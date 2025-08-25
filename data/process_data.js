const fs = require('fs');

// Read the data.tsv file
const rawData = fs.readFileSync('/home/gsej/repos/me-tracker/data/data.tsv', 'utf8');
const lines = rawData.trim().split('\n');

// Process each line
const weightRecords = lines.map(line => {
    const [dateStr, weightStr] = line.split('\t');

    // Parse the date (from DD/MM/YY to ISO format YYYY-MM-DDT08:00:00Z)
    const [day, month, year] = dateStr.split('/');
    // Assume the year is 20YY
    const fullYear = `20${year}`;
    const isoDate = `${fullYear}-${month}-${day}T08:00:00Z`;

    // Parse the weight
    const weight = parseFloat(weightStr);

    return {
        date: isoDate,
        weight: weight
    };
});

// Create the final JSON object
const jsonData = {
    weightRecords: weightRecords
};

// Write to a new JSON file
fs.writeFileSync('/home/gsej/repos/me-tracker/data/weight_records.json', JSON.stringify(jsonData, null, 2));

console.log('Successfully created weight_records.json');
