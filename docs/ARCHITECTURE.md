# Boardgame Tracker – Architektur

## 1. Zielbild

Boardgame Tracker ist ein **modularer Monolith mit pragmatischen Vertical Slices**.
Die Architektur soll mehrere Brettspiele unterstützen, ohne eine generische Game
Engine zu bauen. Domain-Regeln in Core bleiben unabhängig von MAUI, EF Core und
SQLite; technische Abstraktionen werden nur eingeführt, wenn sie einen konkreten
Nutzen haben.

Erstes vollständig unterstütztes Spiel ist **Arche Nova** (`ArkNova`).

Das Projekt dient zusätzlich als Lernprojekt. Nicht offensichtliche Trade-offs werden
nachvollziehbar dokumentiert. Lernorientierung rechtfertigt keine zusätzlichen
Abstraktionsschichten; Verständlichkeit und reale fachliche Grenzen bleiben maßgeblich.

## 2. Projektgrenzen

```text
App -> Application -> Core
Application -> EF Core
App -> EF Core SQLite
```

- **Core** enthält Domain-Entities, fachliche Regeln und spielespezifische
  Domain-Erweiterungen.
- **Application** enthält Vertical Slices, Use Cases, konkrete Queries, Read Models,
  DbContext, relationale EF-Konfigurationen, Initialisierung und Seeding. EF Core ist
  eine bewusste Application-Abhängigkeit; SQLite-spezifische APIs sind dort verboten.
- **App** enthält MAUI, XAML, ViewModels, Navigation, Präsentationslogik und den
  Composition Root sowie die SQLite-Providerkonfiguration.

SQLite wird einmal am Host-Rand ausgewählt: AddBoardgameTrackerSqlite registriert
IDbContextFactory, UseSqlite, EF-Warning-Regeln und SqliteNameCollationInterceptor.
ViewModels und Pages erhalten konkrete Use Cases/Queries, niemals einen DbContext.

Der nachvollziehbare Programmfluss lautet:

```text
ViewModel-Command -> konkreter UseCase/Query -> DbContext -> Domain -> SaveChangesAsync
```

Beispiele: CreateGroupViewModel -> CreateGroupUseCase, HistoryPage ->
CompletedGameSessionsQuery und ArkNovaScoringViewModel -> ArkNovaScoringUseCase.
Die fachliche Aktion und ihr Transaktionsende sind im jeweiligen Slice sichtbar.

## 3. Verbindliches Domänenvokabular

Für die vier zentralen Domänenkonzepte gelten ausschließlich diese Namen:

```text
Game
GameSession
Group
Player
```

Kombinationen verwenden dieselben Begriffe:

```text
GroupPlayer
GameSessionPlayer
ArkNovaGroupPlayer
ArkNovaGameSessionPlayer
```

Nicht als Synonyme verwenden:

```text
Person / People
Member als Ersatz für Player
Participant als Ersatz für Player
Session als Ersatz für GameSession
```

C#-Bezeichner verwenden konsequent `GameSession`, z. B. `GameSessionId`, nicht
`SessionId`.

## 4. Organisation nach allgemeinem Code und Spielmodulen

In **jedem Produktionsprojekt** ist `Games/<GameName>/` der Einstieg für
spielespezifischen Code. Allgemeiner Code liegt außerhalb eines konkreten
Spieleordners.

Beispiel:

```text
BoardgameTracker.Core/
  GameSessions/
    GameSession.cs
    GameSessionPlayer.cs
  Groups/
    Group.cs
    GroupPlayer.cs
  Players/
    Player.cs
  Games/
    Game.cs
    ArkNova/
      GameSessions/
      Groups/
      Players/

BoardgameTracker.Application/
  GameSessions/
    OpenGameSessionQuery.cs
    RunningGameSessionUseCase.cs
  Groups/
    CreateGroupUseCase.cs
    EditGroupUseCase.cs
    ListGroupsQuery.cs
    Colors/
  Players/
    EditPlayerUseCase.cs
    ListPlayersQuery.cs
  Games/
    ArkNova/
      GameSessions/
        StartArkNovaGameSessionUseCase.cs
        ArkNovaRunningGameSessionQuery.cs
        Preparation/
        Scoring/
        History/
        Results/
      SpecialCards/
      Settings/
      Persistence/
        BoardgameTrackerDbContext.ArkNova.cs
        Configurations/
        Seeding/
  Persistence/
    BoardgameTrackerDbContext.cs
    Configurations/         # allgemeines relationales EF-Modell
    DatabaseInitializer.cs
    IDatabaseInitializationStep.cs

BoardgameTracker.App/
  Persistence/SQLite/       # Providerregistrierung und Connection-Interceptor
  Features/                 # allgemeine UI-Features
    Games/
    Groups/
    History/
    Settings/
  Games/
    ArkNova/
      Features/
        GameSessions/
        SpecialCards/
        Settings/
```

Ein konkretes Spiel darf innerhalb `Games/<GameName>/` die allgemeine Struktur
spiegeln, wenn es dort tatsächlich eigene Logik oder Daten besitzt. Es werden
keine leeren Spiegelordner nur aus Symmetriegründen angelegt.

### Ownership-Regel

Spielespezifischer Code gehört vollständig dem jeweiligen Spielmodul. Ein
`TerraformingMars`-Feature darf beispielsweise nicht von `ArkNova`-Typen
abhängen.

Allgemeine Konzepte wie `GameSession`, `Group` und `Player` bleiben dagegen im
gemeinsamen Kern und werden von den Spielemodulen verwendet bzw. erweitert.

## 5. Vertical Slices

Application- und UI-Code wird primär nach **Use Case/Feature** organisiert und
nicht nach technischen Klassentypen.

Slice-Grenzen sind fachliche Grenzen und nicht automatisch Verzeichnisgrenzen.
Kleine Slices liegen flach im übergeordneten fachlichen Bereich; ihre kleinen
Commands, Inputs, Read Models und Results stehen standardmäßig in der Hauptdatei:

```text
Application/
  Groups/
    CreateGroupUseCase.cs    # auch Command, PlayerInput und SelectablePlayer
    EditGroupUseCase.cs      # auch Command, Details und Player-Projektion
    ListGroupsQuery.cs       # auch GroupListItem
    Colors/                 # gemeinsam verwendete Farbregeln und Modulvertrag
```

