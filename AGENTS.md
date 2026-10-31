# Repository Guidelines

## Projekt und Struktur
Boardgame Tracker ist eine bestehende mobile .NET-MAUI-App zur Erfassung und Auswertung von Brettspielpartien; Arche Nova ist das erste vollständig unterstützte Spiel. Ohne ausdrücklichen Auftrag weder neu aufbauen noch grundlegend umstrukturieren.

`BoardgameTracker.sln` bündelt drei Produktionsprojekte unter `src/`; `tests/` enthält Core- und Application-Tests (xUnit v3/Microsoft Testing Platform). Ressourcen und Lokalisierung liegen unter `src/BoardgameTracker.App/Resources/`.

## Repository access boundary
Work only inside the current Boardgame-Tracker repository.

Do not inspect, read, search, modify, execute, or enumerate files or directories outside the repository root unless the user explicitly requests access to a specific external path.

This includes, by default, no access to:
- parent directories of the repository;
- sibling repositories;
- the user's home directory;
- Desktop, Documents, Downloads, Pictures, or other personal folders;
- unrelated source-code directories;
- system directories;
- temporary directories outside the repository;
- other mounted drives or volumes;
- global configuration files;
- files belonging to other projects.

Do not use broad filesystem searches that escape the repository root.

Examples of prohibited default behavior include:
- `find ..`
- searching from the user's home directory;
- recursively enumerating parent directories;
- scanning other repositories for similar code;
- reading unrelated configuration files outside the repository;
- looking for assets, screenshots, databases, logs, or source files elsewhere on the machine.

If a required dependency or file appears to exist outside the repository, stop and report the missing external dependency instead of searching the system for it.

Access outside the repository is allowed only when the user explicitly identifies or authorizes the external file, directory, or resource.

Normal external tooling required to build and test the repository is still allowed, for example:
- installed .NET SDK/tooling;
- NuGet package restore;
- compiler and test runner executables;
- Git itself.

Using those tools does not authorize browsing or inspecting their installation directories.

Default rule:

`repository contents + required build/test tools only`

Everything else is opt-in.

## Quellen der Wahrheit
Vor relevanten Änderungen die zuständigen Dokumente lesen:

| Entscheidung | Maßgebliche Datei |
|---|---|
| Produktverhalten und Anforderungen | `docs/PRODUCT_SPEC.md` |
| Architektur und Abhängigkeiten | `docs/ARCHITECTURE.md` |
| Drittanbieter-Abhängigkeiten und Lizenzanforderungen | `docs/THIRD_PARTY_LICENSE_POLICY.md` |
| Domain und Persistenzmodell | `docs/DATA_MODEL.md` |
| UI-Regeln | `docs/UI_DESIGN_GUIDE.md` |
| Screens und Navigation | `docs/UI_WIREFRAME.md` |
| Umgebung, Build, Tests, Versionierung, Lieferung | `SETUP.md` |
| Projektüberblick | `README.md` |
| Historische Änderungen | `CHANGELOG.md` |

Die kanonischen Spezifikationen liegen ausschließlich unter `docs/`; keine gleichnamigen Kopien an anderen Stellen anlegen. Dokumentation nicht durch Annahmen ersetzen. Widersprüche zwischen Code und Dokumentation untersuchen; bei fachlich oder architektonisch relevanten Entscheidungen nachfragen, statt stillschweigend eine Seite zu wählen. Keine Anforderungen zum Füllen von Lücken erfinden.

## Architektur und Vertical Slices
Modularer Monolith mit pragmatischen Vertical Slices:

```text
App -> Application -> Core
Application -> EF Core
App -> EF Core SQLite
```

- Core: Domainmodell, Invarianten, Berechnungen, reine Fachregeln.
- Application: Vertical Slices, Use Cases, konkrete Queries, Read Models, EF Core, DbContext, Modellkonfiguration und Seeding.
- App-Providerbereich: SQLite-Konfiguration und verbindungsspezifische Collation.
- App: MAUI, XAML, ViewModels, Navigation, Presentation, Lokalisierung, Composition Root.

