# Research Batch V3: Struktur-/Referenzevidenz gegen Color/Occupancy

**Die untersuchten B1-Varianten rechtfertigen kein A+B-Ensemble.** Der vorab
ausgewählte gepoolte Strukturvergleich ergänzt A auf V3 genau einmal. Oracle-Top1
steigt lediglich von **103/156 auf 104/156**. Seine physische Belegungs-Recall ist
**2/148 = 1,35 %**. Die vielen Enthaltungen sind kein Nachweis robuster Erkennung.
DINOv3 konnte innerhalb der autorisierten Datengrenze nicht ausgeführt werden;
ein Vergleich DINOv3 gegen B1 bleibt offen. B3 wurde vor V3 begründet ausgelassen.

## Basis, Quellen und Integrität

- Repository `Gartenzaun2/Boardgame-Tracker`, sauberer `master` bei
  `299a6897fa5ee9fd6fafcc8dd10fc63cb8e94841`; nach `git fetch origin master`
  identisch mit `origin/master`. Lokaler Arbeitsordner: `BoardgameTracker`.
- Repository-Dokumentation ist kanonisch. Das ausdrücklich freigegebene
  `ARK_NOVA_CODEX_RESEARCH_BATCH_V3.zip` wurde nur als Forschungsinput benutzt.
  Der darin erwähnte andere ZIP-Dateiname ist keine zusätzliche Datenquelle.
- [Herkunft und Archivhash](input-provenance.json). Ausgepackt nach
  `artifacts/ResearchV3`; keine anderen externen Verzeichnisse, Modellcaches oder
  Modelle wurden gesucht oder gelesen. Keine Web-/Modellrecherche.
- Alle 23 verkleinerten Bilddateien stimmen mit `scaled_sha256` des Manifests
  überein: zehn Empty-Referenzen, 13 belegte Fall-Fotos. Alle registrieren
  erfolgreich mit der bestehenden SIFT-/Homographie-Pipeline.
- A verwendet das ausgewählte Ranking `A_NoZero` und sekundär die unveränderte
  Confidence E. Farbmasken, Scoreformeln, Geometrie, Registrierungsparameter,
  Ground Truth und bestehende Confidence bleiben unverändert.
- Die einzigen Ergänzungen an bestehenden CV-Klassen sind lesende Observer und
  die Freigabe des bereits berechneten Registrierungsdiagnostik-Records.
  Sie liefern keine Evidenz an A zurück. Das Forschungsprogramm ist das separate
  Entwicklungsprojekt [StructureResearch](../../../StructureResearch/).

## Auswahl und Holdout-Disziplin

V3-Fallbilder wurden nicht für Thresholds, Gewichte, Patchgrößen, Featureauswahl,
Anomaly-Parameter oder die Methodenauswahl benutzt. Vor Freeze wurde die V3-Ground
Truth ausschließlich für Schema-/Integritätsprüfung geöffnet; dabei war auch ein
Dateipräfix mit ersten Labels sichtbar. Es wurden daraus keine Auswahlmetriken
berechnet. Fall-4-Blau ist unverändert **52;21;15**.

1. A auf dem alten Calibration-/Holdout-Split reproduziert; bestehende Freeze- und
   Dataset-Checks bestanden. Kein V3-Fallfoto wurde dafür ausgewertet.
2. B1-Schwellen ausschließlich mit Empty-Bildern bestimmt. Zwei fest definierte
   Referenzvarianten, eine Patchgröße, keine Grid-Suche.
3. Auf den 17 alten V2-Calibration-Fotos verglichen. B1_pool anhand physischer
   Belegungs-F1 ausgewählt. Alte Holdout-Ergebnisse dienten nur A-Reproduktion.
4. B3 vor V3 verworfen: vorab implementiertes Gate verlangt mindestens einen
   komplementären Kontakt-Treffer und mehr als 25 % physische Recall auf alter
   Calibration. B1_pool erreicht nur 12,6 %. B1_view findet dort keine Belegung.
