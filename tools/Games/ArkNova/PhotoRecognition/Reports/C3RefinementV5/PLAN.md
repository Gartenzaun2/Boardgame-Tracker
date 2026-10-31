# C3 V5: begrenzter Abschlussversuch, vor neuen Datenmetriken

Basis master/origin/master `295022bd3f2dfe855f663e449c1b8059a0b887e9`.
Kein neuer Detektor, keine C1/C2-/ECC-/ML-Versuche. V4-SVD, 16×16, Rang 3,
Lambda .04 und acht Sparse-Schritte unverändert. Alte Calibration wählt;
Empty fitten; V2/V3 ausschließlich wiederverwendete Vergleiche.

## Drei aufeinander aufbauende Stände

1. P0: V4-C3 und V4-Feld-CoreColor-Readout unverändert reproduzieren.
2. P1: ausschließlich Attraktivität konservativer: je Feld Pixelgrenze
   max(V4-Trackgrenze, 99%-Quantil der eigenen Empty-LOO-Sparse-Werte im Polygon),
   Occupancygrenze max(V4-Trackgrenze, maximaler Empty-LOO-Feldscore).
   Artenschutz/Ruf bleiben exakt gleich. Keine Patch-/Rang-/Lambda-Suche.
   P1 nur wählen, wenn Attraktivitäts-FP auf Calibration um mindestens 25 % sinken
   und Recall höchstens fünf Prozentpunkte verliert; sonst P0-Occupancy behalten.
3. P2: auf ausgewählter Occupancy lokale Farbmerkmale nur in akzeptierten
   Sparse-Komponenten im Feld/Kern lesen. Bestehende A-Strict-Masken für die
   Farben; für Schwarz zusätzlich bestehendes strict-seeded Relaxed-Wachstum.
   Keine neue HSV-/LAB-Schwellenfamilie. Mehrere Farben pro Feld zulassen,
   sofern jede eigene Kontakt-/Corepixel und mindestens 10 % der Sparse-
   Kontaktfläche beiträgt (mindestens sechs Pixel; Schwarz mindestens drei
   eigene strenge Seeds). Keine pauschale Gewinnerfarbe pro Feld.
   Pro Farbe erst lokalen Coreanteil, dann Occupancy ordnen; knappe eigene
   Feldabstände als Mehrdeutigkeit diagnostizieren. Keine neue gewichtete Formel.

P2 nur wählen, wenn mindestens zwei zusätzliche schwarze oder gemeinsame-Feld-
Treffer entstehen, Gesamt-Top1 höchstens zwei Treffer sinkt und zusätzliche
Phantome höchstens zwei gegenüber dem zugehörigen alten Readout betragen.
Die Ablationsstände bleiben auch bei Ablehnung getrennt sichtbar.

## Zwei feste Fusionen

F1 (Bestätigung): A High/Medium ohne starken Widerspruch beibehalten; sonst nur
starkes C3 bei Übereinstimmung mit A-Rohwert akzeptieren; Konflikt -> null.

F2 (Track-Regel): derselbe sichere A-Zweig. Attraktivität nur bei Übereinstimmung,
sehr starker isolierter C3-Farbe und zusätzlicher Farbstützung. Artenschutz darf
starkes C3 mit eindeutigem Abstand übernehmen; bei gleichzeitig starkem
widersprechendem A-Rohwert Enthaltung. Ruf bei Übereinstimmung oder sehr starkem
C3 bei A-null und unabhängiger Farbstützung. Keine Meta-Gewichte.

Stark: Occupancy mindestens 1,5× Grenze (Attraktivität 2×), lokale Farbdichte .20,
lokaler Core-Farbanteil .06. Sehr stark: 2× Grenze, Dichte .35 und Coreanteil .12.
Isolation: Abstand des lokalen Core-Farbanteils zum nächsten Feld mindestens .03.
Farbstützung: mindestens ein anderer Track derselben Farbe mit A-CoreColor >=.15
oder starkem lokalem C3. Starker A-Rohwert: Score >=.80, Margin >=.20,
CoreColor >=.20. Alle Konstanten vor Calibration festgelegt.

Fusion auf Calibration nach Accuracy inklusive Phantomwerten wählen: Vorrang
für >=98 % Accuracy und null Phantome, dann richtige ausgegebene Werte; sonst
höchste Accuracy, dann Treffer. Coverage-Ziel >=75 % separat berichten.
Kein Zielerfolg durch Entfernen von Enthaltungen aus dem Nenner.

## Separat: technische A-Härtung

Maskenanteile sind mathematisch [0,1]. Nur numerische Überschreitungen bis 1e−12
werden begrenzt, echte ungültige Werte/NaN bleiben Fehler. Keine Änderung an
Scoringformeln, Masken, Ranking oder Confidence. Alte A-Zahlen erhalten und
gehärtetes A separat reporten. Alle sonst erfolgreichen A-Entscheidungen auf
Gleichheit prüfen. Der bekannte Fall zusätzlich durch echten Analyzer-Lauf testen.

Historische Quellen/Reports/Freezes nicht überschreiben; ausschließlich die
ausdrücklich erlaubten zwei Maskenmittelwert-Aufrufstellen technisch härten.
Neue Quellen, Modelle, Auswahl und Inputs vor V2/V3-Vergleich hashen. Wenige
gezielte Diagnosepatches; keine neue große Forschungsrunde.
