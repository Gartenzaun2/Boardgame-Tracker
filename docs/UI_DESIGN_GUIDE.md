# Boardgame Tracker – UI Design Guide

## 1. Ziel

Die Oberfläche soll ruhig, freundlich und auf einem 6,1-Zoll-Smartphone
zuverlässig mit einer Hand bedienbar sein.

Funktionale Klarheit hat Vorrang vor Dekoration.

Primäres Referenzgerät:

- Smartphone
- Hochformat
- ca. 6,1 Zoll
- Referenzauflösung `1170 × 2532`

## 2. Zentrale Farben

| Rolle | Wert | Verwendung |
|---|---|---|
| App-Hintergrund | `#F8F5EA` | warmer Grund |
| Oberfläche | `#FFFDF7` | Karten/Eingaben |
| Pastell-Blau | `#EAF4F7` | sekundäre Bereiche |
| Pastell-Grün | `#EDF5E8` | ergänzend/positiv |
| Pastell-Gelb | `PaleGoldenrod` | Gruppen-/Auswahlflächen |
| Pastell-Violett | `#F1ECF7` | ergänzende Akzente |
| Primär-Akzent | `YellowGreen` | bestätigende Hauptaktion |
| Sekundär-Akzent | `#3E6E83` | Icons/sekundäre Aktionen |
| Primärtext | `#28322D` | fachliche Inhalte |
| Sekundärtext | `#65706A` | Hilfs-/Metatext auf neutralem Grund |
| Kontur | `#D8D5C8` | Karten-/Bereichsgrenzen |

Farben werden zentral als MAUI-Ressourcen definiert. Einzelne Screens bauen
keine unabhängige Farbpalette.

## 3. Verbindliche Kontrastregel für Spielerfarben

Arche-Nova-Spielerkacheln können dezent in Blau, Gelb, Rot oder Schwarz getönt
sein.

**Auf farbig getönten Spieler-Kacheln darf fachlicher Inhalt niemals die graue
Sekundärtextfarbe verwenden.**

Das gilt insbesondere für:

- Name
- Punkte
- Platzierung
- Spielerfarbe
- Zooplan
- Attraktivität
- Artenschutz
- Ruf
- besondere Karten
- sonstige Ergebnis-/Wertungswerte

Diese Inhalte verwenden eine ausreichend kontrastreiche Primärtextfarbe.

Sekundärtext ist nur für echte Hilfs-/Metainformationen auf einem dafür
geeigneten, neutralen Hintergrund vorgesehen.

Spielerfarbe darf außerdem nie die einzige Zustandskennzeichnung sein; Farbe
bleibt zusätzlich textuell oder strukturell erkennbar.

## 4. Touch-Ziele und Icons

Richtlinie:

- sichtbares Icon etwa 20–24 dp,
- Touch-Ziel etwa 44–48 dp,
- ausreichender Abstand zwischen benachbarten Touch-Zielen.

Wiederkehrende Bedeutung:

- Pfeil links: zurück
- Info: zusätzliche Erklärung
- Stift: bearbeiten
- Palette: Spielerfarbe ändern
- besonderes-Karten-Icon: Auswahl besonderer Karten

Icons sind echte Bildressourcen und keine fontabhängigen Unicode-Symbole.

Wichtige Funktionen dürfen nicht nur über eine unsichtbare Touch-Fläche
erreichbar sein.

## 5. Buttons und Aktionen

Buttons:

- normale Schriftgröße,
- kompakter Innenabstand,
- moderate Rundung,
- keine unnötig großen Farbflächen.

Pro Screen möglichst eine klar erkennbare Hauptaktion.

Bestätigende Hauptaktionen verwenden den grünen Primär-Akzent.

Destruktive Aktionen wie `Verwerfen`, `Entfernen`, `Löschen` sind weniger
prominent und erhalten bei realistischem Fehlbedienungsrisiko eine Bestätigung.

### Disabled

Eine deaktivierte Hauptaktion muss **sichtbar** deaktiviert sein.

Beispiele:

- `Starten` vor vollständiger Zooplanauswahl,
- `Wertung abschließen` vor vollständigen Pflichtwerten.

Nur technisch `IsEnabled=false` ohne klare visuelle Änderung genügt nicht.

## 6. Informationshierarchie

1. Seitentitel
2. Abschnittstitel
3. primäre Inhalte
4. sekundäre Informationen
5. Hilfetexte

Längere Erklärungen werden nicht dauerhaft zwischen Arbeitsinhalte gestellt,
wenn sie nur gelegentlich benötigt werden.

Dafür: Info-Icon.

- Pointer: Tooltip/Hover
- Touch: Tippen öffnet Erklärung

Das gilt insbesondere auch für Hauptübersichten wie `Spiele` und
`Einstellungen`.

## 7. Abstände

Bevorzugte Abstufungen:

```text
4 / 8 / 12 / 16 / 20 / 24 / 32
```

Ähnliche Elemente verwenden ähnliche Abstände.

## 8. Karten und Bereiche

Karten:

- heller oder fachlich begründeter Hintergrund,
- dezente Kontur,
- moderate Rundung,
- standardmäßig keine starken Schatten.

Pastellflächen strukturieren funktionale Bereiche; nicht jeder Inhalt bekommt
eine eigene Farbe.

