# Boardgame Tracker – Code Guidelines

## 1. Geltungsbereich und Priorität

Dieses Dokument ist die kanonische Quelle für **C#- und XAML-Stil, Benennung,
Lesbarkeit, ViewModels, Async-Verhalten, Kommentare und Fehlerbehandlung**. Es
beschreibt den gewünschten Stil für bestehenden und neuen Code; es ist **kein
pauschaler Auftrag**, unberührte Dateien zu ändern.

- **`.editorconfig`** definiert die maschinenprüfbaren Formatierungs- und
  Naming-Optionen. Hier stehen ihre beabsichtigte Anwendung, Beispiele und
  die Regeln, die sich nicht zuverlässig maschinell ausdrücken lassen.
- **`ARCHITECTURE.md`** definiert Schichten, Abhängigkeiten, Rollen und
  Navigationsverträge. **`UI_DESIGN_GUIDE.md`** definiert Design/Interaktion,
  **`UI_WIREFRAME.md`** konkrete Screens.
- **`../AGENTS.md`** regelt Auftragsumfang, Arbeitsweise und Verifikation.

Bei vergleichbaren Aufgaben zuerst die zuständige kanonische Regel anwenden,
danach eine bereits **regelkonform** gelöste Stelle mit derselben Verantwortung als
Orientierung verwenden. Ein altes Beispiel ersetzt keine heutige Regel. Gleiche
Probleme möglichst gleich lösen; unterschiedliche Anforderungen nicht in eine
künstlich einheitliche Schablone zwingen.

### Leseschlüssel: verbindliche Regeln und illustrative Beispiele

**Verbindliche Regeln** stehen im erklärenden Text der jeweiligen Abschnitte.
„Muss“ und „nicht“ benennen verbindliche Vorgaben; „bevorzugt“ beschreibt eine
Regel mit begründbaren Ausnahmen, „darf“ eine erlaubte Möglichkeit. Fachliches
Verhalten und Architekturverträge werden nicht durch Stilbeispiele festgelegt,
sondern durch ihre jeweils zuständigen kanonischen Dokumente.

**Beispiele (nicht als Implementierungsauftrag verstehen):** C#- und XAML-
Codeblöcke in diesem Dokument veranschaulichen die unmittelbar erläuterten
Stil-, Struktur- oder Verhaltensregeln. Sie sind weder vollständig zu kopierende
Implementierungen noch eine Liste verpflichtender Typen, Properties, Commands
oder Methodennamen. Sie behaupten insbesondere **nicht**, dass die gezeigten
APIs bereits im Repository existieren oder überall neu anzulegen sind.
Beispielnamen wie `RefreshPositionsAndMoveAvailability()`,
`GamesAvailableToAdd` und `OpenGameSelectionCommand` nur verwenden, wenn
sie zur tatsächlichen Verantwortung, den Aufrufern und vorhandenen Verträgen
passen. Ein Beispiel kann bewusst nur einen relevanten Ausschnitt zeigen;
nicht dargestellte Fälle sind dadurch nicht automatisch verboten oder erledigt.

**Lesereihenfolge:** Zuerst die Regel und ihren Geltungsbereich verstehen, dann
am gekennzeichneten Beispiel das gemeinte Muster erkennen, zuletzt im
tatsächlichen Code die benötigten Varianten und Randfälle prüfen. Wenn ein
Beispiel scheinbar mehr fordert als die Regel, gilt die ausdrücklich formulierte
Regel. Bei Widersprüchen zu einem kanonischen Fach-/Architekturvertrag die
zuständige Spezifikation beachten, nicht das Beispiel verallgemeinern.

## 2. Namen und Verantwortlichkeiten

Namen sollen erkennen lassen, **was** ein Zustand beschreibt oder **welche** Aktion
ausgeführt wird – möglichst ohne die Implementierung öffnen zu müssen.

- Methoden bevorzugen **Verb + Gegenstand**, etwa `LoadCorrectionDraftAsync()` oder
  `RefreshPositionsAndMoveAvailability()`. Kurze Namen wie `SaveAsync()` sind in
  einem eindeutig benannten Editor sinnvoll. Asynchrone Methoden enden auf `Async`.
- Fachliche IDs eindeutig benennen (`groupId`, `playerId`, `gameSessionId`), keine
  unnötigen Abkürzungen oder konkurrierenden Begriffe für denselben Sachverhalt.
- Bei mehreren DI-Abhängigkeiten Rolle und Typ erkennen lassen, z. B.
  `_editGroupUseCase`, `_gamesQuery`, `_navigationService`, nicht `_activation`
  oder ein mehrdeutiges `_useCase`.
- Technische Dialog- und Formularzustände (`IsBusy`, `IsSaveCompleted`,
  `IsCreateMode`) klar von fachlichen Daten (`PlayerId`, `PlayerName`,
  `IsPlayerGuest`, `IsPlayerArchived`, `GroupName`, `IsGroupArchived`)
  unterscheiden. `IsNew` ist beispielsweise unklar, wenn eigentlich der
  **Erstellmodus des Editors** gemeint ist, nicht eine Eigenschaft des Spielers.
- Fachliche Properties benennen ihren Gegenstand, wenn sie sonst mehrdeutig
  wären; dies **konsistent in vergleichbaren Dialogen** anwenden. `IsArchived`
  lässt offen, *was* archiviert ist; `IsPlayerArchived` bzw.
  `IsGroupArchived` machen es deutlich. Das `Is`-Präfix allein ist keine
  fachliche Gruppierung. Nicht blind jedem Member ein Präfix geben, sondern
  dessen Verantwortung erkennbar machen.
