# Arche-Nova-Fotoanalyse – technischer Prototyp, Phase 1A

**Aktuelle Entscheidung: Erkennungsforschung ABGESCHLOSSEN / FROZEN.**
Gehärtetes A bleibt Basis, C1/C2 verworfen, C3 research-only, keine F1/F2-Fusion
und keine C3-Warnregel. [Kanonischer Status](../../../../docs/ARK_NOVA_CV_STATUS.md),
[Abschlussmetriken und letztes Stressfoto](Reports/FinalAcceptance/README.md).
Die nachfolgenden Forschungsstände sind historisch; ihre früheren Empfehlungen
öffnen keine neue Forschungsrunde. Der vorgelagerte
[native Backend-Spike](../NativeVisionSpike/README.md) baut für Android ARM64,
verfehlt aber noch das Paritätsgate. Mobile Produktintegration bleibt offen;
OpenCvSharp ist kein Android-/iOS-Produktionsweg. iOS wurde vorerst nur gegen
Dokumentation geprüft. Keine Schwellen-/Confidence-Änderung aus dem Stressfoto.

Ausführbares .NET-10-Werkzeug für Windows x64. Es verarbeitet eine Bilddatei lokal
und liefert Registrierung, zwölf Feldkandidaten, Confidence, mögliche Nachbarwerte
und Stufenzeiten als JSON. Es ist noch keine mobile Kamera-Funktion.

Aktueller Confidence-V2-Stand: **38,3 % Coverage bei 23/23 richtigen akzeptierten
Holdout-Werten**. Das 75-%-Coverage-Ziel wird nicht erreicht. Alle Ergebnisse,
Ablationen, Fehlertypen und Perspektiven stehen in [CONFIDENCE_V2.md](CONFIDENCE_V2.md).

Die reine [Schwarz-/Artenschutz-Diagnose](Reports/BlackConservationDiagnosis/README.md)
für IMG_4265–4268 enthält Masken, Heatmaps, Referenz-/Core-Vergleiche, Pixelstatistiken
und lokale Feature-Residuen. Ground Truth 20 und Polygon 20 sind bestätigt.
Die Messung findet Feld 0 als Fehlgewinner; Feld 21 enthält keine strengen Schwarzpixel.

Das nachfolgende [A/B/C-Rankingexperiment](Reports/RankingExperiment/README.md)
übernimmt ausschließlich den CV-Ausschluss von 0 mit Mindest-Evidenz und Enthaltung.
Die Holdout-Rohgenauigkeit steigt von 42/60 auf 45/60, Schwarz-Artenschutz von
2/5 auf 5/5. Die Confidence-Policy bleibt unverändert. `analyze` verwendet diese
ausgewählte Rangfolge; historische Vergleichs-/Diagnosebefehle behalten ihre Baseline.

Der [Research Batch V3](Reports/ResearchV3/README.md) vergleicht das unveränderte A
mit zwei farbunabhängigen Struktur-/Empty-Referenzvarianten. Ergebnis: A 103/156,
B1_pool 2/156, Oracle 104/156; kein Ensemble empfohlen. Die Confidence akzeptiert
auf V3 14 falsche Werte unter 75 akzeptierten. Ein numerischer A-Ausfall zählt als
Fehler. Original-Freeze, technischer Abbruch und Wiederaufnahme ohne Parameteränderung
sind offengelegt. DINOv3 war ohne bereitgestellte Gewichte innerhalb der autorisierten
Datengrenze nicht ausführbar; B3 wurde vor V3 wegen schwacher B1-Calibration ausgelassen.
Das separate Entwicklungsprojekt `../StructureResearch` enthält Forschungsbefehle
und Kontakt-Gate-Tests. Die historischen Rankingsiegel bleiben unverändert;
der neue lesende Frame-Observer verändert den Capture-Dateihash, sodass ihr alter
Holdout-Befehl unter diesem Arbeitsstand die erneute Auswertung verweigert.

Der nachfolgende [klassische CV-Research-Batch V4](Reports/ClassicCvResearchV4/README.md)
vergleicht geometrisches Directional Chamfer, Census ohne/mit lokaler Track-ECC
und eine approximative Low-Rank/Sparse-Projektion. C3 liefert auf wiederverwendetem
V3 78,4 % physische Recall bei 64,4 % Precision und zwölf komplementäre farbige
Treffer; Oracle 115 statt 103/156. Die getestete A+C3-Regel erreicht nur 96/156 und
wird verworfen. C3 bleibt ein Forschungskandidat, keine produktive Fusion.
Alle Vergleichssätze sind ausdrücklich wiederverwendet. A und historische Freezes
bleiben gegenüber diesem Auftragsbeginn unverändert.

