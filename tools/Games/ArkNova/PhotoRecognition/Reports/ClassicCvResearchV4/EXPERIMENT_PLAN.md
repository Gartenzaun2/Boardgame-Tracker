# V4: vor Auswertung festgelegter Versuchsplan

Basis: sauberer master/origin/master `e7d34f44de6ae05e7ea21e92ce4b4aa96dd3accd`.
Neue Werkzeuge ausschließlich unter `tools/Games/ArkNova/ClassicCvResearch`.
Keine Änderungen an A, bisherigen Forschungsquellen oder historischen Freezes.
V2-Calibration ist Auswahl-/Diagnosedatensatz; V2-Holdout und V3 sind ausdrücklich
wiederverwendete Vergleichsdaten, keine neue unabhängige Validierung.

## Feste Methoden und Grenzen

- Gemeinsamer registrierter 2000×1400-Frame aus bestehenden FrameObservern.
  A-Ausnahmen bleiben Fehler mit null für alle zwölf Werte, kein Clamping.
- Geometrie unverändert. Feldpatch mit 24 px Kontext; Entscheidung verlangt
  Kontakt innerhalb des Polygons und des um 12 % erodierten Cores. Score 0
  ausschließlich Diagnose, keine automatische Ausgabe oder Ersatz-Snapping.
- C1: gerichtetes Chamfer auf acht ungerichteten Gradientenorientierungs-Bins.
  Canny 60/150 nach 3×3 Gaussian; Distanztransform je Bin mit Nachbarbin-Toleranz.
  Geometrische Rechteck-/eingekerbte Langmarker-Kontur, keine Fototemplates.
  Größen aus Track-Feldgeometrie: Attraktivität 0,80 Feldbreite × 0,75 Feldhöhe;
  Artenschutz 0,42 Feldbreite × 0,55 Feldhöhe; Ruf Quadrat mit 0,72 Feldhöhe.
  Größenfaktoren 0,85/1/1,15; Winkel −12/0/+12 Grad; Zentren im Feldumfeld ±25 %
  Breite/Höhe, 4-px-Schritte. Mittlerer orientierter Kantenabstand, auf 8 px
  begrenzt; Score exp(−distance/2,5). Ausschluss über Shape-/Polygonkontakt,
  NMS gegen doppelte Markerzentren. Nur ein Parametersatz.
- C2a: 5×5 Census (24 lokale Ordnungsbits), minimale Hamming-Distanz zu zehn
  Empty-Referenzen derselben Position, keine Gradienten-Differenzschwelle.
- C2b: derselbe Census nach Translation-ECC pro Track zur sauberen Referenz.
  30 Iterationen, epsilon 1e−4, maximal 4 px je Achse. Größere/fehlgeschlagene
  Korrekturen verwerfen und globalen Warp behalten. Maskierte Borderräume ohne
  Feldcores dienen ECC; kein GT-/Steinmaskeninput, keine globale Neuregistrierung.
- C3: 16×16 Graupatches, Empty-Mittelwert plus drei SVD-Komponenten;
  acht alternierende Low-Rank-/Soft-Threshold-Schritte. Klassische approximative
  Sparse-Projektion auf ein festes Normal-Unterraummodell, ausdrücklich keine
  exakte Principal Component Pursuit und kein neuronales/ML-Modell.
- C2/C3: zusammenhängende Änderungen mit mindestens 8 Pixeln, mindestens 3 %
  Polygonkontakt und mindestens 1 % Corekontakt. Belegungsscore als Mittel aus
  Polygon-/Coreanteil. Außenpixel tragen nie zum Belegungsscore bei.
- Schwellen aus Empty-LOO: C1 je Track 99,5. Perzentil von Shape-Scores (Floor .65);
  Census-Pixelgrenze 99. Perzentil (Floor 6/24); Sparse-Pixelgrenze 99. Perzentil
  (Floor .04 auf [0,1]); Occupancygrenze 99,5. Perzentil (Floor .06).
  C3 Regularisierung .04, Rang 3 und Iterationszahl werden nicht gesucht.

## Auswahl und wenige Kombinationen

Zunächst C1/C2a/C2b/C3 unabhängig auf alter Calibration messen. Primär physische
TP/FP/FN über eindeutige belegte Felder; Null/Registrierungsfehler bleiben im Nenner.
Keine V3-Metrik zur Auswahl. Nach diesen Einzelmetriken höchstens:

- K1 Shape→A-Farbe, falls C1 Recall und Precision jeweils mindestens .25.
  Zusätzlich farbiger Readout aller Verfahren rein diagnostisch: stärkste
  vorhandene A-CoreColor im gefundenen Feld, mindestens .06 und Dominanz ≥1,5.
  Farbe bestimmt niemals die vorherige Occupancy. Pro Farbe/Track höchster
  Detektorscore, keine GT-Anzahl, keine gewichtete Fusion.
- K2 ECC→Shape nur wenn C2b physische F1 verbessert und die mittlere maskierte
  ECC-Korrelation um mindestens .01 steigt; sonst auslassen.
- K3 RPCA→Shape nur wenn C3 Recall ≥.25 und C1 Precision ≥.30. Feldmengen-Schnitt,
  keine Gewichtung. C3 erzeugt Kandidaten, C1 bestätigt.
- K4 A+X nur wenn mindestens zwei echte farbige Calibration-Treffer A-falsch/X-richtig
  und physische Recall ≥.15 vorliegen. Wahl nach komplementären Treffern, dann F1.
  A sicher (unveränderte High/Medium)→A; unsicher und X sehr sicher→X;
  Konflikt beider sicher→null; sonst null. Sehr sicher: doppelte Occupancygrenze
  (C1 zusätzlich ShapeScore ≥Grenze+.08), CoreColor ≥.15 und Farbdominanz ≥2.
  Oracle separat für farbigen Readout sowie farbunabhängige Kontaktmenge ausgeben.

Generalized Hough ist optional: nur bei hinreichender C1-Trennbarkeit und
vertretbarem zusätzlichem Nutzen; kein zweiter Shape-Ausbau bei klarer Schwäche.
Nach Auswahl Quellen, Parameter, Ergebnisse und alle Inputs hashen. Dann alte
Vergleichsdaten auswerten, ohne Parameteränderung. Diagnosen aus gespeicherten
Karten/Frames; keine neuen Labels für nicht dokumentierte Near-Objects erfinden.
