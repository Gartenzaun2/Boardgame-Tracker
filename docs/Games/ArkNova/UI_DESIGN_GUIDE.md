# Arche Nova – UI Design Guide

## 1. Geltungsbereich

Diese Datei ergänzt den
[allgemeinen UI Design Guide](../../UI_DESIGN_GUIDE.md) ausschließlich um
**Arche-Nova-spezifische Darstellung und Interaktion**.

Allgemeine Theme-, Dialog-, Button-, Accessibility-, PlayerColor- und
XAML-Regeln werden hier nicht wiederholt.

Konkrete Arche-Nova-Screens und deren Anordnung stehen in
[UI_WIREFRAME.md](../../UI_WIREFRAME.md), solange der Wireframe noch nicht
spielspezifisch aufgeteilt wurde.

## 2. Fachliche Visuals bleiben themeunabhängig

Ein Theme-Wechsel verändert keine Arche-Nova-Fachvisuals:

- Spielerfarben;
- leichte Färbung von Spielerkarten anhand der Spielerfarbe;
- Zooplan-Grafiken;
- Spezialkartensymbole;
- andere fachlich notwendige Spielgrafiken;
- fachliche Status-/Warnsemantik.

Theme-Flächen, Konturen und App-Chrome umrahmen diese Inhalte.

Arche Nova erlaubt aus der gemeinsamen PlayerColor-Palette ausschließlich:

```text
Blue
Yellow
Red
Black
```

Es gibt keine „Arche-Nova-Rot“- oder andere eigene Präsentationsvariante.

## 3. Gruppenfarben

Arche Nova verwendet den allgemeinen Gruppenfarben-Workflow aus
[`docs/UI_DESIGN_GUIDE.md`](../../UI_DESIGN_GUIDE.md).

Die erlaubte Teilmenge ist:

```text
Blue
Yellow
Red
Black
```

Es gibt keine zusätzlichen Arche-Nova-spezifischen Farbinteraktionen oder
Präsentationswerte. Die gemeinsame `PlayerColor`-Palette bleibt unverändert.

## 4. Zoopläne

Zooplan-Grafiken sind fachlicher Inhalt und werden nicht durch Themefarben ersetzt.

Eine Zooplanauswahl markiert die **gesamte Kachel** mit:

- `SelectedBorder`;
- sichtbarer Auswahlmarkierung zusätzlich zur Farbe.

Skizze und Nummer bleiben sichtbar.

Helle Grafikbadges verwenden `GameGraphicText`, auch auf einer dunklen
Spielerfläche.

Konkrete Anordnung und Größen stehen im Wireframe.

## 5. Wertungskarten

Arche-Nova-Wertungskarten reservieren getrennte Informationsbereiche für:

- Spielername und Punkte;
- Zooplan;
- Rohwerte;
- Spezialkarten und Ruf.

Fehlende Werte werden als `–` dargestellt.

Das Karten-Icon trägt keinen Zähler direkt im Icon. Die Anzahl erscheint in der
zugehörigen Informationszeile.

Farbige Spielerflächen verwenden die gemeinsame PlayerColor-Hintergrund- und
Textkombination. Eingebettete Eingabecontrols bleiben neutrales App-Chrome.

## 6. Begrenzte numerische Wertungseingaben

Zahlenrad und direkt editierbares Feld bilden je Rohwert eine gemeinsame Spalte:

- gleiche Breite;
- gleiche Rundungsfamilie wie `CardBorder`;
- transparente Eingabefläche innerhalb der gerundeten Surface;
- Theme-Konturen und Theme-Text;
- keine Spielerfarbe als Akzent des Controls.

Der Ruf-Slider verwendet:

- `Primary` für Griff und aktive Spur;
- `Outline` für inaktive Spur.

Bedienhinweise liegen hinter Info. Bei Tastatur oder großer Schrift bleibt der
Inhalt scrollbar.

Fachliche Wertebereiche stehen im
[DATA_MODEL.md](DATA_MODEL.md); konkrete Maße im Wireframe.

## 7. Gleichstandsauflösung

Die Gleichstandsansicht verwendet neutrale Platzüberschriften mit
`HeaderDivider`. Auch leere Zielgruppen bleiben sichtbar.

Darunter stehen kompakte Spielerzeilen mit:

- Name;
- Platz-Chips;
- rechtsbündigen Punkten.

Die Spielerzeile übernimmt Hintergrund und Text aus der gemeinsamen PlayerColor-Palette.
Überschriften und Aktionen bleiben neutral.

Regeln für die Chips:

- etwa 32 dp;
- nur die Platznummer;
- `Surface` + `Outline` für verfügbare Plätze;
- `SelectedSurface` + `SelectedBorder` und stärkere Kontur für eigene Auswahl.

