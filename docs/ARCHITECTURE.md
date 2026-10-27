# Boardgame Tracker – Architektur v0.5

## 1. Zweck

Dieses Dokument ist der verbindliche Architekturstand für den Boardgame
Tracker nach dem ersten Persistenz-Prototypen.

Die Architektur soll:

- für eine mobile .NET-MAUI-App auf Android und iOS geeignet sein,
- für einen einzelnen Entwickler verständlich und wartbar bleiben,
- mehrere Brettspiele unterstützen, ohne eine generische Game Engine zu
  bauen,
- fachliche Regeln unabhängig von MAUI, EF Core und SQLite halten,
- Features weitgehend unabhängig voneinander entwickeln lassen,
- Vertical Slice Architecture pragmatisch einsetzen,
- unnötige Schichten, Interfaces, Handler und Repository-Abstraktionen
  vermeiden.

Erstes vollständig unterstütztes Spiel ist **Arche Nova**.

------------------------------------------------------------------------

## 2. Architekturentscheidung in einem Satz

Der Boardgame Tracker ist ein **modularer Monolith mit pragmatischen
Vertical Slices**, einem kleinen gemeinsamen Domain-Kern, einer
schlanken Application-Schicht für Use Cases und Ports sowie einer
Infrastructure-Schicht für konkrete technische Implementierungen.

Vertical Slice bedeutet dabei:

> Code wird primär nach fachlichem Feature und Use Case organisiert,
> nicht nach technischer Dateisorte.

Es bedeutet ausdrücklich **nicht**, dass jeder Use Case dieselbe Anzahl
an Klassen, Interfaces oder Schichten besitzen muss.

------------------------------------------------------------------------

## 3. Solution-Struktur

``` text
BoardgameTracker.sln

src/
  BoardgameTracker.App/
  BoardgameTracker.Application/
  BoardgameTracker.Core/
  BoardgameTracker.Infrastructure/

tests/
  BoardgameTracker.Core.Tests/
  BoardgameTracker.Application.Tests/
  BoardgameTracker.Infrastructure.Tests/
```

Nicht jedes Testprojekt muss sofort existieren. Es wird erst angelegt,
wenn dort reale Tests entstehen.

### Warum vier Produktionsprojekte?

Die vier Projekte haben konkrete unterschiedliche Verantwortlichkeiten:

``` text
App
UI, MAUI, XAML, ViewModels
        |
        v
Application
Use Cases, Read Models, Ports
        |
        v
Core
Domainmodell und reine Fachregeln

Infrastructure
EF Core, SQLite, Kamera, Dateisystem
        |
        +------> Application
        |
        +------> Core
```

`Application` wird nicht eingeführt, um eine
Clean-Architecture-Schablone nachzubauen.

Sie wird eingeführt, weil der erste reale Gruppen-Slice gezeigt hat,
dass andernfalls entweder:

- ViewModels direkt Infrastructure-Klassen konsumieren, oder
- Infrastructure UI-/Use-Case-spezifische Modelle wie `GroupSummary`
  definiert.

Beides vermischt Verantwortlichkeiten.

------------------------------------------------------------------------

## 4. Compile-Time-Abhängigkeiten

### Core

`BoardgameTracker.Core` kennt kein anderes Boardgame-Tracker-Projekt.

Core kennt insbesondere nicht:

- .NET MAUI
- XAML
- CommunityToolkit.Mvvm
- EF Core
- SQLite
- Dateisystem
- Kamera

### Application

`BoardgameTracker.Application` referenziert:

``` text
Application -> Core
```

Application kennt keine konkrete Infrastructure-Implementierung.

### Infrastructure

`BoardgameTracker.Infrastructure` referenziert:

``` text
Infrastructure -> Core
Infrastructure -> Application
```

Infrastructure implementiert technische Ports, die von Application
benötigt werden.

### App

`BoardgameTracker.App` referenziert hauptsächlich:

``` text
App -> Application
App -> Core
```

`App` darf Infrastructure im **Composition Root** (`MauiProgram`)
referenzieren, um konkrete Implementierungen für Dependency Injection zu
registrieren.

Feature-ViewModels sollen dagegen keine Klassen aus
`BoardgameTracker.Infrastructure` verwenden.

------------------------------------------------------------------------

## 5. Vertical-Slice-Regeln

### 5.1 Feature vor Dateityp

Ordner werden nach fachlicher Verantwortung gegliedert.

Nicht:

``` text
Services/
Repositories/
Models/
Helpers/
Managers/
```

sondern beispielsweise:

``` text
Groups/
People/
GameSessions/
Games/ArkNova/
History/
Statistics/
Settings/
```

Innerhalb eines größeren Features wird nach Use Case geschnitten:

``` text
Groups/
  ListGroups/
  CreateGroup/
  EditGroup/
```

### 5.2 Ein Slice darf klein sein

Ein Read-Slice kann nur aus zwei Typen bestehen:

``` text
ListGroups/
  GroupListItem.cs
  IListGroupsQuery.cs
```

Ein Write-Slice mit Fachlogik kann mehr benötigen:

``` text
CreateGroup/
  CreateGroupCommand.cs
  CreateGroupUseCase.cs
  ICreateGroupDataAccess.cs
```

