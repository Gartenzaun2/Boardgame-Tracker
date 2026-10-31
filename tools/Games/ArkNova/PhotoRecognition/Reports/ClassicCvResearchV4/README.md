# Classic CV Research V4

**C3 liefert einen ernsthaften zweiten Occupancy-Signalweg. Die getestete K4-Regel
ist noch kein geeignetes Ensemble.** Auf dem wiederverwendeten V3-Satz erreicht
C3 physisch 116 TP / 64 FP / 32 FN: 78,4 % Recall, 64,4 % Precision, F1 0,707.
Mit einem einfachen nachgelagerten Farb-Readout korrigiert C3 zwölf A-Fehler;
fünf davon betreffen den bekannten numerischen A-Ausfall, sieben sind zusätzliche
Feld-/Farbtreffer bei regulär gelaufenem A. Der farbige Oracle steigt von 103 auf
115/156. Die vorab gewählte K4-Regel liefert dagegen nur 96/156 richtige Werte.

Die getesteten geometrischen Shape-Templates und Census-Konfigurationen bleiben
ohne belegte Feldtreffer. ECC verbessert die Hintergrundkorrelation, führt aber
zu keinem Occupancy-Gewinn. Diese Konfigurationen werden nicht weiter kombiniert.

## Forschungsbasis und Disziplin

- Sauberer `master`, nach Fetch identisch mit `origin/master` auf
  `e7d34f44de6ae05e7ea21e92ce4b4aa96dd3accd`.
- Ausschließlich separates Entwicklungsprojekt
  [ClassicCvResearch](../../../ClassicCvResearch/). Bestehende A-Quellen,
  Produktionslogik, Feldgeometrie, Confidence, frühere Forschungsquellen,
  historische Reports und Freeze-Dateien bleiben unverändert.
- Kein ML-Framework, kein neuronales Netz, keine externen Gewichte, keine
  Web-/Modellrecherche. C3 benutzt ausschließlich klassische SVD und
  Soft-Threshold-Arithmetik auf den autorisierten Empty-Fotos.
- Bestehende Registrierung über `ArkNovaPhotoAnalyzer` und dessen vorhandenen
  `FrameObserver` wiederverwendet; keine neue globale Registrierung.
  Derselbe registrierte Frame und dieselben Feldpolygone gelten für alle Verfahren.
- 48 vorhandene Bilder: zehn Empty-Normale, 17 alte Calibration-, acht alte
  V2-Vergleichs- und 13 V3-Vergleichsfotos. Alle registrieren erfolgreich.
  Originalbild-Hashes aus beiden Manifesten wurden geprüft.
- **Kein unabhängiger Holdout.** Die 17 alten Calibration-Fotos entscheiden über
  Kombinationen. V2-Holdout und V3 sind wiederverwendete Forschungs-/Vergleichsdaten.
  V3 wurde nicht zur Parameterauswahl benutzt; es ist trotzdem kein frischer
  Holdout für diese neuen Methoden. Wiederholte Views einer Stellung sind keine
  unabhängigen Partien.

Der [Versuchsplan](EXPERIMENT_PLAN.md) entstand vor den Methodenmetriken. Alle
Konstanten sind dort bzw. in den eingefrorenen Quellen dokumentiert. Der Shape-
Suchraster wurde vor Belegungsdaten an einer synthetischen Rechtecklokalisierung
auf Translation null verankert; keine Optimierung anhand eines V3-Fehlerfotos.
Schwellen stammen ausschließlich aus Empty-Daten. Nach den unabhängigen
Calibration-Metriken werden wenige Kombinationen über feste Gates ausgewählt.

[selection.json](selection.json) und [freeze.json](freeze.json) wurden vor dem
Vergleich der wiederverwendeten V2-/V3-Daten geschrieben. 173 SHA-256-Werte decken
neue und vorhandene CV-Quellen, Projekt-/Paketkonfiguration, Plan, Modelle,
Auswahl, Calibration-Berichte, ursprüngliche Bilder, Ground Truth, Geometrie und
registrierte Caches ab. Sie wurden vor und nach dem Vergleich geprüft.
Der Vergleich lief vollständig durch; ein `CreateNew`-Latch verhindert Wiederholung
im selben Berichtordner. Keine Parameteränderung nach Vergleichsergebnissen.