Neue Funktionalität primär nach fachlichem Use Case bzw. Slice organisieren, nicht nach technischem Typ. Änderungen möglichst innerhalb des betroffenen Slice halten. Ein Slice darf seine eng zusammengehörigen ViewModels, Commands, Read Models, Application-Logik und EF-Zugriffe nah beieinander halten.

Keine gemeinsame Abstraktion nur deshalb einführen, weil zwei Slices ähnlich aussehen. Gemeinsamen Code erst extrahieren, wenn eine stabile gemeinsame fachliche oder technische Semantik erkennbar ist. Cross-Slice-Abhängigkeiten vermeiden; vorhandene Domain- und Application-Verträge nutzen.

Autoritative Fachregeln gehören nicht in ViewModels. Feature-ViewModels kennen keine konkreten EF-/SQLite-Implementierungen. SQLite ausschließlich im Host-/Providerbereich konfigurieren; keine SQLite-spezifischen APIs in Application. EF-Entities nicht bis in App/ViewModels durchreichen; App und ViewModels arbeiten mit Domain-/Application-Modellen oder expliziten Read Models.

Vertical Slices und Composition erhalten; Arche-Nova-Logik bleibt unter `Games/ArkNova/`. Allgemeine Gruppen-Commands, Read Models und EF-Zugriffe dürfen keine spielspezifischen Typen oder festverdrahteten Farbregeln enthalten. Gemeinsame semantische `PlayerColor`-Werte sind erlaubt; jedes Spiel registriert seine erlaubte Teilmenge und einen eigenen Farb-Persistenzadapter. Gruppenformulare bleiben allgemein. Der Architekturtest für allgemeine Gruppenverträge muss erhalten bleiben. Domänenbegriffe `Game`, `GameSession`, `Group`, `Player` beibehalten.

Keine generischen Repositories, kein MediatR, keine unnötigen Manager-/Service-/Helper-/Utils-Abstraktionen und keine Interfaces ausschließlich für Mockbarkeit oder Symmetrie einführen.

Navigationsparameter ausschließlich über gemeinsame Konstanten aus `App/Navigation/NavigationParameters.cs` senden und in `ApplyQueryAttributes` auslesen; keine String-Literale für Schlüssel. Details: `docs/ARCHITECTURE.md`.

## MAUI, XAML und ViewModels
Sichtbare page-interne modale Overlays deaktivieren den darunterliegenden Page-Inhalt explizit; rein visuelles Überlagern reicht nicht. Dialog und Busy-Layer liegen außerhalb dieses deaktivierten Containers. Details einschließlich Shell-Navigation: `docs/UI_DESIGN_GUIDE.md`.

Neue oder geänderte XAML-Bindings grundsätzlich als compiled bindings implementieren.

- `x:DataType` auf dem Scope setzen, auf dem der jeweilige `BindingContext` gilt.
- `DataTemplate` mit dem Typ des Template-Elements separat typisieren.
- Bestehende Strict-XAML-/Binding-Compiler-Warnungen nicht durch `x:Object`, `x:Null`, `NoWarn` oder Abschalten der Compileroptionen umgehen.
- String-basierte Bindings nur verwenden, wenn compiled bindings technisch nicht sinnvoll möglich sind.
- Keine EF-Entities oder EF-Navigation-Properties direkt an UI binden.

ViewModel-Konstruktoren nur für Dependency Injection und einfachen synchronen Initialzustand verwenden. Keine Datenbankzugriffe, Navigation oder asynchrones Laden aus Konstruktoren starten.

Seiten-/ViewModel-Ladevorgänge über die vorhandenen Lifecycle-/Load-Mechanismen durchführen und gegen unbeabsichtigtes mehrfaches Laden absichern.