## 9. Busy, Navigation und Mehrfachtaps

Eine asynchrone Aktion wird nach dem ersten Tap sofort gegen erneutes Auslösen
geschützt.

Insbesondere:

- Navigation,
- Speichern,
- Starten/Beenden,
- Wertung öffnen/abschließen,
- spätere Fotoanalyse.

Während laufender Navigation darf dieselbe Seite nicht mehrfach auf den Stack
gelegt werden.

Bei wahrnehmbarer Dauer:

- `ActivityIndicator`,
- ggf. dezentes blockierendes Overlay.

Nach Erfolg oder Fehler wird wieder freigegeben.

## 10. Empty States und bedingte Meldungen

Nicht verfügbare Bedienelemente werden nicht nutzlos angezeigt.

Beispiel Gruppenerstellung:

```text
Keine weiteren Personen vorhanden.
```

Picker/Button verschwinden dann.

Bedingte Fehler-/Statuszeilen reservieren keinen Leerraum, wenn kein Text
vorhanden ist.

## 11. Hauptnavigation

Reihenfolge:

```text
Spiele · Gruppen · Historie · Einstellungen
```

`Gruppen` ist ein Root-Ziel und darf keinen alten Detailstack als vermeintliche
Übersicht wiederherstellen.

Die Gruppenübersicht zeigt direkt unter `Gruppen` den aktiven Spielnamen.

## 12. Gruppen

Gruppenkarte:

- Gruppenname
- Mitglieder
- letzte Partie im aktiven Spielkontext
- ggf. Aktiv-Icon
- getrenntes Stift-Icon

Tap auf Karte und Bearbeiten dürfen sich nicht überlagern.

Filter `Temporär` und `Archiviert` können kompakt nebeneinander stehen.

## 13. Gruppe anlegen/bearbeiten

Kopf:

```text
[←] Gruppe anlegen [i]                  [Speichern]
```

bzw.

```text
[←] Gruppe bearbeiten [i]               [Speichern]
```

Mitgliederreihenfolge:

```text
Gruppenmitglieder [i]

Anna                              ↑ ↓ ×
Ben                               ↑ ↓ ×
```

Erklärung zur Reihenfolge liegt hinter dem Info-Icon.

## 14. Partievorbereitung

Spielerkarten sind dezent in Spielerfarbe getönt.

Farbe bleibt zusätzlich textuell erkennbar.

Zooplan-Kacheln:

- helle gemeinsame Kachel für Bild + Nummer,
- Nummer sichtbar,
- Planname in der Vorbereitung nicht zusätzlich nötig,
- Auswahlrahmen außen um die gesamte Kachel,
- ohne Auswahl kein grüner Rahmen,
- Bild selbst ohne zweiten Rahmen.

Farbwähler:

- kompakt,
- Touchfläche mindestens 44 dp,
- semantischer Name für Blau/Gelb/Rot/Schwarz.

`Starten` steht klar als Hauptaktion und ist bis zur gültigen Vorbereitung
sichtbar deaktiviert.

## 15. Laufende Partie

Spielerkacheln verwenden dieselbe Spielerfarblogik wie Vorbereitung.

Primäraktion:

```text
Partie beenden
```

Sekundär:

```text
Verwerfen
```

## 16. Wertung

Spielerkachel:

```text
Claudi · 68 Punkte                  [Karten-Icon]
Blau · Parkrestaurant
Attraktivität: 72 · Artenschutz: 16
Besondere Karten: 1 · Ruf: 4
```

Ohne Ruf-Erfassung:

```text
Besondere Karten: 1
```

Regeln:

- jede Infozeile besitzt eigenen Layoutbereich,
- keine Überlagerung,
- fehlende Pflichtwerte als `–`,
- kein zusätzlicher Text `Wertung nicht vollständig`,
- kein Counter-Badge am Karten-Icon,
- Punkte hinter dem Namen sobald berechenbar,
- Spielerfläche und besondere-Karten-Aktion getrennte Touchflächen,
- Auswahl besonderer Karten schließt nach einer Aktion direkt.

`Wertung abschließen` ist bis zur Vollständigkeit sichtbar deaktiviert.

## 17. Ergebnis

Ergebnis-Spielerkarten verwenden dieselbe dezente Spielerfarbtönung wie die
anderen Session-Screens.

Darauf ausschließlich gut lesbarer Primärtext für fachliche Inhalte.

Darstellung umfasst:

- Platzierung
- Name
- Punkte
- Farbe/Zooplan
- Attraktivität/Artenschutz
- Ruf
- besondere Karten

Zusätzlich im Screen:

- Gruppe
- Datum
- Partiedauer

## 18. Accessibility

Reine Icon-/Farbaktionen erhalten semantische Beschreibungen.

Eine Funktion darf weder nur durch ein Symbol noch nur durch Farbe verständlich
sein.

Prüfliste:

- Touchziele ausreichend groß?
- Hauptaktion klar?
- Disabled-Zustand sichtbar?
- Kontrast auf farbigen Kacheln ausreichend?
- Icon konsistent?
- Hilfetext aus Hauptfluss entfernt?
- destruktive Aktion klar getrennt?
- Touch und Pointer sinnvoll unterstützt?
