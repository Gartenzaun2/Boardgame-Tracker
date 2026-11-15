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

**Beispiele veranschaulichen Regeln, keine verbindlichen Member-Namen oder
Refactoring-Aufträge.** Namen wie `RefreshPositionsAndMoveAvailability()`,
`GamesAvailableToAdd` und `OpenGameSelectionCommand` sind Beispiele, keine
Behauptung über bereits vorhandenen Code. Sie nur übernehmen, wenn sie zur
konkreten Verantwortung und den bestehenden Verträgen passen; nicht allein
wegen eines Beispiels projektweit umbenennen.

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
- Technische Dialogzustände (`IsBusy`, `IsSaveCompleted`) von fachlichen Daten
  (`GroupName`, `IsGroupArchived`) begrifflich unterscheiden. Das `Is`-Präfix
  allein ist **keine** fachliche Gruppierung. Fachliche Präfixe verwenden, wenn
  der Gegenstand sonst unklar bleibt; nicht alle Members reflexartig präfixieren.
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

Beispiel für einen **notwendigen Warum-Kommentar** an einem Dialogzustand
(nicht als Kommentarpflicht für jeden Speichervorgang verstehen):

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

Beispiel für einen **Warum-Kommentar** an einem MAUI-Lifecycle-Override:

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
Ports, Adapter) müssen **direkt am zentralen Vertrag** so dokumentiert sein, dass
auch Einsteiger die Entscheidung nachvollziehen können. Eine englischsprachige
XML-Dokumentation beantwortet, soweit für das konkrete Konzept relevant:

1. **Warum** existiert die Abstraktion und welche Kopplung oder welches Problem
   verhindert sie?
2. **Wer** verwendet den Vertrag und **wer** übernimmt seine Implementierung?
3. **Wie** sieht ein echter Aufruf oder ein Test ohne die konkrete Implementierung
   aus? Ein kurzes `<example>` ist erwünscht, wenn es den Einstieg erleichtert.
4. **Welchen Vorteil und welchen Preis** hat die Trennung? Die ausführliche
   Alternativenabwägung gehört in `ARCHITECTURE.md`.

Positives Beispiel für einen fachlich begründeten Navigationsvertrag:

```csharp
/// <summary>
/// Lets ViewModels request navigation without depending on MAUI Shell.
/// Pages and their code-behind handle MAUI-specific UI behavior, while
/// a Shell adapter performs the navigation requested by a ViewModel.
/// Tests can replace this interface with a fake, so a save workflow can
/// be tested without creating a Page or starting a running Shell.
/// </summary>
/// <example>
/// After saving: <code>await _navigationService.GoBackAsync();</code>
/// A test fake can record the call and verify that navigation was requested.
/// </example>
public interface INavigationService {
    Task GoBackAsync();
    Task NavigateToMainSectionAsync(AppMainSection section);
}
```

Hier ist der **Grund der Entkopplung**, nicht bloß die technische Weiterleitung,
dokumentiert. Die Schnittstelle hält ViewModels von **MAUI-Navigations-APIs**
unabhängig; das garantiert nicht automatisch, dass alle anderen Abhängigkeiten
eines ViewModels frameworkfrei sind. Eine kurze Notiz bei der DI-Registrierung
kann die Wahl des Adapters erklären. Die Begründung nicht an jeder Aufrufstelle
wiederholen; keine Interfaces allein zum Vorführen eines Architekturpatterns
erschaffen.

C#-Bezeichner und Code-/XML-Kommentare sind **Englisch**; Projektdokumentation ist
**Deutsch**.

## 4. Dateien, Klassen und Methoden gliedern

**Klassen:** Bevorzugte Lesereihenfolge in ViewModels: explizite `readonly`-
DI-Felder, Konstruktor, eigener Zustand/Properties, anschließend Methoden entlang
des fachlichen Ablaufs. Technischen Dialogzustand, fachliche Daten, abgeleitete
Bedingungen und Collections **erkennbar nach Verantwortung** gruppieren.
`CanEdit` oder `CanSave` können direkt bei verwandten Prozessproperties stehen;
keine starre Sortierung nach Sichtbarkeit, Typ oder Alphabet. Zusammengehörige
Hilfsmethoden nahe beim zugehörigen Ablauf lassen.