Es wird kein künstlich identisches Schema für alle Slices erzwungen.

### 5.3 Keine generischen Handler-Abstraktionen

Nicht vorgesehen:

``` text
ICommand
IQuery<T>
ICommandHandler<T>
IQueryHandler<T>
IRequest<T>
```

MediatR wird nicht benötigt.

Use Cases dürfen normale C#-Klassen und Methoden sein.

### 5.4 Keine gemeinsame Abstraktion ohne echten gemeinsamen Bedarf

Wenn zwei Slices zufällig ähnliche Daten lesen, entsteht nicht sofort
ein globaler Service.

Kleine Duplikation ist zunächst akzeptabler als eine vorschnelle
gemeinsame Abstraktion.

Erst bei nachgewiesener gemeinsamer fachlicher Bedeutung wird Code
extrahiert.

### 5.5 Features besitzen Use Cases, nicht Datenbanktabellen

Ein `CreateGroup`-Slice darf Personen lesen, weil vorhandene Personen
für die Gruppenerstellung benötigt werden.

Das macht die Funktion nicht automatisch zu einem `People`-Feature.

Der Unterschied ist die Bedeutung:

``` text
People/ListPeople
```

bedeutet:

> Personen verwalten bzw. als eigenständige Funktion anzeigen.

Dagegen bedeutet:

``` text
Groups/CreateGroup/GetCreationOptions
```

> Daten laden, die zum Anlegen einer Gruppe benötigt werden.

Die zweite Query darf intern die Tabelle `Persons` lesen und bleibt
trotzdem Teil des Gruppen-Slices.

------------------------------------------------------------------------

## 6. Verantwortlichkeit der Projekte

## 6.1 BoardgameTracker.Core

Core enthält:

- Domain Entities
- Aggregate-Grenzen
- Value Types / Enums
- fachliche Invarianten
- reine Berechnungen
- reine Spielregeln

Core enthält nicht:

- ViewModels
- DTOs für einzelne Screens
- EF-Core-Konfigurationen
- SQLite-Queries
- Interfaces nur zum Zweck des Mockings
- Application-Use-Cases
- UI-Formatierung

### Vorgesehene Struktur

``` text
BoardgameTracker.Core/
  People/
    Person.cs

  Groups/
    Group.cs
    GroupPerson.cs

  Games/
    Game.cs

    ArkNova/
      ArkNovaPlayerColor.cs
      ArkNovaGroupPlayer.cs
      ArkNovaSessionPlayer.cs
      ArkNovaZooPlan.cs

      # später bei realem Bedarf
      ArkNovaSpecialCard.cs
      ArkNovaSpecialCardCategory.cs
      ArkNovaScoreCalculator.cs
      ArkNovaZooPlanDraw.cs

  GameSessions/
    GameSession.cs
    GameSessionPlayer.cs
    GameSessionStatus.cs
    StartingPlayerRotation.cs
```

`Game.cs` gehört unter `Games`, nicht unter `GameSessions`.

Es gibt bewusst keine zusätzlichen globalen Ordner `Domain`, `Rules`
oder `General`.

Wenn `Games/ArkNova` später groß wird, wird nach fachlichen
Unterbereichen geteilt, zum Beispiel:

``` text
Games/ArkNova/
  Setup/
  Scoring/
  ReferenceData/
```

Nicht automatisch nach technischen Typen wie `Entities`, `Services` und
`Rules`.

------------------------------------------------------------------------

## 6.2 BoardgameTracker.Application

Application enthält:

- konkrete Use Cases,
- Orchestrierung über mehrere Domainobjekte,
- Use-Case-spezifische Commands,
- Read Models,
- Ports für technische Abhängigkeiten, wenn sie wirklich benötigt
  werden.

Application enthält keine:

- XAML-Seiten,
- ViewModels,
- EF-Core-Typen,
- SQLite-Klassen,
- MAUI-Typen.

### Beispielstruktur

``` text
BoardgameTracker.Application/
  Groups/
    ListGroups/
      GroupListItem.cs
      IListGroupsQuery.cs

    CreateGroup/
      CreateGroupCommand.cs
      CreateGroupUseCase.cs
      ICreateGroupDataAccess.cs

    EditGroup/
      # wird mit der Gruppenbearbeitung ergänzt

  People/
    # erst wenn eigenständige Personen-Use-Cases entstehen

  GameSessions/
    # spätere allgemeine Partie-Use-Cases

  Games/
    ArkNova/
      # spätere Arche-Nova-Use-Cases
```

Application wird ebenfalls nach Features und Use Cases organisiert.

Es gibt keinen globalen `Services`- oder `Handlers`-Ordner.

------------------------------------------------------------------------

## 6.3 BoardgameTracker.Infrastructure

Infrastructure enthält konkrete technische Implementierungen:

- EF Core
- SQLite
- `DbContext`
- Entity-Konfigurationen
- Migrations
- Datenbankinitialisierung
- Implementierungen von Application-Ports
- Kamera
- Bild-/Dateizugriff
- spätere Bildanalyse

### Beispielstruktur

