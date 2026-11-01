# Boardgame Tracker – Setup und Entwicklung

## 1. Zweck

Diese Datei ist die kanonische Quelle für **Entwicklungsumgebung, Build/Test,
Entwicklungsdatenbank, Versionierung und Lieferung**.

Nicht hierher gehören Produktstatus, Softwarearchitektur, UI-Regeln, Forschungsstände
oder historische Schemaänderungen. Dafür gelten die jeweils zuständigen Dokumente
unter `docs/`.

Arbeits- und Verifikationspolicy: [`AGENTS.md`](AGENTS.md).

## 2. Voraussetzungen

- .NET 10
- .NET MAUI
- EF Core 10
- SQLite
- Visual Studio bzw. passende .NET-MAUI-Toolchain
- Android- und/oder iOS-Toolchain für die jeweilige Zielplattform

Unterstützte Mindestplattformen gemäß App-Projekt:

```text
Android 11 / API 30
iOS 15
```

Lokale macOS-/iOS-Konfiguration darf unter `.local/` liegen. Dieser Bereich wird
nicht versioniert. Für private Konfiguration und Diagnoseausgaben gelten die
folgenden Sicherheitsregeln.

Paketversionen werden zentral über `Directory.Packages.props` verwaltet.

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

Für jeden abgeschlossenen Änderungsauftrag gilt:

- `ApplicationDisplayVersion` und `ApplicationVersion` werden beide erhöht;
- dies gilt auch für reine Dokumentations-, Test- oder Konfigurationsänderungen;
- mehrere Dateien und Korrekturschritte desselben Auftrags erzeugen keine
  zusätzlichen Erhöhungen;
- ohne ausdrücklich festgelegten größeren Versionssprung steigt die letzte
  Komponente von `ApplicationDisplayVersion` um eins;
- `ApplicationVersion` steigt ebenfalls um eins;
- beide Werte müssen gegenüber dem Auftragsbeginn erhöht sein;
- bei Auslieferung entspricht `ApplicationDisplayVersion` exakt der ausgelieferten
  Version;
- vor Abschluss werden beide Werte und der zugehörige `CHANGELOG.md`-Eintrag geprüft;
- vor einer Verpackung werden sie erneut geprüft.

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
