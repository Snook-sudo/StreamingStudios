using Jellyfin.Data.Enums;
using Jellyfin.Plugin.StreamingStudios.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.StreamingStudios.ScheduledTasks;

public class ProviderScanTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly TmdbClient _tmdb;
    private readonly ILogger<ProviderScanTask> _logger;

    public ProviderScanTask(ILibraryManager libraryManager, TmdbClient tmdb, ILogger<ProviderScanTask> logger)
    {
        _libraryManager = libraryManager;
        _tmdb = tmdb;
        _logger = logger;
    }

    public string Name => "Scansiona piattaforme streaming (Studios)";

    public string Key => "StreamingStudiosScan";

    public string Description =>
        "Cerca su TMDB dove sono disponibili film e serie della libreria e aggiunge la piattaforma come Studio.";

    public string Category => "Streaming Studios";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var cfg = Plugin.Instance?.Configuration;
        if (cfg is null || string.IsNullOrWhiteSpace(cfg.TmdbApiKey))
        {
            throw new InvalidOperationException("Chiave API TMDB non impostata (Dashboard > Plugin > Streaming Studios).");
        }

        var kinds = new List<BaseItemKind>();
        if (cfg.ScanMovies) { kinds.Add(BaseItemKind.Movie); }
        if (cfg.ScanSeries) { kinds.Add(BaseItemKind.Series); }
        if (kinds.Count == 0) { return; }

        var categories = new List<string>();
        if (cfg.IncludeFlatrate) { categories.Add("flatrate"); }
        if (cfg.IncludeFree) { categories.Add("free"); }
        if (cfg.IncludeAds) { categories.Add("ads"); }
        if (cfg.IncludeRent) { categories.Add("rent"); }
        if (cfg.IncludeBuy) { categories.Add("buy"); }

        var excluded = new HashSet<string>(
            (cfg.ExcludedProviders ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);

        var country = string.IsNullOrWhiteSpace(cfg.Country) ? "IT" : cfg.Country.Trim().ToUpperInvariant();

        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = kinds.ToArray(),
            IsVirtualItem = false,
            Recursive = true
        });

        _logger.LogInformation("Streaming Studios: {Count} elementi da analizzare (paese {Country})", items.Count, country);

        var cache = new Dictionary<string, List<string>?>();
        int done = 0, updated = 0, noId = 0, noData = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!item.ProviderIds.TryGetValue("Tmdb", out var tmdbId) || string.IsNullOrWhiteSpace(tmdbId))
            {
                noId++;
            }
            else
            {
                var isMovie = item is Movie;
                var cacheKey = (isMovie ? "m:" : "t:") + tmdbId;

                if (!cache.TryGetValue(cacheKey, out var providers))
                {
                    providers = await _tmdb.GetProvidersAsync(isMovie, tmdbId, country, cfg.TmdbApiKey, categories, cancellationToken)
                        .ConfigureAwait(false);
                    cache[cacheKey] = providers;
                    await Task.Delay(40, cancellationToken).ConfigureAwait(false);
                }

                if (providers is null || providers.Count == 0)
                {
                    noData++;
                }
                else
                {
                    var current = item.Studios ?? Array.Empty<string>();
                    var toAdd = providers
                        .Where(p => !excluded.Contains(p) && !current.Contains(p, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (toAdd.Count > 0)
                    {
                        item.Studios = current.Concat(toAdd).ToArray();
                        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken)
                            .ConfigureAwait(false);
                        updated++;
                    }
                }
            }

            done++;
            progress.Report(done * 100.0 / items.Count);
        }

        _logger.LogInformation(
            "Streaming Studios: completato. Aggiornati {Updated}, senza ID TMDB {NoId}, senza piattaforme {NoData}.",
            updated, noId, noData);
        progress.Report(100);
    }
}