5. [Original-Freeze](freeze.json) speichert Quellen, Config, Normalcaches,
   Calibration-Berichte und Eingabehashes vor dem ersten V3-Lauf.

### Technischer Abbruch und unveränderte Wiederaufnahme

Es gab **einen abgebrochenen Versuch und eine vollständige Auswertung**, nicht
einen störungsfreien einmaligen Prozesslauf. Der erste Versuch berechnete zehn
Fotos, brach dann in A bei `Fall3Senkrecht.jpg` ab. OpenCV liefert auf Ruf-Feld 3
`coreDifference = 1.0000000000000002`; die bestehende strenge Bereichsprüfung in A
wirft `ArgumentOutOfRangeException`. Das ist ein numerischer Randfall, keine
unzureichende Registrierung. Er wurde **nicht** durch Clamping oder Änderungen
an A korrigiert.

[Abbruchprotokoll](interrupted-run.json), Original-Freeze und ursprünglicher
Run-Latch bleiben erhalten. Ausschließlich Observer-Plumbing und Fehlerzählung
im Forschungsprogramm wurden ergänzt: B erhält den registrierten Frame bereits
vor A-Scoring, A-Ausnahmen werden als Ausfälle gezählt. Der
[Recovery-Freeze](recovery-freeze.json) verweist auf den Original-Freeze und prüft
jeden ursprünglichen Hash außerhalb dieser explizit benannten Plumbing-Dateien.
Insbesondere `StructureDetector.cs`, `model.json`, alle Referenzen, A-Analyzer,
Rankingregeln, Confidence und Feldscoreformeln sind gegenüber dem Original-Freeze
identisch. Keine Parameter wurden anhand des Teil-Holdouts geändert.

Die zehn wiederholten A-Ergebnisse sind identisch zum Abbruchprotokoll:
`6,8,7,6,7,8,12,9,9,10` richtige Werte. Die vollständige Wiederaufnahme ist eine
technische Reproduktion desselben eingefrorenen Experiments, keine zweite
unabhängige Validierung. Die Vorgabe eines einzigen ungestörten Holdout-Aufrufs
konnte wegen dieses Fehlers nicht eingehalten werden.

Der finale Bericht zählt alle zwölf Werte des A-Ausfalls als falsch/Enthaltung.
Ein fehlendes Ergebnis wird niemals aus dem Nenner entfernt. Beide Run-Latches
verweigern erneuten Aufruf mit `FileMode.CreateNew`. Alle 101 Hashes des
Recovery-Freeze wurden am Ende nochmals geprüft; siehe
[abgeleitete Diagnostik](derived-diagnostics.json).

Die alten historischen `ranking-selection.json`-Hashes wurden nicht umgeschrieben.
Der zusätzliche Observer in `ArkNovaRankingCapture.cs` verändert dessen Dateihash;
ein erneuter historischer `ranking ... holdout`-Aufruf verweigert deshalb unter
diesem Arbeitsstand erwartungsgemäß die Auswertung. Die vorher reproduzierten
Berichte bleiben vollständig erhalten. Das ist keine neue historische Auswahl.

## Was B1 misst

Eine Feldhypothese ist zunächst **farbunabhängige physische Occupancy**.
B1 verwendet weder A-Farbmasken noch A-Difference-Maske. Es gibt keine Fusion.

- Bestehende Geometrie in Analyseauflösung 2000×1400. Polygon und um 12 % der
  kleineren Feldkante erodierter Core; konstanter Nullrand bei Erosion.
- Kontextmargin `min(12, ceil(0.20 * min(fieldWidth, fieldHeight)))` Pixel.
  Keine lokale Nachregistrierung, kein ECC/Wireframe.
- Graustufe in [0,1], Gaussian 3×3/Sigma 1, lokale Beleuchtungskorrektur
  `(gray - Gaussian(gray, sigma=16)) / 0.15`.
