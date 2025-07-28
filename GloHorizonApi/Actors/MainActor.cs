using GloHorizonApi.Data;

namespace GloHorizonApi.Actors;

public class MainActor: BaseActor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainActor> _logger;
    private readonly ApplicationDbContext _db;

    public MainActor(
        IServiceProvider serviceProvider,
        ILogger<MainActor> logger,
        ApplicationDbContext db
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _db = db;
    }
}