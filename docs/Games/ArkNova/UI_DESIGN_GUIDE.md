# Arche Nova – UI Design Guide

## 1. Geltungsbereich

Diese Datei ergänzt [docs/UI_DESIGN_GUIDE.md](../../UI_DESIGN_GUIDE.md) nur um
Arche-Nova-spezifische Darstellung. Produktverhalten steht in
[PRODUCT_SPEC.md](PRODUCT_SPEC.md), Werte/Invarianten in [DATA_MODEL.md](DATA_MODEL.md),
konkrete Screens und Navigation in [UI_WIREFRAME.md](UI_WIREFRAME.md).

## 2. Fachvisuals

Theme-Wechsel verändern nicht:

- Arche-Nova-Spielerfarben aus der gemeinsamen Palette;
- Zooplan-Grafiken;
- Spezialkartensymbole;
- andere fachliche Spielgrafiken.

Die PlayerColor-Darstellung folgt vollständig der allgemeinen Palette. Erlaubte
Arche-Nova-Farben und fachliche Invarianten stehen in [PRODUCT_SPEC.md](PRODUCT_SPEC.md)
und [DATA_MODEL.md](DATA_MODEL.md).

## 3. Zoopläne

Zooplan-Grafiken bleiben untinted. Eine Auswahl markiert die gesamte Kachel mit
`SelectedBorder` und sichtbarer Auswahlmarkierung. Skizze und Nummer bleiben sichtbar.

Helle Grafikbadges verwenden `GameGraphicText`, auch auf dunklen Spielerflächen.

## 4. Wertung

Wertungskarten trennen visuell:
- Spielername/Punkte;
- Zooplan;
- Rohwerte;
- Spezialkarten/Ruf.

Fehlende Werte erscheinen als `–`. Das Karten-Icon trägt keinen Zähler; die Anzahl
steht in der Informationszeile.

Farbige Spielerflächen verwenden ihr PlayerColor-Textpaar; eingebettete Controls
bleiben neutrales App-Chrome.

### Numerische Eingabe

Zahlenrad und direkt editierbares Feld eines Rohwerts bilden eine gemeinsame Spalte
mit gleicher Breite/Rundung. Eingaben verwenden Theme-Kontur/Text, keine
Spielerfarbe als Control-Akzent.

Der Ruf-Slider verwendet `Primary` für Griff/aktive Spur und `Outline` für die
inaktive Spur. Konkrete Anordnung, Maße und Scrollverhalten stehen in
[UI_WIREFRAME.md](UI_WIREFRAME.md).

## 5. Gleichstandsauflösung

Platzüberschriften sind neutral und verwenden `HeaderDivider`; auch leere zulässige
Zielgruppen bleiben sichtbar.

Spielerzeile: Name, Platz-Chips, rechtsbündige Punkte. Die Zeile verwendet die
PlayerColor-Palette; Gruppenüberschrift und Controls bleiben neutral.

Platz-Chips zeigen nur die Zahl:
- verfügbar: `Surface` + `Outline`;
- ausgewählt: `SelectedSurface` + `SelectedBorder` + stärkere Kontur.

Konkrete Platzwahl und Navigation: [UI_WIREFRAME.md](UI_WIREFRAME.md).

## 6. Spezialkarten

Darstellung:

```text
Symbol1 -> Symbol2 -> Symbol3 -> aktueller Name
```

Grafiken liegen unter `Resources/Images/ark_symbol_*.png`, bleiben untinted und
bewahren Reihenfolge/Wiederholungen. Auf farbigen Spielerflächen erhalten Karten eine
neutrale `Surface`-Unterlage.

### Verwaltung/Editor

Verwendet `HeaderDivider`, `CardBorder`, Primary für Neu/Speichern und transparente
Bearbeiten-Icons. Sichtbarer Archivstatus wird zusätzlich textlich vermittelt.

Der Symbolwähler verwendet `SelectionDialog` mit grafischen Einträgen und sichtbarer
Auswahlmarkierung; Namen dürfen umbrechen. Konkrete Controls und Screenverhalten
stehen im [UI_WIREFRAME.md](UI_WIREFRAME.md).

### Auswahl in einer Partie

- Zielspieler zugeordnet: `SelectedSurface`;
- mindestens anderem Spieler zugeordnet: gestrichelter 2-dp-`Danger`-Rahmen,
  Muster 4/3;
- beide Zustände: beide Markierungen;
- sonst normale `Outline`.

Keine separate sichtbare Statuszeile; Screenreader erhalten den vollständigen Status.
Auswahl-/Back-Navigation folgt dem allgemeinen `SelectionDialog` und dem
[UI_WIREFRAME.md](UI_WIREFRAME.md).

## 7. Fotoerkennung

Arche Nova verwendet für Confidence die allgemeinen Tokens
`RecognitionHigh`, `RecognitionMedium` und Danger und folgt vollständig der
allgemeinen Erkennungsstatus-Darstellung.

Foto-Review bleibt visuell ein isolierter Entwurf. Fachlicher Review- und
Speicherablauf: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).
Technischer/Forschungsstatus: [CV_STATUS.md](CV_STATUS.md).
