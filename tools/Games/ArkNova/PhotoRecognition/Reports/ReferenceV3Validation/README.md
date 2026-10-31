# ReferenceV3Validation – 31.10.2026

**FAIL: alleiniger Referenz-/Polygonwechsel insgesamt schlechter. Bisherigen
Produktions-/Referenzstand beibehalten. Keine Nachoptimierung.**
Calibration gewinnt zwei Rohwerte, V2 verliert einen, V3 zwei, die sechs älteren
Fotos einen. Besonders kritisch: V3 hat 11 statt 8 falsche High-Vorschläge;
Schwarz auf V2 fällt roh von 10 auf 8/15 und nach E von 7 auf 4/15.
Die Registrierung ist unverändert erfolgreich auf allen 54 verfügbaren Fotos.
Ursächlich nachgewiesen ist nur der gemeinsame Assetwechsel; Bild und Polygone
wurden nicht getrennt abliert, Einzelfehler nicht durch Forschung erklärt.

## Umfang und Integrität

Gepaarter Lauf mit gehärtetem A_NoZero und unveränderter Confidence E. Alle 456
historischen Calibration-/V2-/V3-Entscheidungen der alten Referenz stimmen in
Rohwert, E-Wert und E-Confidence mit V5 überein (einschließlich Abwesenheiten).
Historische Reports, Auswahl-/Freeze-Dateien und Assets bleiben unverändert.
Kein neuer unabhängiger Holdout; wiederverwendete Vergleichsdaten.

Alle sieben SHA-256-Werte aus dem Handoff-Manifest stimmen. Neue Referenz:
5712 × 4284; alte Referenz: 4000 × 2800. Gelieferte Geometrie unverändert:
114 Attraktivitätsfelder 0–113 mit vier Ecken, 41 Artenschutzfelder 1–41 mit fünf
Ecken, Ruf 1 mit sechs und 2–15 mit acht Ecken. Artenschutz 0 fehlt absichtlich;
Ruf 15 bleibt in der gelieferten Erweiterung. Die Candidate-5-Datei bezeichnet
sich intern weiterhin als `3.0-candidate4` / `v3_candidate4`; keine Umdeutung oder
Korrektur. Markierungen, Overlay und Zooms wurden nicht zur Optimierung genutzt.

Der Analyzer kann ausschließlich für diesen expliziten Vergleich eine andere
Geometriedatei laden; der Bildname kommt aus deren `reference.image`.
Der Standard bleibt `scoring_regions_v2.json` mit alter Referenz. SIFT-Features
werden pro Analyzer neu berechnet, kein serialisierter Cache vorhanden.
Homographie, Warp, SIFT, Matching, Masken, Scoring, Ranking, Farben, Occupancy,
Analysemaßstab und Confidence wurden nicht geändert. Neue Assets bleiben unter
ignoriertem `artifacts/ReferenceV3Validation/handoff`, nicht im aktiven Assetsatz.

Suche in eigenem CV-Quellcode: keine fest verdrahteten 4000-/2800-Größen.
Alte Dateinamen bleiben bewusst in den Standard-/Negativ-/Benchmarkpfaden,
`ArkNovaNativeReference`, historischen Ranking-Freezes und den eingefrorenen
StructureResearch-/ClassicCvResearch-/C3Refinement-Werkzeugen. Wegen FAIL werden
diese Pfade und native Referenzartefakte nicht umgestellt oder neu generiert.

`IMG_4263.jpeg` ist im Repository nicht verfügbar. Daher keine neue Stressmessung;
historische 5/6 nach E sind kein Ergebnis der neuen Referenz. Vorhandene V3-Fälle
einschließlich Fall3Senkrecht, Mehrfachbelegung und Empty-Fotos wurden vollständig
ausgewertet, zusätzlich alle sechs alten Benchmarkfotos (72 Werte).

## Verifikation und Reproduktion

Solution-Build erfolgreich, 0 Warnungen/Fehler. 73 Core- und 198 Application-Tests
bestanden, einschließlich bestehender Ark-Nova-Scoring-, Ranking-, Confidence-
und MaskFraction-Regressionen. Bestehende Negativprüfungen bei beiden Referenzen
bestanden: Blank abgewiesen, halbe Tafel abgewiesen, leere Referenz ohne High.
Keine UI-/Geräteprüfung. Ein früher Start scheiterte am Registry-/MSBuild-Zugriff
der Sandbox, ein weiterer vor Bildanalyse am Einlesen numerischer V3-Fallnummern.
Danach genau ein vollständiger gepaarter Fotolauf. Separate Negativprüfung:
erster Buildversuch durch laufenden Release-Apphost gesperrt, anschließend Debug
erfolgreich. Keine Bilder aufgrund dieser Start-/Buildfehler erneut ausgewertet.

