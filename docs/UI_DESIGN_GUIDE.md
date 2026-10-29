# Boardgame Tracker – UI Design Guide

## 1. Grundprinzip

Die App verwendet ein zentrales, tokenbasiertes Farbsystem.

Die visuelle Struktur eines Screens bleibt unabhängig vom gewählten Theme gleich. Ein Theme ändert Farben, aber nicht Navigation, Abstände, Größen, Informationshierarchie oder fachliches Verhalten.

Der Nutzer kann zwischen drei Themes wählen:

1. **Deep Forest**
2. **Königsblau**
3. **Dunkel**

Die Theme-Auswahl gehört in die allgemeinen App-Einstellungen und wird dauerhaft gespeichert.

**Deep Forest ist das Standard-Theme.**

Spielbezogene Farben und Grafiken bleiben vom App-Theme getrennt. Insbesondere werden nicht verändert:

- Arche-Nova-Spielerfarben
- farblich getönte Spielerkarten
- Zooplan-Grafiken
- Spiel-Logos und Illustrationen
- fachlich notwendige Status- und Warnfarben

Dadurch bleibt die App-Oberfläche ruhig, während die eigentlichen Brettspielinhalte Farbe und Persönlichkeit liefern.

---

## 2. Gemeinsame Farbrollen

Screens dürfen keine eigenen unabhängigen Farbpaletten definieren.

Stattdessen verwenden alle Oberflächen semantische Theme-Tokens.

Verbindliche Rollen:

- `AppBackground`
- `Surface`
- `SurfaceSecondary`
- `Primary`
- `PrimaryPressed`
- `PrimaryForeground`
- `SelectedSurface`
- `SelectedBorder`
- `TextPrimary`
- `TextSecondary`
- `Outline`
- `NavigationBackground`
- `NavigationText`
- `NavigationSelectedSurface`
- `NavigationSelectedText`
- `Danger`
- `DangerSurface`

Bestehende Komponenten sollen diese semantischen Rollen verwenden und nicht konkrete Theme-Farben kennen.

---

## 3. Theme: Deep Forest

Deep Forest ist das Standard-Theme.

Es soll ruhig, natürlich und hochwertig wirken, ohne einen beigen oder papierartigen Eindruck zu erzeugen.

| Rolle | Farbe |
|---|---|
| AppBackground | `#D9E6DF` |
| Surface | `#FFFFFF` |
| SurfaceSecondary | `#EEF5F1` (Sekundäraktion) |
| Primary | `#466655` |
| PrimaryPressed | `#385346` |
| PrimaryForeground | `#FFFFFF` |
| SelectedSurface | `#C7DCCF` |
| SelectedBorder | `#789A85` |
| TextPrimary | `#263D31` |
| TextSecondary | `#68776F` |
| Outline | `#B8CAC0` |
| NavigationBackground | `#FFFFFF` |
| NavigationText | `#68776F` |
| NavigationSelectedSurface | `#C7DCCF` |
| NavigationSelectedText | `#263D31` |
| Danger | `#B3261E` |
| DangerSurface | `#F9DEDC` |

Deep Forest soll nicht durch zusätzliche gelbe oder beige Standardflächen ergänzt werden.

---

## 4. Theme: Königsblau

Königsblau ist das alternative helle Theme.

Es soll etwas klarer, kühler und digitaler wirken als Deep Forest, ohne kräftig oder technisch steril zu werden.

| Rolle | Farbe |
|---|---|
| AppBackground | `#DCE5F4` |
| Surface | `#FFFFFF` |
| SurfaceSecondary | `#EEF3FB` (Sekundäraktion) |
| Primary | `#315B9B` |
| PrimaryPressed | `#274A80` |
| PrimaryForeground | `#FFFFFF` |
| SelectedSurface | `#C8D8F1` |
| SelectedBorder | `#708EBE` |
| TextPrimary | `#18386A` |
| TextSecondary | `#66758E` |
| Outline | `#BCCAE1` |
| NavigationBackground | `#FFFFFF` |
| NavigationText | `#65748D` |
| NavigationSelectedSurface | `#C8D8F1` |
| NavigationSelectedText | `#18386A` |
| Danger | `#B3261E` |
| DangerSurface | `#F9DEDC` |

