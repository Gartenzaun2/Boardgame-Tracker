# Gemeinsamer nativer Ark-Nova-CV-Kern und Mobile-Host

**Native Mobile-CV fachlich freigegeben für assistierte Wertung.**
Erkennungsforschung bleibt eingefroren. A nutzt weiterhin die alte Referenz,
v2-Geometrie, A_NoZero, Confidence E, RegistrationEdge 1600 und AnalysisScale 0,5.
B/C/C3 sind keine mobilen Produktionsassets.

Die [direkte Ground-Truth-Abnahme](NativeGroundTruthAcceptance.md) des bestehenden
Runs ergibt nach E 294 statt 297/411 richtige Werte und 11 statt 10/120
Empty-Phantome. V3 False High bleibt 8, keine neuen falschen Highs.
Die dokumentierten Plattformabweichungen sind durch ausdrückliches fachliches
GO akzeptiert: keine neuen False Highs oder High-Phantome, Nutzerprüfung/Korrektur
und ausdrückliche Bestätigung vor Speicherung. Das frühere GT-Gate-FAIL bleibt
historisch dokumentiert; es blockiert die assistierte Produktfreigabe nicht mehr.
Native Mobile ist Produktionspfad, Windows/OpenCvSharp Referenz-/Regressionstool.
Keine neue CV-Ausführung, Ursachenforschung, Optimierung oder Parameteränderung.
Nach E: 115 High (107 richtig/8 falsch), 77 Medium (72 richtig/5 falsch),
219 Uncertain einschließlich 68 ohne Wert. Alle Ergebnisse bleiben Vorschläge.
Keine weitere CV-Forschung geplant. Offen sind ausschließlich die manuellen
Runtime-Nachweise: echter Android-ARM64-Gerätelauf und
`iOS build/link/runtime verification pending manual user verification`.

## Architektur und aktueller Nachweis

Die gemeinsamen Quellen liegen unter
`src/BoardgameTracker.App/Games/ArkNova/PhotoRecognition/Native`.
Dieses Werkzeug baut dieselben Quellen und ergänzt ausschließlich CLI-Diagnostik.
C++ übernimmt Decode/EXIF nach BGR8, Registrierung/SIFT/BF/RANSAC, Gates, Warp,
LAB/HSV/Helligkeitsnormalisierung, Difference/Farbmasken/Grow und Field/Core-Evidenz.
Application besitzt denselben unveränderten Ranking-/Confidence-Code für App
und Windows-Referenz. MAUI besitzt Erwerb, Review, Logging und Bestätigung.

C-ABI v1 bleibt kompatibel zum alten Probe-Aufruf und ergänzt
`anv_engine_create/analyze/destroy`. Keine C++-Typen über ABI. Ein SafeHandle
besitzt Referenz/Masken; C# serialisiert Analyse und Freigabe. Gepinnte Eingaben
sind nur während des synchronen Aufrufs geliehen, Ausgabepuffer gehören C#.
Exceptions verlassen C++ nicht; Fehlercodes werden erst an der UI-Boundary in
verständliche Meldungen übersetzt. Technische Details gehen ins Plattformlog.

| Prüfung | Ergebnis |
|---|---|
| C++ Android ARM64/x64 Release | baut mit NDK r30 / OpenCV 4.13.0 |
| MAUI Android ARM64/x64 Release, vollständiges Trimming | baut; EF-/JSON-Trimwarnungen bleiben |
| Android API 30 x64 | tatsächlicher App-/Interop-/Picker-/Review-/Persistenzlauf, isoliertes Testpaket |
| 54 bekannte Regressionfotos, 648 Entscheidungen | Registrierung 54/54, **30 Rohwert- und 27 E-Abweichungen** |
| Fachliche Produktentscheidung | **GO für assistierte Wertung**; Plattformabweichungen bewusst akzeptiert |
| Historischer Paritäts-/GT-Gatevergleich | fehlgeschlagen, Messwerte unverändert; durch fachliches GO als Freigabeblocker aufgehoben |
| iOS C# auf Windows | kompiliert; kein Apple-Link-/Runtime-Nachweis |
| ARM64-Gerät | nicht ausgeführt |

Messung: [mobile-parity.json](mobile-parity.json). Gleicher Registrierungserfolg
ist keine funktionale Parität. Die Vergleiche verwenden einmal unter Windows
normalisierte PNGs, um unterschiedliche JPEG-Decoder/EXIF von der CV-Parität
zu trennen. Matches/Inlier/p95/Visibility/Spread, Homographie, Warpabweichungen,
MaskFraction-Abweichungen und alle abweichenden Wert-/Confidence-Entscheidungen
werden protokolliert. Feature-Reihenfolge/Gleitkomma-Kontraktion wurden technisch
untersucht, ohne die Parität herzustellen; vollständige Ursache nicht nachgewiesen.
Keine Änderungen an eingefrorenen Parametern, keine Einzelfallregeln.
Ein erneuter Windows-Lauf reproduziert Registrierung, Rohwerte, E und alle
Feldevidenzen auf allen 54 Bildern unverändert. Bei zwölf verglichenen Warps
liegt die native MAE zwischen 2,2722 und 9,6611; dies ist kein bestandenes Gate.

Kamera und Systempicker benutzen denselben Service. Android-Kamera verwendet
ActivityResultContracts.TakePicture mit temporärem FileProvider-URI; nur CAMERA,
keine Speicher-/Bibliotheksberechtigung. iOS nutzt MAUI CapturePhotoAsync und
Systempicker. JPEG/PNG bleiben unverändert beim gemeinsamen OpenCV-Decoder;
andere unterstützte Formate werden über ImageDecoder bzw. UIImage orientiert als
PNG normalisiert. Der Review ist vom manuellen Autosave getrennt. Erst Bestätigung
validiert und speichert einen Spielerbatch in einem SaveChangesAsync.