Core- und Application-Code dürfen keine Abhängigkeit auf MAUI `MainThread`, `Dispatcher` oder andere UI-Thread-APIs besitzen. UI-Thread-Marshalling ausschließlich in App/Presentation durchführen und nur, wenn es tatsächlich erforderlich ist. Keine `MainThread`-Aufrufe verwenden, um unbekannte Nebenläufigkeitsprobleme zu verdecken.

Sichtbare Texte ausschließlich über die bestehende Lokalisierung führen. Bestehende Theme-, Resource- und Style-Systeme verwenden; Farben, Abstände oder Fonts nicht lokal hartcodieren, wenn bereits semantische Ressourcen vorhanden sind.

## Async und Nebenläufigkeit
Asynchrone Commands, Use Cases sowie Persistenz- und I/O-Aufrufe vollständig `await`en.

- `async void` nur für echte UI-/Framework-Eventhandler verwenden.
- Keine Fire-and-forget-Tasks aus ViewModels, Application-Use-Cases starten, sofern Lebensdauer und Fehlerbehandlung nicht ausdrücklich definiert sind.
- Schreibende Commands gegen unbeabsichtigte Parallel- oder Doppelaufrufe über vorhandene Busy-/CanExecute-Mechanismen absichern.
- Vorhandene `CancellationToken` vollständig bis zu EF-/I/O-Aufrufen durchreichen; nicht ohne Grund durch `CancellationToken.None` ersetzen.
- Keine künstliche Parallelisierung einführen, wenn der Use Case sequenziell ist.

Exceptions nur dort abfangen, wo sie sinnvoll behandelt, übersetzt oder geloggt werden können. Kein leeres `catch` und kein pauschales `catch (Exception)` ausschließlich zum Unterdrücken technischer Fehler. Technische Details an einer geeigneten Boundary loggen; dem Benutzer nur eine fachlich verständliche Meldung zeigen.

## EF Core und Persistenz
`BoardgameTrackerDbContext` gehört zu Application und wird direkt in Slices verwendet, niemals in Pages/ViewModels. Über `IDbContextFactory<BoardgameTrackerDbContext>` lebt ein Context nur für eine Operation.

Keine Store-/Repository-/DbContext-Interfaces zum Verstecken von EF Core. Reine Reads verwenden konkrete Query-Klassen; Interfaces bleiben für echte Polymorphie, Host-/Plattformgrenzen und Spielmodul-Erweiterungspunkte. Datenbankbezogene Application-Slices mit echten relationalen EF-/SQLite-Integrationstests prüfen; Core-Tests bleiben technologieunabhängig.

`BoardgameTrackerDbContext` als kurzlebige Unit of Work behandeln.

- keinen DbContext in Singleton-Services oder langlebigen ViewModels halten;
- für unabhängige Operationen über die vorhandene `IDbContextFactory` einen neuen Context erzeugen;
- einen DbContext niemals parallel aus mehreren Tasks verwenden;
- jeden EF-Async-Aufruf vollständig `await`en, bevor derselbe Context erneut verwendet wird;
- einen Context nach einer EF-`InvalidOperationException`, die auf einen ungültigen Context-Zustand hindeutet, nicht weiterverwenden;
- `SaveChangesAsync` möglichst am fachlichen Transaktionsende ausführen statt nach jedem kleinen Zwischenschritt.

Reine Leseabfragen ohne Change Tracking ausführen, sofern die geladenen Entities nicht anschließend verändert werden. Für Read Models bevorzugt direkt in die tatsächlich benötigte Form projizieren statt vollständige Entity-Graphen zu materialisieren. `Include` nur verwenden, wenn der konkrete Use Case die Relation benötigt.

Eine fachliche Aktion wie „Partie abschließen“ oder „Wertung speichern“ möglichst als atomare Persistenzoperation behandeln. Keine teilweise gespeicherten Zustände hinterlassen, wenn nachfolgende Schritte derselben fachlichen Aktion fehlschlagen. Explizite Transaktionen nur einführen, wenn ein einzelnes `SaveChangesAsync` die notwendige Atomarität nicht herstellen kann.