Eine eigene Datei bleibt sinnvoll für sliceübergreifend verwendete Typen,
eigenständige Fachbegriffe, echte Contracts/Extension Points, relevante eigene
Logik oder einen unabhängigen Änderungsgrund. Eine Zusammenführung darf die
Hauptdatei nicht deutlich unübersichtlicher machen. So bleiben etwa gemeinsame
Ergebnisdaten von Scoring und Korrektur, GroupColorDefaults sowie die echten
Persistenz-Erweiterungspunkte separat.

Ordner dienen der Navigation und Verständlichkeit. Ein eigener Slice-Ordner
gruppiert mehrere eigenständige Komponenten; ein kleiner Use Case braucht ihn
nicht automatisch. Verschachtelung folgt der tatsächlichen Komplexität des Features,
nicht Symmetrie oder einer festen Mindestzahl von Dateien.

Für umfangreichere Features sind Unterordner weiterhin sinnvoll, zum Beispiel
in der App mit XAML, Pages und ViewModels:

```text
App/Games/ArkNova/Features/GameSessions/
  Preparation/
  Running/
  Scoring/
  Result/
```

Nicht als Standardstruktur:

```text
Commands/
Queries/
Services/
Validators/
DTOs/
```

Ein Slice darf nur die Klassen enthalten, die er wirklich benötigt. Es gibt
keine Pflicht, jeden Slice mit identischer Klassenanzahl oder identischen
Abstraktionsschichten aufzubauen.

## 6. Composition statt persistierter Spiel-Entity-Vererbung

Spielespezifische Daten, die eine allgemeine Entity **ergänzen**, werden über
Composition modelliert.

### GameSessionPlayer

```text
GameSessionPlayer
- GameSessionId
- PlayerId
- Order
- Placement

        1
        |
        0..1
        |
ArkNovaGameSessionPlayer
- GameSessionId
- PlayerId
- Ark-Nova-spezifische Felder
```

`ArkNovaGameSessionPlayer` dupliziert keine allgemeinen
`GameSessionPlayer`-Felder. Sein Schlüssel `(GameSessionId, PlayerId)` verweist
1:1 auf den allgemeinen `GameSessionPlayer`.

Dasselbe Prinzip gilt für Gruppen:

```text
GroupPlayer
        1
        |
        0..1
        |
ArkNovaGroupPlayer
```

Damit bleiben allgemeines Schema und einzelne Spielmodule getrennt. Neue
Terraforming-Mars-Felder verändern nicht die Ark-Nova-Tabellen und umgekehrt.

### Vererbung bleibt erlaubt

Vererbung ist nicht grundsätzlich verboten. Sie wird verwendet, wenn fachlich
wirklich eine stabile **is-a**-Beziehung besteht und polymorphes Verhalten einen
Nutzen bringt. Insbesondere Strategien/Regeln dürfen gemeinsame Interfaces oder
abstrakte Basistypen verwenden.

EF-Core-Entity-Vererbung wird jedoch nicht allein eingesetzt, um Properties zu
sparen.

## 7. Entities und Value Objects

Entities besitzen eine fachliche Identität, z. B.:

- `Player` über `PlayerId`,
- `Group` über `GroupId`,
- `GameSession` über `GameSessionId`,
- `GroupPlayer` über `(GroupId, PlayerId)`,
- `GameSessionPlayer` über `(GameSessionId, PlayerId)`.

Value Objects werden nur eingeführt, wenn ein klarer fachlicher Werteblock ohne
eigene Identität entsteht und das Modell dadurch verständlicher wird. EF Core
10 `Complex Types` sind dafür eine mögliche Persistenzabbildung, aber kein
Selbstzweck. Aktuelle Score-Felder bleiben dort, wo sie fachlich bereits klar
modelliert sind.

## 8. EF Core und Persistenz

- Application verwendet EF Core 10 und das relationale Persistenzmodell direkt.
- App und Integrationstests wählen SQLite als Provider; Core kennt keine Persistenztechnologie.
- Keine Repositories, Stores oder DbContext-Interfaces nur zum Verstecken von EF Core.
- Interfaces bleiben für echte Polymorphie, Host-/Plattformgrenzen und Spielmodul-Erweiterungspunkte.
- Ein Slice erzeugt über IDbContextFactory einen kurzlebigen Context pro Operation;
  kein Singleton hält einen erzeugten Context und kein Context wird parallel verwendet.
- Konkrete Queries verwenden AsNoTracking und gezielte Read-Model-Projektionen.
- Schreibaktionen verwenden Domainmethoden und SaveChangesAsync am fachlichen Ende.
  Explizite Transaktionen schützen Aktionen mit mehreren direkten relationalen Writes,
  etwa Reihenfolgetausch, Farbdefaults, Korrektur und History-Import.
- Allgemeine Konfigurationen liegen in Application/Persistence/Configurations;
  Spielkonfigurationen in Application/Games/<GameName>/Persistence/Configurations.

Es gibt genau einen gemeinsamen `sealed partial BoardgameTrackerDbContext`.
Der generische Teil unter `Application/Persistence` enthält die allgemeinen DbSets
und `OnModelCreating`; die Arche-Nova-DbSets stehen in
`Games/ArkNova/Persistence/BoardgameTrackerDbContext.ArkNova.cs` im selben
Namespace wie der gemeinsame Context. Die allgemeinen und spielspezifischen Modellkonfigurationen bleiben getrennt.

`DatabaseInitializer` erstellt den Context, führt `EnsureCreatedAsync` aus und
führt anschließend die registrierten `IDatabaseInitializationStep`-Implementierungen
in Registrierungsreihenfolge auf demselben Context aus. `MauiProgram` ruft explizit `AddArkNova()` auf. Diese Extension unter
`App/Games/ArkNova` registriert die Ark-Nova-Application- und UI-Komponenten
einschließlich `ArkNovaReferenceDataSeeder` als spielspezifischem Schritt. Allgemeine
Dienste bleiben in `MauiProgram`. Die Registrierungen bleiben explizit, ihre Lifetimes
und die Reihenfolge innerhalb mehrfach registrierter Verträge bleiben erhalten. Der generische
Initialisierer kennt keine Spielmodule; Fehler und Abbruch werden weitergegeben.

