using CapiBeadsSV.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Alanis: Inyección del contexto para la base de datos
builder.Services.AddDbContext<capibeadsBDContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("capibeadsDbConnection")
    )
);

// Manejador de memoria
builder.Services.AddSession(options =>
{
    // Los segundos en que queremos que permanezca el estado 
    options.IdleTimeout = TimeSpan.FromSeconds(3600);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Indicamos que haremos uso de estos métodos con la siguiente función
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
