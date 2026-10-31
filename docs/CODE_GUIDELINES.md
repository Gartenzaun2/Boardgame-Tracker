# Boardgame Tracker – Code Guidelines

## 1. Zweck

Dieses Dokument enthält projektweite Regeln für **C#-, XAML- und ViewModel-
Implementierung**, insbesondere Codequalität, Benennung, Async/Nebenläufigkeit,
Bindings, Kommentare und Fehlerbehandlung.

Es wird nur bei Aufgaben gelesen, die Produktions- oder Testcode betreffen.

- Architektur und Modulgrenzen: `ARCHITECTURE.md`
- visuelle/Interaktionsregeln: `UI_DESIGN_GUIDE.md`
- konkrete Screens: `UI_WIREFRAME.md`
- Arbeitsweise und Tool-Nutzung: `../AGENTS.md`

## 2. Benennung und Lesbarkeit

Namen sollen fachlichen Gegenstand und Aktion bzw. Rolle erkennen lassen, ohne dass
die Implementierung geöffnet werden muss.

Methoden bevorzugen Verb + Gegenstand, z. B. `LoadCorrectionDraftAsync`,
`SaveCorrectionAsync` oder `GetPreparationAsync`. Generische Namen wie
`ExecuteAsync`, `BeginAsync`, `Prepare`, `ProcessAsync`, `HandleAsync`, `SaveAsync`
oder `DeleteAsync` nur, wenn Typkontext und Signatur die Rolle bereits eindeutig
machen.

Keine unnötigen Abkürzungen. Fachliche IDs entsprechend benennen, z. B.
`gameSessionId`, `playerId`, `specialCardId`. Asynchrone Methoden tragen `Async`.

Interne Renames vollständig durchführen; keine Compatibility-Aliase nur für alte
interne Namen einführen.

Fachliche Rollen und Domänenvokabular definiert `ARCHITECTURE.md`.
Allgemeine Naming-Regeln, die `.editorconfig` bereits schützt, werden dort technisch
erzwungen und hier nicht dupliziert.

## 3. Lernorientierung und Kommentare

Bevorzugt wird lesbarer, idiomatischer Produktionscode mit nachvollziehbaren
nicht offensichtlichen Entscheidungen statt unnötig cleverer Implementierungen.

Kommentare erklären **warum**, geschützte Invarianten, Transaktionsgrenzen oder
relevante Plattform-/Provider-Einschränkungen. Sie wiederholen nicht den sichtbaren
Code und ersetzen keine guten Namen.

XML-Dokumentation gezielt für zentrale Verträge und ungewöhnliche APIs verwenden,
nicht für jede triviale Methode oder Property. Hilfreiche Erklärungen bei
Refactorings erhalten und an geändertes Verhalten anpassen.

C#-Bezeichner sowie Code-/XML-Kommentare sind Englisch; Projektdokumentation bleibt
Deutsch.

## 4. Dateien und Begleittypen

Kleine Typen, die ausschließlich einem Slice/Haupttyp gehören, bleiben
standardmäßig in derselben Datei, insbesondere Commands, Inputs, Options, kleine
Read Models, Results, datentragende Records und slice-interne Enums.

Eine eigene Datei ist sinnvoll bei Wiederverwendung, eigenständiger fachlicher oder
technischer Rolle, relevanter eigener Logik, unabhängigem Änderungsgrund oder
deutlich besserer Lesbarkeit.

Öffentliche Verträge und Extension Points bleiben separat. Reine
Implementierungsdetails möglichst privat verschachteln, soweit XAML-, DI-, EF- oder
Modulgrenzen dies zulassen.

Ordner-/Slicestruktur: `ARCHITECTURE.md`.

## 5. C#-Stil

Einfache oder kurze Typen explizit schreiben, insbesondere `string?`, `bool`, `int`,
`Guid`, Enums und gut lesbare Modelltypen. `var` verwenden, wenn es erforderlich
oder bei komplexen Typen klarer ist; ein offensichtlicher Initialisierer allein ist
kein Grund für `var`.

Bei `if` mit genau einer einfachen Anweisung keine geschweiften Klammern; die
Anweisung steht eingerückt in einer eigenen Zeile. Bei mehreren Anweisungen einen
mehrzeiligen Klammerblock verwenden. Bei verschachtelten Bedingungen Klammern
verwenden, wenn sie die Zuordnung klarer machen.