Registrierte Vollbilder/Validity und unveränderte A-Merkmale liegen ausschließlich
im ignorierten `artifacts/ClassicCvV4`-Cache. Persistierte JSON-Ergebnisse und eine
kleine Diagnoseauswahl liegen hier. [Summarize.ps1](Summarize.ps1) berechnet nachträglich
nur Tabellen und Audit-Zählungen aus gespeicherten Ergebnissen, ohne Fotos oder
Detektoren erneut auszuwerten oder eine Regel auszuwählen.

## Implementierte Einzelverfahren

### C1: Directional / Oriented Chamfer

Geometrische Templates statt Fototemplates oder ML: Rechtecke für Artenschutz
und Ruf, eine seitlich eingekerbte Langform für Attraktivität. Größen beziehen
sich auf die Feldgeometrie: Attraktivität 0,80 Feldbreite × 0,75 Feldhöhe;
Artenschutz 0,42 Feldbreite × 0,55 Feldhöhe; Ruf Quadrat mit 0,72 Feldhöhe.
Keine Anpassung anhand einzelner V3-Marker.

Canny 60/150 nach 3×3-Gaussian, Sobel-Orientierungen modulo 180° in acht Bins,
Nachbarbin-Toleranz. Distanztransform pro Orientierung; Templatekontur alle
drei Pixel abgetastet. Suche nur im Feldumfeld: ±25 % Feldbreite/-höhe in
4-px-Schritten, Größen 0,85/1/1,15, Winkel −12/0/+12°. Das ist eine feste
Posensuche, keine Hyperparameter-Grid-Suche.

Chamferkosten sind mittlere auf acht Pixel begrenzte orientierte Abstände;
`shapeScore = exp(-cost/2.5)`. Rückgabe enthält Track, Feld, Score, Center,
Kontur und Candidate. NMS unterdrückt doppelte positive Center innerhalb
18 Pixeln desselben Tracks. Farbe beeinflusst keine Shape-Entscheidung.
Gefüllte Templatekontur muss Polygon und Core schneiden; automatische 0 ist
ausgeschlossen. Schwelle ist max(0,65, Empty-99,5%-Quantil) je Track; alle drei
Tracks bleiben am Floor 0,65.

**Ergebnis: ungeeignet in dieser Konfiguration.** Kein Feldtreffer auf Calibration
und beiden Vergleichssätzen. Der höchste Calibration-ShapeScore liegt bei 0,591,
unter dem vorab festgelegten Floor. Konturen treffen teilweise Boarddruck oder
den falschen Teil eines erhöhten Steins; geometrische Größenproxies und kleine
Winkelvariation bilden die reale 3D-Projektion nicht ausreichend ab. Eine visuell
eingezeichnete Kontur ist ausdrücklich kein akzeptierter Treffer.

Generalized Hough wurde **nicht zusätzlich implementiert**: Schon C1 liefert
keine trennbare akzeptierte Feldbelegung. Ein weiterer generischer Shape-Ausbau
auf denselben ungeprüften Silhouetten verspricht in diesem Batch keinen ausreichend
begründeten Zusatznutzen. Das ist kein allgemeiner Gegenbeweis gegen Shape-CV.
Ein späterer Versuch müsste Körpermaße/Projektion oder wenige alte Calibration-
Templates fachlich bestätigen, statt diesen V3-Satz weiter zu optimieren.

### C2a/C2b: Census, ohne und mit lokaler ECC

5×5-Census bildet für jedes Pixel 24 lokale Intensitätsordnungsbits. Minimaler
Hamming-Abstand zu Empty-Referenzcodes derselben registrierten Position, geteilt
durch 24. Das ist ein Ordnungs-/Bitmustervergleich und keine Neuauflage von B1-
Gradienten-/Grauwertschwellen. Pixelgrenze max(6/24, Empty-LOO-99%-Quantil), hier
für alle Tracks 11/24 = 0,4583; positive Änderung verlangt strikt größere Distanz.