Der begrenzte [C3-Abschlussversuch V5](Reports/C3RefinementV5/README.md) testet nur
Feld-Floors, lokalen Sparse-Farbreadout und zwei feste Fusionen. Feld-Floors helfen
moderat; lokaler Readout verbessert Schwarz, erzeugt auf Calibration aber zusätzliche
Phantome und bleibt nicht ausgewählt. F2 erreicht auf wiederverwendetem V3 104/156
bei 85,2 % Ausgabe-Accuracy und ist nicht zur automatischen Übernahme geeignet.
Separat behebt eine auf 1e−12 begrenzte Maskenanteil-Rundungshärtung den A-Ausfall:
A erreicht nun 113 statt 103/156 Rohwerte. Masken, Scoringformeln und Confidence
bleiben gleich; alte Reports und Quell-Freeze-Dateien werden nicht umgeschrieben.

## Ausführen

Vom Solution-Root in PowerShell:

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- benchmark tools/Games/ArkNova/PhotoRecognition/Assets artifacts/ark-nova-benchmark.json
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- analyze tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/IMG_4257.JPG artifacts/ark-nova-photo.json
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- confidence tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2 calibration artifacts/confidence-v2-calibration.json
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- confidence tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2 holdout artifacts/confidence-v2-holdout.json
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- diagnose-black tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2 tools/Games/ArkNova/PhotoRecognition/Reports/BlackConservationDiagnosis
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- ranking tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2 calibration artifacts/ranking-calibration.json
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- ranking tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2 holdout artifacts/ranking-holdout.json
```

`benchmark` prüft die unveränderte **Baseline A** auf allen sechs alten Fotos und zusätzlich ein leeres
Bild, eine halbierte Referenztafel sowie die leere Referenztafel. Exitcode 0 verlangt
genau 72 ausgewertete Werte, mindestens 60 Treffer, keine falschen High-Werte in den
drei bekannten Rot-Artenschutz-Konflikten, abgewiesene schlechte/unvollständige
Registrierung und keine High-Kandidaten auf der leeren Referenztafel.
`analyze` liefert bei gescheiterter Registrierung Exitcode 1 und eine leere Farbmenge;
Exitcode 0 bestätigt nur die Registrierung, nicht die Richtigkeit der Kandidaten.
`analyze` verwendet die ausgewählte abgesicherte Confidence-Variante E.
Zuvor schließt die ausgewählte Ranking-Variante A Nullfelder aus, prüft Mindest-
Evidenz und liefert bei unzureichender Unterstützung null. Manuelle Score-0-Eingaben
werden dadurch nicht verändert. Weder B noch C wurden für `analyze` aktiviert.

`ranking` liefert Rohgenauigkeit, Rang 2, Null-/Phantomzählungen, Abstände,
Farbe-/Track-Auswertungen und die unveränderte Confidence als Sekundärwert.
Calibration vergleicht acht feste Flag-Kombinationen; Holdout lädt vor Bildzugriff
die versiegelte Auswahl. Beide Aufrufe prüfen die Eingabebild-Hashes. Ein erneuter
Holdout-Aufruf ist ausschließlich Reproduktion derselben Regeln, keine neue Validierung.
`ranking-debug <assets> <dataset> <output>` exportiert nur IMG_4265–4267;
IMG_4268 wurde im einzigen tatsächlichen Holdout-Lauf mit exportiert.

`confidence` prüft Dataset-Hashes und den unveränderlichen 17/8-Split. Calibration
vergleicht die fünf benannten Varianten, Holdout ausschließlich A und die zuvor
eingefrorene Auswahl. Ein fehlender oder veränderter Policy-/Split-Freeze verhindert
Holdout-Auswertung. Die mitgelieferten Berichte sind die ursprüngliche Auswertung;
erneute Aufrufe dienen der Reproduktion, nicht weiterer Holdout-Kalibrierung.
Exitcode 0 bedeutet: Sicherheitsgate der gewählten Variante bestanden (kein falsches
High, kein Phantom-High, exakte Entscheidungszahl und Negativtests bestanden).
Exitcode 1 bedeutet: Sicherheitsgate verletzt. Coverage-/Accuracy-Ziele stehen als
separate boolesche Ergebnisse im Bericht; Exitcode 0 bestätigt keine Produktionsreife.
Die Accuracy zählt akzeptierte Phantomfarben als Fehler. Alle Details zu Rohkandidat,
Enthaltung, Sichtbarkeit und beiden Nachbarn bleiben in den Confidence-Berichten.
Aufruf ohne gültige Argumente: Exitcode 2.

`diagnose-black` beobachtet die unveränderten Zwischenmatrizen der vier ausdrücklich
untersuchten Fotos. Alle Bildoperationen des Exports schreiben auf Kopien. Er exportiert
PNG-Vergleiche, kontinuierliche Float-TIFF-Differenzdaten und JSON-/Markdown-Messwerte;
die Diagnose verändert keine Erkennungsregel. Die manuellen Innenproben in
`manual-stone-samples.json` sind ausschließlich Diagnoseannotationen und beeinflussen
keine Feldentscheidung. Diagnosezeiten enthalten Dateiausgabe und sind keine Benchmarks.

Ergebnisse sind technische Diagnostik. Der Analyzer kennt weder Spielernamen noch
Partie-IDs und persistiert keine Wertung. Ein fehlender Kandidat ist `null`, kein
scheinbar erkannter Nullwert. Alle Schwellen sind experimentell.

## Aufbau und Herkunft

- `Assets`: unverändert übernommenes, vom Benutzer bereitgestelltes Masterbild,
  Polygongeometrie v2, Ground Truth und sechs Fotos. SHA-256-Werte stehen in
  `Assets/manifest.json`. Zusätzlich enthält `Assets/ConfidenceV2` die 25 unveränderten
  2000-px-Bilder, Ground Truth, Manifest, Datensatznotizen und den vor Kalibrierung
  fixierten Split. Insgesamt rund 46 MB, ausschließlich Entwicklungsdaten;
  die App verpackt diese Dateien nicht.
- `ArkNovaPhotoAnalyzer`: EXIF-orientiertes Decode, SIFT auf maximal 1600 px langer
  Kante, Ratio-Test, RANSAC, Qualitäts-/Sichtbarkeitsprüfung, Warp auf halbe
  Masterauflösung, Analyse ausschließlich des Scoring-Bereichs, physische HSV-Masken
  kombiniert mit lokal helligkeitsnormalisierter LAB-Referenzdifferenz,
  Occupancy für Feld und erodierten Kern, konservative Nachbarfeldprüfung.
- `Application/.../Scoring/ArkNovaFieldOccupancy`: bildbibliotheksfreie
  Score-/Confidence-Formeln und Konfliktprädikat. Deren Unit Tests laufen in der
  vorhandenen Application-Testsuite.
- `ArkNovaConfidencePolicy`: reine Confidence-Ablationen und gewählte Absicherung;
  ihre Tests werden ohne CV-Abhängigkeit in der Application-Testsuite kompiliert.
- `confidence-policy-selection.json`: vor Holdout versiegelte Auswahl einschließlich
  Quellcode-/Split-Hashes und Calibration-Ergebnis. `Reports` enthält Ausgangsdiagnose,
  Calibration-Ablationen und unveränderte erste Holdout-Auswertung.
- Referenzfeatures werden einmal pro Analyzer-Aufbau berechnet und im Speicher
  wiederverwendet. Es existiert noch kein serialisiertes Descriptor-Asset.
- `benchmark-results.json`: gemessener Release-Lauf vom 30.10.2026 inklusive aller
  Vorhersagen, Fehler und Stufenzeiten. Neue Ergebnisse gehören unter `artifacts/`.

Die ursprünglichen Python-Skripte und vollständigen Segmentierungsparameter waren
nicht im Paket enthalten. Dies ist eine rekonstruierte Pipeline und keine exakte
Portierung. Gleiche Gesamtgenauigkeit bedeutet nicht dieselben Einzelfehler.
Es werden weder OCR noch Ground-Truth-Werte zur Erkennung verwendet.

## Baseline auf den sechs ursprünglichen Fotos

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

Der weitere Integrationsweg ist im [CV-Status](../../../../docs/ARK_NOVA_CV_STATUS.md)
und im [nativen Build-Spike](../NativeVisionSpike/README.md) festgelegt. Keine
weitere Forschungs-/Threshold-Runde und kein neues Holdout als Voraussetzung
für manuell bestätigte Vorschläge. Zunächst muss der technische Backend-Spike
seine Paritätsgates erfüllen. Android ARM64 baut; die numerische Parität ist noch
offen, iOS vorerst nur dokumentarisch geprüft. Kamera, Galerie-Systempicker,
Review, Confidence-Anzeige und Bestätigung vor Persistenz sind danach umzusetzen.

OpenCvSharp bleibt Windows-Forschungs-/Referenzcode. Der gemeinsame native
OpenCV-C++-Kern soll den mobilen Produktpfad übernehmen; reine Ranking-/Confidence-
Logik bleibt C#. Es gibt derzeit noch keine funktionsfähige mobile Fotoanalyse.