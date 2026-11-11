# Boardgame Tracker – Architektur

## 1. Zielbild

Boardgame Tracker ist ein **modularer Monolith mit pragmatischen Vertical Slices**.

Das Projekt ist bewusst ein Lernprojekt: Fachgrenzen, Abhängigkeiten und Ordnerstruktur
sollen sichtbar und nachvollziehbar bleiben. Verständlichkeit hat Vorrang vor cleveren
Abstraktionen; Lernorientierung rechtfertigt keine zusätzlichen Schichten oder
vorsorgliche Generalisierung.

> Allgemeiner Fach-/Presentation-Code kennt keine konkreten Spiele. Jedes Spiel lebt
> in `Games/<GameName>/` und bindet sich über kleine allgemeine Erweiterungspunkte ein.

## 2. Schichten

```text
App -> Application
App -> Core
Application -> Core
Application -> EF Core
App -> EF Core SQLite
```

- **Core:** Domain-Entities, Value Objects und fachliche Regeln; kein MAUI, EF Core
  oder SQLite.
- **Application:** Use Cases, Queries, Read Models, gemeinsamer DbContext,
  EF-Konfiguration und Initialisierung. EF Core ist erlaubt, SQLite-spezifische APIs
  nicht.
- **App:** MAUI/XAML, ViewModels, Navigation, Presentation, Plattformadapter,
  Composition Root und SQLite-Provider.

App darf Domain-Typen aus Core direkt verwenden. Dafür wird keine künstliche
DTO-/Wrapper-Schicht eingeführt.

Pages/ViewModels erhalten Use Cases oder Queries, niemals einen DbContext.

```text
ViewModel -> UseCase/Query -> DbContext -> Domain -> SaveChangesAsync
```

## 3. Spielmodule als harte Grenze

Spielabhängiger Produktionscode liegt auf allen Schichten unter:

```text
src/BoardgameTracker.Core/Games/<GameName>/
src/BoardgameTracker.Application/Games/<GameName>/
src/BoardgameTracker.App/Games/<GameName>/
```

Spieltests spiegeln diese Struktur; spielabhängige Entwicklungswerkzeuge dürfen unter
`tools/Games/<GameName>/` liegen.

Regeln:

- Ein Entwickler soll am Pfad erkennen, ob Code allgemein oder spielabhängig ist.
- Ein Spielmodul darf allgemeinen Code verwenden, aber kein anderes Spielmodul.
- Allgemeiner Code enthält keine konkreten Spieltypen, Spiel-Namespaces oder
  `GameKey`-Switches.
- Konkrete Spiele implementieren allgemeine Erweiterungspunkte in ihrem Modul.
- Der Composition Root kennt nur die öffentliche Modulregistrierung, nicht einzelne
  Featuretypen.
- Leere Spiegelordner werden nicht nur für Symmetrie angelegt.
- Gemeinsamer Code entsteht erst aus real nachgewiesener Gemeinsamkeit, nicht als
  Vorbereitung auf hypothetische Spiele.

### Partial-Type-Ausnahme

Spielabhängige Partial-Erweiterungen eines gemeinsamen Typs dürfen technisch dessen
allgemeinen Namespace verwenden. Ownership bleibt durch den Dateipfad im Spielmodul
bestimmt. Das ist keine Erlaubnis für sonstige konkrete Spielabhängigkeiten im
allgemeinen Code.

## 4. Domänenvokabular

Verbindliche allgemeine Begriffe:

```text
Game
GameSession
Group
Player
GroupPlayer
GroupGame
GameSessionPlayer
```

Nicht als Synonyme verwenden: `Person`, `Member`/`Participant` für `Player` oder
`Session` für `GameSession`.

Konkrete Felder und Invarianten: [DATA_MODEL.md](DATA_MODEL.md).

## 5. Vertical Slices und Ordner

Application und UI werden primär nach Fachfeature/Use Case organisiert.

