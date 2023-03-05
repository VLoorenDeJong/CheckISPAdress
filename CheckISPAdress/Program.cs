using CheckISPAdress.Helpers;
using CheckISPAdress.Interfaces;
using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient and CheckISPAddressService
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IISPAdressCounterService, ISPAdressCounterService>();
builder.Services.AddSingleton<ICheckISPAddressService, CheckISPAddressService>();
builder.Services.AddSingleton<IMailService, MailService>();

// Configure interval using options
builder.Services.Configure<ApplicationSettingsOptions>(builder.Configuration.GetSection(AppsettingsSections.ApplicationSettings));

// Start the service
ServiceProvider? serviceProvider = builder.Services.BuildServiceProvider();

if (serviceProvider != null)
{
    ICheckISPAddressService? checkISPAddressService = serviceProvider?.GetService<ICheckISPAddressService>();

    CancellationToken token = new CancellationToken();
    checkISPAddressService?.CheckISPAddressAsync(token);
}

if (builder is not null)
{
    WebApplication app = builder.Build();


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
    app.Run();
}
