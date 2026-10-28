# Boardgame Tracker – UI / Wireframe Entscheidungen v0.2

## Referenzgerät

- 6,1 Zoll Smartphone
- Hochformat
- 1170 × 2532 px

## Navigation

Hauptbereiche:

- Spiele · Gruppen · Historie · Einstellungen

## Gruppen → Partievorbereitung

Eine Gruppe besitzt keinen separaten Detail-Screen im normalen
Startflow.

Beim Antippen einer Gruppe öffnet sich direkt die Partievorbereitung.

Oben können kompakt angezeigt werden:

- Gruppenname
- letzte Partie
- ggf. Gewinner der letzten Partie

Danach unmittelbar:

- Zoopläne auslosen
- Startspieler zufällig bestimmen
- Spielerkacheln

## Spielerkachel

Wiederverwendete Kernkomponente.

### Vorbereitung

- Reihenfolge
- Name
- dezente Spielerfarbe
- Farbe ändern
- zwei Zooplanoptionen

### Laufende Partie

- Reihenfolge
- Name
- Farbe
- gewählter Zooplan

### Wertung

- Name
- Farbe
- Zooplan
- Attraktivität
- Artenschutz
- Ruf
- Spezialkarten

### Ergebnis

- Platzierung
- Name
- Farbe
- Punkte
- Zooplan
- Rohwerte
- Spezialkarten

## Gruppenverwaltung

Bei Erstellung/Bearbeitung einer Gruppe:

- Spieler auswählen
- Gäste anlegen
- für das jeweilige Spiel Default-Farben setzen
- Default-Reihenfolge mit Hoch-/Runter-Aktionen festlegen

## Startspieler-Auslosung

Der Zufallsbutton verändert nicht beliebig die relative Reihenfolge.

Er führt konzeptionell eine zufällige Anzahl zyklischer Rotationen aus.

Beispiel:

Vorher:

1.  Anna
2.  Ben
3.  Chris
4.  David

Ausgelost wird Chris:

1.  Chris
2.  David
3.  Anna
4.  Ben

Animation:

- kurz
- dezent
- z\. B. einmaliges Aufglühen der ausgewählten Kachel
- keine lange Roulette-Animation

## Zooplanauswahl

- pro Spieler zwei kompakte Zooplan-Kacheln nebeneinander
- Skizze + Plannummer
- der Planname wird in der Vorbereitung nicht zusätzlich eingeblendet
- pro Spieler zwei Optionen
- ausgewählte Option klar hervorgehoben

## Wertung

Direkte Eingabe der drei Werte zunächst bevorzugt.

Kamera-Button prominent sichtbar.

Besondere Karten: **Karten-Icon beim Spieler antippen → konkrete Karte wählen**

## Wireframe-Basis

Der frühere HTML-Prototyp ist nicht im Repository enthalten. Maßgeblich sind dieses Dokument und `UI_DESIGN_GUIDE.md`.

------------------------------------------------------------------------

## Gruppenverwaltung

### Gruppenübersicht

Jede Gruppenkarte besitzt rechts eine kleine, eigenständige
Bearbeiten-Aktion.

``` text
┌────────────────────────────────────────────┐
│ Freitagabend                          ✎    │
│ Anna · Ben · Chris                          │
│ Zuletzt gespielt: 24.10.2026                │
└────────────────────────────────────────────┘
```

Interaktion:

- Tippen auf die Gruppenkarte → Partievorbereitung.
- Tippen auf `✎` → Gruppe bearbeiten.
- Die Touch-Flächen dürfen sich nicht überlagern.
- Das Bearbeiten-Icon soll sichtbar, aber gegenüber der primären
  Gruppenaktion zurückhaltend sein.

### Gruppe anlegen

Header auf AppBackground: Zurück, Titel, Info und Primary-Speichern. Eine feine Outline-Unterkante trennt den Arbeitsbereich.

```text
Gruppenname     [z. B. Freitagabend]
Temporäre Gruppe                 ○

Gruppenmitglieder  [Info]
Noch keine Spieler in der Gruppe.
[+ Vorhandenen Spieler hinzufügen]

Neuen Player anlegen
Name            [z. B. Alex]
Gast                             ○
[Player hinzufügen]
```

