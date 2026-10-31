# Gemeinsamer nativer OpenCV-Kern – Architektur-/Build-Spike

**Spike, nicht Produktion. Vollständiger A-Port derzeit nicht freigegeben.**
Android ARM64 baut; SIFT/Matching/RANSAC/Warp und getrimmtes C#-Interop laufen
auf Android x64. Die Zwischenwerte sind aber nicht numerisch äquivalent zur
Windows-Referenz. iOS wurde auf Benutzerwunsch vorerst ausschließlich anhand
offizieller Dokumentation geprüft. Keine C++-Portierung der App, keine mobile
OpenCvSharp-Abhängigkeit und keine kommerzielle Bibliothek.

## Nachweise und Entscheidung

| Gate | Tatsächlicher Nachweis | Status |
|---|---|---|
| Android ARM64 | NDK r30, API 30, Release-C++-Build und signiertes Release-Test-APK | bestanden, ARM64-Laufzeit ungetestet |
| iOS ARM64 | gemeinsamer Quellcode, Xcode/CMake-Rezept, statisches Archiv und NativeReference vorbereitet; Dokumentationsabgleich unten | dokumentarisch geprüft; Build/Laufzeit auf Benutzerwunsch übersprungen |
| SIFT/Matching/Homographie/Warp | über echte .NET-P/Invoke-Aufrufe im Android-API-35-x64-Emulator ausgeführt | bestanden auf x64 |
| Zwischenwerte wie Referenz | IMG_4257 gegen unveränderten OpenCvSharp-Analyzer | **nicht bestanden** |
| Release/Trimming/Linking | `PublishTrimmed=true`, `TrimMode=full`, Android ARM64/x64; x64-Aufruf ausgeführt | Android bestanden; iOS ungetestet |
| Größe | ARM64 `.so` 4.818.408 Bytes, x64 5.800.960 Bytes; APK unten | gemessen, keine Produktprognose |

Die Toleranzen standen **vor dem ersten nativen Lauf** in `ProbeCheck.cs`:
gleiche Match-/Inlierzahl, p95-Differenz ≤1e−6, Warp-MAE ≤0,1 und maximaler
Pixelunterschied ≤8. Tatsächliches Ergebnis:

| Zwischenwert | Windows/OpenCvSharp 4.13.0 | Android/OpenCV 4.13.0 x64 |
|---|---:|---:|
| Matches | 415 | 415 |
| Inlier | 288 | 274 |
| Reprojektion p95 | 2,4184011583 | 2,3938570445 |
| Warp-MAE / Maximaldifferenz | Referenz | **8,354142381 / 250** |

[Referenz](reference.json), [native Messung](parity-android-x64.json),
[Verifikation](verification.json), [Dependencies](dependencies.json).
Die Ursache der Abweichung ist nicht nachgewiesen. Gleiche Versionsnummer und
Matchzahl beweisen keine Gleichheit von Features, Match-Reihenfolge oder RANSAC.
Kein Nachjustieren von Erkennungsschwellen oder Toleranzen und keine Fehlerbild-
Optimierung. Die notwendige technische Paritätsklärung ist noch offen; deshalb
kein vollständiger A-Port, keine Occupancy- oder Scorefreigabe und noch keine
Kamera-/Picker-/Bestätigungsintegration in der App.

## Grenze und Speicherbesitz

`IArkNovaVisionEngine` lebt zunächst nur in diesem Werkzeug. `ProbeRegistration`
nimmt zwei normalisierte BGR8-Bilder an. Ein grober C-Aufruf erledigt Resize,
SIFT(6000), BF/L2-KNN, Ratio-Test 0,72, RANSAC(3 px), Transform und Warp. Diese
Parameter stammen aus A; keine neue Erkennungslogik. Es fehlen noch A-Footprint-,
Feldsichtbarkeits-/Spread-Gates, Masken und Occupancy. Ein erfolgreiches Probe-
Ergebnis ist ausdrücklich **keine** akzeptierte Boardregistrierung oder Wertung.

