# Manuelle Spiele – Architektur

## Zuständigkeit

`ManualGames` ist **ein** Modul für viele benutzerdefinierte `Game`-Instanzen, kein dynamisch generiertes Modul pro Spiel. Die bestehende modulare Monolith-Struktur (`Core`, `Application`, `App` unter `Games/ManualGames/`) und die allgemeinen Spielverträge bleiben erhalten; keine generische Spiele-Engine oder `GameKey`-Fallunterscheidungen in allgemeinen Features.

- Das allgemeine `Game` trägt Identität, Namen, Capsule-Tag und Spielergrenzen; `ManualGame` trägt den variablen Erfassungsvertrag.
- Fest registrierte Modulschlüssel bleiben integrierten Spielen vorbehalten. Der reservierte lokale Schlüsselraum `manual:<GUID>` wird vom **einen** ManualGames-Modul aufgelöst. Ein zusätzliches persistiertes `ModuleKey` ist nicht nötig.
- Navigation, Resume, Wertung, historische Korrektur und Transfer sind eigene ManualGames-Handler, die vorhandene allgemeine Infrastruktur nutzen.
- Die gemeinsame Farb- und Session-Persistenz wird auch von Arche Nova verwendet; ManualGames spezifiziert lediglich deren optionale Verwendung.
- Spieleigene Konfigurationsänderungen verändern keine bereits gestarteten Session-Verträge; historische Ergebnisse werden nicht anhand aktueller Einstellungen rekonstruiert.

Das allgemeine Spiel-Limit wird ausschließlich aus `Game.MinPlayers`/`Game.MaxPlayers` geprüft, nicht über einen zusätzlichen ManualGames-Limit-Katalog. Integrierte Spiele füllen ihre gemeinsamen Stammdaten aus festen Moduldefinitionen; manuelle aus Benutzereingaben bzw. Import.

Gemeinsame Architektur- und Transfergrenzen: [../../ARCHITECTURE.md](../../ARCHITECTURE.md), [../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).
