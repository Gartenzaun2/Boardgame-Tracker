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

Zustandsnamen sollen den tatsächlich geschützten Ablauf bezeichnen. Ein Flag für
Speichern und anschließende Navigation darf nicht nur das Speichern suggerieren.

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

Bei ungewöhnlichen Zuständen und Schutzmechanismen dürfen Kommentare ausführlicher
sein: Sie sollen Anlass, Zeitpunkt und Konsequenz einer Entscheidung verständlich
machen. Offensichtliche Getter und selbsterklärende Properties bleiben unkommentiert.
XML-`<summary>` darf bei kurzen Erklärungen vollständig in einer Zeile stehen;
für umfangreichere Begründungen ist eine mehrzeilige Summary sinnvoll.
Eine XML-Summary steht **vor** den Attributen der dokumentierten Deklaration.

Beispiel für einen erklärungsbedürftigen Dialogzustand (nicht als pauschale
Vorlage für jeden Speichervorgang verwenden):

```csharp
/// <summary>
/// Set to true after a successful save to preserve the completed state if navigation fails.
/// This prevents further editing or saving while the dialog remains open,
/// allowing the user to retry leaving without modifying the saved player.
/// </summary>
[ObservableProperty]
public partial bool IsSaveCompleted { get; private set; }
```

XML-Dokumentation gezielt für zentrale Verträge und ungewöhnliche APIs verwenden,
nicht für jede triviale Methode oder Property. Hilfreiche Erklärungen bei
Refactorings erhalten und an geändertes Verhalten anpassen.

Nicht offensichtliche Rückgabebedeutungen, insbesondere `bool` mit mehreren
Abbruchgründen, gezielt mit XML-`<returns>` erläutern. Eine redundante
`<summary>` ist dafür nicht nötig. Bei System-/Framework-Overrides Auslöser
und Zweck mit einer kurzen XML-`<summary>` erklären, z. B. bei
`OnBackButtonPressed()` die vom eigenen Zurück-Button unabhängige
System-Zurückaktion.

Kommentare dürfen auch ungewöhnliche Zustandszweige, Early Returns und
zeitliche Abläufe erklären, wenn daraus erst der Grund einer Entscheidung
verständlich wird. Kein Kommentarzwang für offensichtliche Anweisungen.

C#-Bezeichner sowie Code-/XML-Kommentare sind Englisch; Projektdokumentation bleibt
Deutsch.

### Lernorientierte Dokumentation von Architekturabstraktionen

Bewusst eingeführte Abstraktionen an Schicht- und Framework-Grenzen (z. B.
Interfaces, Ports und Adapter) müssen **am zentralen Vertrag selbst** so erklärt
werden, dass auch ein Einsteiger die Entscheidung nachvollziehen kann. Die
englischsprachige XML-Dokumentation beschreibt bei Bedarf:

- **Warum** die Abstraktion existiert und welche konkrete Kopplung sie vermeidet;
- **wer** den Vertrag nutzt und **wer** die technische Umsetzung übernimmt;
- **wie** er im echten Projekt verwendet wird, möglichst mit einem kurzen Beispiel;
- **welchen Nutzen** diese Trennung hat, etwa Tests ohne laufendes UI-Framework.

Ein aussagekräftiges `<example>` ist erwünscht, wenn es die Verwendung schneller
verständlich macht. Eine ausführlichere Abwägung einschließlich bewusst verworfener
Alternativen gehört in die zuständige Architekturentscheidung in
`ARCHITECTURE.md`, nicht wiederholt an jeden Aufrufort.

Beispiel einer **begründeten** Navigationsschnittstelle (nicht als Pflichtvorlage
für beliebige Interfaces verwenden):

```csharp
/// <summary>
/// Lets ViewModels request navigation without depending on MAUI Shell.
/// A Shell adapter handles the framework-specific call, while tests can
/// replace this contract with a fake and verify the requested navigation.
/// This avoids requiring a running Page or Shell just to test a save flow.
/// </summary>
/// <example>
/// After a successful save: <code>await _navigationService.GoBackAsync();</code>
/// The same call can be verified in a ViewModel test without creating a Page.
/// </example>
public interface INavigationService {
    Task GoBackAsync();
}
```

Die Schnittstelle bleibt unabhängig von **MAUI-Navigations-APIs**. Das bedeutet
nicht, dass sämtliche ViewModels schon vollständig unabhängig von jeder anderen
App- oder Framework-Abhängigkeit sind. Kein Pflichtkommentar für triviale Getter,
selbsterklärende Methoden oder bloß um des Lernens willen erfundene Interfaces.

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

