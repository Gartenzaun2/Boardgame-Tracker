# Boardgame Tracker – Dokumentationslandkarte

## Zweck

Diese Datei definiert nur die Zuständigkeiten der Projektdokumentation.
Sie enthält keine Fachregeln, sondern legt fest, **welches Dokument welche
Information kanonisch besitzt**.

- Jede dauerhafte Regel hat genau eine kanonische Quelle.
- Allgemeine Regeln stehen unter `docs/`, Spielergänzungen unter
  `docs/Games/<GameName>/`.
- Sollzustand, technische Schuld und historische Evidenz bleiben getrennt.
- Neue Markdown-Dateien entstehen nur bei einer echten neuen Verantwortung.

## Generalisierungstest

Vor Inhalt unter `docs/Games/<GameName>/` fragen:

> Würde dieselbe Regel für jedes zukünftige Spiel gelten – oder für jedes Spiel,
> das dieses allgemeine Konzept verwendet?

Wenn ja, gehört sie ins allgemeine Dokument. Spielspezifisch ist Inhalt nur bei
konkreten Spielwerten/-typen/-screens/-abläufen, bewusst abweichbaren
Spielentscheidungen oder Status/Forschung genau dieses Spiels.

Bei relevanter unklarer Zuordnung mit Produkt-, Architektur-, Persistenz- oder
erheblicher UX-Auswirkung wird der Entwickler gefragt.

## Kanonische Dokumente

| Dokument | Zuständigkeit | Nicht zuständig für |
|---|---|---|
| `PRODUCT_SPEC.md` | Produktverhalten und markierte Zielbilder | Layout, Persistenz, Architektur |
| `ARCHITECTURE.md` | Schichten, Modulgrenzen, Abhängigkeiten, Extension Points | Produktabläufe, UI-Layout, Historie |
| `DATA_MODEL.md` | Domain-/Persistenzmodell und Invarianten | UI, Workflows, Setup |
| `CODE_GUIDELINES.md` | Code/XAML/Async/Naming/Kommentare/Fehlerbehandlung | Produkt, Facharchitektur, Screenlayout |
| `UI_DESIGN_GUIDE.md` | Designsystem, Themes, Tokens, Interaction, Accessibility | konkrete Screens, Produktlogik |
| `UI_WIREFRAME.md` | Screens, Controls, Maße, Navigation, Screen-/Modalzustände | Fachvalidierung, Speicherlogik, Styles |
| `IMPORT_EXPORT.md` | Format, Versionierung, Validierung, Matching, Commit/Transfer | Screenlayout, allgemeine UI-Regeln |
| `THIRD_PARTY_LICENSE_POLICY.md` | Dependency- und Lizenz-/NOTICE-Regeln | Inventar, allgemeine Release-Anleitung |
| `TODO.md` | bekannte technische Schulden und Soll-/Ist-Abweichungen | Roadmap, Wunschliste, Historie |

## Spielspezifische Ergänzungen

Ein Spiel ergänzt dieselben Dokumentfamilien unter `docs/Games/<GameName>/`,
typischerweise `PRODUCT_SPEC.md`, `ARCHITECTURE.md`, `DATA_MODEL.md`,
`UI_DESIGN_GUIDE.md`, `UI_WIREFRAME.md` und bei eigenem Transferschema
`IMPORT_EXPORT.md`.

Bei einer konkreten Spielaufgabe gilt zuerst die allgemeine Quelle und ergänzend
die gleichnamige Spielergänzung, sofern sie existiert.

Sie ersetzen die allgemeinen Quellen nicht, sondern enthalten nur die konkrete
Spielausprägung. Spezialdokumente entstehen nur bei einer eigenen dauerhaften
Verantwortung, z. B. Forschungs- oder Integrationsstatus.

## Repository-nahe Dokumente

- `../AGENTS.md`: Arbeitsregeln und Routing für Coding Agents.
- `../README.md`: menschlicher Einstieg und kurze Projektorientierung.
- `../SETUP.md`: Setup, Build, Tests, Entwicklungsdatenbank, Versionierung, Lieferung.
- `../CHANGELOG.md`: historische Änderungen.

Keine dieser Dateien ersetzt eine fachliche kanonische Spezifikation.

## Status, Tools und Historie

Kanonische Spezifikationen beschreiben den **aktuellen gewünschten Zustand**.
Soll-/Ist-Abweichungen gehören in `TODO.md`, Historie in `CHANGELOG.md`.

Dokumentation unter `tools/` beschreibt nur Werkzeug, Bedienung oder Reproduktion.
Reports und Messungen sind Evidenz, keine Quelle für aktuellen Produkt- oder
Architekturzustand.

## Vor neuer oder geänderter Dokumentation

1. Welche Verantwortung beschreibt der Inhalt?
2. Welche bestehende kanonische Datei besitzt sie bereits?
3. Allgemein oder wirklich spielspezifisch?
4. Sollzustand, technische Schuld, How-to oder historische Evidenz?
5. Braucht es wirklich eine neue Datei?

Eine neue Datei ist nur gerechtfertigt, wenn ihre Verantwortung eigenständig,
dauerhaft wiederkehrend und in keiner bestehenden Quelle sinnvoll aufgehoben ist.
