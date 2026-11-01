# Boardgame Tracker – Offene technische Schulden

Diese Datei hält **konkrete bekannte Abweichungen oder Follow-ups** fest, die nicht
Teil der gewünschten Architektur oder Produktanforderung sind.

Sie ist kein allgemeiner Backlog und wird nicht bei normalen Aufgaben vorsorglich
gelesen. Neue Einträge nur aufnehmen, wenn ein Problem bewusst offen bleibt und
später gezielt bearbeitet werden soll. Erledigte Einträge entfernen statt eine
Historie aufzubauen; Historie gehört in Git/CHANGELOG, wenn relevant.

## Plattformnachweise

### Android ARM64: nativer Gerätelauf

**Status:** offen

**Ist:** Der native Mobile-CV-Pfad ist fachlich freigegeben. Ein tatsächlicher
Android-API-30-x64-App-/Interop-/Review-/Persistenzlauf ist vorhanden. Der
vollständige iOS-Simulator-Build-/Runtime-/CV-Ergebnisnachweis ist ebenfalls
erbracht und fachlich akzeptiert. Ein echter Android-ARM64-Gerätelauf der nativen
Integration fehlt weiterhin.

**Ziel:** Den bestehenden produktiven Android-ARM64-Pfad einmal auf einem echten
geeigneten Gerät technisch ausführen und bestätigen, dass App-/Interop-/native
Engine-Lauf ohne plattformspezifischen technischen Fehler funktioniert. Dies ist
kein neues CV-Forschungs- oder Produktfreigabegate und darf keine Optimierung oder
Parameteränderung auslösen.

## iOS-Navigation

### iOS: Haupttab-Retap / Root-Reset

**Status:** offen

**Ist:** Nach Entfernung des fehlerhaften eigenen `MainTabRenderer` verwendet iOS
wieder MAUIs Standard-`ShellItemRenderer`. Dadurch ist der bestätigte
Startup-Black-Screen beseitigt. Das vollständige gewünschte Root-Reset-Verhalten
bei expliziten Haupttab-Taps, insbesondere beim erneuten Tap auf den bereits
aktiven Tab, ist auf iOS noch nicht abschließend umgesetzt/verifiziert.

Manueller Repro: **Historie → Partie → Wertung/Korrektur → App neu starten → Historie
antippen**. Statt den Root `HistoryPage` zu öffnen, kann die App wieder in der tieferen
Wertung/Korrektur landen.

**Ziel:** Die allgemeine Produktregel aus `PRODUCT_SPEC.md` / `UI_WIREFRAME.md`
erfüllen, ohne erneut einen eigenen `ShellItemRenderer` einzuführen, der die native
iOS-Shell-/ViewController-Hierarchie beschädigt. Ein expliziter Haupttab-Tap muss
immer den Root des gewählten Bereichs öffnen. Eine Lösungsvariante ist noch nicht
entschieden.

### iOS: Crash beim Zurück aus historischer Korrektur

**Status:** offen

**Ist:** Manueller Repro:
**Historie → Partie → Ergebnis/Wertung bearbeiten → Zurück**. Beim Zurück aus der
historischen Korrektur wird die iOS-App beendet bzw. stürzt ab.

**Ziel:** Zurück verwirft ungespeicherte Korrekturen, zeigt wieder das historische
Ergebnis derselben Partie und erhält dessen ursprüngliches Rückziel **Historie**.
Dabei darf kein Crash oder App-Ende auftreten.

### iOS: App-Ende beim Verwerfen einer offenen Partie

**Status:** offen

**Ist:** Bei einer laufenden Arche-Nova-Partie führt **Verwerfen → Bestätigen** im
manuellen iOS-Test zum App-Ende. Der aktuelle Navigationspfad verwendet zudem ein
festes Gruppen-Rückziel statt des tatsächlichen Aufrufers.

**Ziel:** Die offene Partie wird nach Bestätigung verworfen, die App bleibt geöffnet
und navigiert zum erhaltenen Aufrufer zurück: aus **Gruppen** zur Gruppenübersicht,
aus **Historie** zur Historie und aus **Einstellungen → Spieler** zur
Spielerübersicht.

## Navigation

### GameSession: aufrufenden Bereich als Rückziel erhalten

**Status:** offen

**Ist:** Das allgemeine Soll verlangt ein erhaltenes Rückziel, die aktuelle
GameSession-Navigation zieht offene `Running`-/`Scoring`-Partien jedoch teilweise
in den Gruppen-Kontext. Insbesondere **Einstellungen → Spieler → Zuletzt gespielt**
öffnet eine offene Partie so, dass Zurück bei Gruppen landet. Running-/Scoring-
Screens verwenden außerdem teilweise fest verdrahtete Gruppen-Routen für Zurück
oder Verwerfen.