**Lesereihenfolge innerhalb einer Klasse:** Bei ViewModels bevorzugt zuerst
explizite DI-Felder, dann Konstruktor, danach eigener Zustand und Properties,
anschließend Methoden in der Reihenfolge des fachlichen Ablaufs. Hilfsmethoden
stehen nach Möglichkeit nahe bei dem Ablauf, zu dem sie gehören; mehrfach genutzte
Hilfsmethoden dürfen gesammelt werden. Diese Reihenfolge ist eine Orientierung,
keine starre Sortierung nach Sichtbarkeit oder Member-Typ.

Technische Dialogzustände (z. B. `IsBusy`), fachliche Prozessdaten (z. B.
`PlayerId`, `Name`) und reine UI-Text-Properties werden möglichst erkennbar
gruppiert. Zusammengehörige Zustände und ihre abgeleiteten Properties bleiben
beieinander. Keine Abschnittskommentare, die nur Offensichtliches wiederholen.

`#region` ist zur Navigation erlaubt, aber kein Ersatz für passende
Verantwortlichkeiten. Regions heißen konkret nach ihrem Inhalt, z. B.
`#region Localized UI Text` für lokalisierte Titel und Buttontexte. Eine solche
Region darf dort stehen, wo sie den Lesefluss am besten unterstützt; sie muss
nicht am Dateiende liegen. Eine zusätzliche verschachtelte Klasse oder
`partial`-Datei nur für einfache Texte ist kein Standard.

Bei mehreren injizierten Abhängigkeiten deren Rolle im Feldnamen erkennbar machen,
z. B. `_editGroupUseCase`, `_gamesQuery` und `_navigationService` statt
mehrdeutigem `_useCase` oder `_activation`.

Technische Dialogzustände wie `IsBusy` oder `IsSaveCompleted` und fachliche Daten
wie `GroupName` oder `IsGroupArchived` nicht allein aufgrund eines gemeinsamen
`Is`-Präfixes vermischen. Den fachlichen Gegenstand bei Mehrdeutigkeit benennen,
aber nicht jedes Member reflexartig mit `Group` präfixieren.

Kleine parameterlose `CanExecute`-Bedingungen dürfen als berechnete `bool`-Properties
(`CanSave`) unmittelbar bei den zugehörigen Prozessproperties stehen; eine Methode
ist nicht zwingend erforderlich. `[RelayCommand(CanExecute = ...)]` wertet die
Bedingung aus, verfolgt deren Abhängigkeiten aber **nicht** selbst. An den
ursächlichen Observable-Properties weiterhin
`[NotifyCanExecuteChangedFor(nameof(SaveCommand))]` verwenden;
`PropertyChanged(nameof(CanSave))` ersetzt das nicht.

Bei abgeleiteten Ansichten einer `ObservableCollection<T>` Collection-Mutationen
(`CollectionChanged`) von Änderungen innerhalb enthaltener Elemente
(`INotifyPropertyChanged`) unterscheiden. Benachrichtigungen samt nötiger
An-/Abmeldungen zentral und vollständig pflegen. Keine eigene reaktive
Attribut-Infrastruktur oder zusätzliche Bibliothek für einen einzelnen einfachen
Filterfall einführen; wiederkehrenden Bedarf gesondert bewerten.

Ordner-/Slicestruktur: `ARCHITECTURE.md`.

## 5. C#-Stil

Einfache oder kurze Typen explizit schreiben, insbesondere `string?`, `bool`, `int`,
`Guid`, Enums und gut lesbare Modelltypen. `var` verwenden, wenn es erforderlich
oder bei komplexen Typen klarer ist; ein offensichtlicher Initialisierer allein ist
kein Grund für `var`.

### 5.1 Klammern und Einrückung

Vier **Leerzeichen** je Einrückungsebene, keine Tab-Zeichen. Die Tab-Taste im
Editor darf selbstverständlich Leerzeichen einfügen. Öffnende geschweifte
Klammern stehen bei Typen, Konstruktoren, Methoden und Kontrollstrukturen
**in derselben Zeile**; schließende Klammern stehen auf einer eigenen Zeile
(außer bei kompakten Property-Accessors).

Bei `if`, `foreach` und vergleichbaren Kontrollstrukturen mit genau einer
einfachen Anweisung keine geschweiften Klammern; die Anweisung steht eingerückt
auf einer eigenen Zeile. Bei mehreren Anweisungen einen mehrzeiligen Klammerblock
verwenden. Bei verschachtelten Kontrollstrukturen Klammern setzen, wenn sie die
Zuordnung und den Ablauf klarer machen.