- `IsBusy` bezeichnet allgemein einen gerade laufenden Vorgang; keine
  künstlichen tätigkeitsbezogenen Busy-Namen pro Dialog verlangen.
  `IsSaveCompleted` bezeichnet dagegen einen erfolgreich abgeschlossenen Write,
  unabhängig vom Ergebnis der anschließenden Navigation.
- Interne Umbenennungen vollständig in Aufrufern und Bindings nachziehen; keine
  Compatibility-Aliase allein für alte interne Namen.

Verbindliche Domänenbegriffe und Rollen stehen in `ARCHITECTURE.md`. Die formale
Groß-/Kleinschreibung und Feldpräfixe stehen in `.editorconfig`.

## 3. Lernorientierter Code und Kommentare

Lesbarer, idiomatischer Produktionscode hat Vorrang vor möglichst kurzer oder
besonders cleverer Syntax. **Kommentare erklären ausdrücklich das Warum**, nicht
nur das *Was*: Welches Problem verhindert diese Entscheidung, welche Invariante
schützt sie, welche Konsequenz hätte ihr Weglassen? Ein guter Name erklärt die
Aufgabe; ein sinnvoller Kommentar ergänzt den nicht offensichtlichen Grund.

Besonders bei ungewöhnlichen Zuständen, Retry- und Fehlerpfaden, zeitlichen
Abläufen, Transaktionsgrenzen oder Framework-/Provider-Einschränkungen die
Begründung **nicht für bloße Kürze opfern**. Ein etwas längerer, präziser
Kommentar ist besser als ein kurzer Kommentar, der die entscheidende
Information weglässt. Offensichtliche Getter und selbsterklärende Anweisungen
brauchen dagegen keine Kommentare.

**Beispiel (nur für einen Dialog mit Save-/Navigations-Retry):** Ein
notwendiger Warum-Kommentar an einem nicht offensichtlichen Zustand; keine
generelle Kommentarpflicht für jeden Speichervorgang:

```csharp
/// <summary>
/// Set to true after a successful save to preserve the completed state if navigation fails.
/// This prevents further editing or saving while the dialog remains open,
/// allowing the user to retry leaving without modifying the saved player.
/// </summary>
[ObservableProperty]
public partial bool IsSaveCompleted { get; private set; }
```

Dieser Kommentar erklärt **nicht nur**, wann der Zustand gesetzt wird, sondern
**warum** er trotz fehlgeschlagener Navigation bestehen bleiben muss und welches
Fehlverhalten er verhindert. Eine kürzere Form ist nur besser, wenn sie alle
diese Aussagen erhält. Hilfreiche Kommentare bei Refactorings erhalten und an
geändertes Verhalten anpassen; nicht allein zur Reduktion von Zeilen entfernen.

XML-`<summary>` steht **vor** den Attributen der dokumentierten Deklaration.
Eine kurze Erklärung darf einzeilig, eine begründende Erklärung mehrzeilig sein.
XML-Dokumentation gezielt an nicht offensichtlichen Zuständen und zentralen
Verträgen, nicht flächendeckend bei trivialen Properties, verwenden.

Nicht offensichtliche Rückgabebedeutungen – beispielsweise `bool`, das zwischen
fehlgeschlagenem Speichern, blockiertem Aufruf und bereits abgeschlossenem Save
unterscheidet – gezielt in XML-`<returns>` erklären. Framework-Overrides mit
einer kurzen XML-`<summary>` versehen, wenn Aufrufgrund und Zweck nicht aus
dem Methodennamen hervorgehen (z. B. `OnBackButtonPressed()` für System-Zurück
unabhängig vom Header-Button). Auch ungewöhnliche Guards und Early Returns
kommentieren, sofern ihr Grund ohne Kommentar unverständlich wäre.

**Beispiel (nur bei entsprechender Lifecycle-Gefahr):** Ein Warum-Kommentar
an einem MAUI-Override, dessen erneuter Aufruf ungespeicherte Eingaben
überschreiben könnte:

```csharp
/// <summary>
/// MAUI calls this when the page appears or resumes; dialog transitions may
/// also trigger it. Skip loading while a dialog is open, or unsaved form
/// changes could be overwritten.
/// </summary>
protected override async void OnAppearing() {
    base.OnAppearing();
    if (AppDialogPage.IsOpen(this))
        return;

    if (_groupId != Guid.Empty)
        await _viewModel.LoadAsync(_groupId);
}
```

Der Kommentar begründet die **zusätzliche Lifecycle-Prüfung**; er beschreibt
nicht bloß, dass `OnAppearing()` beim Anzeigen einer Seite aufgerufen wird.

### Bewusste Architekturabstraktionen für Einsteiger dokumentieren

Bewusst eingeführte Abstraktionen an Schicht- und Framework-Grenzen (Interfaces,
Ports, Adapter) **direkt am zentralen Vertrag** so erklären, dass Einsteiger
Verantwortung und Entscheidung nachvollziehen können. Die Dokumentation soll
das **Warum**, die beteiligten Rollen und den konkreten Nutzen vermitteln,
ohne denselben Gedanken mehrfach auszuführen.

- `<summary>` beschreibt kurz **warum** die Abstraktion existiert, **wer**
  die technische Umsetzung übernimmt und **welchen Nutzen** die Trennung hat.
- `<example>` zeigt bei Bedarf einen **echten Aufruf**, statt die Begründung zu
  wiederholen. Bei trivialer Verwendung ist es nicht nötig.
- `<remarks>` nur für **zusätzliche wichtige Informationen**, nicht als
  längere Wiederholung der Summary. Alternativen, Kosten und ausführliche
  Architekturentscheidungen gehören in `ARCHITECTURE.md`.

