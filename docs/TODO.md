# Boardgame Tracker – Offene technische Schulden

Diese Datei hält **konkrete bekannte Abweichungen oder Follow-ups** fest, die nicht
Teil der gewünschten Architektur oder Produktanforderung sind.

Sie ist kein allgemeiner Backlog und wird nicht bei normalen Aufgaben vorsorglich
gelesen. Neue Einträge nur aufnehmen, wenn ein Problem bewusst offen bleibt und
später gezielt bearbeitet werden soll. Erledigte Einträge entfernen statt eine
Historie aufzubauen; Historie gehört in Git/CHANGELOG, wenn relevant.

## .NET 11 / MAUI 11: kontrollierte Migration

**Status:** vorbereitet; .NET-11-Umstellung, Builds und Runtime-Abnahme noch offen.
**Ausgangslage (13.11.2026):** App-TFMs `net10.0-android;net10.0-ios`, MAUI
`10.0.110`, EF Core `10.0.14`, lokal dokumentierte Mac-SDK-/Workload-Pins
`10.0.403`/`10.0.401`; die Root-`global.json` legt weiterhin nur den
Microsoft-Testing-Platform-Runner fest. Diese Ist-Werte erst bei tatsächlicher
Migration in `SETUP.md` ersetzen.

**Ziel:** Android und iOS gemeinsam auf .NET 11 / MAUI 11 / EF Core 11 bringen,
mit möglichst standardmäßigen MAUI-Handlern und CoreCLR statt unnötiger
historischer iOS-Sonderkonfiguration. Bewährte und nötige native Adapter bleiben
bestehen. Das iPhone 12 Pro mit iOS 26.2 ist ein verbindliches Abnahmegerät;
frühere iOS-Versionen müssen nicht allein aus Kompatibilitätsgründen erhalten
bleiben. Die konkrete deklarierte Mindest-iOS-Version anhand des finalen SDKs
festlegen und die tatsächlich unterstützten Geräte beachten.

**Technische Referenzen:**
- `https://github.com/dotnet/skills/tree/main/plugins/dotnet-upgrade/skills/migrate-dotnet10-to-dotnet11`
  als migrationsspezifischer Leitfaden; Stand 13.11.2026 enthält er noch
  Preview-3-Angaben, die nicht als aktuelle GA-Versionen/Pins übernommen werden.
- `https://learn.microsoft.com/dotnet/maui/whats-new/dotnet-11`
- `https://learn.microsoft.com/dotnet/maui/deployment/runtimes-compilation`
- `https://learn.microsoft.com/dotnet/maui/macios/interpreter`
- `https://learn.microsoft.com/dotnet/core/compatibility/11`
- `https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/breaking-changes`

**Gemeinsames Migrationspaket (in sinnvoller Reihenfolge):**
1. Entwickler legt vor der ersten Nicht-Markdown-Änderung gemäß `SETUP.md` die
   Versionsart fest. Vor Umsetzung effektive SDK-/Workload-/MAUI-/EF-Core-GA-Versionen
   für beide Build-Hosts prüfen; keine Preview-Pins aus dem Skill übernehmen.
2. Core-, Application-, App- und Test-TFMs sowie zentral verwaltete Microsoft-/EF-
   Pakete konsistent auf .NET 11 aktualisieren. Root-`global.json` ohne Mac-Pin
   belassen, Mac-SDK-/Workload-Pin unter `.local/ios/` lokal anpassen; ggf. weitere
   konkret betroffene Build-Konfigurationen prüfen. Inkrementelle Verifikation
   gemäß `AGENTS.md`/`SETUP.md`; keine vorsorglichen Clean-/Rebuilds.
3. .NET-11-/C#-15-/EF-Core-11-Breaking-Changes nur dort korrigieren, wo der Code
   betroffen ist. SQLite-Provider/SQLitePCLRaw, relationale EF-Queries und
   vorhandene `EnsureCreatedAsync`-Entwicklungsdatenbankstrategie prüfen; keine
   ungeplanten EF-Migrationen oder Datenbank-Löschungen.
4. iOS-Release-Einstellungen prüfen: `MtouchInterpreter` steuert den CoreCLR-
   Interpreter unter .NET 11 nicht und ist als bisheriger Mono-Sonderweg zu
   entfernen, sobald die neue Konfiguration und Runtime-Verifikation stehen.
   `MtouchNoSymbolStrip` und `TrimmerRootAssembly` einzeln auf Erforderlichkeit
   prüfen; nicht ohne Nachweis pauschal löschen. CoreCLR-/ReadyToRun-/Trimming-
   Verhalten für EF-Reflexion, Startup und SQLite validieren.
5. Shell/`ThemeShellRenderer` und die plattformspezifische ManualGames-
   Punkteingabe auf MAUI-11-Standardmechanismen überprüfen. Nur nach belegtem
   gleichwertigem Verhalten vereinfachen; kein vorsorglicher Renderer-Umbau.
   Vorhandenes iOS-M-04-Follow-up bleibt bis zur Runtime-Abnahme offen.
6. Native Ark-Nova-Interop-Bibliothek und Xcode-/iOS-Workload-Kompatibilität
   prüfen; native Bibliotheken nur bei geänderter Toolchain, fehlendem oder
   ungültigem Artefakt neu erstellen. Android x64/ARM64 und iOS Device/Simulator
   getrennt betrachten. Signing- und Provisioning-Daten bleiben lokal.
7. Verifikation: einmal passende Core-/Application-Tests mit echtem SQLite,
   gezielte Android-/iOS-Builds sowie explizite Migrations-Abnahme für iOS Debug
   und Release. Nach unmittelbarer Entwicklerfreigabe auf dem iPhone 12 Pro
   Startup, Tabs/Zurücknavigation inkl. Rückkehr aus Unterseiten, Punkte-/Minus-
   Eingabe, Datenbestand nach Update, Kamera/Fotoerkennung und Release-Runtime
   prüfen. Native Android-ARM64-Geräteprüfung bleibt eigenes offenes Gate.
8. Erst nach nachgewiesenem neuen Ist-Zustand `SETUP.md` auf .NET-11-Pins,
   TFM-/Buildbefehle und iOS-Toolchain aktualisieren; Tests und nachweislich
   erledigte TODO-Punkte abhaken/entfernen. Relevante Versions-/Historienänderung
   nach `SETUP.md` in `CHANGELOG.md` festhalten.

**Abbruchkriterien:** fehlender kompatibler SDK-/Workload-/Xcode-Satz,
fehlgeschlagener Restore/Build, nicht bestandene Core-/Application-Tests,
iOS-Startup-/Release-Fehler, Datenverlust oder fehlerhafte Navigation/Interop.
Bei Plattformprüfungen Umfang und Freigaben aus `AGENTS.md`/`SETUP.md` beachten.

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