- Sobel x/y, Kernel 3, Skalierung 1/8. Residual als kleinster absoluter
  normalisierter Grauwertabstand; Gradientenänderung als kleinster euklidischer
  x/y-Abstand zu sichtbaren Empty-Referenzen derselben Pixelposition. Das erfasst
  neue und verschwundene Referenzkanten gemeinsam, ohne separate Kantensemantik.
- B1_pool benutzt alle sichtbaren Empty-Referenzen. B1_view benutzt, falls
  vorhanden, nur dieselbe View-Kategorie; sonst den ganzen Pool. Der nächste
  Normalvergleich wird pro Pixel/Feature gewählt, kein einziges Masterfoto.
- Pixelgrenzen: 99. Perzentil der Leave-one-image-out-Empty-Distanzen je Track,
  feste Floors 0,45 für Residual und 0,25 für Gradient. Jedes siebte Polygonpixel
  wird zur Quantilschätzung benutzt. Abweichung verlangt Residual über Grenze
  und Gradient über Grenze oder Residual über doppelter Grenze.
- Opening und Closing mit 3×3-Ellipse. Komponenten müssen mindestens 3×3 groß
  sein, mindestens 20 % Boundingbox-Füllung sowie mindestens max(6, 1,5 % der
  Polygonfläche) Pixel besitzen. Dünne Zahlen-/Kantenreste werden unterdrückt.
- **Contact-Gate:** mindestens max(4, 3 % Polygonfläche) Kontaktpixel und
  max(4, 1,5 % Corefläche) Corepixel innerhalb derselben Komponente.
  Außenpixel gehen niemals in den Occupancy-Score ein. Score ist der Mittelwert
  aus akzeptiertem Polygon- und Coreanteil. Mindestens 97 % Feldsichtbarkeit.
- Scoregrenze: max(0,06, 99,5. Perzentil der Empty-LOO-Kontaktscores).
  Alle drei Tracks bleiben bei 0,06. Automatische Kandidaten schließen 0 aus;
  fehlende Kontaktevidenz bleibt null, ohne Snapping auf einen Ersatzwert.

Die gepoolten Pixelgrenzen Residual/Gradient betragen für Attraktivität
0,5595/0,3554, Artenschutz 0,5867/0,3968, Ruf 0,6070/0,4448. Bei B1_view sind sie
1,0847/0,5900, 1,1818/0,6739 und 1,2300/0,7415. Nur zwei Normale je bekannter View
erzeugen deutlich größere LOO-Residuen als der Pool mit neun anderen Bildern.
Auf V2 ist die View unbekannt: dort verwendet B1_view den Pool-Fallback, behält
aber seine normalbasierten höheren Grenzen. Das begrenzt den Variantenvergleich.
`lower_side` besitzt auch auf V3 keine Empty-Referenz und verwendet den Fallback.

Pro Variante werden 1680 sichtbare unterstützte Empty-Feldbeobachtungen geprüft:
keine positive Belegung. Dies sind Fit-/LOO-Diagnosen derselben Normale und keine
unabhängige Spezifitätsvalidierung. Die minimale Normaldistanz, hohen Quantile und
Komponenten-/Corefilter können zusammen echte Steinänderungen wegfiltern. Die
gemessene geringe Recall bestätigt das Gesamtproblem; der Einzelbeitrag jedes
Filters wurde nicht nachträglich auf V3 ablationiert oder optimiert.

## B2 und B3: klare Grenzen

**B2 nicht ausgeführt.** Repository und autorisiertes Archiv enthalten keinen
DINOv3-Checkpoint, keine zugehörige lokale Feature-Extraktionsumgebung und keine
Modellreferenz mit Gewichtsdatei. Andere externe Modellquellen oder Caches waren
nicht freigegeben. Daher gibt es keinen DINOv3-Backbone-/Layer-Freeze, keine
DINO-Heatmaps, Accuracy oder Laufzeit. Ein HOG-/SIFT- oder untrainiertes Modell
wurde nicht als DINOv3 ausgegeben. Daraus folgt weder ein Vorteil noch ein
Nachteil von DINOv3. Für einen neuen Versuch werden ausdrücklich bereitgestellte
Gewichte und ein ausführbarer Extraktionspfad benötigt; V3 ist danach kein
frischer Methodenauswahl-Holdout mehr.

