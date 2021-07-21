using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.MapGet("/noop", () => Task.CompletedTask);

app.MapGet("/wait/{p}", async (int p) => {
    await Task.Delay(p);
});

app.Run();
