# Diagnose: schwarzer Artenschutzstein, IMG_4265–4268

**Ergebnis: Feld 21 gewinnt in keinem der vier Bilder.** Auf 21 gibt es weder
strenge noch gewachsene Schwarzpixel. Feld 20 schlägt 21 in jeder Ansicht.
Der tatsächliche globale Fehlgewinner ist **Feld 0**. Der physische Standpunkt 20
und die korrekte Position des Polygons 20 werden uneingeschränkt beibehalten.
Die Diagnose liefert keinen Hinweis auf einen physischen Standpunkt 21.

Arbeitsbasis: sauberer `master`/`origin/master`, `8eb351e` (0.10.0.9).
Keine Schwelle, Gewichtung, Confidence-Regel, Ground Truth oder Polygongeometrie
wurde verändert. Der optionale Observer liest die echten Zwischenmatrizen;
Visualisierungen schreiben auf Kopien. [Integritätsnachweis](diagnostic-integrity.json):
alle zwölf Schwarz-Rohentscheidungen (vier Bilder × drei Tracks), deren Scores,
Full-/Core-Farbe und Confidence entsprechen exakt den bestehenden V2-Berichten.
Die geschützten Regeln und Eingabedaten sind gegenüber HEAD unverändert.

IMG_4268 wurde als früheres Holdout-Foto auf ausdrücklichen Diagnoseauftrag erneut
untersucht. Es wurde keine Policy danach ausgewählt oder angepasst. Diese Diagnose
ist kein neuer unabhängiger Validierungslauf und keine Benchmark-Optimierung.

## Welche Evidenz gewinnt tatsächlich?

| Foto | Score 19 | Score 20 | Score 21 | tatsächlicher Gewinner 0 | Score20 − Score21 |
|---|---:|---:|---:|---:|---:|
| IMG_4265 | 0,08466 | 0,25745 | 0,06644 | 0,60162 | +0,19101 |
| IMG_4266 | 0,09067 | 0,43677 | 0,07163 | 0,61024 | +0,36515 |
| IMG_4267 | 0,06987 | 0,53969 | 0,08194 | 0,98114 | +0,45775 |
| IMG_4268 | 0,05611 | 0,91396 | 0,05493 | 0,98003 | +0,85903 |

Die bestehende Rangfolge sucht das Maximum über alle sichtbaren Artenschutzfelder,
nicht nur 19–21. Deshalb wurde Feld 0 als zusätzliche Kontrollregion exportiert.
Die gewählte Confidence-Policy E hält Artenschutz weiterhin als Uncertain zurück;
hier geht es um die rohe Feldentscheidung, nicht um eine automatische Speicherung.

## Vollständige numerische Diagnose

[Messwerttabelle](measurements.md) enthält für **jedes Bild und 0/19/20/21**:
FullColor, CoreColor, FullDifference, CoreDifference, Score, Grown-Anteil,
Feldsichtbarkeit sowie Polygon-/Core-Pixelfläche. Die JSON-Dateien ergänzen den
Relaxed-Anteil und jeden gewichteten Scoreterm. [Gesamtdaten](summary.json).

Koordinaten und Flächen beziehen sich auf das tatsächlich analysierte Bild mit
2000 × 1400 px. Für Master-Koordinaten/-Versätze mit 2 multiplizieren, für Flächen
mit 4. Polygon-/Core-Flächen sind belegte **Rastermaskenpixel**, keine Rechteckflächen.
Für 19/20/21 betragen sie jeweils 4785/3219, 4751/3195 und 4684/3148 Pixel.
Alle vier untersuchten Felder sind in allen Bildern vollständig sichtbar.

Zusätzliche Kontraste zwischen 20 und 21:

| Foto | Δ FullColor | Δ CoreColor | Δ FullDifference | Δ CoreDifference | Δ gewichtete Difference-Evidenz |
|---|---:|---:|---:|---:|---:|
| IMG_4265 | +0,02736 | +0,03725 | +0,20028 | +0,21908 | +0,04194 |
| IMG_4266 | +0,06483 | +0,07606 | +0,17535 | +0,20320 | +0,03785 |
| IMG_4267 | +0,08461 | +0,10235 | +0,11498 | +0,12530 | +0,02403 |
| IMG_4268 | +0,18607 | +0,20469 | +0,26739 | +0,32290 | +0,05903 |

Alle Deltas sind Wert20 minus Wert21. Die letzte Spalte ist
`0.1*(fullDiff20+coreDiff20-fullDiff21-coreDiff21)`.

