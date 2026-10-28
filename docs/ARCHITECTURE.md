# Boardgame Tracker – Architektur

## 1. Zielbild

Boardgame Tracker ist ein **modularer Monolith mit pragmatischen Vertical Slices**.
Die Architektur soll mehrere Brettspiele unterstützen, ohne eine generische Game
Engine zu bauen. Fachliche Regeln bleiben unabhängig von MAUI, EF Core und
SQLite; technische Abstraktionen werden nur eingeführt, wenn sie einen konkreten
Nutzen haben.

Erstes vollständig unterstütztes Spiel ist **Arche Nova** (`ArkNova`).

## 2. Projektgrenzen

```text
App -> Application -> Core
          ^
          |
Infrastructure -> Application + Core
```

- **Core** enthält Domain-Entities, fachliche Regeln und spielespezifische
  Domain-Erweiterungen.
- **Application** enthält Use Cases, Read Models und use-case-spezifische Ports.
- **Infrastructure** enthält EF Core, SQLite und konkrete Implementierungen der
  Application-Ports.
- **App** enthält MAUI, XAML, ViewModels, Navigation, Präsentationslogik und den
  Composition Root.

`App` darf `Infrastructure` nur im technischen Startup/Composition Root direkt
verwenden. ViewModels kennen keine konkrete SQLite- oder EF-Core-Implementierung.

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
  Groups/
  Games/
    ArkNova/
      GameSessions/
        Preparation/
        Running/
        Scoring/
        StartGameSession/
      Settings/

BoardgameTracker.Infrastructure/
  Persistence/
    SQLite/
      Configurations/       # allgemeine EF-Konfigurationen
      GameSessions/
      Groups/
  Games/
    ArkNova/
      Persistence/
        SQLite/
          Configurations/
          Seeding/

BoardgameTracker.App/
  Features/                 # allgemeine UI-Features
    Games/
    Groups/
    History/
    Settings/
  Games/
    ArkNova/
      Features/
        GameSessions/
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

Bevorzugt:

```text
Games/ArkNova/GameSessions/
  Preparation/
  Running/
  Scoring/
  StartGameSession/
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

- EF Core 10 + SQLite.
- Keine generischen Repository-Abstraktionen.
- Persistenzports sind use-case-spezifisch und gehören der Application-Schicht.
- Konkrete EF-/SQLite-Implementierungen liegen in Infrastructure.
- Allgemeine EF-Konfigurationen liegen unter
  `Persistence/SQLite/Configurations`.
- Spielspezifische EF-Konfigurationen liegen ausschließlich unter
  `Games/<GameName>/Persistence/SQLite/Configurations`.

### Aktuelle Pre-Alpha-Datenbankstrategie

Der Runtime-Start verwendet `Database.EnsureCreatedAsync()` und anschließend
idempotentes Referenzdaten-Seeding.

Schemaänderungen werden in der Pre-Alpha-Phase durch Löschen/Neuaufbau der
lokalen Datenbank übernommen. Ein kanonischer EF-Migrationsverlauf wird erst
wieder eingeführt, wenn veröffentlichte Nutzerdaten versionsübergreifend
migriert werden müssen. Historische Migrationen sind deshalb nicht Bestandteil
des aktuellen Quellmodells.

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

- Namespace-Hierarchien folgen der fachlichen Ordnerstruktur.
- Dateiname und primärer Typname stimmen überein.
- Tests spiegeln die fachliche Struktur des getesteten Produktionscodes.
- Bei Refactorings werden alte Synonyme nicht als Compatibility-Aliase im Domain
  Model weitergeführt.

## 13. Architekturregel für zukünftige Spiele

Ein neues Spiel wie Terraforming Mars wird zunächst als eigenes Modul angelegt:

```text
Games/TerraformingMars/
```

Nur wirklich allgemeine Konzepte werden in den gemeinsamen Kern verschoben.
Gemeinsamer Code entsteht durch nachgewiesene Gemeinsamkeit, nicht durch
vorsorgliche Abstraktion.

## Historien-Slice

Allgemeine Completed-Leseprojektion unter `Application/History`, SQLite-Adapter in Infrastructure, Listenoberfläche unter `App/Features/History`. Arche-Nova-Korrekturen gehören unter `Games/ArkNova/GameSessions/History`: eigener Use Case und Persistenzport für Completed, UI verwendet die vorhandenen Wertungs- und Gleichstandskomponenten. Ein ansichtsgebundener Entwurf wird atomar gespeichert; der Scoring-Use-Case behält seine Statusgrenze.

## Navigationsparameter

Alle Schlüssel für Shell-Navigationsparameter werden zentral als Konstanten in `src/BoardgameTracker.App/Navigation/NavigationParameters.cs` definiert, bevorzugt mit `nameof`. Sender (`ShellNavigationQueryParameters`) und Empfänger (`IQueryAttributable.ApplyQueryAttributes`, insbesondere `TryGetValue`) verwenden dieselbe Konstante, niemals separat ausgeschriebene String-Literale. Neue Parameter werden zuerst dort ergänzt. So können Schlüssel nicht unbemerkt nur auf einer Seite geändert werden.

## Zentrales Theme-System

App-Themes sind Präsentation und lokale Preferences, keine Domain-/SQLite-Daten. Das bestehende `AppNavigationSettings`-/Preferences-Port erhält eine Theme-Auswahl; fehlende oder ungültige Werte fallen auf Deep Forest zurück. `App/Theming/ThemePalette` definiert reine semantische Farbwerte, `ThemeManager` ersetzt die MAUI-Ressourcen beim Start und Wechsel. XAML-Farben verwenden `DynamicResource`, keine Theme-Namen in Screens. Der globale `ContentPage`-Style setzt `ApplyToDerivedTypes="True"`, damit die tatsächlichen Page-Unterklassen den dynamischen AppBackground erben; keine separaten Page-Farbpaletten. Kleine plattformspezifische Shell-Appearance-Tracker ergänzen die vorhandene native TabBar um Kontur und Auswahlfläche; keine eigene Navigation. Fachliche Farben und Grafiken bleiben in ihren Spielmodulen.

## Spielerfarben-Präsentation

`App/Presentation/PlayerColors` definiert die kleine gemeinsame UI-Palette mit gekoppelten Hintergrund-/Textwerten und separater Lokalisierung. `Games/ArkNova/ArkNovaPlayerColorMapping` ordnet die vier Domainfarben diesen UI-Kategorien zu; `ArkNovaPlayerColorPresentation` stellt MAUI-Farben und Namen bereit. Die Palette ist themeunabhängig und kein Erkennungskatalog. Das Domain-/Persistenzmodell und die erlaubten Arche-Nova-Farben bleiben unverändert.