```csharp
foreach (Player player in Players)
    player.RefreshScore();

if (IsHistorical) {
    if (CanSave)
        await SaveCorrectionAsync();
    else
        ShowValidationError();
}
```

Eine Anweisung pro Zeile. Komplexe Abläufe mit Absätzen, benannten
Zwischenschritten und bei Bedarf privaten Methoden gliedern.

Early Returns und die Short-Circuit-Auswertung mit `&&`/`||` bevorzugen, wenn
sie den Kontrollfluss klarer und flacher machen. Mehrfach verschachtelte
`if`-Blöcke nach Möglichkeit vermeiden. Ternäre Ausdrücke nur für einfache,
unmittelbar erkennbare Wertauswahl verwenden; nicht innerhalb von
`if`-Bedingungen und nicht zur Auswahl verschiedener Methodenaufrufe,
insbesondere asynchroner Abläufe. Komplexe Bedingungen bei Bedarf durch
sprechend benannte boolesche Zwischenvariablen erklären; für triviale
Bedingungen keine zusätzlichen Variablen einführen.

Guards bei unterschiedlichen Abbruchgründen erkennbar gliedern, etwa
Nebenläufigkeit getrennt von ungültigem oder nicht zugehörigem UI-Modell.
Zusammengehörige triviale Prüfungen dürfen in einer Bedingung bleiben.

Boolesche Flag-Parameter, die wesentlich unterschiedliche Abläufe steuern,
kritisch prüfen. Zuerst eine direkte Fallunterscheidung mit Early Return
bevorzugen; zusätzliche Methoden oder Delegates nur bei Lesbarkeitsgewinn.

### 5.2 Attribute, Properties und Methoden

Attribute stehen **immer oberhalb** der zugehörigen Deklaration, nie in derselben
Zeile wie Feld, Property, Methode oder Typ. Parameterlose Attribute dürfen
zusammen in einer Attributzeile stehen; jedes Attribut mit Parametern steht
bevorzugt in einer eigenen Zeile. Mehrere Parameter desselben Attributs bleiben
möglichst zusammen in einer Zeile. Zwischen Attributen und Deklaration steht
keine Leerzeile.

```csharp
[ObservableProperty, JsonIgnore]
[NotifyPropertyChangedFor(nameof(IsHistorical), nameof(IsLiveScoring))]
public partial ScoringMode Mode { get; private set; }
```

Zwischen unabhängigen Methoden eine Leerzeile; dasselbe gilt standardmäßig für
eigenständige Properties. Einfache,
logisch zusammengehörige Hilfsproperties ohne Attribute dürfen ohne Leerzeile
gruppiert werden, auch wenn eine tatsächlich notwendige einzeilige Summary
davorsteht. Selbsterklärende Getter-only-Properties benötigen keinen Kommentar.

Kompakte Properties dürfen in einer Zeile stehen. Kurze Methoden dürfen einen
Expression Body verwenden, aber `=>` steht dann **eingerückt am Anfang der
nächsten Zeile**. Blockmethoden verwenden die Klammerregel oben.

```csharp
public bool IsHistorical => Mode == ScoringMode.Historical;
public bool IsLiveScoring => Mode == ScoringMode.Live;

private bool HasValidScore(PlayerScore player)
    => player.Score >= 0;

private void ResetScores()
    => Players.Clear();

private void RefreshScores() {
    foreach (PlayerScore player in Players)
        player.RefreshScore();
}
```

Parameter von Methoden und Konstruktoren bleiben grundsätzlich auf einer Zeile.
Ein manueller Zeilenumbruch ist erlaubt, wenn er die Lesbarkeit verbessert,
beispielsweise bei einem langen letzten Argument. **Keine Regel „ein Parameter
pro Zeile“ und keine starre maximale Zeilenlänge.** Rein visueller Zeilenumbruch
im Editor ändert die Quelldatei nicht.

### 5.3 Zustand und Felddeklarationen

Auto-Properties bevorzugen, wenn kein explizites Backing-Field erforderlich ist:
`public Guid PlayerId { get; private set; }` statt eines privaten Feldes mit
bloßem weiterleitendem Getter. Bei konkretem Bedarf bewusst zwischen
Auto-Property, `field`, Toolkit-generierter Observable Property und explizitem
Backing-Field entscheiden; keine pauschale Ersetzung von Feldern.

Bei ViewModels klassische Konstruktoren mit sichtbar deklarierten `readonly`-
DI-Feldern bevorzugen. Primary Constructors können bei sehr kleinen,
übersichtlichen Klassen ohne nennenswerte weitere Zustandsverwaltung passend sein.
Positional Records erfüllen eine andere, datenorientierte Rolle. Keine starre
Grenze nach Dateilänge.