``` text
BoardgameTracker.Infrastructure/
  Persistence/
    SQLite/
      BoardgameTrackerDbContext.cs
      BoardgameTrackerDbContextFactory.cs
      DatabaseInitializer.cs

      Configurations/
        People/
        Groups/
        GameSessions/
        Games/
          ArkNova/

      Groups/
        SqliteListGroupsQuery.cs
        SqliteCreateGroupDataAccess.cs

      Migrations/
```

Infrastructure darf für eine konkrete Query hochgradig optimierten
EF-Code enthalten.

Was Infrastructure **nicht** entscheiden darf, ist, welches Read Model
ein Screen benötigt.

Der Vertrag kommt aus Application.

------------------------------------------------------------------------

## 6.4 BoardgameTracker.App

App enthält:

- MAUI Application und Window,
- Shell und Navigation,
- XAML Pages,
- ViewModels,
- UI-Zustand,
- UI-Formatierung,
- Animationen,
- lokalisierte sichtbare Texte,
- Composition Root.

### Beispielstruktur

``` text
BoardgameTracker.App/
  Features/
    Groups/
      ListGroups/
        GroupListPage.xaml
        GroupListPage.xaml.cs
        GroupListViewModel.cs

      CreateGroup/
        CreateGroupPage.xaml
        CreateGroupPage.xaml.cs
        CreateGroupViewModel.cs

    People/
      # sobald eigenständige Personenverwaltung existiert

    GameSessions/
      Preparation/
      Running/
      Result/

    Games/
      ArkNova/
        Scoring/

    History/
    Statistics/
    Settings/

  Controls/
    PlayerTile.xaml

  Resources/
    Localization/
```

ViewModels dürfen Application-Use-Cases und Query-Verträge konsumieren.

ViewModels dürfen keinen `DbContext` und keine SQLite-Implementierung
kennen.

------------------------------------------------------------------------

## 7. Der Gruppen-Slice als Referenz

## 7.1 Gruppenliste

Fachliche Anforderung:

- nur reguläre Gruppen,
- nach letzter Nutzung sortiert,
- Mitglieder anzeigen,
- letztes Spieldatum anzeigen.

Application definiert das benötigte Read Model:

``` csharp
public sealed record GroupListItem(
    Guid Id,
    string Name,
    IReadOnlyList<string> MemberNames,
    DateTimeOffset? LastPlayedAt);
```

und den Vertrag:

``` csharp
public interface IListGroupsQuery
{
    Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}
```

Infrastructure implementiert diesen Vertrag mit EF Core und SQLite.

Die konkrete Implementierung darf:

- Joins,
- Projections,
- `AsNoTracking`,
- SQLite-spezifische Workarounds,
- In-Memory-Nachbearbeitung

verwenden.

Die Semantik der Query wird aber von Application vorgegeben.

Damit ist eine hochgradig UI-nahe Read Query weiterhin mit Vertical
Slice vereinbar, ohne dass Infrastructure die UI modelliert.

### Kein `GroupSummary` in Infrastructure

`GroupSummary` als Infrastructure-Typ wird entfernt.

Infrastructure liefert Application-Typen über einen Application-Vertrag.

------------------------------------------------------------------------

## 7.2 Gruppe anlegen

Der Use Case umfasst:

- Gruppenname,
- regulär/temporär,
- vorhandene aktive Personen auswählen,
- neue Person/Gast inline anlegen,
- feste Zusammensetzung,
- Default-Reihenfolge,
- atomar speichern.

Hier gibt es echte Orchestrierung und Domainregeln.

Daher bekommt dieser Slice eine konkrete Application-Klasse:

``` text
CreateGroupUseCase
```

Das ViewModel verwendet diese konkrete Klasse direkt.

Es gibt bewusst **kein** zusätzliches:

``` text
ICreateGroup
```

nur um ein Interface zu besitzen.

### Technische Grenze

`CreateGroupUseCase` darf EF Core nicht kennen.

Deshalb existiert genau an dieser notwendigen Grenze ein Port:

``` text
ICreateGroupDataAccess
```

Der Port enthält ausschließlich technische Operationen, die dieser Slice
benötigt.

Infrastructure implementiert ihn beispielsweise als:

``` text
SqliteCreateGroupDataAccess
```

Es entsteht kein allgemeines `IGroupRepository`.

------------------------------------------------------------------------

## 7.3 Personen für die Gruppenerstellung

Es wird kein generisches `GetActivePersonsAsync` in einem `GroupStore`
geben.

Ebenso wird nicht vorschnell ein globales `PersonStore` eingeführt.

Für den Create-Group-Slice ist die fachliche Frage:

> Welche vorhandenen Personen dürfen in dieser Gruppenerstellung
> angeboten werden?

Diese Datenanforderung gehört zum Create-Group-Use-Case.

Wenn später eine eigenständige Personenverwaltung gebaut wird, erhält
sie eigene Slices wie:

``` text
People/ListPeople
People/EditPerson
People/DeactivatePerson
```

Erst wenn sich danach eine wirklich gemeinsame Application-Abstraktion
ergibt, wird sie extrahiert.

------------------------------------------------------------------------

## 8. Interface-Regel

Ein Interface wird nur eingeführt, wenn mindestens einer dieser Gründe
vorliegt:

