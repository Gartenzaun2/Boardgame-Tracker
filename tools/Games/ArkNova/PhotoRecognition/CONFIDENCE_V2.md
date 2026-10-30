# Confidence V2 – Ergebnis und Grenzen

Arbeitsbasis: sauberer `master`/`origin/master` bei `9c77226`.
Die Bildpipeline, Farbmasken, Registrierung und Feldrangfolge wurden nicht
kalibriert. Änderungen betreffen Confidence, Enthaltung und Diagnostik.
Keine Object Detection, kein ML, keine OCR und keine App-/Mobile-Anbindung.

## Datensatz und Trennung

Die 25 verkleinerten Bilder aus dem bereitgestellten ZIP wurden unverändert
übernommen; alle SHA-256-Werte des Manifests wurden geprüft. Originalbilder wurden
nicht gesucht oder verwendet. `Assets/ConfidenceV2/split.json` wurde vor der ersten
Schwellenänderung angelegt und nicht geändert:

- Calibration: 4265–4267, 4269–4271, 4273–4274, 4276, 4278–4285.
- Holdout: 4268, 4272, 4275, 4277, 4286–4289.

Calibration enthält 204 Entscheidungen: 123 vorhandene Werte und 81 explizit
abwesende Farb-/Track-Kombinationen. Holdout enthält 96 Entscheidungen: 60 Werte
und 36 Abwesenheiten. Gesamt: genau 300 Entscheidungen.

Alle 25 Fotos registrieren erfolgreich. Eine erfolgreiche Registrierung bestätigt
die planare Zuordnung, nicht den physischen Sockel eines erhöhten Spielsteins.

Coverage bezieht sich auf **vorhandene** Werte, inklusive eventueller
Registrierungsfehler im Nenner. Accuracy akzeptierter Werte zählt **auch akzeptierte
Phantomfarben als Fehler**. Die Berichte enthalten zusätzlich Accuracy ausschließlich
vorhandener Farben (`presentAcceptedAccuracy`), um beide Fragen getrennt zu zeigen.
Rohgenauigkeit misst die unveränderten Feldkandidaten vor Enthaltung. `null` ist
kein Score 0. Bei keiner akzeptierten Entscheidung ist Accuracy `null` statt 100 %.

## Ausgangsdiagnose

| Baseline A | Calibration | Holdout, erst nach Auswahl ausgewertet |
|---|---:|---:|
| Rohwerte korrekt | 93/123 (75,6 %) | 42/60 (70,0 %) |
| High: richtig / falsch, vorhandene Werte | 56 / 4 | 17 / 1 |
| Medium: richtig / falsch, vorhandene Werte | 15 / 6 | 4 / 3 |
| Uncertain: richtig / falsch, vorhandene Werte | 22 / 20 | 21 / 14 |
| High Coverage | 48,8 % | 30,0 % |
| High+Medium Coverage | 65,9 % | 41,7 % |
| High Accuracy, inkl. Phantomfarben | 81,2 % | 73,9 % |
| High+Medium Accuracy, inkl. Phantomfarben | 69,6 % | 61,8 % |
| High+Medium Accuracy, nur vorhandene Farben | 87,7 % | 84,0 % |
| Phantomwerte / davon High / davon akzeptiert | 58 / 9 / 21 | 28 / 5 / 9 |

Vor jeder Confidence-Änderung wurden sämtliche Calibration-Uncertain-Entscheidungen
mit ursprünglicher/finaler Confidence, Score, Margin, FullColor, CoreColor,
Track-/Feldsichtbarkeit, gewachsener Farbmaske im Kandidaten und beiden Nachbarn,
Warncode, Farbe und Track erfasst. Sie stehen vollständig in
`Reports/confidence-v2-initial-diagnosis.json`.

Ursachen der 22 **korrekten** Calibration-Uncertain-Werte:

| Ursache | Anzahl | Tracks | Farben |
|---|---:|---|---|
| Score/Margin | 14 | 7 Attraktivität, 1 Artenschutz, 6 Ruf | 6 Gelb, 4 Rot, 3 Schwarz, 1 Blau |
| Track unter 99 % sichtbar | 7 | 4 Attraktivität, 3 Artenschutz | 3 Rot, 2 Gelb, 1 Schwarz, 1 Blau |
| Pauschales Nachbar-Veto | 1 | Artenschutz | Schwarz |

