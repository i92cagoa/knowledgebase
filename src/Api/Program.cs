using KnowledgeBase.Api.Endpoints;
using KnowledgeBase.Application;
using KnowledgeBase.Infrastructure;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.MapScalarApiReference();

app.MapHealthEndpoints();
app.MapWorkspaceEndpoints();
app.MapNoteEndpoints();
app.MapTagEndpoints();
app.MapTagMergeEndpoints();
app.MapAttachmentEndpoints();

app.Run();

public partial class Program;