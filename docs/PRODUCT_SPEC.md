# Boardgame Tracker – Produktspezifikation

## 1. Ziel

Boardgame Tracker erfasst, setzt fort und wertet Brettspielpartien aus. Spieler, Gruppen, Spielkontext, Partielebenszyklus und Historie sind spielübergreifend; Vorbereitung, Wertung und weitere Fachfunktionen werden von Spielmodulen ergänzt.

Aktuell vollständig unterstütztes Spiel: **Arche Nova**. Dessen Anforderungen stehen in [Games/ArkNova/PRODUCT_SPEC.md](Games/ArkNova/PRODUCT_SPEC.md).

Diese Datei beschreibt Produktverhalten. Datenstrukturen, UI-Details, Architektur sowie Import-/Lizenzdetails stehen in ihren eigenen kanonischen Dokumenten.

## 2. Spieler

Spieler sind appweit wiederverwendbar und können regulär oder Gast sein. Umbenennen, Gaststatusänderung oder Archivierung erhalten Identität und Historie; Archivierung löscht keine Gruppen oder Partien.

Unter **Einstellungen → Spieler** werden Name und Archivstatus gepflegt. Standardmäßig erscheinen aktive Nicht-Gäste; **Gäste anzeigen** und **Archivierte anzeigen** sind unabhängige Screenfilter. Der Gaststatus wird dort nicht bearbeitet.

Ein Spieler kann seine zuletzt gestartete Partie öffnen. Maßgeblich ist der neueste Startzeitpunkt, auch bei offenen Partien. Das zuständige Spielmodul öffnet je nach Status laufende Partie, Wertung oder Ergebnis.

## 3. Gruppen

Eine Gruppe hat eine feste Spielerzusammensetzung und Default-Reihenfolge. Sie kann temporär oder permanent sowie aktiv oder archiviert sein.

Temporäre und archivierte Gruppen bleiben historisch erhalten, sind aber standardmäßig verborgen. Temporär kann zu permanent werden, nicht umgekehrt. Eine andere Spielerzusammensetzung wird als neue Gruppe angelegt.

Spielmodule dürfen optionale gruppenbezogene Defaults ergänzen, ohne die allgemeinen Gruppendaten zu duplizieren.

## 4. Spielkontext und Navigation

Die Hauptbereiche sind:

```text
Spiele · Gruppen · Historie · Einstellungen
```

Die Gruppenübersicht arbeitet immer im Kontext eines ausgewählten Spiels. Beim Öffnen einer Gruppe entscheidet das Spielmodul, ob eine offene Partie/Wertung fortgesetzt oder eine neue Partie vorbereitet wird.

Lokaler Navigationszustand merkt sich optionales Standardspiel, zuletzt ausgewähltes Spiel, letzten Hauptbereich und die zuletzt explizit geöffnete offene Partie/Wertung.

Beim Start gilt:
1. gemerkte offene Partie/Wertung fortsetzen, wenn weiterhin gültig;
2. sonst letzten Hauptbereich öffnen;
3. ohne bisherigen Kontext bei Standardspiel zu **Gruppen**;
4. sonst zu **Spiele**.

Mehrere Gruppen oder Spiele dürfen gleichzeitig offene Partien besitzen; pro Kombination aus Spiel und Gruppe höchstens eine.

## 5. App- und Spielmaterialsprache

App-Sprache und Spielmaterialsprache sind getrennt. App-Sprache steuert allgemeine UI-Texte; ein Spiel kann eine eigene Materialsprache anbieten.

Ohne gespeicherte Materialsprache wird die App-Sprache verwendet, wenn unterstützt, sonst die Defaultsprache des Spiels. Die Auswahl wird pro Spiel gespeichert und durch spätere App-Sprachwechsel nicht automatisch überschrieben.

Materialsprache verändert weder App-Kultur noch historische Partiedaten und wird nicht mit Partien exportiert. Nutzerinhalt hat bei der Namensauflösung Vorrang. Weitere Contentregeln stehen in Architektur und Datenmodell.

## 6. Gruppenübersicht

Die Gruppenübersicht:
- zeigt reguläre Gruppen standardmäßig;
- blendet temporäre und archivierte Gruppen standardmäßig aus, beide separat zuschaltbar;
- sortiert nach letzter Nutzung im aktuellen Spiel;
- trennt Öffnen und Bearbeiten als eigene Aktionen;
- kennzeichnet offene Partie oder Wertung für Gruppe + Spiel;
- bietet das Anlegen einer neuen Gruppe.

Ein allgemeiner Gruppendetail-Screen ist aktuell nicht erforderlich.

## 7. Gruppe anlegen und bearbeiten

Beim Anlegen werden vorhandene aktive Spieler priorisiert; neue Spieler können mit Name und Gaststatus im selben Entwurf ergänzt werden. Bereits enthaltene Spieler werden nicht erneut angeboten.

Bearbeitbar sind Gruppenname, Default-Reihenfolge, Archivstatus, `temporär → permanent` sowie vom aktuellen Spiel unterstützte Defaults. Die Mitgliedermenge einer bestehenden Gruppe bleibt unverändert.