**Beispiel (illustrativer Navigationsvertrag):** Kurz, aber mit vollständiger
Begründung. Verbindliche Operationen und deren Verantwortung legt
`ARCHITECTURE.md` fest, nicht dieser Stilcode.

```csharp
/// <summary>
/// Lets ViewModels request navigation without depending on MAUI Shell.
/// A Shell adapter performs the navigation, while tests can replace
/// this interface with a fake to verify navigation without a running UI.
/// </summary>
/// <example>
/// After a successful save: <c>await _navigationService.GoBackAsync();</c>
/// </example>
public interface INavigationService {
    Task GoBackAsync();
    Task NavigateToMainSectionAsync(AppMainSection section);
}
```

- **Summary, Satz 1 – Warum:** Keine direkte Kopplung des ViewModels an
  MAUI Shell.
- **Summary, Satz 2 – Zuständigkeit und Nutzen:** Der Adapter navigiert;
  Tests können dieselbe Schnittstelle ohne laufende Oberfläche ersetzen.
- **Example – Anwendung:** Ein konkreter Aufruf nach erfolgreichem Speichern,
  ohne die Vorteile erneut zu erklären.

`<example>` ist ein reguläres C#-XML-Dokumentationstag, **kein Teil der Summary**.
Die Summary erscheint in der üblichen IntelliSense-Kurzinformation; ob ein
separates Example dort zusätzlich angezeigt wird, hängt vom Editor ab.
Wichtige Gründe gehören deshalb in `<summary>`, nicht ausschließlich in
`<example>`.

Die Trennung macht die **Navigation**, nicht automatisch sämtliche anderen
ViewModel-Abhängigkeiten, unabhängig von MAUI-Navigations-APIs. Eine kurze
Notiz bei der DI-Registrierung darf eine nicht offensichtliche Adapterwahl
erklären. Die Begründung nicht an jeder Aufrufstelle wiederholen und keine
Interfaces allein für ein Architekturbeispiel einführen.

C#-Bezeichner und Code-/XML-Kommentare sind **Englisch**; Projektdokumentation ist
**Deutsch**.

## 4. Dateien, Klassen und Methoden gliedern

**Klassen:** Bevorzugte Lesereihenfolge in ViewModels: explizite `readonly`-
DI-Felder, Konstruktor, eigener Zustand/Properties, anschließend Methoden entlang
des fachlichen Ablaufs. Technischen Dialogzustand und Formularmodus (etwa
`IsBusy`, `IsSaveCompleted`, `IsCreateMode`) von fachlichen Spielerdaten
(`PlayerId`, `PlayerName`, `IsPlayerGuest`, `IsPlayerArchived`) trennen;
abgeleitete Bedingungen und Collections **erkennbar nach Verantwortung**
gruppieren. Eine rein berechnete, unmittelbar zugehörige Negation
`IsNotBusy => !IsBusy` steht **direkt unter `IsBusy`, ohne Leerzeile**.
`CanEdit` oder ein fachlich eigenständiges `CanSave` stehen bei verwandtem
Prozesszustand; keine starre Sortierung nach Sichtbarkeit, Typ oder Alphabet.
Zusammengehörige Hilfsmethoden nahe beim zugehörigen Ablauf lassen.

**Methoden:** Auch **innerhalb** einer Methode die Schritte in logischer,
fachlicher und zeitlicher Reihenfolge anordnen: Vorbedingungen → Daten laden →
Ergebnis prüfen → zusammengehörige Eigenschaften übernehmen → Collections
aufbauen → erfolgreich abschließen. Unabhängige Phasen mit Leerzeilen trennen,
ohne Überschriftenkommentare für Selbstverständliches. Die Reihenfolge niemals
allein kosmetisch ändern, wenn Fehler-, Nebenläufigkeits- oder Zustandssemantik
betroffen ist. Beispielsweise eine Kennung, die "bereits geladen" signalisiert,
erst nach **vollständig erfolgreichem** Laden setzen. Bei Lademethoden
beispielsweise `IsBusy = true;` und `ErrorMessage = null;` als Prozessbeginn
zusammenhalten, dann eine Leerzeile vor dem **zusammenhängenden** Laden und
Übernehmen der fachlichen Werte. Zwischen der geladenen `Player`-Variablen
und der anschließenden Zuweisung von `PlayerName` ohne sachlichen Grund
keine Leerzeile setzen.

Kleine Hilfsmethoden bei ihrem zugehörigen Ablauf halten. Methoden mit mehreren
zusammengehörigen Nebenwirkungen entsprechend benennen; beispielsweise ist
`RefreshPositionsAndMoveAvailability()` aussagekräftiger als `UpdatePositions()`,
wenn gleichzeitig Position und Verschiebeberechtigungen aktualisiert werden.

Kleine, nur einem Slice gehörende Inputs, Results, Records und Hilfstypen
bevorzugt beim Haupttyp lassen. Eine eigene Datei ist sinnvoll bei
Wiederverwendung, eigenständigem Vertrag oder relevantem unabhängigen
Änderungsgrund. Öffentliche Verträge und Extension Points separat halten;
keine künstlichen Ordner oder Partials nur zum Verkürzen einer Datei.

`#region` ist zur Navigation erlaubt, aber ersetzt keine klare Verantwortung.
Für lokalisierte Titel und Buttontexte z. B. `#region Localized UI Text` mit
`SaveText`; diese Region darf dort stehen, wo sie den Lesefluss am besten
unterstützt, nicht zwingend am Dateiende. Keine künstlichen `partial`-Dateien
oder verschachtelten Typen nur für einfache UI-Texte anlegen.

## 5. C#-Stil und Kontrollfluss

