# Reference4000Ablation – 31.10.2026

**Entscheidung: alten Stand behalten. STOP; keine Nachoptimierung.**
A ist schlechter als Baseline. B verbessert gegenüber A die Roh-Top1 um 2/411
(Artenschutz gewinnt), erzeugt aber mehr False High und Empty-Phantome. B bleibt
gegenüber Baseline schlechter. A spricht damit eher für einen Einfluss des neuen
Referenzbildes; Candidate5 bringt gegenüber A begrenzten messbaren Nutzen, keine
übernahmefähige Gesamtverbesserung. Keine weitere Ursachenforschung.

Zellen: **richtig/falsch/null roh → nach unveränderter Confidence E**.
Null/Abstention bleibt im historischen Nenner. False High: vorhandene GT + Empty.
Empty Phantom: roh → E bei 120 Abwesenheitsentscheidungen auf zehn Empty-Fotos.

| Variante | Calibration (123) | V2 (60) | V3 (156) | Legacy6 (72) | False High | Empty Phantom |
|---|---|---|---|---|---|---|
| Baseline | 94/12/17 → 87/9/27 | 45/8/7 → 41/5/14 | 113/30/13 → 109/28/19 | 60/11/1 → 60/11/1 | 8 + 0 | 40 → 10 |
| Test A | 93/14/16 → 87/11/25 | 43/9/8 → 38/5/17 | 112/32/12 → 108/29/19 | 60/11/1 → 60/11/1 | 9 + 0 | 41 → 15 |
| Test B | 96/12/15 → 90/11/22 | 43/8/9 → 38/6/16 | 113/31/12 → 109/28/19 | 58/13/1 → 58/13/1 | 10 + 1 | 43 → 16 |

Top1 roh (Calibration / V2 / V3 / Legacy6): Baseline **76,4 / 75,0 / 72,4 / 83,3 %**;
A **75,6 / 71,7 / 71,8 / 83,3 %**; B **78,0 / 71,7 / 72,4 / 80,6 %**.
Gesamt roh: 312/411 → 308/411 → 310/411; nach E: 297/411 → 293/411 → 295/411.

Registrierung für jede Variante: Calibration 17/17, V2 8/8, V3 13/13, Legacy6 6/6,
Empty 10/10; keine Ausfälle. Blank/halbe Tafel abgewiesen, eigene leere Referenz
ohne High: alle drei Varianten bestanden. Abwesende V2-Farben: Phantomwerte roh
31/33/33, nach E jeweils 0 (separat von Empty, kein zusätzlicher False High).

Einmalige Abbildung ausschließlich zwischen den zwei leeren Referenzen:
bestehendes SIFT-/Matching-/RANSAC-Verfahren, 1965 Matches, 1608 Inlier,
p95 2,376 Registrierungs-Pixel. Perspektivischer Warp auf 4000×2800, keine freie
Resize-Verzerrung. Homographie und Outputhashes vor Auswertung eingefroren.
A/B verwenden dieselbe PNG (SHA-256 `614d3aa0182b86e7db94d423ac4bbdca309ddf2ab4b7c7b985c1f7b00a767314`).
A: alte Polygone unverändert. B: exakt dieselbe Homographie auf alle 779 Eckpunkte,
ohne Runden, Clipping oder Nachzeichnen; Metadaten entsprechend transformiert.
Artenschutz 0 fehlt weiterhin, Ruf 15 behält die Erweiterung.

Build: 0 Warnungen/Fehler; 73 Core- und 198 Application-Tests einschließlich
relevanter CV-Regressionen bestanden. Alle 648 Baseline-Wert-/Confidence-
Entscheidungen stimmen mit dem vorherigen Report überein. A-/E-Logik, Thresholds,
historische Assets und Reports unverändert. Keine Übernahme der Testassets.

[Vollständige Einzelmessungen](results.json), [Farb-/Track-/Confidence-Metriken je Datensatz](metrics.json),
[Homographie und Hashes](preparation.json), [Geometrieprüfung](geometry-verification.json),
[Baseline-Parität und Verifikation](verification.json).

| Track/Farbe (alle 411 GT-Werte) | Baseline roh → E | Test A roh → E | Test B roh → E |
|---|---|---|---|
| attractiveness | 100/25/12 → 96/22/19 | 96/28/13 → 94/23/20 | 95/27/15 → 93/25/19 |
| conservation | 96/24/17 → 89/22/26 | 98/24/15 → 89/23/25 | 104/22/11 → 94/21/22 |
| reputation | 116/12/9 → 112/9/16 | 114/14/9 → 110/10/17 | 111/15/11 → 108/12/17 |
| red | 87/13/2 → 87/12/3 | 85/15/2 → 85/14/3 | 84/16/2 → 84/15/3 |
| blue | 68/16/15 → 66/15/18 | 72/14/13 → 69/14/16 | 75/11/13 → 72/11/16 |
| yellow | 95/10/0 → 95/10/0 | 93/12/0 → 93/12/0 | 91/14/0 → 91/14/0 |
| black | 62/22/21 → 49/16/40 | 58/25/22 → 46/16/43 | 60/23/22 → 48/18/39 |
