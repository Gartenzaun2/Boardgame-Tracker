# Boardgame Tracker – Setup und Entwicklung

## 1. Zweck

Diese Datei ist die kanonische Quelle für **Entwicklungsumgebung, Build/Test,
Entwicklungsdatenbank, Versionierung und Lieferung**.

Nicht hierher gehören Produktstatus, Softwarearchitektur, UI-Regeln, Forschungsstände
oder historische Schemaänderungen. Dafür gelten die jeweils zuständigen Dokumente
unter `docs/`.

Arbeits- und Verifikationspolicy: [`AGENTS.md`](AGENTS.md).

## 2. Voraussetzungen

- .NET SDK `11.0.100` für die `net11.0`-Ziele und passende Plattform-Workloads.
- .NET MAUI `11.0.10` (über `MauiVersion` im App-Projekt gepinnt)
- EF Core / Microsoft.Data.Sqlite und Microsoft.Extensions `11.0.0`
- SQLite
- Visual Studio bzw. passende .NET-MAUI-Toolchain
- Android- und/oder iOS-Toolchain für die jeweilige Zielplattform

Unterstützte Mindestplattformen gemäß App-Projekt:

```text
Android 11 / API 30
iOS 15
```

Paketversionen werden zentral über `Directory.Packages.props` verwaltet.
Der EF-SQLite-Provider löst SQLitePCLRaw `3.0.5` und SQLite `3.53.4` auf.

Der verwendete Windows-Buildsatz besteht aus SDK `11.0.100`, Android-Workload
`37.2.31`, iOS-/MacCatalyst-Workloads `27.2.11039` und MAUI-Windows `11.0.0`
(Manifestmodus `11.0.100-manifests.073a2387`). Die App-Paketversion wird separat
durch `MauiVersion` bestimmt. Ein Windows-iOS-Build prüft verwalteten Code; für
native Verlinkung und Release-Pakete ist weiterhin ein Mac erforderlich.

MAUI 11 verwendet auf mobilen Plattformen standardmäßig CoreCLR. Die bisherige
Mono-Einstellung `MtouchInterpreter` entfällt; CoreCLR verwaltet seinen Interpreter
selbst. Die EF-/Domain-`TrimmerRootAssembly`-Einträge bleiben wegen reflektierter
Mappings/Konstruktoren erhalten, ebenso die native Linkereinbindung und
`MtouchNoSymbolStrip`. Eigene Shell-/NumberWheel-Handler und die iOS-Tastaturanpassung
bleiben bestehen, solange kein gleichwertiger Standardmechanismus nachgewiesen ist.

