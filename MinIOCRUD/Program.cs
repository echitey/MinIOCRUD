using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MinIOCRUD.Data;
using MinIOCRUD.Middleware;
using MinIOCRUD.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(opt =>
opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddHostedService<FileCleanupService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Exception handling middleware
app.UseMiddleware<CustomExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// Ensure database migrations are applied at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
