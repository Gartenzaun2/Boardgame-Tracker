# A/B/C-Experiment: rohe Feldrangfolge

Übernommen wird **nur A: kein CV-Kandidat 0 mit Mindest-Evidenz/Enthaltung**.
Calibration verbessert sich von **93 auf 94/123**, Holdout von **42 auf 45/60**
richtige ausgegebene Rohwerte. Schwarz-Artenschutz steigt von **6 auf 8/11** bzw.
**2 auf 5/5**. B wird verworfen; C liefert keinen zusätzlichen Nutzen gegenüber A.
Die Schwarzmaske im ausgewählten Verfahren bleibt deshalb unverändert.

Die Verbesserung ist gezielt und begrenzt: Der echte Stein in IMG_4265/4266 wird
noch nicht korrekt als Top-1 erkannt. Diese Bilder bleiben abstinent. Es wurde
keine perfekte Erkennung und keine neue automatische Scoring-Freigabe erreicht.

## Arbeitsbasis, Split und Auswahl

- Sauberer `master`/`origin/master` bei `deb028f`, Version 0.10.0.10.
- Unveränderte 2000-px-Fotos, Ground Truth und Polygone, insbesondere 20/21.
- Unveränderter Split: 17 Calibration-Fotos, 8 Holdout-Fotos.
- Alle Varianten eines Fotos teilen dieselbe Registrierung und dieselben
  Feldmasken. Die rekonstruierte Baseline wird für jede Entscheidung gegen den
  tatsächlichen Analyzer auf identischen Kandidaten und Score geprüft.
- Zwei fachlich motivierte B-Modelle, keine Grid-Suche. Alle Parameterentscheidungen
  und die Auswahl A erfolgten ausschließlich anhand von Calibration.
- [ranking-selection.json](../../ranking-selection.json) wurde vor Holdout angelegt.
  Es enthält Quellcode-/Eingabe-Hashes und das Calibration-Ergebnis. Vor Bildzugriff
  prüft der Holdout-Befehl diese Hashes. Der Holdout wurde in dieser Aufgabe
  **genau einmal** ausgewertet, ausschließlich Baseline gegen ausgewähltes A.
- Nach Holdout wurden weder Regeln noch Schwellen angepasst. Die drei Calibration-
  Diagnosebilder wurden anschließend exportiert; IMG_4268 wurde bereits während
  des einzigen Holdout-Laufs exportiert.

Der V2-Holdout wurde in früheren Aufgaben bereits ausgewertet bzw. diagnostiziert.
Er ist kein neuer unberührter Produktionsdatensatz. Seine aktuellen Ergebnisse
wurden nicht zur Auswahl oder Parameteränderung verwendet.

## A: 0 ausschließen und nicht blind ersetzen

0 bleibt fachlich/manuell gültig. Nur die CV-Kandidatenliste schließt 0 aus;
die Geometriedatei wird nicht umgeschrieben. 0 bleibt als reine Kontroll-/Diagnose-
Region messbar, erscheint aber weder als Kandidat noch als Nachbaralternative.

Die verbleibende Rangfolge bleibt zunächst dieselbe. Anschließend wird der erste
Kandidat zurückgehalten, wenn mindestens eine dieser Bedingungen gilt:

- Score <0,38, FullColor <0,06 oder CoreColor <0,06.
- Der vorherige Gewinner war 0 und der Ersatz hat CoreColor <0,10.
- Die ausgeschlossene Kontrollregion 0 zeigt mindestens 30 % Full- und Core-Farbe
  und mindestens denselben Score wie der Ersatz: `unsupported_zero_occupancy`.

Die ersten Grenzen knüpfen an die vorhandenen Evidenzgrößen an; die stärkere
Kernstützung schützt speziell die durch Ausschluss erzwungene Ersatzentscheidung.
Das ist Kandidaten-Enthaltung, keine Änderung der High-/Medium-Schwellen.
Ein fehlender oder zurückgehaltener Wert ist null, niemals Score 0.

Es gibt im V2-Satz keine physische Score-0-Stellung. Die Nullfeld-Enthaltung wurde
deshalb zusätzlich mit synthetischen Feld-Evidenzen getestet. Eine bildbasierte
Garantie für alle realen 0-Stellungen wurde damit noch nicht nachgewiesen.

## B: relative Schwarz-Evidenz – untersucht und verworfen

