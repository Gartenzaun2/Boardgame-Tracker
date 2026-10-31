# Boardgame Tracker – UI Design Guide

## 1. Geltungsbereich

Diese Datei beschreibt die **spielübergreifende visuelle Sprache und
Interaktionskonventionen** der App.

Spielspezifische UI-Regeln stehen unter:

```text
docs/Games/<GameName>/UI_DESIGN_GUIDE.md
```

Konkrete Screens, Reihenfolgen, Navigation und Maße eines Screens stehen in
[UI_WIREFRAME.md](UI_WIREFRAME.md). Fachliches Verhalten steht in
[PRODUCT_SPEC.md](PRODUCT_SPEC.md).

Grundprinzip:

> Das App-Theme bildet einen ruhigen, konsistenten Rahmen. Spielmaterial,
> Spieleridentität und andere fachliche Visuals behalten ihre eigene Semantik.

Ein Theme ändert Farben, aber nicht Navigation, Abstände, Größen,
Informationshierarchie oder fachliches Verhalten.

## 2. Themes

Die App unterstützt:

1. **Deep Forest**
2. **Königsblau**
3. **Dunkel**

**Dunkel ist das Standard-Theme**, wenn keine gültige Auswahl gespeichert ist.
Die gespeicherte Nutzerwahl wird vor Aufbau der ersten App-Ansicht angewendet.

Der Theme-Wechsel:

- aktualisiert zentrale semantische Ressourcen;
- wird ohne App-Neustart sichtbar;
- wird als allgemeine App-Preference gespeichert;
- ist keine Einstellung eines einzelnen Spiels.

Die native Splashfläche verwendet unabhängig von der gespeicherten Auswahl
`#141A17`, da sie vor dem Laden der MAUI-Preferences entsteht. Die anschließende
Ladeansicht verwendet sofort das gespeicherte bzw. standardmäßig dunkle Theme.

## 3. Semantische Theme-Rollen

Screens definieren keine eigenen Theme-Paletten und entscheiden nicht selbst anhand
eines Theme-Namens.

Verbindliche allgemeine Rollen:

```text
AppBackground
Surface
SurfaceSecondary
Primary
PrimaryPressed
PrimaryForeground
SelectedSurface
SelectedBorder
TextPrimary
TextSecondary
Outline
NavigationBackground
NavigationText
NavigationSelectedSurface
NavigationSelectedText
Danger
DangerSurface
DisabledSurface
DisabledText
BusyOverlay
IconSurface
GameGraphicText
BusyIndicator
```

Features dürfen zusätzliche semantische Statusrollen verwenden, wenn diese einen
eigenständigen fachlichen Zustand ausdrücken. Solche Rollen werden zentral
bereitgestellt, aber im jeweiligen Feature-/Spiel-Guide erklärt.

Neue Screens müssen mit allen Themes funktionieren, wenn sie ausschließlich
semantische Ressourcen und ausdrücklich fachliche Farben verwenden.

## 4. Theme-Paletten

### Deep Forest

| Rolle | Farbe |
|---|---|
| AppBackground | `#D9E6DF` |
| Surface | `#FFFFFF` |
| SurfaceSecondary | `#EEF5F1` |
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
| DisabledSurface | `#FFFFFF` |
| DisabledText | `#263D31` |
| BusyOverlay | `#99000000` |
| IconSurface | `#EEF5F1` |
| GameGraphicText | `#101713` |
| BusyIndicator | `#FFFFFF` |

Deep Forest wirkt ruhig und natürlich. Keine zusätzlichen gelben oder
papier-beigen Standardflächen.

### Königsblau

| Rolle | Farbe |
|---|---|
| AppBackground | `#DCE5F4` |
| Surface | `#FFFFFF` |
| SurfaceSecondary | `#EEF3FB` |
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
| DisabledSurface | `#FFFFFF` |
| DisabledText | `#18386A` |
| BusyOverlay | `#99000000` |
| IconSurface | `#EEF3FB` |
| GameGraphicText | `#101713` |
| BusyIndicator | `#FFFFFF` |