Die maschinenprüfbaren Formatierungs- und Naming-Optionen stehen in
`.editorconfig`. Es gelten **vier Leerzeichen**, keine Tab-Zeichen, und
öffnende geschweifte Klammern bei Typen, Methoden und Kontrollstrukturen auf
derselben Zeile (**K&R**). Schließende Klammern stehen normalerweise allein;
direkt zusammengehörige Fortsetzungen stehen jedoch **auf derselben Zeile**:
`} else`, `} catch` und `} finally`. Kompakte Property-Accessors sind eine
weitere Ausnahme. Die folgenden Beispiele legen zudem
fest, wie wir die Regeln anwenden, wenn technische Editoroptionen nicht alle
Lesbarkeitsfälle ausdrücken können.

- Typen überwiegend **explizit** schreiben (`string`, `bool`, `Guid`, Enums,
  übersichtliche Modelltypen); `var` nur, wenn es für komplexe Typen lesbarer oder
  erforderlich ist. Keine rein mechanische `var`-Umstellung.
- Bei `if`, `foreach` und vergleichbaren Kontrollstrukturen mit **genau einer
  einfachen Anweisung** gewöhnlich **keine** Klammern; die Anweisung steht
  eingerückt auf einer eigenen Zeile. Bei **mehreren Anweisungen** einen
  mehrzeiligen Klammerblock verwenden. Bei verschachtelten Kontrollstrukturen
  dürfen Klammern die Zuordnung von `else` und den Ablauf verdeutlichen.
- **Eine Anweisung je Zeile.** Guards nach unterschiedlichen Gründen trennen,
  zusammengehörige triviale Bedingungen dürfen verbunden bleiben. Eine einfache
  Anweisung nach `if` oder `foreach` folgt **auf der nächsten eingerückten
  Zeile**, nicht direkt hinter der Bedingung.
- **Early Returns** für erwartete Abbruchbedingungen bevorzugen, wenn sie den
  Ablauf abflachen. Unabhängige Validierungs-Guards mit Rückgabe nicht künstlich
  zu `else if`-Ketten verbinden; `if`/`else` ist für echte gegenseitig
  ausschließende Alternativen (etwa Erstellen oder Aktualisieren) sinnvoll.
  Unerwartete Fehler nicht stillschweigend als erfolgreichen No-op behandeln.
  Ein lesbares einzeiliges `?? throw` ist für einen unerwarteten fehlenden
  Pflichtwert erlaubt; keine Pflicht, dafür einen künstlichen Mehrzeilenblock
  zu bauen. Eine existierende Vertrags-/Fehlergrenze nutzen, statt Validierung
  in jeder Schicht zu duplizieren.
- `else`, `catch` und `finally` folgen direkt der schließenden Klammer des
  vorherigen Blocks (`} else`, `} catch`, `} finally`). Bei je **einer einfachen
  Anweisung** im `if`- und `else`-Zweig bleiben beide Zweige dagegen ohne
  Klammern; `try`, `catch` und `finally` benötigen immer ihre Blöcke.
- Gemeinsame Abschlussschritte nicht unnötig in mehreren Branches duplizieren.
  Beispiel: Bei bereits erfolgreichem Save nur den Write überspringen, danach
  denselben Navigationsaufruf verwenden.
- Ternäre Ausdrücke nur für offensichtliche **Wertauswahl**; keine komplexen
  `if`-Bedingungen oder Auswahl unterschiedlicher (insbesondere asynchroner)
  Methodenaufrufe per `?:`. Benannte boolesche Bedingungen nur bei echtem Gewinn.
- Boolesche Parameter, die wesentlich unterschiedliche Abläufe steuern,
  kritisch prüfen; keine zusätzlichen Methoden/Delegates ohne Lesbarkeitsgewinn.

