# NativeGroundTruthAcceptance

> **Aktuelle Produktentscheidung: fachliches GO für assistierte Wertung.**
> Die dokumentierten Plattformabweichungen sind bewusst akzeptiert: keine neuen
> False Highs/High-Phantome und verpflichtende Nutzerprüfung/Bestätigung.
> Die folgende frühere Gate-Auswertung und alle Messwerte bleiben unverändert
> als historischer Nachweis erhalten. Ihr FAIL blockiert die Produktfreigabe
> nicht mehr. Aktueller [Projektstatus](../../../../docs/ARK_NOVA_CV_STATUS.md).

## Historische Ground-Truth-Gate-Auswertung

**Entscheidung: FAIL – native Produktionsfreigabe bleibt offen.**
Nach Confidence E entstehen insgesamt drei korrekte Entscheidungen weniger (294 statt 297/411), und V3-Empty-Phantome nach E steigen von 10 auf 11. Beide Punkte verletzen die vorgegebenen Freigabekriterien. Keine Reparatur, Optimierung oder Ursachenforschung begonnen.

## Grundlage und Nenner

Ausschließlich der vorhandene native Android-API-30-x64-Run aus [mobile-parity.json](mobile-parity.json) wurde neu tabelliert. Kein neuer Bild-/CV-Lauf, keine Parameter-, Threshold-, Polygon-, Farb-, Occupancy-, Ranking- oder Confidence-Änderung. Alte Referenz/v2-Geometrie, A_NoZero, E, RegistrationEdge 1600, AnalysisScale 0,5 unverändert.

Windows A ist hier nur Vergleichsbaseline, kein zwingendes Bit-/Entscheidungsparitätsziel. Die ursprünglichen Ground Truths für Calibration/V2, V3 und Legacy6 wurden direkt geladen; jede der 648 gespeicherten Expected-Zuordnungen stimmt mit ihnen überein. V3 Empty wurde anhand des bestehenden Manifests als abwesend geprüft. Native Werte/Klassen wurden verlustfrei aus der vollständigen gespeicherten Differenzliste und unveränderten Windows-Positionen rekonstruiert. Alle Before-Werte wurden gegen den historischen A-Snapshot geprüft. Das erzwingt keine Gleichheit der Native-Entscheidungen.

54 Fotos, 648 Entscheidungen: 411 vorhandene GT-Werte (123/60/156/72), zusätzlich 237 Abwesenheitsentscheidungen (81 Calibration, 36 V2, 120 V3 Empty). Keine null-Werte entfernt. Bestehende wiederverwendete Datensätze, kein neuer unabhängiger Holdout. Registrierung in beiden vorhandenen Läufen 54/54; dies ist kein Qualitäts-PASS.

## Vergleich gegen Ground Truth

Delta = Native minus Windows; bei Trefferzahlen sind negative Werte schlechter, bei Fehler-/Phantomzahlen positive. Nenner bleiben unverändert.

| Metrik | Windows A | Native | Delta |
|---|---:|---:|---:|
| Calibration raw | 94/123 | 92/123 | -2 |
| V2 Holdout raw | 45/60 | 44/60 | -1 |
| V3 raw | 113/156 | 113/156 | 0 |
| Legacy6 raw | 60/72 | 62/72 | +2 |
| Gesamt raw | 312/411 | 311/411 | -1 |
| Calibration nach E | 87/123 | 85/123 | -2 |
| V2 Holdout nach E | 41/60 | 38/60 | -3 |
| V3 nach E | 109/156 | 109/156 | 0 |
| Legacy6 nach E | 60/72 | 62/72 | +2 |
| **Gesamt nach E** | **297/411** | **294/411** | **-3** |
| V3 False High | 8 | 8 | 0 |
| Empty Phantom raw | 40/120 | 42/120 | +2 |
| **Empty Phantom E** | **10/120** | **11/120** | **+1** |
| Neue falsche High-Vorschläge gegenüber A | – | 0 | – |
| Korrekt-High bei A → falsch-High bei Native | – | 0 | – |

Tripel = richtig/falsch/null auf vorhandenen GT-Werten. Auch Uncertain mit Wert wird anhand des Wertes bewertet; die Klasse wird separat ausgewiesen, entsprechend der bisherigen Reports.

| Datensatz | Windows raw | Native raw | Windows nach E | Native nach E |
|---|---:|---:|---:|---:|
| Calibration | 94/12/17 | 92/12/19 | 87/9/27 | 85/8/30 |
| V2 Holdout | 45/8/7 | 44/7/9 | 41/5/14 | 38/4/18 |
| V3 | 113/30/13 | 113/30/13 | 109/28/19 | 109/28/19 |
| Legacy6 | 60/11/1 | 62/9/1 | 60/11/1 | 62/9/1 |
| **Gesamt** | **312/61/38** | **311/58/42** | **297/53/61** | **294/49/68** |

V3 Empty wird separat über alle 120 Abwesenheitsentscheidungen bewertet: raw 40 Phantome/80 null → 42/78; nach E 10 Phantome/110 null → 11/109.

## Tracks

| Bereich | Nenner | Windows raw | Native raw | Windows nach E | Native nach E |
|---|---:|---:|---:|---:|---:|
| Attraktivität | 137 | 100/25/12 | 100/24/13 | 96/22/19 | 96/19/22 |
| Artenschutz | 137 | 96/24/17 | 95/23/19 | 89/22/26 | 88/21/28 |
| Ruf | 137 | 116/12/9 | 116/11/10 | 112/9/16 | 110/9/18 |

## Farben

| Bereich | Nenner | Windows raw | Native raw | Windows nach E | Native nach E |
|---|---:|---:|---:|---:|---:|
| Rot | 102 | 87/13/2 | 87/13/2 | 87/12/3 | 87/12/3 |
| Blau | 99 | 68/16/15 | 69/15/15 | 66/15/18 | 67/14/18 |
| Gelb | 105 | 95/10/0 | 95/10/0 | 95/10/0 | 95/10/0 |
| Schwarz | 105 | 62/22/21 | 60/20/25 | 49/16/40 | 45/13/47 |

## Confidence und falsche Highs

High/Medium/Uncertain nach E auf den vorhandenen GT-Werten; null bleibt in Uncertain enthalten.

| Datensatz | Windows H/M/U | Native H/M/U | False High Windows/Native |
|---|---:|---:|---:|
| Calibration | 38/23/62 | 37/23/63 | 0/0 |
| V2 Holdout | 12/11/37 | 14/10/36 | 0/0 |
| V3 | 48/32/76 | 47/31/78 | 8/8 |
| Legacy6 | 17/12/43 | 17/13/42 | 0/0 |
| **GT gesamt (411)** | **115/78/218** | **115/77/219** | **8/8** |
| Alle Entscheidungen (648, inkl. Abwesenheit) | 115/79/454 | 115/78/455 | 8/8; keine High-Phantome |

| Klasse, nur vorhandene GT | Windows richtig/falsch/null | Native richtig/falsch/null |
|---|---:|---:|
| High | 107/8/0 | 107/8/0 |
| Medium | 72/6/0 | 72/5/0 |
| Uncertain | 118/39/61 | 115/36/68 |

Alle acht falschen High-Vorschläge stammen aus V3 und sind in Wert und Klasse bereits in Windows A falsch. Keine neuen falschen Highs, kein bisher korrekter High wird falsch-High, keine High-Phantome bei abwesenden Farben. Die unveränderte Anzahl wurde auf Identität der Einzelentscheidungen geprüft:

| Foto | Farbe / Track | GT | Windows A = Native, jeweils High |
|---|---|---:|---:|
| Fall1Gegenueber.jpg | red / attractiveness | 63 | 61 |
| Fall1Gegenueber.jpg | yellow / reputation | 1 | 2 |
| Fall1Links.jpg | red / attractiveness | 63 | 61 |
| Fall1Links.jpg | yellow / reputation | 1 | 2 |
| Fall1Rechts.jpg | yellow / reputation | 1 | 2 |
| Fall1Senkrecht.jpg | red / attractiveness | 63 | 61 |
| Fall1Senkrecht.jpg | yellow / reputation | 1 | 2 |
| Fall1Vorne.jpg | yellow / reputation | 1 | 2 |

## Entscheidung und Nachweisgrenzen

- **FAIL:** Gesamt nach E 297/411 → 294/411 (−3); Empty Phantom nach E 10/120 → 11/120 (+1).
- Weitere verschlechterte GT-Metriken: Calibration raw −2 und nach E −2; V2 raw −1 und nach E −3; Gesamt raw −1. Empty Phantom raw +2. Legacy6 +2 kompensiert die Verluste nach E nicht. V3-Trefferzahlen und False High unverändert.
- Nach E nach Tracks: Artenschutz −1, Ruf −2, Attraktivität gleich. Nach Farben: Schwarz −4, Blau +1, Rot/Gelb gleich. Weniger falsche ausgegebene Werte gehen mit mehr Enthaltungen einher (nach E falsch 53 → 49, null 61 → 68); dies hebt den FAIL nicht auf.
- Fehlende Windows-Parität allein wäre nach diesem Auftrag kein Ausschlussgrund. Die konkreten GT-/Sicherheitsrückschritte verhindern jedoch die Freigabe. Keine automatische Übernahme, keine Optimierung oder weitere Forschungsrunde.
- ARM64-Gerätelauf weiterhin nicht vorhanden: separater manueller Runtime-Nachweis; keine Ursache oder Bedingung für diesen x64-GT-FAIL.
- iOS: `iOS build/link/runtime verification pending manual user verification`.

[Maschinenlesbare Metriken, alle 648 GT-/Entscheidungszeilen, Gate-Auswertung und Quellprüfsummen](NativeGroundTruthAcceptance.json). Ursprünglicher [Windows-A-Snapshot](../PhotoRecognition/Reports/Reference3WayComparison/c-boundsfix-results.json) und [nativer Run](mobile-parity.json) unverändert. Hashes der originalen GT-/Split-/V3-Manifestdateien stehen im JSON. Es wurden ausschließlich gespeicherte Ergebnisse tabelliert.

Verifikation: 648 ursprüngliche GT-Zuordnungen und alle Gruppennenner geprüft; Rekonstruktion ergibt exakt die gespeicherten 30 Raw-/27 E-Differenzen. Solution-Build ohne Warnungen/Fehler, 73 Core- und 207 Application-Tests bestanden. Kein neuer CV-/Native-Lauf. Dokumentationsversion 0.10.0.23 / ApplicationVersion 60; keine Erkennungsänderung.
