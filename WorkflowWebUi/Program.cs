using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Options;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Elsa.Studio.Models;


// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register root components.
//builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Register shell services and modules.
var backendConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => builder.Configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler)
};
builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendConfig);
builder.Services.AddLoginModule();
builder.Services.UseElsaIdentity();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();


// Build the application.
var app = builder.Build();

// Apply client config.
//var js = app.Services.GetRequiredService<IJSRuntime>();
//var clientConfig = await js.InvokeAsync<JsonElement>("getClientConfig");
//var apiUrl = builder.Configuration["Backend:Url"] ?? throw new InvalidOperationException("No API URL configured.");
//app.Services.GetRequiredService<IOptions<BackendOptions>>().Value.Url = new(apiUrl);

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Run the application.
await app.RunAsync();