C2b korrigiert jeden Track separat zur sauberen Masterreferenz über Translation-
ECC mit höchstens 30 Iterationen/epsilon 1e−4. Feldcores sind aus der ECC-Maske
ausgenommen; keine GT-/Steinmaske. Jede Achse ist auf maximal vier Pixel begrenzt.
Größere, fehlgeschlagene oder nach maskierter Korrelation verschlechterte
Korrekturen werden verworfen. Separate Trackframes verhindern Überschreiben
überlappender Track-ROIs. Um Warp-Ränder nicht als neue Information zu behandeln,
wird Validity für C2b vor Prüfung um vier Pixel konservativ erodiert.

Der synthetische Test verschiebt die Referenz um (+2,−1) Pixel und bestätigt die
Rückgewinnung dieser Translation. Auf realen Empty-Fotos werden 23/30 Trackkorrekturen
übernommen, auf Calibration 46/51, altem V2-Vergleich 21/24 und V3 35/39.
Mittlerer Gewinn der maskierten Hintergrundkorrelation: +0,0093 / +0,0102 /
+0,0082 / +0,0126. Das sind Verbesserungen des Bildähnlichkeitsmaßes, **keine
unabhängig annotierten geometrischen Reprojektionsfehler**.

**Occupancy: kein Gewinn.** C2a und C2b bleiben beide ohne Treffer. Nach
Hamming-Schwelle/Opening bilden sich keine genügend großen Kontaktkomponenten.
Lokale Ordnung ist beleuchtungsrobust, aber der pixelweise Minimalabstand über
mehrere Normale plus hoher Normalquantil-Floor ist hier zu unempfindlich.
Ein positives ECC-Optimierungsmaß allein rechtfertigt deshalb K2 nicht.

### C3: approximative Low-Rank + Sparse-Projektion

Pro Feld einschließlich 24-px-Kontext wird ein Graupatch auf 16×16 reduziert.
Aus zehn Empty-Vektoren: Mittelwert µ und drei größte SVD-Richtungen U des
zentrierten Normalstapels. Kein besetztes Foto fließt in diesen Unterraum ein.
Bei einer Anfrage wird achtmal alterniert:

```text
alpha = Uᵀ (x - µ - S)
L     = µ + U alpha
S     = soft_threshold(x - L, lambda=0.04)
```

Ausgewertet wird |S|. Das ist eine klassische approximative robuste Projektion
auf einen festen Normal-Unterraum, **keine exakte Principal Component Pursuit**:
keine gemeinsame Nuclear-Norm-/L1-Optimierung des gesamten Bildstapels und keine
Behauptung eines global optimalen PCP-Ergebnisses. Rang, Lambda und Iterationen
wurden vor Datenmetriken algorithmisch festgelegt, nicht gesucht.

Pixel-/Feldgrenzen werden mit je einem ausgelassenen Empty-Bild bestimmt; auch
der Unterraum wird für diese LOO-Probe ohne dieses Bild neu berechnet. Finaler
Unterraum für Anfragen benutzt alle zehn Normale. Sparse-Karte wird auf die
Feldpatchgröße interpoliert. Pixel-Floors 0,04; Occupancy-Floor 0,06.

| Track | Sparse-Pixelgrenze | Occupancy-Grenze |
|---|---:|---:|
| Attraktivität | 0,06159 | 0,06877 |
| Artenschutz | 0,06967 | 0,06753 |
| Ruf | 0,04587 | 0,08211 |

### Kontaktregeln und Normaldiagnose

C2/C3 benötigen nach 3×3-Opening zusammenhängende Änderungen mit mindestens acht
Pixeln, mindestens 3 % Polygonkontakt und mindestens 1 % Corekontakt. Nur
Komponentenpixel **innerhalb** Polygon/Core zählen zum Score; Score ist der
Mittelwert beider Anteile. Mindestens 97 % Feldsichtbarkeit. Große Außenanomalien
können allein keinen Score erzeugen. Das ist eine räumliche Evidenzregel,
keine semantische Garantie, dass Schatten/Parallaxe physischen Bodenkontakt beweisen.

