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
| Pastell-Gelb    | `PaleGoldenrod` | Gruppen-/Auswahlflächen         |
| Pastell-Violett | `#F1ECF7` | spätere ergänzende Akzente      |
| Primär-Akzent   | `YellowGreen` | bestätigende Hauptaktion        |
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

Spieler:

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
werden. Gibt es beim Anlegen einer Gruppe keine weitere vorhandenen
Spieler, verschwinden Picker und Hinzufügen-Button vollständig.
Stattdessen steht dezent: `Keine weiteren Spieler vorhanden.`

### Gelb

Das gelbe Oberflächen-Token ist bewusst klarer gelb und weniger beige
als in v0.6.1. Gruppenkarten dürfen dieses sanfte Gelb als
wiederkehrende Identitätsfläche verwenden.

------------------------------------------------------------------------

## 14. Farbsemantik

- **Grün** ist die verbindliche Farbe für bestätigende Hauptaktionen wie
  `Speichern`, `Starten` oder vergleichbare Abschlüsse.
- **PaleGoldenrod** ist das Gruppen-/Auswahlgelb und wird für
  Gruppenkarten sowie passende Gruppenflächen verwendet.
- Die Farbe einer Aktion folgt ihrer Bedeutung; ein Bestätigen soll
  nicht nur aufgrund des aktuellen Screens gelb werden.

---

## 15. Vorbereitungsscreen und Farbfeinschliff

- Bestätigende Primäraktionen verwenden vorläufig `YellowGreen` statt eines kräftigen dunklen Grüns.
- `PaleGoldenrod` bleibt die visuelle Gruppenfarbe.
- Sekundäre Auswahlbereiche wie `Vorhandene Spieler hinzufügen` verwenden wieder `SurfaceBlue`.
- Die beiden kurzen Gruppenlistenfilter `Temporär` und `Archiviert` stehen kompakt nebeneinander, solange sie auf dem Referenzgerät ohne Enge darstellbar sind.
- In der Partievorbereitung ist die gesamte Spielerkarte leicht in der tatsächlichen Spielerfarbe getönt; Farbe bleibt zusätzlich als Text sichtbar.
- Zooplan-Karten verwenden die abgestimmten monochromen Bleistiftskizzen. In der Vorbereitung bleibt die Plannummer sichtbar; der Planname wird dort zugunsten der kompakten Darstellung nicht zusätzlich eingeblendet.


------------------------------------------------------------------------

## 16. Bedingte Meldungen und Zooplan-Auswahl

Bedingte Status- und Fehlermeldungen dürfen keinen leeren Layoutbereich
reservieren. Ist kein Text vorhanden, wird das zugehörige UI-Element
komplett ausgeblendet (`IsVisible = false` bzw. äquivalentes Verhalten).

Für die Zooplanauswahl gilt:

- die helle/transluzente Zooplan-Kachel bleibt als gemeinsame Fläche für
  Bild und Plannummer erhalten,
- der grüne Auswahlrahmen umfasst die **gesamte Kachel**,
- ohne Auswahl existiert kein grüner Rahmen,
- das Bild selbst besitzt keinen zusätzlichen Auswahlrahmen.

Farbwähler bleiben kompakt, ihre Touch-Flächen sollen trotzdem die bereits
festgelegten 44–48 dp erreichen. Da die Farbfelder visuell nur aus Farbe
bestehen, sollen sie bei der späteren Accessibility-Runde zusätzlich einen
semantischen Namen wie `Blau`, `Gelb`, `Rot` oder `Schwarz` erhalten.

------------------------------------------------------------------------

## 17. Touch-Ziele und bedingte Zustände

Die allgemeine Touch-Richtlinie von 44–48 dp gilt auch für kompakte Aktionen
und reine Farbauswahlflächen. Die sichtbare Farbfläche darf optisch klein
bleiben, die interaktive Fläche selbst soll jedoch mindestens 44 dp erreichen.

Fehlerlabels auf Gruppen-, Vorbereitungs- und Running-Screens werden nur dann
in das Layout aufgenommen, wenn tatsächlich ein Fehlertext vorhanden ist.
Dadurch entstehen keine leeren reservierten Fehlerzeilen.

Sichtbare Texte des Partien-Flows werden über die App-Lokalisierungsressourcen
bereitgestellt. Farbname und Farbton sind gemeinsame Präsentationslogik der
App; die Domain-Farbe selbst bleibt frei von MAUI- und Lokalisierungswissen.

### Accessibility

Reine Icon- und Farbaktionen erhalten zusätzlich zu Tooltip bzw. visueller
Darstellung eine semantische Beschreibung. Das gilt insbesondere für Zurück,
Information, Bearbeiten, Reihenfolge-Aktionen, Entfernen, Farbpalette und die
einzelnen Spielerfarben. Eine Funktion soll damit weder nur durch ein Symbol
noch nur durch Farbe verständlich sein.


---

## 18. Wertung und Tabs

- Die Tab-Reihenfolge lautet `Spiele`, `Gruppen`, `Historie`, `Einstellungen`.
- Der Gruppen-Tab ist ein Root-Ziel und soll keine alte Detailnavigation wiederherstellen.
- Navigation aus antippbaren Wertungskacheln wird gegen schnelle Mehrfachtaps geschützt.
- Spezialkarten verwenden in der Übersicht einen kompakten Bildbutton mit komfortabler
  Touch-Fläche; mehrere Karten werden dort nur als Anzahl dargestellt.
- Berechnete Punkte gehören bei vorhandenen Pflichtwerten direkt in die primäre
  Spielerzeile.


---

## 19. UI-Cleanup

- Deaktivierte bestätigende Primäraktionen erhalten appweit denselben klar erkennbaren Disabled-Zustand; technisch deaktiviert allein genügt nicht.
- Mehrzeilige Spielerkacheln reservieren für jede Informationszeile eine eigene Layoutzeile; Status- und Werteinformationen dürfen sich nicht überlagern.
- Fehlende Wertungswerte werden direkt mit `–` gezeigt, statt zusätzlich einen konkurrierenden Text wie „Wertung nicht vollständig“ einzublenden.
- Selten benötigte Erläuterungen auf Hauptübersichten werden über ein Info-Icon angeboten und nicht dauerhaft zwischen Arbeitsinhalten angezeigt.