Namen dürfen bei Platzmangel gekürzt werden.
Keine redundante Rangnummer neben dem Namen.

Ein Info-Icon öffnet die lokalisierte Erklärung über `AppDialogPage`.
Kein permanenter Hilfetext, Drag & Drop oder Pfeil-Workflow.

Die fachliche Tie-Resolution steht im
[DATA_MODEL.md](DATA_MODEL.md).

## 8. Spezialkartenanzeige

Arche-Nova-Spezialkarten verwenden die gelieferten Einzelgrafiken unter:

```text
Resources/Images/ark_symbol_*.png
```

Dargestellt wird:

```text
Symbol1 -> Symbol2 -> Symbol3 -> aktueller Name
```

Reihenfolge und Wiederholungen bleiben unverändert.

Die 14 Symbole sind fachliche Identität und werden nicht durch Theme-Tints ersetzt.
Kontur, Hintergrund und Text verwenden semantische Theme-Rollen.

Auf farbigen Spieler-/Ergebnisflächen erhalten Spezialkarten eine neutrale
`Surface`-Unterlage.

## 9. Spezialkartenverwaltung und Editor

Verwaltung und Editor verwenden:

- `HeaderDivider`;
- `CardBorder`;
- Primary für Neu/Speichern;
- transparentes Bearbeiten-Icon direkt an der Kartenansicht;
- keinen zusätzlichen Aktionskarten-Container.

Archivieren/Entarchivieren erfolgt ausschließlich über den Switch **„Archiviert“**
im Bearbeiten-Editor und wird mit Speichern übernommen.

Bei Neuanlage ist der Archiv-Switch verborgen.

Archivierung ist reversibel und keine Danger-Löschaktion.
Der Archivstatus wird zusätzlich als Text gezeigt.

Das Info-Icon im Listenkopf öffnet den lokalisierten Spezialkartenhinweis über den
gemeinsamen App-Dialog.

Der Symbolwähler verwendet `SelectionDialog` mit:

- grafischen Einträgen;
- mindestens 48 dp Höhe;
- Auswahlmarkierung zusätzlich zur Farbe.

Namen dürfen umbrechen. Der Screen bleibt auf dem 6,1-Zoll-Zielgerät kompakt und
scrollbar.

## 10. Spezialkartenauswahl in einer Partie

Die äußere Dialog-/Listenfläche verwendet `AppBackground`; einzelne Karten
verwenden `Surface`.

Statusdarstellung:

- dem Zielspieler zugeordnet:
  `SelectedSurface`;
- mindestens einem anderen Spieler zugeordnet:
  gestrichelter 2-dp-Rahmen, Muster 4/3, in `Danger`;
- Zielspieler **und** anderer Spieler:
  beide Markierungen kombiniert;
- nicht zugeordnet:
  normale `Outline`.

Eine separate sichtbare Zuordnungszeile entfällt.
Der Status bleibt für Screenreader vollständig beschrieben.

Zum Schließen dient der allgemeine Zurück-Pfeil des `SelectionDialog`.

## 11. Fotoerkennung / CV-Vorschläge

Fotoerkennung bleibt ein assistierter Arche-Nova-Workflow.
Erkannte Werte sind Vorschläge und werden vor Speicherung geprüft.

Die zentralen Theme-Tokens für Erkennungsstatus sind:

| Theme | RecognitionHigh | RecognitionMedium |
|---|---|---|
| Deep Forest | `#256A37` | `#806000` |
| Königsblau | `#256A37` | `#806000` |
| Dunkel | `#8CD9A3` | `#F5D45E` |

Unsichere/fehlerhafte Zustände verwenden die vorhandene Danger-Semantik.

Status darf nie nur über Farbe vermittelt werden. Die UI kombiniert Farbe mit
Text/Icon; die konkrete fachliche Statusbedeutung steht in
[CV_STATUS.md](CV_STATUS.md).

Foto-Review verändert keine persistierten Wertungsdaten, bevor der Nutzer
ausdrücklich bestätigt.

## 12. Dokumentgrenzen

| Thema | Quelle |
|---|---|
| allgemeine UI-Regeln | `docs/UI_DESIGN_GUIDE.md` |
| Arche-Nova-UI-Regeln | `docs/Games/ArkNova/UI_DESIGN_GUIDE.md` |
| konkrete Screens/Navigation | `docs/UI_WIREFRAME.md` |
| Arche-Nova-Datenmodell | `docs/Games/ArkNova/DATA_MODEL.md` |
| Arche-Nova-Architektur | `docs/Games/ArkNova/ARCHITECTURE.md` |
| CV-Status | `docs/Games/ArkNova/CV_STATUS.md` |
| Produktverhalten | `docs/PRODUCT_SPEC.md` |
