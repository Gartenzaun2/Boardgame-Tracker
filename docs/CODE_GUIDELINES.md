# Boardgame Tracker – Code Guidelines

## Zweck

Dieses Dokument enthält die projektweiten Regeln für C#-Codequalität, Benennung,
asynchrone Ausführung und Kommentare.

Es wird nur bei Aufgaben gelesen, die Produktions- oder Testcode betreffen.
Architekturregeln stehen in `ARCHITECTURE.md`; UI-/XAML-Regeln in
`UI_DESIGN_GUIDE.md`; Arbeitsweise, Scope und Tool-Nutzung in `../AGENTS.md`.

## Benennung und Lesbarkeit

Namen sollen den primär behandelten fachlichen Gegenstand und die Aktion bzw. Rolle
erkennen lassen, ohne dass die Implementierung geöffnet werden muss.

Methoden bevorzugen Verb und Gegenstand, zum Beispiel
`LoadCorrectionDraftAsync`, `SaveCorrectionAsync` oder `GetPreparationAsync`.
Generische Namen wie `ExecuteAsync`, `BeginAsync`, `Prepare`, `ProcessAsync`,
`HandleAsync`, `SaveAsync` und `DeleteAsync` nur verwenden, wenn Typkontext und
Signatur die konkrete Rolle bereits eindeutig machen.

Keine unnötigen Abkürzungen verwenden. Fachliche IDs entsprechend ihrem Gegenstand
benennen, z. B. `gameSessionId`, `playerId` oder `specialCardId`.
Asynchrone Methoden tragen gemäß .NET-Konvention das Suffix `Async`.

Interne Renames vollständig durchführen; keine Compatibility-Aliase nur zur
Aufrechterhaltung alter interner Namen einführen.

Die fachlichen Rollen `Query`, `UseCase`, `Persistence`, `Store` und `Handler`
sowie das verbindliche Domänenvokabular sind in `ARCHITECTURE.md` definiert.
Objektive allgemeine Naming-Regeln wie PascalCase und private Feldnamen werden
zusätzlich durch `.editorconfig` geschützt.

## Lernorientierung und Kommentare

Boardgame Tracker ist ein Lernprojekt. Bevorzugt wird lesbarer, idiomatischer
Produktionscode mit nachvollziehbaren nicht offensichtlichen Entscheidungen statt
unnötig knapper oder cleverer Implementierungen.

Kommentare erklären das Warum, geschützte Invarianten, Transaktionsgrenzen oder
relevante Plattform-/Provider-Einschränkungen. Sie wiederholen nicht den unmittelbar
sichtbaren Code und ersetzen keine guten Namen.

XML-Dokumentation gezielt für zentrale Verträge und ungewöhnliche APIs verwenden,
nicht für jede triviale Methode oder Property. Bestehende hilfreiche Erklärungen
bei Refactorings erhalten und an geändertes Verhalten anpassen.

Englische C#-Bezeichner sowie Code- und XML-Kommentare verwenden; deutsche
Projektdokumentation beibehalten.

## Dateien, Begleittypen und Struktur

Kleine Typen, die ausschließlich einem Slice bzw. Haupttyp gehören, stehen
standardmäßig in derselben Datei wie ihr Haupttyp. Dazu gehören insbesondere
Commands, Inputs, Options, kleine Read Models, Results, datentragende Records ohne
eigenständige Logik und ausschließlich slice-interne Enums.

Eine eigene Datei ist sinnvoll, wenn ein Typ von mehreren Slices verwendet wird,
eine eigenständige fachliche oder technische Rolle besitzt, relevante eigene Logik
enthält, unabhängig geändert wird oder die gemeinsame Datei sonst deutlich
unübersichtlicher würde.

Echte öffentliche Verträge und Extension Points bleiben separat. Reine
Implementierungsdetails möglichst privat verschachteln, soweit XAML-, DI-, EF- oder
Modulgrenzen dies zulassen.

Die fachliche Ordner- und Slice-Struktur selbst ist in `ARCHITECTURE.md` definiert.

## C#-Stil