**Methoden:** Auch **innerhalb** einer Methode die Schritte in logischer,
fachlicher und zeitlicher Reihenfolge anordnen: Vorbedingungen → Daten laden →
Ergebnis prüfen → zusammengehörige Eigenschaften übernehmen → Collections
aufbauen → erfolgreich abschließen. Unabhängige Phasen mit Leerzeilen trennen,
ohne Überschriftenkommentare für Selbstverständliches. Die Reihenfolge niemals
allein kosmetisch ändern, wenn Fehler-, Nebenläufigkeits- oder Zustandssemantik
betroffen ist. Beispielsweise eine Kennung, die "bereits geladen" signalisiert,
erst nach **vollständig erfolgreichem** Laden setzen.

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
derselben Zeile (**K&R**); schließende Klammern auf einer eigenen Zeile,
ausgenommen kompakte Property-Accessors. Die folgenden Beispiele legen zudem
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
  Ablauf abflachen. Unerwartete Fehler nicht stillschweigend als erfolgreichen
  No-op behandeln. Eine existierende Vertrags-/Fehlergrenze nutzen, statt
  Validierung in jeder Schicht zu duplizieren.
- Gemeinsame Abschlussschritte nicht unnötig in mehreren Branches duplizieren.
  Beispiel: Bei bereits erfolgreichem Save nur den Write überspringen, danach
  denselben Navigationsaufruf verwenden.
- Ternäre Ausdrücke nur für offensichtliche **Wertauswahl**; keine komplexen
  `if`-Bedingungen oder Auswahl unterschiedlicher (insbesondere asynchroner)
  Methodenaufrufe per `?:`. Benannte boolesche Bedingungen nur bei echtem Gewinn.
- Boolesche Parameter, die wesentlich unterschiedliche Abläufe steuern,
  kritisch prüfen; keine zusätzlichen Methoden/Delegates ohne Lesbarkeitsgewinn.

Diese Beispiele zeigen **beide** zulässigen Formen sowie einen verschachtelten
Fall, bei dem Klammern die Lesbarkeit verbessern:

```csharp
// One simple statement: no braces.
foreach (Player player in Players)
    player.RefreshScore();

// Multiple statements: braces required.
foreach (Player player in Players) {
    player.RefreshScore();
    player.RefreshRank();
}

// Braces clarify the nested if/else structure.
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

Parameterlisten grundsätzlich auf einer Zeile, sofern ein sinnvoller Umbruch
nicht lesbarer ist; **keine starre Zeilenlänge** oder Pflicht "ein Parameter pro
Zeile". Projekt-Typen über `using` einbinden, außer bei Namenskonflikten.

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
eigenen Busy-Namen zu erfinden ist nicht nötig. Für einfache **Negationen** im XAML
den vorhandenen `BoolNegationConverter` verwenden, nicht separate `IsIdle`-
Properties. Fachlich eigenständige Bedingungen wie
`CanEdit => !IsBusy && !IsSaveCompleted` bleiben berechnete Properties.

Parameterlose `CanExecute`-Bedingungen dürfen als berechnete `bool`-Properties
neben verwandtem Dialogzustand stehen, statt weit unten als Methode versteckt
zu sein. `[RelayCommand(CanExecute = nameof(CanSave))]` ruft die Bedingung ab,
**beobachtet ihre Abhängigkeiten aber nicht automatisch**. Die verursachenden
Observable-Properties müssen den Command mittels
`[NotifyCanExecuteChangedFor(nameof(SaveCommand))]` benachrichtigen;
`[NotifyPropertyChangedFor(nameof(CanSave))]` allein ist **kein Ersatz**.
Bei Collection-basierten Bedingungen (z. B. `Players.Count`) die erforderliche
Benachrichtigung bei den tatsächlichen Collection-Änderungen sicherstellen.
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

Einfache Elemente auf einer Zeile, umfangreiche auf **wenige semantisch
gruppierte Attributzeilen** verteilen (Layout/Darstellung, Binding/Zustand,
Interaktion, Accessibility). Kein pauschales "ein Attribut pro Zeile", keine
starre maximale Länge und kein zusätzlicher XAML-Formatter ohne Bedarf.

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

Konkretes Beispiel (die Page hat `x:Name="Page"` und eine öffentliche,
typisierte `ViewModel`-Property):

```xml
<!-- Outside an item template: EditGroupViewModel is the BindingContext. -->
<Button Command="{Binding OpenGameSelectionCommand}" />

<!-- Inside the list: the GameSelection item is the BindingContext. -->
<CollectionView ItemsSource="{Binding GamesAvailableToAdd}">
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:GroupGameSelection">
            <Border>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer
                        Command="{Binding Source={x:Reference Page}, Path=ViewModel.AddGameCommand, x:DataType=local:EditGroupPage}"
                        CommandParameter="{Binding .}" />
                </Border.GestureRecognizers>
                <Label Text="{Binding Name}" />
            </Border>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

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
