using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;


var builder = WebApplication.CreateBuilder(args);
var configManager = builder.Configuration;
var dbConnection = configManager.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("dbConnection is null: Database connection string cannot be null");
var signingKey = configManager["Identity:SigningKey"] ?? throw new ArgumentNullException("signingKey is null: Token signing key cannot be null");
var apiBaseUrl = configManager["Api:BaseUrl"] ?? throw new ArgumentNullException("apiBaseUrl is null: API base URL cannot be null");
var apiBasePath = configManager["Api:BasePath"] ?? throw new ArgumentNullException("apiBasePath is null: API base path cannot be null");

builder.WebHost.UseStaticWebAssets();

builder.Services.AddElsa(elsa =>
{
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = token => token
            .SigningKey = signingKey; // TODO: Replace temp 256 bit signing key before launch.
        identity.UseAdminUserProvider();
    }); // Setup Identity features for authentication/authorization.
    elsa.UseDefaultAuthentication(authentication => authentication
        .UseAdminApiKey()); // Configure ASP.NET authentication/authorization.
    elsa.UseWorkflowManagement(workflowManagement => 
        workflowManagement.UseEntityFrameworkCore(ef => ef
            .UseSqlServer(dbConnection))); // Configure Management layer to use EF Core.    
    elsa.UseWorkflowRuntime(runtime => 
        runtime.UseEntityFrameworkCore(ef => ef
            .UseSqlServer(dbConnection))); // Configure Runtime layer to use EF Core.    
    elsa.UseScheduling(); // Use timer activities.                             
    elsa.UseJavaScript(javaScript => javaScript
        .AllowClrAccess = true); // Enable JavaScript workflow expressions
    elsa.UseLiquid(); // Enable Liquid workflow expressions    
    elsa.UseCSharp(); // Enable C# workflow expressions
    elsa.UseHttp(http => http
        .ConfigureHttpOptions = httpConfig => configManager
            .GetSection("Http")
            .Bind(httpConfig)); // Enable HTTP activities.
    elsa.UseHttp(http => http
        .ConfigureHttpOptions = httpConfig => httpConfig
            .BaseUrl = new(apiBaseUrl)); // Set HTTP base URL for api endpoints.
    elsa.UseHttp(http => http
        .ConfigureHttpOptions = httpConfig => httpConfig
            .BasePath = new(apiBasePath)); // Set HTTP base path for api endpoints.
    elsa.UseWorkflowsApi(); // Expose Elsa API endpoints.    
    elsa.AddActivitiesFrom<Program>(); // Register custom activities from the application, if any.
    elsa.AddWorkflowsFrom<Program>(); // Register custom workflows from the application, if any.
    elsa.UseRealTimeWorkflows(); // Setup a SignalR hub for real-time updates from the server.

});

builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin() //TODO: For development purposes only. Use a specific origin for production.
        .WithExposedHeaders("*")) // Replace "*" with "x-elsa-workflow-instance-id" to only expose the workflow instance ID header.
    ); // Configure CORS to allow designer app hosted on a different origin to invoke the APIs.

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

} // Add Swagger/Swashbuckle if it is not design-time and it is development environment.

builder.Services.AddRazorPages(options => options
    .Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()) // Disable antiforgery token validation.
);
       

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer(); // Add API Explorer for Swagger/Swashbuckle

var app = builder.Build();

app.UseCors();
app.UseRouting(); // Required for SignalR.
// If it is not DesignTime, but it is development environment, use Swagger.
if (!isDesignTime && app.Environment.IsDevelopment()) 
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
if (app.Environment.IsDevelopment())
{
    //app.UseDeveloperExceptionPage();
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
//app.MapStaticAssets(); // TODO: add after migrating to .NET 10, also add the Microsoft.AspNetCore.StaticAssets nuget package if needed.
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi(); 
app.UseWorkflows();
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration for real-time updates. 
app.MapFallbackToFile("/_Host");
app.MapHealthChecks("/health"); // TODO: remove this line? (not part of original setup)
app.Run();