# Boardgame Tracker -- Architektur v0.3

## 1. Ziel

Dieses Dokument beschreibt die technische Architektur des Boardgame
Trackers.

Die Architektur soll:

-   für eine mobile App auf Android und iOS geeignet sein,
-   für einen einzelnen Entwickler verständlich und wartbar bleiben,
-   den Einstieg aus einer WPF-/C#-Welt erleichtern,
-   eine lokale/offline Nutzung ermöglichen,
-   später weitere Brettspiele unterstützen,
-   spielspezifische Logik sauber vom allgemeinen App-Kern trennen,
-   unnötige Enterprise-Komplexität vermeiden.

Erstes vollständig unterstütztes Spiel ist **Arche Nova**.

------------------------------------------------------------------------

## 2. Technologiestack

### App-Framework

-   **.NET MAUI**
-   **C#**
-   **XAML**

Zielplattformen:

-   Android
-   iOS

Die Entwicklung erfolgt zunächst primär unter Windows.

Für iOS wird später ein vorhandenes MacBook als Build-Host verwendet.

### UI-Architektur

-   **MVVM**
-   **CommunityToolkit.Mvvm**

Das Toolkit soll insbesondere verwendet werden für:

-   `ObservableObject`
-   Properties mit Change Notification
-   Commands
-   asynchrone Commands
-   Reduktion von MVVM-Boilerplate

### Persistenz

-   **SQLite**
-   **Entity Framework Core 10**
-   **Microsoft.EntityFrameworkCore.Sqlite**
-   **EF Core Migrations**
-   lokale Speicherung auf dem Gerät
-   zunächst kein Backend
-   zunächst keine Benutzerkonten
-   zunächst keine Cloud-Synchronisierung

Die App folgt damit einem **Local-First-Ansatz**.

Das relationale Mapping wird bevorzugt über
`IEntityTypeConfiguration<T>` im Infrastructure-Projekt konfiguriert.
Domainklassen sollen nicht unnötig mit Persistenzattributen gekoppelt
werden.

### Plattformfunktionen

Über .NET MAUI bzw. abstrahierte Services:

-   Kamera
-   Fotoauswahl
-   lokale Dateien
-   Gerätespeicher
-   spätere Bildanalyse

------------------------------------------------------------------------

## 2.1 C#-Sprachregeln und Null-Sicherheit

Alle Projekte verwenden Nullable Reference Types:

``` xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

Nullability besitzt fachliche Bedeutung:

-   `string` / Referenztyp ohne `?`: Wert muss vorhanden sein.
-   `string?` / Referenztyp mit `?`: Wert darf fachlich fehlen.
-   Nullable Value Types wie `int?` werden für noch nicht vorhandene
    Ergebnisse verwendet.

Nullable-Warnungen werden nicht pauschal mit dem Null-Forgiving-Operator
`!` unterdrückt.

Moderne C#-Möglichkeiten werden gezielt genutzt:

-   `required` für bei der Initialisierung verpflichtende Werte
-   `init` für nach der Initialisierung unveränderliche Eigenschaften
-   `record` bzw. `record struct` für geeignete unveränderliche
    Value-/Result-Typen
-   aktuelle Sprachfeatures dort, wo sie Lesbarkeit und Typsicherheit
    verbessern

Moderne Sprachfeatures werden nicht allein um ihrer Neuheit willen
eingesetzt.

## 3. Architekturprinzip

Die App verwendet eine pragmatische Kombination aus:

1.  **Vertical-Slice-/Feature-Organisation**
2.  **gemeinsamem Domain-Kern**
3.  **spielespezifischen Modulen**
4.  **MVVM für die Benutzeroberfläche**
5.  **Infrastructure-Schicht für technische Details**

Vertical Slice wird dabei nicht dogmatisch umgesetzt.

Ziel ist:

> Code nach Benutzerfunktion und fachlicher Verantwortung organisieren,
> nicht ausschließlich nach technischer Dateisorte.

Es wird bewusst **kein vollständiges CQRS-/MediatR-Modell** eingeführt.

------------------------------------------------------------------------

## 4. Solution-Struktur

Vorgesehene Struktur:

``` text
BoardgameTracker.sln

src/
  BoardgameTracker.App/
  BoardgameTracker.Core/
  BoardgameTracker.Infrastructure/