**B3 vor V3 ausgelassen.** Die alte Calibration zeigt zu wenig trennbare
Strukturevidenz für den zusätzlichen Versuch. Das Forschungsprogramm enthält
einen nicht gefitteten PaDiM-inspirierten diagonalen Gaussian-Zweig auf den drei
klassischen Merkmalen, kein validiertes Deep-PaDiM und kein PatchCore-Ergebnis.
Das `fit-b3`-Gate verweigert ihn für diesen Batch. Es wurden keine B3-Parameter
auf Fall-Bildern gewählt. Über tatsächlichen Zusatznutzen von PaDiM/PatchCore
kann dieser Batch keine positive oder negative Messbehauptung liefern.

## Alte Calibration und alter Holdout – getrennte Reproduktion

| Lauf | Calibration, 123 vorhandene Werte | Alter Holdout, 60 Werte |
|---|---:|---:|
| Historische Farbbaseline vor CV-0-Ausschluss | 93 | 42 |
| Aktuelles A, ausgegebene Rohwerte | 94 | 45 |
| A roh null bei vorhandenen Werten | 17 | 7 |
| A akzeptiert nach unveränderter Confidence | 61 | 23 |
| Davon richtig | 61 | 23 |
| Akzeptierte Phantomwerte | 0 | 0 |

Quellen: [alte Calibration](a-old-calibration.json),
[alter Holdout](a-old-holdout.json). Keine erneute Auswahl anhand des alten Holdouts.

| B1 auf alter Calibration | Pool | View/Fallback |
|---|---:|---:|
| Top1 / Top2 / Top3, je 123 | 8 / 14 / 15 | 0 / 0 / 0 |
| Null bei vorhandenen Werten | 76 | 123 |
| Physische TP / FP / FN, eindeutige Felder | 15 / 7 / 104 | 0 / 0 / 119 |
| Physische Precision / Recall | 68,2 % / 12,6 % | 0 % / 0 % |
| A falsch / B Top1 richtig | 0 | 0 |
| A falsch / B Kontaktmenge enthält GT | 3 | 0 |
| Top1-Oracle A ODER B | 94/123 | 94/123 |

[Vollständiger B1-Calibration-Bericht](b1-calibration.json). Bereits hier ist B1
kein überzeugender Nachweis eines unabhängigen zweiten Signalwegs.

## V3-Metrikdefinition

Alle 13 Fotos enthalten laut GT vier Farben × drei Tracks: **156 Farbwerte**.
Top1 zählt den tatsächlich ausgegebenen Wert nach Mindest-Evidenz/Enthaltung.
Top2/Top3 zählen Aufnahme des richtigen Feldes in die jeweilige Kandidatenliste;
bei A ist diese Liste auch bei Enthaltung diagnostisch vorhanden.
Bei B kommen nur Felder oberhalb der eingefrorenen Occupancy-Grenze in die Liste.
Eine Enthaltung zählt nicht als korrekter Wert.

B bekommt keine Farbe und keine Spieleranzahl als Detektorinput. Für die verlangte
Farbgruppierung wird **dieselbe** Trackrangfolge unverändert gegen jede vorhandene
GT-Farbe geprüft. Bei mehreren Steinen kann ein farbunabhängiger Top1 dadurch
nicht alle Farben gleichzeitig treffen. Das ist eine Retrieval-Diagnose, keine
behauptete Farberkennung. Deshalb zusätzlich physische TP/FP/FN über eindeutige
Scorefelder: mehrere Farben auf demselben Feld zählen dort einmal. V3 hat 148
solche belegten Felder, keines mit Score 0. Auch diese angemessenere Metrik zeigt
die sehr geringe Recall von B1. Mangels absent-Farben ist Phantom-Farbspezifität
auf V3 nicht messbar; Nullzählungen sind keine Score-0-Labels.

