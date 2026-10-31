# C3 V5 – begrenzter Abschlussversuch

**Noch nicht abschlussreif für automatische Übernahme.** Die ausgewählte kleine
Fusion F2 erreicht auf wiederverwendetem V3 **104/156 richtige Werte**, 78,2 %
Coverage und **85,2 % Accuracy ihrer Ausgaben**. Schon auf alter Calibration
verfehlt sie das 98-%-Ziel. Ein echter neuer Holdout bleibt nötig, kann diesen
bereits sichtbaren Qualitätsmangel aber nicht allein beheben.

Das belastbare technische Ergebnis ist die isolierte A-Härtung: der numerische
Ausfall entfällt, A steigt auf V3 von **103 auf 113/156**. Der lokale C3-Readout
verbessert Schwarz und gemeinsame Felder, produziert jedoch zusätzliche Phantome.
Deshalb wurde er nach den vorab festgelegten Calibration-Gates nicht übernommen.

## Umfang und Auswahl

Basis: sauberer aktueller `master`/`origin/master` bei
`295022bd3f2dfe855f663e449c1b8059a0b887e9`. Neues Werkzeug:
[C3Refinement](../../../C3Refinement/). Keine neue Detektorfamilie, kein ML,
keine C1/C2-/ECC-Weiterentwicklung und keine App-Workflow-Integration.
V4-Geometrie, Unterraum, 16×16-Auflösung, Rang 3, Lambda .04 und acht Sparse-
Schritte wurden wiederverwendet. Keine Patchgrößen-/Gewichtungs-/Parameter-Grid-Suche.

[PLAN.md](PLAN.md) legt Varianten, Gates und Fusionsregeln vor den neuen Metriken
fest. Ausschließlich Empty-Daten fitten; 17 alte Calibration-Fotos wählen.
V2-Holdout und V3 sind wiederverwendete Vergleiche, **keine unabhängige Validierung**.
[selection.json](selection.json) und [freeze.json](freeze.json) entstanden vor
deren Auswertung. 366 Quell-/Konfigurations-/Inputhashes wurden vor und nach
Vergleich geprüft. Nach Vergleich kein Nachjustieren.

Historische Quellen von V3/V4, Reports und Freeze-Dateien wurden nicht überschrieben.
Nur zwei bestehende Maskenmittelwert-Aufrufstellen in A wurden separat technisch
gehärtet. Alte Freeze-Prüfungen dieser Quellen schlagen daher unter aktuellem
Code erwartungsgemäß an; ihre historischen Hashes wurden nicht umgeschrieben.

## 1. Welche minimalen Verbesserungen wurden getestet?

| Stand | Änderung | Auswahl ausschließlich auf Calibration |
|---|---|---|
| P0 | V4-C3 einschließlich altem Feld-CoreColor-Readout | reproduzierte Baseline |
| P1 | Nur Attraktivität: eigene Empty-LOO-Pixel-/Feldgrenzen als konservative Floors | verworfen: FP-Rückgang unter dem vorher festgelegten 25-%-Gate |
| P2 | Lokaler mehrfarbiger Sparse-Readout auf der ausgewählten Occupancy P0 | verworfen: zusätzliche Phantome über dem festgelegten Sicherheitsgate |

P1 nutzt je Attraktivitätsfeld max(V4-Trackgrenze, eigenes 99-%-Normalquantil)
für Sparse-Pixel sowie max(V4-Trackgrenze, maximaler eigener Normal-Feldscore)
für Occupancy. Jedes Normalbild wird zur Schwellenbestimmung aus dem jeweiligen
Unterraum ausgelassen. Artenschutz und Ruf bleiben unverändert.

P2 verwendet A-Strict-Farbmasken ausschließlich in akzeptierten Sparse-Komponenten
innerhalb Feld/Core. Schwarz darf zusätzlich das bereits bestehende strenge-
Seeds-/Relaxed-Wachstum nutzen, benötigt aber mindestens drei eigene lokale
strenge Seeds. Andere Farben benötigen mindestens sechs eigene Pixel, jede
Farbe mindestens zwei Corepixel und 10 % der Sparse-Kontaktfläche. Außenpixel
werden getrennt diagnostiziert und tragen nicht zur positiven Farbevidenz bei.
Mehrere Farben dürfen dasselbe Feld ausgeben. Je Farbe werden lokaler Coreanteil
und danach Occupancy geordnet, ohne gewichtete Meta-Formel.

## 2. Was half gegen Attraktivitäts-FPs?

| Satz | P0 TP/FP/FN Attraktivität | P1 TP/FP/FN | FP-Rückgang |
|---|---|---|---:|
| Calibration | 40/111/1 | 40/98/1 | 11,7 % |
| Alter V2-Vergleich | 15/44/1 | 15/30/1 | 31,8 % |
| V3-Vergleich | 37/47/13 | 37/39/13 | 17,0 % |

