#!/bin/bash

# Define the URL and headers for the curl command
URL="http://order-management-api.10.0.1.201.sslip.io/"
HEADER_ACCEPT="accept: */*"
HEADER_CONTENT_TYPE="Content-Type: application/json"
DATA='{
  "id": "string",
  "customerId": "string",
  "productId": "string",
  "quantity": 0
}'

# Prompt the user to enter the total number of requests
read -p "Enter the total number of requests to send: " total_requests

# Temporary file to store the status codes
temp_file=$(mktemp)

# Function to send a single request and save the status code to the temp file
send_request() {
    status_code=$(curl -s -o /dev/null -w "%{http_code}" -X 'POST' "$URL" -H "$HEADER_ACCEPT" -H "$HEADER_CONTENT_TYPE" -d "$DATA")
    echo "$status_code" >> "$temp_file"
    echo "Status Code: $status_code"
}

# Initialize counter
i=1

# Loop to start the specified number of asynchronous curl requests
while [ "$i" -le "$total_requests" ]
do
    send_request &
    sleep 0.008
    i=$((i + 1))
done

# Wait for all background processes to complete
wait

# Count the occurrences of status codes
total_200=$(grep -c '^200$' "$temp_file")
total_5xx=$(grep -c '^5[0-9][0-9]$' "$temp_file")

# Output the results
echo "Total 200 responses: $total_200"
echo "Total 5xx responses: $total_5xx"

# Clean up the temporary file
rm "$temp_file"

