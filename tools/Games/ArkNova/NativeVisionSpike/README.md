# Gemeinsamer nativer Arche-Nova-CV-Kern

## 1. Zweck

Dieses Verzeichnis enthält Build-, Reproduktions- und technische
Verifikationswerkzeuge für den **gemeinsamen nativen C++/OpenCV-Kern** der
Arche-Nova-Fotoerkennung.

Diese Datei definiert **keine Produktfreigabe und keine Produkt-UX**.

Aktueller Forschungs-/Integrationsstatus:
[`docs/Games/ArkNova/CV_STATUS.md`](../../../../docs/Games/ArkNova/CV_STATUS.md).

Produktworkflow:
[`docs/Games/ArkNova/PRODUCT_SPEC.md`](../../../../docs/Games/ArkNova/PRODUCT_SPEC.md).

## 2. Technischer Aufbau

Die produktiven nativen Quellen liegen unter:

```text
src/BoardgameTracker.App/Games/ArkNova/PhotoRecognition/Native
```

Dieses Werkzeug baut dieselben Quellen und ergänzt CLI-Diagnostik.

Der native Kern übernimmt insbesondere:

```text
Decode / EXIF
Registrierung / SIFT / BF / RANSAC
Gates / Warp
LAB / HSV / Helligkeitsnormalisierung
Difference- und Farbmasken
Grow
Field/Core-Evidenz
```

Ranking `A_NoZero` und `Confidence E` liegen gemeinsam für App und
Windows-Referenz in Application.

Die C-ABI bleibt die stabile Grenze. Sie unterstützt den bestehenden Probe-Aufruf
sowie `anv_engine_create`, `analyze` und `destroy`.

- keine C++-Typen über die ABI;
- geliehene Eingabepuffer nur während des synchronen Aufrufs;
- Ausgabepuffer gehören der C#-Seite;
- Exceptions verlassen C++ nicht;
- Engine-Lebensdauer wird auf C#-Seite sicher verwaltet und serialisiert.

## 3. Abhängigkeiten und Artefakte

Verwendeter OpenCV-Checkout:

```text
OpenCV 4.13.0
fe38fc608f6acb8b68953438a62305d8318f4fcd
```

Bezugsdaten: [dependencies.json](dependencies.json).

Toolchains, Quellcheckouts, lokale Libraries, APKs und temporäre
Bildvergleichsdaten liegen unter ignoriertem `artifacts/` bzw. den normalen
`bin/obj`-Verzeichnissen und werden nicht versioniert.

OpenCV und die eingebundenen JPEG-/PNG-Codecs werden in der App mit den
erforderlichen Lizenztexten paketiert.

## 4. Android – nativen Kern bauen

Vom Repository-Root:

```powershell
& tools/Games/ArkNova/NativeVisionSpike/Build-Android.ps1 `
  -NdkDirectory artifacts/native-spike/android-ndk-r30 `
  -OpenCvSource artifacts/native-spike/opencv `
  -CMake "$PWD/artifacts/native-spike/cmake-3.31.6-windows-x86_64/bin/cmake.exe" `
  -Ninja "$PWD/artifacts/native-spike/ninja/ninja.exe"
```

Standardziel ist ARM64. Für x64 zusätzlich:

```text
-Abi x86_64
-BuildDirectory artifacts/native-spike/android-x64
```

Alternative native Ausgabeordner können über
`ArkNovaNativeBuildDirectory`/`ArkNovaNativeAbi` an den App-Build übergeben werden.

### MAUI Android Release

```powershell
dotnet publish src/BoardgameTracker.App -f net10.0-android -c Release `
  -p:RuntimeIdentifier=android-arm64 `
  -p:PublishTrimmed=true `
  -p:TrimMode=full
```

Für x64:

```text
-p:RuntimeIdentifier=android-x64
```

Der aktuelle technische Nachweis umfasst Android ARM64/x64 Builds sowie einen
tatsächlichen API-30-x64-App-/Interop-/Review-/Persistenzlauf mit isoliertem
Testpaket. Der echte ARM64-Gerätelauf ist weiterhin offen; Statusdetails stehen in
`CV_STATUS.md`.

## 5. Native Parity reproduzieren

Golden-Eingaben erzeugen:

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- `
  native-parity-export . artifacts/native-product/golden
