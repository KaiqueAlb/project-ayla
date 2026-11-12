using Server.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddHostedService<TcpServerService>();

var app = builder.Build();

app.Run();
