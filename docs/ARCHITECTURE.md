# Boardgame Tracker – Architektur

## 1. Zielbild

Boardgame Tracker ist ein **modularer Monolith mit pragmatischen Vertical Slices**.
Die Architektur unterstützt mehrere Brettspiele, ohne eine generische Game Engine
vorwegzunehmen.

Das Projekt ist bewusst ein Lernprojekt. Architektur, Ordnerstruktur und
Abhängigkeitsrichtungen sollen reale fachliche Grenzen sichtbar und nachvollziehbar
machen. Verständlichkeit hat Vorrang vor cleveren Abstraktionen. Lernorientierung
rechtfertigt keine zusätzlichen Schichten, Interfaces oder Generalisierungen.

Die zentrale Regel lautet:

> Allgemeiner Code kennt keine konkreten Spiele. Ein Spiel lebt vollständig in seinem
> eigenen Modul und verwendet den gemeinsamen Kern nur über allgemeine Verträge.

## 2. Schichten und Abhängigkeiten

```text
App -> Application -> Core
Application -> EF Core
App -> EF Core SQLite
```

- **Core** enthält Domain-Entities, Value Objects und fachliche Regeln. Core kennt
  weder MAUI noch EF Core noch SQLite.
- **Application** enthält konkrete Use Cases, Queries, Read Models, den gemeinsamen
  DbContext, relationale EF-Konfigurationen, Initialisierung und Seeding.
  EF Core ist eine bewusste Application-Abhängigkeit; SQLite-spezifische APIs sind
  dort verboten.
- **App** enthält MAUI, XAML, ViewModels, Navigation, Präsentationslogik,
  Plattformadapter, den Composition Root und die SQLite-Providerkonfiguration.

ViewModels und Pages erhalten konkrete Use Cases oder Queries, niemals einen DbContext.

Der typische Ablauf bleibt sichtbar:

```text
ViewModel-Command -> konkreter UseCase/Query -> DbContext -> Domain -> SaveChangesAsync
```

## 3. Spielmodule als harte Architekturgrenze

Spielespezifischer Produktionscode liegt auf **allen drei Ebenen** unter
`Games/<GameName>/`:

```text
src/
  BoardgameTracker.Core/
    Games/
      <GameName>/

  BoardgameTracker.Application/
    Games/
      <GameName>/

  BoardgameTracker.App/
    Games/
      <GameName>/
```

Spielespezifische Tests spiegeln dieselbe fachliche Struktur in den Testprojekten.
Spielespezifische Entwicklungswerkzeuge liegen unter `tools/Games/<GameName>/`.

Diese Pfade sind Teil der Architektur, nicht nur eine Ablagekonvention. Ein Entwickler
soll am Pfad und Namespace erkennen können, ob ein Typ allgemein oder
spielespezifisch ist.

### Ownership-Regeln

- Dateien, Typdeklarationen und Namespaces eines konkreten Spiels gehören in dessen
  `Games/<GameName>/`-Baum.
- Allgemeiner Code außerhalb dieses Baums darf nicht von konkreten Spieltypen oder
  konkreten Spiel-Namespaces abhängen.
- Ein Spielmodul darf allgemeinen Code verwenden.
- Ein Spielmodul darf nicht von einem anderen Spielmodul abhängen.
- Allgemeine Features enthalten keine `GameKey`-Switches oder andere
  Fallunterscheidungen nach konkreten Spielen.
- Ein konkretes Spiel implementiert allgemeine Erweiterungspunkte innerhalb seines
  Moduls.
- Der Composition Root bindet ein Spiel über dessen öffentliche Modulregistrierung an;
  konkrete Featuretypen, Pages, Persistenzimplementierungen und Routen bleiben im Modul.
- Leere Spiegelordner werden nicht nur aus Symmetriegründen angelegt.

