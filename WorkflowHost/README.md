Migrations for Elsa are automatically generated unless they are custom.
The commands to updat the database are:

If neither of these work:
dotnet ef database update --context Elsa.Workflows.Management.Entities.ManagementDbContext --no-build
dotnet ef database update --context Elsa.Workflows.Runtime.Entities.RuntimeDbContext --no-build

Try these:
dotnet ef database update --context ManagementElsaDbContext --no-build
dotnet ef database update --context RuntimeElsaDbContext --no-build


As of 2/20/2026 Elsaworkflows must run on Microsoft.CodeAnalysis.Common 4.14.0