Auch in diesem Theme bleiben Spielerfarben, Zoopläne und sonstige Spielgrafiken unverändert.

---

## 5. Theme: Dunkel

Das dunkle Theme ist ein eigenständiges Theme und keine einfache automatische Invertierung der hellen Farben.

| Rolle | Farbe |
|---|---|
| AppBackground | `#141A17` |
| Surface | `#222A26` |
| SurfaceSecondary | `#29352F` (Sekundäraktion) |
| Primary | `#5B8A70` |
| PrimaryPressed | `#6D9B82` |
| PrimaryForeground | `#101713` |
| SelectedSurface | `#355244` |
| SelectedBorder | `#668875` |
| TextPrimary | `#F0F4F1` |
| TextSecondary | `#C6D0CA` |
| Outline | `#3B4942` |
| NavigationBackground | `#222A26` |
| NavigationText | `#AEBBB4` |
| NavigationSelectedSurface | `#355244` |
| NavigationSelectedText | `#FFFFFF` |
| Danger | `#FFB4AB` |
| DangerSurface | `#5F2423` |

Das dunkle Theme verwendet keine rein schwarzen großen Flächen und keine rein weißen Konturen.

---

## 6. Flächenhierarchie

Die App verwendet grundsätzlich drei visuelle Ebenen:

### Ebene 1 – AppBackground

Große Grundfläche eines Screens.

Sie darf deutlich sichtbar sein und soll nicht wie eine weiße Standard-App wirken.

### Ebene 2 – Surface

Interaktive oder inhaltlich abgeschlossene Bereiche:

- Partiekarten
- Gruppenkarten
- Einstellungskarten
- einzelne Inhalts-/Auswahleinträge innerhalb von Dialogen; äußere SelectionDialog-Flächen verwenden AppBackground
- Bottom-Navigation
- Eingabebereiche, wenn eine klare Gruppierung erforderlich ist

### Ebene 3 – SelectedSurface

Für:

- ausgewählte Filter
- aktiven Navigationseintrag
- ausdrücklich ausgewählte Zustände

Farbe soll nicht verwendet werden, nur um möglichst viele Flächen voneinander zu unterscheiden.

---

## 7. Karten, Formulare und Metainformation

**Karten sind Inhalt, Buttons sind Aktionen, Formulare bleiben flach und Metainformation steht ohne Karte.**

Surface-Karten sind eigenständige inhaltliche oder interaktive Einheiten, z. B. Gruppen und historische Partien. Sie verwenden eine moderate Rundung und Outline ohne starken Standardschatten. Eine Karte ist kein universeller Gruppierungsrahmen für Überschrift und Text oder einzelne Formularsektionen.

Gruppenstammdaten und „Neuen Spieler anlegen“ sind flache Inline-Formulare. Gruppenname und Spielername verwenden dasselbe Label-links/Eingabe-rechts-Muster mit gleicher Typografie und Eingabehöhe. „Letzte Partie“ ist ein flacher Metabereich mit kleiner Überschrift, Datum und Ergebnis. Leere Gruppenmitglieder benötigen keine Karte.

Farbige Spielerflächen sind eine bewusste Identitäts-Ausnahme: Ihre gekoppelte Hintergrund-/Textfarbe bleibt erhalten. Eingefügte Gruppenmitglieder können als eigenständige interaktive Einheiten abgegrenzt bleiben.

Sektionen werden zuerst durch Abstand, Überschrift und Ausrichtung getrennt; Divider nur gezielt. Detail-/Bearbeitungsheader bleiben auf AppBackground und erhalten eine feine Outline-Unterkante, keine Header-Karte oder starken Schatten.

Die Regeln gelten für alle aktuellen Seiten: Jeder Seitenheader erhält eine feine Unterkante über den gemeinsamen Style `HeaderDivider`. Gruppen anlegen/bearbeiten und Wertungseingaben verwenden flache Formulare; Einstellungs-, Ergebnis- und interaktive Spielerkarten bleiben eigenständige Inhaltseinheiten. Leerzustände stehen ohne dekorative Karte.

---

## 8. Bottom-Navigation

Die Hauptnavigation enthält weiterhin:

`Spiele · Gruppen · Historie · Einstellungen`

Die Bottom-Bar verwendet grundsätzlich dieselbe Farbe wie `Surface`.