Neue Spiele beginnen als eigenes Modul. Code wird erst in den gemeinsamen Bereich
verschoben, wenn eine reale, fachlich nachgewiesene Gemeinsamkeit existiert.
Keine vorsorgliche Extraktion für hypothetische zukünftige Spiele.

## 4. Dokumentationsarchitektur

Allgemeine Architekturentscheidungen stehen ausschließlich in dieser Datei.
Spielspezifische Architekturentscheidungen stehen unter:

```text
docs/Games/<GameName>/ARCHITECTURE.md
```

Eine Spielarchitektur dokumentiert **nur zusätzliche konkrete Entscheidungen des
Spiels**. Sie wiederholt keine allgemeinen Modul-, Composition-, Farb-, Content-,
Persistenz- oder Navigationsregeln nur deshalb, weil das Spiel sie aktuell als erstes
implementiert.

Weitere vollständig spielspezifische Dokumente liegen im selben Spielordner, zum
Beispiel:

```text
docs/Games/<GameName>/
  ARCHITECTURE.md
  CV_STATUS.md
  ...
```

Bei Arbeiten an einem konkreten Spiel werden zuerst diese allgemeine Architektur und
danach dessen Spielarchitektur gelesen. Weitere Spezialdokumente werden nur geladen,
wenn ihr Thema tatsächlich betroffen ist.

Gemischte allgemeine Dokumente werden bei ihrer jeweiligen Konsolidierung getrennt,
wenn klar abgrenzbare spielspezifische Abschnitte entstehen. Jede dauerhafte Regel
hat genau eine kanonische Quelle.

## 5. Verbindliches Domänenvokabular

Für die zentralen allgemeinen Konzepte gelten diese Namen:

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

Konkrete Entityfelder, Invarianten und Persistenzidentitäten stehen in
[DATA_MODEL.md](DATA_MODEL.md).

## 6. Vertical Slices

Application- und UI-Code wird primär nach **Use Case/Feature** organisiert, nicht
nach technischen Klassentypen.

Kleine Slices bleiben flach im fachlichen Bereich. Kleine ausschließlich
slice-eigene Commands, Inputs, Read Models, Results und Optionen stehen
standardmäßig beim Haupttyp.

Eine eigene Datei oder ein eigener Unterordner ist sinnvoll, wenn ein Typ:

- sliceübergreifend wiederverwendet wird,
- einen eigenständigen Fachbegriff oder Vertrag darstellt,
- relevante eigene Logik besitzt,
- einen unabhängigen Änderungsgrund hat,
- oder der Haupttyp sonst deutlich unübersichtlicher würde.

Nicht als Standardhierarchie:

```text
Commands/
Queries/
Services/
Validators/
DTOs/
```

Ordner und Dateien folgen der tatsächlichen fachlichen Komplexität, nicht einer
vorgegebenen Symmetrie.

## 7. Domainmodellierung und spielabhängige Ergänzungen

Entities besitzen fachliche Identität. Aggregate schützen ihre eigenen
Invarianten. Cross-Aggregate-Beziehungen verwenden bevorzugt IDs; öffentliche
Navigation Properties werden nicht nur für EF-Bequemlichkeit eingeführt.

### Composition statt Duplikation

Spielspezifische Daten, die ein allgemeines Modell **ergänzen**, werden über
Composition modelliert. Das Spiel dupliziert keine allgemeinen Felder.

Für Partieteilnehmer gilt grundsätzlich:

```text
GameSessionPlayer
        1
        |
        0..1
        |
<GameName>GameSessionPlayer
```

`<GameName>GameSessionPlayer` verwendet denselben fachlichen Teilnehmerbezug und
enthält ausschließlich spielespezifische Daten. Allgemeine Felder wie Reihenfolge,
Platzierung oder Player-Identität werden nicht erneut gespeichert.

Dasselbe Prinzip gilt für Gruppenmitglieder:

```text
GroupPlayer
        1
        |
        0..1
        |
<GameName>GroupPlayer
```

