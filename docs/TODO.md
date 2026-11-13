# Boardgame Tracker – Offene technische Schulden

Diese Datei hält **konkrete bekannte Abweichungen oder Follow-ups** fest, die nicht
Teil der gewünschten Architektur oder Produktanforderung sind.

Sie ist kein allgemeiner Backlog und wird nicht bei normalen Aufgaben vorsorglich
gelesen. Neue Einträge nur aufnehmen, wenn ein Problem bewusst offen bleibt und
später gezielt bearbeitet werden soll. Erledigte Einträge entfernen statt eine
Historie aufzubauen; Historie gehört in Git/CHANGELOG, wenn relevant.

## Plattformnachweise

### iOS M-04: Native Haupttabzuordnung zur Laufzeit verifizieren

**Status:** zurückgestellt – MacBook derzeit nicht verfügbar; Runtime-Verifikation offen

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

### Ark-Nova-Buildverdrahtung in der allgemeinen App-Projektdatei

**Status:** beobachten / bei weiterem Wachstum auslagern

`src/BoardgameTracker.App/BoardgameTracker.App.csproj` bindet Ark-Nova-native Assets,
Buildverzeichnisse und native Bibliotheken direkt ein.

**Ziel:** Die App-Projektdatei darf als Hostintegrationspunkt Spielassets paketieren.
Wenn die spielabhängige Buildkonfiguration weiter wächst, soll sie in eine
modul-eigene importierte Builddatei unter `Games/ArkNova/` ausgelagert werden,
damit die allgemeine Projektdatei übersichtlich bleibt.
