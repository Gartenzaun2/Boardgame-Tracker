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
- Windows/OpenCvSharp bleibt Referenz-/Regressionstool.
- Die Erkennungsforschung ist **abgeschlossen / frozen**.
- Keine weitere CV-Optimierung, Ursachenforschung oder Parameteränderung ist geplant.

Das frühere Paritäts-/Ground-Truth-Gate bleibt als historisch fehlgeschlagener
Messstand erhalten. Die dokumentierten Plattformabweichungen wurden durch
ausdrückliches fachliches GO für den assistierten Workflow akzeptiert.

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

Der vorhandene native Ground-Truth-Lauf bleibt unverändert:

```text
Native nach Confidence E: 294 / 411 richtig
Windows A nach E:         297 / 411 richtig
Native High:              115, davon 107 richtig / 8 falsch
V3 Empty nach E:          11 / 120 Phantome
Windows A Empty nach E:   10 / 120 Phantome
Registrierung:            54 / 54
Native vs. Windows A:     30 Raw- / 27 E-Abweichungen
```

Alle acht falschen High-Werte waren bereits bei Windows A in gleicher Weise falsch.
Es entstanden keine neuen False-High-Fälle, kein korrektes High wurde zu einem
falschen High und keine neuen High-Phantome wurden eingeführt.

Die dokumentierten Qualitätsgrenzen sind für den oben verlinkten assistierten
Workflow bewusst akzeptiert. Bekannte schwierige Fälle bleiben insbesondere:

- Schwarz;
- Nachbarfelder;
- mehrere Farben auf demselben Feld;
- Schatten;
- Karten oder anderes Störmaterial.

Vollständige Einzelentscheidungen und das historische Gate:
[NativeGroundTruthAcceptance](../../../tools/Games/ArkNova/NativeVisionSpike/NativeGroundTruthAcceptance.md).

Der zusätzliche Stress-Test bleibt historische Evidenz und kein unabhängiger,
statistisch ausreichender Holdout:
[FinalAcceptance](../../../tools/Games/ArkNova/PhotoRecognition/Reports/FinalAcceptance/README.md).

## 5. Offene Runtime-Nachweise

Diese Punkte sind **keine offenen CV-Forschungs- oder Produktfreigabegates**:

- **Android ARM64:** echter Gerätelauf noch offen. Ein tatsächlicher
  Android-API-30-x64-App-/Interop-/Review-/Persistenzlauf ist vorhanden.
- **iOS:** `iOS build/link/runtime verification pending manual user verification`.

Der iOS-C#-Target kompiliert auf Windows; das beweist keinen Apple-Build, keine
Linkauflösung und keinen iPhone-/Simulatorlauf.

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