**Beispiele (nur C#-Form und Kontrollfluss):** Sie zeigen zulässige
Klammerformen und einen verschachtelten Fall, in dem Klammern die Lesbarkeit
verbessern. Die genannten Methoden, Zustände und Abläufe sind keine
Implementierungsvorgaben:

```csharp
// One simple statement: no braces.
foreach (Player player in Players)
    player.RefreshScore();

// Multiple statements: braces required.
foreach (Player player in Players) {
    player.RefreshScore();
    player.RefreshRank();
}

// One statement per branch: no braces for if/else.
if (IsCreateMode)
    await CreatePlayerAsync();
else
    await UpdatePlayerAsync();

// Multiple statements per branch: else follows the closing brace.
if (IsCreateMode) {
    ResetDraft();
    ShowCreateFields();
} else {
    LoadExistingData();
    ShowEditFields();
}

// try/catch/finally always use blocks, with adjacent keywords on the same line.
try {
    await SavePlayerAsync();
} catch (Exception exception) {
    _logger.LogError(exception, "Could not save player.");
} finally {
    IsBusy = false;
}

// Braces also clarify a nested if/else structure.
if (IsHistorical) {
    if (CanSave)
        await SaveCorrectionAsync();
    else
        ShowValidationError();
}
```

Die erläuternden Zeilenkommentare in diesem **Guideline-Beispiel** dienen dem
Lernen; solche Kommentare sind im selbsterklärenden Produktionscode nicht nötig.

### Attribute, Properties und Konstruktoren

Attribute stehen **oberhalb** der Deklaration, ohne Leerzeile dazwischen.
Parameterlose Attribute dürfen in einer Zeile gruppiert werden; parametrisierte
bevorzugt einzeln und mit ihren Parametern in derselben Zeile. Die XML-Summary
steht **vor** den Attributen.

```csharp
[ObservableProperty, JsonIgnore]
[NotifyPropertyChangedFor(nameof(IsHistorical), nameof(IsLiveScoring))]
public partial ScoringMode Mode { get; private set; }
```

Zwischen eigenständigen Properties und Methoden eine Leerzeile lassen;
zusammengehörige kompakte, nicht annotierte Hilfsproperties dürfen direkt
beieinander stehen. Berechnete Properties können einzeilig sein;
**expression-bodied Methoden** setzen `=>` eingerückt an den Anfang der
nächsten Zeile. Blockmethoden folgen dem Klammerstil.

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

Parameterlisten – auch **DI-Konstruktoren** – grundsätzlich auf einer Zeile
schreiben. Nur umbrechen, wenn ein Umbruch die Lesbarkeit tatsächlich verbessert;
**keine starre oder versteckte maximale Zeilenlänge**, kein willkürlicher
Umbruch nach dem zweiten Parameter und keine Pflicht "ein Parameter pro Zeile".
Ein längerer, aber noch übersichtlicher Konstruktor darf vollständig auf einer
Zeile stehen. Projekt-Typen über `using` einbinden, außer bei Namenskonflikten.

Auto-Properties verwenden, wenn kein explizites Backing-Field nötig ist,
z. B. `public Guid PlayerId { get; private set; }` statt einer privaten
Variablen mit bloß weiterleitendem Getter. Zwischen Auto-Property,
`[ObservableProperty]`, `field` und explizitem Backing-Field entsprechend dem
tatsächlichen Benachrichtigungs- und Kapselungsbedarf entscheiden; nicht
pauschal umstellen. ViewModels bevorzugen klassische Konstruktoren mit
sichtbaren `readonly`-DI-Feldern; Primary Constructors können sehr kleine,
übersichtliche Typen vereinfachen. Positional Records sind datenorientierte Typen,
kein Ersatz für jedes ViewModel.

Moderne C#-Features verwenden, wenn sie Absicht und Lesbarkeit verbessern;
keine Modernisierung unberührter Stellen allein wegen neuer Syntax.

## 6. ViewModels, Commands und Collections

### Zustände, CanExecute und Bindings

`IsBusy` beschreibt allgemein, dass gerade gearbeitet wird und keine
konkurrierende Aktion beginnen soll. Die Guards und `finally`-Blöcke müssen
alle tatsächlich geschützten Schritte abdecken; für jeden Dialogschritt einen
eigenen Busy-Namen zu erfinden ist nicht nötig.

Brauchen mehrere Commands dieselbe Voraussetzung `!IsBusy`, reicht **eine**
private berechnete Property `IsNotBusy`, unmittelbar unter `IsBusy` **ohne
Leerzeile**. Nicht für jeden Command bedeutungsgleiche Aliase (`CanSave`,
`CanGoBack`) schaffen. Es entsteht keine zweite Zustandsquelle. Fachlich
zusätzliche Voraussetzungen erhalten eine eigenständige berechnete Bedingung,
z. B. `CanEdit => IsNotBusy && !IsSaveCompleted` oder
`CanSaveAndAddAnother => IsNotBusy && IsCreateMode`.

Das `CanExecute`-Attribut und der **interne Guard desselben Commands** müssen
die **identische Bedingung** verwenden: bei `nameof(IsNotBusy)` den Guard
`if (!IsNotBusy)` (mit `return` in der Folgezeile), nicht `if (IsBusy)`;
bei `nameof(CanSaveAndAddAnother)` entsprechend `if (!CanSaveAndAddAnother)`.
Ein interner Guard bleibt sinnvoll, weil Methoden auch außerhalb der UI
aufgerufen werden können. Bei unterschiedlichem Schutzbedarf müssen die
Bedingungen bewusst begründet werden, nicht unbemerkt auseinanderlaufen.

`CanExecute` ist **nicht für jeden** `[RelayCommand]` verpflichtend. Ist eine
Aktion ausschließlich in einem bereits durch `CanEdit` gesperrten XAML-Bereich
verfügbar, können zusätzliche Command-Bedingungen dieselbe UI-Sperre nur
duplizieren. Item-spezifische Bedingungen wie `CanMoveUp`, `CanMoveDown` oder
`CanChange` dürfen am jeweiligen Steuerelement gebunden sein. Mutierende
Commands behalten ihre erforderlichen internen Guards, weil ein XAML-`IsEnabled`
keine programmgesteuerten Aufrufe absichert. Ein harmloser Schließen-Command,
der keine Fachdaten ändert, braucht nicht allein aus Symmetrie einen Busy-Guard.
`CanExecute` verwenden, wenn der **Command selbst** unabhängig von seiner
aktuellen View-Verwendung deaktivierbar sein muss; ist es gesetzt, müssen
Command-Bedingung und interner Guard übereinstimmen.

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(GoBackCommand), nameof(SaveAndAddAnotherCommand))]
[NotifyPropertyChangedFor(nameof(CanEdit))]
public partial bool IsBusy { get; private set; }
private bool IsNotBusy => !IsBusy;

public bool CanEdit => IsNotBusy && !IsSaveCompleted;

