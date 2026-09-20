@feature3 @tags

Feature: Manage tags
	In order to keep the tag vocabulary tidy
	As a user
	I want to rename, merge and delete tags

Background:
	Given the API is running with a clean database

Scenario: Rename a tag
	Given a tag named "cleanup" with color "#000000"
	When I rename that tag to "architecture" with color "#123456"
	Then the tag "architecture" exists with color "#123456"
	And the tag "cleanup" does not exist

Scenario: Merge a tag into another reassigns notes
	Given a workspace named "Merge"
	And a tag named "web" with color "#000000"
	And a note tagged "web, backend" in that workspace
	And a note tagged "web, backend" in that workspace
	When I merge tag "web" into tag "backend"
	Then the tag "backend" has 2 notes
	And the tag "web" does not exist