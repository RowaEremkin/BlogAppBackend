using BlogAppBackend.Tools.Console;
using BlogAppBackend.Tools.Devices;
using BlogAppBackend.Tools.Sql;
using BlogAppBackend.Tools.Sql.Data;
using BlogAppBackend.Tools.Tokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

#region Services
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Добавляем схему безопасности
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Указываем, что операции требуют авторизации
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});
builder.Services.AddScoped<IDebugConsole, DebugConsole>();
Action<SqlAddressData> configureSqlAddressData = (data) => data = new SqlAddressData();
;
builder.Services.Configure<SqlAddressData>((SqlAddressData data) => data = new SqlAddressData());
builder.Services.Configure<SqlLoginData>((SqlLoginData data) => data = new SqlLoginData());
builder.Services.AddScoped<SqlAddressData>();
builder.Services.AddScoped<SqlLoginData>();
builder.Services.AddScoped<ISqlAccess, SqlAccess>();
builder.Services.AddScoped<IDeviceStorage, DeviceStorage>();
TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("2d5d7be491f50c3ea4c47ef01889850f256fb88f2ac4782b905a568aa13c8e29")),
    ValidateIssuer = false, // Установите true, если вы хотите проверять issuer
    ValidateAudience = false // Установите true, если вы хотите проверять audience
};
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddScoped<ITokenStorage, TokenStorage>();
builder.Services.AddWebEncoders(o =>
{
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic, UnicodeRanges.CyrillicExtendedA, UnicodeRanges.CyrillicExtendedB);
});
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var culture = new CultureInfo("ru-ru");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
CultureInfo.CurrentCulture = culture;
CultureInfo.CurrentUICulture = culture;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;


app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
