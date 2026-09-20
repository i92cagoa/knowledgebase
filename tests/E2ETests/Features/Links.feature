@feature5 @links

Feature: Import an article link as a note
	In order to capture articles quickly
	As a user
	I want to add a link that becomes a note with analyzed tags and a summary

Background:
	Given the API is running with a clean database

Scenario: Import a link creates an analyzed note
	Given a workspace named "Inbox"
	When I import the link "https://example.com/article"
	Then a note exists with source "https://example.com/article"
	And that note has the summary of the article
	And that note has the tag "postgres"