Damit bildet sie dieselbe visuelle Ebene wie Karten und andere feste Oberflächen.

Sie wird vom App-Inhalt abgegrenzt durch:

- eine sichtbare obere Kontur in `Outline`,
- optional einen sehr zurückhaltenden Schatten nach oben,
- eine eindeutige Markierung des aktiven Tabs.

Der aktive Tab erhält:

- `NavigationSelectedSurface`,
- `NavigationSelectedText`,
- eine gemeinsame abgerundete Fläche hinter Icon **und Beschriftung**, nicht nur hinter dem Icon. Die Fläche verwendet `NavigationSelectedSurface` ohne abweichende Randfarbe; die obere Kontur der gesamten TabBar bleibt `Outline`.

Die aktive Navigation darf nicht ausschließlich anhand einer anderen Textfarbe erkennbar sein.

---

## 9. Filter und Auswahl

Kurze Filter sollen bevorzugt als kompakte Filterflächen bzw. Chips dargestellt werden.

Beispiele:

`[ Arche Nova ▾ ] [ Alle Gruppen ▾ ]`

Ein aktiver oder bewusst gewählter Filter darf zusätzlich eine Auswahlmarkierung erhalten:

`[ ✓ Arche Nova ▾ ]`

Filter:

- sind nicht standardmäßig vollbreit,
- stehen bei ausreichendem Platz nebeneinander,
- besitzen komfortable Touch-Ziele,
- verwenden `Surface` im normalen Zustand,
- verwenden `SelectedSurface` und `SelectedBorder` im ausgewählten Zustand.

Die eigentliche Auswahl darf weiterhin über Picker, Popup, Dialog oder eine geeignete native Auswahloberfläche erfolgen.

Lange Listen sollen später bei Bedarf über eine suchbare Auswahl oder ein Bottom Sheet dargestellt werden.

---

## 10. Historie

Abgeschlossene Partien werden nach Monaten gruppiert.

Beispiel:

`Oktober 2026`

darunter die entsprechenden Partiekarten.

Die Monatsüberschrift verwendet keine eigene Karte.

Die Historienfilter für Spiel und Gruppe stehen kompakt oberhalb der Liste.

Ob Monatsüberschriften später beim Scrollen sticky werden, ist keine Voraussetzung für den ersten Design-Umbau.

---

## 11. Primary, Secondary und Text Actions

- **PrimaryCompactButton:** wichtigste Seitenaktion, gefüllt mit Primary und PrimaryForeground, Pressed über PrimaryPressed. „Neue Gruppe“, „Speichern“ und „Starten“ sind Primary-Aktionen.
- **SecondaryActionButton:** echte nachgeordnete Aktion mit leichter SurfaceSecondary-Fläche, SelectedBorder-Kontur und kontrastreichem TextPrimary. Dazu gehören „Zoopläne auslosen“, „Reihenfolge rotieren“, „Vorhandenen Spieler hinzufügen“ und „Spieler hinzufügen“. Die Aktionsfläche unterscheidet sich von weißen Inhaltskarten.
- **TextActionButton:** kleine kontextuelle Aktion ohne starke Fläche, transparent ohne Kontur, z. B. Schließen oder Abbrechen einer Auswahl.

Die Styles teilen Höhe (mindestens 44 dp), Radiusfamilie und Typografie. Disabled-Zustände bleiben technisch deaktiviert und visuell erkennbar. Normale Buttons verwenden standardmäßig SecondaryActionButton; kontextuelle Reihenfolgeaktionen und Schließen verwenden TextActionButton. CompactActionButton dient nur als gemeinsame Stilbasis, nicht als zusätzliche Aktionsrolle. Destruktive Aktionen bleiben sekundär und verwenden Danger.

---

## 12. Farbsemantik

Hauptaktionen verwenden `Primary` und `PrimaryForeground`, unabhängig vom konkreten Farbton. Gefahr/Löschen verwendet `Danger` und `DangerSurface`.

---

## 13. Theme-Auswahl

In den allgemeinen Einstellungen wird **Darstellung / Theme** angeboten.

Optionen:

- Deep Forest
- Königsblau
- Dunkel

Deep Forest ist der Standard für neue Installationen bzw. wenn noch keine Auswahl gespeichert ist.

Ein Theme-Wechsel aktualisiert die zentralen Ressourcen und wird ohne App-Neustart sichtbar.