Empty-LOO: C2a/C2b keine positiven Felder; C3 zehn positive Normalfeldbeobachtungen
bei 1710 geometrischen Beobachtungen einschließlich diagnostischer Nullregionen.
Diese Zählungen stammen aus dem Fit und sind keine unabhängige Spezifitätsvalidierung.
Score 0 bleibt manuell gültig; keine neue Methode gibt ihn automatisch aus.
Ein fehlender Kandidat wird nicht auf Feld 1 oder einen Nachbarn verschoben.

## Metriken: Occupancy und Farb-Readout getrennt

Physische TP/FP/FN zählen pro Foto/Track **eindeutige** Scorefelder. Mehrere Farben
auf demselben Feld zählen dort einmal. Daher 119 belegte Calibration-Felder,
48 im alten V2-Vergleich und 148 in V3, gegenüber 123/60/156 Farbentscheidungen.
Null/Enthaltung und A-Ausfälle bleiben bei vorhandenen Werten im Nenner.

Alle Detektoren liefern zuerst farbunabhängige Kandidatenmengen. Ein festes,
getrenntes diagnostisches Readout benutzt danach vorhandene A-CoreColor-Werte
im detektierten **Feld**: stärkste Farbe ≥0,06 und ≥1,5-fache zweitstärkste Farbe.
Je Farbe/Track gewinnt der höchste Detektorscore. Keine GT-Spielerzahl,
keine GT-Farbe als Input, kein gewichtetes A+C-Score. Dieses Readout ist keine
neue Farberkennung und kein Nachweis einer von A unabhängigen Farbklassifikation.
Es kann gemischte/übereinander stehende Farben auf demselben Feld nicht auflösen.

Farbige Top1/Top2/Top3 beziehen sich auf dieses Readout. Farbfrei geordnete
Top-k-Feldlisten stehen zusätzlich als `physicalTop1/2/3` im JSON; dieselbe
Liste wird dort gegen jede GT-Farbe geprüft, weshalb dies nur Retrieval-Diagnostik
und keine Spielerzuordnung ist. Primäre farbfreie Metrik bleibt die Feldmenge.
Die Physical-Spalten in den JSON-Farbgruppen wiederholen bewusst die globale
Feldmenge; nur Readout-Spalten sind farbspezifisch.

## Einzelmetriken

[Alle Tabellen nach Methode, Track und View](METRIC_TABLES.md),
[Calibration](calibration-independent.json), [alter V2-Vergleich](old_holdout_reused.json),
[V3-Vergleich](v3_reused.json). Bei C1/C2a/C2b jeweils TP=FP=0 und FN=119/48/148;
alle vorhandenen Farbwerte bleiben null. Das ist Scheitern, keine hohe Accuracy.

| C3 | TP | FP | FN | Precision | Recall | F1 | farbige Top1 / Top2 / Top3 | null |
|---|---:|---:|---:|---:|---:|---:|---|---:|
| Alte Calibration | 112 | 137 | 7 | 45,0 % | 94,1 % | 0,609 | 94 / 96 / 96 von 123 | 19 |
| Alter V2-Vergleich | 43 | 62 | 5 | 41,0 % | 89,6 % | 0,562 | 30 / 32 / 32 von 60 | 23 |
| V3-Vergleich | 116 | 64 | 32 | 64,4 % | 78,4 % | 0,707 | 100 / 100 / 100 von 156 | 34 |

A wird exakt wie zuvor reproduziert: 94/123, 45/60, 103/156 richtige Rohwerte.
Auf V3 bleibt der bestehende `1.0000000000000002`-Evidenzfehler in
`Fall3Senkrecht` ein vollständiger zwölf-Werte-A-Ausfall. Kein Fix/Clamping an A.

### Komplementarität des farbigen C3-Readouts