1.  Application muss eine technische Abhängigkeit aufrufen, ohne
    Infrastructure zu referenzieren.
2.  Es gibt tatsächlich mehrere austauschbare Implementierungen.
3.  Eine externe Plattform-/Gerätegrenze muss abstrahiert werden.
4.  Die Abstraktion besitzt eigenständige fachliche oder technische
    Bedeutung.

Kein Interface nur für:

- jede Klasse,
- jedes ViewModel,
- jeden Handler,
- jede Entity,
- vermeintlich bessere Unit-Testbarkeit.

### Beispiele

Sinnvoll:

``` text
IListGroupsQuery
ICreateGroupDataAccess
ICameraService
IScoringBoardAnalyzer
```

Nicht vorgesehen:

``` text
IGroupRepository
IPersonRepository
IGameRepository
ICreateGroup
IGroupService
IUnitOfWork
```

solange kein konkreter Bedarf dafür entsteht.

EF Core `DbContext` stellt bereits Unit-of-Work- und
Repository-Funktionalität bereit; diese wird nicht pauschal erneut
abstrahiert.

------------------------------------------------------------------------

## 9. Domain-Review

## 9.1 Person

``` text
Person
- Id
- Name
- IsGuest
- IsActive
```

Entscheidungen:

- Gast und reguläre Person bleiben derselbe Typ.
- `IsGuest` darf später geändert werden, ohne Historie zu verlieren.
- `IsActive` ist ein echter boolescher Zustand und nicht nullable.
- Personennamen müssen nicht global eindeutig sein.

`Person` ist ein eigenes Aggregate Root.

------------------------------------------------------------------------

## 9.2 Group als Aggregate Root

``` text
Group
- Id
- Name
- IsTemporary
- Members
```

`GroupPerson` ist Bestandteil des Group-Aggregats:

``` text
GroupPerson
- GroupId
- PersonId
- DefaultOrder
```

### Wichtige Invarianten

Eine Gruppe:

- besitzt mindestens eine Person,
- enthält dieselbe Person höchstens einmal,
- besitzt eine eindeutige Reihenfolge,
- verwendet lückenlose Reihenfolgen `1..n`,
- besitzt eine feste Personenzusammensetzung.

Da laut Produktspezifikation eine andere Zusammensetzung eine andere
Gruppe darstellt, soll die Mitgliedermenge nach Erstellung nicht
beliebig über `Add`/`Remove` verändert werden.

Die Domain soll diese Regeln schützen.

Infrastructure darf diese Regeln zusätzlich über Keys, Unique Indices
und Constraints absichern, aber nicht deren primärer Besitzer sein.

### Konsequenz für den aktuellen Code

Regeln wie:

``` text
"Gruppe muss mindestens eine Person enthalten"
"Person darf nur einmal vorkommen"
```

dürfen nicht in `SqliteCreateGroupDataAccess` bzw. einem Store leben.

Sie gehören in `Group` bzw. in die Application-Orchestrierung, wenn
mehrere Aggregate beteiligt sind.

------------------------------------------------------------------------

## 9.3 Cross-Aggregate-Referenzen über IDs

Die Domain benötigt nicht automatisch für jeden Foreign Key eine
öffentliche EF-Navigation.

Bevorzugt:

``` csharp
public Guid PersonId { get; private set; }
```

statt zusätzlich:

``` csharp
public Person? Person { get; private set; }
```

wenn die Domain die `Person`-Referenz selbst nicht benötigt.

### Navigationen behalten wir dort, wo sie fachlich sinnvoll sind

Zum Beispiel:

``` text
Group -> Members
GameSession -> Players
```

Diese Collections gehören zum jeweiligen Aggregat.

### Navigationen entfernen wir dort, wo sie nur für EF-Bequemlichkeit existieren

Kandidaten:

``` text
GroupPerson.Person
GroupPerson.Group

GameSession.Game
GameSession.Group

GameSessionPlayer.GameSession
GameSessionPlayer.Person

ArkNovaGroupPlayer.GroupPerson

ArkNovaSessionPlayer.GameSessionPlayer
ArkNovaSessionPlayer.ZooPlanOption1
ArkNovaSessionPlayer.ZooPlanOption2
ArkNovaSessionPlayer.SelectedZooPlan
```

EF Core kann die Beziehungen weiterhin explizit über Foreign Keys
konfigurieren.

Dadurch:

- gibt es weniger potenziell inkonsistente Objektzustände,
- bedeutet `null` nicht mehr gleichzeitig "fachlich optional" und "nicht
  geladen",
- bleiben Aggregate klarer,
- werden Read Queries stärker auf Projections statt auf vollständige
  Entity-Graphen ausgerichtet.

------------------------------------------------------------------------

## 9.4 Game

`Game` ist allgemeine Referenzinformation für unterstützte Spiele.

``` text
Game
- Id
- Key
```

`Game` gehört fachlich unter:

``` text
Core/Games/
```

nicht unter `GameSessions`.

Der Key ist sprachneutral und stabil.

Beispiele:

``` text
ark-nova
terraforming-mars
```

Sichtbare Namen werden lokalisiert und nicht als sprachabhängige
Persistenzwerte verwendet.