Kleine sliceeigene Commands, Inputs, Results und Read Models dürfen beim Haupttyp
bleiben. Eigene Dateien/Ordner entstehen nur bei eigenständigem Fachbegriff,
Wiederverwendung, relevanter Logik oder unabhängigen Änderungsgründen.

Keine Standardhierarchie aus `Commands/`, `Queries/`, `Services/`, `Validators/`
oder `DTOs/` nur um einer Schablone zu folgen.

## 6. Domainmodellierung und Composition

Entities besitzen fachliche Identität; Aggregate schützen ihre Invarianten.
Cross-Aggregate-Beziehungen referenzieren bevorzugt IDs. Navigation Properties
werden nicht nur für EF-Bequemlichkeit öffentlich gemacht.

Spielabhängige Ergänzungen verwenden Composition statt Duplikation:

```text
Game              1 -> 0..1 <GameName>Game
GameSession       1 -> 0..1 <GameName>GameSession
GameSessionPlayer 1 -> 0..1 <GameName>GameSessionPlayer
```

Der gemeinsame Schlüssel darf als PK/FK wiederholt werden. Allgemeiner fachlicher
Zustand wie Reihenfolge oder Placement wird nicht kopiert.

`Game` speichert `Name`, `BadgeText`, `MinPlayers` und `MaxPlayers` als gemeinsame Stammdaten. Integrierte Module liefern die festen Werte beim Seeding; benutzerdefinierte Spiele speichern ihre Konfiguration. `GroupGame` prüft bei **neuen** Zuordnungen die tatsächlich geladene Mitgliedermenge gegen `Game.MinPlayers..Game.MaxPlayers`; ungültige Zuordnungen werden nicht gespeichert. Die Application lädt alle dafür erforderlichen Daten. Historische Sessions benötigen keine aktuelle `GroupGame`-Zuordnung; beim History-Import werden zu große/kleine neue Zuordnungen ausgelassen. Zentraler Code verwendet keine spieltypabhängigen Limit-Abfragen.

`GroupGame` ist die allgemeine aktuelle Gruppe↔Spiel-Verknüpfung. Sie ist nicht Teil der historischen `GameSession`; die Spielmodule definieren keine parallele Zuordnung.

Persistierte Vererbung wird nur für eine echte stabile is-a-Beziehung mit
polymorphem Verhalten eingesetzt. Value Objects werden für echte fachliche
Werteblöcke eingeführt, nicht als technische Pflicht.

## 7. Application und Persistenz

Application arbeitet bewusst direkt mit EF Core.

- Keine Repositories/DbContext-Interfaces nur zum Verstecken von EF.
- Interfaces nur für echte Polymorphie, Plattformgrenzen oder Modul-Erweiterung.
- Kurzlebiger DbContext pro Operation über `IDbContextFactory`.
- Ein DbContext wird nicht parallel verwendet und nicht als Singleton gehalten.
- Queries: `AsNoTracking` und gezielte Projektionen.
- Writes: Domainmethoden + `SaveChangesAsync`; explizite Transaktion bei
  zusammengehörigen Multi-Writes.
- Allgemeine EF-Konfiguration: `Application/Persistence`.
- Spielkonfiguration: `Application/Games/<GameName>/Persistence`.

Es gibt einen gemeinsamen `sealed partial BoardgameTrackerDbContext`. Spielmodule
ergänzen ihn über modul-eigene Partial-Dateien/Konfigurationen.

`DatabaseInitializer` bleibt allgemein und führt registrierte
`IDatabaseInitializationStep`-Implementierungen **sequenziell** aus; konkrete Spiele
registrieren ihre eigenen Schritte. Referenzdaten-Seeder ergänzen fehlende feste
Datensätze idempotent und setzen bereits vorhandenen veränderlichen Fachzustand
nicht auf Defaults zurück.

SQLite wird nur am App-/Host-Rand gewählt. Providerregistrierung und
SQLite-spezifische Konfiguration gehören nicht in Core oder allgemeine
Application-Fachlogik.