```

CLI, native Library und Golden-Verzeichnis auf das **explizit gewählte Testgerät**
übertragen und mit passendem `LD_LIBRARY_PATH` ausführen:

```text
arknova_probe --self-test
arknova_probe --engine-self-test <golden-Verzeichnis>
arknova_probe --score <golden-Verzeichnis>
```

`native-results.jsonl` zurückholen und vergleichen:

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- `
  native-parity-compare `
  artifacts/native-product/golden `
  artifacts/native-product/native-final-results.jsonl `
  artifacts/native-product/parity-final.json
```

Der Vergleich liefert Exitcode `1` bei fachlicher Abweichung. Alle bekannten
Farben, Tracks, null-Werte und Confidence-Klassen zählen; Fälle werden nicht auf
erfolgreiche Ergebnisse vorgefiltert.

CLI-Tests decken außerdem ungültige Eingaben, zu kleine Evidenz-/Warp-Puffer,
Registrierungsfehler, unveränderte Fehlerausgabe und sichere Freigabe ab.

Die aktuellen gespeicherten Vergleichsdaten liegen in
[mobile-parity.json](mobile-parity.json). Produktbewertung und akzeptierte Grenzen
stehen ausschließlich in `CV_STATUS.md`.

## 6. iOS Device

Voraussetzungen: macOS, Xcode, CMake, .NET-iOS-Workload und lokaler
OpenCV-Checkout.

```bash
bash tools/Games/ArkNova/NativeVisionSpike/Build-iOS.sh   artifacts/native-spike/opencv   artifacts/native-spike/ios-arm64

dotnet build src/BoardgameTracker.App -f net10.0-ios -c Release   -p:RuntimeIdentifier=ios-arm64
```

Das Skript:

- baut Device ARM64 für iOS 15;
- führt die gebauten OpenCV-/Codec-Archive mit `libtool` zusammen;
- prüft die benötigten ABI-Exporte.

Die App bindet das resultierende statische Archiv über `NativeReference` ein.
P/Invoke verwendet `__Internal`; C++/zlib sowie
Foundation/CoreGraphics/ImageIO/Accelerate werden gelinkt.

Fehlt das Archiv auf einem echten Apple-Build, bricht der Build mit konkretem
Fehler ab.

## 7. iOS Simulator

Device-Archive niemals für Simulatoren wiederverwenden.

ARM64-Simulator:

```bash
ANV_IOS_SDK=iphonesimulator ANV_IOS_ARCH=arm64   bash tools/Games/ArkNova/NativeVisionSpike/Build-iOS.sh   artifacts/native-spike/opencv   artifacts/native-spike/ios-simulator-arm64

dotnet build src/BoardgameTracker.App -f net10.0-ios -c Release   -p:RuntimeIdentifier=iossimulator-arm64   -p:ArkNovaIosBuildDirectory="$PWD/artifacts/native-spike/ios-simulator-arm64"
```

Intel-Simulator analog mit:

```text
ANV_IOS_ARCH=x86_64
-p:RuntimeIdentifier=iossimulator-x64
```

Signing bleibt lokale Benutzerkonfiguration.

Der Windows-C#-Build des iOS-Targets ist **kein** Apple-Link-/Runtime-Nachweis.
Der aktuelle offene Plattformstatus steht in `CV_STATUS.md`.

## 8. Evidenz

Wichtige gespeicherte Nachweise:

- [NativeGroundTruthAcceptance.md](NativeGroundTruthAcceptance.md)
- [mobile-parity.json](mobile-parity.json)
- [reference.json](reference.json)
- [parity-android-x64.json](parity-android-x64.json)
- [verification.json](verification.json)

Historische Messdateien und Reports werden nicht rückwirkend verändert, um einen
aktuellen Produktstatus abzubilden. Sie bleiben reproduzierbare Evidenz ihres
jeweiligen Laufs.