Die App erhält `IEnumerable<IGameSessionResumeHandler>` aus `App/Navigation` per DI
und wählt für eine gemerkte offene Partie den ersten Handler anhand des GameKey.
Ohne passenden Handler wird LastGameSessionId gelöscht und die bestehende
Hauptnavigation verwendet. `ArkNovaGameSessionNavigationHandler` unter
`App/Games/ArkNova/Navigation` übernimmt Spiel-/Gruppenkontext und Navigation
zu Running oder Scoring. `ArkNovaRoutes.Register()` registriert dort explizit
die bisherigen Spielrouten und wird von `AddArkNova()` aufgerufen; App und Shell
kennen keine konkreten Arche-Nova-Seiten. Navigation Memory bleibt unverändert.

### Aktuelle Pre-Alpha-Datenbankstrategie

Der Runtime-Start verwendet `Database.EnsureCreatedAsync()` und danach idempotentes
Referenzdaten-Seeding. Neue Datenbanken erhalten direkt das aktuelle Modell.
Die Startkarten besitzen deterministische IDs; Seeding ergänzt fehlende Karten,
überschreibt aber keine Benutzeränderungen oder Archivzustände.

Bis zum ersten Deployment gibt es keine Migrationen, Schema-Upgrades oder
Kompatibilitätspfade für ältere Entwicklungsstände. Auch die Startnavigation
verwendet ausschließlich den explizit gespeicherten Kontext gemäß PRODUCT_SPEC;
sie sucht keinen Ersatzkontext aus alten Entwicklungsdaten. Hinweise zu nötiger
Neuinstallation und Datenverlust stehen in `SETUP.md`.

### Spezialkarten-Slice

`Core/Games/ArkNova/ArkNovaSpecialCard` schützt Identität, geordnete Symbole
und Archivstatus. Der feste Symbolkatalog ist ein Enum, die Kartenidentität eine GUID.
`Application/Games/ArkNova/SpecialCards` enthält Verwaltung und Leseprojektion mit direktem EF-Zugriff.
Verwaltungsdaten gehören nicht in `IArkNovaSettingsStore` oder Preferences.
Die UI unter `App/Games/ArkNova/Features/SpecialCards` verwendet einen gemeinsamen
Editor für Create/Edit und dieselbe ikonbasierte Kartenansicht wie die Wertung.
Die Symbol-Präsentation ordnet ausschließlich feste Symboltypen auf Bildressourcen
und lokalisierte Accessibility-Namen ab. Kartennamen kommen aus der unten
beschriebenen Game-Content-Auflösung.

Scoring arbeitet weiterhin ausschließlich mit Scoring-Sessions; die Korrektur
bleibt ein eigener Completed-Pfad. Katalogdaten werden aktuell gelesen und
Entwürfe beim Wiederöffnen aktualisiert. Neue Assignments setzen eine aktive
Karte voraus; archivierte bestehende Assignments bleiben erhalten oder werden
entfernt. Application und transaktionaler Persistenzpfad prüfen dies beim Speichern.

### App-Lokalisierung und Game Content

Es gibt zwei fachliche Lokalisierungsbereiche:

~~~text
App Localization → AppResources/.resx (aktuelle App-Sprache)
Game Content → spielmodulspezifisches content.json + persistente Nutzerressourcen
~~~

Application/Games/ArkNova/Content/content.json ist die einzige mitgelieferte
Content-Datei des Moduls und als EmbeddedResource paketiert. Sie deklariert
supportedLanguages/defaultLanguage sowie Namen unter specialCards (GUID) und
zooPlans (Number). Derzeit nur de; fehlende verifizierte Übersetzungen werden nicht
ersetzt oder erfunden. Bestehende sprachneutrale Skizzen bleiben App-Bildressourcen.

ArkNovaContentCatalog liest die Datei einmal bei Modulregistrierung; ArkNovaContent
führt Nutzerquelle und Built-in-Quelle zusammen. Die einzige Auflösungsregel lautet:
Nutzer → ausgewählte Materialsprache → unterstützte App-Sprache → Default →
stabiler [missing:ark-nova/.../name]-Marker mit Debug-Diagnose. Kein First-Fallback.
Tests prüfen sämtliche erwarteten Defaultressourcen und bekannte IDs/Nummern.
JSON und SQLite sind Speicherquellen desselben Game Content, keine eigenständigen
Lokalisierungsarchitekturen. Built-in-Content wird nicht in SQLite gespiegelt.

GameContentLanguagePreference verwendet den vorhandenen Hostgrenzentyp eines
Preferences-Ports: IGameContentLanguageStore, implementiert durch
App/Features/Settings/MauiGameContentLanguageStore. Der native Schlüssel lautet
Game.{Game.Key}.ContentLanguage. Beim ersten Lesen wird eine unterstützte App-Sprache,
sonst DefaultContentLanguage initialisiert und gespeichert. Kein CurrentUICulture-Wechsel.
Das Modul registriert seine Sprachdeklaration über den Content-Katalog; die gemeinsame
Preference kennt keine Ark-Nova-Details und ist unabhängig von ArkNovaSettings.

ArkNovaUserContent ist eine kleine modulinterne relationale Namensabbildung mit
(ResourceType, ObjectId) als Schlüssel. Vorhandene Domainidentitäten werden direkt
verwendet; keine redundanten Content-Keys oder Content-Entity pro Objektart. Lesen
und Schreiben bleiben bei konkreten EF-Slices. Kartenverwaltung, Ergebnisse und
laufende Partie verwenden dieselbe Auflösung; ViewModels erhalten Read Models.
Symbole sind Domainzustand, ihre sprachlichen Accessibility-Labels bleiben .resx.

Es gibt keine generische Game-Content-Engine, Repository-Hierarchie oder Übersetzungs-
Plattform. Erst reale Anforderungen weiterer Spiele könnten gemeinsame Extraktion
begründen. Historische Assignments bleiben GUID-Referenzen auf aktuelle Kartendaten;
Namen und Materialsprachen werden nicht historisiert oder an Partien gespeichert.

## 9. Domänenaggregate

### Player

`Player` ist ein eigenständiges Aggregate Root. Gast und regulärer Spieler sind
derselbe Typ; `IsGuest` beschreibt den Zustand.

### Group

`Group` ist ein Aggregate Root und besitzt seine `GroupPlayer`-Einträge. Die
Spielerzusammensetzung einer Gruppe ist fest; Reihenfolge, Name,
Temporär-/Archivstatus dürfen innerhalb der definierten Use Cases geändert
werden.