| Satz | A richtig / C3 richtig | A richtig / C3 falsch | A falsch / C3 richtig | beide falsch | A allein | farbiger Oracle | Kontaktmengen-Oracle |
|---|---:|---:|---:|---:|---:|---:|---:|
| Calibration | 88 | 6 | 6 | 23 | 94/123 | 100/123 | 117/123 |
| Alter V2-Vergleich | 29 | 16 | 1 | 14 | 45/60 | 46/60 | 56/60 |
| V3-Vergleich | 88 | 15 | **12** | 41 | 103/156 | **115/156** | 133/156 |

Kontaktmengen-Oracle erlaubt jede wahre Farbe an einem gefundenen Feld und ist
damit eine optimistische Obergrenze. Er ist weder Farbvorhersage noch Algorithmus.
Der farbige Oracle benutzt ebenfalls GT nur zur Diagnose, nie als Auswahlregel.
Auf V3 ohne das A-Fehlerbild bleiben **sieben** komplementäre Treffer und
110/144 statt 103/144 richtige Oracle-Werte. Die Verbesserung hängt somit nicht
ausschließlich an der Fehlerbehandlung, generalisiert aber nicht gleich stark
auf den alten V2-Satz. Alle Einzelfälle: [derived-diagnostics.json](derived-diagnostics.json).

Beispiele regulärer A-Fehler, die C3 korrigiert:

- Fall1Gegenueber: Blau-Artenschutz 8 statt A 41.
- Fall2Gegenueber: Blau-Artenschutz 22 statt A 41.
- Fall2Rechts: Schwarz-Attraktivität 58 statt A 59, Schwarz-Ruf 6 statt A 5,
  Rot-Ruf 5 statt A 4.
- Fall2Unten: Schwarz-Artenschutz 23 statt A null.
- Fall4Senkrecht: Blau-Artenschutz **21** statt A 24; die unveränderte GT ist 52;21;15.

### Track, Farbe, View auf V3

| Track C3 | TP / FP / FN | Precision | Recall | farbige Top1 /52 | null | A falsch / C3 richtig |
|---|---|---:|---:|---:|---:|---:|
| Attraktivität | 37 / 47 / 13 | 44,0 % | 74,0 % | 29 | 11 | 3 |
| Artenschutz | 39 / 7 / 9 | **84,8 %** | **81,3 %** | 36 | 11 | 5 |
| Ruf | 40 / 10 / 10 | 80,0 % | 80,0 % | 35 | 12 | 4 |

Artenschutz zeigt den deutlichsten physisch brauchbaren Zusatzkanal. Attraktivität
erzeugt viele zusätzliche Feldhypothesen und bleibt die größte FP-Quelle.

| Farbe | A Top1 /39 | C3 Top1 /39 | C3 null | A falsch / C3 richtig |
|---|---:|---:|---:|---:|
| Rot | 30 | 31 | 3 | 3 |
| Blau | 27 | 27 | 6 | 4 |
| Gelb | 30 | 25 | 7 | 2 |
| Schwarz | 16 | 17 | 18 | 3 |

Die farbfreie Kontaktmenge stützt 17 zuvor falsche schwarze A-Werte, der Farb-
Readout macht daraus aber nur drei richtige schwarze Zusatzwerte. Das trennt
Occupancy-Fortschritt von fortbestehender Schwäche der schwarzen Farbmerkmale.

| View C3 | TP / FP / FN | Precision | Recall | farbige Top1 | null |
|---|---|---:|---:|---:|---:|
| opposite_side | 19 / 12 / 5 | 61,3 % | 79,2 % | 15/24 | 6 |
| left | 19 / 14 / 5 | 57,6 % | 79,2 % | 19/24 | 1 |
| right | 19 / 21 / 5 | 47,5 % | 79,2 % | 18/24 | 1 |
| vertical_top_down | 42 / 7 / 10 | 85,7 % | 80,8 % | 33/60 | 22 |
| front | 7 / 6 / 5 | 53,8 % | 58,3 % | 6/12 | 2 |
| lower_side | 10 / 4 / 2 | 71,4 % | 83,3 % | 9/12 | 2 |