[RelayCommand(CanExecute = nameof(IsNotBusy))]
private async Task GoBackAsync() {
    if (!IsNotBusy)
        return;

    await _navigationService.GoBackAsync();
}
```

Dieses Beispiel veranschaulicht ausschließlich **gemeinsame Command-Sperre
und Guard**; die fachliche Zurück-Navigation eines konkreten Editors kann
abweichen (z. B. nach einem abgeschlossenen Anlegen).

Für einfache Negationen in **XAML** weiterhin `IsBusy` mit dem vorhandenen
`BoolNegationConverter` binden, keine öffentliche `IsNotBusy`-Property samt
zusätzlicher PropertyChanged-Benachrichtigung schaffen. Fachlich eigenständige
Zustände wie `CanEdit` bleiben separat gebunden.

`[RelayCommand(CanExecute = nameof(IsNotBusy))]` ruft die Bedingung ab,
**beobachtet ihre Abhängigkeiten aber nicht automatisch**. Die verursachenden
Observable-Properties müssen den Command mittels
`[NotifyCanExecuteChangedFor(nameof(SaveCommand))]` benachrichtigen;
`[NotifyPropertyChangedFor(nameof(CanEdit))]` allein ist **kein Ersatz**.
Zusätzliche Abhängigkeiten ebenfalls melden: `IsCreateMode` muss bei Änderungen
`SaveAndAddAnotherCommand` benachrichtigen, `IsSaveCompleted` die berechnete
Property `CanEdit`. Bei Collection-basierten Bedingungen Benachrichtigungen
nur für **tatsächlich mögliche und relevante** Collection-Änderungen einrichten.
Manuelle `SaveCommand.NotifyCanExecuteChanged()`-Aufrufe nicht duplizieren,
wenn ein garantierter `IsBusy`-Wechsel die Prüfung bereits anstößt.

Bei abgeschlossenen Writes muss der Command die **Navigation erneut versuchen
können**, auch wenn ursprüngliche Formularbedingungen nicht mehr erfüllt sind.
Eine berechnete `CanSave`-Bedingung kann das ausdrücklich abbilden, ohne eine
zweite Speicherung zu erlauben.

### Ein Konzept für gleichartige Dialoge

Vergleichbare Editierdialoge verwenden einen verständlichen Ablauf:
**XAML-Command → ViewModel validiert und speichert → ViewModel fordert Navigation
über `INavigationService` an**. Der Vertrag, seine Begründung und die
Shell-Implementierung gehören zur Architektur (`ARCHITECTURE.md`). Keine
`SavedAsync`-Rückrufkette Page → ViewModel → Page nur zur Navigation.

`IsSaveCompleted` wird **nach erfolgreichem Schreiben** gesetzt. Schlägt danach
Navigation fehl, bleibt der Write abgeschlossen und ein erneuter Navigationsversuch
führt **keinen** weiteren Write aus. Busy-/CanExecute-Schutz, Fehlerbehandlung
und `finally` decken den gesamten jeweiligen Ablauf ab; keinen zweiten
konkurrierenden Sperrzustand in der Page einführen, wenn er sich vermeiden lässt.

**Methodenname und tatsächliche Aufgabe müssen zusammenpassen:** Ein
`GoBackAsync()` sollte nicht `SaveAsync()` erneut aufrufen, **nur** um dessen
Navigationsanteil wiederzuverwenden. Den gemeinsamen Navigations-/Abschlussweg
bei Bedarf klar benennen und auslagern, ohne doppelte Navigation oder künstliche
Hilfsmethoden zu erzeugen. Ein Save-Retry darf weiterhin keinen weiteren Write
verursachen.

MAUI-Lifecycle, systemseitiges Zurück sowie konkrete Dialog- und View-Mechanik
bleiben in `.xaml.cs`. Framework-Ereignisse dürfen einen ViewModel-Command
auslösen; gewöhnliche UI-Aktionen sollen dagegen möglichst **direkt aus XAML**
gebunden werden. Keine Page-Handler nur als unnötige Weiterleitung zu einem
bereits vorhandenen Command. Für `OnBackButtonPressed()` die synchrone Rückgabe und den sicheren
asynchronen Folgeablauf beachten.

### Collection-Änderungen und abgeleitete Ansichten

`ObservableCollection<T>.CollectionChanged` meldet Änderungen an der **Liste**,
nicht automatisch Änderungen der **Eigenschaften ihrer Elemente**.
`INotifyPropertyChanged` eines Items signalisiert nur dessen Zustand; gefilterte
`IReadOnlyList`-Projections aktualisieren sich nicht selbst.

**Regel:** Collection-Abonnements und Count-abhängige `CanExecute`-
Prüfungen nur einrichten, wenn sich die betreffende Collection im tatsächlichen
Ablauf auf relevante Weise verändern kann. Bereits an einer verantwortlichen
Fach- oder Vertragsgrenze garantierte Invarianten nicht vorsorglich als
veränderbaren Formularzustand modellieren. Einen unerwartet ungültigen
Ladezustand gegebenenfalls **an der Ladegrenze** sichtbar behandeln.

**Beispiel (nur ein Editor mit feststehenden Gruppenmitgliedern):** Werden
vorhandene Mitglieder ausschließlich umsortiert und garantiert die Domain
mindestens ein Mitglied, braucht `SaveCommand` kein
`Players.CollectionChanged`-Abonnement für Hinzufügen/Entfernen und `CanSave`
keine Prüfung `Players.Count > 0`. Bei einem Editor, der Mitglieder tatsächlich
hinzufügt oder entfernt, ist die Lage anders; dort können beide Mechanismen
nötig sein. Fachliche Spielerzahlgrenzen, etwa bei der Spielzuordnung,
bleiben unabhängig davon bestehen.

Wenn Änderungen ausschließlich durch bekannte Commands erfolgen, die
abgeleiteten Ansichten **direkt nach einer tatsächlich erfolgreichen Mutation**
benachrichtigen. Ein Item-`PropertyChanged`-Abonnement ist sinnvoll, wenn Items
auch außerhalb dieser kontrollierten Aufrufe ihren relevanten Zustand ändern.
Dann An-/Abmeldung, Reset und Lebensdauer bewusst regeln; nicht jedes
irrelevante PropertyChanged zum Neuaufbau sämtlicher Ansichten nutzen.
Nach initialem Laden die Ansichten einmal aktualisieren. Wiederkehrende
Filteranforderungen dürfen Anlass für eine reaktive Lösung sein; keine eigene
Attribut-Infrastruktur oder zusätzliche Bibliothek nur für einen Einzelfall.

Eine persistierte Gruppenzuordnung und ein **bearbeitbarer Entwurf** sind
unterschiedliche Zustände. Namen dafür dürfen sich unterscheiden, müssen aber
den Unterschied ausdrücken; nicht `Selected`, `Assigned`, `Visible` und `Enabled`
unbemerkt als Synonyme mischen. Fachvokabular und Persistenz liegen in
`ARCHITECTURE.md` bzw. `DATA_MODEL.md`.

## 7. XAML und compiled Bindings

### Attributreihenfolge und Zeilenumbrüche

**Regel:** XAML-Elemente mit übersichtlichen Attributen bevorzugt vollständig
auf **einer Zeile** lassen. Es gibt **keine feste oder versteckte Obergrenze
für die Zeilenlänge** – auch nicht 120, 160 oder 200 Zeichen. Erst umbrechen,
wenn das Element durch Zahl oder Zusammenspiel seiner Attribute tatsächlich
schlechter lesbar wird; Editorbreite und automatische Anzeigeumbrüche sind
keine Begründung für feste Quelltextumbrüche.

Bei Bedarf in **wenige semantische Gruppen** gliedern: Grundkonfiguration
und Zustand (`x:Name`, `Grid.Row`, `IsEnabled`, `Style`), Inhalt (`Text`,
`Title`, `Source`), Interaktion (`Command` und `CommandParameter` oder
`Clicked`) sowie zusammengehörige Hilfetexte (`ToolTipProperties.Text`
und `SemanticProperties.Description`). Zusammengehöriges möglichst auf
derselben Attributzeile halten; insbesondere `Command` und
`CommandParameter` nicht ohne Lesbarkeitsgrund trennen. Das ist eine
**Orientierung, keine starre Reihenfolge oder Pflicht zum Umbruch**.
Kein Formatierer nur zur Erzwingung kosmetischer Zeilenlängen.

**Beispiele (nur Attributgliederung, keine vorgegebenen Controls/Commands):**

```xml
<header:AppPageHeader x:Name="PageHeader" Title="{x:Static res:AppResources.EditGroup}" Variant="Subpage" BackCommand="{Binding GoBackCommand}" />