```

### BoardgameTracker.App

Enthält:

-   .NET-MAUI-Anwendung
-   XAML Pages
-   ViewModels
-   Navigation
-   UI-nahe Services
-   plattformspezifische Integration, sofern nicht sinnvoll
    abstrahierbar

### BoardgameTracker.Core

Enthält:

-   fachliches Domainmodell
-   spielunabhängige Logik
-   spielspezifische Domainlogik
-   fachliche Services
-   Abstraktionen/Interfaces, wenn sie von Core benötigt werden

Core soll möglichst unabhängig von MAUI, SQLite und konkreten
Plattform-APIs bleiben.

### BoardgameTracker.Infrastructure

Enthält technische Implementierungen:

-   SQLite / EF Core
-   Datenbankinitialisierung und Migrationen
-   gezielte persistenznahe Stores/Services für konkrete Anwendungsfälle
-   Kamera-/Medienimplementierungen
-   spätere Bildanalyse
-   weitere technische Adapter

Ein generisches Repository pro Entity wird bewusst nicht eingeführt. Für einen
Slice darf ein kleiner, zielgerichteter Persistenz-Service wie `GroupStore` die
benötigten Queries und Schreiboperationen bündeln.

------------------------------------------------------------------------

## 5. Abhängigkeitsrichtung

Bevorzugte Richtung:

``` text
BoardgameTracker.App ───────► BoardgameTracker.Core
        │                            ▲
        └────► BoardgameTracker.Infrastructure ─────┘
```

Praktisch bedeutet dies:

-   `Core` kennt MAUI, EF Core und SQLite nicht.
-   `Infrastructure` kennt `Core` und persistiert dessen fachliche Typen.
-   `App` darf für pragmatische Vertical Slices gezielte Infrastructure-Services
    konsumieren; daraus wird keine künstliche zusätzliche Application-Schicht
    nur um der Schichtentrennung willen erzeugt.
-   konkrete Implementierungen werden über Dependency Injection registriert.

Wo eine zusätzliche Abstraktion keinen praktischen Nutzen bringt, wird
sie nicht künstlich eingeführt.

------------------------------------------------------------------------

## 6. Feature-/Vertical-Slice-Struktur

UI-Code wird nach Features organisiert.

Beispiel:

``` text
BoardgameTracker.App/
  Features/

    Groups/
      GroupListPage.xaml
      GroupListPage.xaml.cs
      GroupListViewModel.cs

    GameSession/
      Preparation/
        PreparationPage.xaml
        PreparationPage.xaml.cs
        PreparationViewModel.cs

      Running/
        RunningGamePage.xaml
        RunningGamePage.xaml.cs
        RunningGameViewModel.cs

      Scoring/
        ScoringPage.xaml
        ScoringPage.xaml.cs
        ScoringViewModel.cs

      Result/
        ResultPage.xaml
        ResultPage.xaml.cs
        ResultViewModel.cs

    History/
      HistoryPage.xaml
      HistoryPage.xaml.cs
      HistoryViewModel.cs

    Statistics/
      StatisticsPage.xaml
      StatisticsPage.xaml.cs
      StatisticsViewModel.cs

    Settings/
      ...
```

### Warum diese Struktur?

Ein Benutzerflow wie **Partie vorbereiten** besteht fachlich aus
zusammengehörigen Funktionen:

``` text
Gruppe laden
    ↓
Spieler laden
    ↓
Default-Farben übernehmen
    ↓
Default-Reihenfolge übernehmen
    ↓
Zoopläne auslosen
    ↓
Zooplan auswählen
    ↓
Startspieler auslosen
    ↓
Partie starten
```

Die dazugehörigen UI-Komponenten sollen räumlich zusammenliegen.

------------------------------------------------------------------------

## 7. Featureorientierter Core

Auch der Core wird nach fachlicher Verantwortung organisiert, nicht nach
technischen Klassentypen wie `Domain`, `Rules`, `Services` oder `Helpers`.

Aktuelle Struktur:

``` text
BoardgameTracker.Core/
  People/
    Person.cs

  Groups/
    Group.cs
    GroupPerson.cs

  GameSessions/
    Game.cs
    GameSession.cs
    GameSessionPlayer.cs
    GameSessionStatus.cs
    StartingPlayerRotation.cs

  Games/
    ArkNova/
      ArkNovaGroupPlayer.cs
      ArkNovaPlayerColor.cs
      ArkNovaSessionPlayer.cs
      ArkNovaZooPlan.cs