**B1:** Ein Pixel muss auf einer ausreichend hellen Referenzfläche liegen
(`reference LAB L >=100`), relativ dunkler sein und Chroma gegenüber der Referenz
verlieren. Seeds: bestehendes `lightDelta<-18`, bestehendes `distance>24`,
Chroma-Abnahme mindestens 8. Wachstum: `lightDelta<-10`, Abstand >24,
Chroma-Abnahme mindestens 4, zwölf vorhandene GrowMask-Schritte. Ein V-Maximum
wird nicht verwendet. Chroma ist `sqrt((a-128)^2+(b-128)^2)` in kodiertem OpenCV-LAB.

Das soll bereits dunklen Boarddruck abwerten und aufgehelltes schwarzes Holz auf
einer chromatischen Referenzfläche erfassen. Die zwei neuen Hauptgrenzen sind
Referenz-L 100 und Seed-Chroma-Abnahme 8; 4 ist die halbierte Wachstumsgrenze.
Diese Grenzen sind experimentell, nicht kalibrierte Materialkonstanten.

B1 fällt auf Calibration auf 79/123 und Schwarz auf 7/33 zurück. Die Masken
interpretieren auch flächige Farbverschiebungen des grünen Boarddrucks als Holz.
Der vollständige erste Bericht liegt in
[initial-chroma-calibration.json](initial-chroma-calibration.json).

**B2:** Dieselben Schwellen, zusätzlich lokale Normalisierung der a/b-Kanäle mit
derselben Gaussian-Sigma 25 wie bei der vorhandenen L-Korrektur:

```text
corrected_a = photo_a - smooth(photo_a) + smooth(reference_a)
corrected_b = photo_b - smooth(photo_b) + smooth(reference_b)
```

Die Chroma-Abnahme wird mit diesen korrigierten Werten berechnet. Diese Variante B
steht in der folgenden vollständigen Achtfach-Ablation. Sie verbessert B1, bleibt
aber mit 87/123 und Schwarz 15/33 unter der Baseline. Schwarz-Artenschutz ist 0/11.
Auf Feld 20 steigt z. B. die Full-Farbe von 2,74 auf 14,17 % in IMG_4265;
andere grüne Felder erhalten jedoch noch größere falsche Evidenz. Es gewinnt dort
41, bei IMG_4266 ebenfalls 41 und bei IMG_4267 Feld 2. Feld 20 fällt auf Rang
10/13/16 zurück. Die bessere lokale Holz-Erfassung allein löst die Spezifität nicht.

B2 erzeugt zusätzlich Phantomfarben und zwei falsch akzeptierte Entscheidungen
unter der unveränderten Confidence-Policy. Deshalb wird es **nicht** übernommen.
Es wurden keine weiteren Schwellen gesucht und keine B-Variante nach Holdout angepasst.
Rot/Gelb/Blau verwenden in B weiterhin exakt ihre bisherigen Masken.

## C: Kern-Ranginformation bei gesättigten Farbtermen

Wenn der ursprüngliche Top-Kandidat beide Farbterme sättigt
(`FullColor>=0,18` und `CoreColor>=0,16`), werden die vollständig gesättigten
Kandidaten innerhalb dieser Stufe zuerst nach tatsächlicher CoreColor, dann nach
Originalscore geordnet. Die übrigen Kandidaten behalten ihre Score-Reihenfolge.
Andernfalls bleibt die gesamte ursprüngliche Reihenfolge erhalten.

Die Occupancy-Gewichte und Originalscores bleiben unverändert. C führt keine neue
Sättigungsschwelle oder trainierte Gewichtung ein. Es bewahrt die bereits gemessene
Kernbelegung, die im bisherigen Score vollständig abgeschnitten wurde.
Die ursprünglichen Scores gehen auch weiterhin in die unveränderte Confidence ein.
Ein durch C negativer Originalscore-Abstand wird dort wie bisher auf 0 begrenzt.

C allein korrigiert auf Calibration einen zusätzlichen blauen Attraktivitätswert.
A korrigiert denselben Wert bereits. A+C liefert keinen zusätzlichen Treffer und
erhöht die Komplexität. Deshalb bleibt C ebenfalls **experimentell deaktiviert**.

## Vollständige Calibration-Ablation

Rohgenauigkeit zählt die **ausgegebenen** Kandidaten für vorhandene Werte; null
zählt als nicht korrekt. Top-1 ist gleich dieser ausgegebenen Treffermenge.
Vor Enthaltung sind bei A 97/123 Rang-1-Kandidaten richtig; ausgegeben werden davon
nur 94. Es werden keine Enthaltungen als Erkennungserfolge gezählt.