------------------------------------------------------------------------

## 9.5 GameSession als Aggregate Root

``` text
GameSession
- Id
- GameId
- GroupId
- Status
- StartedAt
- EndedAt?
- ModifiedAt
- Players
```

`GameSessionPlayer` gehört zum Session-Aggregat:

``` text
GameSessionPlayer
- GameSessionId
- PersonId
- Order
- Placement?
```

### Invarianten

Beim Erstellen einer gestarteten Partie:

- gibt es mindestens einen Teilnehmer,
- jeder Teilnehmer kommt höchstens einmal vor,
- Reihenfolgen sind eindeutig und lückenlos,
- `Order = 1` ist der Startspieler.

Statusübergänge:

``` text
Running -> Scoring -> Completed
```

Andere Übergänge sind nicht erlaubt.

`Complete` darf nicht direkt von `Running` aufgerufen werden.

`EndedAt` darf nicht vor `StartedAt` liegen.

Da die Vorbereitung im MVP nicht persistiert wird, ist die
Startreihenfolge beim Erzeugen der Session bereits final und wird als
Snapshot gespeichert.

------------------------------------------------------------------------

## 9.6 StartingPlayerRotation

`StartingPlayerRotation` ist eine allgemeine Partie-Regel und gehört
daher zu:

``` text
Core/GameSessions/
```

nicht zu Arche Nova.

Sie ist:

- rein,
- zustandslos,
- ohne Infrastructure-Abhängigkeit,
- direkt unit-testbar.

------------------------------------------------------------------------

## 9.7 Arche-Nova-Gruppendaten

``` text
ArkNovaGroupPlayer
- GroupId
- PersonId
- DefaultColor
```

Diese Daten erweitern eine allgemeine Gruppenmitgliedschaft um
Arche-Nova-spezifische Defaults.

Die bestehende zusammengesetzte FK-Beziehung zu:

``` text
GroupPerson(GroupId, PersonId)
```

bleibt sinnvoll.

Default-Farben müssen innerhalb einer Gruppe eindeutig sein.

------------------------------------------------------------------------

## 9.8 Arche-Nova-Partiedaten

``` text
ArkNovaSessionPlayer
- GameSessionId
- PersonId
- Color
- ZooPlanOption1Id
- ZooPlanOption2Id
- SelectedZooPlanId
- Attractiveness?
- Conservation?
- Reputation?
```

Die zusammengesetzte Beziehung zu:

``` text
GameSessionPlayer(GameSessionId, PersonId)
```

bleibt sinnvoll.

### Korrektur: SelectedZooPlanId

Da die Vorbereitung nicht persistiert wird und eine Partie erst
gestartet werden darf, nachdem jeder Spieler einen der beiden
angebotenen Zoopläne gewählt hat, soll:

``` text
SelectedZooPlanId
```

bei einer persistierten laufenden Partie **nicht nullable** sein.

Die Nullbarkeit war nur notwendig, wenn ein unfertiger Setup-Zustand
persistiert würde.

### Nullable Score-Werte bleiben korrekt

Während die Partie läuft bzw. bevor die Wertung vollständig erfasst ist,
dürfen:

``` text
Attractiveness
Conservation
Reputation
```

nullable sein.

### Session-weite Zooplan-Eindeutigkeit

Die Regel:

> Jeder Zooplan darf innerhalb einer Partie insgesamt nur einmal
> angeboten werden.

ist eine session-weite Invariante.

Sie kann nicht sinnvoll allein in einem einzelnen `ArkNovaSessionPlayer`
geschützt werden.

Sie gehört in die Arche-Nova-Vorbereitungs-/Startlogik, die alle Spieler
gleichzeitig kennt.

Die Datenbank darf zusätzliche Constraints verwenden, aber das Schema
wird nicht künstlich normalisiert, nur um diese eine Regel mit einem
Unique Index ausdrücken zu können.

------------------------------------------------------------------------

## 9.9 Spezialkarten

Die Spezialkarten gehören zum Arche-Nova-Modul.

Geplant:

``` text
ArkNovaSpecialCard
ArkNovaSessionSpecialCard
```

Die konkrete Karte wird gespeichert, nicht nur eine Anzahl.

Die Karte kann pro Partie höchstens einem Spieler zugeordnet werden.

Die Kategorien:

``` text
Elephant
Eagle
```

werden für spätere Statistiken verwendet.

Diese Tabellen müssen nicht vorzeitig in die Initial-Migration
aufgenommen werden. Eine spätere Migration ist ausdrücklich vorgesehen.

------------------------------------------------------------------------

## 10. Persistenz mit EF Core

Verwendet werden:

- EF Core 10
- Microsoft.EntityFrameworkCore.Sqlite
- Migrations
- `IDbContextFactory<BoardgameTrackerDbContext>`
- Fluent Mapping mit `IEntityTypeConfiguration<T>`

### Keine Lazy-Loading-Proxies

Lazy Loading wird nicht verwendet.

Queries laden exakt die benötigten Daten über:

- Projection,
- Join,
- explizite Includes, wenn ein Entity-Graph wirklich benötigt wird.

### Reads

Read-only Queries verwenden bevorzugt:

