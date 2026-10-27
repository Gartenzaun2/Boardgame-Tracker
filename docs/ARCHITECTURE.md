# Boardgame Tracker – Architektur

## 1. Architekturentscheidung

Boardgame Tracker ist ein **modularer Monolith mit pragmatischen Vertical
Slices**.

Ziele:

- mobile .NET-MAUI-App für Android/iOS,
- verständlich und wartbar für einen einzelnen Entwickler,
- mehrere Brettspiele ohne generische Game Engine,
- fachliche Regeln unabhängig von MAUI, EF Core und SQLite,
- so wenig Abstraktion wie nötig,
- Feature-/Use-Case-orientierte Struktur.

## 2. Projekte und Abhängigkeiten

```text
App -> Application -> Core
          ^
          |
Infrastructure -> Application
Infrastructure -> Core
```

### Core

Enthält:

- Domain Entities
- Aggregate-Grenzen
- Value Types / Enums
- fachliche Invarianten
- reine Berechnungen
- reine Spielregeln

Core kennt insbesondere nicht:

- MAUI
- XAML
- CommunityToolkit
- EF Core
- SQLite
- Kamera
- Dateisystem

### Application

Referenziert nur `Core`.

Enthält:

- konkrete Use Cases,
- Orchestrierung,
- Commands,
- Read Models,
- technische Ports, wenn eine echte Grenze nötig ist.

Application enthält keine:

- XAML-Seiten,
- ViewModels,
- EF-Core-Typen,
- SQLite-Klassen,
- MAUI-Typen.

### Infrastructure

Referenziert `Application` und `Core`.

Enthält:

- EF Core
- SQLite
- DbContext
- Entity-Konfigurationen
- Datenbankinitialisierung
- Application-Port-Implementierungen
- später Kamera-/Datei-/Bildanalyse-Implementierungen

### App

Enthält:

- MAUI Application
- Shell
- XAML Pages
- ViewModels
- UI-Zustand
- Navigation
- Presentation
- Lokalisierung
- Composition Root

`App` darf `Infrastructure` nur im Composition Root/Startup direkt verwenden.
Feature-ViewModels kennen keine konkreten Infrastructure-Klassen.

## 3. Vertical-Slice-Regeln

Code wird primär nach fachlichem Feature und Use Case organisiert.

Beispiele:

```text
Groups/
  ListGroups/
  CreateGroup/
  EditGroup/

Games/
  ArkNova/
    Preparation/
    StartSession/
    Running/
    Scoring/
    Settings/
```

Nicht nach pauschalen technischen Sammelordnern wie:

```text
Services/
Repositories/
Helpers/
Managers/
Common/
Utils/
```

Ein Slice darf klein sein. Es gibt kein künstlich identisches Schema für alle
Use Cases.

## 4. Interfaces und Abstraktionen

Ein Interface wird eingeführt, wenn mindestens einer dieser Gründe vorliegt:

1. Application muss eine technische Abhängigkeit nutzen, ohne Infrastructure zu
   referenzieren.
2. Es existieren tatsächlich mehrere Implementierungen.
3. Eine externe Plattform-/Gerätegrenze wird abstrahiert.
4. Die Abstraktion hat eigene fachliche/technische Bedeutung.

Nicht allein für:

- jede Klasse,
- jedes ViewModel,
- Mockbarkeit,
- architektonische Symmetrie.

Nicht vorgesehen, solange kein echter Bedarf entsteht:

- Generic Repository
- Unit-of-Work-Wrapper um EF Core
- MediatR
- generisches CQRS-Framework
- Event Sourcing
- `INavigationService` nur aus Prinzip
- AutoMapper für triviale 1:1-Zuordnungen
- Microservices
- generische Game Engine

## 5. Aggregate und Zuständigkeiten

### Person

Eigenes Aggregate Root.

### Group

Aggregate Root für:

- feste Mitgliedschaft,
- Default-Reihenfolge,
- temporär/permanent,
- Archivierung.

Spielspezifische Erweiterungen wie Arche-Nova-Defaultfarben sind separat
modelliert.

### GameSession

Aggregate Root für:

- Spiel-/Gruppenreferenz,
- Teilnehmer-Snapshot,
- Reihenfolge,
- Status,
- Zeitpunkte,
- finale Platzierung.

Lifecycle:

```text
Running -> Scoring -> Completed
```

### Arche Nova

