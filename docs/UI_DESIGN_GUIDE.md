# Boardgame Tracker – UI Design Guide

## 1. Geltungsbereich

Diese Datei definiert die spielübergreifende visuelle Sprache und
Interaktionskonventionen. Spielergänzungen stehen unter
`docs/Games/<GameName>/UI_DESIGN_GUIDE.md`.

Konkrete Screenstruktur/Maße: [UI_WIREFRAME.md](UI_WIREFRAME.md).
Fachverhalten: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).

> Das Theme bildet den neutralen App-Rahmen. Fachliche Spielvisuals behalten ihre
> eigene Semantik.

Ein Theme ändert visuelle Ressourcen, aber nicht Navigation, Abstände, Größen,
Informationshierarchie oder fachliches Verhalten.

## 2. Themes und Tokens

Themes:

```text
Deep Forest
Königsblau
Dunkel
```

**Dunkel ist der Default**, wenn keine gültige Auswahl gespeichert ist. Eine
gespeicherte Auswahl wird vor der ersten App-Ansicht angewendet; Wechsel wirken ohne
Neustart.

Die native Splashfläche bleibt fest `#141A17`, weil sie vor dem Laden der
MAUI-Preferences entsteht.

Screens verwenden semantische Ressourcen statt Theme-Namen:

```text
AppBackground Surface SurfaceSecondary
Primary PrimaryPressed PrimaryForeground
SelectedSurface SelectedBorder
TextPrimary TextSecondary Outline
NavigationBackground NavigationText
NavigationSelectedSurface NavigationSelectedText
Danger DangerSurface
DisabledSurface DisabledText
BusyOverlay IconSurface GameGraphicText BusyIndicator
RecognitionHigh RecognitionMedium
```

Featurecode entscheidet nicht mit `if Forest/Blue/Dark`.

Zusätzliche semantische Statusrollen werden nur für einen realen fachlichen Zustand
eingeführt und zentral bereitgestellt; Features definieren keine ad-hoc-Farben.

`DisabledSurface`/`DisabledText` kennzeichnen deaktivierte Aktionen,
`BusyOverlay`/`BusyIndicator` den blockierenden Busy-Zustand. `IconSurface` ist nur
für eingebettete Grafik-/Farbwahlfelder gedacht; `GameGraphicText` ist der feste
dunkle Textton auf hellen fachlichen Grafikbadges.

## 3. Paletten

| Rolle | Deep Forest | Königsblau | Dunkel |
|---|---|---|---|
| AppBackground | `#D9E6DF` | `#DCE5F4` | `#141A17` |
| Surface | `#FFFFFF` | `#FFFFFF` | `#222A26` |
| SurfaceSecondary | `#EEF5F1` | `#EEF3FB` | `#29352F` |
| Primary | `#466655` | `#315B9B` | `#5B8A70` |
| PrimaryPressed | `#385346` | `#274A80` | `#6D9B82` |
| PrimaryForeground | `#FFFFFF` | `#FFFFFF` | `#101713` |
| SelectedSurface | `#C7DCCF` | `#C8D8F1` | `#355244` |
| SelectedBorder | `#789A85` | `#708EBE` | `#668875` |
| TextPrimary | `#263D31` | `#18386A` | `#F0F4F1` |
| TextSecondary | `#68776F` | `#66758E` | `#C6D0CA` |
| Outline | `#B8CAC0` | `#BCCAE1` | `#3B4942` |
| NavigationBackground | `#FFFFFF` | `#FFFFFF` | `#222A26` |
| NavigationText | `#68776F` | `#65748D` | `#AEBBB4` |
| NavigationSelectedSurface | `#C7DCCF` | `#C8D8F1` | `#355244` |
| NavigationSelectedText | `#263D31` | `#18386A` | `#FFFFFF` |
| Danger | `#B3261E` | `#B3261E` | `#FFB4AB` |
| DangerSurface | `#F9DEDC` | `#F9DEDC` | `#5F2423` |
| DisabledSurface | `#FFFFFF` | `#FFFFFF` | `#222A26` |
| DisabledText | `#263D31` | `#18386A` | `#F0F4F1` |
| BusyOverlay | `#99000000` | `#99000000` | `#99000000` |
| IconSurface | `#EEF5F1` | `#EEF3FB` | `#EEF5F1` |
| GameGraphicText | `#101713` | `#101713` | `#101713` |
| BusyIndicator | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` |
| RecognitionHigh | `#256A37` | `#256A37` | `#8CD9A3` |
| RecognitionMedium | `#806000` | `#806000` | `#F5D45E` |

