using Akka.Actor;
using GloHorizonApi.Actors;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddActorSystem(this IServiceCollection services, string systemName)
    {
        services.AddSingleton<ActorSystem>(provider =>
        {
            // Create actor system
            var actorSystem = ActorSystem.Create(systemName);
            
            // Get required services for actors
            var smsService = provider.GetRequiredService<ISmsService>();
            var emailService = provider.GetRequiredService<IEmailService>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var logger = provider.GetRequiredService<ILogger<BookingNotificationActor>>();
            
            // Create and register the booking notification actor
            var bookingNotificationActor = actorSystem.ActorOf(
                BookingNotificationActor.Props(smsService, emailService, configuration, logger),
                "booking-notification-actor"
            );
            
            // Store the actor system in TopLevelActor for access
            TopLevelActor.ActorSystem = actorSystem;

            return actorSystem;
        });

        return services;
    }
}