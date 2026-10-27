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

``` text
←  Gruppe anlegen   ⓘ                Speichern
─────────────────────────────────────────────

Gruppenname              [ Freitagabend     ]

Temporäre Gruppe          [ ○ ]

Spieler
─────────────────────────────────────────────
1.  Anna                          ↑  ↓  ×
2.  Ben                           ↑  ↓  ×
3.  Chris                         ↑  ↓  ×

Vorhandene Player hinzufügen
[ Player auswählen              ▼ ]   [ + ]

Neue Player
[ Name                              ]
Gast                             [ ○ ]
                              [ Hinzufügen ]
```

#### Verhalten

- Die Kopfzeile bleibt die primäre Aktionszone; `Speichern` befindet
  sich oben rechts und ist grün hervorgehoben.
- Es gibt keinen redundanten zweiten Titel im Inhaltsbereich.
- Das Informations-Icon zeigt Hilfetext per Hover, wenn Hover vorhanden
  ist, ansonsten per Tippen.
- `Gruppenname` verwendet eine kompakte Label-/Eingabefeld-Zeile.
- Bereits hinzugefügte Spieler stehen vor den Hinzufügen-Aktionen.
- Vorhandene Spieler werden gegenüber dem Anlegen einer neuen Spieler
  priorisiert.
- Die Auswahl vorhandener Spieler erfolgt über Picker/Dropdown.
- Bereits hinzugefügte Spieler werden im Picker nicht angeboten.
- Neue Spieler werden erst beim Speichern der Gruppe persistiert.
- `Temporäre Gruppe` bleibt bei den grundlegenden Gruppeneigenschaften
  oberhalb des Spielerbereichs.

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
- Die App verwendet eine sanfte Pastellpalette, die von den
  Arche-Nova-Rückseiten inspiriert ist.
- Der Erklärungstext zur Spielerreihenfolge wird nicht dauerhaft
  angezeigt, sondern über ein Info-Icon bei `Gruppenmitglieder`
  geöffnet.
- Die sichtbare Icongröße und die Touch-Fläche werden getrennt
  behandelt: kleine Icons behalten komfortable Touch-Ziele.

------------------------------------------------------------------------

## Empty/Busy States

`Vorhandene Player hinzufügen` besitzt zwei Zustände:

``` text
Vorhandene Player hinzufügen
[ Player auswählen ▼ ] [ + ]
```

oder, wenn keinen weiteren Spieler verfügbar ist:

``` text
Vorhandene Player hinzufügen
Keine weiteren Spieler vorhanden.
```

Picker und Hinzufügen-Button werden im zweiten Zustand nicht angezeigt.

Navigation und persistierende Aktionen zeigen einen Busy-Zustand und
verhindern Mehrfachtaps.

------------------------------------------------------------------------

## Vorbereitungsscreen – aktueller Aufbau

Aktueller Aufbau:

``` text
┌──────────────────────────────────────┐
│ ←  Partie vorbereiten          [🎲]  │
│    <Gruppenname>                     │
│    <Fehler nur wenn vorhanden>       │
├──────────────────────────────────────┤
│ Letzte Partie ...                    │
│ [Zoopläne auslosen] [durchrotieren] │
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
- Bei Auswahl liegt der grüne Rahmen außen um die komplette helle
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
│ Claudi · 68 Punkte              [▣2] │
│ Blau · Parkrestaurant                │
│ Attraktivität 72 · Artenschutz 16    │
└──────────────────────────────────────┘
```

Der rechte Kartenbutton verwendet das neutrale Besondere-Karten-Icon. Seine
Touch-Fläche bleibt groß genug, obwohl die Darstellung kompakt ist. Die Zahl
zeigt nur die Anzahl zugewiesener Spezialkarten; konkrete Karten werden erst
in der Auswahl gezeigt. Nach einer Auswahl schließt sich die Auswahl direkt.


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