Königsblau ist das kühlere helle Theme, ohne kräftig oder technisch steril zu wirken.

### Dunkel

| Rolle | Farbe |
|---|---|
| AppBackground | `#141A17` |
| Surface | `#222A26` |
| SurfaceSecondary | `#29352F` |
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
| DisabledSurface | `#222A26` |
| DisabledText | `#F0F4F1` |
| BusyOverlay | `#99000000` |
| IconSurface | `#EEF5F1` |
| GameGraphicText | `#101713` |
| BusyIndicator | `#FFFFFF` |

Das dunkle Theme ist keine automatische Invertierung. Große Flächen sind nicht rein
schwarz; Konturen nicht rein weiß.

## 5. Flächenhierarchie

Die Inhaltsflächen verwenden drei Ebenen:

### AppBackground

Grundfläche eines Screens.

### Surface

Eigenständige inhaltliche oder interaktive Einheiten, beispielsweise:

- Gruppen- und Historienkarten;
- Einstellungskarten;
- Dialogeinträge;
- klar abgegrenzte interaktive Inhalte.

### SelectedSurface

Nur für einen tatsächlich ausgewählten Zustand, z. B.:

- aktiver Filter;
- aktive Navigation;
- ausgewählter Dialogeintrag.

`SurfaceSecondary` ist **keine vierte Kartenebene**, sondern die leichte Fläche
sekundärer Aktionen.

Sektionen werden primär durch Abstand, Überschrift und Ausrichtung getrennt.
Divider werden gezielt eingesetzt.

Detail-/Bearbeitungsheader bleiben auf `AppBackground` und verwenden eine feine
Unterkante über `HeaderDivider`, keine Header-Karte und keinen starken Schatten.

Leerzustände und reine Metainformation benötigen keine dekorative Karte.

## 6. Karten und Formulare

> Karten sind Inhalt, Buttons sind Aktionen, Formulare bleiben flach und
> Metainformation steht ohne Karte.

`CardBorder` ist die gemeinsame Surface-Karte mit moderater Rundung und Outline.

Formulare verwenden keine Karte nur zur Gruppierung einzelner Felder.
Gleichartige Felder verwenden gleichartige Typografie, Höhe und Ausrichtung.

Farbige Spielerflächen sind eine bewusste fachliche Identitätsausnahme. Ihre
gekoppelte Hintergrund-/Textfarbe bleibt unabhängig vom App-Theme erhalten.

## 7. Buttons und Aktionen

### PrimaryCompactButton

Wichtigste Seitenaktion:

- `Primary`;
- `PrimaryForeground`;
- Pressed über `PrimaryPressed`;
- mindestens 44 dp hoch.

### SecondaryActionButton

Normale nachgeordnete Aktion:

- `SurfaceSecondary`;
- `SelectedBorder`;
- `TextPrimary`.

Normale Buttons verwenden standardmäßig diese Rolle.

### TextActionButton

Kleine kontextuelle Aktion ohne starke Fläche und ohne Kontur, beispielsweise
Abbrechen oder Schließen eines passenden Dialogs.

### DangerButton

Destruktive Aktion:

- `DangerSurface`;
- `Danger`;
- Danger-Kontur.

Destruktive Aktionen bleiben visuell sekundär und werden bei weitreichenden Folgen
ausdrücklich bestätigt.

`CompactActionButton` ist nur gemeinsame Stilbasis, keine zusätzliche semantische
Aktionsrolle.

Disabled-Zustände bleiben technisch deaktiviert und zusätzlich visuell erkennbar.

## 8. Icons und Touch-Ziele