Absolute Occupancy-Scores nahe 1 sind häufig gesättigt; die Margin verliert dann
Information. Die unbeschnittenen Kern-Farbanteile unterscheiden einige dieser
Kandidaten weiterhin. Umgekehrt beweisen hoher Score und große Margin allein
keinen Stein: bedruckte Randfelder erzeugen Phantomfarben. Auf Calibration ist
Artenschutz nur zu 22/41 richtig; Ruf dagegen zu 38/41. Blau (14/27) und Schwarz
(21/33) sind erheblich schwächer als Rot (28/30) und Gelb (30/33).

## Ablationen ausschließlich auf Calibration

Es gab fünf benannte Varianten, keine automatische Grid-Suche:

| Variante | Änderung | Coverage H+M | Accuracy H+M inkl. Phantomfarben | Falsche High, vorhanden | Phantom-High |
|---|---|---:|---:|---:|---:|
| A | Baseline | 65,9 % | 69,6 % | 4 | 9 |
| B | Gewähltes Feld statt kompletter Track | 74,0 % | 66,1 % | 6 | 11 |
| C | Nachbar mindestens 15 %, Balance mindestens 0,75 statt pauschal 6 % | 69,9 % | 66,1 % | 8 | 11 |
| D | B+C | 78,9 % | 62,7 % | 11 | 13 |
| E | B+C plus konservative Farbstützung/Kernprüfung, Artenschutz-Enthaltung | 49,6 % | 100 % | 0 | 0 |

B gibt sieben richtige und drei falsche vorhandene Werte zusätzlich frei; außerdem
sechs weitere Phantomentscheidungen. C gewinnt nur einen richtigen vorhandenen Wert,
aber vier falsche und zwei Phantomentscheidungen hinzu. D erfüllt die Coverage-Vorgabe
auf Kosten vieler Fehler. Diese Varianten sind keine sichere Verbesserung.

## Gewählte Variante E

E wurde vor der Holdout-Auswertung ausgewählt. `confidence-policy-selection.json`
enthält Begründung, Calibration-Ergebnis und SHA-256 von Split, Policy, Analyzer und
Calibration-Bericht. Der Holdout-Aufruf prüft Policy-/Split-Freeze vor Bildzugriff.
Seit der ersten Holdout-Auswertung wurden keine Confidence-Regeln angepasst.

E ist eine **Absicherung**, kein erfolgreicher Nachweis höherer Coverage:

- Mindestens zwei Tracks derselben Farbe müssen je mindestens 15 % Kernfarbe zeigen.
  Sonst bleibt die Anwesenheit unbestätigt: final `null/Uncertain`. Der rohe Kandidat
  bleibt im Benchmark separat erhalten. Dies verwirft auch einige echte, schwache Farben.
- Lokale Sichtbarkeit des gewählten Feldes muss mindestens 97 % betragen. Entfernte,
  abgeschnittene Felder lösen kein pauschales Veto mehr aus.
- Ein starker Kern verlangt mindestens 30 % Kernfarbe und 20 % Gesamtfarbe sowie
  mindestens das 1,5-Fache der Kernfarbe des zweitbesten Feldes. Bei Score mindestens
  0,58 kann diese Dominanz eine durch Sättigung kleine Margin als Medium auffangen.
- Für Ruf reicht alternativ mindestens 15 % Kern-/12 % Gesamtfarbe bei Margin 0,12;
  Kern-Dominanz und Farbstützung gelten weiterhin.
- Artenschutz bleibt grundsätzlich Uncertain. Auf Calibration existieren falsche
  High-Kandidaten ohne Nachbarfarbe, unter anderem 20→21 und Schwarz→0. Die vorhandenen
  Merkmale trennen diese nicht zuverlässig von korrekten Kandidaten. Eine bloße
  Nachbarregel reicht dafür nicht aus. Der Kandidatenwert wird nicht korrigiert.

## Finale Ergebnisse

| Variante E | Calibration | Holdout |
|---|---:|---:|
| Rohwerte korrekt | 93/123 | 42/60 |
| High: richtig / falsch | 38 / 0 | 12 / 0 |
| Medium: richtig / falsch | 23 / 0 | 11 / 0 |
| Uncertain | 62 | 37 |
| Davon roher Kandidat richtig | 32 | 19 |
| High Coverage / High Accuracy | 30,9 % / 100 % | 20,0 % / 100 % |
| High+Medium Coverage / Accuracy | 49,6 % / 100 % | 38,3 % / 100 % |
| Abwesende Werte explizit null | 81/81 | 36/36 |
| Phantomwerte / Phantom-High / akzeptierte Phantome | 0 / 0 / 0 | 0 / 0 / 0 |
| Vorhandene Werte final null | 15 | 12 |
| Auf 12 vorhandene Werte normierte manuelle Prüfungen | 6,05 | 7,40 |

