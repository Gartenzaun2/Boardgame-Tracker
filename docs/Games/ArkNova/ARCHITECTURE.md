# Arche Nova – Architektur

## 1. Geltungsbereich

Diese Datei ergänzt [docs/ARCHITECTURE.md](../../ARCHITECTURE.md) nur um
Arche-Nova-spezifische Architekturentscheidungen. Allgemeine Modul-, Composition-,
Persistenz-, Content-, UI- und Transferregeln werden nicht wiederholt.

## 2. Modul und Composition

Arche Nova liegt vollständig unter `Games/ArkNova/` in Core, Application und App.

Das allgemeine Composition-Muster wird konkret durch:

- `ArkNovaGroupPlayer`;
- `ArkNovaGameSessionPlayer`

umgesetzt. Beide wiederholen nur ihren gemeinsamen PK/FK; allgemeiner Zustand bleibt
im Basismodell.

`Application/Games/ArkNova/Persistence/BoardgameTrackerDbContext.ArkNova.cs`
erweitert den gemeinsamen partial DbContext. Wegen des Partial-Typs verwendet die
Datei technisch den allgemeinen Persistence-Namespace; Ownership bleibt durch den
Pfad im Ark-Nova-Modul bestimmt.

Felder und Constraints: [DATA_MODEL.md](DATA_MODEL.md).

## 3. Registrierung und Navigation

`AddArkNova()` ist der öffentliche Modul-Einstieg für den App-Composition-Root.

Das Modul registriert intern unter anderem:

- `ArkNovaReferenceDataSeeder` als `IDatabaseInitializationStep`;
- `ArkNovaRoutes`;
- `ArkNovaGameSessionNavigationHandler` für Navigation/Resume;
- seine spielabhängigen Application-/UI-Komponenten.

Beim Resume stellt der Navigation-Handler zusätzlich den Arche-Nova-Spiel- und
Gruppenkontext der Zielseite wieder her.

Der allgemeine Host kennt diese internen Typen nicht einzeln.

## 4. Fachliche Slices

Arche Nova trennt insbesondere:

```text
Preparation
Scoring
Results
History
SpecialCards
Settings
Content
PhotoRecognition
```

Live-Scoring und historische Completed-Korrektur bleiben getrennte Schreibpfade.
Gemeinsam nutzbare Ergebnisprojektionen liegen im Results-Bereich und bleiben
lesend.

Produktregeln: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).

## 5. Game Content

Arche Nova implementiert Game Content mit:

```text
Application/Games/ArkNova/Content/content.json
ArkNovaContentCatalog
ArkNovaContent
ArkNovaUserContent
```

Stabile Contentidentitäten sind Spezialkarten-GUID und Zooplannummer.
`ArkNovaUserContent` speichert veränderliche Nutzernamen; fachlicher Kartenzustand
bleibt im Domainmodell.

Datenmodell: [DATA_MODEL.md](DATA_MODEL.md).

## 6. History-Transfer

Arche Nova implementiert die allgemeinen Transfer-Erweiterungspunkte innerhalb
`Application/Games/ArkNova/`.

Format, Matching und Commitregeln bleiben ausschließlich in
[../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).

## 7. Fotoerkennung

Fotoerkennung ist ein Arche-Nova-Modul.

Produktionspfad:

```text
MAUI/App
-> IArkNovaVisionEngine in Application
-> stabile C-ABI
-> gemeinsamer nativer C++/OpenCV-Kern
```

Produktiver nativer Code liegt unter
`App/Games/ArkNova/PhotoRecognition/Native`; Windows/OpenCvSharp bleibt
Referenz-/Regressionstool unter `tools/Games/ArkNova/`.

CV-Vorschläge werden erst nach expliziter Nutzerbestätigung über den normalen
Application-Speicherpfad persistiert. Bilder und Confidence-Werte sind keine
historischen Partiedaten.

Forschungs-/Integrationsstatus: [CV_STATUS.md](CV_STATUS.md). Native Technik:
`tools/Games/ArkNova/NativeVisionSpike/README.md`.

## 8. Bekannte Abweichungen

Bekannte Abweichungen vom gewünschten Architekturzustand stehen ausschließlich in
[`docs/TODO.md`](../../TODO.md) und gelten nicht als Architektur-Ausnahmen.

## 9. Dokumente

| Thema | Quelle |
|---|---|
| allgemeine Architektur | `docs/ARCHITECTURE.md` |
| Arche-Nova-Architektur | `docs/Games/ArkNova/ARCHITECTURE.md` |
| Produkt | `docs/Games/ArkNova/PRODUCT_SPEC.md` |
| Datenmodell | `docs/Games/ArkNova/DATA_MODEL.md` |
| UI | `docs/Games/ArkNova/UI_DESIGN_GUIDE.md` |
| CV-Status | `docs/Games/ArkNova/CV_STATUS.md` |
| Import/Export | `docs/IMPORT_EXPORT.md` |
