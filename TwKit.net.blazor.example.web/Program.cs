using TwKit.net.blazor.example.web.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddRazorComponents()
       .AddInteractiveWebAssemblyComponents();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveWebAssemblyRenderMode()
   .AddAdditionalAssemblies(typeof(TwKit.net.blazor.example.web.Client._Imports).Assembly);

app.Run();