| Variante | roh richtig /123 | Schwarz /33 | Schwarz-Artenschutz /11 | korrekter Wert Rang 2 | null, alle 204 Entscheidungen | rohe Phantomwerte |
|---|---:|---:|---:|---:|---:|---:|
| Baseline | 93 | 21 | 6 | 10 | 25 | 58 |
| A | 94 | 21 | 8 | 7 | 77 | 21 |
| B2 | 87 | 15 | 0 | 9 | 21 | 62 |
| C | 94 | 21 | 6 | 9 | 25 | 58 |
| A+B2 | 88 | 15 | 0 | 8 | 56 | 34 |
| A+C | 94 | 21 | 8 | 7 | 77 | 21 |
| B2+C | 93 | 20 | 0 | 5 | 21 | 62 |
| A+B2+C | 93 | 20 | 0 | 5 | 56 | 34 |

Die acht Kombinationen enthalten A+B+C als feste Interaktionskontrolle. Dass B+C
einige B-Fehler kompensiert, rechtfertigt wegen der verbleibenden Regressionen und
Phantome keine Übernahme der Kombination.

| Variante | mittlerer Ranking-Abstand | Median | mittlerer signierter Originalscore-Abstand | Median Originalscore |
|---|---:|---:|---:|---:|
| Baseline | 0,37050 | 0,33035 | 0,37050 | 0,33035 |
| A | 0,37799 | 0,34371 | 0,37799 | 0,34371 |
| B2 | 0,30085 | 0,11732 | 0,30085 | 0,11732 |
| C | 0,43023 | 0,41175 | 0,37181 | 0,33035 |
| A+B2 | 0,30445 | 0,12395 | 0,30445 | 0,12395 |
| A+C | 0,43604 | 0,41375 | 0,37933 | 0,34371 |
| B2+C | 0,37714 | 0,33105 | 0,30235 | 0,11732 |
| A+B2+C | 0,37906 | 0,33867 | 0,30598 | 0,12395 |

Bei C ist der Ranking-Abstand innerhalb der gesättigten Stufe eine **CoreColor-
Differenz**, sonst die Score-Differenz. Er ist daher nicht direkt über Varianten
vergleichbar und keine Confidence-Wahrscheinlichkeit. Die signierten Originalscore-
Abstände stehen zusätzlich explizit im Bericht. Abstände beziehen sich auf vorhandene
Werte, einschließlich zurückgehaltener Rangfolgen.

## Was bewirkt A allein, welche Ersatzfehler entstehen?

Auf Calibration sind sieben vorhandene Werte zunächst fälschlich 0. Ein bloßes
Entfernen von 0 liefert vier richtige Rang-1-Werte, aber drei neue falsche Ersatzwerte:

- IMG_4265 Schwarz-Artenschutz: 2 statt 20.
- IMG_4266 Schwarz-Artenschutz: 1 statt 20.
- IMG_4280 Blau-Attraktivität: 37 statt 48.

Die Mindest-Evidenz hält alle drei zurück. Endgültig entstehen **keine neuen
ausgegebenen falschen Ersatzwerte** aus diesen sieben Nullfeldfehlern: drei werden
korrekt, vier werden null. Der vierte Nullfall ist IMG_4283 Schwarz-Artenschutz:
23 liegt zwar auf Rang 1, erreicht aber nicht die stärkere Ersatz-Kernstützung.

Zusätzlich werden zwei zuvor richtige, sehr schwache Schwarz-Rufwerte
(IMG_4283/4285: 10) null. Sie waren schon vor dieser Aufgabe durch die sichere
Confidence zurückgehalten. Diese Regression bleibt in der Rohgenauigkeit sichtbar.

Auf Holdout verschwinden drei vorhandene Nullfeldfehler vollständig:
IMG_4268 Schwarz-Artenschutz 20, IMG_4287/4288 Schwarz-Artenschutz 24.
Keine neuen Ersatzfehler oder neuen Verluste korrekter Werte entstehen dort.

## Einzelsteinbilder 4265–4268

Ausgewähltes A verändert die Schwarzmaske und den Score des Feldes 20 nicht.
Es verändert die Kandidatenmenge und die Enthaltung.

