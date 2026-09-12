using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Helpers;

Host.CreateDefaultBuilder()
    .ConfigureServices(ConfigureServices)
    .Build();

static void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<NobaRentalDbContext>(options => options
        .UseSqlServer("Server=.;Database=NobaRental;Trusted_Connection=True;TrustServerCertificate=True;",
            x => x.WithDefaultOptions()));
}