Die Auswahl wird dauerhaft gespeichert.

Das Theme ist eine App-Einstellung und keine Einstellung eines einzelnen Spiels.

---

## 14. Accessibility und Kontrast

Ein Zustand darf nie ausschließlich anhand einer Farbe vermittelt werden.

Dies gilt insbesondere für:

- aktive Navigation
- ausgewählte Filter
- ausgewählte Zoopläne
- Spielerfarben
- Disabled-Zustände
- Fehlerzustände

Text- und UI-Kontraste sollen für alle drei Themes geprüft werden.

Insbesondere der Dunkelmodus darf nicht lediglich durch Austausch von Hintergrundfarben entstehen; Text, Konturen, Disabled-Zustände, Icons und destruktive Aktionen müssen separat geprüft werden.

---

## 15. Fachliche Spielgestaltung

Theme-Wechsel verändern folgende fachliche Inhalte nicht:

- App-Logo
- Zooplan-Grafiken
- Arche-Nova-Spielerfarben
- leichte Färbung der Spielerkarten anhand der Spielerfarbe
- kompakte Wertungsdarstellung
- fachlich benötigte Symbole und Spielgrafiken

Das Theme bildet den neutralen Rahmen um diese Inhalte.

---

## 16. Technische Leitlinie

Themes sollen zentral definiert werden.

Kein Screen soll anhand des Theme-Namens selbst entscheiden:

`if Forest ...`
`if Blue ...`
`if Dark ...`

Stattdessen greifen Screens ausschließlich auf semantische Ressourcen wie `AppBackground`, `Surface`, `Primary`, `TextPrimary` usw. zu.

Die Theme-Umschaltung ersetzt bzw. aktualisiert diese Ressourcen zentral.

Neue Screens müssen automatisch mit allen Themes funktionieren, wenn sie ausschließlich die semantischen Ressourcen verwenden.

## Bedienung und Layout

- Zielgerät: 6,1 Zoll, Hochformat, 1170 × 2532 px. Ruhige Informationshierarchie und einhändige Bedienung haben Vorrang vor Dekoration.
- Abstände bevorzugt 4 / 8 / 12 / 16 / 20 / 24 / 32 dp. Moderate Rundungen, keine starken Standardschatten.
- Icons ca. 20–24 dp, Touch-Ziele mindestens 44–48 dp. Kleine Symbole behalten komfortable interaktive Flächen und semantische Beschreibungen.
- Zurück, Info, Bearbeiten und Farbpalette behalten ihre Bedeutung; Bildressourcen statt fontabhängiger Unicode-Icons. Hinweise über Info-Aktionen, bei Pointer zusätzlich Tooltip.
- Gruppenkarte und Bearbeiten haben getrennte Touch-Ziele. Gruppenerstellung priorisiert vorhandene Spieler; leere Auswahl verschwindet zugunsten eines Leerzustands.
- Asynchrone Navigation und Speicherung gegen Mehrfachtaps schützen. Busy-Overlays blockieren Eingaben und zeigen einen Indikator. Fehlermeldungen nur bei vorhandenem Text anzeigen.
- Destruktive Aktionen sekundär gestalten und bei weitreichenden Folgen ausdrücklich bestätigen.
- Wertungskarten reservieren eigene Zeilen für Name/Punkte, Zooplan, Rohwerte und Karten/Ruf. Fehlende Werte als „–“. Karten-Icon ohne Zähler; Kartenanzahl in der Informationszeile.
- Zooplanauswahl markiert die gesamte Kachel mit `SelectedBorder` und einer sichtbaren Auswahlmarkierung; Skizze und Nummer bleiben fachliche Inhalte.
- In deutschen sichtbaren Texten konsequent „Spieler“ verwenden; technische Typen und Ressourcenschlüssel behalten `Player`.
- Alle sichtbaren Sprachtexte einschließlich Einstellungen, Ergebnisüberschriften und Abschlussbuttons führen über die bestehende Lokalisierung. Android und iOS teilen die Informationsarchitektur; native Bedienerwartungen bleiben erhalten. Bekannte Codeabweichungen stehen im Wireframe.

## Zusätzliche semantische Rollen

