using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurioBackendFunction.Logic; // Adjust namespace if needed
using SecurioBackendFunction.Repositories; // Adjust namespace if needed
using System;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// --- DEPENDENCY INJECTION REGISTRATION ---
builder.Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// 1. Get the connection string from local.settings.json or Azure Environment
string sqlConn = Environment.GetEnvironmentVariable("SqlConnectionString");
if (string.IsNullOrEmpty(sqlConn))
    throw new InvalidOperationException("SqlConnectionString environment variable is not configured.");

// 2. Register the Repository as a Singleton (one instance shared by everyone)
builder.Services.AddSingleton<IUserRepository>(new UserRepository(sqlConn));

// 3. Register your Managers/Logic as Scoped (new instance created per request)
builder.Services.AddScoped<UserManager>();
builder.Services.AddScoped<AuthManager>();

builder.Build().Run();