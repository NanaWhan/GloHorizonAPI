using GloHorizonApi.Actors;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Services;

/// <summary>
/// Provides notification services for actors with proper singleton lifetime
/// to avoid dependency injection scope issues in the actor system
/// </summary>
public class ActorNotificationServices
{
    public ISmsService SmsService { get; }
    public IEmailService EmailService { get; }
    public IConfiguration Configuration { get; }
    public ILogger<QuoteNotificationActor> Logger { get; }

    public ActorNotificationServices(
        ISmsService smsService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<QuoteNotificationActor> logger)
    {
        SmsService = smsService;
        EmailService = emailService;
        Configuration = configuration;
        Logger = logger;
    }
}