Neue Spieler, Gruppe und registrierte Spieldefaults werden beim Speichern atomar angelegt. Abbruch hinterlässt keine neuen Spieler.

## 8. Spielerfarben als optionales Spielkonzept

Verwendet ein Spiel die gemeinsame `PlayerColor`-Semantik, definiert es seine erlaubte Teilmenge. Gruppen können dann optionale spielabhängige Defaultfarben halten; ohne Farbunterstützung gibt es keine Farbauswahl.

Partievorbereitung darf Gruppendefaults übernehmen und für die konkrete Partie überschreiben. Partiefarben verändern die Gruppendefaults nicht.

UI-Verhalten: [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md). Datenmodell: [DATA_MODEL.md](DATA_MODEL.md).

## 9. Partielebenszyklus

Eine neue Partie beginnt als nicht persistierter spielabhängiger Entwurf. Erst beim Start entsteht die `GameSession`.

```text
Vorbereitung → Running → Scoring → Completed
```

Eine laufende Partie kann fortgesetzt, regulär beendet oder nach Sicherheitsabfrage verworfen werden. Beim Übergang zu `Scoring` wird die Spielendzeit festgehalten.

Während `Scoring` dürfen unvollständige spielabhängige Zwischenstände gespeichert werden; sie zählen noch nicht als abgeschlossene Historie oder Statistik. Erst `Completed` ist das fertige Ergebnis.

Welche Daten beim Start und Abschluss erforderlich sind, bestimmt das Spielmodul.

## 10. Historie und Korrektur

Die Historie zeigt ausschließlich `Completed`-Partien, neueste zuerst, mit Filtern
nach Spiel und Gruppe. Ein sinnvoll vorhandener Spielkontext darf vorausgewählt
sein; sonst gilt zunächst „alle Spiele“. Namen werden aktuell aus Player/Group
aufgelöst; gespeicherte Placements bleiben maßgeblich.

Ein Tap öffnet die spielabhängige Ergebnisansicht. Unterstützt das Spiel historische Korrekturen, bleibt die Partie dabei `Completed`; das Spielmodul definiert die korrekturfähigen Ergebnisdaten.

Partien können nach Sicherheitsabfrage gelöscht werden. Spieler und Gruppe bleiben erhalten.

## 11. Statistik – Zielbild

Statistik ist ein späterer, von der Partienliste unabhängiger Slice. Allgemeine Zielmetriken sind Anzahl Partien, Siege, Gewinnquote und durchschnittliche Platzierung. Allgemeine Filter können Spiel, Gruppe, Spieler und Zeitraum umfassen; Spielmodule dürfen zusätzliche Dimensionen ergänzen.

## 12. Einstellungen

Allgemeine Einstellungen enthalten mindestens Theme, Standardspiel, Spieler, rechtliche/Open-Source-Informationen und Einstiege in registrierte Spieleinstellungen.

Themes: **Deep Forest**, **Königsblau**, **Dunkel**. **Dunkel ist der Default**, wenn keine gültige Auswahl gespeichert ist. Details stehen in [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md).

Drittanbieter-Lizenzanforderungen stehen ausschließlich in [THIRD_PARTY_LICENSE_POLICY.md](THIRD_PARTY_LICENSE_POLICY.md).

## 13. Import und Export

Die Historie kann abgeschlossene Partien importieren und exportieren. Analyse/Vorschau schreibt nichts; erst die ausdrückliche Importbestätigung persistiert atomar. Abbruch oder Fehler hinterlassen keine Teildaten. Spielmodule liefern ihre zusätzlichen Daten über allgemeine Transfer-Erweiterungspunkte.

Format, Matching, Konfliktauflösung und Transportdetails: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).

## 14. Manuell erfasste Spiele – Zielbild

Später sollen Benutzer eigene Spiele anlegen und einfache Ergebnisarten wie „gespielt“, Gewinner, Punkte und/oder Platzierung konfigurieren können. Das konkrete Modell wird erst bei Umsetzung spezifiziert; daraus wird vorab keine generische Spiele-Engine abgeleitet.

## 15. Dokumentgrenzen

| Thema | Quelle |
|---|---|
| allgemeines Produktverhalten | `docs/PRODUCT_SPEC.md` |
| Produktverhalten eines Spiels | `docs/Games/<GameName>/PRODUCT_SPEC.md` |
| Datenmodell | `docs/DATA_MODEL.md` + Spiel-Datenmodell |
| UI-Regeln | `docs/UI_DESIGN_GUIDE.md` + Spiel-UI-Guide |
| konkrete Screens | `docs/UI_WIREFRAME.md` |
| Architektur | `docs/ARCHITECTURE.md` |
| Import/Export | `docs/IMPORT_EXPORT.md` |
| Drittanbieter-Lizenzen | `docs/THIRD_PARTY_LICENSE_POLICY.md` |

Der Generalisierungstest aus `ARCHITECTURE.md` gilt auch hier: Eine Anforderung wird nicht spielbezogen, nur weil aktuell erst ein Spiel implementiert ist.