C-ABI v1: zwei Exporte (`anv_abi_version`, `anv_probe`), 64-Bit-Targets,
32-Byte-Bildstruktur, 104-Byte-Ergebnisstruktur, feste Ganzzahltypen und Cdecl.
Keine `cv::Mat`, C++-Strings oder Container über die Grenze. Eingabepuffer sind
nur synchron geliehen; C# pinnt sie während des Aufrufs. Ausgabe gehört ebenfalls
dem Aufrufer. Native Matrizen werden ausschließlich im C++-Aufruf freigegeben.
Keine nativen Handles oder nachträglichen Free-Aufrufe. Fehlercodes 1–4 unterscheiden
ungültige Eingabe, zu kleinen Ausgabepuffer, Registrierungsausfall und internen
Fehler. Bei Fehler Ergebnis nullgesetzt, Bildausgabe ignorieren. Exceptions
verlassen C++ nie. Die C#-Exception ist technische Diagnose, kein UI-Text.

Die gemeinsamen Quelldateien werden für beide Plattformen verwendet. Android:
NDK/CMake, statisches OpenCV/C++ in einer `.so`, nur die C-ABI exportiert und
16-KiB-Segmentausrichtung. iOS: gleiches C++, statische Archive zusammenführen,
`NativeReference`/`__Internal`, C++-Runtime und zlib beim Apple-Linken.
Source-generiertes `LibraryImport` verwendet nur blittable Daten; der tatsächliche
getrimmte Android-Aufruf belegt die Managed/Native-Strukturübereinstimmung.

Der endgültige Engine-Vertrag erhält erst beim Port die tatsächlichen Messwerte.
Ranking, Confidence E, Rundungshärtung, Feld-/Konfliktlogik, Domain und UI sollen
in C# erhalten bleiben. Decoder/EXIF-Normalisierung sind mit den BGR-Spike-Dateien
noch nicht mobil nachgewiesen. Kein zweiter dauerhafter Produktdetektor:
Windows/OpenCvSharp bleibt Referenz; dieser Backendpfad soll später Produktion sein.

## Reproduzieren ab Repository-Root

Normale externe Buildwerkzeuge, keine globalen Installationsänderungen.
Downloads/Quellen/Builds liegen lokal unter ignoriertem `artifacts/native-spike`.
Die konkreten URLs, Revisionen und beim Bezug gemessenen Prüfsummen stehen in
`dependencies.json`. OpenCV muss ein unveränderter Checkout des offiziellen
4.13.0-Commits `fe38fc608f6acb8b68953438a62305d8318f4fcd` sein.

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- native-reference tools/Games/ArkNova/PhotoRecognition/Assets artifacts/native-spike/parity

& tools/Games/ArkNova/NativeVisionSpike/Build-Android.ps1 `
  -NdkDirectory artifacts/native-spike/android-ndk-r30 `
  -OpenCvSource artifacts/native-spike/opencv `
  -CMake "$PWD/artifacts/native-spike/cmake-3.31.6-windows-x86_64/bin/cmake.exe" `
  -Ninja "$PWD/artifacts/native-spike/ninja/ninja.exe"

dotnet build tools/Games/ArkNova/NativeVisionSpike/ManagedSmoke/NativeVisionSmoke.csproj `
  -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm64 `
  -p:AndroidNativeAbi=arm64-v8a `
  "-p:NativeBuildDirectory=$PWD/artifacts/native-spike/android-arm64"
```

Für Emulator x64 beim C++-Build zusätzlich `-Abi x86_64 -BuildDirectory
artifacts/native-spike/android-x64`; beim .NET-Build `android-x64`, `x86_64` und
dieses NativeBuildDirectory verwenden. Ein verbundenes Ziel explizit per ADB-Seriennummer
auswählen, das separate `de.boardgametracker.vision.spike`-APK installieren/starten.
Es schreibt einen JSON-Bericht unter dem Logcat-Tag `ArkNovaVisionSpike` und
in seinen privaten Dateien. `passed=false` muss als fehlgeschlagenes Gate gelten;
ein erfolgreicher Build allein bestätigt keine Parität. Das native CLI bietet
zusätzlich `--self-test`: null/zu kurze Eingabe, zu kleine Ausgabe, leeres Bild,
null Ergebniszeiger, unveränderte Ausgabe bei Fehler. Alle sechs Checks liefen
im Android-x64-Emulator erfolgreich. Die Produkt-App wird nicht gestartet.

