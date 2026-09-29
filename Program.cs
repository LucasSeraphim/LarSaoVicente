using LarSaoVicente.Data;
using LarSaoVicente.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Conecta o projeto ao banco de dados usando a string de conexão do appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



// Aplica as migrations pendentes e cria os papéis e o usuário administrador
// na primeira vez que o sistema roda.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    string[] papeis = { "Administrador", "Farmacia", "Consulta" };
    foreach (var papel in papeis)
    {
        if (!await roleManager.RoleExistsAsync(papel))
            await roleManager.CreateAsync(new IdentityRole(papel));
    }

    string emailAdmin = "admin@larsaovicente.local";
    string? senhaAdmin = builder.Configuration["AdminSeed:Senha"];

    if (await userManager.FindByEmailAsync(emailAdmin) == null)
    {
        if (string.IsNullOrWhiteSpace(senhaAdmin))
        {
            logger.LogWarning("Administrador não criado: defina a senha em AdminSeed:Senha.");
        }
        else
        {
            var admin = new ApplicationUser
            {
                UserName = emailAdmin,
                Email = emailAdmin,
                NomeCompleto = "Administrador",
                EmailConfirmed = true
            };
            var resultado = await userManager.CreateAsync(admin, senhaAdmin);
            if (resultado.Succeeded)
                await userManager.AddToRoleAsync(admin, "Administrador");
        }
    }
}


app.Run();