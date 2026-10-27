# Boardgame Tracker – UI Design Guide v0.1

## 1. Ziel

Die Oberfläche soll ruhig, freundlich und auf einem 6,1-Zoll-Smartphone
zuverlässig mit einer Hand bedienbar sein.

Die visuelle Sprache orientiert sich lose an den farbigen
Arche-Nova-Rückseiten, übernimmt deren kräftige Farben aber nicht
direkt. Stattdessen verwendet die App sanfte Pastelltöne.

Funktionale Klarheit hat Vorrang vor Dekoration.

## 2. Farbkonzept

Zentrale App-Farben:

| Rolle           | Farbe     | Verwendung                      |
|-----------------|-----------|---------------------------------|
| App-Hintergrund | `#F8F5EA` | warmer, heller Grund            |
| Oberfläche      | `#FFFDF7` | Karten und Eingabebereiche      |
| Pastell-Blau    | `#EAF4F7` | sekundäre Bereiche              |
| Pastell-Grün    | `#EDF5E8` | ergänzende/positive Bereiche    |
| Pastell-Gelb    | `#FFF3C9` | Aktionsflächen                  |
| Pastell-Violett | `#F1ECF7` | spätere ergänzende Akzente      |
| Primär-Akzent   | `#D9A51B` | wichtigste Aktion eines Screens |
| Sekundär-Akzent | `#3E6E83` | Icons und sekundäre Aktionen    |
| Primärtext      | `#28322D` | Haupttext                       |
| Sekundärtext    | `#65706A` | Hilfs- und Metatext             |
| Kontur          | `#D8D5C8` | Karten-/Bereichsgrenzen         |

Die Farben werden als zentrale MAUI-Ressourcen definiert. Einzelne
Screens sollen keine eigene, davon unabhängige Farbpalette entwickeln.

Die fachlichen Arche-Nova-Spielerfarben Blau, Gelb, Rot und Schwarz
bleiben von diesem UI-Farbsystem getrennt. Sie dürfen später nicht die
einzige Kennzeichnung eines Spielers oder Zustands sein.

## 3. Touch-Ziele und Icons

Ein sichtbares Icon darf klein und zurückhaltend sein, seine
Touch-Fläche muss aber komfortabel bleiben.

Richtlinie:

- sichtbares Icon ungefähr 20–24 dp,
- Touch-Ziel ungefähr 44–48 dp,
- genügend Abstand zwischen benachbarten Touch-Zielen.

Wiederkehrende Icons behalten überall dieselbe Bedeutung:

- Pfeil nach links: zurück,
- Info: zusätzliche Erklärung,
- Stift: bearbeiten,
- Palette: Spielerfarbe ändern.

Wichtige Funktionen dürfen niemals nur über eine unsichtbare
Touch-Fläche erreichbar sein.

## 4. Buttons

Buttons sollen nicht deutlich größer wirken als der zugehörige Text.

Standard:

- normale Schriftgröße,
- kompakter horizontaler und vertikaler Innenabstand,
- leicht gerundeter Hintergrund,
- keine unnötig großen farbigen Flächen.

Pro Screen gibt es möglichst eine klar erkennbare Hauptaktion. Sekundäre
und destruktive Aktionen werden visuell zurückgenommen.

## 5. Informationshierarchie

Die Oberfläche verwendet eine klare Hierarchie:

1.  Seitentitel
2.  Abschnittstitel
3.  primäre Inhalte
4.  sekundäre Informationen
5.  Hilfetexte

Längere Erklärungen werden nicht dauerhaft zwischen die Arbeitsinhalte
gestellt, wenn sie nur gelegentlich benötigt werden. Dafür wird ein
Info-Icon verwendet.

Info-Icons:

- Pointer-Geräte: Tooltip/Hover,
- Touch-Geräte: Tippen öffnet die Erklärung.

## 6. Abstände

Bevorzugte Abstufungen:

``` text
4 / 8 / 12 / 16 / 20 / 24 / 32
```

Ähnliche Elemente verwenden ähnliche Abstände. Abstände sollen
Hierarchie erzeugen und nicht zufällig variieren.

## 7. Karten und Bereiche

Karten werden sparsam eingesetzt, um funktionale Bereiche zu gruppieren.

Sie verwenden:

- hellen Hintergrund,
- dezente Kontur,
- moderate Rundung,
- keine starken Schatten als Standard.

Pastellflächen dürfen verschiedene funktionale Bereiche unterscheiden,
sollen aber nicht jeden einzelnen Inhalt bunt einfärben.

## 8. Zustände und Feedback

Ein Zustand darf nicht nur durch Farbe dargestellt werden.

Beispiele:

- ausgewählter Zooplan: Farbe plus sichtbare Auswahlmarkierung,
- Spielerfarbe: Farbe plus Name/Position,
- deaktivierte Aktion: visuell deaktiviert und nicht nur andersfarbig.

Aktionen geben unmittelbar Feedback:

- Speichern während der Ausführung nicht mehrfach auslösen,
- Fehler möglichst nahe am betroffenen Kontext anzeigen,
- längere Aktionen zeigen einen Busy-Zustand.

