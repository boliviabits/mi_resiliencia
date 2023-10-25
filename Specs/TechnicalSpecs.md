# Technical specifications of https://github.com/GEOTEST-AG/MiResiliencia 
## Framework 
Microsoft .NET 6.0 Core

## Directory Structure

MiResiliencia/ 
├── .git/ # Git version control directory 
├── .github/ # GitHub-specific configurations 
├── MiResiliencia / # Source code 
│ ├── App_Data/Import / # Temporary imported project files 
│ ├── Areas/Identity/Pages # Identity Management Sites and Logic 
│ ├── Components/ # Views Components 
│ ├── Helpers/ # Helper Classes
│ ├── Models/ # Model classes, DB abstraction layer
│ ├── Views/ # Razor views 
│ ├── wwwroot/ # Static web assets (CSS, JavaScript, images) 
│ ├── appsettings.json # Application settings 
│ ├── Program.cs # Application startup configuration
│ ├── Dockerfile # Docker generation file
│ └── ... 
├── README.md # Project documentation 
├── LICENSE # License information 
└── ...

## Packages 

### Most important packages

#### ASP.NET Core
- **Description**: ASP.NET Core is a high-performance, cross-platform framework for building modern, cloud-based, and internet-connected applications.
- **GitHub**: [ASP.NET Core Repository](https://github.com/dotnet/aspnetcore)

#### Entity Framework Core
- **Description**: Entity Framework Core is an object-relational mapping (ORM) framework for .NET. It simplifies database interactions in your application.
- **GitHub**: [Entity Framework Core Repository](https://github.com/dotnet/efcore)

#### Newtonsoft.Json
- **Description**: Newtonsoft.Json is a popular JSON framework for .NET that helps with JSON serialization and deserialization.
- **GitHub**: [Newtonsoft.Json Repository](https://github.com/JamesNK/Newtonsoft.Json)

#### Microsoft.AspNetCore.Identity
- **Description**: AspNetCore.Identity is an API that supports user interface (UI) login functionality.
- **GitHub**: [Microsoft.AspNetCore.Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-7.0&tabs=visual-studio)

#### Bootstrap
- **Description**: Bootstrap is a widely-used open-source CSS framework that provides responsive design and user interface components for web applications.
- **GitHub**: [Bootstrap Repository](https://github.com/twbs/bootstrap)

#### jQuery
- **Description**: jQuery is a fast, small, and feature-rich JavaScript library that simplifies HTML document traversal and manipulation, event handling, and AJAX interactions.
- **GitHub**: [jQuery Repository](https://github.com/jquery/jquery)

#### Openlayers
- **Description**: OpenLayers makes it easy to put a dynamic map in any web page. It can display map tiles, vector data and markers loaded from any source.
- **GitHub**: [Openlayers](https://openlayers.org/)



```PM> Get-Package | Select-Object Id,LicenseUrl```

| Id                                                     | LicenseUrl                             |
| --                                                     | ----------                             |
| Microsoft.VisualStudio.Web.CodeGeneration.Design       | https://licenses.nuget.org/Apache-2.0  |
| Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite | https://licenses.nuget.org/PostgreSQL  |
| Microsoft.EntityFrameworkCore.SqlServer                | https://licenses.nuget.org/MIT         |
| EFCore.NamingConventions                               | https://licenses.nuget.org/Apache-2.0  |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore      | https://licenses.nuget.org/MIT         |
| Microsoft.EntityFrameworkCore.Relational               | https://licenses.nuget.org/MIT         |
| Npgsql.EntityFrameworkCore.PostgreSQL                  | https://licenses.nuget.org/PostgreSQL  |
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore   | https://licenses.nuget.org/MIT         |
| Microsoft.AspNetCore.Identity.UI                       | https://licenses.nuget.org/MIT         |
| NLog.Web.AspNetCore                                    | https://licenses.nuget.org/BSD-3-Clause|
| Microsoft.EntityFrameworkCore.Tools                    | https://licenses.nuget.org/MIT         |
| Microsoft.EntityFrameworkCore.Proxies                  | https://licenses.nuget.org/MIT         |

## Database
The database schema is created using Entity Framework Core and Code first generation. The tables and relations are completely mapped in the code (models).
Additionally, views for export and GeoServer connection were developed.