C3 hat auf den senkrechten Fotos weniger physische FP, aber dort andere Stellungen
und mehr Farb-Enthaltungen. Kein allgemeiner Kamerawinkel-Nachweis. Für den
vergleichbaren Fall1/Fall2-Readout: senkrecht 14/24, schräg 67/96; kein Vorteil
der senkrechten Aufnahme bei vollständigen Farbwerten. V2-Views bleiben unbekannt,
statt aus Aufnahmereihenfolge einen Kamerawinkel zu erfinden.

## Begründete Kombinationen

Entscheidung ausschließlich anhand der unabhängigen Einzelmetriken auf alter
Calibration und der vorher im Plan festgelegten Gates:

| Versuch | Entscheidung |
|---|---|
| K1 Shape→Farbe | Kein formaler Ausbau: C1 Recall/Precision unter .25. Der diagnostische Readout liefert ohnehin null. |
| K2 ECC→Shape | Ausgelassen: C2b-F1 verbessert C2a-F1 nicht, obwohl die ECC-Korrelation steigt. |
| K3 RPCA→Shape | Ausgelassen: C3 hat Recall, aber C1 erreicht die verlangte Bestätigungs-Precision .30 nicht. |
| K4 A+C3 | Durchgeführt: sechs komplementäre farbige Calibration-Treffer und physische Recall .941 erfüllen die Gates. |

K4-Regel ohne Gewichtungsoptimierung:

```text
A sicher = vorhandener High/Medium-Wert der unveränderten Confidence
X sehr sicher = C3-Score > 2*Occupancygrenze,
                CoreColor >= .15, Farbdominanz >= 2
beide sicher, aber Konflikt -> null
sonst A sicher             -> A
sonst X sehr sicher        -> X
sonst                      -> null
```

"Sicher" bezeichnet hier eine experimentelle Regel, keine belegte Wahrscheinlichkeit.
Insbesondere gilt die alte A-Confidence auf V3 bereits nachweislich nicht fehlerfrei.

| Satz | A roh richtig | A High/Medium richtig / Ausgaben | K4 richtig / vorhandene Werte | K4 null | K4 Phantomwerte | Accuracy aller K4-Ausgaben inkl. Phantome |
|---|---:|---|---:|---:|---:|---:|
| Calibration | 94/123 | 61/61 | 86/123 | 32 | 2 | 86/93 = 92,5 % |
| Alter V2-Vergleich | 45/60 | 23/23 | 31/60 | 27 | 1 | 31/34 = 91,2 % |
| V3-Vergleich | 103/156 | 61/75 | 96/156 | 41 | 0 | 96/115 = 83,5 % |

K4 erweitert die Ausgabe gegenüber A-High/Medium, übernimmt dabei zusätzliche
Fehler und verfehlt A bei der Roh-Treffermenge. Auf V3 sind 19 der 115 ausgegebenen
Werte falsch. **Diese Regel verwerfen, keine produktive Übernahme.** Nicht nach
V3 nachjustieren. C3 bleibt wegen physischer Recall und realer komplementärer
Treffer ein Kandidat für ein später separat entwickeltes A+X-Ensemble.

## Fehlertypen und Diagnoseartefakte

Sechs kleine Diagnosegruppen: alte V2-Fotos 4268/4272/4275 sowie V3 Fall1Vorne,
Fall3Senkrecht, Fall5Senkrecht. Insgesamt 44 PNGs, keine tausenden Exporte.
Je Bild enthält `decisions.json` GT, A, neue Ausgaben und Top-k; PNG-Header nennen
GT und tatsächlich ausgegebenes Feld. Links registrierter Grauausschnitt mit
Polygon; Mitte Kanten/Census-/Sparse-Karte; rechts Templatefläche bzw. geänderte
Komponenten vor Kontaktfilter. Bei C1 zusätzlich gefundene Kontur und roter Center.
Weiße Pixel im rechten Panel allein bedeuten ausdrücklich noch keine positive
Feldentscheidung; vollständiger Score/Candidate steht im JSON.