| Foto | Baseline-Gewinner | A-Ausgabe | Rang 20 vorher → nachher | Score 20 | bestes falsches Feld nach A / Score | Full/Core 20 |
|---|---:|---|---|---:|---|---|
| IMG_4265 | 0 | null | 3 → 2 | 0,25745 | 2 / 0,36585 | 0,02736 / 0,03725 |
| IMG_4266 | 0 | null | 4 → 3 | 0,43677 | 1 / 0,55075 | 0,06483 / 0,07606 |
| IMG_4267 | 0 | 20 | 2 → 1 | 0,53969 | 2 / 0,43077 | 0,08461 / 0,10235 |
| IMG_4268 | 0 | 20 | 2 → 1 | 0,91396 | 2 / 0,25156 | 0,18607 / 0,20469 |

Damit ist die Untererfassung von IMG_4265/4266 nicht behoben. Null ist hier die
korrekte Systemreaktion auf unzureichende Evidenz, aber kein richtiger Rohwert.

## Finale Calibration- und Holdout-Ergebnisse

| Kennzahl | Calibration Baseline → A | Holdout Baseline → A |
|---|---|---|
| richtige Rohwerte | 93/123 → 94/123 | 42/60 → 45/60 |
| rohe Accuracy | 75,6 % → 76,4 % | 70,0 % → 75,0 % |
| Schwarz gesamt | 21/33 → 21/33 | 7/15 → 10/15 |
| Schwarz-Artenschutz | 6/11 → 8/11 | 2/5 → 5/5 |
| korrekter Wert auf Rang 2 | 10 → 7 | 5 → 2 |
| null bei vorhandenen Werten | 2 → 17 | 2 → 7 |
| null insgesamt | 25/204 → 77/204 | 10/96 → 33/96 |
| rohe Phantom-Farb-/Trackwerte | 58 → 21 | 28 → 10 |
| rohe Phantomfarben, eindeutige Foto/Farbe-Paare | 27 → 20 | 12 → 9 |
| Phantomwerte nach unveränderter Confidence | 0 → 0 | 0 → 0 |
| High+Medium Coverage | 49,6 % → 49,6 % | 38,3 % → 38,3 % |
| Accuracy akzeptierter Werte | 100 % → 100 % | 100 % → 100 % |

Die rohe Phantomzählung zählt keine ausgedachten Ground-Truth-Nullen als Score 0.
Sie misst falsche Vorschläge für ausdrücklich abwesende Farben. Die vorhandene
Confidence-Policy bleibt erforderlich; A allein macht nicht alle Phantome abstinent.
Ihre sekundäre Accuracy zählt akzeptierte Phantomwerte ausdrücklich als Fehler.
Artenschutz bleibt unter der unveränderten Policy E Uncertain, auch bei nun richtigem
Rohwert. Es gab keine neuen High-/Medium-Grenzen und keine automatische Übernahme.

### Pro Farbe und Track

| Farbe | Calibration vorher → nachher | Holdout vorher → nachher |
|---|---|---|
| Rot | 28/30 → 28/30 | 12/15 → 12/15 |
| Blau | 14/27 → 15/27 | 9/15 → 9/15 |
| Gelb | 30/33 → 30/33 | 14/15 → 14/15 |
| Schwarz | 21/33 → 21/33 | 7/15 → 10/15 |

| Track | Calibration vorher → nachher | Holdout vorher → nachher |
|---|---|---|
| Attraktivität | 33/41 → 34/41 | 14/20 → 14/20 |
| Artenschutz | 22/41 → 24/41 | 11/20 → 14/20 |
| Ruf | 38/41 → 36/41 | 17/20 → 17/20 |

Der Holdout bestätigt den gerichteten Nutzen für Schwarz-Artenschutz, aber nicht
eine allgemeine Lösung für Schwarz: Attraktivität und schwache Rufbelegung bleiben
problematisch. Der Effekt auf alle Werte beträgt drei zusätzliche Treffer, fünf
Prozentpunkte; es handelt sich weiter um einen kleinen wiederverwendeten Datensatz.

## Verbleibende Fehler und nächste Empfehlung

Auf Holdout bleiben insbesondere Gelb-Artenschutz 20→21, Blau-Artenschutz
(41 oder Enthaltung), Schwarz-Attraktivität (110/113/35 oder Enthaltung), eine
fehlende blaue Rufbelegung und IMG_4275 als schwierige rote Einzelaufnahme.
Weitere falsche Werte liegen teils weit hinten in der Kandidatenliste; das ist
keine reine Confidence-Frage. Auf Calibration bleiben IMG_4265/4266 schwach.