Ein Spiel darf daneben eigene Entities besitzen, wenn sie wirklich nur zu diesem
Spiel gehören.

Persistierte EF-Entity-Vererbung wird nicht allein verwendet, um Properties zu
sparen. Vererbung bleibt erlaubt, wenn fachlich eine stabile is-a-Beziehung und
echtes polymorphes Verhalten bestehen.

Value Objects werden eingeführt, wenn ein eigenständiger fachlicher Werteblock ohne
Identität dadurch verständlicher wird; nicht als technischer Selbstzweck.

Konkrete Aggregate, Felder und Invarianten: [DATA_MODEL.md](DATA_MODEL.md).

## 8. Application, EF Core und Persistenz

Application verwendet EF Core und das relationale Modell direkt.

- Keine Repositories, Stores oder DbContext-Interfaces nur zum Verstecken von EF Core.
- Interfaces nur für echte Polymorphie, Host-/Plattformgrenzen und
  Modul-Erweiterungspunkte.
- Ein Slice erzeugt über `IDbContextFactory` einen kurzlebigen Context pro Operation.
- Kein Singleton hält einen erzeugten Context; ein Context wird nicht parallel verwendet.
- Queries verwenden `AsNoTracking` und gezielte Read-Model-Projektionen.
- Schreibaktionen verwenden Domainmethoden und `SaveChangesAsync` am fachlichen Ende.
- Explizite Transaktionen schützen zusammengehörige Multi-Write-Aktionen.
- Allgemeine EF-Konfigurationen liegen unter `Application/Persistence`.
- Spielkonfigurationen liegen unter
  `Application/Games/<GameName>/Persistence`.

Es gibt genau einen gemeinsamen `sealed partial BoardgameTrackerDbContext`.
Der allgemeine Teil besitzt das allgemeine relationale Modell. Spielmodule ergänzen
denselben Context über ihre eigenen Partial-Dateien und Konfigurationen innerhalb
des jeweiligen Spielmoduls.

`DatabaseInitializer` bleibt allgemein. Er erstellt den Context, führt die
allgemeine Datenbankinitialisierung aus und verarbeitet registrierte
`IDatabaseInitializationStep`-Implementierungen in Reihenfolge. Spielmodule
registrieren ihre eigenen Schritte; der allgemeine Initialisierer kennt keine
konkreten Spiele.

SQLite wird nur am Host-Rand der App gewählt. Providerregistrierung,
Connection-Interceptor und SQLite-spezifische Konfiguration gehören in die App,
nicht in Core oder in allgemeine Application-Fachlogik.

DB-Lifecycle, Pre-Alpha-Strategie, Build- und Migrationsregeln stehen in
[`SETUP.md`](../SETUP.md).

## 9. Modulregistrierung und allgemeine Erweiterungspunkte

Jedes Spiel besitzt einen klaren öffentlichen Einstieg für die Hostregistrierung,
bevorzugt als `Add<GameName>()` im App-Modul. Dort werden die konkreten
Application-/UI-Komponenten und die benötigten allgemeinen Erweiterungspunkte des
Spiels registriert.

Gemeinsamer Code arbeitet mit Spielen nur über kleine fachliche Erweiterungspunkte,
zum Beispiel:

- Session-Navigation und Resume,
- spielabhängige Regeln wie erlaubte Spielerfarben,
- Persistenzadapter für spielabhängige Gruppendefaults,
- Import-/Export-Schema und -Persistenz,
- Initialisierungsschritte.

Der allgemeine Vertrag liegt außerhalb eines konkreten Spielmoduls. Die konkrete
Implementierung bleibt im jeweiligen `Games/<GameName>/`-Baum.

Spielrouten werden innerhalb des Spielmoduls registriert. Shell und allgemeine
Navigation kennen keine konkreten Spielseiten.

