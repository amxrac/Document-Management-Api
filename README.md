# Document Management API

## Overview

This is a .NET 8 Web API for managing documents, users, and reports. It includes authentication, role-based access control, document tagging, metadata management, and report generation in JSON, Excel, and PDF formats.

## Features

- **User Management**: Authentication and authorization with roles (Admin, Editor)
- **JWT**: For securing endpoints
- **Document Metadata**: CRUD operations for document metadata
- **Tagging System**: Add, update, and retrieve document tags
- **Audit Logging**: Logs user modifications to documents and metadata
- **Reports**: Generate document reports in JSON, Excel, and PDF
- **Rate Limiting**: Prevents excessive API requests


## Tech Stack

- **.NET 8**
- **SQL Server**
- **Entity Framework Core**
- **Identity Framework** (User Management)
- **iText & ClosedXML** (PDF & Excel Report Generation)

## Setup Instructions

### Prerequisites

- **.NET 8
- **SQL Server**

### Installation

1. Clone the repository:
    
    ```sh
    git clone https://github.com/amxrac/Document-Management-Api
    cd Document-Management-Api
    ```
    
2. Configure the database connection in `appsettings.json`:
    
    ```json
    "ConnectionStrings": {
        "DefaultConnection": "Server=your_server;Database=DMSDB;User Id=your_user;Password=your_password;"
    }
    ```
    
3. Restore dependencies:

    ```sh
    dotnet restore
    ```

4. Apply migrations:

    ```sh
    dotnet ef database update
    ```

5. Run the API:

    ```sh
    dotnet run
    ```


## API Endpoints

### Authentication

- `POST /api/auth/register` – Register a new user
- `POST /api/auth/login` – Authenticate and get a bearer token

### Document Management

- `GET /api/documents` – Get all documents
- `POST /api/documents` – Create a new document
- `PUT /api/documents/{id}` – Update document metadata
### Tags

- `GET /api/tags` – Get all tags

### Reports

- `GET /api/reports?format=json` – Get reports in JSON
- `GET /api/reports?format=excel` – Download reports as Excel
- `GET /api/reports?format=pdf` – Download reports as PDF

---