``` csharp
.AsNoTracking()
```

Read-Slices dürfen direkt in ein Application-Read-Model projizieren.

### Writes

Writes laden nur die Aggregate bzw. Daten, die zur Ausführung des Use
Cases notwendig sind.

Businessregeln werden vor `SaveChangesAsync` in Domain/Application
ausgeführt.

Datenbank-Constraints bleiben als zusätzliche letzte Schutzlinie
erhalten.

------------------------------------------------------------------------

## 11. Keine Repository-Schicht pro Entity

Nicht vorgesehen:

``` text
IPersonRepository
IGroupRepository
IGameSessionRepository
IArkNovaRepository
```

nur weil diese Entities Tabellen besitzen.

Der EF-Core-`DbContext` bietet bereits Repository-/Unit-of-Work-artige
Funktionalität.

Wir abstrahieren stattdessen nur die technische Grenze, die ein
konkreter Application-Slice benötigt.

Beispiele:

``` text
IListGroupsQuery
ICreateGroupDataAccess
```

Diese Verträge sind use-case-spezifisch und wachsen nicht zu einem
zentralen God Repository.

------------------------------------------------------------------------

## 12. Read Models sind Application-Typen

Ein Read Model repräsentiert eine konkrete Anwendungsanforderung.

Beispiele:

``` text
GroupListItem
HistoryListItem
ArkNovaScoringPlayer
StatisticsSummary
```

Sie sind:

- keine Domain Entities,
- keine Infrastructure-Modelle,
- nicht automatisch ViewModels.

Application definiert die fachlich benötigten Daten.

App darf daraus UI-spezifische ViewModels erzeugen, etwa um:

- Datumswerte zu formatieren,
- Texte zu lokalisieren,
- Commands hinzuzufügen,
- visuelle Zustände abzuleiten.

------------------------------------------------------------------------

## 13. Domain Entities sind keine Screen-DTOs

Views und Listen sollen nicht aus Bequemlichkeit vollständige
Domain-Graphen laden.

Beispiel Gruppenliste:

Nicht bevorzugt:

``` text
Group
  -> GroupPersons
      -> Persons
  + alle GameSessions
```

nur um vier Textzeilen anzuzeigen.

Bevorzugt:

``` text
IListGroupsQuery
    -> Projection
    -> GroupListItem
```

Domain Entities werden vor allem für Write Use Cases und echte
Domainregeln verwendet.

------------------------------------------------------------------------

## 14. MVVM-Grenze

### View

Verantwortlich für:

- Layout,
- Controls,
- Bindings,
- visuelle Zustände,
- Animationen.

### ViewModel

Verantwortlich für:

- Screen State,
- Commands,
- Navigation aus Nutzersicht,
- UI-Validierung,
- Aufruf von Application-Use-Cases.

Nicht verantwortlich für:

- SQL/EF Queries,
- Domain-Invarianten,
- spielspezifische Berechnungen.

### Application

Verantwortlich für:

- konkreten Use Case,
- Orchestrierung,
- Berechtigungen/Existenzprüfungen im Use Case,
- Auswahl der benötigten Domainoperationen,
- technische Ports.

### Core

Verantwortlich für:

- Regeln, die unabhängig vom konkreten Screen gelten.

------------------------------------------------------------------------

## 15. Feature-Grenzen

## People

Besitzt eigenständige Use Cases rund um Personen:

- Personenliste,
- Person bearbeiten,
- Gaststatus ändern,
- Person deaktivieren.

Eine Person darf gleichzeitig von anderen Features referenziert werden.

## Groups

Besitzt:

- Gruppenliste,
- Gruppe anlegen,
- Default-Reihenfolge,
- temporär/regulär,
- später Gruppenbearbeitung.

Der Create-Group-Slice darf Personen als Eingabe referenzieren und neue
Personen inline erzeugen.

## GameSessions

Besitzt allgemeine Partie-Lifecycle-Funktionen:

- Start,
- laufende Partie,
- Statuswechsel,
- Teilnehmer,
- Reihenfolge,
- Platzierung,
- Wiederaufnahme.

## Games

Besitzt allgemeine Spieldefinitionen und darunter spielspezifische
Module.

``` text
Games/
  Game
  ArkNova/
  später TerraformingMars/
```

## Games/ArkNova

Besitzt nur Arche-Nova-spezifische Regeln und Daten:

- Farben,
- Zoopläne,
- Arche-Nova-Wertung,
- Spezialkarten,
- Wertungstafel-/Fotoanalyse-Verträge, soweit fachlich.

## History

Ist primär ein Read-Feature über persistierte Sessions.

## Statistics

Ist primär ein Read-/Berechnungsfeature und darf game-spezifische
Statistikmodule besitzen.

## Settings

Besitzt App- und spielbezogene Präferenzen.

------------------------------------------------------------------------

## 16. Shared Code

Es gibt keinen pauschalen `Common`, `Helpers` oder `Utils`-Ordner.

Code wird erst geteilt, wenn mindestens zwei reale Verbraucher
existieren und die gemeinsame Bedeutung klar ist.

Sinnvoll geteilt:

