# Arche-Nova-Fotoanalyse – technischer Prototyp, Phase 1A

Ausführbares .NET-10-Werkzeug für Windows x64. Es verarbeitet eine Bilddatei lokal
und liefert Registrierung, zwölf Feldkandidaten, Confidence, mögliche Nachbarwerte
und Stufenzeiten als JSON. Es ist noch keine mobile Kamera-Funktion.

## Ausführen

Vom Solution-Root in PowerShell:

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- benchmark tools/Games/ArkNova/PhotoRecognition/Assets artifacts/ark-nova-benchmark.json
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- analyze tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/IMG_4257.JPG artifacts/ark-nova-photo.json
```

`benchmark` prüft alle sechs Fotos gegen die Ground Truth und zusätzlich ein leeres
Bild, eine halbierte Referenztafel sowie die leere Referenztafel. Exitcode 0 verlangt
genau 72 ausgewertete Werte, mindestens 60 Treffer, keine falschen High-Werte in den
drei bekannten Rot-Artenschutz-Konflikten, abgewiesene schlechte/unvollständige
Registrierung und keine High-Kandidaten auf der leeren Referenztafel.
`analyze` liefert bei gescheiterter Registrierung Exitcode 1 und eine leere Farbmenge;
Exitcode 0 bestätigt nur die Registrierung, nicht die Richtigkeit der Kandidaten.
Aufruf ohne gültige Argumente: Exitcode 2.

Ergebnisse sind technische Diagnostik. Der Analyzer kennt weder Spielernamen noch
Partie-IDs und persistiert keine Wertung. Ein fehlender Kandidat ist `null`, kein
scheinbar erkannter Nullwert. Alle Schwellen sind experimentell.

## Aufbau und Herkunft

- `Assets`: unverändert übernommenes, vom Benutzer bereitgestelltes Masterbild,
  Polygongeometrie v2, Ground Truth und sechs Fotos. SHA-256-Werte stehen in
  `Assets/manifest.json`. Rund 27 MB, ausschließlich Entwicklungsdaten;
  die App verpackt diese Dateien nicht.
- `ArkNovaPhotoAnalyzer`: EXIF-orientiertes Decode, SIFT auf maximal 1600 px langer
  Kante, Ratio-Test, RANSAC, Qualitäts-/Sichtbarkeitsprüfung, Warp auf halbe
  Masterauflösung, Analyse ausschließlich des Scoring-Bereichs, physische HSV-Masken
  kombiniert mit lokal helligkeitsnormalisierter LAB-Referenzdifferenz,
  Occupancy für Feld und erodierten Kern, konservative Nachbarfeldprüfung.
- `Application/.../Scoring/ArkNovaFieldOccupancy`: bildbibliotheksfreie
  Score-/Confidence-Formeln und Konfliktprädikat. Deren Unit Tests laufen in der
  vorhandenen Application-Testsuite.
- Referenzfeatures werden einmal pro Analyzer-Aufbau berechnet und im Speicher
  wiederverwendet. Es existiert noch kein serialisiertes Descriptor-Asset.
- `benchmark-results.json`: gemessener Release-Lauf vom 30.10.2026 inklusive aller
  Vorhersagen, Fehler und Stufenzeiten. Neue Ergebnisse gehören unter `artifacts/`.

Die ursprünglichen Python-Skripte und vollständigen Segmentierungsparameter waren
nicht im Paket enthalten. Dies ist eine rekonstruierte Pipeline und keine exakte
Portierung. Gleiche Gesamtgenauigkeit bedeutet nicht dieselben Einzelfehler.
Es werden weder OCR noch Ground-Truth-Werte zur Erkennung verwendet.

## Gemessener Stand

Windows 10.0.26200, .NET 10.0.14, OpenCV 4.13, 16 logische Prozessoren:

| Foto | Richtige Werte |
|---|---:|
| IMG_4257 | 10/12 |
| IMG_4258 | 10/12 |
| IMG_4259 | 11/12 |
| IMG_4260 | 9/12 |
| IMG_4261 | 10/12 |
| IMG_4262 | 10/12 |
| Gesamt | 60/72 |

21 High-Werte und 7 Medium-Werte sind auf diesem Entwicklungsset korrekt,
44 Werte sind Uncertain (32 richtig). Gegenüber dem früheren PoC mit 42 High-Werten
ist die High-Abdeckung deutlich geringer. Die Konfliktschicht schreibt keinen Wert um.
Zusätzlich zu den ursprünglichen Regeln genügt im rekonstruierten Prototyp bereits
6 % erweiterte Farbmaske im Nachbarfeld zum konservativen High-Veto.
Unvollständig sichtbare Tracks werden ebenfalls als Uncertain behandelt.
Die ursprüngliche Richtungsheuristik ist ohne kalibrierte Implementierung im
Analyzer deaktiviert. JSON-Warncodes unterscheiden Nachbarfeldkonflikte und
unvollständige Tracks.

Alle sechs Fotos registrieren erfolgreich. Pro Foto: rund 439–733 ms inklusive
Decode; einmalige Referenzvorbereitung rund 346 ms. Prozessweiter Peak Working Set
rund 659 MiB während des gesamten Benchmarks einschließlich Negativtests;
das ist keine isolierte Messung des Speicherbedarfs einer mobilen Einzelanalyse.
Die JSON-Datei enthält die genauen Werte je Stufe.

Der kleine Datensatz wurde zur Entwicklung benutzt. Die Messung ist weder eine
unabhängige Genauigkeitsvalidierung noch ein Gerätebenchmark. Es wurden keine
weiteren Schwellen-Suchläufe zur Maximierung der Trefferzahl durchgeführt.

## Bibliotheksentscheidung und nächste Phasen

OpenCvSharp4 4.13.0.20260627 mit der Slim-Windows-Runtime ist ausschließlich eine
Desktop-Spike-Abhängigkeit. Das Slim-Profil enthält die hier benötigten
Feature-/Homographie-Module; die drei Produktionsprojekte bleiben ohne CV-Pakete.
Die Paketversion ist zentral festgelegt. OpenCvSharp und OpenCV stehen unter
Apache-2.0; mitgelieferte native Drittkomponenten benötigen bei einer späteren
Auslieferung eine gesonderte Lizenzprüfung.

Die aktuelle Android-Vorschau von OpenCvSharp enthält nur core/imgproc/imgcodecs;
sie genügt nicht für diese SIFT-/RANSAC-Pipeline. Ein geeigneter iOS-Pfad wurde
damit ebenfalls nicht nachgewiesen. Native-AOT, Trimming, mobile Paketgröße und
Geräteperformance sind offen. Diese Paketwahl ist keine mobile Architekturentscheidung.

Quellen: [OpenCvSharp-Plattformen und Profile](https://github.com/shimat/opencvsharp),
[Android-Profil](https://shimat.github.io/opencvsharp/articles/getting-started/android.html),
[OpenCvSharp-Lizenz](https://github.com/shimat/opencvsharp/blob/main/LICENSE),
[OpenCV-Lizenz](https://opencv.org/license/).

Vor Phase 1B/1C sind nötig:

1. Mobile CV-Bibliothek mit Feature-Matching/Homographie für Android und iOS
   auswählen und Release-/Trimming-/Paketgrößenverhalten nachweisen.
2. Referenzfeatures als versioniertes Asset vorberechnen; Laufzeit und Speicher auf
   echtem Mittelklassegerät messen. Die 2-/4-/5-Sekunden-Vorgaben sind bisher
   ausschließlich Ziele; ein harter mobiler 5-Sekunden-Abbruch ist nicht implementiert.
3. 10–30 unabhängige Fotos und weitere Negativfälle zur Kalibrierung auswerten.
4. Erst dann den bestehenden Kamera-Handler anbinden und Ergebnisse nach Spielerfarbe
   in den vorhandenen Scoring-Entwurf übernehmen; manuelle Korrektur bleibt möglich.

Keine neue Kameraoberfläche, kein paralleler Scoring-Flow und kein zusätzliches
Produktionsprojekt wurden angelegt.
