@health

Feature: API health
	In order to know the API is running
	As a desktop client
	I want to query its health endpoint

Scenario: Healthy service is reported
	Given the API is running
	When I request the health endpoint
	Then the response has status "Healthy"