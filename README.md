# Streaming Studios (Jellyfin 12)

Scansiona film e serie, interroga TMDB (watch/providers) e aggiunge la piattaforma (Netflix, Prime Video, Disney+...) come **Studio**, senza toccare gli studi già presenti.

## Compilare
- Opzione A (niente da installare): carica questa cartella su un repository GitHub, vai in Actions -> "Build plugin" -> scarica l'artifact `StreamingStudios` (contiene lo zip).
- Opzione B: installa .NET 10 SDK + zip, poi `./build.sh` -> `dist/StreamingStudios_1.0.0.0.zip`.

## Installare
1. Ferma Jellyfin.
2. Nella cartella dati di Jellyfin crea `plugins/StreamingStudios_1.0.0.0` e copiaci `Jellyfin.Plugin.StreamingStudios.dll` e `meta.json` (contenuto dello zip).
3. Avvia Jellyfin.
4. Dashboard -> Plugin -> Streaming Studios: inserisci la chiave API TMDB (gratis) e il paese, salva.
5. Dashboard -> Attività pianificate -> "Scansiona piattaforme streaming (Studios)" -> avvia. Aggiungi un'esecuzione periodica se vuoi.

## Note
- I titoli devono avere l'ID TMDB (lo mettono gli scraper standard).
- Le piattaforme dipendono dal paese scelto.
- Un refresh completo dei metadati può sovrascrivere gli Studios: rilancia l'attività.