DB-/Build-/Migrationsregeln: [`SETUP.md`](../SETUP.md).

## 8. Modulregistrierung und Host-Integration

Jedes **integrierte Spielmodul** besitzt einen öffentlichen Registrierungseinstieg, bevorzugt
`Add<GameName>()`. `ManualGames` registriert **ein** Modul für beliebig viele lokal angelegte Spiele.

Allgemeiner Code arbeitet mit Spielmodulen nur über kleine fachliche Verträge.
Der allgemeine Vertrag liegt außerhalb eines konkreten Spielmoduls; die konkrete
Implementierung bleibt im jeweiligen `Games/<GameName>/`-Baum, z. B.:

- registrierte feste Identität und Seeding gemeinsamer `Game`-Metadaten bei integrierten Spielen; dynamische Auflösung des reservierten `manual:<GUID>`-Schlüsselraums durch `ManualGames`;
- Session-Navigation/Resume;
- optionale PlayerColor-Regeln über die gemeinsame Farb-/Default-Persistenz;
- optionale Spiel-Einstellungsnavigation;
- Initialisierungsschritte;
- Import-/Export-Schema und -Persistenz.

Spielrouten werden im Modul registriert. Allgemeine Shell-/History-/Gruppenfeatures
kennen keine konkreten Spielseiten.

Composition Root und Host-Projektdatei dürfen Module registrieren, paketieren und
modul-eigene Buildassets einbinden. Fachliche Spielentscheidungen gehören dort nicht
hin. Wächst spielabhängige Buildlogik, soll sie in eine modul-eigene importierte
Builddatei ausgelagert werden.

Ein neuer Erweiterungspunkt entsteht erst, wenn allgemeiner Code tatsächlich eine
austauschbare oder spielabhängige Implementierung benötigt.

## 9. Gemeinsame Querschnittskonzepte

### PlayerColor

`Core/Players/PlayerColor` ist der gemeinsame semantische Farbtyp für Spiele, die
Spielerfarben verwenden. Er enthält keine UI- oder Kamerawerte.

Ein Spiel definiert nur seine erlaubte Teilmenge und Validierungsregeln. Optionale
Gruppen-/Spieldefaults und historische Sessionfarben liegen in den allgemeinen
Farbzuordnungen; eigene redundante Farbtabellen in Spielmodulen entfallen. Benötigt
ein Spiel ein anderes Marker-Konzept, erhält dieses einen eigenen Fachbegriff.

Datenwerte: [DATA_MODEL.md](DATA_MODEL.md). Darstellung:
[UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md).

### Game Content

App-Lokalisierung und Spielmaterial sind getrennt.

- Built-in-Content liegt im Spielmodul.
- Identitäten/Keys bleiben sprachneutral.
- Built-in-Namen werden nicht allein für Lokalisierung in SQLite gespiegelt.
- Nutzerinhalt darf persistent sein.
- Materialsprache ist Preference pro `Game.Key`, kein Sessionfeld.
- Ein allgemeiner Preference-Port kennt keine konkreten Spieltypen.
- Für diese Preference wird keine eigene Content-Sprach-Entity eingeführt.
- Historie speichert IDs, keine lokalisierten Namenssnapshots.
- Eine generische Content-Engine entsteht erst aus mehreren realen Spielen.

Die verbindliche Content-Fallbackkette steht in
[PRODUCT_SPEC.md](PRODUCT_SPEC.md); Persistenzidentitäten in
[DATA_MODEL.md](DATA_MODEL.md).

### Navigation und lokale Settings

Allgemeine Navigation kennt keine konkreten Spielseiten. Spielziele werden über
registrierte Handler aufgelöst.

Shell-Parameter verwenden zentrale Konstanten in
`App/Navigation/NavigationParameters.cs`.

Navigation, Theme, die aktiv geschalteten Spiele und andere nicht-historische Preferences sind Hostzustand, keine
GameSession-Daten.