Arche-Nova-spezifisch bleiben:

- Farben
- Zoopläne
- Startlogik
- Wertungslookup
- besondere Karten
- Gleichstandsregeln
- spätere Wertungsmattenanalyse

## 6. Read Models

Read Models sind Application-Typen für einen konkreten Anwendungsfall.

Beispiele:

```text
GroupListItem
OpenGameSession
ArkNovaScoring...
```

Sie sind:

- keine Domain Entities,
- keine Infrastructure-Modelle,
- nicht automatisch ViewModels.

Infrastructure darf hochgradig optimierte EF-/SQLite-Projections verwenden,
liefert aber den von Application definierten Vertrag.

Vollständige Entity-Graphen werden nicht nur zum Anzeigen weniger UI-Felder
geladen.

## 7. MVVM-Grenze

### View

Verantwortlich für:

- Layout,
- Controls,
- Bindings,
- visuelle Zustände,
- rein visuelle Animationen,
- MAUI-spezifische APIs.

### ViewModel

Verantwortlich für:

- Screen State,
- Commands/UI-Aktionen,
- unmittelbare UI-Validierung,
- Aufruf von Application-Use-Cases.

Zustände wie `IsBusy` oder `IsNavigating` gehören bevorzugt in den
ViewModel-Zustand, nicht parallel zusätzlich ins Code-behind.

### Application/Core

Autoritative Fachregeln bleiben außerhalb der UI.

UI-Vorprüfungen sind für schnelles Feedback erlaubt, dürfen aber keine
fachliche Wahrheit ersetzen.

## 8. Navigation

Die konkrete Shell-Navigation bleibt App-Verantwortlichkeit.

Stabile Navigationsparameter werden zentral benannt:

```text
GroupId
SessionId
```

Empfangende Seiten validieren Pflichtparameter über `IQueryAttributable`.

Ein fehlender/falsch typisierter Pflichtparameter ist ein Programmierfehler und
wird nicht still ignoriert.

### Hauptbereiche

```text
Games
Groups
History
Settings
```

`Groups` ist ein Shell-Root, aber fachlich im normalen Bedienfluss immer an
einen gültigen Spielkontext gebunden.

### Lokaler Navigationskontext

`IAppNavigationSettingsStore` ist eine von Application definierte technische
Grenze.

Gespeichert in nativen MAUI Preferences:

- DefaultGameKey
- SelectedGameKey
- LastMainSection
- LastSessionId

Diese Werte sind kein historischer Domainzustand.

Spielauswahl, Standardspiel-Start und Wiederaufnahme einer Session müssen den
Spielkontext **vor** der Gruppen-Navigation setzen.

## 9. Offene Sessions

Fachliche Zielinvariante:

```text
höchstens eine offene Session pro (GameId, GroupId)
```

Offen:

```text
Running | Scoring
```

Mehrere offene Sessions mit unterschiedlichen Spielen und/oder Gruppen sind
zulässig.

Diese Regel gehört in den Start-Use-Case/Application-Fluss und darf durch
Datenbankconstraints zusätzlich geschützt werden.

Aktueller Implementierungsunterschied: `SqliteStartArkNovaSessionDataAccess`
prüft derzeit noch global auf irgendeine offene Session. Das ist zu korrigieren.

Die Wiederherstellung verwendet nicht „jüngste Session = globale Wahrheit“,
sondern den explizit zuletzt betrachteten Sessionkontext. Fehlt dieser, greifen
Hauptbereich/Spiel/Standardspiel.

## 10. Arche-Nova-Start

Die Vorbereitung bleibt bis `Starten` nicht persistent.

Autoritative Regeln:

- gültige Spielerzahl,
- eindeutige Personen,
- eindeutige finale Farben,
- vollständige Zooplanauswahl,
- sessionweit eindeutige angebotene Zoopläne.

Der Start-Use-Case prüft zusätzlich:

- Gruppe existiert,
- Gruppe nicht archiviert,
- Teilnehmer entsprechen der festen Gruppenbesetzung,
- Referenz-Zoopläne existieren.

Danach werden allgemeine Session und Arche-Nova-Snapshots gemeinsam
persistiert.

## 11. Wertung

Rohwerte dürfen während `Scoring` gespeichert werden.

`Completed` ist die fachliche Grenze für Historie/Statistik.

Berechnete Punkte bleiben reine Core-Berechnung und werden nicht gespeichert.

