# Boardgame Tracker v0.6.3.1 – Setup

## Upgrade

``` cmd
tools\upgrade-from-v063.cmd
```

Keine Dateien werden gelöscht oder verschoben. Keine EF-Core-Migration
erforderlich.

## Build-Test

1.  `Clean Solution`
2.  `Rebuild Solution`
3.  Prüfen, dass die früheren Fehler `AppResources.Retry`,
    `AppResources.TechnicalDetails` und `CreateGroupButton` nicht mehr
    auftreten.
4.  Android-App starten.
5.  `Neue Gruppe` schnell mehrfach antippen; der Screen darf nur einmal
    geöffnet werden.
6.  Startup-Fehlerbildschirm und Busy-Zustände bei Gelegenheit prüfen.

In der Paket-Erstellungsumgebung steht kein .NET SDK zur Verfügung;
daher wurde statisch statt mit `dotnet build` validiert.
