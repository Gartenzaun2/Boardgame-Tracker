# Reference3WayComparison – 31.10.2026

**Finaler Stand:** A bleibt Produktionsbaseline. B brachte keinen Produktgewinn.
C v2 registriert technisch 54/54, bringt nach Confidence E keinen Vorteil und bleibt
dokumentierter Referenzkandidat/Forschungsstand. Keine automatische Übernahme;
Referenz-/Heuristikoptimierung beendet. Nächster Schritt: nativer Mobile-CV-Port
mit Paritätsnachweis und anschließende Produktintegration.

**Ergänzung:** Das gültige C mit Bounds-Fix ist im [Nachtrag unten](#nachtrag-c-v2-bounds-fix) dokumentiert.
Der folgende ursprüngliche Lauf mit 1506 Pixel Höhe bleibt als historischer Nachweis erhalten.

1. **B gegenüber A: gemischt, keine Gesamtverbesserung.** Roh leicht besser
   (314 statt 312/411), nach E gleich viele Treffer (297/411), aber mehr falsche
   High-Vorschläge und Empty-Phantome. Calibration gewinnt, Legacy6 verliert.
2. **C gegenüber A/B: technisch schlechter, keine auswertbare Erkennungsqualität.**
   Alle 54 Eingaben enden mit OpenCV-ROI-Exception; sämtliche GT-Werte zählen als null.
3. **C-Registrierung nicht als stabil nachgewiesen.** Der Fehler tritt vor Rückgabe
   der Registrierungsdiagnostik auf; Homographiequalität ist deshalb nicht messbar.

**Produktionsstandard bleibt unverändert. STOP, keine Optimierung.**
A/B/C sind ausschließlich die drei unveränderten Paare aus dem neuen ZIP;
sie entsprechen nicht den A/B-Bezeichnungen der vorherigen Ablation.

Tripel = richtig/falsch/null. Identische historische Nenner einschließlich null:
Calibration 123, V2 60, V3 156, Legacy6 72. V3 Empty separat: 10 Fotos / 120
Abwesenheitsentscheidungen. Keine unabhängige neue Validierung.

| Variante | Calibration raw | V2 raw | V3 raw | Legacy6 raw | V3 False High | V3 Empty Phantom raw/E |
|---|---:|---:|---:|---:|---:|---:|
| A_current | 94/12/17 | 45/8/7 | 113/30/13 | 60/11/1 | 8 | 40/10 |
| B_new_scaled_4000x2800 | 96/11/16 | 45/6/9 | 114/31/11 | 59/12/1 | 10 | 41/15 |
| C_rectified_scoring_4000 | 0/0/123* | 0/0/60* | 0/0/156* | 0/0/72* | n/a* | n/a* |

Nach E (Calibration / V2 / V3 / Legacy6):
A **87/9/27 · 41/5/14 · 109/28/19 · 60/11/1**;
B **91/10/22 · 39/3/18 · 108/29/19 · 59/12/1**;
C unverändert ausschließlich technische null-Werte.

Registrierung über dieselben 54 Fotos, Spannen min–max:

| Variante | Erfolgreich | Matches | Inliers | p95 (Registrierungs-px) | Visibility | Spread |
|---|---:|---:|---:|---:|---:|---:|
| A | 54/54 | 415–1929 | 288–1579 | 1,787–2,639 | 0,930–1,000 | 0,735–2,273 |
| B | 54/54 | 506–1777 | 338–1423 | 1,485–2,754 | 0,954–1,000 | 0,870–2,247 |
| C | 0 abgeschlossen / 54 technische Fehler | n/a | n/a | n/a | n/a | n/a |

*C: Ruf-Felder 9–15 ergeben gerundete Analyse-Bounding-Boxes bis y=753–756 bei
753 Pixel Bildhöhe (gültig 0–752). Der unveränderte Analyzer greift außerhalb der
Validity-Matrix zu. Kein Clipping, Padding, Polygon- oder Threshold-Fix. Die
Nullwerte zählen im Nenner, bedeuten keine reguläre Enthaltung. Die im JSON für
technische Fehler eingesetzten Diagnose-Nullen/„Uncertain“-Gruppierungen sind
Platzhalter und keine gemessenen Registrierungs-/Confidencewerte. Auch C-Negativ-
Checks konnten wegen derselben Exception nicht vollständig abgeschlossen werden;
A/B bestehen Blank-, Halbtafel- und eigene Empty-Referenzprüfung jeweils.

Alle zehn Manifestprüfsummen stimmen; A stimmt zusätzlich mit den aktiven Assets
überein. Seine 648 Wert-/Confidence-Entscheidungen reproduzieren den vorherigen
Report ohne Abweichung. Erster Lauf brach bei C vor gemeinsamem Speichern ab;
danach Wiederholung mit reinem Fehler-/Zwischenergebnis-Logging, ohne CV-Änderung.
Build erfolgreich, 0 Warnungen/Fehler; 73 Core- und 198 Application-Tests bestanden.
RegistrationEdge=1600, AnalysisScale=0,5, gehärtetes A_NoZero und E unverändert.

[Farb-/Track-/Confidence-Tabellen, Top1 und Auflösungs-/Morphologiehinweise](Details.md),
[vollständige Einzelmessungen und Eingabeauflösung je Foto](results.json),
[alle Metriken je Datensatz](metrics.json), [Manifest](handoff-manifest.json),
[Verifikation](verification.json). Historische Reports und Assets bleiben erhalten.

Reproduktion ab Solution-Root nach Entpacken des ZIP unter dem angegebenen Pfad:
`dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- compare-reference-3way . artifacts/Reference3WayComparison/handoff artifacts/Reference3WayComparison/new-comparison.json`

## Nachtrag C v2 Bounds-Fix

**Nur C neu berechnet; A/B unverändert aus dem obigen gespeicherten Report übernommen.**
Alle Eingabefoto-Hashes und die vollständigen A-/B-Ergebnisobjekte stimmen überein.
Verwendet wurden ausschließlich `board_reference_rectified_scoringonly_4000_v2.png`
und `scoring_regions_v3_rectified_candidate5_scoringonly_v2.json` aus dem Bounds-Fix-ZIP.
Manifestprüfsummen bestanden. Vor Fotoauswertung explizit geprüft: tatsächliche
Referenz **4000×1544**, tatsächliche Analyzer-Fläche nach Skalierung **2000×772**.
Tracks einschließlich aller Polygonkoordinaten und das Quellviereck sind identisch
zum vorherigen C. Max. Polygon-y=1512; 31 nutzbare Pixel darunter bis Index 1543.

Tripel = richtig/falsch/null, gleiche Nenner einschließlich Enthaltungen:

| Variante | Calibration raw | V2 raw | V3 raw | Legacy6 raw | V3 False High | V3 Empty Phantom raw/E |
|---|---:|---:|---:|---:|---:|---:|
| A (übernommen) | 94/12/17 | 45/8/7 | 113/30/13 | 60/11/1 | 8 | 40/10 |
| B (übernommen) | 96/11/16 | 45/6/9 | 114/31/11 | 59/12/1 | 10 | 41/15 |
| C v2 (neu) | 98/9/16 | 43/8/9 | 115/28/13 | 60/11/1 | 10 | 44/15 |

Nach E (Calibration / V2 / V3 / Legacy6):
A **87/9/27 · 41/5/14 · 109/28/19 · 60/11/1**;
B **91/10/22 · 39/3/18 · 108/29/19 · 59/12/1**;
C **89/8/26 · 38/5/17 · 110/26/20 · 60/11/1**.
Gesamt roh A/B/C: **312/314/316 von 411**, nach E jeweils **297/411**.
Keine zusätzlichen High-Phantome bei abwesenden Farben oder Empty (alle drei 0).

Registrierung C v2: Calibration 17/17, V2 8/8, V3 13/13, Legacy6 6/6, Empty 10/10:
**54/54 erfolgreich, 0 technische Fehler.** Bestehende Blank-, Halbtafel- und eigene
Empty-Referenz-Negativprüfungen bestanden. Registrierungsvergleich min–max:

| Variante | Matches | Inliers | p95 (Registrierungs-px) | Visibility | Spread |
|---|---:|---:|---:|---:|---:|
| A (übernommen) | 415–1929 | 288–1579 | 1,787–2,639 | 0,930–1,000 | 0,735–2,273 |
| B (übernommen) | 506–1777 | 338–1423 | 1,485–2,754 | 0,954–1,000 | 0,870–2,247 |
| C v2 (neu) | 250–1731 | 148–1459 | 1,273–2,707 | 0,952–1,000 | 0,593–1,336 |

C registriert auf diesen vorhandenen Daten stabil. Das Qualitätsbild bleibt gemischt:
roh mehr Treffer als A/B, nach E insgesamt gleich viele, V2 schwächer und gegenüber A
mehr falsche V3-High-Vorschläge/Empty-Phantome. Keine automatische Übernahme.
A_NoZero, E, RegistrationEdge=1600, AnalysisScale=0,5, Thresholds, Farben,
Occupancy, Ranking und Homographie unverändert. Keine Optimierung. **STOP.**

Build: 0 Warnungen/Fehler; 73 Core- und 198 Application-Tests bestanden.
[Track-/Farb-/Confidence-Tabellen und Top1](C_BoundsFixDetails.md),
[neues C plus unverändert übernommenes A/B](c-boundsfix-results.json),
[Metriken je Datensatz](c-boundsfix-metrics.json), [Höhen-/Geometrieprüfung](c-boundsfix-precheck.json),
[Manifest](c-boundsfix-manifest.json), [Verifikation der A/B-Wiederverwendung](c-boundsfix-verification.json).
Alte `results.json`, `metrics.json` und `Details.md` bleiben unverändert.

C-only-Reproduktion mit neuer Ausgabedatei:
`dotnet run --project tools/Games/ArkNova/PhotoRecognition -c Release -- compare-reference-c-boundsfix . artifacts/Reference3WayComparison/C_boundsfix tools/Games/ArkNova/PhotoRecognition/Reports/Reference3WayComparison/results.json artifacts/Reference3WayComparison/new-c-boundsfix.json`