Eine Anweisung pro Zeile. Komplexe Abläufe mit Absätzen, benannten
Zwischenschritten und bei Bedarf privaten Methoden gliedern.

Projekt-Typnamen über `using` einbinden, sofern kein Konflikt besteht. Keine festen
Zeilengrenzen oder zusätzliche Wrapper nur zur optischen Aufteilung.

Moderne C#-Features verwenden, wenn sie die Absicht klarer ausdrücken und zum
bestehenden Stil passen. Unberührten Code nicht nur zur Syntaxmodernisierung ändern.

## 6. XAML und Bindings

Neue oder geänderte Bindings werden als **compiled bindings** umgesetzt:

- passendes `x:DataType` pro `BindingContext` und `DataTemplate`;
- bei `BindingContext`-Wechsel neu typisieren;
- `x:Reference` über typisierte Page-/ViewModel-Properties;
- String-Bindings nur, wenn technisch erforderlich;
- keine EF-Entities oder EF-Navigation-Properties direkt an UI binden.

Strict-XAML-Warnungen nicht über `x:Object`, `x:Null`, `NoWarn` oder deaktivierte
Compileroptionen umgehen.

Projektweit aktiv sind:

```text
MauiStrictXamlCompilation
MauiEnableXamlCBindingWithSourceCompilation
XC0022–XC0025 als Buildfehler
```

Gemeinsame semantische Styles aus `UI_DESIGN_GUIDE.md` wiederverwenden; neue globale
Styles nur bei realem mehrfachen Bedarf. Visuelle Werte oder Screenmaße werden nicht
in diesem Dokument definiert.

## 7. ViewModels, Async und Nebenläufigkeit

ViewModel-Konstruktoren dienen ausschließlich Dependency Injection und einfachem
synchronem Initialzustand. Keine Datenbankzugriffe, Navigation oder asynchrones Laden
aus Konstruktoren. Laden über vorhandene Lifecycle-/Load-Mechanismen und gegen
unbeabsichtigte Mehrfachausführung absichern.

Asynchrone Commands, Use Cases, Persistenz- und I/O-Aufrufe vollständig `await`en.
`async void` ausschließlich für echte UI-/Framework-Eventhandler.

Keine Fire-and-forget-Tasks aus ViewModels oder Application-Use-Cases, sofern
Lebensdauer und Fehlerbehandlung nicht ausdrücklich definiert sind.

Schreibende Commands und asynchrone Navigation gegen unbeabsichtigte Parallel- oder
Doppelaufrufe über vorhandene Busy-/CanExecute-Mechanismen absichern.

Vorhandene `CancellationToken` bis EF-/I/O-Aufrufen durchreichen und nicht ohne Grund
durch `CancellationToken.None` ersetzen.

Keine künstliche Parallelisierung bei sequenziellen Use Cases. Einen EF-`DbContext`
niemals parallel verwenden; Persistenzregeln stehen in `ARCHITECTURE.md`.

Core und Application dürfen nicht von MAUI `MainThread`, `Dispatcher` oder anderen
UI-Thread-APIs abhängen. UI-Thread-Marshalling gehört in App/Presentation und nur
dorthin, wo es tatsächlich nötig ist. `MainThread` nicht zum Verdecken ungeklärter
Nebenläufigkeitsprobleme verwenden.

## 8. Fehlerbehandlung

Exceptions nur dort abfangen, wo sie sinnvoll behandelt, übersetzt oder geloggt
werden können.

Keine leeren `catch`-Blöcke und kein pauschales `catch (Exception)` ausschließlich
zum Unterdrücken technischer Fehler.

Technische Details an geeigneter Boundary loggen; sichtbare Fehlermeldungen bleiben
fachlich verständlich und folgen UI-/Lokalisierungsregeln.

## 9. Performance

Keine Performance-Optimierungen auf Verdacht.

Zuerst vorhandene strukturelle Regeln einhalten, insbesondere gezielte
Read-Model-Projektionen, kurze DbContext-Lebensdauer, compiled XAML bindings und
keine unnötige Layout- oder Datenmaterialisierung.

Größere Performance-Arbeiten nur bei gemessenem Problem oder ausdrücklichem Auftrag.