Referenzen: [MAUI 11](https://learn.microsoft.com/dotnet/maui/whats-new/dotnet-11),
[mobile Codegenerierung](https://learn.microsoft.com/dotnet/core/compatibility/maui/11/mobile-code-generation),
[EF Core 11](https://learn.microsoft.com/ef/core/what-is-new/ef-core-11.0/breaking-changes).

### Gemeinsame SDK-Konfiguration und macOS/iOS

Die **versionierte `global.json` im Repository-Root** enthält nur die
`Microsoft.Testing.Platform`-Runner-Konfiguration. Sie legt weder SDK-Version noch
Workload-Set fest und darf nicht für die lokale Mac-/iOS-Toolchain gepinnt werden.
Insbesondere Windows/Visual Studio wird dadurch nicht auf eine Mac-spezifische
SDK-Version festgelegt. Ein kompatibles SDK und die erforderlichen Workloads müssen
auf jedem Entwicklungsrechner trotzdem installiert sein.

**Mac-Codex verwaltet die eigene, nicht versionierte Konfiguration unter
`.local/ios/global.json` (Repository-Root → `.local` → `ios`).** `.local/` steht in
`.gitignore`; weder diese Datei noch persönliche Signing-/Device-Konfigurationen
werden ins Repository übernommen. Für den .NET-11-Build ist folgender lokaler
Mac-Pin vorgesehen; die tatsächliche Mac-Installation ist vor dem Build abzugleichen:

```json
{
  "sdk": {
    "version": "11.0.100",
    "rollForward": "disable",
    "allowPrerelease": false,
    "workloadVersion": "11.0.100.1"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Das [iOS-Workload-Set `11.0.100.1`](https://github.com/dotnet/macios/releases/tag/dotnet-11.0.1xx-xcode27.2.11039)
verwendet iOS `27.2.11039` mit Xcode `27.2` und benötigt macOS `16.6` oder neuer.
Die SDK-Version für den Build setzt nicht die Mindest-iOS-Version der App;
das Abnahmegerät iPhone 12 Pro mit iOS 26.2 bleibt vorgesehen.
Die MAUI-Paketversion `11.0.10` bleibt unabhängig davon im App-Projekt gepinnt.
Bei einer Toolchain-Aktualisierung SDK,
Workload-Set und MAUI-Paket gemeinsam prüfen, aber keinen plattformübergreifenden
Pin aus der Mac-Konfiguration in die Root-`global.json` übertragen.

**Wichtig für Mac-Codex:** `.local/ios/global.json` ist kein automatischer Override
für Builds vom Repository-Root. Die .NET-CLI sucht ab ihrem aktuellen
Arbeitsverzeichnis, der MSBuild-SDK-Resolver kann dagegen beim Solution- bzw.
Projektverzeichnis beginnen. Mac-Codex muss deshalb den lokalen Build-Einstieg und
die *tatsächlich verwendete* SDK-/Workload-Version prüfen, statt nur das Vorhandensein
der Datei vorauszusetzen.

Für die SDK-/Workload-Auswahl zunächst aus `.local/ios/` prüfen:

```bash
cd .local/ios
dotnet --version
dotnet workload --version
dotnet workload restore ../../src/BoardgameTracker.App/BoardgameTracker.App.csproj
```

Der erwartete lokale SDK-Wert ist `11.0.100`, die Workload-Set-Version `11.0.100.1`.
Vor einem iOS-Build zusätzlich überprüfen, ob auch der konkrete MSBuild-Aufruf
diese Toolchain verwendet. Stimmt die effektive Auswahl nicht, muss **die lokale
Mac-Build-Aufrufkonfiguration** korrigiert werden, nicht die gemeinsame Root-Datei.
Die allgemeinen Build-/Test- und Freigaberegeln dieses Dokuments gelten weiterhin.

### Repository- und Log-Sicherheit

Secrets, Zugangsdaten und private/persönliche Kennungen gehören niemals in
versionierte Repository-Dateien, Patches oder Commits. Dazu zählen insbesondere:

- API-Keys und andere Schlüssel, Access-/Refresh-Tokens, Passwörter und Credentials;
- private Schlüssel, Zertifikatsmaterial sowie Provisioning-/Signing-Geheimnisse;
- persönliche E-Mail-Adressen, sofern nicht ausdrücklich als öffentliche
  Projektdaten vorgesehen;
- Apple-/Developer-Team-IDs, persönliche Account-/Benutzerkennungen;
- Geräte-UDIDs, Seriennummern und vergleichbare eindeutige Gerätekennungen;
- lokale Benutzernamen und persönliche Home-Verzeichnispfade, wenn sie für die
  Diagnose nicht erforderlich sind;
- sonstige Secrets oder personenbezogene/private IDs.

Öffentliche technische Projektkennungen wie der normale App-Bundle-Identifier
müssen nicht pauschal entfernt werden, sofern sie keine privaten Daten darstellen.

Logs, Buildlogs, JSON-Diagnosen, Crash-/Device-Ausgaben und Terminalausgaben, die
ein Agent dem Entwickler bereitstellt, müssen **vor dem Bereitstellen** redigiert
sein. Der Entwickler soll sie direkt an externe Analysewerkzeuge bzw. ChatGPT
weitergeben können, ohne selbst noch nach privaten Daten suchen zu müssen.
Dasselbe gilt für kopierte Ausschnitte im Abschlussbericht: keine privaten Werte
aus Rohlogs in Antworten übernehmen.

Eindeutige, diagnostisch brauchbare Platzhalter verwenden, zum Beispiel
`<REDACTED_EMAIL>`, `<REDACTED_TOKEN>`, `<SIGNING_IDENTITY>`, `<TEAM_ID>`,
`<DEVICE_ID>`, `<USER>`, `<HOME>` und `<PRIVATE_ID>`. Die Redaktion darf die
diagnostisch relevante Struktur nicht unnötig zerstören. Zeitstempel, PID,
Fehlermeldungen, Framework-/SDK-Versionen, öffentliche Bundle-Identifier und
technische Stacktraces bleiben erhalten, soweit sie keine privaten Daten enthalten.

Technisch benötigte Rohlogs ausschließlich lokal und unversioniert behandeln:
niemals committen oder als weitergabefähiges Ergebnis präsentieren. Für die
Weitergabe eine redigierte Kopie erzeugen, möglichst bereits beim Erfassen.
Bevor ein Logpfad als Ergebnis genannt wird, dessen Inhalt auf private Daten
prüfen und sicherstellen, dass die genannte Datei die redigierte,
weitergabefähige Variante ist.

## 3. Native Arche-Nova-Abhängigkeit

Die produktive Arche-Nova-Fotoerkennung benötigt vor einem entsprechenden
Android-/iOS-App-Build die lokal gebaute native Bibliothek.

Buildpfade, gepinnte Abhängigkeiten und Plattformbefehle stehen ausschließlich in:

`tools/Games/ArkNova/NativeVisionSpike/README.md`

Die erzeugten nativen Libraries werden nicht versioniert.

Produktstatus der Fotoerkennung:
`docs/Games/ArkNova/CV_STATUS.md`.

## 4. Build und automatisierte Tests

Vom Verzeichnis mit `BoardgameTracker.sln`:

```powershell
dotnet restore BoardgameTracker.sln
dotnet build BoardgameTracker.sln --no-restore
dotnet test --project tests/BoardgameTracker.Core.Tests --no-restore
dotnet test --project tests/BoardgameTracker.Application.Tests --no-restore
```

Die Tests verwenden xUnit v3 mit Microsoft Testing Platform; deshalb wird
`dotnet test --project` verwendet.

Welcher Teil davon für einen konkreten Auftrag tatsächlich ausgeführt werden soll,
richtet sich nach `AGENTS.md`. Für Simulator-/Emulator-/Geräte- und manuelle
UI-Prüfungen gelten die dortigen Freigaberegeln; Umfang und Abbruchkriterien des
gezielten iOS-Simulator-Smoke-Tests stehen im folgenden Unterabschnitt.

### Inkrementelle Builds und Wiederverwendung

Builds sollen vorhandene gültige Zwischenergebnisse, Caches und native Artefakte
wiederverwenden. Standardmäßig keinen `clean` oder vollständigen `rebuild`
ausführen, `bin`/`obj` oder andere Build-Caches nicht vorsorglich löschen und
keinen Restore wiederholen, wenn der vorhandene Restore zur aktuellen
Projektkonfiguration passt; dann bevorzugt mit `--no-restore` bauen und testen.

Ein App-, Simulator- oder Geräte-Artefakt gilt nur dann als gültig, wenn es den für
die Prüfung relevanten aktuellen Quell-, XAML-, Ressourcen-, Projekt- und
Versionsstand enthält. Relevante Änderungen seit seinem Build erfordern vor dem
Runtime-Test einen neuen passenden inkrementellen Build; ein Clean/Rebuild ist dafür
nicht nötig.

Native Ark-Nova-/OpenCV-Artefakte nur erneut bauen, wenn sich ihre Quellen,
Buildparameter, Toolchain oder Zielplattform/-architektur geändert haben, das
passende Artefakt fehlt oder konkret als ungültig erkannt wurde. iOS-Device- und
iOS-Simulator-Artefakte bleiben strikt getrennt und werden nicht gegenseitig
wiederverwendet.

Vom Buildsystem als abhängig erkannte Schritte dürfen normal erneut laufen. Änderungen
an Managed Assemblies können insbesondere erneutes AOT, LLVM oder natives Linking
erforderlich machen; solche notwendigen Schritte nicht künstlich überspringen.

Clean/Rebuild, Cache-Löschung, erneuter Restore oder erneuter Native-Build nur bei
einem konkreten technischen Grund. Keinen zweiten Build derselben Konfiguration
allein „zur Sicherheit“ ausführen.

### iOS-Geräte-Deployment und Signing unter macOS

Für ein angeschlossenes iPhone kann die lokale Deployment-Konfiguration unter
`.local/ios/` verwendet werden. `deploy-iphone.sh`, `signing.py`, `config.sh` und
das bei Bedarf erzeugte Xcode-Hilfsprojekt sind **nicht versioniert** und auf
anderen Rechnern nicht automatisch vorhanden. Die folgenden Befehle setzen diese
lokalen Skripte voraus und werden vom Repository-Root ausgeführt:

```bash
# Nur iOS-Signing prüfen; kein Build, keine Installation und kein App-Start:
bash .local/ios/deploy-iphone.sh --check-signing

# iOS-Provisioning bei Bedarf über Xcode erneuern; ohne App-Installation/-Start:
bash .local/ios/deploy-iphone.sh --repair-signing

# Vollständiges iOS-Deployment: Signing, Release-Build, Installation und Start:
bash .local/ios/deploy-iphone.sh
```

Voraussetzungen sind eine in Xcode angemeldete Apple-ID mit passenden
Signing-Berechtigungen, ein nutzbares Apple-Development-Zertifikat samt privatem
Schlüssel im Schlüsselbund und das verbundene, entsperrte iPhone mit eingerichteter
Entwicklerverbindung. Das Zielgerät wird ausschließlich lokal in `config.sh`
konfiguriert. SDK und Workloads werden gemäß dem Abschnitt
„Gemeinsame SDK-Konfiguration und macOS/iOS“ über `.local/ios/global.json`
ausgewählt; der lokale Skript-Einstieg muss zu diesem Pin passen.

Die lokale Signing-Prüfung berücksichtigt beide macOS-Profilordner
`~/Library/Developer/Xcode/UserData/Provisioning Profiles` und
`~/Library/MobileDevice/Provisioning Profiles`. Sie prüft die Bundle-ID
`de.boardgametracker.app`, das Ablaufdatum, die Aufnahme des Zielgeräts und die
Zuordnung zu einer verfügbaren Signing-Identität. Ein gültiges Zertifikat allein
genügt nicht: Das Entwicklungsprofil kann unabhängig davon ablaufen.

Fehlt ein nutzbares Profil, verwendet die lokale Reparatur Xcode Automatic Signing
mit `-allowProvisioningUpdates` und `-allowProvisioningDeviceRegistration`.
Dafür wird ein kleines Hilfsprojekt unter `.local/ios/SigningHelper/` gebaut,
aber **nicht auf dem iPhone installiert**. Das bisherige Team und die Bundle-ID
bleiben erhalten; bei uneindeutiger Team-Zuordnung wird abgebrochen. Ein separates
Signing-Testprojekt muss nicht manuell geöffnet werden. Nach der Erneuerung wird
das Profil erneut geprüft, bevor das eigentliche iOS-Deployment fortgesetzt wird.

Auch `--repair-signing` kann einen Geräte-Build des Hilfsprojekts auslösen.
Agenten holen deshalb unmittelbar vor dessen Ausführung beziehungsweise vor dem
Geräte-Build mit Installation/Start die ausdrückliche Freigabe gemäß `AGENTS.md`
ein. Bei erforderlicher Apple-ID-Anmeldung oder Schlüsselbundfreigabe anhalten:
den bestehenden Account unter **Xcode → Settings → Accounts** erneut anmelden
beziehungsweise die angeforderte Schlüsselbundfreigabe manuell erteilen.

Nach erfolgreicher Installation kann iOS den Start wegen fehlenden
Entwicklervertrauens blockieren. Bei gültiger Signatur und passendem eingebettetem
Profil auf dem iPhone unter **Einstellungen → Allgemein → VPN und Geräteverwaltung
→ Entwickler-App** dem bisherigen Entwickler vertrauen, sofern iOS diesen Schritt
anbietet. Bleibt die Startsperre bestehen, Signatur und Entitlements gezielt prüfen;
keine automatische Deinstallation oder Datenlöschung als Reparatur durchführen.
Das bestehende App-Datenverzeichnis soll beim Update erhalten bleiben.

Team- und Gerätekennungen, Zertifikatsdaten und Signing-Rohlogs bleiben lokal und
unversioniert. Für weitergegebene Diagnosen gelten die Redaktionsregeln unter
„Repository- und Log-Sicherheit“.

### iOS-Simulator-Builds

Für normale UI-/Navigationsprüfungen im iOS-Simulator `Debug` mit
`net11.0-ios` und passender Simulator-RID verwenden. Release nur, wenn
Release-, AOT-, Trimming- oder Device-Verhalten geprüft werden soll.

Bei unerwartet langer `AOTCompile`-Phase zuerst Konfiguration, RuntimeIdentifier
und mögliche Single-Core-Schalter (`-m:1`, `MaxCpuCount=1`,
`BuildInParallel=false`) prüfen.

Gültige Restore-/Build-Zwischenergebnisse und passende native
Simulator-Artefakte wiederverwenden; kein Clean/Rebuild oder Native-Neubuild
ohne konkreten Grund.

### Gezielter iOS-Simulator-Smoke-Test

Der iOS-Simulator ist **keine Standardprüfung** und wird nicht nach jeder Änderung
gestartet. Ein Smoke-Test ist sinnvoll, wenn die konkrete Änderung plausibel
mindestens einen dieser Bereiche beeinflussen kann:

- App-Startup oder Initialisierung;
- Dependency Injection / Startup-Registrierung;
- initiale Shell oder Navigation;
- den ersten sichtbaren Hauptscreen;
- iOS-spezifische Renderer, Handler oder Lifecycle-Logik;
- native iOS-Integration, deren Fehler den App-Start verhindern können;
- Startup-Ressourcen, deren Fehler zu Crash, Black Screen oder fehlendem Hauptscreen
  führen können.

Wenn Build/Unit-Tests die geänderte Eigenschaft bereits ausreichend prüfen und kein
plausibles Startup-/Initial-UI-Risiko besteht, keinen Simulatorlauf ergänzen.

Der Smoke-Test bleibt strikt minimal:

1. passenden iOS-Simulator-Build erfolgreich erstellen;
2. nach der gemäß `AGENTS.md` erforderlichen unmittelbaren ausdrücklichen Freigabe
   genau einen geeigneten Simulator starten;
3. App einmal installieren/starten;
4. prüfen, dass Initialisierung/Splash beendet werden;
5. prüfen, dass der erwartete erste Hauptbildschirm sichtbar ist;
6. prüfen, dass kein Crash oder Black Screen vorliegt;
7. direkt sichtbare Hauptnavigation darf mitgeprüft werden;
8. App danach beenden.

Damit ist der Smoke-Test beendet. Bei fehlgeschlagenem Build, nicht endender
Initialisierung, Crash, Black Screen oder fehlendem Hauptscreen abbrechen und den
Befund berichten; den Prüfumfang nicht ausweiten.

Ausdrücklich **nicht** Bestandteil dieses Smoke-Tests:

- Navigation durch weitere Screens;
- vollständige Tab-/Navigationsprüfung;
- UI-Regressionstest;
- Screenshot-Serie;
- systematisches Prüfen sämtlicher Views;
- Kamera-/Fotoerkennungs-Fachtest;
- Datenbankreset;
- echter Gerätetest;
- Signing-/Provisioning-Reparatur;
- weitere Simulatoren oder Gerätematrix „zur Sicherheit“.

Ein einzelner Screenshot des ersten Screens darf nur verwendet werden, wenn er zur
visuellen Bestätigung technisch erforderlich ist. Keine Screenshot-Serie und den
Screenshot nicht ungefragt als Ergebnis bereitstellen.

### Gesperrte App-Assembly

Ist `BoardgameTracker.App.dll` gesperrt:

1. laufende App bzw. Debug-Sitzung beenden und Build wiederholen;
2. bei fortbestehender Sperre die Solution schließen;
3. keinen Datenbankreset als Buildfix verwenden.

## 5. Entwicklungsdatenbank

Der Runtime-Start verwendet:

```text
Database.EnsureCreatedAsync()
```

Danach werden notwendige Referenzdaten idempotent ergänzt.

Für die aktuelle Pre-Alpha-Phase gilt:

- keine EF-Migrationsstrategie;
- keine Schema-Upgrades für ältere Entwicklungsdatenbanken;
- keine Compatibility-Aliase nur zum Erhalt alter Entwicklungsschemata;
- `EnsureCreated` migriert vorhandene Schemas nicht;
- bei inkompatiblen Schemaänderungen ist ein bewusster Neuaufbau bzw. eine
  Neuinstallation der Entwicklungsdatenbank zulässig;
- vor einem notwendigen Neuaufbau ausdrücklich auf möglichen lokalen Datenverlust
  hinweisen;
- vorhandene App-Daten niemals ungefragt löschen;
- vor dem ersten Deployment wird die Datenbankstrategie neu festgelegt.

Historische einzelne Schemaumbrüche gehören in `CHANGELOG.md`, nicht dauerhaft in
diese Setup-Anleitung.

Kompatibilität von `.bgt`-Dateien und Spielschemas steht in
`docs/IMPORT_EXPORT.md`.

## 6. Versionsregel

In `src/BoardgameTracker.App/BoardgameTracker.App.csproj`:

```xml
<ApplicationDisplayVersion>x.y.z.r</ApplicationDisplayVersion>
<ApplicationVersion>n</ApplicationVersion>
```

Bedeutung von `ApplicationDisplayVersion`:

- `x` **Major**: inkompatibler großer Wechsel oder bewusst gesetzter stabiler Meilenstein;
- `y` **Minor**: neue abwärtskompatible Nutzerfunktion;
- `z` **Patch**: Fehlerbehebung oder Korrektur bestehenden Nutzerverhaltens;
- `r` **Revision**: interne technische Änderung ohne neue Nutzerfunktion oder Nutzer-Bugfix.

Für Änderungsaufträge gilt:

- Reine Markdown-Änderungen (`*.md`, einschließlich `CHANGELOG.md`) erzeugen keinen Versionsschritt.
- Jeder Auftrag mit mindestens einer versionierten Nicht-Markdown-Änderung erzeugt genau einen Versionsschritt.
- Der Entwickler legt die Versionsart vor der Umsetzung fest; der Agent wählt oder erhöht sie nicht eigenständig. Fehlt sie oder passt sie wegen einer Scope-Änderung nicht mehr, wird sie vor der Versionsänderung geklärt.
- Die gewählte Stelle steigt um eins; alle rechts davon liegenden Stellen werden auf `0` gesetzt. `1.0.0.0` oder andere Release-Meilensteine werden nur ausdrücklich festgelegt.
- `ApplicationVersion` steigt bei jedem versionspflichtigen Auftrag unabhängig davon genau um eins.
- Vor Abschluss werden beide Versionswerte und der zugehörige `CHANGELOG.md`-Eintrag geprüft; bei Auslieferung entspricht `ApplicationDisplayVersion` der ausgelieferten Version.
- Der Abschlussbericht enthält einen Committext-Vorschlag mit ein bis zwei Sätzen, beginnend mit der neuen `ApplicationDisplayVersion`. Kein Commit ohne ausdrücklichen Auftrag.

Versionshistorie steht ausschließlich in `CHANGELOG.md`.

## 7. Entwicklungs-Updatepakete

Standardlieferung ist ein **Delta-Paket**, kein vollständiges Repository.

Ein Delta enthält nur:

- geänderte und neue Dateien;
- `UPDATE_FILES.txt` bzw. kompatibel `DELETE_FILES.txt`, wenn Dateien gelöscht,
  verschoben oder umbenannt werden;
- bei jeder Operationsdatei `APPLY_UPDATE.cmd` und `APPLY_UPDATE.ps1`.

Nicht enthalten werden `bin/`, `obj/` oder andere generierte Build-Artefakte.

Unterstützte Operationen:

```text
DELETE|pfad/zur/datei
MOVE|alter/pfad|neuer/pfad
RENAME|alter/pfad|neuer/pfad
```

Einfache Pfadzeilen in `DELETE_FILES.txt` gelten aus Kompatibilitätsgründen als
`DELETE`.

Die Apply-Skripte bleiben generisch. Sie müssen:

- alle Pfade relativ zum Repository-Root behandeln;
- Pfade außerhalb des Repositorys ablehnen;
- fehlende Dateien bei `DELETE` tolerieren;
- Zielordner für `MOVE`/`RENAME` anlegen;
- bei echten Fehlern mit Fehlercode abbrechen.

### Updatepaket übernehmen

1. Update-ZIP in den Repository-Root entpacken bzw. übernehmen.
2. Falls `UPDATE_FILES.txt` oder `DELETE_FILES.txt` enthalten ist,
   `APPLY_UPDATE.cmd` im Repository-Root ausführen.
3. `bin` und `obj` ausschließlich unter dieser Solution bereinigen.
4. NuGet Restore.
5. Solution neu bauen.
6. erforderliche automatisierte Tests ausführen.
7. Geräte-/Installations-/Releaseprüfung nur gemäß `AGENTS.md`.

## 8. Abhängigkeiten und Lizenzen

Bei Änderungen an Drittanbieterabhängigkeiten gelten die Regeln aus
[`docs/THIRD_PARTY_LICENSE_POLICY.md`](docs/THIRD_PARTY_LICENSE_POLICY.md).

Lizenzanzeige, Produktverhalten und konkrete Assetstruktur werden hier nicht
dupliziert.

## 9. Weiterführende Quellen

- Arbeitsweise und Verifikation: `AGENTS.md`
- Dokumentationszuständigkeiten: `docs/README.md`
- Softwarearchitektur: `docs/ARCHITECTURE.md`
- Import/Export: `docs/IMPORT_EXPORT.md`
- Drittanbieter-Lizenzen: `docs/THIRD_PARTY_LICENSE_POLICY.md`
- historische Änderungen: `CHANGELOG.md`