### GameSession

`GameSession` ist ein Aggregate Root und besitzt die allgemeinen
`GameSessionPlayer`-Snapshots. Statusübergänge werden durch die Domain geschützt.

Spielmodule ergänzen diese allgemeinen Snapshots über Composition.

## 10. Navigation und Einstellungen

Die Hauptnavigation lautet:

```text
Spiele · Gruppen · Historie · Einstellungen
```

Gruppen werden im Kontext eines ausgewählten Spiels angezeigt. Nicht-historische
Navigationseinstellungen und lokale Ark-Nova-Präferenzen werden über MAUI
`Preferences` gespeichert, nicht in SQLite.

### Navigationsparameter

Alle Schlüssel für Shell-Navigationsparameter werden zentral als Konstanten in `src/BoardgameTracker.App/Navigation/NavigationParameters.cs` definiert, bevorzugt mit `nameof`. Sender (`ShellNavigationQueryParameters`) und Empfänger (`IQueryAttributable.ApplyQueryAttributes`, insbesondere `TryGetValue`) verwenden dieselbe Konstante, niemals separat ausgeschriebene String-Literale. Neue Parameter werden zuerst dort ergänzt. So können Schlüssel nicht unbemerkt nur auf einer Seite geändert werden.

### Offline-Lizenzanzeige

`App/Features/Settings/Licenses` trennt Katalog/Modell, `LicenseTextService`, den
MAUI-Package-Adapter und ViewModels/Pages. Lesen über
`FileSystem.OpenAppPackageFileAsync`, kein Runtime-Netzwerk und keine Datenbank.
Das zentrale `Resources/Raw/ThirdPartyLicenses/catalog.json` fasst Bibliotheksfamilien
zusammen und referenziert deduplizierte Texte. Die acht nativen Originaldateien
unter `Games/ArkNova/PhotoRecognition/Native/licenses` bleiben unverändert.
Source-generierte JSON-Metadaten unterstützen Trimming/AOT. Shell nutzt
`NavigationParameters.LicenseComponentId`; compiled bindings und bestehende
Styles bleiben maßgeblich. Fehlende Texte werden geloggt und als lokalisierter,
wiederholbarer Fehler behandelt. Lizenzpflege gemäß
[Third-Party-Policy](THIRD_PARTY_LICENSE_POLICY.md), kein automatischer Audit.

### Zentrales Theme-System

App-Themes sind Präsentation und lokale Preferences, keine Domain-/SQLite-Daten. Der `AppNavigationSettings`-/Preferences-Port speichert die Theme-Auswahl; fehlende oder ungültige Werte fallen auf Deep Forest zurück. `App/Theming/ThemePalette` definiert reine semantische Farbwerte, `ThemeManager` ersetzt die MAUI-Ressourcen beim Start und Wechsel. XAML-Farben verwenden `DynamicResource`, keine Theme-Namen in Screens. Der globale `ContentPage`-Style setzt `ApplyToDerivedTypes="True"`, damit die tatsächlichen Page-Unterklassen den dynamischen AppBackground erben; keine separaten Page-Farbpaletten. Kleine plattformspezifische Shell-Appearance-Tracker ergänzen die vorhandene native TabBar um Kontur und Auswahlfläche; keine eigene Navigation. Fachliche Farben und Grafiken bleiben in ihren Spielmodulen.

## 11. Nicht gewünschte Muster

Ohne konkreten Bedarf werden nicht eingeführt:

- generische Repositories,
- MediatR nur als Dispatch-Schicht,
- `Common`-/`Utils`-Sammelordner,
- bedeutungslose Ein-Methoden-Interfaces,
- ein allgemeiner Navigation-Service nur zum Wrappen von Shell,
- eine generische Game Engine,
- technische Ordnerhierarchien, die zusammengehörige Features auseinanderziehen.

## 12. Namespaces, Dateien und Tests

Namespace-Hierarchien folgen der fachlichen Ordnerstruktur; Tests spiegeln den
getesteten Slice. Gemeinsame Fachbegriffe und Architekturverträge bleiben ihrer
Schicht zugeordnet. Core-Tests bleiben reine Unit-Tests ohne EF/SQLite.
Application-Slice-Tests verwenden echte EF-/SQLite-Datenbanken mit isolierter Lebensdauer
und Cleanup; keine Fakes für entfernte Store-/Query-Interfaces und kein EF InMemory als
Ersatz für Constraints, Übersetzung oder Transaktionen. Architekturtests schützen Core,
Providergrenze, EF-freie ViewModels sowie Gruppen- und Spielmodul-Ownership.
Kleine ausschließlich slice-eigene Begleittypen stehen standardmäßig beim Haupttyp;
Tests spiegeln die fachliche Gruppierung ohne unnötige Ein-Datei-Unterordner.
XAML, DI und EF zählen als Verwendung. Die konkreten C#-Stilregeln zu Typdeklarationen,
`var`, Klammern und mehrzeiligen Blöcken stehen ausschließlich in `AGENTS.md`.
Bei Refactorings werden alte Domänensynonyme nicht als Compatibility-Aliase
weitergeführt.

## 13. Architekturregel für zukünftige Spiele

Ein neues Spiel wie Terraforming Mars wird zunächst als eigenes Modul angelegt:

```text
Games/TerraformingMars/
```

Nur wirklich allgemeine Konzepte werden in den gemeinsamen Kern verschoben.
Gemeinsamer Code entsteht durch nachgewiesene Gemeinsamkeit, nicht durch
vorsorgliche Abstraktion.

## Historien-Slice

Die allgemeine Spielerverwaltung liegt in `Application/Players/ListPlayersQuery.cs`
und `Application/Players/EditPlayerUseCase.cs` sowie den zugehörigen App-Features und
konkreten EF-Slices. Die Listenprojektion lädt Spieler und Partiemetadaten gebündelt;
das Bearbeiten speichert ausschließlich Name und Archivstatus des Player-Aggregats.
`App/Navigation/IGameSessionNavigationHandler` ist die gemeinsame Navigation von Historie
und Spielerverwaltung zu einer Partie. Jedes Spiel registriert seinen Handler
im Modul; dieser entscheidet anhand des Session-Status über die konkrete Seite.
Das allgemeine Player-Feature enthält keine Spielmodul-Abhängigkeiten.