ZIP nach `artifacts/ReferenceV3Validation/handoff` entpacken; die bestehenden
V3-Daten müssen unter `artifacts/ResearchV3/dataset_v3` vorliegen. Ab Solution-Root:

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- validate-reference-v3 . artifacts/ReferenceV3Validation/handoff artifacts/ReferenceV3Validation/new-replay.json
```

Der Befehl verweigert bestehende Ausgabedateien. Der separate Befehl
`validate-reference-v3-negatives` führt nur die Negativprüfungen aus.
Der gespeicherte Fotolauf [results.json](results.json) entstand vor Ergänzung der
Negativprüfungsoption; deren Ergebnisse stehen separat in [negatives.json](negatives.json).
[verification.json](verification.json) belegt historische Parität und GT-/Splithashes;
[handoff-manifest.json](handoff-manifest.json) identifiziert die gelieferten Assets.

## Ergebnisse

Alle Angaben alt → neu. Tripel = richtig / falsch / null. Top1 enthält sämtliche
vorhandenen GT-Werte im Nenner, auch null/Abstention und Registrierungsfehler.
Abwesende Farben und Empty-Fotos werden separat gezählt.

| Satz | Registrierung | n | Roh-A richtig/falsch/null | Top1 roh | Nach E richtig/falsch/null |
|---|---|---:|---|---|---|
| calibration | 17/17 → 17/17 | 123 | 94/12/17 → 96/12/15 | 76,4 % → 78,0 % | 87/9/27 → 89/10/24 |
| holdout | 8/8 → 8/8 | 60 | 45/8/7 → 44/5/11 | 75,0 % → 73,3 % | 41/5/14 → 39/3/18 |
| v3 | 13/13 → 13/13 | 156 | 113/30/13 → 111/33/12 | 72,4 % → 71,2 % | 109/28/19 → 108/31/17 |
| legacy6 | 6/6 → 6/6 | 72 | 60/11/1 → 59/12/1 | 83,3 % → 81,9 % | 60/11/1 → 59/12/1 |

### Tracks und Farben

| Satz | Track/Farbe | n | Roh-A richtig/falsch/null | Nach E richtig/falsch/null |
|---|---|---:|---|---|
| calibration | attractiveness | 41 | 34/3/4 → 34/3/4 | 32/1/8 → 32/2/7 |
| calibration | conservation | 41 | 24/8/9 → 26/9/6 | 21/7/13 → 23/8/10 |
| calibration | reputation | 41 | 36/1/4 → 36/0/5 | 34/1/6 → 34/0/7 |
| calibration | red | 30 | 28/2/0 → 28/2/0 | 28/2/0 → 28/2/0 |
| calibration | blue | 27 | 15/3/9 → 17/2/8 | 14/2/11 → 15/2/10 |
| calibration | yellow | 33 | 30/3/0 → 30/3/0 | 30/3/0 → 30/3/0 |
| calibration | black | 33 | 21/4/8 → 21/5/7 | 15/2/16 → 16/3/14 |
| holdout | attractiveness | 20 | 14/4/2 → 15/1/4 | 13/3/4 → 14/1/5 |
| holdout | conservation | 20 | 14/2/4 → 14/2/4 | 12/2/6 → 11/2/7 |
| holdout | reputation | 20 | 17/2/1 → 15/2/3 | 16/0/4 → 14/0/6 |
| holdout | red | 15 | 12/1/2 → 12/1/2 | 12/0/3 → 12/0/3 |
| holdout | blue | 15 | 9/2/4 → 10/1/4 | 8/2/5 → 9/1/5 |
| holdout | yellow | 15 | 14/1/0 → 14/1/0 | 14/1/0 → 14/1/0 |
| holdout | black | 15 | 10/4/1 → 8/2/5 | 7/2/6 → 4/1/10 |
| v3 | attractiveness | 52 | 32/14/6 → 30/16/6 | 31/14/7 → 30/15/7 |
| v3 | conservation | 52 | 41/8/3 → 41/9/2 | 39/7/6 → 39/8/5 |
| v3 | reputation | 52 | 40/8/4 → 40/8/4 | 39/7/6 → 39/8/5 |
| v3 | red | 39 | 33/6/0 → 33/6/0 | 33/6/0 → 33/6/0 |
| v3 | blue | 39 | 29/9/1 → 28/10/1 | 29/9/1 → 28/10/1 |
| v3 | yellow | 39 | 33/6/0 → 33/6/0 | 33/6/0 → 33/6/0 |
| v3 | black | 39 | 18/9/12 → 17/11/11 | 14/7/18 → 14/9/16 |
| legacy6 | attractiveness | 24 | 20/4/0 → 20/4/0 | 20/4/0 → 20/4/0 |
| legacy6 | conservation | 24 | 17/6/1 → 18/5/1 | 17/6/1 → 18/5/1 |
| legacy6 | reputation | 24 | 23/1/0 → 21/3/0 | 23/1/0 → 21/3/0 |
| legacy6 | red | 18 | 14/4/0 → 12/6/0 | 14/4/0 → 12/6/0 |
| legacy6 | blue | 18 | 15/2/1 → 16/1/1 | 15/2/1 → 16/1/1 |
| legacy6 | yellow | 18 | 18/0/0 → 16/2/0 | 18/0/0 → 16/2/0 |
| legacy6 | black | 18 | 13/5/0 → 15/3/0 | 13/5/0 → 15/3/0 |

### Bestehende Confidence E

| Satz | High/Medium/Uncertain | Falsche High | Phantomwerte roh / nach E (separate Abwesenheitsnenner) |
|---|---|---|---|
| calibration | 38/23/62 → 39/21/63 | 0 → 0 | 21/0 → 18/0 (n=81) |
| holdout | 12/11/37 → 15/10/35 | 0 → 0 | 10/0 → 9/0 (n=36) |
| v3 | 48/32/76 → 48/33/75 | 8 → 11 | 0/0 → 0/0 (n=0) |
| legacy6 | 17/12/43 → 16/16/40 | 0 → 0 | 0/0 → 0/0 (n=0) |

V3 Empty: 10/10 → 10/10 registriert, 120 Abwesenheitsentscheidungen; Phantomwerte roh 40→44, nach E 10→15. Keine Vermischung mit dem Top1-Nenner von 156 belegten V3-Werten.

Über Calibration/V2/V3: roh 252→251/339 (74,3→74,0 %), nach E 237→236/339 (69,9→69,6 %). Mit separaten Legacy6: roh 312→310/411.
