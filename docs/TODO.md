# Boardgame Tracker – Offene technische Schulden

Diese Datei hält **konkrete bekannte Abweichungen oder Follow-ups** fest, die nicht
Teil der gewünschten Architektur oder Produktanforderung sind.

Sie ist kein allgemeiner Backlog und wird nicht bei normalen Aufgaben vorsorglich
gelesen. Neue Einträge nur aufnehmen, wenn ein Problem bewusst offen bleibt und
später gezielt bearbeitet werden soll. Erledigte Einträge entfernen statt eine
Historie aufzubauen; Historie gehört in Git/CHANGELOG, wenn relevant.

## .NET 11 / MAUI 11: ausstehende Plattformabnahme

**Status:** Mac-Toolchain und iOS-Builds geprüft; Runtime-/iPhone-Abnahme offen.
SDK-/Workload-/Paketvorgaben stehen ausschließlich in `SETUP.md`.

Die Mac-Toolchain ist eingerichtet; der lokale SDK-/Workload-Pin wurde auch in
MSBuild nachgewiesen. Die nativen ARM64-Archive für Device und Simulator wurden
mit der neuen Apple-Toolchain getrennt gebaut. Simulator-Debug- und signierter
Device-Release-Build einschließlich nativer Verlinkung sind erfolgreich.
Simulator-/iPhone-Runtime und die Updateinstallation sind noch nicht abgenommen.

**Gezielte verbleibende Prüfungen:**
1. Nach unmittelbar zuvor erteilter Entwicklerfreigabe auf dem iPhone 12 Pro mit
   iOS 26.2 App-Start, alle vier Haupttabs einschließlich Reselect, Rückkehr aus
   Unterseiten und Zurücknavigation prüfen; dabei das untenstehende M-04-Gate
   einbeziehen. Safe Areas, helles/dunkles Theme und Layout kontrollieren.
2. ManualGames-Punkteeingabe einschließlich negativer Zahlen sowie NumberWheel,
   Kamera, Fotobibliothek und native Ark-Nova-Fotoerkennung prüfen.
3. SQLite-Laden/-Speichern und Updateinstallation über den vorhandenen Datenbestand
   ohne Verlust prüfen; Import-/Exportvertrag unverändert erhalten.
4. iOS-Release-Runtime mit CoreCLR/ReadyToRun und EF-Reflexion/Trimming gezielt
   abnehmen. Erst mit belegtem Ersatz weitere Shell-/Tastatur-/Trimming- oder
   Symbolstrip-Sonderkonfiguration entfernen.

**Abbruchkriterien:** inkompatibler Toolchain-Satz, fehlerhafte native Verlinkung,
Startup-/Release-Fehler, Datenverlust oder fehlerhafte Navigation/Interop.
Keine dieser Runtime-Prüfungen ist durch die erfolgreichen Builds erledigt.
Das native Android-ARM64-Geräte-Gate bleibt separat bestehen. Umfang/Freigaben
richten sich nach `AGENTS.md`/`SETUP.md`; iPhone-Deployment ist zurückgestellt.

## Plattformnachweise

### iOS M-04: Native Haupttabzuordnung zur Laufzeit verifizieren

**Status:** Runtime-Verifikation offen

**Ist:** Der iOS-`ThemeShellRenderer` ordnet einen Tap auf die native `UITabBar` über
direkte `UIControl`-Subviews, deren Positionen (`Frame.X`) und eine RTL-Umkehr den
`ShellSection`-Tabs zu. Diese Zuordnung ist durch die öffentliche UIKit-Schnittstelle
nicht garantiert. Statische Prüfungen und vorhandene Navigationstests bestätigen
keinen konkreten Fehlablauf, können die native Trefferzuordnung aber nicht belegen.