Konkrete CompletedGameSessionsQuery unter `Application/History`, Listenoberfläche unter `App/Features/History`. Arche-Nova-Korrekturen gehören unter `Games/ArkNova/GameSessions/History`: eigener Use Case mit direktem EF-Zugriff für Completed, UI verwendet die vorhandenen Wertungs- und Gleichstandskomponenten. Ein ansichtsgebundener Entwurf wird atomar gespeichert; der Scoring-Use-Case behält seine Statusgrenze.

## Gemeinsame Spielerfarben und Spielregeln

`Core/Players/PlayerColor` definiert zwölf gemeinsame semantische Farben ohne UI-Hexwerte. Es gibt kein separates Arche-Nova-Farbenum. `GamePlayerColorRules` beschreibt eine erlaubte Teilmenge; `Games/ArkNova/ArkNovaPlayerColors` erlaubt Blau, Gelb, Rot und Schwarz. Gruppen- und Partiedomänenobjekte des Spielmoduls prüfen diese Regeln. `Application/Groups/Colors/GamePlayerColorCatalog` verwendet die im Composition Root registrierten Regeln. Eine leere oder fehlende Teilmenge bedeutet: keine Spielerfarben.

`App/Presentation/PlayerColors` ordnet dem gemeinsamen Farbwert Hintergrund, Textfarbe und lokalisierten Namen zu. Diese Palette ist themeunabhängig und kein Kamera-Erkennungskatalog. Physische Farbe → gemeinsame semantische Farbe → UI-Farbpaar; Hexwerte sind keine Erkennungsgrenzen.

## Gruppenbearbeitung und Modulgrenze

`Application/Groups/CreateGroupUseCase.cs` und `EditGroupUseCase.cs` enthalten jeweils den Use Case und seine kleinen Begleittypen für gemeinsame Gruppendaten und optionale semantische Farbauswahlen für den aktuellen Spielschlüssel. Sie kennen keine konkreten Spieltypen oder Listen erlaubter Farben. Der registrierte Regelkatalog validiert die Auswahl; Gruppenfarben bleiben pro Spiel und Gruppenmitglied, keine globale Eigenschaft von `Player` oder `GroupPlayer`.

Die allgemeinen Gruppen-Use-Cases speichern Gruppe, neue Spieler, Reihenfolge und Farben in einer Transaktion. `Application/Groups/Colors/IGroupColorPersistence` ist die kleine technische Erweiterungsgrenze für Lesen und Ersetzen der Spieldefaults auf demselben DbContext. Der registrierte `Games/ArkNova/.../ArkNovaGroupColorPersistence` besitzt die Arche-Nova-Tabelle. Allgemeine Use Cases greifen nicht direkt auf diese Tabelle zu; ohne Farbmodul werden keine fremden Defaults gelesen oder verändert.

Beide Gruppenformulare bleiben unter `App/Features/Groups`. `GroupColorSelectionViewModel` und `GroupColorPickerView` werden für Anlage und Bearbeitung gemeinsam verwendet. Der aktuelle Spielkontext entscheidet über die Palette. Der Composition Root registriert zusätzlich den lokalisierten Anzeigenamen des Spiels für den Dialogtitel. Ein weiteres Spiel ergänzt seine Regeln und bei Farbunterstützung seinen Persistenzadapter; gemeinsame Formulare und Verträge erhalten keine Spiel-Fallunterscheidungen. Der Architekturtest schützt die öffentlichen Gruppenverträge vor spielspezifischen Typen.

## UI-Bindings und Fehlerdiagnose

Die App aktiviert `MauiStrictXamlCompilation` und `MauiEnableXamlCBindingWithSourceCompilation`; XC0022 bis XC0025 sind Buildfehler. Bei geändertem BindingContext wird `x:DataType` angegeben. Command-Bindings über `x:Reference` verwenden eine typisierte ViewModel-Property der Page statt des untypisierten BindingContext. Keine Unterdrückung durch `x:Object`.

Abgefangene technische Fehler in Historie und Wertungsansichten werden über injiziertes `ILogger<T>` mit Exception und relevantem ID-Kontext protokolliert; sichtbare Meldungen bleiben lokalisiert. Im Debug-Build gibt der Microsoft-Debug-Provider die Meldungen bei angeschlossenem Debugger aus. Es werden keine dauerhaften Protokolldateien oder Telemetriedienste eingeführt.

## Lokaler Austausch historischer Partien

Unter `Application/History/Transfer` liegen das unabhängige BGT-Austauschmodell,
Formatprüfung, ImportPlan und die Export-/Import-Use-Cases. JSON wird ausschließlich
in DTOs deserialisiert. Der Plan enthält globale 1:1-Spielerzuordnungen und bleibt
bis zum Commit ohne produktive Writes. Die App führt Dateiauswahl, Resolve und
Preview im History-Feature aus; sie kennt keine konkreten Persistenzadapter.

Die konkrete HistoryTransferPersistence im History/Transfer-Slice führt Exportlesevorgänge und den abschließenden Import jeweils
auf einem kurzlebigen Context in einer Transaktion aus. Vor dem Import wird der
lokale Snapshot einschließlich Modulreferenzen erneut geprüft; Änderungen erfordern
neue Analyse. Die persistierten Entities entstehen über Domainkonstruktoren und
Domainmethoden. SQLite-Namensindizes verwenden die pro Verbindung registrierte
BGT_NAME-Collation mit der gemeinsamen EntityName-Semantik (Trim + OrdinalIgnoreCase).
Der App-seitige Connection-Interceptor registriert sie beim Erzeugen/Öffnen jeder
Verbindung, auch für EnsureCreatedAsync. Das relationale Modell benennt die Collation,
kennt aber keine SQLite-Typen. Tests verwenden denselben Registrierungscode und
registrieren bei selbst übergebenen bereits offenen Verbindungen ausdrücklich vorab.

Bekannte Spielmodule registrieren IGameTransferSchema (Version und Validierung)
und IGameTransferPersistence (Projektion, Referenzmatching und Persistenz auf demselben
Context). Arche Nova besitzt seine DTOs und EF-Persistenz unter Application/Games/ArkNova; allgemeiner
Transfercode kennt keine Arche-Nova-Felder. Custom Games sind noch nicht unterstützt.
Format, Matching und Limits: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).