```

Ein zusätzlicher Ordner `General` wird bewusst vermieden. Die Ordnernamen
beschreiben stattdessen fachliche Bereiche. Unterordner wie `Rules` werden erst
eingeführt, wenn ein konkreter Bereich so groß wird, dass sie die Orientierung
wirklich verbessern.

`StartingPlayerRotation` liegt unter `GameSessions`, weil die zyklische
Startreihenfolge eine allgemeine Partieregel und keine Arche-Nova-Regel ist.

------------------------------------------------------------------------

## 8. Spielspezifische Module

Spielspezifische Fachlogik wird nicht in generische
Schlüssel-/Wert-Strukturen gepresst.

Stattdessen bekommt jedes unterstützte Spiel einen eigenen stark
typisierten Bereich.

Beispiel:

``` text
BoardgameTracker.Core/
  Games/
    ArkNova/
      ArkNovaGameSessionPlayer.cs
      ArkNovaZooPlan.cs
      ArkNovaSpecialCard.cs
      ArkNovaScoring.cs
      ArkNovaRules.cs
```

Später beispielsweise:

``` text
Games/
  ArkNova/
  TerraformingMars/
```

### Grundsatz

Der allgemeine Kern kennt:

-   Personen
-   Gruppen
-   Partien
-   Teilnehmer
-   Reihenfolge
-   Platzierung

Arche Nova ergänzt:

-   Spielerfarbe
-   Zooplanoptionen
-   gewählten Zooplan
-   Attraktivität
-   Artenschutz
-   Ruf
-   Spezialkarten
-   Arche-Nova-spezifische Punkteberechnung

------------------------------------------------------------------------

## 9. MVVM-Verantwortlichkeiten

### View

Die View ist hauptsächlich für Darstellung und UI-Bindings zuständig.

Beispiele:

-   Layout
-   Buttons
-   Eingabefelder
-   visuelle Zustände
-   Animationen

Geschäftslogik soll nicht in XAML-Code-Behind landen.

### ViewModel

Das ViewModel enthält den UI-Zustand und Benutzeraktionen.

Beispiel `PreparationViewModel`:

``` text
Players
IsReadyToStart
DrawZooPlansCommand
SelectZooPlanCommand
RotatePlayerOrderCommand
RandomizeStartingPlayerCommand
StartGameCommand
```

### Domain / Application Logic

Fachliche Regeln sollen nicht ausschließlich im ViewModel implementiert
werden.

Beispiele:

-   ein Zooplan darf pro Partie nur einmal angeboten werden
-   jeder Spieler erhält genau zwei Zooplanoptionen
-   mehrere Spieler dürfen bei echtem Gleichstand Platz 1 haben
-   Startspieler-Auslosung erhält die relative Sitzreihenfolge

Solche Regeln gehören in wiederverwendbare Domain- oder fachliche
Service-Logik.

------------------------------------------------------------------------

## 10. Startspieler und Sitzreihenfolge

Die Gruppe besitzt eine gespeicherte Default-Sitzreihenfolge.

Diese wird beim Erstellen einer neuen Partie übernommen.

### Rotation

Beispiel:

``` text
Anna
Ben
Chris
David
```

Eine Rotation ergibt:

``` text
Ben
Chris
David
Anna
```

### Zufälliger Startspieler

Die Auslosung wählt einen zufälligen Spieler und rotiert die bestehende
Reihenfolge so, dass dieser auf Position 1 landet.

Die relative Reihenfolge aller Spieler bleibt erhalten.

Beispiel:

``` text
Vorher:
Anna
Ben
Chris
David

Chris wird ausgelost.

Danach:
Chris
David
Anna
Ben
```

Die UI zeigt lediglich eine kurze dezente Animation, beispielsweise ein
einmaliges Aufglühen der neuen Startspieler-Kachel.

------------------------------------------------------------------------

## 11. Persistenz

SQLite speichert den fachlichen Zustand lokal.

### Ziel

Die Persistenz soll:

-   einfach sein,
-   offline funktionieren,
-   App-Neustarts überstehen,
-   offene Partien wiederherstellen,
-   historische Daten für Statistiken bereitstellen.

### Persistierte Kernbereiche

-   Personen
-   Gruppen
-   Gruppenzuordnungen
-   Default-Sitzreihenfolge
-   Spiele
-   Partien
-   Partie-Spieler
-   Arche-Nova-spezifische Partiedaten
-   Spezialkartenzuordnungen
-   App-/Arche-Nova-Einstellungen

### Nicht redundant speichern

Berechnete Werte sollen nach Möglichkeit aus Rohdaten erzeugt werden.

Beispiele:

-   Dauer
-   Gewinner
-   Gewinnquote
-   durchschnittliche Platzierung
-   alte Punkte
-   neue Punkte

------------------------------------------------------------------------


## 11.1 Erster implementierter Persistenz-Slice: Gruppen

Der erste echte Vertical Slice ist:

``` text
Gruppenübersicht
    ↓