Nur für die UI benötigte **einfache boolesche Negationen** bevorzugt über einen
vorhandenen invertierenden Bool-Converter im XAML ausdrücken, statt eine zusätzliche
ViewModel-Property wie `IsIdle => !IsBusy` anzulegen. Eine abgeleitete Property
beibehalten, wenn sie eine eigenständige, verständliche Bedingung ausdrückt,
insbesondere wenn dieselbe zusammengesetzte Bedingung mehrfach gebraucht wird
(z. B. `CanEdit => !IsBusy && !IsSaveCompleted`). Keine unnötige Verdopplung
unabhängig gespeicherter Zustände. Vor dem Entfernen oder Umbenennen einer Property
alle Bindings und Änderungsbenachrichtigungen prüfen.

Ein `IsBusy`-Zustand darf verschiedene laufende Operationen kennzeichnen;
der konkrete Scope muss dem tatsächlichen Dialogverhalten entsprechen. Ein
Kommentar kann den Schutz vor parallelen Vorgängen und die Rücksetzung auch
nach Fehlern erklären, wenn das nicht ohnehin eindeutig ist.

Projekt-Typnamen über `using` einbinden, sofern kein Konflikt besteht. Keine festen
Zeilengrenzen oder zusätzliche Wrapper nur zur optischen Aufteilung.

Moderne C#-Features verwenden, wenn sie die Absicht klarer ausdrücken und zum
bestehenden Stil passen. Unberührten Code nicht nur zur Syntaxmodernisierung ändern.

## 6. XAML und Bindings

Einfache Elemente bevorzugt vollständig auf einer Zeile schreiben. Umfangreiche
Elemente auf wenige logisch gruppierte Attributzeilen verteilen, etwa
Darstellung/Layout, Bindings/Zustand, Interaktion und Accessibility. Ein Attribut
pro Zeile bleibt eine seltene Ausnahme für außergewöhnlich komplexe Elemente.
Keine starre maximale Zeilenlänge und keine pauschale Umformatierung aller Views.

`.editorconfig` legt aktuell unter anderem Encoding, Zeilenenden und C#-
Einrückung/Namensregeln. Die semantische Gruppierung von XAML-Attributen und die
Auswahl passender UI-Parameter bleiben Reviewaufgaben; dafür keinen zusätzlichen
Formatter ohne konkreten Bedarf einführen.

Ist das betroffene UI-Modell bereits der `BindingContext`, bei Event- und
Commandparametern bevorzugt das Objekt mit `{Binding .}` übergeben. Nicht nur
eine ID weiterreichen und dasselbe Objekt ohne fachlichen Grund erneut suchen.
Prüfungen auf Collection-Zugehörigkeit, Objektidentität, Zugriffsberechtigung und
Nebenläufigkeit erhalten. IDs bleiben an Navigations-, Persistenz- und sonstigen
Schichtgrenzen sinnvoll.

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

### Einheitliche Editierdialoge

Vergleichbare Editierdialoge nutzen denselben Grundablauf: Ein XAML-Command löst
im ViewModel die Validierung und Speicherung über Application-Use-Cases aus.
Nach erfolgreichem Speichern fordert das ViewModel über den gemeinsamen
`INavigationService` die Navigation an. Eine Callback-Kette
Page → ViewModel → Page (`SavedAsync`) ist dafür nicht erforderlich.

Ein erfolgreich abgeschlossener Schreibvorgang bleibt auch bei einem Fehler der
anschließenden Navigation abgeschlossen. `IsSaveCompleted` sperrt erneute
Bearbeitung und verhindert weitere Writes, erlaubt aber einen neuen
Navigationsversuch. Busy-/CanExecute-Schutz, Logging und verständliche Fehler
umfassen den tatsächlichen Ablauf. `try/catch/finally` bleibt sinnvoll; eine
zusätzliche konkurrierende Busy-Verwaltung in der Page ist zu vermeiden.

MAUI-spezifischer Lifecycle und Framework-Ereignisse wie `OnAppearing` und
`OnBackButtonPressed` bleiben in `.xaml.cs`. Dort ausgelöste Aktionen dürfen an
ViewModel-Commands weitergegeben werden; die synchronen Rückgabebedingungen
des Frameworks und sichere asynchrone Ausführung sind zu berücksichtigen.
Navigation ist kein Vorwand, beliebige UI-Control-Manipulationen oder
Dialogpersistenz in den Navigationsservice zu verschieben. Die Schichtgrenze
ist in `ARCHITECTURE.md` definiert.

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