Die Application erzeugt BgtExportContent genau einmal vor der Ausgabeauswahl; das
versionierte Austauschformat kennt keinen Transport. App/Features/History/Export
orchestriert über HistoryExportFlow und registrierte IHistoryExportTarget-Ausgabeziele
Speichern oder Teilen. Native Dokument-Picker liegen unter Platforms/Android und
Platforms/iOS; Share verwendet MAUI. Weitere Transportziele ändern weder das
Austauschmodell noch die Importpipeline. Details stehen in IMPORT_EXPORT.md.

## Fachliche Rollen und gemeinsame Implementierungen

- `Query`: reine Leseprojektion ohne Mutation; etwa `CompletedGameSessionsQuery` oder `ArkNovaPreparationQuery`.
- `Store`: weiterhin sinnvoll für echte Preferences-/Hostgrenzen, kein EF-Wrapper.
- `Persistence`: konkrete zusammenhängende EF-Komponente oder Spielmodul-Erweiterung, insbesondere für Arbeiten auf einem bestehenden Context/in derselben Transaktion (z. B. Gruppenfarben oder Spiel-Importdaten).
- `UseCase`: Application-Orchestrierung, die Domainregeln und Persistenzoperationen für einen fachlichen Ablauf verbindet.
- `Handler`: Dispatch-/Routing-/Navigations- oder Transportentscheidung. Kein allgemeiner Service-Ersatz.

`DataAccess` ist kein bevorzugter Standardname. Konkrete Benennungs- und Kommentarregeln
stehen in AGENTS.md; objektive Produktions-Namingregeln in .editorconfig.

Scoring und historische Korrektur verwenden EF Core direkt in getrennten Use Cases.
Sie teilen ausschließlich die konkrete ArkNovaGameSessionResultQuery ohne Interface.
Diese Klasse besitzt nur die gemeinsame Leseprojektion, keine Schreibmethoden.

Die gemeinsame Projektion `ArkNovaGameSessionResultData`, ihre Spielerwerte und der
CompletionPlan gehören unter `Application/Games/ArkNova/GameSessions/Results`, nicht
zum Live-Scoring-Slice. Sie können unvollständige Werte und alle GameSession-Status
repräsentieren. Die Schreibregeln bleiben spezifisch: Live-Scoring speichert inkrementell;
historische Korrektur ersetzt Ergebniswerte/Placements/Kartenzuordnungen atomar für
Completed. Prüfungen in Application und an der Persistenzgrenze sind bewusst vorhanden,
damit inzwischen archivierte Karten nicht neu zugeordnet werden. Die Domainmethoden
ändern ausschließlich editierbare Ergebniswerte am geladenen Zustand; Setupdaten bleiben erhalten.
Der EF-Code gehört zum jeweiligen Spiel-/Use-Case-Unterordner.

`ArkNovaGameSessionNavigationHandler` implementiert die allgemeinen Ports
`IGameSessionNavigationHandler` und `IGameSessionResumeHandler`. Gruppenliste, Historie,
Spielerbereich und Resume delegieren Status→Zielroute an denselben Modulbaustein. Resume
restauriert zusätzlich den gespeicherten Gruppenkontext. Die allgemeinen Gruppen-/History-
Features kennen keine konkreten Ark-Nova-Partieseiten. Der gemeinsame Wertungsscreen bleibt
erhalten; `ArkNovaScoringMode` benennt Live-Scoring und historische Korrektur explizit.

ImportPlan/ImportPlanner, HistoryTransferPersistence und HistoryTransferUseCase liegen in getrennten,
fachlich benannten Dateien. Kleine eng zugehörige Begleittypen bleiben beim Haupttyp.
Weder Zeilenzahl noch ein gemeinsamer Entitytyp begründen eine Zusammenlegung/Trennung:
maßgeblich sind Ownership und derselbe Änderungsgrund. Keine künstlichen Mini-Interfaces.

## Bewusste technische Entscheidungen

### Arche-Nova-Fotoanalyse

Aktuelle Abschlussentscheidung: [CV-Status](ARK_NOVA_CV_STATUS.md).
Erkennungsforschung eingefroren, gehärtetes A bleibt Referenz/Basis; keine C3-Fusion.
MAUI/C# → `IArkNovaVisionEngine` in Application → C-ABI → gemeinsamer C++-OpenCV-Kern
unter `App/Games/ArkNova/PhotoRecognition/Native`. Keine OpenCvSharp-Pakete in der App.
Native Evidenz enthält nur Registrierung und Feldmesswerte; unverändertes Ranking
A_NoZero/Confidence E bleiben in Application. SafeHandle besitzt den serialisierten
nativen Kontext, aufrufereigene Puffer werden nur synchron geliehen. Beide Hosts
verwenden dieselben Quellen, Referenzassets und ABI. Android paketiert eine `.so`,
iOS ein zusammengeführtes statisches Archiv mit `NativeReference`/`__Internal`.
Der implementierte iOS-Host benötigt noch manuelle Apple-Build-/Link-/Runtimeprüfung.

Kamera/Systempicker liefern denselben Service-Input. Foto-Review verwendet eigene
Entwurfsobjekte ohne Autosave-Bindungen und verändert keine persistierten Werte.
Erst ausdrückliche Bestätigung ruft einen validierenden Application-Use-Case mit
einem atomaren `SaveChangesAsync` auf. Fehler lassen die bisherigen Werte bestehen.
Android Release/Trimming und x64-App-Workflow sind geprüft; Native Mobile ist
fachlich für assistierte Wertung freigegebener Produktionspfad. Dokumentierte
Plattformabweichungen sind bewusst akzeptiert. Windows/OpenCvSharp bleibt
Referenz-/Regressionstool. Frühere Empfehlungen in historischen Reports lösen
keine weitere CV-Forschung aus.

`tools/Games/ArkNova/PhotoRecognition` ist ein Windows-x64-Entwicklungswerkzeug
für Phase 1A, kein viertes Produktionsprojekt. Es rekonstruiert die Bildpipeline aus
dem Forschungs-Handoff; die früheren Python-Implementierungen lagen nicht vor.
OpenCvSharp4 und die native Slim-Windows-Runtime werden ausschließlich in den CV-Entwicklungswerkzeugen
referenziert. Core, Application und App enthalten keine CV-Paketreferenzen.
Die reinen experimentellen Feld-/Confidence-Regeln liegen im Arche-Nova-Scoring-Slice
in Application. Das Werkzeug liefert Diagnostik-JSON und schreibt keine Partiewerte.