Gruppe anlegen
    ├─ vorhandene Personen auswählen
    ├─ neue Person / Gast anlegen
    ├─ Standard-Reihenfolge festlegen
    └─ speichern
    ↓
SQLite
    ↓
Gruppenübersicht neu laden
```

Die UI liegt unter `App/Features/Groups`. `GroupListViewModel` und
`CreateGroupViewModel` kennen keinen `DbContext` direkt. Der gezielte
`GroupStore` unter `Infrastructure/Persistence/SQLite/Groups` kapselt genau die
Queries und Schreiboperationen, die dieser Slice benötigt.

Für reine Leseabfragen wird `AsNoTracking()` eingesetzt. Benötigte
Navigationen werden explizit geladen; Lazy Loading ist nicht aktiviert.

Die bestehende EF-Core-Migration bleibt unverändert, da die Umorganisation des
Core nur Namespaces/Dateipfade betrifft und der Gruppen-Slice das Datenmodell
nicht verändert.

## 12. Datenzugriff, EF Core und Services

EF Core ist die primäre Abstraktion für den relationalen Datenzugriff.

Zentrale Bestandteile:

``` text
Infrastructure/
  Persistence/
    SQLite/
      BoardgameTrackerDbContext.cs
      Configurations/
        PersonConfiguration.cs
        GroupConfiguration.cs
        GameSessionConfiguration.cs
        GameSessionPlayerConfiguration.cs
        ArkNova/
          ...
```

Das Mapping erfolgt bevorzugt über Fluent Configuration mit
`IEntityTypeConfiguration<T>`.

Beispiel:

``` csharp
internal sealed class PersonConfiguration
    : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}
```

### Keine Repository-Schicht aus Prinzip

Es wird nicht automatisch für jede Entity ein eigenes Repository
angelegt.

Insbesondere wird eine Struktur wie die folgende vermieden, wenn sie nur
EF Core ohne zusätzlichen fachlichen Nutzen umhüllt:

``` text
IPersonRepository
IGroupRepository
IGameRepository
IZooPlanRepository
...
```

Gezielte Repositories oder Query-/Persistence-Services können eingeführt
werden, wenn sie:

-   komplexe Persistenzoperationen kapseln,
-   eine sinnvolle fachliche Grenze darstellen,
-   Testbarkeit oder Wiederverwendung konkret verbessern.

### Fachliche Services

Für komplexere oder wiederverwendbare Regeln können kleine Services
verwendet werden.

Beispiele:

``` text
ArkNovaZooPlanDrawService
ArkNovaScoringService
StartingPlayerService
```

Ein Service soll nur dann entstehen, wenn er echte fachliche
Verantwortung besitzt.

### Schemaänderungen

Datenbankschemaänderungen werden über **EF Core Migrations**
versioniert.

Dadurch sollen bestehende lokale Datenbanken bei zukünftigen
App-Versionen aktualisiert werden können, ohne Nutzerdaten neu anlegen
zu müssen.

## 13. Dependency Injection

Die eingebaute .NET-/MAUI-Dependency-Injection wird verwendet.

Beispielhafte Registrierung:

``` text
EF Core / Persistence-Services
        ↓
Domain/Fachservices
        ↓
ViewModels
        ↓
Pages
```

Service Locator und globale statische Zustände sollen vermieden werden.

------------------------------------------------------------------------

## 14. Navigation

Die Navigation soll auf MAUI-Mitteln aufbauen.

Voraussichtliche Hauptbereiche:

``` text
Gruppen
Historie
Statistik
Optionen
```

Partieflow:

``` text
Gruppen
  ↓
Partie vorbereiten
  ↓
Laufende Partie
  ↓
Wertung
  ↓
Ergebnis
```

Eine laufende oder noch nicht abgeschlossene Wertung besitzt Vorrang
beim App-Start.

------------------------------------------------------------------------

## 15. Fehler- und Zustandsbehandlung

ViewModels sollen explizite Zustände besitzen, beispielsweise:

``` text
IsBusy
ErrorMessage
HasError
```

Asynchrone Aktionen sollen nach Möglichkeit über `AsyncRelayCommand`
ausgeführt werden.

Fehler aus Infrastruktur oder Kamera dürfen die UI nicht zum Absturz
bringen.

------------------------------------------------------------------------

## 16. Kamera und Bildanalyse

Fotoerkennung wird als separates technisches Modul behandelt.

Sie ist **kein eigener Datenbereich**, sondern ein alternativer
Eingabekanal.

Vorgesehener Ablauf:

``` text
Kamera
  ↓