Das Referenzexport-Kommando verarbeitet ausschließlich das bekannte Entwicklungs-
foto IMG_4257. Es öffnet das FinalAcceptance-Foto nicht. Die normalisierten BGR-
Dateien sind technische Vergleichsdaten, keine neuen Regression-Fotoduplikate im Git.

Auf einem später freigegebenen Mac mit Xcode, CMake 3.31.6 und .NET-iOS-Workload:

```bash
bash tools/Games/ArkNova/NativeVisionSpike/Build-iOS.sh \
  artifacts/native-spike/opencv artifacts/native-spike/ios-arm64
dotnet build tools/Games/ArkNova/NativeVisionSpike/ManagedSmoke/NativeVisionSmoke.csproj \
  -f net10.0-ios -c Release -p:RuntimeIdentifier=ios-arm64 \
  -p:NativeBuildDirectory="$PWD/artifacts/native-spike/ios-arm64"
```

Gerätesigning über die reguläre eigene Toolchain; keine Signing-Daten im Repo.
Ein späterer iOS-Simulator braucht ein gesondertes natives Simulatorarchiv;
das Device-Archiv darf nicht dafür verwendet werden.

## iOS-Dokumentationsabgleich – kein Buildnachweis

- CMake empfiehlt den Xcode-Generator mit `CMAKE_SYSTEM_NAME=iOS`; Architektur,
  SDK und Deployment Target werden über die entsprechenden OSX-Variablen gesetzt.
  Das Rezept setzt iphoneos/arm64/15.0. [CMake Toolchains](https://cmake.org/cmake/help/latest/manual/cmake-toolchains.7.html#cross-compiling-for-ios-tvos-visionos-or-watchos).
- Offizielles OpenCV beschreibt den Apple-/Xcode-Build und ARM64-Auswahl;
  SIFT gehört zum Hauptrepository. Es werden weder contrib noch nonfree aktiviert.
  [OpenCV iOS](https://docs.opencv.org/4.13.0/d5/da3/tutorial_ios_install.html),
  [SIFT](https://docs.opencv.org/4.13.0/da/df5/tutorial_py_sift_intro.html).
- .NET-iOS dokumentiert native Verweise; der offizielle Resolver erkennt `.a`
  als Static. Das Projekt bindet das zusammengeführte Archiv ein, setzt ForceLoad
  und C++-/zlib-Linkflags. Statische P/Invoke-Symbole werden aus `__Internal`
  aufgerufen. [NativeReference](https://learn.microsoft.com/en-us/dotnet/ios/building-apps/build-items#nativereference),
  [Resolver](https://github.com/dotnet/macios/blob/main/msbuild/Xamarin.MacDev.Tasks/Tasks/ResolveNativeReferences.cs),
  [Microsoft C++-Interop](https://github.com/MicrosoftDocs/xamarin-docs/blob/live/docs/cross-platform/cpp/index.md).

Dieser Abgleich stützt die technische Konstruktion, beweist aber weder Apple-
Compilation noch vollständige Linkauflösung, AOT/Trimming, Gerätestart oder Größe.
Kein iOS-Erfolg wird daraus abgeleitet. Insbesondere bleiben tatsächliche Apple-
Toolchainversion und die auf dem Mac benötigten Abhängigkeiten zu verifizieren.

## Größen und Aufbewahrung

Getrimmtes signiertes ARM64-Test-APK: 46.177.201 Bytes; x64: 46.672.739 Bytes.
Beide enthalten 51.437.927 unkomprimierte Bytes Testassets (inklusive BGR-Golden),
keine Aussage über spätere Produktpaketgröße. Der isolierte gestrippte ARM64-
Kern beträgt ca. 4,60 MiB. iOS-Größe nicht gemessen. Eine Prognose für den vollen
Occupancy-Port ist daraus nicht belastbar.

Nur Quellcode, Rezepte und kleine JSON-Nachweise werden versioniert. Keine
Toolchains, OpenCV-Quellkopie, `.so`/`.a`/APK, Warps oder Featurecaches im Git.
OpenCV ist die offizielle freie Bibliothek; vor einer späteren Auslieferung
gehören seine und die tatsächlich gelinkten Drittanbieter-Lizenztexte zum Paket.
Der Spike ersetzt keine vollständige Auslieferungsprüfung.