Keine Persistenzänderung allein zur Bequemlichkeit der UI einführen. Schema-, Migrations- und Kompatibilitätsregeln ausschließlich nach `SETUP.md` und `docs/DATA_MODEL.md` anwenden.

## Benennung und Lernorientierung

Namen sollen den primär behandelten fachlichen Gegenstand und die Aktion/Rolle erkennen lassen, ohne dass die Implementierung geöffnet werden muss. `Query` bezeichnet konkrete reine Read-Projections, `Persistence` eine fachlich benannte EF-Komponente oder einen echten Spielmodul-Erweiterungspunkt, `UseCase` fachliche Application-Orchestrierung und `Handler` Dispatch/Routing/Navigation/Transportentscheidungen. `DataAccess` ist kein Standard-Suffix für neue oder refactorte Typen. Preferences-Stores schützen eine echte Hostgrenze. Feature-/Use-Case-Namen machen den Programmfluss sichtbar; Scoring und historische Korrektur bleiben getrennte Use Cases (ARCHITECTURE.md).

Methoden bevorzugen Verb und Gegenstand, etwa `LoadCorrectionDraftAsync`, `SaveCorrectionAsync` oder `GetPreparationAsync`. Generische Namen wie `ExecuteAsync`, `BeginAsync`, `Prepare`, `ProcessAsync`, `HandleAsync`, `SaveAsync` und `DeleteAsync` nur verwenden, wenn Typkontext und Signatur die konkrete Rolle bereits eindeutig machen. Beispiele sind keine mechanisch anzuwendende Namensschablone. Keine unnötigen Abkürzungen; `GameSession` ausschreiben, IDs fachlich benennen (`gameSessionId`, `playerId`, `specialCardId`). Asynchrone Methoden tragen gemäß .NET-Konvention `Async`. Interne Renames vollständig durchführen, ohne Compatibility-Aliase.

Educational clarity is an explicit project goal. Prefer readable, idiomatic production code with useful explanations of non-obvious decisions over unnecessarily terse or clever implementations.

Code und Dokumentation müssen fachliche/technische Entscheidungen nachvollziehbar machen. Kommentare erklären das Warum, geschützte Invarianten, Transaktionsgrenzen und Plattform-/SQLite-Einschränkungen; sie wiederholen keine sichtbaren Anweisungen und ersetzen keine guten Namen. XML-Dokumentation gezielt für zentrale Verträge und ungewöhnliche APIs verwenden, nicht für jede triviale Methode. Bestehende hilfreiche Erklärungen beim Refactor erhalten und aktualisieren. Projektweite Entscheidungen zusätzlich in der zuständigen kanonischen Dokumentation festhalten. Englische C#-Bezeichner und Code-/XML-Kommentare, deutsche Projektdokumentation. Lernorientierung rechtfertigt keine zusätzlichen Patterns oder Abstraktionsschichten.

## C#-Stil und Lesbarkeit
Kleine, ausschließlich einem Slice bzw. Haupttyp gehörende Begleittypen werden standardmäßig in derselben Datei wie ihr Haupttyp definiert. Dazu gehören Commands, Inputs, Options, kleine Read Models, Results, datentragende Records ohne eigenständige Logik und ausschließlich slice-interne Enums. Keine Datei ausschließlich für einen kleinen Companion Record anlegen, wenn dieser nur einem einzelnen Haupttyp bzw. Slice gehört. Eine eigene Datei ist sinnvoll, wenn der Typ von mehreren Slices verwendet wird, eine eigenständige fachliche oder technische Rolle besitzt, relevante eigene Logik enthält, unabhängig geändert wird oder die gemeinsame Datei sonst deutlich unübersichtlicher würde. Echte öffentliche Verträge und Extension Points bleiben separat. Reine Implementierungsdetails möglichst privat verschachteln; Modul-/Schichtgrenzen sowie XAML-, DI- und EF-Verwendungen bei der Sichtbarkeit beachten.

