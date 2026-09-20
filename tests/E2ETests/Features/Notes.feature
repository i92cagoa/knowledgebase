@feature1 @notes

Feature: Manage notes in workspaces
	In order to organize knowledge
	As a user of the desktop app
	I want to create and organize notes inside workspaces with tags

Background:
	Given the API is running with a clean database

Scenario: Create a note with tags inside a workspace
	Given a workspace named "Architecture"
	When I create a note titled "EF Core" in that workspace
	And I add tags "dotnet, database" to the note
	Then the note is stored under the workspace
	And the note has tags "dotnet, database"

Scenario: Reusing a tag keeps a single tag
	Given a workspace named "Dev"
	When I create a note titled "Note A" in that workspace
	And I add tags "dotnet" to the note
	And I create a note titled "Note B" in that workspace
	And I add tags "dotnet" to the note
	Then the tag "dotnet" exists exactly once