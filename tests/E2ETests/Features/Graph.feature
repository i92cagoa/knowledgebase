@feature4 @graph

Feature: Notes graph
	In order to visualize how notes relate
	As a user
	I want to see notes connected by shared tags

Background:
	Given the API is running with a clean database

Scenario: Graph connects notes sharing tags
	Given a workspace named "Graph"
	And a note tagged "shared, extra" in that workspace
	And a note tagged "shared" in that workspace
	And a note tagged "other" in that workspace
	Then the graph has 3 nodes
	And the graph has 1 edge