## V3-Ergebnisse

| Verfahren | Top1 | Top2 | Top3 | Enthaltung |
|---|---:|---:|---:|---:|
| A | 103/156 (66,0 %) | 115/156 | 116/156 | 24/156 |
| B1_pool, vorab ausgewählt | 2/156 | 2/156 | 2/156 | 144/156 |
| B1_view, eingefrorene Kontrollvariante | 0/156 | 0/156 | 0/156 | 152/156 |
| B2 DINOv3 | nicht verfügbar | – | – | – |
| B3 | vor V3 ausgelassen | – | – | – |

A fällt in einem von 13 Bildern aus; alle zwölf Werte bleiben im Nenner.
Nur unter Ausschluss dieses Fehlerbildes wären es 103/144 = 71,5 %; diese
bedingte Zahl ersetzt nicht das Hauptergebnis. A-Confidence akzeptiert 75/156
(48,1 %), davon **61/75 richtig (81,3 %)**. High: 35/43 richtig, acht falsch;
Medium: 26/32 richtig, sechs falsch. Die frühere V2-Sicherheit generalisiert
auf diese neuen Fotos nicht. Die Confidence wurde ausdrücklich nicht angepasst.

| Physische Belegungsmenge | TP | FP | FN | Precision | Recall |
|---|---:|---:|---:|---:|---:|
| B1_pool | 2 | 2 | 146 | 50,0 % | 1,35 % |
| B1_view | 0 | 1 | 148 | 0 % | 0 % |

### Komplementarität und Oracle

| B-Variante | A richtig / B richtig | A richtig / B falsch | A falsch / B richtig | A falsch / B falsch | A ODER B Top1 |
|---|---:|---:|---:|---:|---:|
| B1_pool | 1 | 102 | **1** | 52 | **104/156 (66,7 %)** |
| B1_view | 0 | 103 | **0** | 53 | 103/156 (66,0 %) |

Die Kontaktmengen-Oracle ergibt dieselben 104 bzw. 103 Treffer. Sie ist zusätzlich
optimistisch, weil eine wahre Farbzuordnung zu den Occupancy-Feldern vorausgesetzt
wird. Keine Oracle-Regel wurde als Ensemble implementiert.

Der einzige komplementäre Top1-Treffer ist **Fall1Vorne, Schwarz-Attraktivität**:
GT 17, A 113, B1_pool 17. Auf `Fall2Gegenueber` findet B1_pool Gelb-Attraktivität 61,
die A ebenfalls richtig erkennt. Schwarz-/Blau-Artenschutz und die schwierigen
senkrechten Fälle werden durch B1 nicht gerettet.

### Nach Track und Farbe

Jede Zelle zeigt Top1 / Top2 / Top3 / Enthaltung. Vollständige Einzelentscheidungen,
Feldscores, Top-3-Scores und alle Gruppen stehen in [holdout.json](holdout.json).

| Track, je 52 Werte | A | B1_pool | B1_view |
|---|---|---|---|
| Attraktivität | 30 / 32 / 33 / 9 | 2 / 2 / 2 / 40 | 0 / 0 / 0 / 52 |
| Artenschutz | 37 / 43 / 43 / 7 | 0 / 0 / 0 / 52 | 0 / 0 / 0 / 48 |
| Ruf | 36 / 40 / 40 / 8 | 0 / 0 / 0 / 52 | 0 / 0 / 0 / 52 |

| Farbe, je 39 Werte | A | B1_pool | B1_view |
|---|---|---|---|
| Rot | 30 / 31 / 31 / 3 | 0 / 0 / 0 / 36 | 0 / 0 / 0 / 38 |
| Blau | 27 / 30 / 30 / 3 | 0 / 0 / 0 / 36 | 0 / 0 / 0 / 38 |
| Gelb | 30 / 31 / 31 / 3 | 1 / 1 / 1 / 36 | 0 / 0 / 0 / 38 |
| Schwarz | 16 / 23 / 24 / 15 | 1 / 1 / 1 / 36 | 0 / 0 / 0 / 38 |

