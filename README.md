# XAF Password Reset

## Features

XAF Password Reset is a password reset strategy that allows users to request for a new password during 
the login sequence and send an email with an api-endpoint to reset their password.

The application is built using the following technologies:

- [x] Email Service: any...
- [x] Frontend: Blazor Server
- [x] Backend: ASP.NET Core 8.0
- [x] Database: SQLite
- [x] ORM: XPO

## Features ToDo

- [ ] Instead of emailing a temp password to the user, email a link to a page where the user can reset their password.
- [ ] When database is accessed via our endpoint the db-updater kicks in and updates the database schema without a assigned tentantName. This constructs new tenants into the separated tenant databases. See the code on Updater.cs line 36-42. Is there a way to switch tenant in code?  
- [ ] Add delight to the experience when all tasks are complete :tada:
