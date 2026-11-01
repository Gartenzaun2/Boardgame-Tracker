# Arche Nova – CV-Status

## 1. Zweck

Diese Datei ist die kanonische Quelle für den **aktuellen Forschungs-,
Freigabe- und Integrationsstatus** der Arche-Nova-Fotoerkennung.

Nicht hier wiederholt werden:

- Produktworkflow → [PRODUCT_SPEC.md](PRODUCT_SPEC.md)
- Darstellung/Confidence-UI → [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md)
- Produktionsarchitektur → [ARCHITECTURE.md](ARCHITECTURE.md)
- Build/Reproduktion/native Technik →
  [NativeVisionSpike/README.md](../../../tools/Games/ArkNova/NativeVisionSpike/README.md)

Historische Reports und Messdateien sind Evidenz, keine konkurrierende
Statusspezifikation.

## 2. Aktueller Status

**Native Mobile-CV ist für den in [PRODUCT_SPEC.md](PRODUCT_SPEC.md) definierten
assistierten Wertungsworkflow fachlich freigegeben.**

- Native Mobile ist der Produktionspfad.
- Android und iOS verwenden denselben produktiven nativen C++/OpenCV-Kern,
  jeweils als plattformspezifisch gebaute Bibliothek.
- Windows/OpenCvSharp bleibt Referenz-/Regressionstool.
- Die Erkennungsforschung ist **abgeschlossen / frozen**.
- Keine weitere CV-Optimierung, Ursachenforschung oder Parameteränderung ist geplant.
- Der technische iOS-Build-/Link-/Runtime-Nachweis sowie der vollständige
  `iossimulator-arm64`-Ergebnislauf sind erbracht und für den assistierten Workflow
  fachlich akzeptiert.

Das frühere Paritäts-/Ground-Truth-Gate bleibt als historisch fehlgeschlagener
Messstand erhalten. Die dokumentierten Android-/Native- und iOS-Plattformabweichungen
wurden durch ausdrückliches fachliches GO für den assistierten Workflow akzeptiert.

## 3. Aktive Produktionsbaseline

Produktiv bleibt das gehärtete **A** aus V5 mit:

```text
board_reference_master_clean.png
scoring_regions_v2.json
ArkNovaMaskFraction.ClampRoundoff
A_NoZero
Confidence E
RegistrationEdge = 1600
AnalysisScale = 0.5
```

Registrierung, Ranking und Confidence bleiben unverändert.

Nicht produktiv:

- C1/C2: abgeschlossen/verworfen;
- C3: ausschließlich `research-only / secondary diagnostic signal`;
- keine A+C3-Fusion;
- kein F1/F2;
- keine optionale C3-Warnregel;
- B/C-Referenzvarianten: dokumentierte Forschungsstände ohne Produktgewinn.

Der finale Referenzvergleich bleibt Evidenz:
[Reference3WayComparison](../../../tools/Games/ArkNova/PhotoRecognition/Reports/Reference3WayComparison/Reference3WayComparison.md).

## 4. Dokumentierte Qualität und akzeptierte Grenzen

Der akzeptierte native Mobile-Ground-Truth-Lauf bleibt unverändert:

```text
Native Mobile nach Confidence E: 294 / 411 richtig
Windows A nach E:                297 / 411 richtig
Native Mobile High:              115, davon 107 richtig / 8 falsch
V3 Empty nach E:                 11 / 120 Phantome
Windows A Empty nach E:          10 / 120 Phantome
Registrierung:                   54 / 54
Native Mobile vs. Windows A:     30 Raw- / 27 E-Abweichungen
```

Alle acht falschen High-Werte der akzeptierten Native-Mobile-Baseline waren bereits
bei Windows A in gleicher Weise falsch. Es entstanden dort keine neuen
False-High-Fälle, kein korrektes High wurde zu einem falschen High und keine neuen
High-Phantome wurden eingeführt.

Der vollständige iOS-Simulator-Ergebnislauf verwendete dieselben unveränderten
Golden-Eingaben und die echte `iossimulator-arm64`-Engine:

```text
Fälle / Registrierungen:          54 / 54
Entscheidungen:                   648
Technische Fehler:                0
iOS nach Confidence E:            294 / 411 richtig
Raw-Abweichungen vs. Mobile:      34
E-/Confidence-Abweichungen:       27
davon geänderte E-Werte:          12
Neue False Highs:                 0
Neue High-Phantome:               0
```

Auf Einzelfallebene wurden nach Confidence E zwei gegenüber der akzeptierten
Native-Mobile-Baseline zuvor korrekte Werte falsch und zwei zuvor falsche Werte
korrekt. Der Gesamtscore bleibt dadurch unverändert bei 294 / 411. Diese
Plattformabweichungen sind für den assistierten Workflow ausdrücklich akzeptiert,
weil keine neuen False-High- oder High-Phantom-Fälle entstanden sind und die
Nutzerprüfung/-korrektur vor dem Speichern Bestandteil des Produktworkflows bleibt.

Die dokumentierten Qualitätsgrenzen sind damit für Android/Native Mobile und iOS
fachlich akzeptiert. Bekannte schwierige Fälle bleiben insbesondere:

- Schwarz;
- Nachbarfelder;
- mehrere Farben auf demselben Feld;
- Schatten;
- Karten oder anderes Störmaterial.

Vollständige historische Einzelentscheidungen und das frühere Gate:
[NativeGroundTruthAcceptance](../../../tools/Games/ArkNova/NativeVisionSpike/NativeGroundTruthAcceptance.md).

Der zusätzliche Stress-Test bleibt historische Evidenz und kein unabhängiger,
statistisch ausreichender Holdout:
[FinalAcceptance](../../../tools/Games/ArkNova/PhotoRecognition/Reports/FinalAcceptance/README.md).

## 5. Plattformnachweise

Für iOS sind die relevanten technischen Nachweise erbracht:

- Apple-/iOS-Simulator-Release-Build für `iossimulator-arm64`;
- native Linkauflösung;
- erfolgreicher App-Startup-Smoke-Test bis zum sichtbaren Hauptbildschirm ohne
  Black Screen oder beobachteten Crash;
- vollständiger nativer CV-Ergebnislauf mit 54 / 54 Registrierungen und
  648 Entscheidungen;
- keine technischen Engine-/ABI-/Decode-Fehler;
- fachliche Akzeptanz der dokumentierten Ergebnisabweichungen.

Der weiterhin offene Android-ARM64-Gerätenachweis wird ausschließlich in
[`docs/TODO.md`](../../TODO.md) geführt und ist kein offenes
CV-Forschungs- oder Produktfreigabegate.

Build- und Reproduktionsbefehle stehen ausschließlich im
[Native README](../../../tools/Games/ArkNova/NativeVisionSpike/README.md).

## 6. Evidenz und Freeze

Historische Messungen, Ground Truths, Freezes und begründete Reports bleiben
unverändert erhalten und werden nicht rückwirkend an die Produktfreigabe angepasst.

Neue Toolchains, Builds, Warps, Caches und andere reproduzierbare Zwischenartefakte
bleiben außerhalb der versionierten Evidenz unter den vorgesehenen ignorierten
Ausgabeordnern.

Die fachliche Freigabe ändert keine historischen Messwerte und erklärt frühere
FAIL-Ergebnisse nicht nachträglich zu PASS.