## Feld 21: keine fälschlich schwarzen Steinpixel

Strict Black ist auf 21 in **allen vier Bildern exakt leer**:
FullColor = CoreColor = Grown = 0. Folglich gibt es dort keine Population
fälschlich als Schwarz erkannter Pixel, für die eine HSV-/LAB-Verteilung angegeben
werden könnte. Diese Verteilungen sind im JSON bewusst `null`, nicht erfunden.

Die Relaxed-Maske enthält auf 21 nur 0 / 15 / 3 / 2 vereinzelte Pixel
(Anteile 0 / 0,003202 / 0,000640 / 0,000427). Sie sind nicht an die strengen Seeds
angebunden; GrowMask übernimmt keinen davon. Sie beeinflussen den Rohscore nicht.

Die kleinen Scores 0,05493–0,08194 stammen **ausschließlich** aus der farbunabhängigen
binären Referenzdifferenz. Die Heatmaps zeigen vor allem die gedruckte Zahl 21,
deren Schildrand, Feldgrenzen und umliegende Drucklinien. Das sind keine Steinpixel.
Die Helligkeits-/Farbabweichung und kleinere Konturversätze erzeugen dort eine
Differenz, aber weder konkurrierende Schwarzbelegung noch eine korrekte Fußhypothese.

## Tatsächlicher Fehlgewinner Feld 0

Die Overlays zeigen falsche Strict-Pixel im dunklen Boarddruck am rechten Tafelrand:
am Übergang zum weißen 0-Schild, im dunkelblauen Hintergrund und teilweise entlang
der rechten Feldseite. Ein schwarzer Stein befindet sich dort nicht.

| Foto | Strict-Pixel 0 / 20 | FullColor 0 / 20 | CoreColor 0 / 20 | FullDiff 0 / 20 |
|---|---:|---:|---:|---:|
| IMG_4265 | 249 / 130 | 0,09257 / 0,02736 | 0,08324 / 0,03725 | 0,92491 / 0,56578 |
| IMG_4266 | 291 / 308 | 0,10818 / 0,06483 | 0,07290 / 0,07606 | 0,88736 / 0,57714 |
| IMG_4267 | 562 / 402 | 0,20892 / 0,08461 | 0,21527 / 0,10235 | 0,89517 / 0,55862 |
| IMG_4268 | 532 / 884 | 0,19777 / 0,18607 | 0,17394 / 0,20469 | 0,89219 / 0,57525 |

Feld 0 ist mit 2690 Maskenpixeln deutlich kleiner als Feld 20 mit 4751. Vergleichbare
falsche Pixelzahlen erzeugen deshalb auf 0 größere Flächenanteile. Die Core-Maske
entfernt die Störung nicht: Ein relevanter Anteil liegt innerhalb des Kerns.
Das ist kein nachgewiesener Fehler der Erosion, sondern fehlende semantische
Unterscheidung zwischen dunklem Boarddruck und dunklem Stein.

Die vorhandene Schwarzregel verlangt `V<85`, korrigiertes `lightDelta<-18` und
Referenzabstand `>24`. **Hue und Saturation werden für Schwarz nicht geprüft.**
Auch dunkler blau gefärbter Boarddruck erfüllt diese Bedingungen.

Medianwerte der tatsächlich als Schwarz gewerteten Pixel auf 0:

| Foto | H / S / V | kodiertes LAB L/a/b | lightDelta | Roh-ΔL | Referenzabstand | relative Position x/y |
|---|---|---|---:|---:|---:|---|
| IMG_4265 | 101 / 40 / 59 | 60 / 129 / 126 | −34 | −57 | 43,93 | 0,65 / 0,45 |
| IMG_4266 | 115 / 40 / 64 | 61 / 129 / 124 | −32 | −44 | 41,28 | 0,74 / 0,51 |
| IMG_4267 | 115 / 29 / 54 | 51 / 129 / 125 | −87 | −124 | 86,28 | 0,40 / 0,38 |
| IMG_4268 | 21 / 74 / 37 | 34 / 128 / 133 | −38 | −74 | 51,28 | 0,72 / 0,45 |