Keine Karten um Stammdaten oder neue Spieler, keine Divider zwischen Formularsektionen. Label-/Inputmuster, Höhe und Typografie sind für Gruppenname und Name gleich. Bereits hinzugefügte Spieler stehen in eigenen interaktiven Einheiten mit Reihenfolge-/Entfernen-Aktionen. Ihre Reihenfolge bleibt veränderbar.

Der Secondary-Button für vorhandene Spieler öffnet dasselbe modale Auswahlmuster wie andere App-Auswahlen: AppBackground außen, Surface-Einträge. Ein Tap fügt den Spieler direkt hinzu und schließt. Bereits enthaltene Spieler fehlen in der Auswahl; bei fehlenden verfügbaren Spielern erscheint der Leerzustand. Neue Spieler werden weiterhin erst gemeinsam mit der Gruppe gespeichert.

### Gruppe bearbeiten

Die Bearbeiten-Seite verwendet dieselbe visuelle Grundstruktur wie
`Gruppe anlegen`:

``` text
←  Gruppe bearbeiten   ⓘ             Speichern
```

Im aktuellen Lifecycle können Gruppenname, Default-Reihenfolge und
Archivstatus geändert werden. Eine temporäre Gruppe kann permanent gemacht
werden; die umgekehrte Richtung ist nicht erlaubt. Die bestehende
Spielerzusammensetzung wird nicht beliebig verändert.

------------------------------------------------------------------------

## Visuelle Grundregeln

Der verbindliche visuelle Leitfaden liegt in `UI_DESIGN_GUIDE.md`.

Für die Gruppen-Screens gelten zusätzlich:

- Zurück, Information und Bearbeiten werden als sichtbare Bild-Icons
  umgesetzt und nicht als fontabhängige Unicode-Zeichen.
- `Neue Gruppe` und `Speichern` verwenden kompakte Button-Flächen.
- Die App verwendet eine zentrale semantische Theme-Palette gemäß `UI_DESIGN_GUIDE.md`.
- Der Erklärungstext zur Spielerreihenfolge wird nicht dauerhaft
  angezeigt, sondern über ein Info-Icon bei `Gruppenmitglieder`
  geöffnet.
- Die sichtbare Icongröße und die Touch-Fläche werden getrennt
  behandelt: kleine Icons behalten komfortable Touch-Ziele.

------------------------------------------------------------------------

## Empty/Busy States

Ohne verfügbare bestehende Spieler ersetzt „Keine weiteren Spieler vorhanden.“ den Auswahlbutton. Eine leere Mitgliedsliste zeigt Text ohne Karte. Navigation und persistierende Aktionen zeigen Busy-Zustände und verhindern Mehrfachtaps.

------------------------------------------------------------------------

## Vorbereitungsscreen – aktueller Aufbau

Aktueller Aufbau:

``` text
┌──────────────────────────────────────┐
│ ←  Partie vorbereiten          [🎲]  │
│    <Gruppenname>                     │
│    <Fehler nur wenn vorhanden>       │
├──────────────────────────────────────┤
│ LETZTE PARTIE (flache Metainformation)│
│ [Zoopläne auslosen] [durchrotieren]   │
├──────────────────────────────────────┤
│ 1  Anna                         [🎨] │
│    Blau                              │
│  ┌────────────┐  ┌────────────┐      │
│  │   Plan 3   │  │  Plan 11   │      │
│  │  Skizze    │  │  Skizze    │      │
│  └────────────┘  └────────────┘      │
│                                      │
│ ... weitere Spieler ...              │
├──────────────────────────────────────┤
│               [ Starten ]            │
└──────────────────────────────────────┘
```

- Die Fehlermeldungszeile existiert layoutseitig nur, wenn tatsächlich ein
  Fehlertext vorhanden ist.
- Die Zooplan-Kacheln sind initial ohne Auswahlrahmen.
- Bei Auswahl liegt der Auswahlrahmen außen um die komplette helle
  Zooplan-Kachel.