**Ziel:** Sobald das MacBook wieder verfügbar ist, die aktuelle App auf einem
iOS-Simulator oder iPhone gezielt prüfen: aus Unterseiten alle vier Haupttabs
antippen, auch den bereits aktiven Tab erneut wählen; jeweils den richtigen Root,
den verworfenen Unterstack und die passende visuelle Tabmarkierung bestätigen. Zusätzlich
RTL und ein breiteres Layout prüfen, soweit in der Testumgebung möglich. Nur bei
reproduzierbarem Fehler die konkrete Tab-/Gestenzuordnung korrigieren; kein
vorsorglicher Renderer-Umbau.

**Freigabe:** Unmittelbar vor jedem Simulator- oder Gerätelauf die ausdrückliche
Entwicklerfreigabe einholen. Bis dahin M-04 nicht als erledigt betrachten.

### ManualGames: Punkteingabe vor dem manuellen Mobiltest

Auf iOS die Minus-Eingabe über `NumbersAndPunctuation`, auf Android über die
vorzeichenbehaftete Ganzzahltastatur mit der tatsächlich verwendeten Tastatur prüfen.
Offen sind Eingabe von `-`, `-1`, `0`, Löschen zum leeren Zwischenstand, Paste,
Fokus beim Tippen sowie Ablehnung negativer Werte bei deaktivierter Option.
Die `int`-Validierung ist automatisiert prüfbar; die angebotenen Tasten und das
Fokusverhalten sind erst auf dem Gerät nachgewiesen. Kein Gerätelauf für diesen Patch.

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

## Arche Nova – CV-Datensammlung

### Default vor dem ersten 1.0-Build bewusst umstellen

**Status:** vor 1.0 erforderlich

Der feste Default der lokalen Bilderkennungsdatensammlung ist in der internen
Pre-1.0-Testphase aktiv. Unmittelbar vor dem ersten 1.0-Build und vor regulärer
externer Verteilung muss dieser Default bewusst auf deaktiviert geändert werden.
Keine Laufzeitentscheidung anhand der App-Version einführen; bestehende ausdrücklich
gespeicherte Nutzerauswahlen erhalten.

## Architektur

### Weitere Editierdialoge auf den gemeinsamen Navigationsvertrag umstellen

`INavigationService` mit Shell-Adapter ist vorhanden. `EditGroup` und `EditPlayer`
verwenden XAML-Commands, ViewModel-gesteuertes Speichern und Navigation mit
Inline-Fehlern sowie Navigation-Retry ohne erneuten Write. Die Spielauswahl im
Gruppeneditor aktualisiert ihre gefilterten Ansichten nach erfolgreichen Commands.

Weitere Editierdialoge nur in ausdrücklich beauftragten Folgeschritten auf diesen
Ablauf umstellen. Verbleibende `SavedAsync`-Callbacks und Page-Navigationsabläufe
jeweils anhand ihrer tatsächlichen Verantwortung prüfen; Spielnavigation und
Sonderaktionen dabei erhalten. Die abgeschlossene Pilotumstellung ist keine
Freigabe für ein projektweites Refactoring.

### Allgemeine Games-Abfrage und Enablement-Use-Case

**Ist:** `GameActivationUseCase` unter `Application/Settings` mischt Lesen und Schreiben und verwendet uneinheitliche Begriffe; weitere allgemeine Spieleabfragen sind auf `PlayOverviewQuery` und den Gruppeneditor verteilt.

**Ziel:**