Die Feld-Floors helfen moderat und verlieren hier keine TP. Das vorab festgelegte
Calibration-Ziel „deutlich“ = mindestens 25 % weniger FP wird verfehlt, deshalb
bleibt die Auswahl bei P0. Die besseren nachträglichen Vergleichszahlen dürfen
diese Auswahl nicht rückwirkend ändern. Keine weitere Auflösungs-/Kontaktablation.

## 3. Was half bei Schwarz, wurden gemeinsame Felder besser?

| Satz | Schwarz P0 → P2 | Gemeinsame Felder P0 → P2 | Phantomwerte P0 → P2 |
|---|---|---|---|
| Calibration | 21 → **29/33** | 3 → **6/8** | 6 → **11** |
| Alter V2-Vergleich | 2 → **11/15** | 9 → **18/24** | 4 → **6** |
| V3-Vergleich | 17 → **27/39** | 3 → **11/15** | 0 → 0, alle Farben vorhanden |

Lokalisiertes seeded Schwarz und das Aufheben des pauschalen Einfarben-Winners
liefern messbaren Nutzen. Calibration-Top1 steigt von 94 auf 101/123. Die fünf
zusätzlichen Phantomwerte verletzen jedoch das vorher festgelegte Gate von
höchstens zwei zusätzlichen Phantomen. Besonders dunkler Boarddruck und kleine
farbige Änderungen können als weitere Spielerfarbe erscheinen.

Gemeinsam bedeutet ausschließlich: mehrere GT-Farben auf demselben Trackwert;
dicht bedeutet: unterschiedliche GT-Farben auf benachbarten Werten. Diese Labels
dienen nur Gruppenauswertung, niemals der Erkennung. P2 löst nicht jede Mischung:
bei Fall3/Ruf 7 gewinnt Schwarz hinzu, Blau kann im Sparse-Kontaktbereich fehlen.
Gehärtetes A trifft auf V3 13/15 gemeinsame Werte, P2 nur 11/15. Der Fortschritt
gegenüber altem C3 ist keine pauschale Überlegenheit gegenüber A.

## 4. Welche kleine Fusionsregel wurde gewählt?

Zwei Regeln, keine Gewichtungssuche. Beide verwenden A-High/Medium, sofern kein
starker C3-Widerspruch besteht; bei starkem Konflikt null.

- **F1:** Ohne sicheres A nur starkes C3 bei Übereinstimmung mit A-Rohwert.
- **F2:** Attraktivität benötigt zusätzlich sehr starke, isolierte C3-Evidenz
  und Farbstützung auf einem anderen Track. Artenschutz darf starkes isoliertes
  C3 helfen; widerspricht zugleich ein starker A-Rohwert, bleibt die Entscheidung
  null. Ruf erlaubt Übereinstimmung oder bei A-null sehr starkes, isoliertes C3
  mit unabhängiger Farbstützung.

Stark/sehr stark/Isolation sind im Plan numerisch festgelegt. Auch beim alten
Readout stammen diese Sicherheitsmerkmale aus dem Sparse-lokalen Farbgebiet;
die ausgegebenen P0-Feldwerte selbst bleiben der alte V4-Readout.

F2 gewinnt den Calibration-Vergleich, erreicht aber nur **88/93 = 94,6 %**
Ausgabe-Accuracy bei 75,6 % Coverage. F1 erreicht 88/94 = 93,6 %. Keine Regel
besteht das 98-%-Gate. Alle fünf falschen F2-Calibration-Ausgaben betreffen
Artenschutz **20→21** bei Rot/Gelb. A und C3 bestätigen hier denselben falschen
Feld-/Sockelkandidaten; Übereinstimmung ist kein Korrektheitsbeweis.

## 5. A-Härtung – strikt getrennt von C3

`ArkNovaMaskFraction.ClampRoundoff` begrenzt ausschließlich Rundungsabweichungen
bis 1e−12 an [0,1]. Mathematisch gültige Anteile bleiben bitgleich; NaN, unendlich
und materielle Bereichsverletzungen werfen weiter eine Exception. Keine Änderung
an Masken, Scoreformeln, Ranking, Confidence, Domain oder manueller Wertung.

[a-hardening.json](a-hardening.json): alle 48 frisch registrierten Frames sind
SHA-256-identisch zum V4-Cache. Bei **564 zuvor erfolgreichen Entscheidungen**
bleiben Kandidaten und Confidence-Werte gleich, Scores stimmen bis 1e−12 überein.
Auch der echte Analyzer-Lauf von Fall3Senkrecht gelingt jetzt. Dort werden 10/12
Rohwerte und fünf zusätzliche High/Medium-Werte richtig; die übrigen zwei
Rohfehler bleiben sichtbar. Diese zehn Treffer werden nicht C3 zugeschrieben.

