using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using static CheckISPAdress.Options.ApplicationSettingsOptions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Add singleton service
builder.Services.AddSingleton<MySingletonService>();

// Add HttpClient and CheckISPAddressService
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICheckISPAddressService, CheckISPAddressService>();

// Configure interval using options
builder.Services.Configure<ApplicationSettingsOptions>(builder.Configuration.GetSection(AppsettingsSections.ApplicationSettings));

// Start the service
ServiceProvider? serviceProvider = builder.Services.BuildServiceProvider();

if (serviceProvider != null)
{
    ICheckISPAddressService? checkISPAddressService = serviceProvider?.GetService<ICheckISPAddressService>();

    CancellationToken cts = new CancellationToken();
    checkISPAddressService?.StartAsync(cts);
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