- Die Kacheln zeigen nur Skizze und Nummer.
- Die aktuellen Bildressourcen sind quadratische Bleistiftskizzen; im
  kompakten Kachelformat werden sie bewusst mit `Fill` dargestellt.
- Die aktuell gewählte Spielerfarbe wird im geöffneten Farbwähler nicht
  erneut angeboten.

------------------------------------------------------------------------

## Laufende Partie

Nach `Starten` wird nicht mehr nur ein Bestätigungsdialog angezeigt,
sondern direkt die persistierte laufende Partie geöffnet.

``` text
Laufende Partie
<Gruppe>
Dauer: 42 min

┌──────────────────────────────────────┐
│ 1  Anna          Blau      [Plan 3] │
└──────────────────────────────────────┘
┌──────────────────────────────────────┐
│ 2  Ben           Rot       [Plan 9] │
└──────────────────────────────────────┘

[ Partie beenden ]              Verwerfen
```

Die Spielerkarten behalten die aus der Vorbereitung bekannte dezente
Spielerfarbtönung. Der gewählte Zooplan wird als Skizze mit Nummer gezeigt.

`Partie beenden` ist die primäre Aktion. `Verwerfen` ist klar sekundär und
öffnet vor dem Löschen eine Bestätigung.

Im Vorbereitungsscreen zeigt der Bereich `Letzte Partie` nun die
Platzierungen aller vorhandenen Spieler. Bei einem geteilten letzten Platz
wird der alphabetisch erste Name für die automatische Rotation auf Position
1 verwendet.


------------------------------------------------------------------------

## Gruppenfilter

Die beiden Filter `Temporär` und `Archiviert` stehen in der Gruppenübersicht
nebeneinander. Die Filterlogik bleibt unverändert; es handelt sich ausschließlich
um eine kompaktere Nutzung des vorhandenen horizontalen Platzes.


---

## Wertungsübersicht und Hauptnavigation

Untere Hauptnavigation:

```text
Spiele · Gruppen · Historie · Einstellungen
```

Der Startbereich richtet sich nach gespeichertem Kontext und Standardspiel. Ein Tap auf den Gruppen-Tab führt
zur Gruppenübersicht.

Spielerkachel in der Wertungsübersicht:

```text
┌──────────────────────────────────────┐
│ Claudi · 68 Punkte               [▣] │
│ Blau · Parkrestaurant                │
│ Attraktivität 72 · Artenschutz 16    │
└──────────────────────────────────────┘
```

Der rechte Kartenbutton verwendet das neutrale Besondere-Karten-Icon. Seine
Touch-Fläche bleibt groß genug, obwohl die Darstellung kompakt ist. Die Anzahl zugewiesener besonderer Karten steht in einer eigenen Informationszeile; der Button hat keinen Zähler. Konkrete Karten werden in der Auswahl gezeigt. Nach einer Auswahl schließt sich die Auswahl direkt.


------------------------------------------------------------------------

## Spielkontext und Startnavigation

- Die Haupttabs bleiben `Spiele · Gruppen · Historie · Einstellungen`.
- `Gruppen` existiert im normalen Bedienfluss nur mit ausgewähltem Spiel.
- Unter der Überschrift `Gruppen` steht der aktuelle Spielname; er wird nicht
  zusätzlich auf jeder Gruppenkarte wiederholt.
- Tap auf ein Spiel setzt den aktuellen Spielkontext und öffnet `Gruppen`.
- Ohne Standardspiel und ohne früheren Kontext startet die App auf `Spiele`.
- Mit Standardspiel startet ein frischer Kontext auf `Gruppen` dieses Spiels.
- Danach werden zuletzt verwendeter Hauptbereich, Spielkontext und bei Bedarf
  die konkret zuletzt geöffnete laufende Partie bzw. Wertung wiederhergestellt.
- Mehrere offene Partien sind damit navigationsseitig möglich; es wird nicht
  pauschal die zuletzt geänderte Partie geöffnet.


------------------------------------------------------------------------

## Gruppen-Kontext und Ergebnisdarstellung