<ImageButton Style="{StaticResource HeaderIconButton}" WidthRequest="48" HeightRequest="48" Source="icon_info.png"
    Clicked="OnInfoClicked"
    ToolTipProperties.Text="{x:Static res:AppResources.EditGroupHint}" SemanticProperties.Description="{x:Static res:AppResources.EditGroupHint}" />

<TapGestureRecognizer Command="{Binding Source={x:Reference Page}, Path=ViewModel.AddGameCommand, x:DataType=local:EditGroupPage}" CommandParameter="{Binding .}" />
```

### XAML-Abschnitte als Navigationshilfe gliedern

**Regel:** Größere verschachtelte XAML-Ansichten durch **aussagekräftige
Abschnittskommentare** gliedern. Anders als C#-Methoden lässt sich diese
Struktur nicht einfach in kleine benannte Funktionen zerlegen. Kommentare
dürfen ausreichend ausführlich sein, um Zweck und Inhalt des Bereichs
auf einen Blick zu erklären. Über- und Unterbereiche sollen in Anordnung
und Einrückung als Hierarchie erkennbar sein. Nicht jedes Grid oder Control
kommentieren und keine technisch nicht vorhandene Hierarchie suggerieren.

**Beispiel (nur schematische Gliederung, kein vollständiges Seitenlayout):**

```xml
<Grid>
    <!-- Page header: navigation, information and save action -->
    <header:AppPageHeader Title="{x:Static res:AppResources.EditGroup}" BackCommand="{Binding GoBackCommand}" />

    <!-- Editable group content: metadata, player order and game assignments -->
    <VerticalStackLayout>
        <!-- Group settings: edit name and archive state -->
        <Entry Text="{Binding GroupName}" />

        <!-- Players: display fixed members and adjust their default order -->
        <VerticalStackLayout BindableLayout.ItemsSource="{Binding Players}" />

        <!-- Games: review assignments and choose additional games -->
        <Button Command="{Binding ChooseGameCommand}" />
    </VerticalStackLayout>

    <!-- Game selection overlay: choose an additional game -->
    <Grid IsVisible="{Binding IsChoosingGame}" />

    <!-- Busy overlay: block interaction during an operation -->
    <Grid IsVisible="{Binding IsBusy}" />
</Grid>
```

**Strukturkommentare** dienen als Navigationshilfe und müssen kein besonderes
Warum erklären. Kommentare zu ungewöhnlichen Framework-Grenzen, Guards oder
anderen nicht offensichtlichen Entscheidungen erläutern weiterhin die
Begründung. Gezeigte Abschnittsnamen sind Beispiele, keine verpflichtenden
Bezeichner oder Layoutvorgaben.

Den **tatsächlichen BindingContext** beachten:

- **Außerhalb** eines `DataTemplate` ist auf dieser Page das ViewModel der
  BindingContext. Dort reicht `Command="{Binding OpenGameSelectionCommand}"`.
- **Innerhalb** eines `DataTemplate` ist der BindingContext das **einzelne Item**
  (`GroupGameSelection`), nicht das übergeordnete ViewModel. Item-Properties
  nutzen `{Binding Name}`; für einen ViewModel-Command explizit über die
  typisierte Page-Property auf das ViewModel zugreifen.
- Das bereits gebundene Item als Parameter bevorzugt mittels
  `CommandParameter="{Binding .}"` übergeben. Keine ID weiterreichen, um
  dasselbe Objekt unnötig erneut zu suchen. Nullbarkeit an der `ICommand`-Grenze,
  Collection-Mitgliedschaft, Zugriffsrechte und Nebenläufigkeit prüfen.
  IDs bleiben an Navigations- und Persistenzgrenzen sinnvoll.

**Beispiel (nur BindingContext-Wechsel):** Die hier illustrative Page
besitzt `x:Name="Page"` und eine öffentliche, typisierte `ViewModel`-
Property. Namen sind weder vorgegeben noch als bereits implementiert anzusehen:

```xml
<!-- Outside an item template: EditGroupViewModel is the BindingContext. -->
<Button Command="{Binding OpenGameSelectionCommand}" />