## 9. Destruktive Aktionen

Entfernen, Verwerfen und Löschen sind weniger prominent als Speichern
oder Starten.

Irreversible bzw. weitreichende Aktionen erhalten eine Bestätigung, wenn
ein versehentliches Auslösen realistisch ist.

## 10. Plattformen

Android und iOS teilen dieselbe visuelle Sprache und
Informationsarchitektur.

Native Bedienerwartungen dürfen trotzdem respektiert werden. Pixelgenaue
Gleichheit zwischen beiden Plattformen ist kein Ziel.

## 11. Gruppen-Screens

### Gruppenübersicht

- `Neue Gruppe` ist kompakt.
- Jede Gruppenkarte besitzt rechts ein sichtbares Stift-Icon.
- Das Stift-Icon hat eine eigene Touch-Fläche.
- Das Antippen der Gruppenkarte und das Bearbeiten bleiben getrennte
  Aktionen.

### Gruppe anlegen / bearbeiten

Kopfzeile:

``` text
[←]  Gruppe anlegen  [i]              [Speichern]
```

bzw.

``` text
[←]  Gruppe bearbeiten  [i]           [Speichern]
```

Die Icons sind echte Bildressourcen und keine fontabhängigen
Unicode-Glyphen.

Mitglieder:

``` text
Gruppenmitglieder [i]

Anna                         ↑  ↓  ×
Ben                          ↑  ↓  ×
```

Der Erklärungstext zur Default-Reihenfolge steht hinter dem Info-Icon
und wird nicht dauerhaft angezeigt.

## 12. Review-Checkliste für neue Screens

Vor Abschluss eines Screens prüfen:

- Ist die Hauptaktion sofort erkennbar?
- Sind alle interaktiven Elemente sichtbar?
- Sind Touch-Ziele ausreichend groß?
- Gibt es eine Funktion, die nur durch Farbe verständlich ist?
- Sind Icons konsistent verwendet?
- Sind selten benötigte Erklärungen aus dem Hauptfluss entfernt?
- Ist der Screen auf 6,1 Zoll ohne unnötige Informationsdichte
  bedienbar?
- Sind destruktive Aktionen klar von normalen Aktionen getrennt?
- Sind Abstände und Typografie konsistent?
- Funktioniert die Interaktion sowohl per Touch als auch – wo sinnvoll –
  mit Pointer/Tooltip?

------------------------------------------------------------------------

## 13. Busy-Zustände und Mehrfachtaps

Eine asynchrone Aktion wird nach dem ersten Tap sofort gegen erneutes
Auslösen geschützt.

- Während einer laufenden Navigation darf dieselbe Navigation nicht
  erneut auf den Stack gelegt werden.
- Bei wahrnehmbaren Ladezeiten erscheint ein `ActivityIndicator`.
- Während kritischer Lade-/Speicheraktionen blockiert ein dezentes
  Overlay weitere Eingaben.
- Ein deaktivierter Button allein genügt nur bei praktisch sofortigen
  Aktionen.
- Nach Abschluss oder Fehler wird die Interaktion wieder freigegeben.

### Empty States

Nicht verfügbare Bedienelemente sollen nicht ohne Nutzen angezeigt
werden. Gibt es beim Anlegen einer Gruppe keine weitere vorhandene
Person, verschwinden Picker und Hinzufügen-Button vollständig.
Stattdessen steht dezent: `Keine weiteren Personen vorhanden.`

### Gelb

Das gelbe Oberflächen-Token ist bewusst klarer gelb und weniger beige
als in v0.6.1. Gruppenkarten dürfen dieses sanfte Gelb als
wiederkehrende Identitätsfläche verwenden.

------------------------------------------------------------------------

## 14. Farbsemantik v0.6.4

- **Grün** ist die verbindliche Farbe für bestätigende Hauptaktionen wie
  `Speichern`, `Starten` oder vergleichbare Abschlüsse.
- **PaleGoldenrod** ist das Gruppen-/Auswahlgelb und wird für
  Gruppenkarten sowie passende Gruppenflächen verwendet.
- Die Farbe einer Aktion folgt ihrer Bedeutung; ein Bestätigen soll
  nicht nur aufgrund des aktuellen Screens gelb werden.

---

## 15. Vorbereitungsscreen und Farbfeinschliff v0.7.0

- Bestätigende Primäraktionen verwenden vorläufig `YellowGreen` statt eines kräftigen dunklen Grüns.
- `PaleGoldenrod` bleibt die visuelle Gruppenfarbe.
- Sekundäre Auswahlbereiche wie `Vorhandene Person hinzufügen` verwenden wieder `SurfaceBlue`.
- Listenfilter mit wenigen binären Optionen werden kompakt untereinander dargestellt (`Temporär`, `Archiviert`).
- In der Partievorbereitung ist die gesamte Spielerkarte leicht in der tatsächlichen Spielerfarbe getönt; Farbe bleibt zusätzlich als Text sichtbar.
- Zooplan-Karten verwenden die abgestimmten monochromen Bleistiftskizzen. Nummer und Name bleiben zusätzlich sichtbar, damit die Information nicht nur über das Bild vermittelt wird.