Der Analyzer hatte vor Enthaltung in beiden Splits je zwei fehlende Kandidaten.
E erhöht diese Zahl bewusst. Deshalb können Rohgenauigkeit und finale Trefferzahl
nach Enthaltung voneinander abweichen; beide stehen im JSON. Die 100 % beziehen sich
auf eine kleine akzeptierte Teilmenge, nicht auf alle Entscheidungen oder eine
statistisch abgesicherte Produktionsquote.

| Track | Calibration: roh / akzeptiert | Holdout: roh / akzeptiert |
|---|---:|---:|
| Attraktivität | 33/41 / 28/41 | 14/20 / 10/20 |
| Artenschutz | 22/41 / 0/41 | 11/20 / 0/20 |
| Ruf | 38/41 / 33/41 | 17/20 / 13/20 |

| Farbe | Calibration: Rohgenauigkeit / Coverage | Holdout: Rohgenauigkeit / Coverage |
|---|---:|---:|
| Rot | 28/30 / 63,3 % | 12/15 / 46,7 % |
| Blau | 14/27 / 48,1 % | 9/15 / 40,0 % |
| Gelb | 30/33 / 66,7 % | 14/15 / 66,7 % |
| Schwarz | 21/33 / 21,2 % | 7/15 / 0 % |

Alle tatsächlich akzeptierten Werte in diesen Untergruppen sind korrekt. Die
fehlende Schwarz-Coverage im Holdout ist eine wesentliche verbleibende Einschränkung.

Das Sicherheitsgate besteht auf beiden Splits: kein falsches High, kein Phantom-High,
genaue Entscheidungszahl, leeres Bild/halbe Tafel abgewiesen und keine High-Werte auf
der leeren Referenz. **Das 75-%-/98-%-Ziel besteht nicht.** Auf Holdout liegen überhaupt
nur 42 richtige Kandidaten vor; selbst perfekte Confidence könnte bei mindestens
45 akzeptierten Werten keine 98 % erreichen. Mehr Confidence-Tuning allein löst das
Problem daher nicht.

## Perspektiven

Alle folgenden High+Medium-Entscheidungen von E sind korrekt. P95 bezieht sich auf
Pixel der Registrierung bei maximal 1600 px langer Kante, nicht auf das Masterbild.

| Split / viewIndex | Registrierung | mittlerer P95 | Rohwerte richtig | H+M Coverage | Uncertain | rohe Nachbarwarnungen |
|---|---:|---:|---:|---:|---:|---:|
| Calibration / 1 | 6/6 | 2,33 | 27/36 | 47,2 % | 19 | 1 |
| Calibration / 2 | 5/5 | 2,35 | 25/33 | 57,6 % | 14 | 2 |
| Calibration / 3 | 4/4 | 2,24 | 21/30 | 43,3 % | 17 | 3 |
| Calibration / 4 | 2/2 | 2,13 | 20/24 | 50,0 % | 12 | 2 |
| Holdout / 1 | 1/1 | 2,56 | 10/12 | 41,7 % | 7 | 1 |
| Holdout / 2 | 2/2 | 2,40 | 10/15 | 46,7 % | 8 | 0 |
| Holdout / 3 | 2/2 | 2,34 | 7/15 | 26,7 % | 11 | 1 |
| Holdout / 4 | 3/3 | 2,48 | 15/18 | 38,9 % | 11 | 1 |

Die Zusammensetzung unterscheidet sich pro Index. Für einen vergleichbareren Blick
auf die Perspektive dient dieselbe schwierige Stellung im Holdout:

| Foto / viewIndex | P95 | Roh richtig | Attraktivität / Artenschutz / Ruf, je 4 | H+M | Uncertain |
|---|---:|---:|---|---:|---:|
| IMG_4286 / 1 | 2,557 | 10/12 | 3 / 3 / 4 | 5/12 | 7 |
| IMG_4287 / 2 | 2,438 | 8/12 | 2 / 2 / 4 | 5/12 | 7 |
| IMG_4288 / 3 | 2,540 | 7/12 | 3 / 2 / 2 | 4/12 | 8 |
| IMG_4289 / 4 | 2,567 | 11/12 | 3 / 4 / 4 | 5/12 | 7 |

