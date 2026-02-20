Migrations for Elsa are automatically generated unless they are custom.
The commands to updat the database are:

dotnet ef database update --context RuntimeElsaDbContext
dotnet ef database update --context ManagementElsaDbContext

As of 2/20/2026 Elsaworkflows must run on Microsoft.CodeAnalysis.Common 4.14.0