## 6. Wie gut sind A, C3 und die reale Fusion?

Top1 zählt alle vorhandenen Werte einschließlich null im Nenner. Ausgabe-Accuracy
zählt bei abwesenden Farben auch Phantomwerte als Fehler. Physische TP/FP/FN
zählen pro Foto/Track eindeutige Felder; mehrere Farben am selben Feld einmal.
Bei A/Fusion ist das die Menge ausgegebener Farbwerte, bei C3 die farbunabhängige
Kandidatenmenge vor Farb-Readout. Automatischer Score 0 bleibt ausgeschlossen.

| Verfahren | Calibration Top1 /123 | Alter V2 Top1 /60 | V3 Top1 /156 | V3 null | V3 Ausgabe-Accuracy |
|---|---:|---:|---:|---:|---:|
| A historisch | 94 | 45 | 103 | 24 | 78,0 % |
| **A gehärtet, roh** | **94** | **45** | **113** | 13 | 79,0 % |
| A gehärtet, High/Medium | 61 | 23 | 66 | 76 | 82,5 % |
| **C3 P0, ausgewählt** | 94 | 30 | 100 | 34 | 82,0 % |
| P1 Feld-Floors, nicht ausgewählt | 94 | 30 | 100 | 35 | 82,6 % |
| P2 lokal, nicht ausgewählt | 101 | 44 | 114 | 14 | 80,3 % |
| F1 Bestätigung | 88 | 32 | 101 | 37 | 84,9 % |
| **F2 Track-Regel, ausgewählt** | **88** | **30** | **104** | 34 | **85,2 %** |

| V3 physisch | TP | FP | FN | Precision | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| A gehärtet | 107 | 26 | 41 | 80,5 % | 72,3 % | 0,762 |
| C3 P0/P2 | 116 | 64 | 32 | 64,4 % | 78,4 % | 0,707 |
| P1 | 116 | 56 | 32 | 67,4 % | 78,4 % | 0,725 |
| F2 | 102 | 18 | 46 | 85,0 % | 68,9 % | 0,761 |

P2 verbessert den Readout, nicht die vorgelagerte farbunabhängige Occupancy.
Seine physischen Zahlen dürfen deshalb nicht nach Farbplausibilität schöngefiltert
werden. Bei fehlenden Vorhersagen wird Precision rechnerisch als 0 geführt.

### Komplementarität gegenüber gehärtetem A und diagnostischer Oracle

| Satz / C3 | beide richtig | A richtig / C3 falsch | A falsch / C3 richtig | beide falsch | Oracle |
|---|---:|---:|---:|---:|---:|
| Calibration P0 | 88 | 6 | 6 | 23 | 100/123 |
| Calibration P2, verworfen | 92 | 2 | 9 | 20 | 103/123 |
| Alter V2 P0 | 29 | 16 | 1 | 14 | 46/60 |
| Alter V2 P2, verworfen | 42 | 3 | 2 | 13 | 47/60 |
| V3 P0 | 93 | 20 | 7 | 36 | **120/156** |
| V3 P2, verworfen | 101 | 12 | 13 | 30 | **126/156** |

Der ausgewählte F2-Weg korrigiert auf V3 drei A-Fehler, verliert aber zwölf zuvor
richtige A-Werte: netto −9. Oracle ist reine GT-Diagnose, keine reale Regel.
Der größere P2-Oracle rechtfertigt nachträglich keine Auswahl dieses Readouts.

Vollständige Top1/2/3-, null-, TP/FP/FN-, Precision/Recall/F1-, Farb-, Track- und
View-Tabellen für **jeden Satz und Stand**: [METRICS.md](METRICS.md).
Einzelentscheidungen: [Calibration](calibration.json), [V2](old_holdout_reused.json),
[V3](v3_reused.json). Gemeinsame/dichte Felder und A-Uncertain/null sind separat
gruppiert. Top-k von A ist diagnostische Rangliste vor Enthaltung; Fusionen geben
höchstens einen Wert aus, ihre Top2/3-Zahlen entsprechen deshalb Top1.

## 7. Grenzen, Diagnose und Aufwand

Nur drei PNG-/JSON-Paare, mit Originalpatch, Sparse-Karte und Kontaktmaske:
[Schwarz-Artenschutz 20](Diagnostics/old_holdout_reused_IMG_4268.png),
[gemeinsames Ruf-Feld 7](Diagnostics/v3_reused_Fall3Senkrecht.png),
[Nachbarschaft bei Ruf 11/12](Diagnostics/v3_reused_Fall5Senkrecht.png).
Der letzte Fall bleibt falsch: GT Schwarz 11, lokaler Readout/A 12. Kein blindes
Zurücksnappen auf 11. Pixelanteile in Feld, Core und Außenbereich stehen im JSON.

