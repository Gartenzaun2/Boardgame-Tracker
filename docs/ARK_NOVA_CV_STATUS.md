# Arche Nova – CV-Abschluss und Integrationsstatus

**Erkennungsforschung: ABGESCHLOSSEN / FROZEN.** Produktionsbasis ist das
gehärtete A aus V5 einschließlich `ArkNovaMaskFraction.ClampRoundoff` und
Regressionstests. Registrierung, Ranking A_NoZero und Confidence E bleiben
unverändert. C1/C2 sind abgeschlossen/verworfen. C3 bleibt ausschließlich
`research-only / secondary diagnostic signal`; keine produktive A+C3-Fusion,
kein F1/F2 und auch keine optionale C3-Warnregel. Eine weitere Untersuchung
dieser optionalen Regel wurde bewusst nicht begonnen.

**Referenzexperimente B/C: abgeschlossen, kein Produktgewinn.** Produktionsbaseline A
bleibt `board_reference_master_clean.png` mit `scoring_regions_v2.json`, gehärtetem
A_NoZero, Confidence E, `RegistrationEdge=1600` und `AnalysisScale=0.5`.
Im [finalen Referenzvergleich](../tools/Games/ArkNova/PhotoRecognition/Reports/Reference3WayComparison/Reference3WayComparison.md)
registriert C v2 technisch erfolgreich 54/54; nach E treffen A/B/C jeweils 297/411.
C v2 bleibt ausschließlich dokumentierter Referenzkandidat/Forschungsstand,
keine automatische Übernahme. Weitere Referenz-/Heuristikoptimierung ist gestoppt.
Der native Mobile-CV-Port und App-Workflow sind implementiert; die Produktfreigabe
bleibt vom noch fehlenden fachlichen Paritätsnachweis abhängig. Keine weitere Referenzforschung.

Die Qualität reicht für manuell geprüfte Vorschläge, nicht für stilles Speichern.
Auf wiederverwendetem V3 sind acht grüne Vorschläge falsch. Bekannte Grenzen:
Schwarz, Nachbarfelder, mehrere Farben auf demselben Feld, Schatten und
Karten/sonstiges Störmaterial. Weitere CV-Forschung nur bei einem reproduzierbaren
Problem aus realer Produktnutzung; keine neue Suche auf V2/V3 oder dem Stressfoto.

Das letzte Stressfoto `IMG_4263.jpeg` registriert erfolgreich: fünf richtige,
ein falscher, kein fehlender Wert. Schwarz-Artenschutz: **2 statt 24**, Rot/Unsicher.
Drei grüne Werte sind korrekt. Dies ist ein zusätzlicher Stress-Test, kein
statistisch ausreichender unabhängiger Holdout. Originalhash, vorab gespeicherte
GT und Einzelauswertung: [Abschlussreport](../tools/Games/ArkNova/PhotoRecognition/Reports/FinalAcceptance/README.md).

## Verbindlicher Produktworkflow

Foto aufnehmen **oder** vorhandenes Foto auswählen → gemeinsamer Bildinput →
Orientierung/Decode → Registrierung → A → Confidence → Werte vorausfüllen →
Benutzer prüft/korrigiert → bestätigt → erst danach speichern.
Unbestätigte CV-Vorschläge dürfen auch nicht vom bestehenden manuellen Autosave
persistiert werden. Bei Analysefehlern bleiben vorhandene Wertungen erhalten;
es erscheinen keine Ersatzwerte oder technischen Exceptions. Picker-Abbruch
ist ein normaler Abbruch.

- Grün: `✓ Sicher` = bestehendes High; trotzdem prüfen, keine Garantie.
- Gelb: `! Prüfen` = bestehendes Medium mit Wert.
- Rot: `? Unsicher` = Uncertain, unbekannter Status oder null; manuell prüfen/eingeben.

Kein neuer Confidence-Threshold. Jeder Status hat Text/Icon zusätzlich zur Farbe.
Ein Info-Icon im Wertungsheader erklärt dies im vorhandenen App-Dialog.
Kamera und Systempicker benutzen denselben Analyzer. iOS: eingebetteter
Photos-Picker ab unterstützter System-/Binding-Version, sonst Systemauswahl.
Android: eingebetteter Picker nur bei verfügbarer stabiler SDK-/Binding-Anbindung,
sonst nativer Systempicker. Keine eigene Galerie, keine Vollbibliotheksberechtigung
allein für die Systemauswahl.

