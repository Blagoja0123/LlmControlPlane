using System.Text.Json.Serialization;
using Domain.Provisioning;
using Infrastructure.Kubernetes;
using Infrastructure.Kubernetes.Backends;
using k8s;
using Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<ProvisioningOptions>(builder.Configuration.GetSection(ProvisioningOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton(KubernetesClientFactory.Create());
builder.Services.AddSingleton<IBackendSpecFactory, VLlmBackendSpecFactory>();
builder.Services.AddSingleton<IBackendSpecFactory, OllamaBackendSpecFactory>();
builder.Services.AddSingleton<IBackendSpecFactory, TgiBackendSpecFactory>();
builder.Services.AddSingleton<IBackendSpecFactory, LlamaCppBackendSpecFactory>();
builder.Services.AddSingleton<BackendSpecFactoryResolver>();
builder.Services.AddSingleton<OllamaModelPuller>();
builder.Services.AddSingleton<IUserDeploymentProvisioner, KubernetesUserDeploymentProvisioner>();
builder.Services.AddScoped<DeploymentOrchestrationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();