- normale Icons etwa 20–24 dp;
- Touch-Ziele mindestens 44–48 dp;
- kleine sichtbare Icons behalten trotzdem eine komfortable interaktive Fläche;
- Zurück, Info, Bearbeiten und Farbpalette behalten appweit dieselbe Bedeutung;
- Bildressourcen statt fontabhängiger Unicode-Icons;
- `IconButton` ist transparent und verwendet den Theme-Textton als Tint;
- auf farbigen Spielerflächen verwendet ein Icon deren gekoppelte Spieler-Textfarbe;
- semantische Beschreibung ist verpflichtend;
- bei Pointer-Geräten kann zusätzlich ein Tooltip verwendet werden.

## 9. Bottom-Navigation

Die permanente Hauptnavigation verwendet dieselbe visuelle Ebene wie `Surface`.

Sie wird abgegrenzt durch:

- obere `Outline`-Kontur;
- optional sehr zurückhaltenden Schatten;
- eindeutig sichtbaren aktiven Zustand.

Der aktive Tab verwendet:

- `NavigationSelectedSurface`;
- `NavigationSelectedText`;
- eine gemeinsame abgerundete Fläche hinter Icon und Beschriftung.

Aktivität darf nie ausschließlich durch Textfarbe erkennbar sein.

Die konkrete Tabstruktur steht in [UI_WIREFRAME.md](UI_WIREFRAME.md).

## 10. Filter und Auswahl

Kurze Filter sind kompakte Filterflächen bzw. Chips statt standardmäßig
vollbreiter Controls.

Normal:

- `Surface`;
- `Outline`.

Ausgewählt:

- `SelectedSurface`;
- `SelectedBorder`;
- sichtbare Auswahlmarkierung, wenn sinnvoll.

Bei ausreichendem Platz stehen Filter nebeneinander. Touch-Ziele bleiben komfortabel.

Die eigentliche Auswahl kann je nach Fall über App-Dialog, Page-internes
`SelectionDialog` oder geeignete native Systemauswahl erfolgen.

Lange Listen können bei Bedarf suchbar werden; dafür wird nicht vorsorglich eine
neue Auswahlarchitektur eingeführt.

## 11. SelectionDialog und modale Page-Auswahl

Page-interne Auswahl-Picker verwenden zentral `SelectionDialog`.

Struktur:

```text
Page Root
├─ Page-Inhalt
├─ modales Auswahl-Overlay
└─ Busy-Layer
```

Während das Overlay offen ist:

- der gesamte darunterliegende Page-Inhalt ist explizit deaktiviert;
- Tastaturfokus und Accessibility-Aktionen erreichen den Hintergrund nicht;
- das Overlay bleibt interaktiv;
- globale Shell-/Bottom-Navigation bleibt davon unberührt;
- eine offene Auswahl ist nicht automatisch `IsBusy`;
- Busy sperrt auch Dialogaktionen.

`SelectionDialog` verwendet:

- `AppBackground` außen;
- `Surface` für Einträge;
- `Outline`;
- `TextPrimary`;
- `SelectedSurface` + `SelectedBorder` + sichtbare Markierung für Auswahl.

Page-interne Auswahl verwendet links den standardisierten Zurück-Pfeil
`icon_back.png` im `IconButton`-Stil. Kein zusätzliches X oder textuelles
„Schließen“.

System-Zurück schließt zuerst die Auswahl und verlässt nicht die Page.
Abbruch verändert den zugrunde liegenden Entwurf nicht.

## 12. AppDialogPage

Informationen, Fehler, Sicherheitsabfragen sowie einfache App-Auswahlen verwenden
`AppDialogPage`.

Regeln:

- modale Fläche: `AppBackground`, `Outline`, `TextPrimary`;
- Einträge: `Surface`;
- aktuelle Auswahl: `SelectedSurface`, `SelectedBorder`, sichtbares Häkchen;
- längere Inhalte scrollbar;
- Bestätigung über Primary bzw. bei Löschen/Verwerfen `DangerButton`;
- getrennte Textaktion zum Abbrechen;
- System-Zurück bricht ab und bestätigt niemals implizit;
- ein Dialog verändert keine ungespeicherten Formularentwürfe.

