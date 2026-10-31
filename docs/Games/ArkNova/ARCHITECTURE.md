# Arche Nova – Architektur-Erweiterung

## 1. Geltungsbereich

Diese Datei ergänzt die allgemeine
[Boardgame-Tracker-Architektur](../../ARCHITECTURE.md) ausschließlich um
**Arche-Nova-spezifische** Entscheidungen.

Allgemeine Regeln zu Modulpfaden, Namespace-Ownership, Composition, Farben,
Game Content, Persistenz, Navigation und Transfer werden hier nicht wiederholt.
Wenn Arche Nova lediglich die allgemeine Regel implementiert, genügt hier die
konkrete Implementierungsinformation.

## 2. Aktuelle Modulbereiche

Arche Nova verwendet die allgemeine `Games/<GameName>/`-Struktur mit
`GameName = ArkNova`.

Die aktuell wesentlichen fachlichen Bereiche sind:

```text
Core/Games/ArkNova/
  ArkNovaCardSymbol.cs
  ArkNovaGame.cs
  ArkNovaPlayerColors.cs
  ArkNovaSpecialCard.cs
  ArkNovaZooPlan.cs
  GameSessions/
  Groups/

Application/Games/ArkNova/
  GameSessions/
    Preparation/
    Scoring/
    History/
    Results/
  SpecialCards/
  Settings/
  Content/
  Persistence/

App/Games/ArkNova/
  Features/
    GameSessions/
    SpecialCards/
    Settings/
  Navigation/
  PhotoRecognition/
```

`tools/Games/ArkNova/` enthält ausschließlich Arche-Nova-Entwicklungs- und
Forschungswerkzeuge.

## 3. Ergänzungen des allgemeinen Datenmodells

Arche Nova implementiert das allgemeine Composition-Muster mit:

- `ArkNovaGameSessionPlayer` für spielabhängige Partieteilnehmerdaten,
- `ArkNovaGroupPlayer` für spielabhängige Gruppendaten.

Beide ergänzen die allgemeinen Datensätze. Sie wiederholen nur den jeweils
gemeinsamen Schlüssel als PK/FK (`GameSessionId + PlayerId` bzw.
`GroupId + PlayerId`), aber keinen allgemeinen fachlichen Zustand wie Reihenfolge
oder Platzierung.

Konkrete Felder, Invarianten, IDs, Constraints und Referenzdaten stehen in
[DATA_MODEL.md](DATA_MODEL.md).

`BoardgameTrackerDbContext.ArkNova.cs` liegt unter
`Application/Games/ArkNova/Persistence`, verwendet aber bewusst den allgemeinen
Namespace `BoardgameTracker.Application.Persistence`, weil es denselben
`partial BoardgameTrackerDbContext` erweitert. Das ist die allgemeine
Partial-Type-Ausnahme aus `docs/ARCHITECTURE.md`; die Datei bleibt trotzdem
Arche-Nova-owned.

## 4. Registrierung, Initialisierung und Navigation

`AddArkNova()` ist der öffentliche Modul-Einstieg im App-Composition-Root.

`ArkNovaReferenceDataSeeder` implementiert den allgemeinen
`IDatabaseInitializationStep` für Arche-Nova-Referenzdaten.

`ArkNovaRoutes.Register()` registriert die konkreten Arche-Nova-Routen innerhalb
des Moduls.

`ArkNovaGameSessionNavigationHandler` implementiert die allgemeinen
Session-Navigation-/Resume-Verträge. Beim Resume stellt er zusätzlich den
Arche-Nova-Spiel-/Gruppenkontext für die Zielseite wieder her.

## 5. Spielerfarben

Arche Nova erlaubt aus dem gemeinsamen `PlayerColor`-Katalog ausschließlich:

```text
Blue
Yellow
Red
Black
```

`ArkNovaPlayerColors` beschreibt diese Teilmenge.

`ArkNovaGroupColorPersistence` persistiert die Arche-Nova-spezifischen
Gruppendefaults innerhalb des Spielmoduls.

Persistenzdetails und Invarianten: [DATA_MODEL.md](DATA_MODEL.md).

## 6. Game Sessions, Scoring und Historie

Vorbereitung, laufende Partie, Scoring, Ergebnisprojektion und historische Korrektur
sind getrennte Arche-Nova-Slices.

Live-Scoring arbeitet ausschließlich auf Scoring-Partien. Historische Korrektur ist
ein eigener Completed-Pfad und führt keine Statusrücktransition zu Scoring durch.

Gemeinsam verwendete Ergebnisprojektionen liegen unter
`Application/Games/ArkNova/GameSessions/Results`.
Die gemeinsame Projektion bleibt lesend; Schreibregeln und Transaktionsgrenzen
liegen beim jeweiligen Use Case.

Historische Korrektur ersetzt die erlaubten Ergebniswerte, Placements und
Kartenzuordnungen atomar. Setup- und Teilnehmerdaten bleiben außerhalb dieses
Korrekturpfads.

