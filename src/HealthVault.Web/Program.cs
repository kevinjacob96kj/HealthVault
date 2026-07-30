using FluentValidation;
using HealthVault.Application.Common.Behaviors;
using HealthVault.Application.People;
using HealthVault.Persistence.Data;
using HealthVault.Web.Authentication;
using HealthVault.Web.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePersonCommand>();
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<CreatePersonCommand>();
    configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services
    .AddAuthentication(HealthVaultAuthDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, HealthVaultUserAuthenticationHandler>(
        HealthVaultAuthDefaults.Scheme,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HealthVaultCon")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowOrigin");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<ApiRequestLoggingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