Foto
  ↓
ausgewähltes Wertungstafel-Template
  ↓
Marker-/Werteerkennung
  ↓
Erkennungsergebnis
  ↓
ScoringViewModel
  ↓
manuell editierbare Werte
```

### Grundsatz

Die Fotoerkennung darf niemals die manuelle Eingabe verhindern.

Unsichere oder nicht erkannte Werte bleiben durch den Nutzer
korrigierbar.

------------------------------------------------------------------------

## 16.1 Lokalisierung

Die App wird von Beginn an technisch für Mehrsprachigkeit vorbereitet.

Für sichtbare UI-Texte werden `.resx`-Ressourcen verwendet.

Vorgesehene Struktur:

``` text
BoardgameTracker.App/
  Resources/
    Localization/
      AppResources.resx
      AppResources.de.resx
```

Spätere Sprachen können ergänzt werden, beispielsweise:

``` text
AppResources.en.resx
AppResources.fr.resx
```

Regeln:

-   Keine unnötig hart codierten sichtbaren UI-Texte in XAML oder C#.
-   Domain- und Persistenzwerte werden nicht als lokalisierte
    Anzeigetexte gespeichert.
-   Enums, IDs und technische Schlüssel bleiben sprachneutral.
-   Die UI übersetzt fachliche/technische Werte über Ressourcen.
-   Eigennamen und offiziell lokalisierte Brettspielbegriffe werden über
    stabile IDs bzw. Ressourcenschlüssel adressiert.

Im MVP wird zunächst nur Deutsch vollständig gepflegt. Die Architektur
erlaubt später weitere Sprachen ohne grundlegenden Umbau.

## 17. Plattformstrategie

### Android

Android wird zunächst die primäre Entwicklungs- und Testplattform.

Vorhandenes Testgerät:

-   Nokia 7.2
-   Android 11

Das Gerät eignet sich insbesondere auch als Test für:

-   Performance
-   kleinere/ältere Hardware
-   unterschiedliche Displayeigenschaften

### iOS

Vorhanden:

-   iPhone 12 Pro
-   MacBook Air

Die iOS-Integration erfolgt später.

Dann werden gemeinsam eingerichtet:

-   Xcode auf dem Mac
-   MAUI Pair-to-Mac / Build-Host
-   Apple Signing
-   Deployment auf das iPhone
-   Debugging

Die App soll von Anfang an so entwickelt werden, dass kein unnötig
Android-spezifischer Code in ViewModels oder Domainlogik gelangt.

------------------------------------------------------------------------

## 18. Bewusst nicht verwendete Architekturbausteine

Zum aktuellen Zeitpunkt werden **nicht** eingesetzt:

-   MediatR
-   vollständiges CQRS
-   Event Sourcing
-   Microservices
-   generische Game Engine
-   Backend
-   Cloud-Synchronisierung
-   komplexe Clean-Architecture-Schablonen mit vielen Projekten
-   separate Command-/Handler-Klassen für jede triviale Aktion

Diese Entscheidungen können später neu bewertet werden, falls die
tatsächliche Komplexität dies verlangt.

------------------------------------------------------------------------

## 19. Architekturregeln

### Regel 1

Spielspezifische Logik bleibt spielspezifisch.

### Regel 2

Allgemeine Domainobjekte enthalten keine Arche-Nova-Sonderfälle.

### Regel 3

ViewModels enthalten UI-Zustand, aber möglichst keine komplexen
Fachregeln.

### Regel 4

Views enthalten möglichst keine Geschäftslogik.

### Regel 5

Core kennt keine konkreten UI- oder SQLite-Details.

### Regel 6

Neue Abstraktionen werden nur eingeführt, wenn sie einen konkreten
Nutzen besitzen.

### Regel 7

Verständlichkeit hat Vorrang vor architektonischer Reinheit.

### Regel 8

Die App bleibt zunächst Local First.

------------------------------------------------------------------------

## 20. Aktuelle technische Entscheidung

Für den MVP gilt damit:

``` text
.NET MAUI
C#
XAML
MVVM
CommunityToolkit.Mvvm
EF Core 10
Microsoft.EntityFrameworkCore.Sqlite
EF Core Migrations
SQLite
Local First

Nullable Reference Types
required / init wo fachlich sinnvoll

RESX-Lokalisierung
Deutsch zunächst als gepflegte Sprache

Feature-/Vertical-Slice-Organisation
+
gemeinsamer Domain-Kern
+
stark typisierte Spielmodule
```

Diese Architektur ist die Grundlage für die erste echte Projektstruktur
und Implementierung.
