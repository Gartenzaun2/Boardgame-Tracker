# Arche Nova – CV-Abschluss und Integrationsstatus

**Mobile Ark-Nova-CV ist fachlich für assistierte Wertung freigegeben.**
Der native Mobile-CV-Kern ist der Produktionspfad. Die dokumentierten
Plattformabweichungen gegenüber Windows A sind durch ausdrückliches fachliches
GO bewusst akzeptiert: keine neuen False-High-Fälle, keine neuen High-Phantome,
Prüfung/Korrektur und ausdrückliche Bestätigung durch den Nutzer vor Speicherung.
Alle Ergebnisse bleiben Vorschläge, einschließlich Grün. Keine automatische
Wertung und keine weitere CV-Forschung oder -Optimierung geplant.

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
Der native Mobile-CV-Port und App-Workflow sind für assistierte Wertung fachlich
freigegeben. Windows-Parität und das historische GT-Gate blockieren diese bewusst
getroffene Produktentscheidung nicht. Keine weitere Referenzforschung.

Die Qualität reicht für manuell geprüfte Vorschläge, nicht für stilles Speichern.
Auf wiederverwendetem V3 sind acht grüne Vorschläge falsch. Bekannte Grenzen:
Schwarz, Nachbarfelder, mehrere Farben auf demselben Feld, Schatten und
Karten/sonstiges Störmaterial. Keine weitere CV-Forschung geplant; keine neue
Suche auf V2/V3 oder dem Stressfoto.

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

## Produktionspfad und dokumentierte Qualität

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

Die Ground-Truth-Ergebnisse bleiben unverändert dokumentiert: nach E **294/411**
richtig; Raw gesamt 311/411. Die 411 vorhandenen GT-Entscheidungen nach E:

| Klasse | Anzahl | Richtig | Falsch | Ohne Wert |
|---|---:|---:|---:|---:|
| High | 115 | 107 | 8 | 0 |
| Medium | 77 | 72 | 5 | 0 |
| Uncertain | 219 | 115 | 36 | 68 |

Die **68 ohne Wert sind in den 219 Uncertain enthalten**. Alle acht falschen
Highs sind bereits bei Windows A in gleicher Weise falsch; keine neuen False
Highs, kein korrekt-High zu falsch-High und keine neuen High-Phantome.
V3 Empty nach E **11/120** Phantome (Windows A 10/120), Raw 42/120 (A 40/120).
Windows A bleibt mit 297/411 nach E die Vergleichsbaseline. Vollständige Tabellen
und Einzelentscheidungen, einschließlich des historischen GT-Gate-FAIL:
[NativeGroundTruthAcceptance](../tools/Games/ArkNova/NativeVisionSpike/NativeGroundTruthAcceptance.md).

Das historische strengere Gate bleibt als Mess-/Bewertungsstand erhalten.
Das ausdrückliche fachliche GO akzeptiert die Plattformabweichungen für den
assistierten Workflow bewusst; es erklärt weder die Ergebnisse zu gleich noch
ändert es Messwerte. 54/54 Registrierungen und 30 Raw-/27 E-Abweichungen bleiben
Vergleichsdaten. Keine neue Messung, Ursachenforschung, Threshold-, Polygon-,
Confidence- oder sonstige Erkennungsänderung. Keine weitere CV-Optimierung geplant.

## Offene manuelle Runtime-Nachweise

Diese Nachweise sind getrennt von der abgeschlossenen fachlichen Produktfreigabe:

- Android ARM64: echter Gerätelauf weiterhin offen; bisheriger Runtime-Nachweis
  auf Android API 30 x64. Keine neue CV-Qualitätsprüfung als Voraussetzung.
- iOS: `iOS build/link/runtime verification pending manual user verification`.

iOS besitzt denselben implementierten Engine-/Service-/Review-Pfad, statisches
Archiv-Buildskript für Device ARM64/Simulator, `NativeReference`, `__Internal`,
Framework-/Runtime-Linkangaben, Assets, Kamera, Systempicker und native
Format-/Orientierungsnormalisierung. Der iOS-C#-Target kompiliert auf Windows;
das ersetzt keinen Apple-Build-/Link-/Laufzeitnachweis:
`iOS build/link/runtime verification pending manual user verification`.

Build-/Reproduktionsanleitung und aktuelle Nachweisgrenzen:
[Native README](../tools/Games/ArkNova/NativeVisionSpike/README.md).
Windows/OpenCvSharp bleibt Referenz-/Regressionstool; Native Mobile ist der
Produktionspfad für assistierte Wertung. Keine weitere CV-Forschung geplant.

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
