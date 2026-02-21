using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var dbConnection = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("dbConnection is null: Database connection string cannot be null");

builder.Services.AddElsa(elsa =>
{
    // Configure Management layer to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlServer(dbConnection)));

    // Configure Runtime layer to use EF Core.
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlServer(dbConnection)));

    // Default Identity features for authentication/authorization.
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // TODO: ADD 256 bit signing key.
        identity.UseAdminUserProvider();
    });

    // Configure ASP.NET authentication/authorization.
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

    // Expose Elsa API endpoints.
    elsa.UseWorkflowsApi();

    // Setup a SignalR hub for real-time updates from the server.
    elsa.UseRealTimeWorkflows();

    // Enable C# workflow expressions
    elsa.UseCSharp();

    // Enable JavaScript workflow expressions
    elsa.UseJavaScript(options => options.AllowClrAccess = true);

    // Enable HTTP activities.
    elsa.UseHttp(options => options.ConfigureHttpOptions = httpOptions => httpOptions.BaseUrl = new("https://localhost:5001"));

    // Use timer activities.
    elsa.UseScheduling();

    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() //TODO: For development purposes only. Use a specific origin for production.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-elsa-workflow-instance-id"))); //TODO: Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.


// Add Swagger/Swashbuckle if it is not design-time and it is development environment.
var isDesignTime = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.Contains("Design") ?? false;
if (!isDesignTime && builder.Environment.IsDevelopment())
{
    try
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type =>
            {
                // For nested types, include parent class name to avoid conflicts
                var fullName = type.FullName ?? type.Name;

                // Replace + with . for nested types and remove special characters
                return fullName.Replace("+", ".").Replace("`", "");
            });
        });
    }
    catch
    {
        // If Swagger/Swashbuckle is not available, we can ignore the exception since it's only for development/design-time purposes.
    }

}

// Add Health Checks.
builder.Services.AddHealthChecks();

// Add API Explorer for Swagger/Swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Build the web application.
var app = builder.Build();

// Configure web application's middleware pipeline.
app.UseCors();
app.UseRouting(); // Required for SignalR.
if (!isDesignTime && app.Environment.IsDevelopment()) // If it is not DesignTime, but it is development environment, use Swagger.
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workflow API v1");
        c.RoutePrefix = "swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger"));
}
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 
app.MapHealthChecks("/health"); // Verify endpoingt mapping for health checks is complete and correct.
app.Run();