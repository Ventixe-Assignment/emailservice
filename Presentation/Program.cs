using Azure.Communication.Email;
using Presentation.Interfaces;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(x => new EmailClient(builder.Configuration["ACS:ConnectionString"]));
builder.Services.AddTransient<IEmailService, EmailService>();




var app = builder.Build();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseCors(x => { x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