**Ziel:** Beim Öffnen einer bestehenden Partie den tatsächlichen Aufrufer
(**Gruppen**, **Historie** oder **Einstellungen → Spieler**) erhalten. Interne
Wechsel zwischen Running, Scoring, Ergebnis und Korrektur dürfen dieses Rückziel
nicht verändern. Zurück und erfolgreiches Verwerfen führen zum entsprechenden
aufrufenden Screen. Ein expliziter Haupttab-Tap bleibt davon unabhängig und öffnet
den jeweiligen Root.

## UI

### Spielerübersicht: Aktivsymbol innerhalb von „Zuletzt gespielt“

**Status:** offen

**Ist:** Das Aktivsymbol für `Running`/`Scoring` steht in der Spielerübersicht als
eigenes Element links neben der Secondary-Aktion **Zuletzt gespielt <Datum>**.

**Ziel:** Das Aktivsymbol ist Bestandteil derselben Buttonfläche und steht rechts
neben Text/Datum. Es bleibt reine Statusanzeige ohne eigene Aktion; bei `Completed`
fehlt es.

### Arche Nova: Foto-Review kompakter und eindeutig navigieren

**Status:** offen

**Ist:** Im aktuellen Foto-Review stehen die Sicherheitsmarkierungen
`✓ Sicher` / `! Prüfen` / `? Unsicher` jeweils in einer eigenen Zeile. Der Hinweis
wird als separater Textblock dargestellt. Unten existiert neben der Bestätigung ein
**Abbrechen**-Button.

**Ziel:** Das Arche-Nova-Wireframe umsetzen: Sicherheitsstatus direkt neben dem
jeweiligen Label in derselben Wertzeile; kompakte Hinweiszeile aus Info-Icon plus
Text; Header Zurück · **Fotowerte prüfen**; unten nur **Bestätigen/Speichern**.
Zurück verwirft unbestätigte Fotovorschläge und zeigt wieder die Wertung.

## Architektur

### Spielmodulgrenze in allgemeinen App-Features

**Status:** offen

`src/BoardgameTracker.App/Features/Games/ListGames/GamesPage.xaml.cs` kennt aktuell
`ArkNovaGame` direkt und besitzt einen konkreten `OnArkNovaTapped`-Pfad.

**Ziel:** Das allgemeine Games-Feature darf keine konkrete Spielabhängigkeit
besitzen. Spielkatalog/Navigation sollen über eine allgemeine Registrierung oder
ein kleines allgemeines Read Model erfolgen; die konkrete Ark-Nova-Implementierung
bleibt im Spielmodul.

### Spielabhängige Einstellungen im allgemeinen Settings-Feature

**Status:** offen

`src/BoardgameTracker.App/Features/Settings/AppSettings/AppSettingsPage.xaml.cs`
kennt aktuell `ArkNovaGame` und `ArkNovaSettingsPage` direkt und baut die
Default-Spielauswahl mit Ark Nova hart auf.

**Ziel:** Allgemeine App-Einstellungen dürfen keine konkreten Spieltypen oder
Spielseiten kennen. Registrierte Spiele und deren Settings-Einstieg sollen über
eine kleine allgemeine Modulgrenze bereitgestellt werden.

### Ark-Nova-Buildverdrahtung in der allgemeinen App-Projektdatei

**Status:** beobachten / bei weiterem Wachstum auslagern

`src/BoardgameTracker.App/BoardgameTracker.App.csproj` bindet Ark-Nova-native Assets,
Buildverzeichnisse und native Bibliotheken direkt ein.

**Ziel:** Die App-Projektdatei darf als Hostintegrationspunkt Spielassets paketieren.
Wenn die spielabhängige Buildkonfiguration weiter wächst, soll sie in eine
modul-eigene importierte Builddatei unter `Games/ArkNova/` ausgelagert werden,
damit die allgemeine Projektdatei übersichtlich bleibt.

### Globaler Architekturtest für Spielmodulgrenzen

**Status:** offen

Die vorhandenen Architekturtests schützen bereits einzelne Grenzen, insbesondere
allgemeine Gruppenverträge und allgemeine Group-UI vor konkreten Spieltypen. Ein
repositoryweiter Schutz aller allgemeinen App-/Application-Bereiche vor
`Games.<ConcreteGame>`-Abhängigkeiten fehlt jedoch noch.

**Ziel:** Einen kleinen Architekturtest ergänzen, der die allgemeine
`Games/<GameName>/`-Ownership projektweit schützt und dabei erlaubte
Integrationspunkte wie Composition Root, Host-Projektdatei und technisch notwendige
Partial-Type-Erweiterungen ausdrücklich berücksichtigt.