### Blickrichtungen

Jede Verfahrenszelle wieder Top1 / Top2 / Top3 / Enthaltung.

| View | Werte | A | B1_pool | B1_view |
|---|---:|---|---|---|
| opposite_side | 24 | 14 / 19 / 20 / 5 | 1 / 1 / 1 / 20 | 0 / 0 / 0 / 24 |
| left | 24 | 20 / 20 / 20 / 0 | 0 / 0 / 0 / 24 | 0 / 0 / 0 / 24 |
| right | 24 | 16 / 20 / 20 / 0 | 0 / 0 / 0 / 24 | 0 / 0 / 0 / 24 |
| vertical_top_down | 60 | 36 / 37 / 37 / 17 | 0 / 0 / 0 / 60 | 0 / 0 / 0 / 56 |
| front | 12 | 7 / 7 / 7 / 0 | 1 / 1 / 1 / 8 | 0 / 0 / 0 / 12 |
| lower_side | 12 | 10 / 12 / 12 / 2 | 0 / 0 / 0 / 8 | 0 / 0 / 0 / 12 |

Senkrechte Fotos sind **hier nicht messbar besser**: A 36/60 = 60,0 % gegenüber
67/96 = 69,8 % schräg. Das vermischt unterschiedliche Stellungen und enthält den
numerischen A-Ausfall. Vergleichbarer sind nur Fall1/Fall2, die mehrere Views
derselben Stellung besitzen: senkrecht 15/24 = 62,5 %, schräg 67/96 = 69,8 %.
Fall1 senkrecht 6/12 versus schräg 28/48; Fall2 senkrecht 9/12 versus schräg 39/48.
Auch dort kein Vorteil. Das sind zwei Stellungen mit wiederholten Fotos, keine
unabhängigen 120 Partien und kein statistischer Nachweis eines optimalen Winkels.
Ohne das Fehlerbild sind alle senkrechten Aufnahmen zusammen 36/48 = 75 %;
dieser andere Fallmix erlaubt ebenfalls keine allgemeine Empfehlung.

## Kontakt und Near-Objects

Die synthetischen Tests prüfen eine große zusammenhängende Änderung von 1080
Pixeln direkt neben einem 20×20-Polygon: exakt null Occupancy. Eine zusätzliche
10×10-Komponente im Core wird erkannt; eine ein Pixel dünne Linie nicht.
Dieser Test bestätigt die räumliche Invariante im Code, keine reale Objektklasse.

Ein reales repräsentatives Beispiel ist
[Fall3 Ruf 8: Kontext und Polygone](Diagnostics/Fall3Senkrecht/reputation-8-B1_pool.context.png)
mit dem Material links beim belegten Ruf-Feld 7. Die unveränderte GT nennt dort
Blau/Schwarz 7 und Rot/Gelb 3, keinen Stein auf 8.
[Residual, Gradienten, Komponenten und Gate](Diagnostics/Fall3Senkrecht/reputation-8-B1_pool.png)
zeigen für Feld 8 Score 0; fünf Pixel einer vollständig außerhalb liegenden
Komponente werden ignoriert. Im selben Bild verfehlt B1 aber auch das echte Feld 7.

Weitere dokumentierte Außenkomponenten: Fall1Vorne Ruf 2: 330 ignorierte Pixel,
Fall4Senkrecht Ruf 2: 250; jeweils Score 0. **Diese beiden Felder sind laut GT
tatsächlich belegt.** Ihre Ablehnung ist ausdrücklich kein Near-Object-Erfolg,
sondern ein Beispiel dafür, dass ein strenges Gate auch Steinbelegung verfehlt.