HSV/LAB sind OpenCV-8-Bit-Kodierungen; insbesondere H = 0…179, nicht Grad,
und a/b sind nicht direkt signierte CIELAB-Werte. Position relativ zur Feld-Boundingbox.
Je Feld enthält `field-*-pixels.json` Min, P10, Median, P90, Max, Mittelwerte und
die Koordinaten/Featurewerte **jedes** Strict-Pixels. Die Fotos belegen dunklen
Boarddruck; Beleuchtung und lokale Referenzabweichung wirken zusammen.
Eine sichere pixelweise Zerlegung in Schatten versus Druck versus Reflexion ist
aus diesen Bildern allein nicht möglich und wurde nicht als neue Klassifikation gebaut.

Die korrigierte Referenzdifferenz auf 0 ist großflächig, nicht nur ein schmaler
Kontursaum. Kleine lokale Versätze können Teile davon erklären, aber nicht als
alleinige Ursache für die rund 89–92 % geänderten Feldpixel nachgewiesen werden.

## Echter Stein auf Feld 20: teilweise bis stark untererfasst

Die Overlays bestätigen den Stein auf 20, ohne Projektion in Richtung 21. Sichtbare
Teile des erhöhten Steins liegen oberhalb des Feldpolygons beim blauen Druckstreifen.
Sie gehen entsprechend nicht in dessen Flächenanteil ein. Hinzu kommen helle/
reflektierende Holzflächen, die den absoluten V-Test nicht erfüllen.

Kleine manuell ausgewählte Innenproben des sichtbaren Holzes erlauben eine konkrete
Prüfung. Sie sind orange eingezeichnet und in `manual-stone-samples.json` dokumentiert.
**Keine vollständige Steinkontur, keine neue Ground Truth und keine automatische Klassifikation.**

| Foto | Samplepixel | V Median [Min–Max] | bestehen V<85 | bestehen ΔL-Test | bestehen Differenztest | Strict / Relaxed / Grown |
|---|---:|---|---:|---:|---:|---|
| IMG_4265 | 100 | 102 [81–180] | 5 | 60 | 93 | 5 / 42 / 40 |
| IMG_4266 | 80 | 81 [76–86] | 74 | 62 | 67 | 59 / 64 / 64 |
| IMG_4267 | 80 | 81 [72–87] | 72 | 66 | 69 | 62 / 67 / 67 |
| IMG_4268 | 120 | 67 [61–70] | 120 | 85 | 99 | 85 / 87 / 87 |

Die Tests zählen unabhängig und überlappen. In IMG_4265 verwirft die absolute
Helligkeitsgrenze fast die gesamte manuelle Holzprobe. In den anderen Proben ist
die Erfassung besser, aber die Referenz-/Darkness-Bedingungen verwerfen zusätzliche
Pixel. Für den gesamten realen Stein lässt sich ohne manuelle Vollmaske kein
exakter Recall nennen. Die Masken zeigen jedoch eindeutig eine partielle Erfassung,
am schwächsten in IMG_4265.

GrowMask erweitert Feld 20 von 2,74/6,48/8,46/18,61 % Strict auf
12,90/12,23/13,09/21,30 %. **Diese gewachsene Maske geht nicht in den Occupancy-Score
ein**, sondern nur in die spätere Nachbar-/Confidence-Prüfung. Mehr Grown-Pixel
können deshalb die falsche Rohentscheidung nicht korrigieren.

## Score-Sättigung und Referenzdifferenz

Die bestehende Formel wird unverändert ausgewertet:

```text
0.46*min(fullColor/0.18,1) + 0.34*min(coreColor/0.16,1)
+ 0.10*fullDifference + 0.10*coreDifference
```

In IMG_4267 sättigen beide Farbterme auf Feld 0; auf 20 bleiben sie darunter.
In IMG_4268 sättigen dagegen **beide Felder** vollständig:

| IMG_4268 | FullColor-Term | CoreColor-Term | FullDiff-Term | CoreDiff-Term | Summe |
|---|---:|---:|---:|---:|---:|
| Feld 0 | 0,46000 | 0,34000 | 0,08922 | 0,09082 | 0,98003 |
| Feld 20 | 0,46000 | 0,34000 | 0,05752 | 0,05643 | 0,91396 |

Obwohl Feld 20 den höheren CoreColor-Anteil hat (20,47 % gegenüber 17,39 %), geht
dieser Vorteil durch Clipping verloren. Die **gesamte Score-Differenz 0,06608**
entsteht dann aus den beiden farbunabhängigen Differenztermen. Der Vergleich zeigt
den Entscheidungsmechanismus direkt; es wurde kein alternativer Score ausprobiert.