Der konkrete Dialogbestand steht in [UI_WIREFRAME.md](UI_WIREFRAME.md).

## 13. Sektionen und Hilfetexte

Längere Erklärungstexte zu Sektionen und Einstellungsgruppen werden über das
Info-Icon im jeweiligen Header angeboten und öffnen den gemeinsamen
App-Informationsdialog.

Nicht verwenden:

- dauerhaft sichtbare lange Erklärungstexte;
- zusätzliche Info-Karten;
- individuelle Info-Button-Muster pro Feature;
- wiederholte Erklärungen pro Listeneintrag.

Kurze fachliche Labels, Statusmeldungen und Validierungstexte sind davon nicht
betroffen.

## 14. Busy, Fehler und Mehrfachtaps

Asynchrone Navigation und Speicherung werden gegen Mehrfachtaps geschützt.

Busy:

- blockiert den betroffenen Inhalt;
- verwendet `BusyOverlay`;
- zeigt `BusyIndicator`;
- sperrt auch offene Dialogaktionen.

Fehlermeldungen werden nur dargestellt, wenn tatsächlich ein Fehlertext vorhanden ist.

## 15. Spielerfarben: UI-Palette

`PlayerColor` ist ein gemeinsamer fachlicher Farbtyp. Die globale UI-Palette liegt
ausschließlich in der App-Presentation und ordnet jedem Wert ein
Hintergrund-/Text-Paar zu.

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

Diese Werte bleiben in allen App-Themes identisch.

Weitere Regeln:

- unbekannte Präsentationswerte fallen definiert auf Grau zurück;
- Namen werden separat lokalisiert und nie aus Hexwerten abgeleitet;
- vollständig farbige Spielerflächen verwenden die zugehörige Textfarbe auch für
  Sekundärtext;
- eingebettete App-Controls bleiben App-Chrome mit Theme-Surface/Outline;
- Spielername, Reihenfolge und/oder ausgeschriebener Farbname bleiben sichtbar;
- Farbe ist nie die einzige Identifikation;
- jede Hintergrund-/Textkombination muss für normalen Text mindestens **4,5:1**
  Kontrast erreichen;
- UI-Hexwerte sind keine Kamera-/Erkennungswerte oder Trainingsdaten.

Welche Teilmenge ein Spiel erlaubt, steht im Spiel-Datenmodell.

### Spielabhängige Farbauswahl in Gruppenformularen

Wenn ein Spiel `PlayerColor` verwendet, bieten Gruppe anlegen und Gruppe bearbeiten
für jedes Gruppenmitglied denselben allgemeinen Farbworkflow an.

Allgemeine Regeln:

- Auswahl nur aus der für das aktuelle Spiel erlaubten Teilmenge;
- zusätzlich „Keine Farbe“, sofern ein nicht gesetzter Default fachlich erlaubt ist;
- Dialog nennt das aktuelle Spiel und den betroffenen Spieler;
- die Auswahl wird zunächst nur im Formularentwurf geändert;
- ein Tap übernimmt die Auswahl und schließt;
- Abbruch bzw. System-Zurück verändert nichts;
- erst Speichern persistiert Group und spielabhängige Defaults atomar;
- eine bereits anderweitig vergebene Farbe wird innerhalb des Entwurfs getauscht,
  statt einen doppelten Default zu erzeugen;
- Spielerkacheln aktualisieren Hintergrund und Textfarbe unmittelbar aus der
  gemeinsamen `PlayerColor`-Palette;
- ohne Farbe bleibt die Kachel auf `Surface`;
- Palettenbutton und Reihenfolge-/Entfernen-Aktionen besitzen mindestens
  48 × 48 dp Touchfläche.

Wenn ein Spiel keine `PlayerColor`-Auswahl unterstützt, entfallen Farbstatus und
Palettenaktion vollständig.

