//using WorkflowWebUi;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddCore();
builder.Services.AddShell();
//builder.Services.AddRemoteBackend( options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler));
builder.Services.AddHttpMessageHandler<AuthenticatingApiHttpMessageHandler>();
builder.Services.AddLoginModule();
builder.Services.AddWorkflowsDesigner();

await builder.Build().RunAsync();
