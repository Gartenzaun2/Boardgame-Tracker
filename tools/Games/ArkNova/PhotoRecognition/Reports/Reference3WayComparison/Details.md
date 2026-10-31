# Messdetails A/B/C

Tripel = richtig/falsch/null; Nenner einschließlich Enthaltungen und technischer Ausfälle. C liefert ausschließlich technische null-Platzhalter, keine gemessenen Confidence-/Registrierungswerte.

| Variante | Satz | n | Roh | Top1 roh | Nach E | High/Medium/Uncertain | Falsche High |
|---|---|---:|---|---|---|---|---:|
| A | calibration | 123 | 94/12/17 | 76,4 % | 87/9/27 | 38/23/62 | 0 |
| A | holdout | 60 | 45/8/7 | 75,0 % | 41/5/14 | 12/11/37 | 0 |
| A | v3 | 156 | 113/30/13 | 72,4 % | 109/28/19 | 48/32/76 | 8 |
| A | legacy6 | 72 | 60/11/1 | 83,3 % | 60/11/1 | 17/12/43 | 0 |
| B | calibration | 123 | 96/11/16 | 78,0 % | 91/10/22 | 41/21/61 | 0 |
| B | holdout | 60 | 45/6/9 | 75,0 % | 39/3/18 | 14/11/35 | 0 |
| B | v3 | 156 | 114/31/11 | 73,1 % | 108/29/19 | 47/33/76 | 10 |
| B | legacy6 | 72 | 59/12/1 | 81,9 % | 59/12/1 | 15/15/42 | 0 |
| C | calibration | 123 | 0/0/123 | 0,0 % | 0/0/123 | nicht gemessen | — |
| C | holdout | 60 | 0/0/60 | 0,0 % | 0/0/60 | nicht gemessen | — |
| C | v3 | 156 | 0/0/156 | 0,0 % | 0/0/156 | nicht gemessen | — |
| C | legacy6 | 72 | 0/0/72 | 0,0 % | 0/0/72 | nicht gemessen | — |

| Track/Farbe, über 411 vorhandene GT-Werte | A roh → E | B roh → E | C roh → E (technische null) |
|---|---|---|---|
| attractiveness | 100/25/12 → 96/22/19 | 99/26/12 → 96/23/18 | 0/0/137 → 0/0/137 |
| conservation | 96/24/17 → 89/22/26 | 102/21/14 → 93/20/24 | 0/0/137 → 0/0/137 |
| reputation | 116/12/9 → 112/9/16 | 113/13/11 → 108/11/18 | 0/0/137 → 0/0/137 |
| red | 87/13/2 → 87/12/3 | 84/16/2 → 84/15/3 | 0/0/102 → 0/0/102 |
| blue | 68/16/15 → 66/15/18 | 74/10/15 → 71/10/18 | 0/0/99 → 0/0/99 |
| yellow | 95/10/0 → 95/10/0 | 94/11/0 → 94/11/0 | 0/0/105 → 0/0/105 |
| black | 62/22/21 → 49/16/40 | 62/23/20 → 48/18/39 | 0/0/105 → 0/0/105 |

## Eingabe- und Analyseauflösung

| Eingabesatz | Native Abmessungen der tatsächlich eingelesenen Dateien (EXIF-normalisiert) | Eingangsskalierung zur Registrierung |
|---|---|---|
| calibration | 1500×2000, 2000×1500 | 0.8 |
| holdout | 1500×2000 | 0.8 |
| legacy6 | 1536×2048, 2048×1536 | 0.78125 |
| v3 | 1500×2000 | 0.8 |
| v3_empty | 1500×2000 | 0.8 |

Jedes Foto mit Einzelauflösung steht in results.json. V2/V3 sind bereits vorbereitete 2000-px-Dateien; V2-Metadaten nennen ursprüngliche Kameraaufnahmen 3024×4032 bzw. 4032×3024, V3-Manifeste führen Originalabmessungen je Datei. Diese Originale wurden hier nicht eingelesen. Legacy6 liegt tatsächlich mit langer Kante 2048 vor.

A/B: Referenz 4000×2800, Referenzregistrierung 1600×1120, Warp/Analyse 2000×1400. C: Referenz 4000×1506, Referenzregistrierung rund 1600×602, vorbereitete Analyse-/Validity-Fläche 2000×753; der eigentliche Kamera-Scoring-Warp wird wegen des ROI-Fehlers nicht erreicht. AnalysisScale bleibt überall 0,5.

Kameraeingänge werden zunächst auf lange Kante 1600 verkleinert (1200×1600 oder 1600×1200), dann perspektivisch in den Boardraum resampelt. Der feste Faktor von referenznormierten Registrierungs- zu Analysekoordinaten ist für A/B/C 2000/1600 = 1,25. Das ist kein globaler Kamerazoom: die tatsächliche lokale Hoch-/Herunterskalierung hängt von Homographie, Bildpunkt und sichtbarer Tafelgröße ab; ein einheitlicher Kamerafaktor ist nicht definiert und wurde nicht gemessen. Der Warp kann die fotografierte Tafel lokal hochskalieren, ohne zusätzliche Bildinformation zu erzeugen.

Beim früheren 5712×4284-Lauf betrug die Analysefläche 2856×2142 und der entsprechende Koordinatenfaktor 2856/1600 = 1,785. Gegenüber 4000×2800 ändern sich Rasterabmessungen um 1,428 horizontal bzw. 1,530 vertikal; das ist nicht pauschal der lokale Feldmaßstab. Die feste 3×3-Dilatation mit maximal 12 Schritten hätte bei größeren Feldern eine relativ kleinere Reichweite. Die Kernerosion skaliert bereits mit 12 % der kleineren Feld-Bounding-Box-Kante (ganzzahlig, mindestens 1 Pixel); sie ist näherungsweise relativ konstant, mit Quantisierung und Geometrieabhängigkeit. Keine Parameter wurden geändert. Der Vergleich kontrolliert den Referenz-/Analysemaßstab, isoliert aber nicht dessen kausalen Anteil von X/Y-Resampling und Geometrieänderung.
