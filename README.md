# Boardgame Tracker – Scaffold v0.6.4

v0.6.4 ergänzt Gruppenarchivierung, Gruppenfilter und die überarbeitete
Gelb-/Grün-Farbsemantik.

## UI

- `SurfaceYellow` verwendet jetzt `PaleGoldenrod`.
- Gruppenkarten in der Gruppenübersicht verwenden dieses Gelb.
- `Neue Gruppe` verwendet den gelben kompakten Aktionsstil.
- Die Gruppenstammdaten in `Gruppe anlegen` und `Gruppe bearbeiten`
  verwenden ebenfalls das gelbe Oberflächen-Token.
- Bestätigende Hauptaktionen wie `Speichern` sind wieder eindeutig grün.
- Grün ist damit die feste Semantik für Bestätigen/Abschließen, Gelb für
  Gruppen-/Auswahlflächen.

## Gruppenübersicht

Zwei Schalter steuern die Sichtbarkeit:

- `Temporäre einblenden` – standardmäßig aus.
- `Archivierte einblenden` – standardmäßig aus.

Temporäre und archivierte Gruppen werden in der Liste entsprechend
gekennzeichnet.

## Gruppen bearbeiten

Eine bestehende permanente Gruppe kann nicht nachträglich temporär
gemacht werden.

Bei einer temporären Gruppe erscheint stattdessen die Option
`Als permanente Gruppe übernehmen`. Diese Änderung ist nur in Richtung
temporär → permanent möglich.

Jede Gruppe kann archiviert oder wieder aktiviert werden. Archivierte
Gruppen verschwinden bei ausgeschaltetem `Archivierte einblenden` aus
der normalen Gruppenübersicht. Historische Daten werden nicht gelöscht.

## Datenbank

`Group` besitzt neu `IsArchived`.

**Für v0.6.3.1 → v0.6.4 ist eine EF-Core-Migration erforderlich.**

Nach dem Kopieren:

``` cmd
tools\upgrade-from-v0631.cmd
tools\create-v064-migration.cmd
```

Danach App starten. `DatabaseInitializer` wendet die Migration beim
Start an.

Wenn in deinem lokalen Projekt noch gar keine `InitialCreate`-Migration
existiert, nicht beide Migrationen neu erzeugen: dann stattdessen die
bisherige Initial-Migration entsprechend dem bestehenden Setup erzeugen,
sodass `IsArchived` bereits Teil des aktuellen Modells ist.