## Tatsächlicher Implementierungsstand – Paritätsgate offen

Gemeinsamer C++/OpenCV-Kern unter `App/Games/ArkNova/PhotoRecognition/Native`,
stabile C-ABI, SafeHandle und source-generiertes P/Invoke sind in der MAUI-App
angebunden. Decode/EXIF, Registrierung, Gates, Warp, LAB/HSV, Helligkeitskorrektur,
Masken, Grow, Field/Core und Occupancy liegen nativ; unveränderte Rankingregeln
A_NoZero und Confidence E liegen gemeinsam für App und Referenzwerkzeug in Application.
Die App paketiert ausschließlich die alte aktive Referenz und v2-Geometrie.

Kamera/Systempicker, isolierter editierbarer Review und atomare Bestätigung sind
implementiert. Unbestätigte Vorschläge hängen nicht am manuellen Autosave.
Android Release mit vollständigem Trimming baut für ARM64 und x64; tatsächliche
Laufzeitprüfung erfolgt im API-30-x64-Emulator mit isolierter Test-App und eigenen
Testdaten. EF-Mapping-/Domain-/Provider-Metadaten bleiben für reflektive EF-Zugriffe
erhalten; Trim-Warnungen sind weiterhin vorhanden. ARM64-Gerätelauf fehlt.

**Keine Produktfreigabe:** Auf allen 54 bisherigen Vergleichsfotos registriert der
Port erfolgreich. Von 648 Entscheidungen unterscheiden sich aber 30 Rohwerte
und 27 Ergebnisse nach E (Wert oder Confidence-Klasse). Gleicher Registrierungserfolg
genügt nicht. Deshalb ist die Gesamtaufgabe noch nicht abgeschlossen. Toleranzen,
Baseline und Erkennungsparameter wurden nicht geändert. Technische Untersuchung
von Feature-Reihenfolge/Gleitkomma-Kontraktion hat die Parität nicht hergestellt;
die vollständige Ursache ist nicht nachgewiesen. Keine fallbezogenen Sonderregeln.

iOS besitzt denselben implementierten Engine-/Service-/Review-Pfad, statisches
Archiv-Buildskript für Device ARM64/Simulator, `NativeReference`, `__Internal`,
Framework-/Runtime-Linkangaben, Assets, Kamera, Systempicker und native
Format-/Orientierungsnormalisierung. Der iOS-C#-Target kompiliert auf Windows;
das ersetzt keinen Apple-Build-/Link-/Laufzeitnachweis:
`iOS build/link/runtime verification pending manual user verification`.

Build-/Reproduktionsanleitung und aktuelle Nachweisgrenzen:
[Native README](../tools/Games/ArkNova/NativeVisionSpike/README.md).
Windows/OpenCvSharp bleibt die unveränderte Referenz. Der Backend-Port nimmt
keine eingefrorene Erkennungsforschung wieder auf.

## Aufbewahrung

V3, V4, V5, PLAN/README, Auswahl-/Freeze-Dateien, Manifeste, JSON-Metriken und
begründete Diagnosen bleiben erhalten; keine historischen Freezes umschreiben.
Vorhandene historische Diagnose-/Normalartefakte bleiben zur Reproduzierbarkeit
an ihren bisherigen Orten. Neue Builds, Warps, Feature-Caches, normalisierte
BGR-Vergleichsdateien, Toolchains und Downloads liegen ausschließlich unter
ignoriertem `artifacts/` bzw. `bin/obj`. Das neue Stressfoto wird nicht dupliziert
oder versioniert; Manifest, GT und vollständiges Ergebnis bleiben im Git.

Redundante lokale Referenzvergleichsausgaben, Zwischenergebnisse und leere
Abbruchdateien sind entfernt. Versionierte Messungen einschließlich des früheren
C-1506-Fehlers bleiben erhalten. Vergleichsbefehle, vollständige Handoff-Eingaben
und eingefrorene Transformationsassets bleiben ausschließlich zur Reproduktion
der dokumentierten Läufe verfügbar; sie ändern keinen Produktionsstandard.