A-High/Medium vererbt auf V3 Fehler; starke Sparse-/Farblokalisation kann
Nachbarmaterial, Druck oder Schatten nicht immer von tatsächlichem Sockelkontakt
trennen. Reale Near-Object-Zuverlässigkeit und Score-0-Enthaltung sind mit diesen
Bildern nicht abschließend validiert. Es wurden keine realen Kontaktlabels erfunden.

Gemeinsame Auswertung von P0/P1/P2, Farbmasken-I/O und Ranglisten: im Mittel
255 ms/Bild auf Calibration, 292 ms auf altem V2, 275 ms auf V3. Kein isolierter
End-to-end- oder Gerätebenchmark, A-Registrierung separat vorbereitet; Diagnose-
export nicht in diesen Zeiten enthalten. Peak Working Set bis etwa 536 MiB.
Keine neue mobile Integration oder Modellsuche.

## 8. Abschlussentscheidung und exakt fehlende Motive

**Nicht „Bilderkennung damit abschließen“. Ein letzter echter neuer Holdout ist
notwendig; allein neue Fotos reichen bei dieser Fusionsqualität nicht zur Freigabe.**
F2 erreicht auf Calibration/V2/V3 94,6/96,8/85,2 % Ausgabe-Accuracy. Das 98-%-Ziel
scheitert überall, auf V2 zusätzlich Coverage nur 51,7 %. Aktuelles A+C3 deshalb
nicht automatisch übernehmen. Den klar isolierten A-Zahlenfix behalten; die
fehlende sichere Farb-/Sockelzuordnung ist keine weitere Detektorfamilienfrage.
Ohne tragfähige Sicherheitsregel bleibt nur manuell überprüfte Erkennung.

Für den nächsten unabhängigen Abschlussnachweis müssen neue Stellungen mit
vorab unabhängig bestätigtem **Sockelfeld und Farbe** enthalten sein:

1. Schwarz und Blau auf allen drei Tracks, einzeln und zusammen; dunkles Holz,
   schwache Reflexion/Belichtung und zusätzliche negative Fotos ohne diese Farbe.
2. Rot/Gelb-Artenschutz bei 20/21 sowie einem anderen benachbarten Feldpaar;
   senkrechte Kontrollaufnahme des Sockels plus dieselbe Stellung schräg. Dies
   prüft ausdrücklich den gemeinsamen A/C3-Nachbarfehler.
3. Zwei Farben auf demselben Attraktivitäts-, Artenschutz- und Ruf-Feld sowie
   zwei Farben auf benachbarten Feldern; gleiche Stellungen in senkrecht/vorn/seitlich.
4. Attraktivität mit unbesetzten dunklen/bedruckten Feldern neben echten Steinen,
   zum Prüfen der FP-Unterdrückung ohne Recallverlust.
5. Karten/Plättchen unmittelbar unter Ruf: eindeutig kein Kontakt, tatsächlicher
   Kontakt als Kontrolle; neue Empty-Aufnahmen bei denselben Lichtverhältnissen.
6. Echte manuell gültige 0-Stellungen für Attraktivität/Artenschutz mit allen
   anderen Tracks belegt: CV muss null liefern, ohne Ersatzfeld.

Diese Motive sind eine Aufnahmespezifikation, keine erfundene Ground Truth.
Neue Auswahl oder Sicherheitsregel nur auf Calibration/Empty festlegen und
vor GT-Metriken des neuen Satzes einfrieren. V2/V3 nicht weiter feinoptimieren.

## Build, Tests und Lieferung

Solution-Build: 0 Warnungen/0 Fehler. **75 Core- und 178 Application-Tests**
bestanden, darunter drei neue numerische Regressionstests. **Acht Forschungschecks**
bestanden: Grenzrundung, F1-Enthaltung, strenge Attraktivität, erlaubte Artenschutz-
Hilfe, leere Sparse-Map, gemeinsame Farben, fehlende Schwarz-Seeds, Außen-/0-Ausschluss.
Details: [verification.json](verification.json).

`C3Refinement` bietet `prepare`, `calibrate`, `compare`, `self-test` mit Repository-
Root als zweitem Argument. Bestehende Freezes/Latches nicht löschen oder überschreiben.
[Summarize.ps1](Summarize.ps1) regeneriert ausschließlich Tabellen aus gespeicherten
Ergebnissen. Quellen/Inputs bleiben eingefroren. App-Version gemäß Repositorypflicht
**0.10.0.14 / 51**; Änderungen lokal, ungestaged/uncommittet auf master, kein Push.