In den visuell untersuchten repräsentativen Original-/Warpfotos ist kein separater,
eindeutig abgegrenzter Karten-Negativpool dokumentiert. Die großen gelben Flächen
unter dem Ruftrack sind Boarddruck; sie wurden nicht als lose Karten annotiert.
Es wurden keine neuen Objektlabels oder Kontakt-Ground-Truth-Konturen erfunden.
Die Kombination aus einem Nachbarfeldbeispiel, synthetischem Gate-Test und sehr
vielen Enthaltungen genügt **nicht**, um zuverlässiges Ignorieren beliebiger naher
Karten/Plättchen bei gleichzeitig robuster Stein-Recall zu behaupten.

## Diagnoseartefakte und Laufzeit

Sechs fest begrenzte Bildgruppen enthalten jeweils Original-Warp, kleine
Referenz-/Fotopatches, Polygon/Core, Residual, Gradientenänderung, Komponenten,
Contact-Gate und ignorierte Außenpixel sowie einen größeren Kontextausschnitt.
Schwarz/Blau, alle Tracks und die zusätzlichen senkrechten Stellungen sind
vertreten. Der Referenzpanel zeigt das erste Poolbild zur Orientierung; die
eigentliche Distanz benutzt mehrere pixelweise Normalreferenzen und entspricht
deshalb nicht immer der einfachen Differenz dieses einen Panels.

- [Komplementärer Schwarz-Attraktivität-Treffer](Diagnostics/Fall1Vorne/attractiveness-17-B1_pool.png)
- [Fall3, A numerisch ausgefallen](Diagnostics/Fall3Senkrecht/registered.png)
- [Fall4](Diagnostics/Fall4Senkrecht/registered.png)
- [Fall5](Diagnostics/Fall5Senkrecht/registered.png)

Pro Bild/Track stehen alle 171 geometrischen Feldmessungen einschließlich
diagnostischer Nullfelder im JSON; automatische B-Kandidaten enthalten keine 0.
Es wurden 100 PNGs einschließlich Normalcaches und Diagnosekontexten exportiert,
keine tausende Einzelbilder. DINO-/B3-Heatmaps existieren nicht.

Grobe Messung im Release-Forschungslauf, ohne Mobile-Optimierung:

| Stufe | Messung |
|---|---|
| Initialisierung A + Geometry + zehn gecachte Normale/Features | 2,41 s |
| B-Featureextraktion pro Bild | ca. 160–178 ms |
| B1_pool Vergleich/Kontakt je Bild | im Mittel 805 ms |
| B1_view Vergleich/Kontakt je Bild | im Mittel 325 ms |
| B1_pool / B1_view je geometrischem Feld | ca. 4,71 / 1,90 ms |
| Empty-LOO-Fit Pool / View | 13,73 / 3,72 s |
| Prozessweiter Peak Working Set | 1005273088 Bytes, ca. 959 MiB |

A-Stufenzeiten im gemeinsamen Lauf enthalten Ranking-Capture und den lesenden
B-Feature-Observer und sind deshalb kein isolierter A-Performancebenchmark.
PNG-Diagnoseexport ist nicht in den ausgewiesenen B-Vergleichszeiten enthalten.
Der Peak umfasst mehrere Methoden, Referenzen und Diagnoseausgabe; keine isolierte
Modellgröße. B1 hat keine neuronalen Gewichte; zehn Float-Featurepools sind sein
Normalmodell. Algorithmisch grundsätzlich mobile-plausibel, aber die aktuelle
Windows-OpenCvSharp-Pipeline und der Speicherbedarf sind nicht mobil validiert.
Zu DINOv3/B3 wurden weder Laufzeit noch Modellspeicher gemessen.

## Entscheidung und nächster fachlicher Schritt

1. **B1 komplementär?** Minimal: ein zusätzlicher V3-Top1-Treffer. Keine hinreichende
   robuste Zusatzinformation über A hinaus; insbesondere keine Artenschutzrettung.
2. **DINOv3 besser/komplementärer?** Offen, technisch nicht ausgeführt. B1-Misserfolg
   ist kein Gegenbeweis gegen DINOv3.