1. Feature-Ownership unter `Application/Games` vereinheitlichen: lesende `GamesQuery` und separaten schreibenden Use Case, beispielsweise `SetGameEnabledUseCase`. `GetAllAsync`, `GetEnabledAsync`, `GetByKeyAsync` sind **Vorschläge**, keine ohne Aufruferprüfung verbindlich festgelegten Methoden.
2. Alle Aufrufer / benötigten Read Models gegenüber `PlayOverviewQuery` und Gruppenauswahl prüfen; tatsächlich gemeinsame Abfragen konsolidieren, sessionbezogene Navigation nicht ohne guten Grund in `GamesQuery` verschieben.
3. Einheitlich `Games`, `IsEnabled`, `SetEnabled` nennen; nicht für denselben Zustand wechselweise `Activation`, `Visible`, `Active`, `GameCatalog`. UI-Modellnamen nur nach Prüfung ihrer übrigen Bedeutung ändern.
4. **MAUI Preferences als persönlichen Speicher unverändert lassen**, Schlüssel nach `Game.Key`. Keine zusätzliche Entity-Spalte oder Datenmigration. Standardzustand und Schutz bei offenen Sessions erhalten; bisherige effektive Enabled-Berechnung beachten.
5. Views, ViewModels, DI und passende Tests nach finaler Aufteilung einheitlich anpassen. Keine reinen Rename-Aliase einführen.

### Weitere bereits angesprochene Aufräumarbeiten

- `GamesPage`: `_creation` in einen sprechenden fachlichen Navigationsnamen ändern (Arbeitsvorschlag `_gameCreationNavigation`, `OpenCreateAsync()` → `NavigateToCreationAsync()`), sofern dies nach der Entscheidung zum zentralen Navigationsservice noch fachlich passt. Kein paralleles zweites Navigationsmuster etablieren.
- Bei UI-Taps/Commands vorhandene gebundene Objekte über `{Binding .}` weitergeben statt IDs zu übergeben und später unnötig neu zu suchen; Prüfungen gegen fremde Objekte erhalten. Nur dort nacharbeiten, wo die aktuelle Pilotüberarbeitung es noch nicht abdeckt.
- Kurzkommentare für tatsächlich erklärungsbedürftige Framework-Overrides (z. B. `OnAppearing` mit Modal-Guard) konsistent anwenden, keine trivialen Kommentare einführen.

### Produktentscheidung zu entfernten Spielmodulen

Das gewünschte Verhalten bei lokal vorhandenen Daten eines später entfernten
Spielmoduls festlegen, insbesondere für Historie, Navigation und verfügbare
Aktionen. Diese Produktentscheidung separat treffen; keine allgemeine Behandlung
im laufenden Presentation-Refactoring vorwegnehmen.

### Ark-Nova-Buildverdrahtung in der allgemeinen App-Projektdatei

**Status:** beobachten / bei weiterem Wachstum auslagern

`src/BoardgameTracker.App/BoardgameTracker.App.csproj` bindet Ark-Nova-native Assets,
Buildverzeichnisse und native Bibliotheken direkt ein.

**Ziel:** Die App-Projektdatei darf als Hostintegrationspunkt Spielassets paketieren.
Wenn die spielabhängige Buildkonfiguration weiter wächst, soll sie in eine
modul-eigene importierte Builddatei unter `Games/ArkNova/` ausgelagert werden,
damit die allgemeine Projektdatei übersichtlich bleibt.

## Spielereditor: zurückgestellte Prüfungen

### MAUI-System-Zurücknavigation gezielt überprüfen

Bei einer späteren Plattformmodernisierung verifizieren, ob
`OnBackButtonPressed()` unter MAUI 11 auf Android und iOS die gewünschten
System-Zurückaktionen einschließlich Gesten zuverlässig erfasst. Bestehende
Speicher-/Navigationssperren erhalten; keine vorsorgliche Änderung ohne
nachgewiesenes Problem. Geräte- und Simulatorläufe nur mit gesonderter Freigabe.

### AsyncOperationGate: optionaler Lesbarkeitstest (zurückgestellt)

Einen kleinen Gate-Prototyp zuerst nur am Spielereditor erwägen, **nach** der
vereinbarten Codestyle-Überarbeitung. Ziel wäre weniger wiederholte Busy-/
`try/finally`-Verwaltung bei gleicher Nebenläufigkeits- und Fehlersemantik.
Nicht pauschal auf ViewModels mit Autosave, Flush oder eigener Semaphore-Logik
übertragen; nur übernehmen, wenn der Gesamtcode verständlicher wird.
