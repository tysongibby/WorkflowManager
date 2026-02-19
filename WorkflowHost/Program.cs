using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
//using Elsa.Studio.Host.Options;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var defaultConnection = config.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection)) throw new InvalidOperationException("The connection string 'DefaultConnection' was not found or is empty.");

// Add Elsa Host services
builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(options => options.UseSqlServer(defaultConnection)));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(options => options.UseSqlServer(defaultConnection)));
    elsa.UseWorkflowsApi();
    elsa.UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = config["Identity:SigningKey"]!;
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });
    elsa.UseDefaultAuthentication();
    elsa.UseScheduling();
    elsa.UseCSharp();
    elsa.UseJavaScript();
    elsa.UseLiquid();
});

// Add Elsa Studio services
//builder.Services.AddElsaStudio(elsa =>
//{
//    elsa.HostOptions = new ElsaHostOptions
//    {
//        HeadlessMode = true
//    };
//});

builder.Services.AddRazorPages();

var app = builder.Build();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.MapFallbackToPage("/_Host");
app.Run();