# Boardgame Tracker – UI / Wireframe Entscheidungen v0.2

## Referenzgerät

- 6,1 Zoll Smartphone
- Hochformat
- 1170 × 2532 px

## Navigation

Hauptbereiche:

- Gruppen
- Historie
- Statistik
- Optionen

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

- Personen auswählen
- Gäste anlegen
- für das jeweilige Spiel Default-Farben setzen
- Sitz-/Spielreihenfolge per Drag & Drop festlegen

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

- große quadratische Touch-Flächen
- Nummer + Icon
- Name sekundär
- pro Spieler zwei Optionen
- ausgewählte Option klar hervorgehoben

## Wertung

Direkte Eingabe der drei Werte zunächst bevorzugt.

Kamera-Button prominent sichtbar.

Spezialkarten: **Icon antippen → Spieler antippen**

## Wireframe-Basis

Aktueller klickbarer HTML-Prototyp:
`boardgame_tracker_wireframe_v02_mobile.html`

Dieser dient als visuelle und interaktive Grundlage für weitere
UI-Iterationen.

------------------------------------------------------------------------

## Aktualisierung v0.2 – Gruppenverwaltung

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

Mitglieder
─────────────────────────────────────────────
1.  Anna                          ↑  ↓  ×
2.  Ben                           ↑  ↓  ×
3.  Chris                         ↑  ↓  ×

Vorhandene Person hinzufügen
[ Person auswählen              ▼ ]   [ + ]

Neue Person
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
- Bereits hinzugefügte Mitglieder stehen vor den Hinzufügen-Aktionen.
- Vorhandene Personen werden gegenüber dem Anlegen einer neuen Person
  priorisiert.
- Die Auswahl vorhandener Personen erfolgt über Picker/Dropdown.
- Bereits hinzugefügte Personen werden im Picker nicht angeboten.
- Neue Personen werden erst beim Speichern der Gruppe persistiert.
- `Temporäre Gruppe` bleibt bei den grundlegenden Gruppeneigenschaften
  oberhalb des Mitgliederbereichs.

### Gruppe bearbeiten

Die Bearbeiten-Seite verwendet dieselbe visuelle Grundstruktur wie
`Gruppe anlegen`:

``` text
←  Gruppe bearbeiten   ⓘ             Speichern
```

Im ersten Stand können Gruppenname, temporär/regulär und
Default-Reihenfolge geändert werden. Die bestehende
Personenzusammensetzung wird nicht beliebig verändert.

------------------------------------------------------------------------

## Aktualisierung v0.3 – visuelle Grundregeln

Der verbindliche visuelle Leitfaden liegt in `UI_DESIGN_GUIDE.md`.

Für die Gruppen-Screens gelten zusätzlich:

- Zurück, Information und Bearbeiten werden als sichtbare Bild-Icons
  umgesetzt und nicht als fontabhängige Unicode-Zeichen.
- `Neue Gruppe` und `Speichern` verwenden kompakte Button-Flächen.
- Die App verwendet eine sanfte Pastellpalette, die von den
  Arche-Nova-Rückseiten inspiriert ist.
- Der Erklärungstext zur Mitgliederreihenfolge wird nicht dauerhaft
  angezeigt, sondern über ein Info-Icon bei `Gruppenmitglieder`
  geöffnet.
- Die sichtbare Icongröße und die Touch-Fläche werden getrennt
  behandelt: kleine Icons behalten komfortable Touch-Ziele.

------------------------------------------------------------------------

## Aktualisierung v0.4 – Empty/Busy States

`Vorhandene Person hinzufügen` besitzt zwei Zustände:

``` text
Vorhandene Person hinzufügen
[ Person auswählen ▼ ] [ + ]
```

oder, wenn keine weitere Person verfügbar ist:

``` text
Vorhandene Person hinzufügen
Keine weiteren Personen vorhanden.
```

Picker und Hinzufügen-Button werden im zweiten Zustand nicht angezeigt.

Navigation und persistierende Aktionen zeigen einen Busy-Zustand und
verhindern Mehrfachtaps.

------------------------------------------------------------------------

## v0.7.0 – Partie vorbereiten

``` text
┌──────────────────────────────────────┐
│ ←  Partie vorbereiten        Starten │
│    <Gruppenname>                     │
├──────────────────────────────────────┤
│ Zoopläne                    Auslosen │
│ 2 eindeutige Pläne je Person         │
├──────────────────────────────────────┤
│ 1  Anna                 ●   ↑   ↓    │
│    Blau                              │
│  ┌──────────┐  ┌──────────┐          │
│  │ Skizze   │  │ Skizze   │          │
│  │ Plan 3   │  │ Plan 11  │          │
│  │ wählen   │  │ wählen   │          │
│  └──────────┘  └──────────┘          │
├──────────────────────────────────────┤
│ 2  Ben                  ●   ↑   ↓    │
│    Gelb                              │
│  ...                                 │
├──────────────────────────────────────┤
│          Startspieler auslosen       │
└──────────────────────────────────────┘
```

Die Spielerkarte ist dezent in der aktuellen Spielerfarbe eingefärbt.
Reihenfolge wird direkt über Hoch/Runter angepasst. Die Zufallsfunktion
rotiert die vorhandene Reihenfolge und öffnet keinen Dialog.

------------------------------------------------------------------------

## Aktualisierung v0.7.1 – Partievorbereitung

- Direkt unter Gruppenname steht die letzte Partie; ohne Historie:
  „Keine letzte Partie vorhanden.“
- „Zoopläne auslosen“ und „Startspieler bestimmen“ stehen nebeneinander.
- Die Spielerreihenfolge ist nicht frei tauschbar. Eine separate Aktion
  rotiert die Reihenfolge zyklisch.
- Zooplan-Kacheln zeigen nur Skizze und Plannummer. Die Nummer steht
  oben mittig in der Skizze.
- Ein gewählter Zooplan erhält einen kleinen grünen Rahmen.
- Die Zooplan-Skizzen sind gegenüber v0.7.0 kompakter.
- Die Palette öffnet eine Auswahl der vier Arche-Nova-Farben. Doppelte
  Farben sind während der Vorbereitung erlaubt, verhindern aber den
  Start.
- „Starten“ steht am unteren Bildschirmrand.
- Erläuterungen wie der Zeitpunkt der Persistierung gehören nicht
  dauerhaft in die Hauptoberfläche; falls erforderlich werden sie als
  Hilfe/Hinweis angeboten.

------------------------------------------------------------------------

## Aktualisierung v0.7.2 – Partievorbereitung und Gruppenfarben

- Der **Verlierer** der letzten abgeschlossenen Partie wird beim Öffnen
  der Vorbereitung automatisch durch zyklische Rotation auf Position 1
  gesetzt.
- „Startspieler bestimmen“ ist kein eigener Textbutton mehr, sondern ein
  Würfel-Icon direkt neben „Partie vorbereiten“.
- Die Farbauswahl zeigt die vier auswählbaren Farben als tatsächlich
  farbige Flächen.
- Zoopläne sind initial nicht ausgewählt. Auswahl erfolgt durch Tippen
  auf die Skizze; nur der ausgewählte Plan erhält einen grünen Rahmen.
- In „Gruppe bearbeiten“ kann für jedes Mitglied die
  Ark-Nova-Standardfarbe gesetzt werden. Diese wird später als Default
  in die Partievorbereitung übernommen.