- [C1, Shape am erhöhten blauen Artenschutzstein](Diagnostics/v3_reused/Fall1Vorne/C1-conservation-8.png):
  geometrisches Template trifft nicht verlässlich den sichtbaren Steinkörper.
- [C2a derselben Szene](Diagnostics/v3_reused/Fall1Vorne/C2a-conservation-8.png):
  Ordnungsänderungen vorhanden, aber zu wenig zusammenhängende Kontaktevidenz.
- [C3 bei gemeinsamem Ruf-Feld 7](Diagnostics/v3_reused/Fall3Senkrecht/C3-reputation-7.png):
  Sparse-Anteil erfasst Belegung, Readout gibt Blau 7 aus und hält Schwarz zurück.
  Ein Feld kann mehrere Farben tragen; die Einfarben-Dominanzregel reicht dann nicht.
- [C3 bei benachbartem Material auf Artenschutz 23](Diagnostics/v3_reused/Fall5Senkrecht/C3-conservation-23.png):
  Sparse-Karte enthält Stein-/Umgebungsänderungen, trotzdem kein zuverlässiger
  blauer/schwarzer Wert. Sichtbare Anomalie ist keine garantierte Fuß-/Farbzuordnung.

Wichtigste C3-Fehler: breite Licht-/Druckabweichungen und benachbarte Steinanteile
erzeugen zusätzliche Attraktivitätsfelder; 16×16-Auflösung und Interpolation
verlieren feine Kontaktgrenzen; gemischte Farben und schwache schwarze CoreColor
werden durch ein reines Felddominanz-Readout nicht gelöst. Die Sparse-Methode
unterscheidet außerdem ohne Semantik nicht sicher Stein, Schatten und anderes Material.

Ein synthetischer Test setzt den gesamten Außenbereich eines Ruf-Polygons auf
Anomaly=1: exakt null Occupancy. Das belegt die räumliche Ausschlussinvariante.
Es gibt keine nachträglich erfundenen Instanz-/Kontaktlabels für reale Near-Objects.
Der Datensatz erlaubt keinen belastbaren Nachweis beliebiger loser Karten unter
der Rufleiste; Boarddruck wird nicht als lose Karte annotiert. Die vorhandenen
FP und Nachbarprobleme verbieten eine pauschale Behauptung zuverlässiger realer
Near-Object-Unterdrückung trotz verbesserter Recall.

## Laufzeit und Speicher

Release, Windows x64, vorhandenes .NET-/OpenCvSharp-Setup; grobe Desktop-Messung.

| Stufe | Calibration ms/Bild | Alter V2-Vergleich | V3-Vergleich |
|---|---:|---:|---:|
| C1 | 407 | 430 | 399 |
| C2a | 150 | 165 | 153 |
| C2b inklusive Track-ECC | 575 | 550 | 584 |
| C3 | 115 | 128 | 118 |

Zeiten schließen den jeweiligen Detektorschritt ein, C2a/C3 zusätzlich den
gemeinsamen Raw-Featurepfad, C2b seine ECC und drei separaten Track-Featurepfade.
C3 teilt derzeit einen Vorverarbeitungspfad mit Census; seine Zeit enthält daher
auch ungenutzte Census-Extraktion. Ausgewählte Diagnoseexemplare enthalten PNG-I/O.
Gemeinsames A-Decoding/Registrierung, vorbereiteter Framecache und A-Farbmerkmale
sind nicht in diesen Zusatzzeiten enthalten. K4 ist kein separat gemessener
End-to-end-Mobile-Benchmark.

Alle Normalreferenzen/ECC/Features laden: rund 6,6 s Initialisierung;
klassischer Fit rund 14,1 s. Prozessweiter Peak beim Vergleich bis etwa 742 MiB,
einschließlich mehrerer Methoden, Caches und Diagnosen. Der reine dichte C3-
Mittelwert/Unterraum enthält ungefähr 1,34 MiB Double-Daten; JSON-Modell einschließlich
Grenzen rund 5,6 MB. Keine neuronalen Modellspeicher. Grundsätzlich ist C3 als
kleiner klassischer Zusatzschritt plausibel; mobile Bibliothek, RAM und Gerätezeit
sind damit nicht nachgewiesen.