Deep Forest bleibt ruhig/natürlich ohne beige Zusatzflächen; Königsblau ist das
kühlere helle Theme; Dunkel ist eigenständig und verwendet keine großen rein
schwarzen Flächen oder rein weißen Konturen.

## 4. Flächen und Formulare

Die Inhaltsflächen sind:

- `AppBackground`: Screen-Grundfläche;
- `Surface`: eigenständige Inhalts-/Interaktionseinheit;
- `SelectedSurface`: tatsächlich ausgewählter Zustand.

`SurfaceSecondary` ist nur die leichte Fläche sekundärer Aktionen, keine weitere
Kartenebene.

> Karten sind Inhalt, Buttons sind Aktionen, Formulare bleiben flach,
> Metainformation benötigt keine Karte.

Sektionen werden zuerst über Abstand/Überschrift getrennt; `HeaderDivider` nur
gezielt. Detail-/Bearbeitungsheader bleiben auf `AppBackground`.
Leerzustände erhalten keine dekorative Karte.

Fachlich farbige Spielerflächen sind eine bewusste Ausnahme und bleiben
themeunabhängig.

## 5. Aktionen und Icons

- **PrimaryCompactButton:** wichtigste Seitenaktion.
- **SecondaryActionButton:** normale nachgeordnete Aktion.
- **TextActionButton:** kleine kontextuelle Aktion ohne starke Fläche.
- **DangerButton:** destruktive Aktion; bei weitreichenden Folgen bestätigen.

`CompactActionButton` ist nur gemeinsame Stilbasis. Disabled bleibt technisch
deaktiviert und visuell erkennbar.

Icons etwa 20–24 dp; Touchziele mindestens 44–48 dp. Zurück, Info, Bearbeiten und
Farbpalette behalten appweit dieselbe Bedeutung. Bildressourcen statt
fontabhängiger Unicode-Icons. Iconbuttons sind transparent und haben eine
semantische Beschreibung; Pointer darf zusätzlich Tooltip nutzen.

## 6. Navigation und Filter

Bottom-Navigation liegt auf `Surface`, hat eine obere `Outline` und markiert den
aktiven Tab mit `NavigationSelectedSurface` + `NavigationSelectedText` hinter Icon
und Beschriftung. Aktivität wird nie nur über Textfarbe vermittelt.

Kurze Filter sind kompakte Chips:
- normal `Surface` + `Outline`;
- ausgewählt `SelectedSurface` + `SelectedBorder` + sichtbare Markierung.

Sie sind nicht unnötig vollbreit; lange Auswahllisten dürfen bei realem Bedarf
suchbar werden.

## 7. Dialoge und modale Auswahl

### SelectionDialog

Page-interne Auswahl verwendet `SelectionDialog`:

```text
Root
├─ deaktivierter Page-Inhalt
├─ modales Auswahl-Overlay
└─ Busy-Layer
```

Hintergrundinhalt ist auch für Fokus/Accessibility deaktiviert. Globale
Shell-Navigation bleibt unberührt. Eine offene Auswahl setzt **nicht** `IsBusy`;
Busy bleibt ein separater Zustand und sperrt gegebenenfalls auch Dialogaktionen.
System-Zurück schließt zuerst die Auswahl.

Darstellung: `AppBackground` außen, `Surface`-Einträge, ausgewählt
`SelectedSurface` + `SelectedBorder` + sichtbare Markierung.

Zum Schließen dient der standardisierte Zurück-Pfeil im `IconButton`-Stil, kein
zusätzliches X/Text-Schließen.

### AppDialogPage

Informationen, Fehler, Sicherheitsabfragen und einfache App-Auswahlen verwenden
`AppDialogPage`. System-Zurück bricht ab und bestätigt niemals implizit.
Längere Inhalte sind scrollbar. Bestätigung nutzt Primary bzw. Danger, Abbruch eine
getrennte Textaktion.

Dialoge verändern keine ungespeicherten Entwürfe der darunterliegenden Page.

## 8. Info, Busy und Fehler

Längere Sektionserklärungen liegen hinter dem gemeinsamen Info-Icon statt als
dauerhafte Infokarten oder wiederholte Hilfetexte.

Asynchrone Navigation/Speicherung wird gegen Mehrfachtaps geschützt. Busy blockiert
den betroffenen Inhalt und offene Dialogaktionen über `BusyOverlay` +
`BusyIndicator`.

Fehler werden nur angezeigt, wenn tatsächlich Fehlertext vorhanden ist.

## 9. PlayerColor-Darstellung

Globale Presentation-Palette:

