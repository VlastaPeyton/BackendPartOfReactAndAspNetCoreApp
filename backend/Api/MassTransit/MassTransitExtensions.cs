using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Api.MessageBroker
{   
    // Ovo moram maknuti u BuildingBlocks (zajednicki folder za sve mikroservise)
    public static class MassTransitExtensions
    {
        public static IServiceCollection AddMassTransitRabbitMQAndOutboxInbox<TDbContext>(this IServiceCollection services, IConfiguration configuration, Assembly? consumerAssembly = null) where TDbContext: DbContext
        {
            /* <TDbContext> jer mikroservisi nemaju isto ime za AppDbContext, pa da odredi u runtime
              consumerAssembly != null za RabbitMQ Publisher microservice
               consumerAsembly = null za RabbitMQ Consumer microservice 
               U RabbitMQ Publisher microservice Program.cs mora builder.Services.AddMassTransitRabbitMQAndOutbox(builder.Configuration) 
               U RabbitMQ Consumer microservice Program.cs mora builder.Services.AddMassTransitRabbitMQAndOutboxInbox(builder.Configuration, Assembly.GetExecutingAssembly()) 
               Consumer/Publisher ako samo prima/salje, treba imati samo Inbox/Outbox, ali ako radi oba, pa na osnovu toga u OnModelCreatign biram da l cu napisati both Inobx-Outbox ili samo jedno 
             */
            services.AddMassTransit(x =>
            {   
                x.AddEntityFrameworkOutbox<TDbContext>(o =>
                {
                    o.QueryDelay = TimeSpan.FromSeconds(5); // Koliko cesto MassTransit built-in background worker proverava ima li noviteta u Outbox tabeli
                    o.UseSqlServer(); // Jer SQL Server koristim i za Consumer i za Publisher

                    // Disable Inbox cleanup za Publisher 
                    if (consumerAssembly is null)
                        o.DisableInboxCleanupService(); // Ne treba mi, jer u OnModelCreating Publishera necu staviti Inbox tabele
                    
                });
                /* Nakon ovoga, dopuni OnModelCreating, pa uradi Migraciju sada da bi se Inbox/Outbox tabele napravile u bazi
                  OnModelCreating za Publisher imace samo Outbox tabele jer gistro taj microservice ce slati samo u RabbitMQ, a nece da prima
                  OnModelCreating za Consumer imace samo Inbox tabele jer gistro taj microservice ce primati samo iz RabbitMQ,a nece slati 
                 */

                // Bez consumer za sada tj samo publisher imam u solution

                // RabbitMQ by default ima "ime_endpoint", ali lepse je "ime-endpoint" 
                x.SetKebabCaseEndpointNameFormatter();

                // Ispunjen uslov samo u Consumer microservice
                if (consumerAssembly is not null)
                    x.AddConsumers(consumerAssembly);

                x.UsingRabbitMq((context, cfg) =>
                {
                    var settings = context.GetRequiredService<MessageBrokerSettings>(); // U Program.cs bindovano appsettings.json to MessageBrokerSettings i upisan u DI

                    cfg.Host(new Uri(settings.Host), host =>
                    {
                        host.Username(settings.UserName); // umesto configuration["MessageBroker:UserName"]
                        host.Password(settings.Password); // umesto configuration["MessageBroker:Password"]
                    });

                    cfg.ConfigureEndpoints(context); // Pravi queueu za svakog consumera
                });
            });

            return services;
        }
    }
}