- `PlayerTile` als tatsächlich mehrfach verwendetes UI-Control,
- reine fachliche Utility wie `StartingPlayerRotation`, wenn sie mehrere
  Session-Flows nutzt.

Nicht automatisch geteilt:

- ähnliche DTOs,
- ähnliche Query-Helfer,
- ähnliche kleine Mapping-Funktionen.

------------------------------------------------------------------------

## 17. Dependency Injection

DI wird für echte Abhängigkeiten verwendet.

Registriert werden beispielsweise:

``` text
IListGroupsQuery
    -> SqliteListGroupsQuery

ICreateGroupDataAccess
    -> SqliteCreateGroupDataAccess

CreateGroupUseCase
GroupListViewModel
CreateGroupViewModel
```

Nicht jede Klasse benötigt ein Interface.

Der Composition Root ist `MauiProgram`.

------------------------------------------------------------------------

## 18. Tests

### Core Tests

Schnelle Unit Tests für:

- Group-Invarianten,
- Session-Statusübergänge,
- Reihenfolge,
- StartingPlayerRotation,
- Arche-Nova-Regeln,
- Punkteberechnung.

### Application Tests

Nur für Use Cases mit realer Orchestrierungslogik.

Beispiel:

``` text
CreateGroupUseCase
```

Hier kann der eine notwendige Port durch einen kleinen Fake ersetzt
werden.

### Infrastructure Tests

EF-Core-Queries und Mapping werden gegen echtes SQLite getestet.

Bevorzugt:

- temporäre SQLite-Datenbank,
- SQLite In-Memory mit geöffneter Connection.

Nicht bevorzugt:

- `DbContext` mocken,
- EF LINQ-Verhalten künstlich nachbauen.

------------------------------------------------------------------------

## 19. Aktueller Umbau gegenüber Scaffold v0.4

Folgende Architekturteile des bisherigen Scaffolds werden geändert:

1.  `BoardgameTracker.Application` wird ergänzt.
2.  `GroupStore` wird aufgelöst.
3.  `GroupSummary`, `PersonSummary` und Create-Request-Typen verlassen
    Infrastructure.
4.  Gruppenliste wird ein eigener Query-Slice.
5.  Gruppenerstellung wird ein eigener Application-Use-Case.
6.  Es wird kein `PersonStore` nur aus Symmetriegründen eingeführt.
7.  `Game.cs` wandert von `GameSessions` nach `Games`.
8.  Cross-Aggregate-Navigationen werden entfernt, wenn die Domain sie
    nicht benötigt.
9.  `Group` übernimmt seine Mitglieder-Invarianten.
10. `GameSession` übernimmt Status- und Teilnehmer-Invarianten.
11. `ArkNovaSessionPlayer.SelectedZooPlanId` wird für persistierte
    Sessions verpflichtend.

------------------------------------------------------------------------

## 20. Bewusst nicht verwendet

Zum aktuellen Zeitpunkt nicht vorgesehen:

- MediatR
- generisches CQRS-Framework
- Event Sourcing
- Domain Events aus Prinzip
- Generic Repository
- Unit-of-Work-Wrapper um EF Core
- Interface für jede Klasse
- Handler-Interface für jeden Use Case
- AutoMapper nur für triviale Mappings
- Microservices
- Backend
- Cloud-Synchronisierung
- generische Game Engine
- `Common`-/`Utils`-Sammelordner
- tiefe Clean-Architecture-Projektbäume

Diese Entscheidungen werden nur geändert, wenn reale Anforderungen
entstehen.

------------------------------------------------------------------------

## 21. Architekturregeln

### Regel 1

Features werden nach fachlicher Verantwortung und Use Case organisiert.

### Regel 2

Core enthält fachliche Wahrheit, nicht Screen-spezifische Datenmodelle.

### Regel 3

Application definiert, was ein Use Case benötigt.

### Regel 4

Infrastructure entscheidet, wie technische Daten beschafft oder
gespeichert werden.

### Regel 5

App entscheidet, wie Daten dargestellt und bedient werden.

### Regel 6

Spielspezifische Daten bleiben im jeweiligen Spielmodul.

### Regel 7

Neue Abstraktionen werden erst eingeführt, wenn sie eine reale Grenze
oder Wiederverwendung abbilden.

### Regel 8

Eine einfache konkrete Klasse ist einer bedeutungslosen
Interface-Kaskade vorzuziehen.

### Regel 9

Datenbank-Constraints ergänzen Domainregeln, ersetzen sie aber nicht.

### Regel 10

Verständlichkeit und Änderbarkeit haben Vorrang vor architektonischer
Symmetrie.

------------------------------------------------------------------------

## 22. Implementierter Gruppen-Slice in Scaffold v0.6

Der Gruppenbereich ist nun nach der vorgesehenen Vertical-Slice-Struktur
umgesetzt:

``` text
App/Features/Groups
        |
        v
Application/Groups
        |
        +------> Core/Groups
        |
        v
Application-Ports
        ^
        |
Infrastructure/Persistence/SQLite/Groups
```

Implementierte Slices:

``` text
Groups/
  ListGroups/
  CreateGroup/
  EditGroup/
```