Die konkrete erlaubte Farbteilmenge steht im jeweiligen Spiel-Datenmodell bzw.
Spiel-UI-Guide.

## 16. Accessibility

Ein Zustand wird nie ausschließlich durch Farbe vermittelt.

Das gilt insbesondere für:

- aktive Navigation;
- ausgewählte Filter;
- PlayerColor;
- Disabled;
- Fehler;
- spielabhängige Auswahl-/Statuszustände.

Text- und UI-Kontraste werden für alle Themes geprüft.

Große Schrift und Tastatur dürfen Kernaktionen nicht verdecken; betroffene Inhalte
sind scrollbar.

Android und iOS teilen Informationsarchitektur und Semantik, behalten aber native
Bedienerwartungen.

## 17. Layout

Zielgerät für die aktuelle mobile Gestaltung:

```text
6,1 Zoll
Hochformat
1170 × 2532 px Referenzauflösung
```

Bevorzugte Abstände:

```text
4 / 8 / 12 / 16 / 20 / 24 / 32 dp
```

Moderate Rundungen, keine starken Standardschatten.

Verbindliche konkrete Screenmaße und Anordnung stehen ausschließlich im
[UI_WIREFRAME.md](UI_WIREFRAME.md).

## 18. Lokalisierung

Alle sichtbaren App-Texte laufen über die bestehende Lokalisierung.

In deutschen sichtbaren Texten wird konsequent **„Spieler“** verwendet.
Technische Typen und Ressourcenschlüssel behalten `Player`.

Spielmaterial-/Content-Sprache ist davon getrennt und folgt der allgemeinen
Architektur-/Datenmodellregel.

## 19. XAML-Bindings und ViewModels

Neue oder geänderte XAML-Bindings werden grundsätzlich als **compiled bindings**
implementiert.

- `x:DataType` auf dem Scope setzen, auf dem der jeweilige `BindingContext` gilt.
- Jedes `DataTemplate` separat mit seinem Elementtyp typisieren.
- Bei geändertem `BindingContext` den passenden `x:DataType` setzen.
- Bindings über `x:Reference` verwenden eine typisierte Page-/ViewModel-Property
  statt eines untypisierten `BindingContext`.
- String-basierte Bindings nur, wenn compiled bindings technisch nicht sinnvoll
  möglich sind.
- Strict-XAML-/Binding-Warnungen nicht durch `x:Object`, `x:Null`, `NoWarn` oder
  Abschalten der Compileroptionen umgehen.
- Keine EF-Entities oder EF-Navigation-Properties direkt an die UI binden.
- Pages/ViewModels arbeiten mit Domain-/Application-Modellen oder expliziten
  Read Models.

Das Projekt aktiviert:

```text
MauiStrictXamlCompilation
MauiEnableXamlCBindingWithSourceCompilation
XC0022..XC0025 als Buildfehler
```

ViewModel-Konstruktoren führen keine Datenbankzugriffe, Navigation oder asynchrones
Laden aus.

Allgemeine C#-/Async-Regeln:
[CODE_GUIDELINES.md](CODE_GUIDELINES.md).

## 20. Gemeinsame Styles

Die vorhandenen gemeinsamen Styles sind bevorzugt wiederzuverwenden:

```text
CompactActionButton
PrimaryCompactButton
SecondaryActionButton
TextActionButton
DangerButton
HeaderDivider
InlineFormEntry
IconButton
CardBorder
GroupPlayerCard
GroupPlayerText
GroupPlayerAction
GroupPlayerRemoveAction
GroupPlayerPalette
SelectionDialog
FilterChip
```

Neue globale Styles werden nur eingeführt, wenn mehrere reale Screens denselben
semantischen Bedarf besitzen.

Spielspezifische Styles bleiben im jeweiligen Spielmodul, sofern sie nicht
nachweislich allgemein sind.