- `Gruppen` darf nur mit gesetztem Spielkontext geöffnet werden. Der Kontext wird
  vor der Navigation gesetzt.
- Defaultspiel, Spielauswahl und Partiewiederaufnahme verwenden dafür denselben
  Navigationspfad.
- Ergebnis-Spielerkacheln verwenden wieder dieselbe dezente Spielerfarbtönung wie
  Vorbereitung, laufende Partie und Wertung; die Farbe bleibt zusätzlich als Text
  verfügbar.

## Verbindliche Vorbereitungsregeln

- Die letzte abgeschlossene Partie zeigt alle Platzierungen. Ihr Verlierer wird durch zyklische Rotation Startspieler; bei geteiltem letzten Platz entscheidet der alphabetisch erste Name.
- Ohne Historie erscheint „Keine letzte Partie vorhanden.“
- Der Würfel neben dem Titel lost den Startspieler aus. Die separate Rotationsaktion verschiebt zyklisch; die relative Reihenfolge bleibt erhalten.
- Doppelte Farben sind während der Vorbereitung erlaubt, verhindern aber den Start. Starten liegt am unteren Bildschirmrand.
- Defaultfarben werden in der Gruppenbearbeitung gepflegt; Farbänderungen der Vorbereitung gelten nur für diese Partie.
- Zooplanauswahl erfolgt durch Tippen auf die Skizze; initial ist kein Plan ausgewählt.

## Historie: Partien

Zwei kompakte Picker (Spiel, Gruppe) oberhalb der scrollbaren Liste. Karten mit drei umbrechenden Informationszeilen: `Arche Nova · 2 h 14 min · 26.10.26`, `Freitagabend`, `1. Anna · 2. Ben · 3. Chris`. Leer- und Fehlerzustand sowie Busy-Schutz vorsehen. Tap öffnet spielabhängige Wertung, keine generische Detailseite.

Historische Arche-Nova-Wertung: gespeicherter Platz vor Spielername, nach Placement sortiert. Bestehende Eingaben und Kartenwahl verwenden; `Korrektur speichern` führt bei Gleichstand in die vorhandene Auflösung. Sekundäre Aktion `Partie löschen` nur nach ausdrücklicher Bestätigung. Zurück verwirft ungespeicherte Korrekturen. Statistik folgt separat mit unabhängigen Filtern.

## Theme und Historiengliederung

Allgemeine Einstellungen enthalten den Picker „Darstellung“ mit Deep Forest, Königsblau und Dunkel. Alle Screens verwenden dieselbe Struktur in allen Themes. Die bestehende Shell-TabBar hat eine obere Kontur und einen aktiven Eintrag mit einer gemeinsamen abgerundeten Auswahlfläche hinter Icon und Text. Der Rand der Auswahlfläche hat keine vom Füllton abweichende Farbe. Historienfilter stehen als kompakte, bei Platzmangel umbrechende Chips oberhalb der Liste. Ein Häkchen kennzeichnet gesetzte Filter; die Auswahl erfolgt weiterhin nativ. Die Liste gruppiert Partiekarten nach lokalem Kalendermonat, z. B. „Oktober 2026“, mit einfacher Überschrift ohne Karte und ohne Sticky-Verhalten.

Die äußere besondere-Karten-Auswahl und die Listenzwischenräume verwenden AppBackground; einzelne Karten liegen auf Surface. Bereits diesem Spieler zugeordnete Karten verwenden die gemeinsame Auswahlfläche, andere Einträge bleiben auf Surface. Secondary-Aktionen in den drei Ziel-Screens verwenden SurfaceSecondary und eine Aktionskontur gemäß UI_DESIGN_GUIDE; bestehende kleine Compact-/Textaktionen außerhalb des Scopes bleiben transparent.

In der Gruppenübersicht ist „Neue Gruppe“ Primary; Gruppenkarten bleiben Surface, Filter bleiben untergeordnete Toggles. In der Vorbereitung sind Auslosen/Rotation Secondary, Starten bleibt Primary. Formular- und Metabereiche werden flach dargestellt.
