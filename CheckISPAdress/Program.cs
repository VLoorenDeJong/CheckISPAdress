using CheckISPAdress.Interfaces;
using CheckISPAdress.Options;
using CheckISPAdress.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

builder.Services.AddSingleton<IApplicationService, ApplicationService>();
builder.Services.AddSingleton<ITimerService, TimerService>();
builder.Services.AddSingleton<IISPAdressCounterService, ISPAdressCounterService>();

builder.Services.AddTransient<ICheckISPAddressService, CheckISPAddressService>();
builder.Services.AddTransient<IMailService, MailService>();

// Configure interval using options
builder.Services.Configure<ApplicationSettingsOptions>(builder.Configuration.GetSection(AppsettingsSections.ApplicationSettings));

// Register TimerService as a hosted service
builder.Services.AddHostedService<ApplicationService>();

var app = builder.Build();

//Start the application
app.Services.GetService<IApplicationService>()!.StartAsync(default).GetAwaiter().GetResult();

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