## Lokale Registrierung

Es wurden ausschließlich die bereits vorhandenen SIFT-/RANSAC-Inlier analysiert.
Kein ECC, keine neue Transformation, keine Verschiebung der Polygone und keine
nachträgliche Ausrichtung. Die Residuen sind `H(photoKeypoint)-referenceKeypoint`.
Pfeile sind nur zur Sichtbarkeit zehnfach verlängert.

| Foto | lokale Inlier bei 19–21 | Median Δx / Δy | Median Betrag | P90 Betrag |
|---|---:|---|---:|---:|
| IMG_4265 | 23 | +0,33 / +1,52 | 1,70 | 1,87 |
| IMG_4266 | 14 | +0,65 / −0,40 | 1,19 | 3,56 |
| IMG_4267 | 6 | −0,23 / +1,93 | 2,03 | 2,31 |
| IMG_4268 | 3 | +1,40 / +0,19 | 1,41 | 1,50 |

Einheit: Analysepixel; Masterpixel jeweils ×2. Die lokalen Druckkonturen zeigen
kleine Versätze in dieser Größenordnung. Diese können die Difference-Bänder an
Zahlen, Schilden und Linien erhöhen. Der Abstand der Zentren 20→21 beträgt dagegen
rund 145 Analysepixel. Keine entsprechende Feldverwechslung durch Registrierung
ist sichtbar; sie würde auch nicht die vollständig leere Strict-Maske auf 21 erklären.

Die separaten Dateien `field-*-local-registration.json` zeigen die Datenlage je Feld:
bei 20 nur 11/4/1/0 Inlier, bei 21 9/9/5/3, bei 0 3/0/0/6. Auf 0 gibt es in zwei
Ansichten überhaupt keine lokale Inlier-Stützung. Die anderen beiden haben mediane
Beträge von 3,37 bzw. 2,42 Analysepixeln. Das sind dünne, gegebenenfalls mehrfach
orientierte Feature-Stichproben. Sie erlauben **keine belastbare flächige lokale
Registrierungsgarantie** und keine präzise Trennung geometrischer von photometrischen
Fehlern. Auch der Steinbereich selbst ist kein planarer Registrierungs-Prüfpunkt.

## Antworten und zwei mögliche nächste Änderungen

1. **Welche Evidenz lässt 21 gewinnen?** Keine: 21 gewinnt nicht. Auf 21 entsteht
   nur schwache Referenzdifferenz. Global gewinnt 0 durch falsche Schwarzbelegung
   plus großflächige Referenzdifferenz; IMG_4268 wird allein durch die Differenzterme entschieden.
2. **Hauptursache?** Kombination aus Schwarzsegmentierung, Beleuchtungs-/Referenzabweichung,
   dunklem Boarddruck und Score-Sättigung. Kleine lokale Registrierungsreste tragen
   zu Druckkontur-Differenzen bei; ihr exakter kausaler Anteil ist nicht isoliert.
3. **Welche falschen Pixel auf 21?** Keine Strict-/Grown-Schwarzpixel. Die falschen
   Schwarzpixel liegen tatsächlich auf 0 und sind in den roten Overlays einzeln sichtbar.
4. **Erfassung auf 20?** Partiell und blickwinkelabhängig, in IMG_4265 besonders schwach.
5. **Gleicher Fehler in vier Bildern?** Gleicher Fehlgewinner 0, aber sehr unterschiedliche
   echte Stein-Evidenz und andere Gewichtung der Ursachen. Der Score20 steigt von
   0,25745 auf 0,91396; auf 21 bleibt die Schwarzmaske durchgehend leer.
6. **Vielversprechendste Änderungen, nur vorgeschlagen:**

   - Referenzgeführte, photometrisch normalisierte Schwarz-Evidenz untersuchen:
     dunklen Boarddruck/Referenzstrukturen von hinzugekommenem Holz unterscheiden und
     aufgehelltes schwarzes Holz anhand relativen Kontrasts erfassen. Eine bloße
     Erhöhung der V-Grenze würde die bereits vorhandenen Rand-Fehlpixel ebenfalls
     vergrößern und ist durch diese Diagnose nicht gerechtfertigt.
   - Die harte Sättigung der Full-/Core-Farbe im Occupancy-Score überprüfen, sodass
     zusätzliche echte Kernbelegung als Ranginformation erhalten bleibt. Das klare
     Beispiel ist IMG_4268: Der vorhandene Kernvorteil auf 20 verschwindet vor dem
     Vergleich und überlässt der allgemeinen Referenzdifferenz die Entscheidung.

