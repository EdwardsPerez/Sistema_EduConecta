using Microsoft.AspNetCore.Authentication.Cookies;
using EduConectaPeru.Data;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("SecretariaOnly", policy => policy.RequireRole("Secretaria"));
    options.AddPolicy("ApoderadoOnly", policy => policy.RequireRole("Apoderado"));
    options.AddPolicy("AdminOrSecretaria", policy => policy.RequireRole("Administrador", "Secretaria"));
});


builder.Services.AddScoped<UserRepositoryADO>();

builder.Services.AddScoped<StudentRepositoryADO>();
builder.Services.AddScoped<LegalGuardianRepositoryADO>();
builder.Services.AddScoped<PaymentRepositoryADO>();
builder.Services.AddScoped<QuotaRepositoryADO>();
builder.Services.AddScoped<MatriculaRepositoryADO>();

builder.Services.AddScoped<BankRepositoryADO>();
builder.Services.AddScoped<PaymentTypeRepositoryADO>();
builder.Services.AddScoped<PaymentStatusRepositoryADO>();
builder.Services.AddScoped<DocenteRepositoryADO>();
builder.Services.AddScoped<GradoSeccionRepositoryADO>();
builder.Services.AddScoped<HorarioRepositoryADO>();

builder.Services.AddScoped<CursoVacacionalRepositoryADO>();
builder.Services.AddScoped<InscripcionCursoVacacionalRepositoryADO>();
builder.Services.AddScoped<QuotaCursoVacacionalRepositoryADO>(); 
builder.Services.AddScoped<ConfiguracionCostoRepositoryADO>();

builder.Services.AddScoped<CarritoComprasRepositoryADO>();
builder.Services.AddScoped<TransaccionPagoRepositoryADO>();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication(); 
app.UseAuthorization();  

app.UseSession(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();