3. **PaDiM/PatchCore weiterer Nutzen?** Nicht nachgewiesen. Ein teurerer Normality-
   Aufbau auf diesen schwach trennenden klassischen Features war vor V3 nicht
   gerechtfertigt. Nach einem besseren Feature-Nachweis erneut prüfbar.
4. **Robusteste physische Belegung?** Keiner der neuen Wege ist robust belegt;
   A liefert mehr richtige Scorewerte, hat aber Schwarzprobleme, falsche akzeptierte
   Werte und einen numerischen Totalausfall. B1 ist extrem unempfindlich.
5. **Near-Objects zuverlässig ignoriert?** Räumliches Ausschlussprinzip im Code
   belegt, reale End-to-end-Zuverlässigkeit mit akzeptabler Recall nicht belegt.
6. **Senkrecht besser?** Nein für diesen Datensatz, auch im Fall1/Fall2-Vergleich.
7. **Oracle-Potenzial?** A+B1_pool 104 statt 103 von 156, nur +0,64 Prozentpunkte.
8. **Ensemble jetzt?** Nein. Keine produktive Fusion, neue Confidence oder
   automatische Übernahme. Es gibt noch kein überzeugendes zweites Signalpaar.
9. **Danach?** Numerischen A-Randfall separat absichern; unabhängige Strukturfeatures
   mit ausdrücklich bereitgestellten DINOv3-Gewichten auf Calibration/Normalen
   untersuchen. Für eine spätere eingefrorene Methode neuen belegten Holdout und
   echte dokumentierte Near-Object-Negative bereitstellen. V3 nicht weiter tunen.

## Ausführung und Verifikation

Vom Repository-Root, nach Entpacken des ausdrücklich bereitgestellten Archivs
nach `artifacts/ResearchV3`:

```powershell
dotnet run --project tools/Games/ArkNova/StructureResearch -c Release -- self-test . artifacts/ResearchV3/dataset_v3 tools/Games/ArkNova/PhotoRecognition/Reports/ResearchV3
```

Der abgeschlossene Batch wird über seine JSONs und Hashes nachvollzogen.
Für ein unabhängiges neues Experiment in einem **neuen** Ausgabeordner:
`fit-b1`, danach `freeze`, danach `holdout` mit denselben drei Pfadargumenten.
Vor Freeze muss `input-provenance.json` vorhanden sein. `fit-b3` ist ausschließlich
vor Freeze und bei bestandenem Calibration-Gate zulässig. `recovery-freeze` und
`holdout-recovery` sind die dokumentierte einmalige technische Wiederaufnahme,
keine Methode zum Nachkalibrieren. Vorhandene Freezes/Latches niemals löschen,
um weitere V3-Parameterläufe zu ermöglichen.

Tatsächlich ausgeführt:

- Release-Build des separaten Forschungsprojekts: erfolgreich, zuletzt ohne Warnung.
- Fünf native Forschungschecks: Außenobjekt, echte Kontaktkomponente, dünne Linie,
  kein CV-0, kein unbelegter/unsichtbarer Ersatzkandidat: bestanden.
- `dotnet build BoardgameTracker.sln --no-restore`: 0 Warnungen, 0 Fehler;
  Android- und iOS-Simulator-Ziel im vorhandenen Entwicklungssetup gebaut.
- Core-Tests: **75/75**; Application-Tests: **175/175**, keine übersprungenen Tests.
- 101 finale Freeze-Hashes und erste zehn A-Reproduktionszahlen geprüft.
- App-Version gemäß Repositorypflicht: **0.10.0.12 / 49**.

Änderungen bleiben lokal, ungestaged und uncommittet auf `master`. Kein Commit,
kein Push, keine App-/Kamera-/Persistenzintegration. B2 bleibt eine ausdrücklich
offene Ausführungsgrenze; der ursprüngliche A-Zahlenfehler bleibt unverändert.