Beide Änderungen wurden **nicht implementiert oder als Gegenfaktum benchmarked**.
Es gibt keine Änderung an Ground Truth 20, Polygon 20/21 oder automatischen Werten.

## Diagnosebilder und Reproduktion

Feldfarben: **19 cyan, 20 grün, 21 magenta**. Weiße Pixel in Masken sind die echten
Maskenwerte; farbige Konturen und Zahlen dienen nur der Darstellung. Ausschnitte
sind dreifach mit Nearest-Neighbor vergrößert. Die Heatmap verwendet bildübergreifend
denselben Bereich 0…100 für den kontinuierlichen korrigierten LAB-Abstand; größere
Abstände sättigen nur in der **Darstellung**. Float-TIFF enthält die unveränderten
kontinuierlichen Werte. Der eigentliche Score verwendet die zusätzlich exportierte
binäre Maske `distance>24` und keine normalisierte Heatmap.

| Bild | Warp | Strict | Relaxed | Grown | Heatmap | Referenz/Core 20 | Referenz/Core 21 |
|---|---|---|---|---|---|---|---|
| 4265 | [Foto](IMG_4265/01-registered-19-21.png) | [Maske](IMG_4265/02-strict-black.png) | [Maske](IMG_4265/03-relaxed-black.png) | [Maske](IMG_4265/04-grown-black.png) | [Differenz](IMG_4265/05-reference-distance-heat.png) | [Vergleich](IMG_4265/06-field-20-reference-photo-difference-core.png) | [Vergleich](IMG_4265/06-field-21-reference-photo-difference-core.png) |
| 4266 | [Foto](IMG_4266/01-registered-19-21.png) | [Maske](IMG_4266/02-strict-black.png) | [Maske](IMG_4266/03-relaxed-black.png) | [Maske](IMG_4266/04-grown-black.png) | [Differenz](IMG_4266/05-reference-distance-heat.png) | [Vergleich](IMG_4266/06-field-20-reference-photo-difference-core.png) | [Vergleich](IMG_4266/06-field-21-reference-photo-difference-core.png) |
| 4267 | [Foto](IMG_4267/01-registered-19-21.png) | [Maske](IMG_4267/02-strict-black.png) | [Maske](IMG_4267/03-relaxed-black.png) | [Maske](IMG_4267/04-grown-black.png) | [Differenz](IMG_4267/05-reference-distance-heat.png) | [Vergleich](IMG_4267/06-field-20-reference-photo-difference-core.png) | [Vergleich](IMG_4267/06-field-21-reference-photo-difference-core.png) |
| 4268 | [Foto](IMG_4268/01-registered-19-21.png) | [Maske](IMG_4268/02-strict-black.png) | [Maske](IMG_4268/03-relaxed-black.png) | [Maske](IMG_4268/04-grown-black.png) | [Differenz](IMG_4268/05-reference-distance-heat.png) | [Vergleich](IMG_4268/06-field-20-reference-photo-difference-core.png) | [Vergleich](IMG_4268/06-field-21-reference-photo-difference-core.png) |

Jeder Bildordner enthält außerdem das gesamte registrierte Foto, die unverdeckten
Feature-/Pixelmessdaten, Float-Differenz, binäre Difference-Maske, LightDelta-
Darstellung, rote Strict-Pixel-Overlays, Feld-0-Vergleiche und die orange Innenprobe.

Aufruf vom Solution-Root:

```powershell
dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- diagnose-black tools/Games/ArkNova/PhotoRecognition/Assets tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2 tools/Games/ArkNova/PhotoRecognition/Reports/BlackConservationDiagnosis
```

Für reproduzierbare Innenproben liegt die kleine manuelle Annotationsdatei bereits
im Ausgabeordner. Sie hat keinen Einfluss auf den Analyzer oder dessen Ergebnisse.
Der Export enthält I/O und Bildbeschriftungen; seine Laufzeit ist kein Performancebenchmark.

Verifikation: Solution-Build mit 0 Warnungen/0 Fehlern, 75/75 Core- und 170/170
Application-Tests bestanden. Der Integritätsnachweis vergleicht die zwölf
Schwarz-Rohentscheidungen mit den vorhandenen Berichten; der ursprüngliche
Calibration-/Holdout-Benchmark wurde für diese Diagnose nicht neu optimiert.