Die kanonische Referenztafel/v2-Geometrie liegen im Werkzeug und werden als
verlinkte MAUI-Assets paketiert; Testfotos und B/C-Forschungsassets bleiben außen vor.
Der Windows-Analyzer berechnet Referenzfeatures einmal beim Aufbau. Der native
Kontext besitzt Referenz/Masken, die Registrierung berechnet Features je Aufruf;
ein serialisierter Featurecache wird nicht eingeführt. Benchmark und Grenzen
stehen im README des Werkzeugs.

Die Registrierung verlangt ausreichende Matches/Inlier, begrenzten Projektionsfehler,
räumlich verteilte Inlier, eine plausible konvexe Bildprojektion und mindestens 75 %
mittlere Feldsichtbarkeit je Track. Kandidaten müssen mindestens 97 % sichtbar sein.
Diese experimentellen Grenzen
sind keine produktiv validierten Schwellen. Außerhalb des Fotos liegende schwarze
Warp-Ränder dürfen keine Farbevidenz liefern. Schlechte Registrierung liefert keine Werte.

Der Analyzer behält die ursprüngliche Baseline als Diagnostik bei: unvollständige
Tracks (<99 %) und mindestens 6 % erweiterte Farbmaske im direkten Artenschutz-
Nachbarfeld reduzieren High auf Uncertain. Werte werden dabei nie umgeschrieben.
Die nicht rekonstruierte Richtungsheuristik ist im Analyzer deaktiviert; das reine
Handoff-Prädikat bleibt für eine spätere kalibrierte Richtungsinformation testbar.

`ArkNovaConfidencePolicy` im Entwicklungswerkzeug vergleicht fünf kleine Varianten.
`analyze` verwendet nach der CV-Kandidatenbildung die auf Calibration gewählte
Confidence-Variante E: lokale Feldsichtbarkeit,
mindestens zwei unabhängig belegte Tracks zur Unterstützung einer Spielerfarbe
(je mindestens 15 % Kernfarbe), kernbasierte Dominanz gegenüber dem zweitbesten
Feld und zurückhaltende Medium-Freigabe bei gesättigten Scores. Ohne Farbstützung
liefert sie null/Uncertain statt eines scheinbaren Werts. RawPrediction bleibt in
den Confidence-Berichten separat erhalten. Artenschutz wird wegen nicht zuverlässig
trennbarer Calibration-Fehler grundsätzlich Uncertain; dies ist eine dokumentierte
Grenze des Prototyps, keine produktive Erkennungsfreigabe.

Der Confidence-V2-Datensatz wird unverändert mit 2000-px-Bildern verwendet.
`split.json` fixiert 17 Calibration- und 8 Holdout-Fotos. Die Auswahl wurde vor
Holdout-Auswertung mit Split-/Quellcode-Hashes eingefroren; der Benchmark prüft
dies vor Zugriff auf Holdout-Bilder. Ground Truth beeinflusst nur Auswertung und
Gates, niemals Feldentscheidung oder Farbanwesenheit. Alle 300 Entscheidungen
(183 vorhandene Werte, 117 explizit abwesende Farb-/Track-Kombinationen) zählen;
akzeptierte Phantomfarben sind Accuracy-Fehler. Die vollständigen Berichte liegen
unter `tools/Games/ArkNova/PhotoRecognition/Reports`.

Der Holdout bestätigt nur 38,3 % Coverage bei 23/23 richtigen akzeptierten Werten.
Die Rohgenauigkeit beträgt 42/60. Die 75-%-/98-%-Ziele sind nicht gemeinsam erreicht;
ein erfolgreiches Sicherheitsgate bezeichnet keine Produktionsreife. `viewIndex`
ist die Aufnahmereihenfolge und keine kalibrierte Kameraposition.

Der optionale `ArkNovaBlackDiagnostics`-Observer im Entwicklungswerkzeug liest die
tatsächlich verwendeten Zwischenmatrizen synchron und exportiert ausschließlich
Diagnostik. Bildannotationen verwenden Kopien. Der Aufruf `diagnose-black` untersucht
IMG_4265–4268 ohne Änderung von Schwellen, Gewichtung, Confidence oder Geometrie.
Die bestätigte physische Belegung ist Artenschutz 20; die Diagnose weist Feld 0
als Fehlgewinner und keine strenge Schwarzbelegung auf 21 nach. Ergebnisse und
methodische Grenzen stehen unter `PhotoRecognition/Reports/BlackConservationDiagnosis`.

`ArkNovaRankingCapture` liest dieselben registrierten Pixel und Feldmasken für das
separate A/B/C-Rankingexperiment. Die acht Varianten teilen eine Registrierung;
die Original-Baseline wird je Entscheidung auf Gleichheit mit dem Analyzer geprüft.
Relative Schwarz-Evidenz und Kernrangfolge bei Sättigung sind nur Experimente.
Übernommen wurde ausschließlich `A_NoZero` in `ArkNovaRankingRules`.
`analyze` schließt Wert 0 aus der CV-Kandidatenbildung aus. Schwache Kandidaten
(Score <0,38, FullColor <0,06 oder CoreColor <0,06) bleiben null. Nach Ersatz eines
früheren Nullfeld-Gewinners gilt zusätzlich CoreColor >=0,10. Ein stark belegtes
ausgeschlossenes Nullfeld kann als reine Kontrollregion Enthaltung auslösen;
es wird weder als Wert noch als Nachbaralternative angeboten. Die Kontrollregel
verlangt mindestens 30 % Full-/Core-Farbe und einen Score mindestens wie der Ersatz.
Keine dieser Grenzen ändert die Confidence-Policy oder die manuelle Domain: dort
bleibt 0 weiterhin zulässig. Die Polygondatei wird nicht verändert.