`DisabledSurface` und `DisabledText` kennzeichnen nicht bedienbare Aktionen; reduzierte Opazität unterstützt die technische Deaktivierung. `BusyOverlay` ist eine abgedunkelte Sperrfläche, `IconSurface` eine helle Unterlage ausschließlich für eingebettete Grafik-/Farbwahlfelder. Normale Info-, Zurück- und Bearbeiten-Iconbuttons sind transparent und verwenden den Theme-Textton als Icon-Tint; auf Spielerflächen gilt die Spieler-Textfarbe. Das Statussymbol für eine offene Partie auf Gruppenkarten ist ebenfalls transparent und verwendet TextPrimary als dynamischen Icon-Tint; es besitzt keine eigene grüne Fläche. `GameGraphicText` ist ein fester dunkler Textton ausschließlich auf hellen Zooplan-Grafikbadges. Spielerkarten verwenden ihre eigene Textfarbe aus der globalen Spielerfarben-Palette. `BusyIndicator` ist ein heller Indikator auf dem abgedunkelten Busy-Overlay. Disabled-Text verwendet den kontrastreichen Primärtextton; Opazität und die deaktivierte Interaktion kennzeichnen den Zustand.

Sekundärtext ist für Surface-Flächen bestimmt. Kleine Texte direkt auf `AppBackground` verwenden `TextPrimary`, da die verbindlichen hellen Paletten dort für `TextSecondary` unter 4,5:1 liegen. Spielerkarten verwenden unabhängig vom App-Theme ihre gekoppelte Spieler-Textfarbe.


## Spielerfarben: fachliche Kategorie und UI-Darstellung

Physische/fachliche Farben werden durch gemeinsame semantische `PlayerColor`-Werte wie Rot oder Blau beschrieben. Jedes Spiel definiert die erlaubte Teilmenge. Die globale UI-Palette ist ausschließlich Presentation in der App-Schicht und ordnet dem gemeinsamen Farbwert Hintergrund und Textfarbe gemeinsam zu. Core kennt weder MAUI-Farben noch Theme-Ressourcen. Ein späterer Erkennungspfad lautet `physische Farbe → gemeinsame semantische Spielerfarbe → UI-Farbpaar`; UI-Hexwerte sind keine Kamera-Messwerte, Trainingsdaten oder RGB-/HSV-Schwellen.

| Spielerfarbe | UI-Hintergrund | UI-Text |
|---|---|---|
| Rot | `#D96B63` | `#312220` |
| Gelb | `#E7C84A` | `#403716` |
| Grün | `#73A66F` | `#243723` |
| Blau | `#6FA9D2` | `#253B4B` |
| Schwarz | `#555B58` | `#E4E9E6` |
| Weiß | `#F4F4F1` | `#39413D` |
| Orange | `#E59A45` | `#493117` |
| Braun | `#805B42` | `#F0E8E2` |
| Lila | `#70599E` | `#EEEAF4` |
| Pink | `#D985AD` | `#472838` |
| Türkis | `#6EB7B4` | `#264543` |
| Grau | `#9AA2A0` | `#323936` |

Diese Werte bleiben in Deep Forest, Königsblau und Dunkel identisch. Keine Neonfarben und keine reinen Schwarz-/Weiß-Extremwerte für Spielerflächen oder Spielertext. Weitere Varianten wie Lime, Navy oder Beige gehören nicht zum Basiskatalog.

Arche Nova erlaubt genau die gemeinsamen Farben Blau, Gelb, Rot und Schwarz. Es gibt keine separate Variante wie „Arche-Nova-Rot“. Weitere acht Farben werden dadurch nicht in Arche Nova freigeschaltet. Unbekannte Präsentationswerte fallen definiert auf Grau zurück. Namen werden separat lokalisiert, niemals aus Hexwerten abgeleitet.

Vollständig farbige Spielerkarten verwenden die zugehörige Textfarbe auch für Sekundärtext. Eingebettete Controls bleiben App-Chrome mit Theme-Surface, Outline und passenden Text-/Iconrollen. Helle Zooplan-Badges behalten dunklen Grafiktext, auch auf schwarzen Spielerkarten. Spielername, Position und/oder ausgeschriebener Farbname bleiben sichtbar; Farbe ist nie die einzige Identifikation.

