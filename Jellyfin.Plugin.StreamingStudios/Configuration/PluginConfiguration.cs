using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.StreamingStudios.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Chiave API v3 (32 caratteri) o Read Access Token v4 (inizia con "eyJ") di TMDB.</summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>Codice paese ISO 3166-1 (es. IT, US, GB).</summary>
    public string Country { get; set; } = "IT";

    public bool ScanMovies { get; set; } = true;
    public bool ScanSeries { get; set; } = true;

    public bool IncludeFlatrate { get; set; } = true;
    public bool IncludeFree { get; set; } = true;
    public bool IncludeAds { get; set; } = true;
    public bool IncludeRent { get; set; } = false;
    public bool IncludeBuy { get; set; } = false;

    /// <summary>Piattaforme da ignorare, separate da virgola.</summary>
    public string ExcludedProviders { get; set; } = string.Empty;
}