Ein Vertical Slice ist eine fachliche Grenze und erhält nicht automatisch einen eigenen Unterordner. Kleine Slices liegen bevorzugt flach im übergeordneten fachlichen Bereich. Ein eigener Slice-Ordner wird verwendet, wenn mehrere eigenständige Dateien dadurch sinnvoll gruppiert und leichter navigierbar werden. Verschachtelung folgt der tatsächlichen Komplexität des Features, nicht Symmetrie oder einer festen Mindestzahl von Dateien. Tests folgen demselben Prinzip.

Einfache oder kurze Typen explizit schreiben, insbesondere `string?`, `bool`, `int`, `Guid`, Enums und gut lesbare Modelltypen. `var` nur für komplexe Typen verwenden, wenn es die Lesbarkeit verbessert, z. B. lange verschachtelte generische `IEnumerable`-/Dictionary-Typen; anonyme Typen benötigen `var`. Ein offensichtlicher Initialisierer ist allein kein Grund für `var`. Beispiele: `string? gameKey = settings.SelectedGameKey;`, `Guid? groupId = selection;`. Keine pauschale nachträgliche Umstellung unberührten Codes.

Bei `if` mit genau einer einfachen Anweisung keine geschweiften Klammern verwenden; die Anweisung steht eingerückt auf einer eigenen Zeile. Ausnahme: Bei verschachtelten `if`-Anweisungen Klammern für eine eindeutige Block-/`else`-Zuordnung verwenden. Mehrere Anweisungen verwenden einen mehrzeiligen Klammerblock. Die öffnende Klammer darf auf derselben Zeile wie der zugehörige Ausdruck stehen. Keine Anweisung direkt hinter der Bedingung auf derselben Zeile und kein vollständiger `finally`-Block auf einer Zeile.

Eine Anweisung pro Zeile; komplexe Abläufe durch Absätze, benannte Zwischenschritte und bei Bedarf private Methoden gliedern. Projekt-Typnamen über `using` einbinden, sofern kein Namenskonflikt besteht. Keine festen Zeilengrenzen oder zusätzlichen Wrapper allein zur Aufteilung einführen.

Moderne C#-Sprachfeatures verwenden, wenn sie die Absicht klarer ausdrücken und zum bestehenden Projektstil passen. Keine Syntaxmodernisierung unberührten Codes nur um der Modernisierung willen.

## Vorgehen und Entscheidungsgrenze
Vor jeder Änderung betroffene Dateien und relevante Dokumentation lesen, die Implementierung verstehen und Auswirkungen auf Tests, Persistenz, UI und Dokumentation prüfen. Danach die kleinste sinnvolle Änderung durchführen; funktionierenden Code nicht unnötig neu schreiben.

Fehler, Ungereimtheiten und Verbesserungsmöglichkeiten außerhalb des aktuellen Auftrags nur benennen und mit dem Benutzer besprechen; nicht automatisch korrigieren oder als beiläufige Bereinigung aufnehmen. Solche Änderungen erst nach ausdrücklicher Beauftragung umsetzen. Ist eine auftragsfremde Korrektur Voraussetzung für die Aufgabe, die Abhängigkeit erklären und vor der Umsetzung abstimmen.

Keine Architektur-, Persistenz- oder Paketänderungen außerhalb des Auftrags. Neue Third-Party-Abhängigkeiten – einschließlich NuGet-Paketen, nativen Bibliotheken und ausgelieferten Drittanbieter-Assets – nur bei klarem Bedarf und nach Lizenzprüfung gemäß `docs/THIRD_PARTY_LICENSE_POLICY.md`. Kleine technische Entscheidungen innerhalb bestehender Spezifikation selbstständig treffen. Vor Änderungen an Architekturgrundsätzen, grundlegendem Datenmodell, Persistenzstrategie/Migration, wesentlichen Benutzerabläufen, größeren Abhängigkeiten oder bestehenden Anforderungen nachfragen; ebenso bei nicht spezifiziertem Fachverhalten.

