# Boardgame Tracker – Offene technische Schulden

Diese Datei hält **konkrete bekannte Abweichungen oder Follow-ups** fest, die nicht
Teil der gewünschten Architektur oder Produktanforderung sind.

Sie ist kein allgemeiner Backlog und wird nicht bei normalen Aufgaben vorsorglich
gelesen. Neue Einträge nur aufnehmen, wenn ein Problem bewusst offen bleibt und
später gezielt bearbeitet werden soll. Erledigte Einträge entfernen statt eine
Historie aufzubauen; Historie gehört in Git/CHANGELOG, wenn relevant.

## Architektur

### Spielmodulgrenze in allgemeinen App-Features

**Status:** offen

`src/BoardgameTracker.App/Features/Games/ListGames/GamesPage.xaml.cs` kennt aktuell
`ArkNovaGame` direkt und besitzt einen konkreten `OnArkNovaTapped`-Pfad.

**Ziel:** Das allgemeine Games-Feature darf keine konkrete Spielabhängigkeit
besitzen. Spielkatalog/Navigation sollen über eine allgemeine Registrierung oder
ein kleines allgemeines Read Model erfolgen; die konkrete Ark-Nova-Implementierung
bleibt im Spielmodul.

### Spielabhängige Einstellungen im allgemeinen Settings-Feature

**Status:** offen

`src/BoardgameTracker.App/Features/Settings/AppSettings/AppSettingsPage.xaml.cs`
kennt aktuell `ArkNovaGame` und `ArkNovaSettingsPage` direkt und baut die
Default-Spielauswahl mit Ark Nova hart auf.

**Ziel:** Allgemeine App-Einstellungen dürfen keine konkreten Spieltypen oder
Spielseiten kennen. Registrierte Spiele und deren Settings-Einstieg sollen über
eine kleine allgemeine Modulgrenze bereitgestellt werden.

### Ark-Nova-Buildverdrahtung in der allgemeinen App-Projektdatei

**Status:** beobachten / bei weiterem Wachstum auslagern

`src/BoardgameTracker.App/BoardgameTracker.App.csproj` bindet Ark-Nova-native Assets,
Buildverzeichnisse und native Bibliotheken direkt ein.

**Ziel:** Die App-Projektdatei darf als Hostintegrationspunkt Spielassets paketieren.
Wenn die spielabhängige Buildkonfiguration weiter wächst, soll sie in eine
modul-eigene importierte Builddatei unter `Games/ArkNova/` ausgelagert werden,
damit die allgemeine Projektdatei übersichtlich bleibt.

### Globaler Architekturtest für Spielmodulgrenzen

**Status:** offen

Die vorhandenen Architekturtests schützen bereits einzelne Grenzen, insbesondere
allgemeine Gruppenverträge und allgemeine Group-UI vor konkreten Spieltypen. Ein
repositoryweiter Schutz aller allgemeinen App-/Application-Bereiche vor
`Games.<ConcreteGame>`-Abhängigkeiten fehlt jedoch noch.

**Ziel:** Einen kleinen Architekturtest ergänzen, der die allgemeine
`Games/<GameName>/`-Ownership projektweit schützt und dabei erlaubte
Integrationspunkte wie Composition Root, Host-Projektdatei und technisch notwendige
Partial-Type-Erweiterungen ausdrücklich berücksichtigt.
