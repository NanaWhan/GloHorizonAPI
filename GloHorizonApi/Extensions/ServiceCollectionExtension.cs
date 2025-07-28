using Akka.Actor;
using GloHorizonApi.Actors;

namespace GloHorizonApi.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddActorSystem(this IServiceCollection services, string systemName)
    {
        services.AddSingleton<ActorSystem>(provider =>
        {
            // Create simple actor system
            var actorSystem = ActorSystem.Create(systemName);
            
            // Store the actor system in TopLevelActor for access
            TopLevelActor.ActorSystem = actorSystem;

            return actorSystem;
        });

        return services;
    }
}