Fachliche Werte und Invarianten: [DATA_MODEL.md](DATA_MODEL.md).
Produktverhalten: [PRODUCT_SPEC.md](../../PRODUCT_SPEC.md).

## 7. Spezialkarten

Spezialkarten sind ein ausschließlich Arche-Nova-spezifischer Fachbereich:

```text
Core/Games/ArkNova/...SpecialCards...
Application/Games/ArkNova/SpecialCards/
App/Games/ArkNova/Features/SpecialCards/
```

Die Kartenidentität ist eine stabile GUID. Symbole und Archivstatus sind
Domainzustand; der sichtbare Name ist Game Content.

Neue Assignments dürfen nur aktive Karten verwenden. Bereits historisch zugeordnete
archivierte Karten bleiben referenzierbar und können entfernt, aber nicht neu
zugeordnet werden.

Details zu Karten, Assignments, Referenz-IDs und Nutzerinhalten:
[DATA_MODEL.md](DATA_MODEL.md).

## 8. Arche-Nova-Game-Content

Arche Nova implementiert die allgemeinen Game-Content-Regeln mit:

```text
Application/Games/ArkNova/Content/content.json
ArkNovaContentCatalog
ArkNovaContent
ArkNovaUserContent
```

`content.json` ist die einzige mitgelieferte Content-Datei des Moduls und wird als
Embedded Resource paketiert. Built-in-Content wird nicht in SQLite gespiegelt.

Stabile Objektidentitäten für Content sind:

- Spezialkarten: GUID,
- Zoopläne: Nummer.

`ArkNovaUserContent` speichert ausschließlich veränderliche Nutzernamen.
Symbole, Archivstatus und andere Domainwerte bleiben in ihren jeweiligen
Domain-/Persistenztypen.

Die Auflösung verwendet die allgemeine Content-Fallbackkette aus
`docs/ARCHITECTURE.md`.

Detailliertes Persistenzmodell: [DATA_MODEL.md](DATA_MODEL.md).
Produktregeln: [PRODUCT_SPEC.md](../../PRODUCT_SPEC.md).

## 9. History-Transfer

Arche Nova implementiert die allgemeinen Transfer-Erweiterungspunkte für sein
spielabhängiges Schema und seine EF-Persistenz unter
`Application/Games/ArkNova/`.

Formatversionen, DTO-Inhalte, Matching, Limits und Commitregeln stehen ausschließlich
in [IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).

## 10. Fotoerkennung

Fotoerkennung ist ein vollständig Arche-Nova-spezifisches Modul.

Der Produktionspfad lautet:

```text
MAUI/App
-> IArkNovaVisionEngine in Application
-> stabile C-ABI
-> gemeinsamer nativer C++/OpenCV-Kern
```

Der native Produktionscode liegt unter
`App/Games/ArkNova/PhotoRecognition/Native`.
Windows/OpenCvSharp bleibt ausschließlich Referenz-/Regressionstool unter
`tools/Games/ArkNova/`.

CV-Ergebnisse sind Vorschläge. Foto-Review verwendet einen isolierten Entwurf und
verändert keine persistierten Wertungsdaten. Erst ausdrückliche Nutzerbestätigung
ruft den validierenden Application-Speicherpfad auf. Bilder und Confidence-Werte
werden nicht als historische Spieldaten gespeichert.

Erkennungsforschung und Produktstatus:
[CV_STATUS.md](CV_STATUS.md).

Native ABI, Hosts, Build- und Reproduktionsdetails:
[NativeVisionSpike README](../../../tools/Games/ArkNova/NativeVisionSpike/README.md).


## 11. Bekannte Abweichungen

Bekannte Abweichungen zwischen aktuellem Code und gewünschter Architektur werden
nicht hier als Ausnahme legitimiert. Sie stehen in
[`docs/TODO.md`](../../TODO.md) und sollen bei passenden Aufträgen gezielt
abgebaut werden.

## 12. Dokumente für Arche-Nova-Arbeit

Nur das jeweils benötigte Dokument lesen:

| Thema | Quelle |
|---|---|
| allgemeine Architektur | `docs/ARCHITECTURE.md` |
| Arche-Nova-Modularchitektur | `docs/Games/ArkNova/ARCHITECTURE.md` |
| CV-Produkt-/Forschungsstatus | `docs/Games/ArkNova/CV_STATUS.md` |
| allgemeines Domainmodell | `docs/DATA_MODEL.md` |
| Arche-Nova-Datenmodell | `docs/Games/ArkNova/DATA_MODEL.md` |
| Produktverhalten | `docs/PRODUCT_SPEC.md` |
| Import/Export | `docs/IMPORT_EXPORT.md` |
| allgemeine UI-Regeln | `docs/UI_DESIGN_GUIDE.md` |
| konkrete Screens | `docs/UI_WIREFRAME.md` |
| native CV-Technik | `tools/Games/ArkNova/NativeVisionSpike/README.md` |
| CV-Forschungsreports | `tools/Games/ArkNova/PhotoRecognition/Reports/` |