Verbindliche Designregel: Jede Kombination aus globaler Spieler-Hintergrundfarbe und zugehöriger Spieler-Textfarbe muss für normalen Text mindestens **4,5:1 Kontrast** erreichen, unabhängig vom App-Theme. Die finale Palette wird automatisiert nach der relativen sRGB-Luminanz ohne Rundung der Prüfschwelle getestet. Braun und Lila verwenden bewusst dunklere Flächen, damit ihre weichen Off-White-Texte erhalten bleiben. Die UI-Hexwerte bleiben ausschließlich Darstellung und sind keine Grundlage für Kameraerkennung. Farbsehschwächen werden durch die Palette allein nicht abgedeckt; Namen und Reihenfolge bleiben notwendig.

Karten mit vergleichbarer Rolle (Gruppen, Historie) verwenden einheitlich `Surface`. Die letzte Partie ist Metainformation ohne Karte. `SurfaceSecondary` ist die leichte Aktionsfläche für Secondary Buttons, keine zusätzliche Kartenfarbe. Auch `DisabledSurface` verwendet `Surface`; Disabled-Zustände bleiben durch Opazität und deaktivierte Interaktion erkennbar. Filterchips öffnen die App-Auswahl direkt und benötigen keine unsichtbaren nativen Picker.

## Reduzierte UI-Flächen

Die Inhaltsflächen bleiben `AppBackground`, `Surface` und `SelectedSurface`; `SurfaceSecondary` ergänzt ausschließlich die visuelle Sprache sekundärer Aktionen. Primary-/Danger-Rollen, Konturen, Textfarben, Sperroverlays und fachliche Spielgrafiken sind davon getrennt. Die Auswahl besonderer Karten verwendet `AppBackground` für die äußere Dialogfläche und den Listenhintergrund sowie `Surface` für einzelne Karten; nur bereits diesem Spieler zugeordnete Karten erhalten `SelectedSurface` und `SelectedBorder`. Die Zuordnung bleibt zusätzlich als Text sichtbar. Nicht zugeordnete oder anderen Spielern zugeordnete Karten erhalten keine zusätzliche Flächenfarbe.

Vorhandene Spieler werden über ein einheitliches App-Auswahlfenster gewählt: AppBackground außen, Surface-Einträge, Outline, TextPrimary und dieselbe modale Sperrfläche wie bei besonderen Karten. Ein Tap auf den Eintrag fügt unmittelbar hinzu und schließt; kein nativer Picker plus zusätzlichem Bestätigungsschritt. Schließen oder System-Zurück bricht nur die Auswahl ab. Namen und Gaststatus bleiben sichtbar.

## Dialogabdeckung

Die appinternen Auswahlen verwenden zentral `SelectionDialog` und `TextActionButton`
zum Schließen. Informationen, Fehler und Sicherheitsabfragen sowie die Auswahl von
Historienfiltern, Theme und Standardspiel werden über `AppDialogPage` dargestellt.
Die modale Fläche verwendet AppBackground, Outline und TextPrimary; Einträge liegen
auf Surface, die aktuelle Auswahl erhält SelectedSurface, SelectedBorder und ein
sichtbares Häkchen. Längere Inhalte sind scrollbar. Bestätigungen verwenden eine
Primäraktion bzw. bei Verwerfen/Löschen DangerButton und eine getrennte Textaktion
zum Abbrechen. System-Zurück bricht ausschließlich den Dialog ab, ohne eine Auswahl
oder destruktive Aktion zu bestätigen. Ein Dialog verändert keine ungespeicherten
Formular- oder Wertungsentwürfe der darunterliegenden Seite.
Der aktuelle Dialogbestand und der Stand der Geräteprüfung stehen in `UI_WIREFRAME.md`.

## Splashscreen und App-Start

Die native Startfläche verwendet unabhängig von der gespeicherten Darstellung fest den dunklen AppBackground `#141A17`, damit beim Öffnen keine unerwartet helle Startfläche erscheint. Das vorhandene Splash-Logo bleibt erhalten. Der Betriebssystem-Splash wird vor dem Auslesen der App-Einstellungen erzeugt und verwendet keine MAUI-DynamicResources. Die anschließende Ladeansicht und App verwenden sofort die gespeicherte Darstellung; bei fehlender Auswahl bleibt Deep Forest das Standard-Theme.

## Farbauswahl in Gruppenformularen