Das Rankingexperiment verwendet denselben 17/8-Split. Die Auswahl wurde vor dem
einzigen Holdout-Lauf in `ranking-selection.json` einschließlich Quellcode-,
Geometrie-, Ground-Truth- und Split-Hashes eingefroren. Calibration verbessert sich
von 93 auf 94/123, Holdout von 42 auf 45/60 richtige ausgegebene Rohkandidaten;
Schwarz-Artenschutz erreicht dort 5/5 statt 2/5. Zwei schwache Schwarz-Rufkandidaten
auf Calibration werden zusätzlich zurückgehalten. Die unveränderte Confidence-
Policy bleibt auf Holdout bei 38,3 % Coverage. Berichte und Vorher-/Nachher-Bilder
liegen unter `PhotoRecognition/Reports/RankingExperiment`. Frühere `benchmark`-,
`confidence`- und `diagnose-black`-Aufrufe behalten ihre dokumentierte historische
Baseline für Regression/Vergleich; sie sind keine aktuelle CV-Eingabefunktion.

`tools/Games/ArkNova/StructureResearch` ist ein weiteres ausschließliches
Entwicklungswerkzeug. Es kompiliert die kanonischen Analyzer-/Rankingquellen
als verlinkte Dateien und benutzt lesende Frame-Observer derselben Registrierung.
Der farbunabhängige Strukturweg erhält Graustufen-/Gradientenreferenzen aus
Empty-Fotos und zählt nur Komponentenpixel innerhalb von Polygon und Core als
Kontakt. Er verändert keine A-Erkennungsregel. Quellen, Config, Normalcaches und
Inputs werden vor Fall-Auswertung gehasht; ein Run-Latch verhindert wiederholtes
Auswerten. Ein technischer A-Zahlenfehler erforderte eine dokumentierte
Wiederaufnahme mit identischen Detektor-/Modellhashes und expliziter Fehlerzählung.
Ergebnisse, Grenzen der Near-Object-Diagnose und DINOv3-Verfügbarkeit stehen unter
`PhotoRecognition/Reports/ResearchV3`. Die sehr geringe Struktur-Recall rechtfertigt
keine Fusion. Historische Freeze-Dateien werden auch bei Änderungen ausschließlich
an Observer-Plumbing nicht umgeschrieben; ihre Quellcodeprüfung kann dadurch
erwartungsgemäß spätere historische Holdout-Aufrufe ablehnen.

`tools/Games/ArkNova/ClassicCvResearch` untersucht als separates Entwicklungswerkzeug
geometrisches Directional Chamfer, 5×5-Census mit/ohne begrenzte Track-Translation-ECC
und eine klassische approximative Low-Rank/Sparse-Projektion aus Empty-Daten.
Es nutzt die vorhandenen Frame-Observer ohne Änderungen an A oder der globalen
Registrierung. Lokale ECC bleibt auf vier Pixel pro Achse begrenzt und verändert
ausschließlich Forschungsframes. C3 zeigt zusätzliche Occupancy, besonders bei
Artenschutz; die getestete einfache A+C3-Regel ist nicht für Übernahme geeignet.
Alle belegten Vergleichsdaten sind wiederverwendete Forschungsdaten, kein neuer
unabhängiger Holdout. Auswahl erfolgt auf alter Calibration, mit neuen Quellen-/
Inputhashes vor dem Vergleich. Ergebnisse und Entscheidungsgrenzen stehen unter
`PhotoRecognition/Reports/ClassicCvResearchV4`; historische Freezes bleiben erhalten.

`tools/Games/ArkNova/C3Refinement` begrenzt die Folgearbeit auf vorhandene
Empty-SVD-/Sparse-Evidenz, Attraktivitäts-Feld-Floors, lokalen Farb-Readout und zwei
feste Track-Fusionen. Mehrere Farben dürfen unabhängig Kontaktpixel desselben Feldes
stützen. Auswahl erfolgt nur auf alter Calibration/Empty; wiederverwendete Vergleiche
führen zu keiner Regeländerung. Die Qualitätsziele werden nicht erreicht; keine Fusion
wird in den App-Workflow übernommen. Ergebnisse: `PhotoRecognition/Reports/C3RefinementV5`.

Davon getrennt begrenzt `ArkNovaMaskFraction.ClampRoundoff` ausschließlich Rundungs-
abweichungen bis 1e−12 an den mathematischen [0,1]-Grenzen der A-Maskenanteile.
Größere/fachlich ungültige Werte bleiben Fehler. Analyzer und Ranking-Capture nutzen
denselben reinen Guard; Masken, Scoreformeln, Confidence und Domain bleiben gleich.
Alle 48 registrierten Frames wurden identisch reproduziert, 564 zuvor erfolgreiche
Kandidaten und Confidence-Werte bleiben erhalten. Historische Quellhashes bleiben
unverändert gespeichert und können unter dem gehärteten Code deshalb ablehnen.

Die mobile Produktionsbibliothek ist der gemeinsame native OpenCV-Kern.
Das fachliche GO für assistierte Wertung akzeptiert die dokumentierten
Plattformabweichungen bewusst; historische Paritäts-/GT-Gate-FAILs und Messwerte
bleiben erhalten. Alle Vorschläge benötigen Nutzerprüfung und Bestätigung.
Offen bleiben die manuellen Runtime-Nachweise auf echtem Android ARM64 und iOS.
Keine weitere CV-Optimierung geplant.
Foto-Review bestätigt dieselben fachlichen Wertungsfelder, ohne neue Tabellen oder
gespeicherte Bild-/Confidence-Daten. Technische Details: NativeVisionSpike-README.

SQLite kann die verwendeten DateTimeOffset-Sortierungen nicht zuverlässig übersetzen.
Die lokalen Listen projizieren deshalb nur benötigte Partiemetadaten und sortieren sie
anschließend im Speicher. Für die derzeit kleine lokale Datenmenge bleibt das bewusst
akzeptierte technische Schuld; keine neue Timestampdarstellung oder Schemaänderung.
IDbContextFactory, gezielte AsNoTracking-Projektionen, ExecuteUpdate/Delete und compiled
MAUI-Bindings bleiben bestehen. Größere Performanceänderungen benötigen Messdaten.

App injiziert feste Settings-/Open-GameSession-Abhängigkeiten direkt. Nur AppShell wird
weiterhin im Bootstrap über IServiceProvider aufgelöst: Die Seitenerzeugung muss nach
InitializeComponent und Theme-Anwendung erfolgen. Eine zusätzliche Startup-Serviceschicht
würde diese Lebensdauer nur verstecken. Navigation-Preferences liegen bei App/Navigation.