Neue/geänderte Fachlogik geeignet testen. Tests niemals für einen grünen Lauf entfernen, abschwächen oder umgehen. UI-Änderungen anhand beider UI-Dokumente prüfen.

Keine Performance-Optimierungen auf Verdacht. Zuerst vorhandene strukturelle Regeln einhalten: compiled bindings, gezielte Read-Model-Projektionen, kurze DbContext-Lebensdauer und keine unnötige Layout-/Datenmaterialisierung. Größere Performance-Arbeiten nur bei gemessenem Problem oder ausdrücklichem Auftrag.

## Arbeitsmodus und Token-Effizienz

### Scope hat Vorrang

Ein Implementierungsauftrag darf nicht implizit zum Audit-, Forschungs- oder
Vollprüfungsauftrag erweitert werden. „Produktionsreif“, „Designrichtlinien
einhalten“, „sauber prüfen“, „korrekt integrieren“ und „relevante Abhängigkeiten
berücksichtigen“ sind keine Freigabe für vollständige Dependency-/Security-/Lizenz-/
Architektur-Audits, Downloads und Analysen des transitiven Paketbaums,
SDK-/NDK-/Runtime-Inventuren oder zusätzliche Release-Matrizen. Ebenso wenig
erlauben sie Emulator-/Geräteläufe, manuelles Durchklicken, Screenshots,
Screenshotanalyse oder Theme-Sichtprüfungen. Solche Arbeiten benötigen einen
ausdrücklichen Auftrag. Zusätzlichen Prüfbedarf im Abschlussbericht nennen,
statt die Prüfung selbstständig auszuführen.

UI-/Designprüfung bedeutet standardmäßig: vorhandene UI-Dokumentation lesen,
XAML, Styles, Ressourcen, Bindings und Navigation entsprechend implementieren,
Build und automatisierte Tests durchführen. Kein App-Start oder Screenshot;
manuelle/visuelle Prüfung bleibt ausschließlich Opt-in.

Standardmäßig auf kurze Durchlaufzeit, geringe Tool-Nutzung und möglichst wenig unnötigen Kontextverbrauch optimieren. Nur die Analyse und Werkzeuge einsetzen, die zur korrekten Umsetzung des aktuellen Auftrags und zu seiner codebasierten Verifikation nötig sind.

Bei kleinen und klar abgegrenzten Aufgaben keine umfangreiche Voranalyse oder Implementierungsplanung erstellen. Zuerst die direkt betroffenen Dateien und die dafür maßgebliche Dokumentation prüfen und dann umsetzen. Die Repository-Erkundung nur erweitern, wenn Abhängigkeiten, Buildfehler oder Testfehler dies konkret erfordern.

Ohne ausdrücklichen Auftrag nicht:
- das gesamte Repository nach zusätzlichen Problemen, Verbesserungen oder Aufräumarbeiten durchsuchen;
- nicht betroffenen Code modernisieren, umbenennen, verschieben oder refactoren;
- externe Web-Recherche oder Dokumentationsrecherche durchführen, wenn Repository-Code und Projektdokumentation zur Lösung ausreichen;
- Git-Historie, `git blame`, alte Commits, Issues oder Pull Requests untersuchen, sofern dies nicht für den konkreten Auftrag nötig ist;
- Dependency-, Security-, Performance- oder Architektur-Audits durchführen;
- Benchmarks oder Profiling starten;
- generierte Binärdateien oder Build-Artefakte analysieren;
- zusätzliche Diagramme, Bilder oder sonstige visuelle Artefakte erzeugen;
- Dokumentation, README, Kommentare oder XML-Dokumentation außerhalb der tatsächlich geänderten Anforderungen überarbeiten;
- nach erfolgreicher codebasierter Verifikation weitere alternative Prüfmethoden nur zur zusätzlichen Absicherung durchführen.

