# GrapheneTrace Project


GRAPHENETRACE

GrapheneTrace Edit is a web application built using ASP.NET Core (.NET 9).
It uses Entity Framework Core with a SQLite database to store and manage data.
The application runs locally in a web browser.

REQUIREMENTS
- .NET SDK 9.0 or later
- Visual Studio 2022+ or dotnet CLI or VS Code

PROJECT STRUCTURE
GrapheneTrace Edit/
- GrapheneTrace_FULL.sln
- GrapheneTrace.csproj
- Program.cs
- appsettings.json
- graphene.db
- Migrations/
- wwwroot/
- bin/
- obj/

HOW TO RUN (Visual Studio or Visual Studio Code)
1. Open GrapheneTrace_FULL.sln in (Visual Studio) or run Program.cs file in Visual Studio Code
2. Restore packages
3. Click Run or Click Run Project associated with this file

HOW TO RUN (CLI)
dotnet restore
dotnet run

DATABASE
SQLite database stored in graphene.db

AUTHENTICATION
Uses ASP.NET Core Identity

## Database
- Database: SQLite
- File: `graphene.db`
- ORM: Entity Framework Core

---

## Login Credentials (Admin)
Use the following default admin credentials to log in:

- **Email:** admin@graphenetrace.com  
- **Password:** Admin@123

---

## Authentication
The application uses **ASP.NET Core Identity** for authentication and authorization.

---