Ein Erweiterungspunkt wird nur eingeführt, wenn allgemeiner Code tatsächlich mit
einer austauschbaren oder spielabhängigen Implementierung arbeiten muss. Ein
Interface ist kein Standardwrapper um eine einzelne EF-Klasse.

## 10. Gemeinsame Spielerfarben

`Core/Players/PlayerColor` ist der gemeinsame semantische Farbtyp für Spiele, die
Spielerfarben verwenden. Er enthält keine UI-Hexwerte und keine
Erkennungs-/Kameraparameter.

Ein Spiel definiert ausschließlich die für dieses Spiel erlaubte Teilmenge der
gemeinsamen Farben. Es führt kein eigenes Farbenum ein, wenn dieselben semantischen
Farben gemeint sind.

Allgemeine Gruppen- und UI-Features kennen nur den gemeinsamen Farbtyp und einen
allgemeinen Regelkatalog. Das Spielmodul liefert:

- die erlaubte Teilmenge,
- gegebenenfalls spielabhängige Default-Persistenz,
- die Validierung seiner eigenen Domainobjekte.

Die gemeinsame Presentation ordnet `PlayerColor` einem Anzeigenamen und
Darstellungswerten zu. Fachliche Farben und UI-Farben bleiben getrennt.

Benötigt ein zukünftiges Spiel ein grundsätzlich anderes Marker- oder
Identitätskonzept statt der gemeinsamen Spielerfarben, wird dafür ein eigener
Fachbegriff modelliert, statt `PlayerColor` zweckzuentfremden.

## 11. Game Content und Lokalisierung

App-Lokalisierung und physisches Spielmaterial sind getrennte Bereiche:

```text
App Localization -> AppResources/.resx / CurrentUICulture
Game Content      -> spielmodulspezifische Built-in-Inhalte + Nutzerinhalte
```

Allgemeine Regeln:

- Built-in-Content liegt im jeweiligen Spielmodul.
- Domainidentitäten und Content-Keys bleiben sprachneutral und stabil.
- Built-in-Content wird nicht allein für Lokalisierung in SQLite gespiegelt.
- Veränderliche Nutzerinhalte werden persistent gespeichert.
- Die Spielmaterial-Sprache ist eine lokale Preference pro stabilem `Game.Key`,
  kein historisches Sessionfeld.
- Ein allgemeiner Preference-Port kennt keine konkreten Spieltypen.
- Auflösung erfolgt in der Reihenfolge:
  Nutzerinhalt -> ausgewählte Materialsprache -> unterstützte App-Sprache ->
  Spiel-Default -> deterministischer Missing-Marker.
- Historische Partien speichern stabile IDs, keine lokalisierten Namenssnapshots.
- Eine generische Game-Content-Engine entsteht erst, wenn mehrere reale Spiele eine
  gemeinsame Extraktion rechtfertigen.

Konkrete Content-Dateien, Objektidentitäten und Nutzerressourcen gehören in die
jeweilige Spielarchitektur bzw. das Datenmodell.

## 12. Navigation, Settings und Host-Grenzen

Die Hauptnavigation und allgemeine Features bleiben spielunabhängig.
Spielabhängige Zielseiten werden über registrierte Modulhandler aufgelöst; allgemeine
History-, Gruppen- oder Player-Features kennen keine konkreten Spielseiten.

Shell-Navigationsparameter verwenden zentrale Konstanten in
`App/Navigation/NavigationParameters.cs`; Sender und Empfänger verwenden denselben
Schlüssel.

Nicht-historische Navigation, Darstellung und lokale Preferences sind Hostzustand und
werden nicht in SQLite modelliert. Spielabhängige lokale Preferences werden über
allgemeine Hostgrenzen und einen stabilen `Game.Key` adressiert.

Theme-, XAML-, Binding-, Dialog- und weitere Presentationregeln stehen in
[UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md) und [UI_WIREFRAME.md](UI_WIREFRAME.md).

## 13. Lokaler History-Transfer

