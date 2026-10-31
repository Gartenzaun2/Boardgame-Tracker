# FinalAcceptance / StressTest – Abschlussreport

**Erkennungsforschung ABGESCHLOSSEN / FROZEN.** Aktuelle Entscheidung und offener
mobiler Integrationsstand: [kanonischer Status](../../../../../../docs/ARK_NOVA_CV_STATUS.md).
Gehärtetes A bleibt Basis, C1/C2 verworfen, C3 research-only. Keine F1/F2-Fusion
oder C3-Warnregel produktiv. Kein neuer Parameter-/Confidence-/Ensembleversuch.

## Letztes Foto

Original `IMG_4263.jpeg`, vom Benutzer ausdrücklich angehängt. SHA-256:
`c38a74770bd2847ff19cb9fba7f11ac9bcadda8319086076775526c10cf717ec`.
[Manifest mit GT](manifest.json) wurde vor der ersten Analyse gespeichert.
Die Originaldatei wird nicht ins Git kopiert; kein klarer Nutzen als dauerhafte
Regression-Fixture nachgewiesen. Dies ist **ein zusätzlicher Stress-Test**, kein
statistisch ausreichender unabhängiger Holdout.

Genau ein Analyzer-Lauf mit gehärtetem A_NoZero und unveränderter Confidence E.
Zwei vorgelagerte Startversuche scheiterten vor Programmausführung an der Sandbox
(dotnet-Telemetrie/Registry, dann MSBuild); sie werteten kein Bild aus.
Der erfolgreiche Lauf wurde nicht wiederholt. [Vollständiges Ergebnis](result.json).

Registrierung erfolgreich: 1433 Matches, 1178 Inlier, p95 1,91475 px,
minimale Tracksichtbarkeit 98,42 %. Gesamtzeit ca. 991 ms, Windows-Referenz,
kein mobiler Performance-Nachweis.

| Farbe | Track | GT | A-Vorschlag nach E | Ergebnis | Confidence / Anzeige |
|---|---|---:|---:|---|---|
| Gelb | Attraktivität | 63 | 63 | korrekt | High / ✓ Sicher, Grün |
| Gelb | Artenschutz | 22 | 22 | korrekt | Uncertain / ? Unsicher, Rot |
| Gelb | Ruf | 11 | 11 | korrekt | High / ✓ Sicher, Grün |
| Schwarz | Attraktivität | 59 | 59 | korrekt | Uncertain / ? Unsicher, Rot |
| Schwarz | Artenschutz | 24 | **2** | **falsch** | Uncertain / ? Unsicher, Rot |
| Schwarz | Ruf | 15 | 15 | korrekt | High / ✓ Sicher, Grün |

Schwarz bleibt problematisch: Artenschutz 24 ist nur zweitbester Kandidat,
Score 0,56733 gegenüber 0,60061 für 2. Kein automatischer Austausch.
Schwarz-Attraktivität wird wegen schwacher/mehrdeutiger Kernevidenz heruntergestuft.
Artenschutz bleibt nach bestehender E-Policy grundsätzlich Uncertain.
Keine null-Werte bei den zwei vorhandenen Farben; Rot/Blau jeweils dreimal null,
also keine Phantomausgabe für abwesende Farben. C3 wurde nicht ausgeführt.

Karten, Stapel und weiteres Material sind im Original vorhanden. A liefert
darauf weiterhin Kandidatenevidenz; eine kausale Zuordnung des Fehlers zu einem
bestimmten Gegenstand wurde nicht untersucht. Kein Nachbarfeldfehler unter den
sechs GT-Werten; 24→2 ist kein Nachbarfehler. Mehrere Farben und dicht stehende
Marker sind enthalten, aber keine GT-Gleichheit zweier Farben auf demselben
Trackwert. Damit kein eigenständiger Nachweis zuverlässiger Mehrfachbelegung.
Keine Sonderregel oder Threshold-Änderung aus diesem Foto.

## Abschlussmetriken

Arithmetik aus gespeicherten V5-Reports, keine neue Inferenz. V2 und V3 sind
wiederverwendet, Calibration ist Entwicklungsdaten. Nur vorhandene GT-Werte;
null wird separat gezählt. Roh-A und Vorschläge nach Confidence E unterscheiden
sich, weil E auch nachträglich auf null setzen kann.

| Satz | n | Roh-A richtig/falsch/null | E-Vorschläge richtig/falsch/null | Grün/Gelb/Rot | Grün-Fehler | Fehler in Gelb/Rot |
|---|---:|---|---|---|---:|---|
| Calibration | 123 | 94 / 12 / 17 | 87 / 9 / 27 | 38 / 23 / 62 | 0 | 9/9 = 100 % |
| Alter V2 | 60 | 45 / 8 / 7 | 41 / 5 / 14 | 12 / 11 / 37 | 0 | 5/5 = 100 % |
| V3 | 156 | 113 / 30 / 13 | 109 / 28 / 19 | 48 / 32 / 76 | **8** | 20/28 = 71,4 % |
| Stressfoto | 6 | nicht separat exportiert | 5 / 1 / 0 | 3 / 0 / 3 | 0 | 1/1 = 100 % |

„Fehler in Gelb/Rot“ bezieht sich auf falsche ausgegebene Werte, nicht auf null.
Mit null als zusätzlich nicht korrekter Entscheidung sind es 36/36, 19/19,
39/47 und 1/1. Die erste Tabelle darf keine statistische Sicherheitsgarantie
begründen. Auch Grün erfordert Benutzerprüfung.
Die separaten Abwesenheits-/Phantommetriken bleiben in den vollständigen
[V5-Metriken](../C3RefinementV5/METRICS.md); sie werden nicht in den Nenner der
vorhandenen Werte hineingerechnet. Stress-Abwesenheit: 0/6 Phantomwerte.

[metrics.json](metrics.json) und [Summarize.ps1](Summarize.ps1) legen Quellen und
Berechnung offen. Stress-Roh-A wird nicht durch eine zweite Auswertung nachgeholt.
Separat diagnostisch: C3 P0 trifft auf Calibration/V2/V3 94/123, 30/60, 100/156
(V5). Dies ist keine Produktqualität. Keine Oracle-Zahl als Produktmetrik.

## Daten und Lieferung

V3/V4/V5-Berichte, Pläne, Auswahl-/Freeze-Dateien und historische Eingabe-/
Metrikmanifeste bleiben unangetastet. Neue native Builds, Toolchains, Warps und
BGR-Puffer sind nicht versioniert. Dieser Report speichert keine zusätzliche
Rohfotokopie. Der native Build-Spike ist separat dokumentiert; sein fehlgeschlagenes
Paritätsgate sperrt den vollständigen Port und die nachfolgende Produktintegration.

Verifikation: `dotnet build BoardgameTracker.sln` erfolgreich, 0 Warnungen/Fehler;
75 Core- und 178 Application-Tests einschließlich bestehender CV-/A-Härtungs-
Regressionen bestanden. Native Android-Release-Builds und sechs ABI-Fehlerchecks
bestanden; Parität fehlgeschlagen. iOS auf Benutzerwunsch nur dokumentarisch geprüft.
UI-/ViewModel-Tests für Kamera/Galerie, Vorschläge, Bestätigung, Status, Info und
Picker-Abbruch sind noch offen, weil die Produktintegration vorgelagert gesperrt
ist. [Nachweise](../../../NativeVisionSpike/verification.json).