Auftragsfremde Auffälligkeiten kurz im Abschlussbericht nennen statt sie automatisch zu bearbeiten.

## Verifikation und Lieferung

### Verifikationsbudget

Die vollständige Standardverifikation für normale Aufgaben ist genau:
ein Solution-Build, Core-Tests einmal und Application-Tests einmal. Nach Erfolg
ist die normale Aufgabe abgeschlossen; es folgt nur der übliche Diff-/Statuscheck.
Keine zusätzlichen ABI-/Plattform-Release-Builds, Trimming-Matrizen,
APK-/IPA-Inhaltsanalysen, Emulator-/Geräteläufe oder alternativen Prüfmethoden
ohne ausdrücklichen Auftrag. Eine konkret beauftragte Plattform-/Packaging-
Eigenschaft kann eine dafür gezielte Prüfung erfordern, keinen automatischen
Vollprüfungsauftrag.

Schlägt eine Prüfung wegen einer vorgenommenen Änderung fehl, Ursache korrigieren
und nur den betroffenen notwendigen Schritt wiederholen. Bereits erfolgreiche
Prüfungen nicht erneut ausführen, nur weil anschließend Dokumentation oder andere
davon unabhängige Dateien geändert wurden. Keine komplette Prüfmatrix neu starten.
Die Standardverifikation ist codebasiert:

```text
Code ändern -> Build -> automatisierte Tests -> fertig
```

Build und relevante automatisierte Tests ausführen; bei sinnvoller Abdeckung darf auch die breitere oder vollständige Testsuite laufen. Neue oder geänderte Unit-, Integrations- und Regressionstests dürfen selbstständig ergänzt werden, wenn sie die angeforderte Änderung sinnvoll absichern. Automatisierte Code-Tests gelten ausdrücklich als normale Standardverifikation.

Beispiele ab Solution-Root:

```powershell
dotnet build BoardgameTracker.sln
dotnet test --project tests/BoardgameTracker.Core.Tests
dotnet test --project tests/BoardgameTracker.Application.Tests
```

### Manuelle und visuelle Verifikation ist Opt-in
Ohne ausdrücklichen Auftrag keine manuelle oder visuelle Smoke-Test-Phase durchführen. Insbesondere nicht:
- die App nur zur manuellen Funktionsprüfung starten;
- Oberflächen manuell durchklicken;
- Screenshots aufnehmen oder erzeugen;
- Screenshots oder andere UI-Bilder analysieren;
- Vorher-/Nachher-Screenshots vergleichen;
- visuelle Regressionstests durchführen;
- Bilder zur Verifikation generieren;
- Layout, Farben, Abstände oder Rendering per Bildanalyse beurteilen;
- manuelle End-to-End-Abläufe durchführen, wenn die Änderung durch Build und automatisierte Tests ausreichend verifiziert werden kann.

Auch bei UI-Code gilt: Quellcode, XAML, Binding-/Navigationslogik, Build und automatisierte Tests sind die Standardprüfung. Eine visuelle oder manuelle Prüfung nur durchführen, wenn der Benutzer sie ausdrücklich verlangt. Wenn eine rein visuelle Anforderung ohne Sichtprüfung nicht zuverlässig beurteilt werden kann, dies im Abschlussbericht benennen, statt automatisch Screenshots oder Bildanalyse zu starten.

Geräte-Smoke-Tests aus `SETUP.md` sind damit keine Standardverifikation für normale Implementierungsaufträge. Sie nur ausführen, wenn der Benutzer ausdrücklich einen Geräte-/UI-/Smoke-Test verlangt oder der konkrete Auftrag selbst die manuelle Geräteprüfung zum Gegenstand hat.

Nach erfolgreichem Build und erfolgreichen automatisierten Tests nicht automatisch weitere Prüfverfahren starten. Fehlgeschlagene/nicht ausführbare Prüfungen ausdrücklich nennen; nur tatsächlich ausgeführte Verifikation als erfolgreich melden.