Import und Export historischer Partien bilden einen allgemeinen Application-Slice.
Das versionierte Austauschformat kennt keinen konkreten Transport.

Spielmodule erweitern den Transfer über allgemeine Schema-/Persistenzgrenzen.
Die konkrete Spielimplementierung bleibt in
`Application/Games/<GameName>/`.

Dateispeichern, Teilen und native Picker sind App-seitige Ausgabe- bzw.
Eingabeadapter und enthalten keine eigenen Format- oder Matchingregeln.

Format, Matching, Validierung, Snapshotprüfung und Commitregeln stehen ausschließlich
in [IMPORT_EXPORT.md](IMPORT_EXPORT.md).

## 14. Fachliche Rollen

- `Query`: reine Leseprojektion ohne Mutation.
- `UseCase`: Application-Orchestrierung eines fachlichen Ablaufs.
- `Persistence`: konkrete zusammenhängende EF-Komponente oder
  Modul-Erweiterungsimplementierung, insbesondere für Arbeiten auf demselben Context.
- `Store`: Host-/Preferences-Grenze; kein EF-Wrapper.
- `Handler`: Dispatch-, Routing-, Navigations- oder Transportentscheidung.

`DataAccess` ist kein bevorzugter Standardname.

Projektweite C#-, Async-, Benennungs- und Kommentarregeln:
[CODE_GUIDELINES.md](CODE_GUIDELINES.md).

## 15. Tests und Architekturschutz

Core-Tests bleiben reine Unit-Tests ohne EF oder SQLite.

Application-Slice-Tests verwenden für relationale Regeln echte EF-/SQLite-
Datenbanken mit isolierter Lebensdauer und Cleanup; EF InMemory ersetzt keine
Constraints, SQL-Übersetzung oder Transaktionen.

Architekturtests schützen insbesondere:

- die Abhängigkeitsrichtung `App -> Application -> Core`,
- die Providergrenze,
- EF-freie ViewModels,
- die harte `Games/<GameName>/`-Ownership,
- das Verbot konkreter Spielabhängigkeiten im allgemeinen Code,
- die Unabhängigkeit verschiedener Spielmodule.

Tests spiegeln die fachliche Slice- und Modulstruktur.

## 16. Nicht gewünschte Muster

Ohne konkreten Bedarf werden nicht eingeführt:

- generische Repositories,
- MediatR nur als Dispatch-Schicht,
- `Common`-/`Utils`-Sammelordner,
- bedeutungslose Ein-Methoden-Interfaces,
- allgemeine Services nur zum Wrappen vorhandener APIs,
- ein allgemeiner Navigation-Service nur zum Wrappen von Shell,
- eine generische Game Engine,
- technische Ordnerhierarchien, die zusammengehörige Features auseinanderziehen,
- leere Spielmodulordner nur für Symmetrie,
- Compatibility-Aliase für alte interne Domänensynonyme.

## 17. Bewusste allgemeine technische Entscheidungen

SQLite kann bestimmte `DateTimeOffset`-Sortierungen nicht zuverlässig übersetzen.
Betroffene lokale Listen projizieren deshalb nur benötigte Metadaten und sortieren
für die derzeit kleine lokale Datenmenge anschließend im Speicher. Eine andere
Timestampdarstellung oder Schemaänderung braucht einen konkreten Bedarf.

`AppShell` darf im Bootstrap über `IServiceProvider` aufgelöst werden, wenn seine
Erzeugung erst nach `InitializeComponent` und der Theme-Anwendung erfolgen muss.
Eine zusätzliche Startup-Serviceschicht wird nicht allein eingeführt, um diesen
Lebenszyklus zu verstecken.

Drittanbieter-Lizenzinformationen werden offline aus paketierten Ressourcen
bereitgestellt. Wartungs- und Lizenzregeln stehen ausschließlich in
[THIRD_PARTY_LICENSE_POLICY.md](THIRD_PARTY_LICENSE_POLICY.md).