<!-- Inside the list: the GameSelection item is the BindingContext. -->
<CollectionView ItemsSource="{Binding GamesAvailableToAdd}">
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:GroupGameSelection">
            <Border>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding Source={x:Reference Page}, Path=ViewModel.AddGameCommand, x:DataType=local:EditGroupPage}" CommandParameter="{Binding .}" />
                </Border.GestureRecognizers>
                <Label Text="{Binding Name}" />
            </Border>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

Eine korrekt an `Command` gebundene MAUI-Schaltfläche übernimmt dessen
`CanExecute`-Zustand und wird bei `false` deaktiviert. Die gemeinsamen
Button-Styles stellen den Disabled-Zustand dar; eine zusätzliche `IsEnabled`-
Bindung nur zur Wiederholung derselben Command-Bedingung ist unnötig.
Eine übergeordnete Busy-Sperre darf in XAML weiterhin direkt `IsBusy` mit
`BoolNegationConverter` verwenden – nicht eine öffentliche `IsNotBusy`-Kopie.

Neue/geänderte Bindings als **compiled bindings** mit passendem `x:DataType`
für Page, Template und jeden BindingContext-Wechsel schreiben. `x:Reference`
über typisierte Page-/ViewModel-Properties auflösen. String-Bindings nur, wo
technisch nötig; keine EF-Entities/EF-Navigation-Properties direkt an XAML.
Projektweit aktiv sind `MauiStrictXamlCompilation`,
`MauiEnableXamlCBindingWithSourceCompilation` und `XC0022`–`XC0025` als
Buildfehler. Diese Prüfungen nicht durch `x:Object`, `x:Null`, `NoWarn` oder
deaktivierte Compileroptionen umgehen.

Gemeinsame semantische Styles aus `UI_DESIGN_GUIDE.md` verwenden; keine Layout- oder
Designwerte in diesen Guidelines festlegen.

## 8. Async, Fehler und Tests

ViewModel-Konstruktoren beschränken sich auf DI und einfachen synchronen Zustand;
keine Datenbankzugriffe, Navigation oder asynchrones Laden daraus. Laden über
Lifecycle-/Load-Methoden gegen unbeabsichtigte Mehrfachausführung schützen.
Asynchrone Commands, Use Cases, Persistenz und I/O vollständig `await`en;
`async void` ausschließlich für echte UI-/Framework-Eventhandler. Keine
Fire-and-forget-Tasks aus ViewModels oder Application-Use-Cases ohne ausdrücklich
definierte Lebensdauer und Fehlerbehandlung. Sequenzielle Abläufe nicht künstlich
parallelisieren; einen EF-`DbContext` niemals parallel verwenden. Vorhandene
`CancellationToken` bis zu EF-/I/O-Aufrufen durchreichen, nicht grundlos durch
`CancellationToken.None` ersetzen.

Exceptions nur an einer Stelle abfangen, die sie **sinnvoll behandeln, loggen
oder in eine verständliche UI-Meldung übersetzen** kann. Keine leeren `catch`-
Blöcke oder stillen Abbrüche bei unerwarteten Fehlern. Erwartete Zustände dürfen
Early Returns verwenden; ungültige Aufrufargumente schützen ihre
Vertragsgrenzen. Eine UI-Load-Methode kann Fehler im bestehenden `catch` loggen
und als `ErrorMessage` darstellen, statt dieselbe Prüfung mehrfach zu ergänzen.

Core und Application sind von MAUI `MainThread`, `Dispatcher` und anderen
UI-Thread-APIs unabhängig. UI-Marshalling nur im App-/Presentation-Layer und
nur bei echtem Bedarf, nicht zum Verbergen von Nebenläufigkeitsfehlern.

Bei Refactorings relevanter Dialogabläufe mindestens die bestehende Semantik
prüfen: erfolgreicher Write, Fehler vor dem Write, Fehler bei Navigation,
Navigation-Retry **ohne erneuten Write**, Busy-/Doppelaufrufschutz und
korrekte Collection-/Command-Benachrichtigungen. Testumfang und
Ausführungsfreigaben regelt `../AGENTS.md`.

## 9. Performance und Änderungsdisziplin

Keine Optimierung auf Verdacht. Bestehende strukturelle Regeln einhalten:
gezielte Read Models, kurze DbContext-Lebensdauer, compiled XAML bindings,
keine unnötigen Materialisierungen. Größere Optimierungen nur bei gemessenem
Problem oder ausdrücklichem Auftrag.

Refactorings **auf vergleichbare Stellen innerhalb des beauftragten Scopes**
übertragen, nicht blind jede Datei nach derselben Schablone umformen. Neue
Dateien orientieren sich an diesen Regeln und an regelkonformen Dateien mit
derselben Aufgabe.