Release-Trimming benötigt erhaltene EF-/Domain-/Mapping-Metadaten. Die
entsprechenden TrimmerRootAssembly-Einträge sind im Host dokumentiert; sie ersetzen
keine Warnungsfreiheit oder einen iOS-Laufzeitnachweis. Es gibt keine DB-Migration.

## Reproduktion

Offizieller unveränderter OpenCV-4.13.0-Checkout:
`fe38fc608f6acb8b68953438a62305d8318f4fcd`.
Toolchains, Quellcheckout, Libraries, APKs und Bildvergleichsdaten bleiben unter
ignoriertem `artifacts/`. Bezugsdaten: [dependencies.json](dependencies.json).

```powershell
& tools/Games/ArkNova/NativeVisionSpike/Build-Android.ps1 `
  -NdkDirectory artifacts/native-spike/android-ndk-r30 `
  -OpenCvSource artifacts/native-spike/opencv `
  -CMake "$PWD/artifacts/native-spike/cmake-3.31.6-windows-x86_64/bin/cmake.exe" `
  -Ninja "$PWD/artifacts/native-spike/ninja/ninja.exe"

dotnet publish src/BoardgameTracker.App -f net10.0-android -c Release `
  -p:RuntimeIdentifier=android-arm64 -p:PublishTrimmed=true -p:TrimMode=full
```

Für x64 zusätzlich `-Abi x86_64 -BuildDirectory artifacts/native-spike/android-x64`
beim C++-Build und `-p:RuntimeIdentifier=android-x64` bei MAUI. Andere native
Ausgabeordner über `ArkNovaNativeBuildDirectory`/`ArkNovaNativeAbi` übergeben.
Zum Runtime-Test wird ausschließlich das isolierte ApplicationId
`de.boardgametracker.vision.integration` verwendet, keine Benutzer-App zurückgesetzt.

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- native-parity-export . artifacts/native-product/golden
# CLI + .so und golden-Verzeichnis auf explizit gewähltes Android-Testgerät kopieren.
# Mit LD_LIBRARY_PATH aufrufen:
# arknova_probe --self-test
# arknova_probe --engine-self-test <golden-Verzeichnis>
# arknova_probe --score <golden-Verzeichnis>
# native-results.jsonl zurückholen, danach:
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- native-parity-compare artifacts/native-product/golden artifacts/native-product/native-final-results.jsonl artifacts/native-product/parity-final.json
```

Der Vergleich liefert Exitcode 1 bei fachlicher Abweichung. Alle bekannten Farben,
Tracks, null-Werte und Confidence-Klassen zählen; keine Auswahl nur erfolgreicher
Fälle. CLI-Tests prüfen ungültige Eingaben, kleine Evidenz-/Warp-Puffer,
Registrierungsfehler, unveränderte Fehlerausgabe und sichere Freigabe.

## iOS: implementierter Pfad, Apple-Verifikation offen

Auf einem Mac mit Xcode, CMake und .NET-iOS-Workload:

```bash
bash tools/Games/ArkNova/NativeVisionSpike/Build-iOS.sh \
  artifacts/native-spike/opencv artifacts/native-spike/ios-arm64
dotnet build src/BoardgameTracker.App -f net10.0-ios -c Release \
  -p:RuntimeIdentifier=ios-arm64
```

Das Skript baut Device ARM64 (iOS 15), führt die tatsächlich gebauten OpenCV-/Codec-
Archive mit libtool zusammen und prüft die ABI-Exporte. NativeReference lädt das
Archiv vollständig; C++/zlib und Foundation/CoreGraphics/ImageIO/Accelerate sind
deklariert. P/Invoke verwendet __Internal. Ein fehlendes Archiv stoppt den
Mac-Build mit einem konkreten Fehler. Auf Windows wird nur der iOS-C#-Pfad kompiliert.

Simulator separat bauen, niemals Device-Archive wiederverwenden:

```bash
ANV_IOS_SDK=iphonesimulator ANV_IOS_ARCH=arm64 \
  bash tools/Games/ArkNova/NativeVisionSpike/Build-iOS.sh \
  artifacts/native-spike/opencv artifacts/native-spike/ios-simulator-arm64
dotnet build src/BoardgameTracker.App -f net10.0-ios -c Release \
  -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:ArkNovaIosBuildDirectory="$PWD/artifacts/native-spike/ios-simulator-arm64"
```

Intel-Simulator analog mit x86_64/iossimulator-x64. Signing bleibt lokale
Benutzerkonfiguration. Kamera-Nutzungstext, Systempicker und UIImage-Normalisierung
sind implementiert; keine NotSupported-Stubs. Dieser Stand beweist keinen Apple-Build,
keine Linkauflösung und keinen iPhone-/Simulatorlauf:
**iOS build/link/runtime verification pending manual user verification**.

OpenCV und eingebundene JPEG/PNG-Codecs sind mit ihren Lizenztexten im MAUI-Paket.
Keine Toolchains, Bibliotheksbinärdateien oder Vergleichsfotos werden versioniert.
Die früheren Spike-Nachweise [reference.json](reference.json),
[parity-android-x64.json](parity-android-x64.json) und
[verification.json](verification.json) bleiben unverändert historische Messungen.