Unterstützte Spiele sind ohne ausdrückliche gespeicherte Abwahl standardmäßig sichtbar. Die Auswahl wird anhand der lokal stabilen `Game.Key`-Werte geführt, ohne GameKey-Switches in allgemeinen Features. Integrierte Spiele besitzen feste Keys; ManualGames lokal erzeugte Keys `manual:<GUID>`, die nicht zum Geräte-Matching verwendet werden. Die frühere Standardspiel-Preference entfällt.

Der Host löst Spieleinstellungen über den zuständigen Spieladapter auf; feste Schlüssel identifizieren integrierte Module, der reservierte manuelle Schlüsselraum das eine ManualGames-Modul. Konkrete Zielseiten bleiben im Modul.

Der Capsule-Tag ist `Game.BadgeText` und kein historisches Sessionfeld. Integrierte Werte stammen aus registrierter Modulmetadaten-Initialisierung, manuelle aus ihrer gespeicherten Spielkonfiguration.

### History-Transfer

Import/Export ist ein allgemeiner Application-Slice. Spielmodule liefern nur ihre
spielabhängigen Schema-/Persistenzteile. Native Picker, Speichern und Teilen sind
App-Adapter und enthalten keine Format-/Matchinglogik.

Details: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).

## 10. Rollen

- **Query:** reine Leseprojektion.
- **UseCase:** Application-Orchestrierung eines fachlichen Ablaufs.
- **Persistence:** konkrete zusammenhängende EF-Komponente oder Modul-Persistenzadapter.
- **Store:** Host-/Preferences-Grenze, kein EF-Wrapper.
- **Handler:** Dispatch-, Routing-, Navigations- oder Transportentscheidung.

`DataAccess` ist kein bevorzugter Standardname.

C#-/Async-/Naming-Regeln: [CODE_GUIDELINES.md](CODE_GUIDELINES.md).

## 11. Tests und Architekturschutz

Core-Tests bleiben ohne EF/SQLite. Application-Tests für relationale Regeln verwenden
echtes EF/SQLite mit isolierter Lebensdauer; EF InMemory ersetzt keine
SQL-/Constraint-/Transaktionsprüfung.

Architekturtests sollen schützen:

- erlaubte Schichtabhängigkeiten;
- Providergrenze;
- EF-freie ViewModels;
- `Games/<GameName>/`-Ownership;
- keine konkrete Spielabhängigkeit im allgemeinen Code;
- Unabhängigkeit der Spielmodule.

Tests spiegeln die fachliche Modul-/Slicestruktur.

## 12. Nicht gewünschte Muster

Ohne konkreten Bedarf nicht einführen:

- generische Repositories;
- MediatR nur als Dispatch-Schicht;
- `Common`-/`Utils`-Sammelordner;
- bedeutungslose Ein-Methoden-Interfaces;
- Wrapper-Services ohne eigene Semantik;
- Navigation-Service nur als Shell-Wrapper;
- generische Game Engine;
- technische Ordnerhierarchien, die ein Feature zerreißen;
- leere Modulordner;
- Compatibility-Aliase für alte interne Begriffe.

## 13. Bewusste technische Entscheidungen

SQLite kann bestimmte `DateTimeOffset`-Sortierungen nicht zuverlässig übersetzen.
Betroffene kleine lokale Listen dürfen deshalb nur benötigte Daten projizieren und
anschließend im Speicher sortieren. Eine Schemaänderung braucht einen realen Bedarf.

`AppShell` darf beim Bootstrap über `IServiceProvider` aufgelöst werden, wenn dies
für den Lebenszyklus nach `InitializeComponent`/Theme-Anwendung erforderlich ist.
Dafür wird keine zusätzliche Startup-Schicht eingeführt.

Drittanbieter-Lizenzinformationen werden offline paketiert; Regeln stehen in
[THIRD_PARTY_LICENSE_POLICY.md](THIRD_PARTY_LICENSE_POLICY.md).
