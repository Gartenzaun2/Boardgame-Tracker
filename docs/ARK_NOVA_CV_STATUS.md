# Arche Nova – CV-Abschluss und Integrationsstatus

**Erkennungsforschung: ABGESCHLOSSEN / FROZEN.** Produktionsbasis ist das
gehärtete A aus V5 einschließlich `ArkNovaMaskFraction.ClampRoundoff` und
Regressionstests. Registrierung, Ranking A_NoZero und Confidence E bleiben
unverändert. C1/C2 sind abgeschlossen/verworfen. C3 bleibt ausschließlich
`research-only / secondary diagnostic signal`; keine produktive A+C3-Fusion,
kein F1/F2 und auch keine optionale C3-Warnregel. Eine weitere Untersuchung
dieser optionalen Regel wurde bewusst nicht begonnen.

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
Ein Info-Icon im Wertungsheader soll dies im vorhandenen App-Dialog erklären.
Kamera und Systempicker sollen denselben Analyzer benutzen. iOS: eingebetteter
Photos-Picker ab unterstützter System-/Binding-Version, sonst Systemauswahl.
Android: eingebetteter Picker nur bei verfügbarer stabiler SDK-/Binding-Anbindung,
sonst nativer Systempicker. Keine eigene Galerie, keine Vollbibliotheksberechtigung
allein für die Systemauswahl.

## Tatsächlicher Implementierungsstand – Produktintegration offen

Der obige Workflow ist entschieden, **noch nicht in der mobilen App umgesetzt**.
Auf ausdrückliche Folgeanweisung geht ein nativer Architektur-/Build-Spike vor.
Sein [Nachweis](../tools/Games/ArkNova/NativeVisionSpike/README.md) enthält einen
gemeinsamen C++-Kern, C-ABI und `IArkNovaVisionEngine` ausschließlich im separaten
Werkzeug. Android ARM64 und getrimmte Release-Test-App bauen. SIFT/Matching/
RANSAC/Warp und Interop laufen im x64-Android-Emulator, aber das numerische
Paritätsgate zur Windows-Referenz scheitert. iOS wurde auf Benutzerwunsch nur
gegen Dokumentation geprüft; Build/Linking/Laufzeit sind ungetestet.

Deshalb kein vollständiger A-Port und keine scheinbar funktionierende Kamera-
oder Pickerintegration. UI-/Bestätigungs-/Persistenz-/Picker-Tests folgen mit
dieser Integration. Die Windows/OpenCvSharp-Implementierung bleibt Referenz und
Forschungswerkzeug, kein mobiler Produktionsweg. Ranking, Confidence, Domain und
Feld-/Konfliktlogik sollen in C# bleiben. Der technische Backend-Port ist keine
Wiederaufnahme der eingefrorenen Erkennungsforschung.

## Aufbewahrung

V3, V4, V5, PLAN/README, Auswahl-/Freeze-Dateien, Manifeste, JSON-Metriken und
begründete Diagnosen bleiben erhalten; keine historischen Freezes umschreiben.
Vorhandene historische Diagnose-/Normalartefakte bleiben zur Reproduzierbarkeit
an ihren bisherigen Orten. Neue Builds, Warps, Feature-Caches, normalisierte
BGR-Vergleichsdateien, Toolchains und Downloads liegen ausschließlich unter
ignoriertem `artifacts/` bzw. `bin/obj`. Das neue Stressfoto wird nicht dupliziert
oder versioniert; Manifest, GT und vollständiges Ergebnis bleiben im Git.