| Farbe | Hintergrund | Text |
|---|---|---|
| Red | `#D96B63` | `#312220` |
| Yellow | `#E7C84A` | `#403716` |
| Green | `#73A66F` | `#243723` |
| Blue | `#6FA9D2` | `#253B4B` |
| Black | `#555B58` | `#E4E9E6` |
| White | `#F4F4F1` | `#39413D` |
| Orange | `#E59A45` | `#493117` |
| Brown | `#805B42` | `#F0E8E2` |
| Purple | `#70599E` | `#EEEAF4` |
| Pink | `#D985AD` | `#472838` |
| Turquoise | `#6EB7B4` | `#264543` |
| Gray | `#9AA2A0` | `#323936` |

Die Paare bleiben in allen Themes identisch; unbekannte Darstellungswerte fallen
auf Gray zurück. Jede Kombination muss für normalen Text mindestens 4,5:1 Kontrast
erreichen. Die Kontrastprüfung verwendet relative sRGB-Luminanz ohne Rundung der
Prüfschwelle. UI-Hexwerte sind keine Kamera-/Trainingswerte.

Farbe ist nie die einzige Identifikation; Name/Reihenfolge/Farbname bleibt sichtbar.

### Gruppenfarbauswahl

Wenn ein Spiel `PlayerColor` verwendet:
- nur seine erlaubte Teilmenge anbieten;
- optional „Keine Farbe“;
- Spiel + Spieler im Dialog nennen;
- Änderungen zunächst nur im Formularentwurf;
- belegte Farbe tauschen statt duplizieren;
- erst Speichern persistiert atomar;
- Kachel aktualisiert Farbe/Text sofort;
- ohne Farbunterstützung entfallen Farbstatus und Palettenaktion.

## 10. Erkennungsstatus

Wenn ein assistierter Erkennungspfad die gemeinsamen Zustände **High** und
**Medium** verwendet, nutzt er `RecognitionHigh` bzw. `RecognitionMedium`.
Unsicher/Fehler verwendet vorhandene Danger-Semantik.

Sicherheits-/Confidence-Zustand wird zusätzlich durch Text/Icon vermittelt, niemals
nur durch Farbe. Die fachliche Bedeutung definiert das jeweilige Feature/Spielmodul.

## 11. Accessibility, Layout und Sprache

- Zustände nie nur durch Farbe vermitteln.
- `TextSecondary` ist für `Surface`-Flächen gedacht; kleine Texte direkt auf
  `AppBackground` verwenden `TextPrimary`, weil `TextSecondary` in den hellen
  Paletten dort den 4,5:1-Kontrast nicht erreicht.
- Zielbaseline: 6,1 Zoll, Hochformat, 1170 × 2532 px Referenzauflösung; konkrete
  Screenmaße stehen im Wireframe.
- Bevorzugte Abstände: `4/8/12/16/20/24/32 dp`.
- Moderate Rundungen, keine starken Standardschatten.
- Große Schrift/Tastatur dürfen Kernaktionen nicht verdecken; betroffene Inhalte
  scrollbar.
- Android/iOS teilen Informationsarchitektur, respektieren native Bedienerwartungen.
- Alle sichtbaren App-Texte laufen über Lokalisierung.
- Deutsch verwendet sichtbar konsequent „Spieler“; technische Typen bleiben `Player`.

## 12. XAML und ViewModels

Neue/geänderte Bindings werden als compiled bindings umgesetzt:

- passendes `x:DataType` pro BindingContext/DataTemplate;
- bei BindingContext-Wechsel neu typisieren;
- `x:Reference` über typisierte Page-/VM-Properties;
- String-Bindings nur wenn technisch nötig;
- Strict-XAML-Warnungen nicht mit `x:Object`, `x:Null`, `NoWarn` oder deaktivierten
  Compileroptionen umgehen;
- keine EF-Entities/Navigation Properties direkt an UI binden.

Aktiv sind `MauiStrictXamlCompilation`,
`MauiEnableXamlCBindingWithSourceCompilation` und XC0022–XC0025 als Buildfehler.

ViewModel-Konstruktoren laden nicht asynchron, navigieren nicht und greifen nicht
auf die Datenbank zu.

Async-/C#-Regeln: [CODE_GUIDELINES.md](CODE_GUIDELINES.md).

## 13. Gemeinsame Styles

Vorhandene semantische Styles wiederverwenden, insbesondere:

```text
PrimaryCompactButton SecondaryActionButton TextActionButton DangerButton
HeaderDivider InlineFormEntry IconButton CardBorder SelectionDialog FilterChip
GroupPlayerCard GroupPlayerText GroupPlayerAction GroupPlayerRemoveAction
GroupPlayerPalette
```

Neue globale Styles nur bei realem mehrfachen Bedarf.