## Empfehlung und neuer echter Holdout

1. **C1 in dieser geometrischen Konfiguration verwerfen.** Keine Schwellenjagd,
   kein GHT-/Shape-Ensemble ohne zunächst fachlich bestätigte Markerprojektion.
2. **C2a/C2b in dieser Konfiguration verwerfen.** ECC hat messbaren Bildähnlichkeits-
   nutzen, aber keinen Occupancy-Nutzen; kein Grund für ECC→Shape in diesem Batch.
3. **C3 weiter untersuchen und als Kandidat für späteres A+X-Ensemble behalten.**
   Besonders Artenschutz liefert sinnvolle unabhängige Occupancy. Der spätere
   Farb-/Fuß-Readout muss Mischbelegung, Schwarz und Attraktivitäts-FP adressieren.
4. **Aktuelles K4 verwerfen.** Oracle-Potenzial ist keine implementierbare Auswahlregel.
   Keine neue Confidence, keine produktive Fusion oder automatische Wertungsübernahme.

Für eine neue Entwicklung: Parameter-/Formauswahl nur auf Calibration/Normalen,
dann einen wirklich neuen belegten Satz vor jeder GT-Metrik versiegeln. Benötigt:
mehrere neue Stellungen mit identischen klar benannten Views je Stellung;
Schwarz/Blau auf Artenschutz und Attraktivität, schwache Farben, gemeinsame und
benachbarte Felder, Grenzwerte und echte manuell gültige 0-Stellungen. Zusätzlich
neue Empty-Referenzen unter verschiedenen Lichtverhältnissen und reale dokumentierte
Near-Object-Negative: Karten/Plättchen dicht unter Ruf, aber ohne Berührung, plus
Kontrollen mit tatsächlichem Kontakt. Physisches Sockelfeld und Spielerfarbe
unabhängig dokumentieren, möglichst mit einer zusätzlichen senkrechten Referenzaufnahme.
Neue Geräte/Beleuchtung getrennt ausweisen. V3 nicht zur Feinoptimierung weiterverwenden.

## Reproduktion und Verifikation

Vom Repository-Root, mit den vorhandenen V2-Assets und dem im vorherigen Auftrag
autorisiert entpackten `artifacts/ResearchV3/dataset_v3`:

```powershell
dotnet run --project tools/Games/ArkNova/ClassicCvResearch -c Release -- self-test .
# Einmalige Batchphasen; vorhandene Caches/Freezes/Latches nicht überschreiben:
# prepare . -> fit . -> calibrate . -> evaluate .
& tools/Games/ArkNova/PhotoRecognition/Reports/ClassicCvResearchV4/Summarize.ps1 -RepositoryRoot (Get-Location).Path
```

`prepare` benutzt nur die vorhandene A-Registrierung und hält A-Ausnahmen als
Fehler fest. `fit` benötigt ausschließlich Empty-Fotos. `calibrate` berechnet
erst die Einzelmetriken, wählt Kombinationen und schreibt den Freeze. `evaluate`
prüft diesen vor Zugriff und vergleicht nur die zuvor ausgewählten Regeln.
Für einen neuen Batch separate Pfade/Version verwenden; Latches nicht löschen,
um weitere Parameterläufe als neue Validierung auszugeben.

Sechs native Forschungschecks: monotone Census-Invarianz, lokalisierter Sparse-
Anteil, keine 0/unsichtbaren/unbelegten Kandidaten, Außenanomalie ohne Kontakt,
Shape-Center gegen kantenfreies Negativ, ECC auf bekannter (+2,−1)-Translation.
Build-/Tests und finaler Quell-/Inputnachweis stehen in [verification.json](verification.json).

Lokaler Stand bleibt ungestaged/uncommittet auf master. Keine App-/Kamera-/Persistenz-
oder Scoringintegration. Nur die laut Repositoryrichtlinie verpflichtende Versions-
metadaten-Erhöhung betrifft die App-Projektdatei; Produktionslogik und A bleiben unverändert.
