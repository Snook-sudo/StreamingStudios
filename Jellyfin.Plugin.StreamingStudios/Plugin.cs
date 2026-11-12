using Jellyfin.Plugin.StreamingStudios.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.StreamingStudios;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Streaming Studios";

    public override Guid Id => Guid.Parse("b7c1d3a2-5e4f-4a8b-9c6d-2f1e0a9b8c7d");

    public override string Description =>
        "Aggiunge la piattaforma streaming (Netflix, Prime Video, Disney+...) come Studio di film e serie TV, usando i dati TMDB.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "StreamingStudios",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
        };
    }
}