**Nächster Schritt: Schwarz-/Occupancy-Evidenz weiter verbessern.** Die Diagnose
und die gescheiterten B-Modelle sprechen dafür, lokale Stein-/Druckstruktur und
photometrische Veränderungen stärker zu unterscheiden. Eine einfache pixelweise
Chroma-Abnahme reicht nicht. Das rechtfertigt gezielte Feld-/Strukturdiagnostik,
keine größere Threshold-Suche. Wireframe/ECC ist angesichts unveränderter guter
Registrierung und der nachgewiesenen Segmentierungsprobleme nicht die erste Wahl.
Ein unabhängiger Algorithmus B wäre eine spätere Vergleichsoption, aber noch kein
durch diese Ergebnisse notwendiger Architekturwechsel.

Keine Feinregistrierung, ML-, Object-Detection-, Mobile-, Kamera- oder Scoring-
Integration wurde umgesetzt. Manuelle Domainwerte einschließlich 0 bleiben gültig.

## Diagnoseartefakte, Reproduktion und Verifikation

| Foto | Feld 0 vorher/nachher | Feld 20 vorher/nachher | Gewinner/Top-3 | vollständige Feldscores |
|---|---|---|---|---|
| 4265 | [Bild](RankingDiagnostics/IMG_4265/field-0-before-after.png) | [Bild](RankingDiagnostics/IMG_4265/field-20-before-after.png) | [Bild](RankingDiagnostics/IMG_4265/winner-top3.png) | [JSON](RankingDiagnostics/IMG_4265/ranking.json) |
| 4266 | [Bild](RankingDiagnostics/IMG_4266/field-0-before-after.png) | [Bild](RankingDiagnostics/IMG_4266/field-20-before-after.png) | [Bild](RankingDiagnostics/IMG_4266/winner-top3.png) | [JSON](RankingDiagnostics/IMG_4266/ranking.json) |
| 4267 | [Bild](RankingDiagnostics/IMG_4267/field-0-before-after.png) | [Bild](RankingDiagnostics/IMG_4267/field-20-before-after.png) | [Bild](RankingDiagnostics/IMG_4267/winner-top3.png) | [JSON](RankingDiagnostics/IMG_4267/ranking.json) |
| 4268 | [Bild](RankingDiagnostics/IMG_4268/field-0-before-after.png) | [Bild](RankingDiagnostics/IMG_4268/field-20-before-after.png) | [Bild](RankingDiagnostics/IMG_4268/winner-top3.png) | [JSON](RankingDiagnostics/IMG_4268/ranking.json) |

Die vier Bildspalten zeigen Foto, alte Schwarzmaske, **nicht übernommenes B2** und
ausgewählte Maske. Bei ausgewähltem A sind alte und ausgewählte Maske identisch;
ausgeschlossen wird der Kandidat 0, nicht dessen Bildinhalt. `selected=null` zeigt
eine Enthaltung, rote Rang-1-Beschriftung nur den nicht freigegebenen Rangvorschlag.
JSON enthält alle Artenschutzfelder, Vorher-/Nachher-Rangfolge und Top-3 samt Score.

- [Calibration: alle acht Varianten und Einzelentscheidungen](calibration.json)
- [Einziger Holdout-Lauf: Baseline und A](holdout.json)
- [Integrität und zusätzliche Phantomfarben-Zählung](integrity-and-derived-counts.json)

Aufrufe stehen im übergeordneten Tool-README. `ranking` prüft im Holdout alle
eingefrorenen Quellen und Inputs; erneute Aufrufe reproduzieren lediglich denselben
Zustand. `ranking-debug` exportiert nur die drei Calibration-Fotos 4265–4267.
Auswahlzustand und Berichte sollten nicht für neue Holdout-Parameterwahl umgeschrieben werden.

Verifikation: Solution-Build mit 0 Warnungen/0 Fehlern, 75 Core- und 175 Application-
Tests bestanden. Fünf neue Tests prüfen schwache Nullfeld-Ersatzkandidaten,
starke ausgeschlossene Nullbelegung ohne Snapping, Differenz ohne Farbe,
Sättigungs-/Kernrangfolge und unveränderte andere Farben unter B. Manuelle 0 ist
zusätzlich über die unveränderte Core-Berechnung geprüft. Alle 25 Fotos registrieren;
leeres Bild/halbe Tafel werden abgewiesen. Auf leerer Referenz entstehen unter
keiner der acht Varianten starke Werte nach der unveränderten Confidence-Policy.
Geometrie, Ground Truth, Split, Domain- und Confidence-Regeln sind unverändert;
alle vor Holdout eingefrorenen Hashes stimmen weiterhin überein.