Der End-to-End-Flow umfasst jetzt Gruppenliste, Gruppenerstellung,
Wiederverwendung bzw. Neuanlage von Personen, Default-Reihenfolge,
Persistenz und Gruppenbearbeitung.

Der nächste größere Feature-Slice ist die Arche-Nova-Partievorbereitung.

------------------------------------------------------------------------

## 23. Dokumentationsstand

Das fachliche Datenmodell wurde mit Scaffold v0.6 als `DATA_MODEL v0.3`
konsolidiert.

Architektur, Produktanforderungen, UI-Wireframe und Datenmodell bilden
damit vor Beginn der Arche-Nova-Partievorbereitung einen gemeinsamen
Referenzstand.

------------------------------------------------------------------------

## 24. Ergänzung – EditGroup und CreateGroup-UI

### EditGroup als eigener Slice

Das Bearbeiten einer Gruppe ist ein eigener Use Case:

``` text
Groups/
  ListGroups/
  CreateGroup/
  EditGroup/
```

Die Gruppenübersicht navigiert über eine separate Bearbeiten-Aktion in
diesen Slice. Das Antippen der Gruppenkarte selbst bleibt der Einstieg
in die Partievorbereitung.

`EditGroup` ist nicht Teil von `ListGroups`, weil Lesen der Übersicht
und Ändern einer Gruppe unterschiedliche Anwendungsfälle sind.

### CreateGroup und Personen

Die UI priorisiert vorhandene Personen. Diese UI-Entscheidung ändert die
fachliche Slice-Grenze nicht:

- `CreateGroup` darf vorhandene aktive Personen für seine Auswahl lesen,
- `CreateGroup` darf neue Personen als Teil seines Workflows erzeugen,
- daraus entsteht kein allgemeiner `PersonStore`,
- eine spätere eigenständige Personenverwaltung erhält eigene
  `People`-Slices.

Die Reihenfolge der Create-Group-Maske ist eine App-Verantwortlichkeit.
Die Regeln, welche Mitglieder eine gültige Gruppe bilden, bleiben
Core-Verantwortlichkeit.

------------------------------------------------------------------------

## Aktualisierung v0.6 – Fehler- und Busy-Konzept

### Startup-Grenze

Die eigentliche `AppShell` darf nur erzeugt werden, wenn
`DatabaseInitializer.InitializeAsync()` erfolgreich abgeschlossen wurde.

- Eine noch nicht vorhandene Datenbank ist ein normaler Erststart und
  wird durch EF-Core-Migrationen erzeugt.
- Scheitert Erzeugung, Öffnen oder Migration, bleibt die App auf einem
  fatalen Startup-Fehlerbildschirm.
- Der Fehlerbildschirm bietet `Erneut versuchen`.
- Ein automatisches Löschen oder Zurücksetzen der Datenbank findet nicht
  statt.

### Fehlerbehandlung

Unerwartete technische Exceptions werden nicht still verworfen.

- Die App-Schicht protokolliert die Exception über
  `Microsoft.Extensions.Logging`.
- Der Benutzer erhält eine verständliche fachliche/kontextuelle Meldung.
- DEBUG-Builds ergänzen die konkrete Exception-Message; beim fatalen
  Startup wird der vollständige Stacktrace angezeigt.
- Release-Oberflächen zeigen keine rohen technischen Details.

### Asynchrone Benutzeraktionen

Eine laufende asynchrone Benutzeraktion darf nicht parallel ein zweites
Mal gestartet werden.

Das gilt insbesondere für Navigation, Speichern, Laden, Partie
starten/beenden und spätere Fotoanalyse. Während der Aktion wird die
Interaktion gesperrt und ein Busy-Zustand sichtbar gemacht.

------------------------------------------------------------------------

## Aktualisierung v0.7 – Gruppen-Lifecycle

`Group.IsTemporary` beschreibt die bei Erstellung gewählte Lebensdauer.
Nach Erstellung ist nur die fachliche Transition
`temporary -> permanent` erlaubt; `permanent -> temporary` ist nicht
erlaubt.

`Group.IsArchived` ist davon unabhängig. Archivieren ist reversibel und
kein Löschen. Read-Queries filtern archivierte und temporäre Gruppen
standardmäßig aus und erhalten explizite Include-Optionen für die UI.

Die Schemaänderung benötigt die EF-Core-Migration `AddGroupArchiving`.

---

## v0.7.0 – Ark-Nova-Partievorbereitung

Der Vorbereitungsscreen erhält seine gruppenbezogenen Daten über `IArkNovaPreparationQuery` aus Application. Die SQLite-Implementierung projiziert ausschließlich die für diesen Use Case benötigten Daten: Gruppe, Mitglieder, Default-Reihenfolge und optionale Ark-Nova-Defaultfarbe.

Die eigentliche Vorbereitung wird noch nicht persistiert. UI-Änderungen an Reihenfolge, Farbe und Zooplan-Auswahl leben bis `Starten` ausschließlich im ViewModel. Dies ist bewusst so gewählt, damit Navigation zurück aus dem Setup keine unvollständige `GameSession` erzeugt.

Die atomare Erzeugung von `GameSession`, `GameSessionPlayer` und `ArkNovaSessionPlayer` wird dem nachfolgenden Start-Use-Case gehören.