Ähnlicher Registrierungsfehler trotz deutlich unterschiedlicher Rohgenauigkeit
spricht für Segmentierungs-/Belegungsprobleme, nicht primär eine schlechte Homographie.
Bei normaler Stellung A liefert Index 3 ebenfalls nur 8/12 gegenüber 10/12 bei
Index 4. In Stellung B sind beide mit 10/12 gleichauf. Es gibt damit einen Hinweis
auf einen ungünstigeren dritten Blick, aber keinen allgemein bewiesenen besten Winkel.

`viewIndex` ist laut Datensatznotizen nur Aufnahmereihenfolge. Eine verbindliche
physische Kameraposition lässt sich daraus nicht angeben. Empfehlung: Die reale
Position von IMG_4289 gegenüber IMG_4288 in einem künftigen kontrollierten Versuch
mit Azimut, Neigung und Abstand erfassen und auf mehreren Stellungen/Geräten
replizieren. Noch keine feste Position vorschreiben und keine Kamera-UX implementieren.

## Verbleibende Uncertain-Werte und benötigte Evidenz

Alle 37 Holdout-Uncertain-Entscheidungen stehen mit Rohkandidat, Erwartungswert und
kompletter Diagnostik in `Reports/confidence-v2-holdout.json`. Von den 19 richtigen
Rohkandidaten bleiben zehn wegen Artenschutz, sechs wegen schwachem/mehrdeutigem
Kern und drei wegen fehlender Farbstützung unsicher.

Die 18 falschen/fehlenden Rohkandidaten betreffen insbesondere:

- Artenschutz: 20→21, Schwarz→0, Blau→41 oder 23. Teilweise fehlen Fuß-/Farbsignale,
  obwohl der sichtbare Steinkörper ein anderes Feld stark belegt.
- Schwarz auf Attraktivität: 62→110/113/1/35; Schatten und dunkle Referenzbereiche
  bleiben schwer unterscheidbar.
- Schwaches Blau, unter anderem fehlender Ruf in IMG_4288.
- IMG_4275: die rote Einzelkonfiguration wird auf allen drei Tracks verfehlt und
  vollständig zurückgehalten.

Einige Einzelstein-Artenschutzlabels 20 stimmen nicht mit dem starken planaren
Kandidaten 21 überein. Ground Truth und Geometrie wurden ausdrücklich unverändert
ausgewertet; vor einer Geometrieänderung sollten physisches Sockelfeld und Annotation
unabhängig gegengeprüft werden. Der Bericht behauptet keine korrigierte Annotation.

Nächster notwendiger Erkenntnisgewinn: bessere Evidenz für tatsächlichen Sockel/
Feldbelegung und farbabhängige Referenzfehler, insbesondere Schwarz/Blau, sowie
unabhängig bestätigte Grenzfälle. Zusätzliche, kontrolliert dokumentierte Ansichten
sind dafür sinnvoll. Erst nach Änderung solcher Merkmale wäre ein **neuer** Holdout
nötig; dieser Holdout darf nicht weiter zur Threshold-Auswahl dienen.

## Reproduktion und Tests

Aufrufe und Exitcodes stehen im README. Die JSON-Berichte enthalten getrennte
Calibration-/Holdout-Auswertungen je Track, Farbe, viewIndex und jede Einzelentscheidung.
Die ausgewählte Policy wird auch bei `analyze` verwendet. Der alte `benchmark`-Aufruf
bleibt der ausdrücklich als Baseline gekennzeichnete 72-Werte-Regressionstest.

Automatisierte Policy-Tests prüfen Sichtbarkeit, ausgewogene Nachbarbelegung ohne
Wertkorrektur, Phantom-Enthaltung, Sättigung/Kerndominanz und Artenschutz-Enthaltung.
Der Solution-Build besteht mit 0 Warnungen und 0 Fehlern. Alle 75 Core- und
170 Application-Tests bestehen. Die ursprüngliche 72-Werte-Regression bleibt bei
60/72 richtigen Rohwerten ohne falsches High; ihre Registrierungs-Negativtests
bestehen ebenfalls. Zusätzlich wurden die Berichtsnenner einschließlich akzeptierter
Phantomfarben sowie Policy-/Analyzer-/Split-/Calibration-Bericht-Hashes geprüft.
Änderungen bleiben lokal und ungestaged; kein Commit oder Push durch den Agenten.