Gruppe anlegen und Gruppe bearbeiten bieten je Gruppenmitglied denselben transparenten Palettenbutton und den ausgeschriebenen Farbstatus, sofern das aktuelle Spiel Farben erlaubt. Ohne erlaubte Farben entfallen Button und Farbstatus vollständig.

Der gemeinsame modale Dialog verwendet `SelectionDialog`: AppBackground außen, Surface-Einträge, SelectedSurface/SelectedBorder und ein sichtbares Häkchen für die Auswahl. Er nennt ausdrücklich das Spiel, z. B. „Farbe für Arche Nova festlegen“, sowie den betroffenen Spieler. Die Einträge zeigen Farbfeld und lokalisierten Namen; nur die erlaubten Farben und „Keine Farbe“ stehen zur Auswahl. Ein Tap übernimmt die Farbe in den Formularentwurf und schließt. Bei einer bereits vergebenen Farbe werden die bisherigen Farben getauscht; kein doppelter Gruppendefault entsteht. Schließen/System-Zurück ändert keine Auswahl. Erst Speichern persistiert die Gruppe samt Defaults atomar.

Spielerkacheln in Gruppe anlegen und bearbeiten übernehmen eine zugewiesene Defaultfarbe unmittelbar aus der gemeinsamen Spielerpalette. Name, Farbname, Gaststatus, Palettenicon und Reihenfolge-/Entfernen-Aktionen verwenden auf der getönten Fläche die gekoppelte Spieler-Textfarbe. Ohne Zuweisung bleibt die Kachel auf dynamischem Surface mit Theme-Text; das Entfernen bleibt dort Danger. Nach Entfernen oder Tauschen einer Farbe aktualisieren sich Fläche und Texte sofort. Hoch-/Runter-/Entfernen-Symbole verwenden 26 sp und mindestens 48 × 48 dp Touch-Fläche; der Palettenbutton besitzt ebenfalls 48 × 48 dp.

## Begrenzte numerische Wertungseingaben

Zahlenräder und zugehörige direkt editierbare Felder bilden je eine zusammengehörige
Spalte mit derselben Breite und CardBorder-Rundung. Die Eingabe bleibt auf ihrer
gerundeten Surface transparent. Konturen und Texte verwenden die Theme-Rollen;
keine Spielerfarben oder eigenen Akzentfarben für diese Controls.
Der Ruf-Slider nutzt Primary für Griff/aktive Spur und Outline für die inaktive Spur.
Vorschläge, Auswahl und Fehler müssen zusätzlich als Status erkennbar bleiben.
Bedienhinweise gehören hinter Info, der Inhalt bleibt bei Tastatur/großer Schrift
scrollbar. Verbindliche Screenmaße und Anordnung stehen ausschließlich im
`UI_WIREFRAME.md`, Abschnitt „Spielereingabe“; fachliche Eingaberegeln in `PRODUCT_SPEC.md`.

## Verwaltbare Arche-Nova-Spezialkarten

Kartenansichten bestehen aus den vom Benutzer gelieferten Einzelgrafiken unter
`Resources/Images/ark_symbol_*.png` und dem aktuellen gespeicherten Namen.
Quellgrafiken: `Icons/Symbole` im übergeordneten Projektordner. Reihenfolge und
Wiederholungen werden unverändert gezeigt: Symbol1 → Symbol2 → Symbol3 → Name.
Die 14 Symbole und ihre Grafiken sind fachliche Identität und werden nicht durch
Theme-Tints ersetzt. Kontur, Hintergrund und Text verwenden semantische Theme-Rollen.
Karten auf farbigen Ergebnisflächen erhalten eine neutrale Surface-Unterlage.

Verwaltung und Editor verwenden HeaderDivider, CardBorder, Primary für Neu/Speichern
sowie transparente Bearbeiten-Icons und Textaktionen für Archivieren/Entarchivieren.
Archivieren ist reversibel; keine Danger-Löschaktion. Archivzustand zusätzlich als
Text anzeigen. Der Symbolwähler folgt SelectionDialog mit grafischen, mindestens
48 dp hohen Einträgen und einer Auswahlmarkierung zusätzlich zur Farbe. Namen
können umbrechen; die Anordnung bleibt auf dem 6,1-Zoll-Zielgerät kompakt und scrollbar.