`git diff` auf unbeabsichtigte Änderungen prüfen, einschließlich neuer Dateien. Dieser abschließende Diff-/Status-Check gehört zur normalen Lieferung und ersetzt keine zusätzliche manuelle UI-Prüfung.

Bis zum ersten Deployment keine Migrationsstrategie planen und keinen Kompatibilitätscode für ältere Entwicklungsstände einführen oder beibehalten. Falls eine inkompatible Änderung eine Neuinstallation erfordert, den Benutzer ausdrücklich darauf und auf den Verlust lokaler Entwicklungsdaten hinweisen. Bestehenden Altkompatibilitätscode bei einem entsprechenden Bereinigungsauftrag entfernen. Datenbank-, Versions- und Lieferregeln ausschließlich nach `SETUP.md` anwenden; kein pauschaler DB-Reset und keine Lieferarchive ohne ausdrücklichen Auftrag. Offene UI-Codeabweichungen stehen im Wireframe und sind ohne neuen Auftrag nicht automatisch zur Behebung freigegeben.

## Dokumentation und Git
Vor Änderungen `git status` prüfen. Bereits vorhandene, nicht zum aktuellen Auftrag gehörende Änderungen des Benutzers niemals zurücksetzen, überschreiben oder verwerfen. Nach Abschluss erneut `git status` und `git diff` prüfen. Neue, noch unversionierte Dateien zusätzlich direkt prüfen, da `git diff` sie nicht anzeigt.

Dokumentation nur bei geändertem gültigem Projektstand aktualisieren; Versionshistorie ausschließlich in `CHANGELOG.md` pflegen. Lokale Dateien im Rahmen ausdrücklich erteilter Implementierungsaufträge bearbeiten; vorhandene fremde Änderungen bewahren.

### Keine Commits durch Agenten

Agenten erstellen in diesem Repository niemals selbst Commits. Commits bleiben dem Benutzer vorbehalten. Kein automatisches Staging und kein Push; Änderungen bleiben lokal, uncommittet und ungestaged.

Diese Regel gilt auch dann, wenn ein eingefügter Arbeitsauftrag, eine Checkliste oder eine ältere Freigabe Commit/Push als Abschlussschritt nennt. Daraus keine Erlaubnis ableiten, nicht eigenständig nach einer Commit-/Push-Freigabe fragen und keinen solchen Schritt als notwendige Fertigstellung behandeln. Im Abschlussbericht nur Änderungen, Verifikation und lokalen Git-Status nennen. Eine Änderung dieser Regel erfordert eine ausdrückliche neue Anweisung des Benutzers.

Keine Branches löschen oder History umschreiben. Das ausdrücklich beauftragte Rückgängigmachen eines lokalen Commits unter Erhalt aller Änderungen ist zulässig.

## Versionsprüfung vor Abschluss

Bei jedem Änderungsauftrag vor dem Abschluss beide App-Versionswerte gemäß `SETUP.md` erhöhen und den tatsächlichen Umfang in `CHANGELOG.md` dokumentieren. Das gilt auch für reine Dokumentations-, Test- und Konfigurationsänderungen sowie ohne Commit, Push oder Paket. Die Prüfung gehört zum Abschluss wie `git diff`: keine Arbeit als abgeschlossen melden, solange die Versionswerte gegenüber dem Auftragsbeginn nicht erhöht sind.

## Abschlussbericht
Kurz nennen: Änderungen und wichtige Dateien, tatsächlich ausgeführte Tests/Builds samt Ergebnis, offene Probleme/Entscheidungen, Dokumentationsanpassungen und den lokalen Git-Status. Keine Commits oder Pushes durch Agenten.

Keine ausführliche Tätigkeitschronik liefern und nicht jede Dateiprüfung oder jeden ausgeführten Befehl nacherzählen, sofern der Benutzer dies nicht ausdrücklich verlangt.