Einfache oder kurze Typen explizit schreiben, insbesondere `string?`, `bool`, `int`,
`Guid`, Enums und gut lesbare Modelltypen. `var` verwenden, wenn es für anonyme Typen
erforderlich ist oder einen komplexen Typ lesbarer macht. Ein offensichtlicher
Initialisierer allein ist kein Grund für `var`.

Bei `if` mit genau einer einfachen Anweisung keine geschweiften Klammern verwenden;
die Anweisung steht eingerückt auf einer eigenen Zeile. Bei mehreren Anweisungen
einen mehrzeiligen Klammerblock verwenden. Bei verschachtelten Bedingungen Klammern
verwenden, wenn sie die Block- bzw. `else`-Zuordnung eindeutig machen.

Eine Anweisung pro Zeile. Komplexe Abläufe mit Absätzen, benannten Zwischenschritten
und bei Bedarf privaten Methoden gliedern.

Projekt-Typnamen über `using` einbinden, sofern kein Namenskonflikt besteht.
Keine festen Zeilengrenzen oder zusätzliche Wrapper allein zur optischen Aufteilung
einführen.

Moderne C#-Sprachfeatures verwenden, wenn sie die Absicht klarer ausdrücken und zum
bestehenden Projektstil passen. Unberührten Code nicht allein zur
Syntaxmodernisierung ändern.

## Async und Nebenläufigkeit

Asynchrone Commands, Use Cases sowie Persistenz- und I/O-Aufrufe vollständig
`await`en.

`async void` ausschließlich für echte UI-/Framework-Eventhandler verwenden.

Keine Fire-and-forget-Tasks aus ViewModels oder Application-Use-Cases starten,
sofern Lebensdauer und Fehlerbehandlung nicht ausdrücklich definiert sind.

Schreibende Commands gegen unbeabsichtigte Parallel- oder Doppelaufrufe über die
vorhandenen Busy-/CanExecute-Mechanismen absichern.

Vorhandene `CancellationToken` bis zu EF-/I/O-Aufrufen durchreichen und nicht ohne
fachlichen oder technischen Grund durch `CancellationToken.None` ersetzen.

Keine künstliche Parallelisierung einführen, wenn der Use Case sequenziell ist.
Einen EF-`DbContext` niemals parallel verwenden; die Persistenzregeln stehen in
`ARCHITECTURE.md`.

ViewModel-Konstruktoren ausschließlich für Dependency Injection und einfachen
synchronen Initialzustand verwenden. Keine Datenbankzugriffe, Navigation oder
asynchrones Laden aus Konstruktoren starten. Seiten- und ViewModel-Ladevorgänge über
die vorhandenen Lifecycle-/Load-Mechanismen durchführen und gegen unbeabsichtigtes
mehrfaches Laden absichern.

Core- und Application-Code dürfen keine Abhängigkeit auf MAUI `MainThread`,
`Dispatcher` oder andere UI-Thread-APIs besitzen. UI-Thread-Marshalling gehört in
App/Presentation und wird nur eingesetzt, wenn es tatsächlich erforderlich ist.
`MainThread` nicht verwenden, um ungeklärte Nebenläufigkeitsprobleme zu verdecken.

## Fehlerbehandlung

Exceptions nur dort abfangen, wo sie sinnvoll behandelt, übersetzt oder geloggt
werden können.

Keine leeren `catch`-Blöcke und kein pauschales `catch (Exception)` ausschließlich
zum Unterdrücken technischer Fehler.

Technische Details an einer geeigneten Boundary loggen; sichtbare Fehlermeldungen
bleiben fachlich verständlich und folgen den UI-/Lokalisierungsregeln.

## Performance

Keine Performance-Optimierungen auf Verdacht. Zuerst vorhandene strukturelle Regeln
einhalten, insbesondere gezielte Read-Model-Projektionen, kurze DbContext-Lebensdauer,
compiled XAML bindings und keine unnötige Layout- oder Datenmaterialisierung.

Größere Performance-Arbeiten nur bei einem gemessenen Problem oder einem
ausdrücklichen Auftrag durchführen.