Platzierung wird gespeichert, weil manuelle Gleichstandsauflösung nicht allein
aus Punkten rekonstruiert werden kann.

Besondere Karten werden über den natürlichen Composite Key
`(GameSessionId, PersonId, SpecialCard)` gespeichert.

## 12. Persistenz

Aktuell:

- EF Core 10
- SQLite
- `IDbContextFactory<BoardgameTrackerDbContext>`
- Fluent Mapping mit `IEntityTypeConfiguration<T>`
- keine Lazy-Loading-Proxies
- Read-only bevorzugt `AsNoTracking()`

### Entwicklungsdatenbank

Runtime:

```text
Database.EnsureCreatedAsync()
```

Danach idempotentes Seeding für:

- `ark-nova`
- Zoopläne 1–14

Bei Schemaänderungen wird die lokale Pre-Alpha-Datenbank aktuell neu erzeugt.

Sobald Nutzerdaten über veröffentlichte Versionen erhalten werden müssen, wird
auf einen kanonischen EF-Migrationsverlauf umgestellt.

Vorhandene historische Migrationsdateien sind im aktuellen
`EnsureCreatedAsync()`-Runtimepfad nicht die autoritative Upgrade-Strategie.

### SQLite und DateTimeOffset

SQLite kann bestimmte `DateTimeOffset`-Sortierungen/Aggregationen im verwendeten
EF-Pfad nicht zuverlässig providerseitig abbilden.

Akzeptiertes Muster:

1. nur minimal benötigte Spalten projizieren,
2. materialisieren,
3. notwendiges `OrderBy`/`Max` im Speicher ausführen.

Dieser Workaround bleibt in Infrastructure.

## 13. Preferences

### Arche Nova

`IArkNovaSettingsStore`:

- Punkteanzeige
- Ruf erfassen

### Navigation

`IAppNavigationSettingsStore`:

- Standardspiel
- SelectedGame
- letzter Hauptbereich
- letzte Session

Beide Ports werden von App mit MAUI Preferences implementiert.

## 14. Fehler-, Busy- und Mehrfachtap-Konzept

Unerwartete technische Exceptions werden nicht still verworfen.

- App protokolliert,
- Benutzer erhält kontextuelle Meldung,
- DEBUG darf technische Details zeigen,
- Release zeigt keine rohen Stacktraces.

Asynchrone Benutzeraktionen dürfen nicht parallel doppelt gestartet werden.

Gilt insbesondere für:

- Navigation
- Speichern
- Laden
- Partie starten/beenden
- Wertung abschließen
- spätere Fotoanalyse

## 15. Lokalisierung und Presentation

Sichtbare Texte werden über App-Lokalisierungsressourcen bereitgestellt.

Domainwerte bleiben sprachneutral.

Beispiele:

- `ArkNovaPlayerColor` bleibt Domain-Enum,
- Farbname/Farbton liegen in gemeinsamer Presentation,
- Zooplanname/Sketch werden aus der stabilen Plannummer in Presentation
  abgeleitet,
- besondere Karten werden über Presentation lokalisiert.

UI-Metadaten werden nicht unnötig in Core/SQLite dupliziert.

## 16. Tests

### Core

Schnelle Unit Tests für:

- Gruppeninvarianten,
- Sessionstatus,
- Reihenfolge,
- Startspielerrotation,
- Arche-Nova-Setupregeln,
- Wertungslookup,
- Punkte,
- Competition Ranking.

### Application

Tests für Use Cases mit realer Orchestrierung.

### Infrastructure

EF-/SQLite-Verhalten wird bei Bedarf gegen echtes SQLite getestet, nicht über
gemockte LINQ-Provider.

## 17. Architekturregeln

1. Feature vor technischem Dateityp.
2. Core enthält fachliche Wahrheit.
3. Application definiert Use Cases und benötigte Verträge.
4. Infrastructure entscheidet technische Datenbeschaffung/-persistenz.
5. App entscheidet Darstellung und Bedienung.
6. Spielspezifisches bleibt im Spielmodul.
7. Abstraktionen nur bei realer Grenze/Wiederverwendung.
8. Konkrete Klasse vor bedeutungsloser Interface-Kaskade.
9. DB-Constraints ergänzen Domainregeln, ersetzen sie nicht.
10. Verständlichkeit und Änderbarkeit vor architektonischer Symmetrie.
