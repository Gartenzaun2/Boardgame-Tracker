# Boardgame Tracker – UI Design Guide

## 1. Geltungsbereich

Diese Datei definiert die spielübergreifende **visuelle Sprache, wiederverwendbare
Interaktionsmuster und Accessibility-Regeln**. Spielergänzungen stehen unter
`docs/Games/<GameName>/UI_DESIGN_GUIDE.md`.

Konkrete Screenstruktur/Maße: [UI_WIREFRAME.md](UI_WIREFRAME.md).
Fachverhalten: [PRODUCT_SPEC.md](PRODUCT_SPEC.md).
XAML-/Binding-/ViewModel-Regeln: [CODE_GUIDELINES.md](CODE_GUIDELINES.md).

> Das Theme bildet den neutralen App-Rahmen. Fachliche Spielvisuals behalten ihre
> eigene Semantik.

Ein Theme ändert visuelle Ressourcen, aber nicht Navigation, Abstände, Größen,
Informationshierarchie oder fachliches Verhalten.

## 2. Themes und Tokens

Themes:

```text
Deep Forest
Königsblau
Dark (Arche Nova)
```

**Dark (Arche Nova) ist der Default**, wenn keine gültige Auswahl gespeichert ist. Eine
gespeicherte Auswahl wird vor der ersten App-Ansicht angewendet; Wechsel wirken ohne
Neustart.

Die native Splashfläche bleibt fest `#141A17`, weil sie vor dem Laden der
MAUI-Preferences entsteht.

Screens verwenden semantische Ressourcen statt Theme-Namen:

```text
AppBackground Surface CardSurface CardOutline SurfaceSecondary
Primary PrimaryPressed PrimaryForeground
SelectedSurface SelectedBorder
TextPrimary TextSecondary Outline
NavigationBackground NavigationText
NavigationSelectedSurface NavigationSelectedText
Danger DangerSurface
DisabledSurface DisabledText
ModalOverlay BusyOverlay IconSurface GameGraphicText BusyIndicator
RecognitionHigh RecognitionMedium
GameContextText GameContextBorder
```

`GameContextText` und `GameContextBorder` dienen ausschließlich der nicht-interaktiven Spielkontext-Capsule; sie sind keine Statusfarben und nicht pro Spiel verschieden.

Featurecode entscheidet nicht nach konkreten Theme-Namen. Zusätzliche semantische
Statusrollen entstehen nur für einen realen Zustand und werden zentral bereitgestellt;
Features definieren keine ad-hoc-Farben.

`DisabledSurface`/`DisabledText` kennzeichnen deaktivierte Aktionen,
`BusyOverlay`/`BusyIndicator` den blockierenden Busy-Zustand. `IconSurface` ist für
eingebettete Grafik-/Farbwahlfelder gedacht; `GameGraphicText` ist der feste dunkle
Textton auf hellen fachlichen Grafikbadges.

`ModalOverlay` dimmt den Hintergrund hinter Dialogen und modalen Auswahlen,
während der darunterliegende Screen erkennbar bleibt. `BusyOverlay` bleibt
ausschließlich tatsächlichen blockierenden Busy-/Ladezuständen vorbehalten.

## 3. Paletten

| Rolle | Deep Forest | Königsblau | Dark (Arche Nova) |
|---|---|---|---|
| AppBackground | `#D9E6DF` | `#DCE5F4` | `#101A1B` |
| Surface | `#FFFFFF` | `#FFFFFF` | `#1B2B30` |
| CardSurface | `#FFFFFF` | `#FFFFFF` | `#263D49` |
| CardOutline | `#B8CAC0` | `#BCCAE1` | `#426070` |
| SurfaceSecondary | `#EEF5F1` | `#EEF3FB` | `#213B37` |
| Primary | `#466655` | `#315B9B` | `#70A98E` |
| PrimaryPressed | `#385346` | `#274A80` | `#61977E` |
| PrimaryForeground | `#FFFFFF` | `#FFFFFF` | `#10251C` |
| SelectedSurface | `#C7DCCF` | `#C8D8F1` | `#34594E` |
| SelectedBorder | `#789A85` | `#708EBE` | `#729B87` |
| TextPrimary | `#263D31` | `#18386A` | `#F0F5F6` |
| TextSecondary | `#68776F` | `#66758E` | `#C0D0D7` |
| Outline | `#B8CAC0` | `#BCCAE1` | `#354A49` |
| NavigationBackground | `#FFFFFF` | `#FFFFFF` | `#172326` |
| NavigationText | `#68776F` | `#65748D` | `#AEBBB4` |
| NavigationSelectedSurface | `#C7DCCF` | `#C8D8F1` | `#34594E` |
| NavigationSelectedText | `#263D31` | `#18386A` | `#F0F5F6` |
| Danger | `#B3261E` | `#B3261E` | `#FFB4AB` |
| DangerSurface | `#F9DEDC` | `#F9DEDC` | `#5F2423` |
| DisabledSurface | `#FFFFFF` | `#FFFFFF` | `#222A26` |
| DisabledText | `#263D31` | `#18386A` | `#F0F4F1` |
| BusyOverlay | `#99000000` | `#99000000` | `#99000000` |
| ModalOverlay | `#66000000` | `#66000000` | `#66000000` |
| IconSurface | `#EEF5F1` | `#EEF3FB` | `#EEF5F1` |
| GameGraphicText | `#101713` | `#101713` | `#101713` |
| BusyIndicator | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` |
| RecognitionHigh | `#256A37` | `#256A37` | `#8CD9A3` |
| RecognitionMedium | `#806000` | `#806000` | `#F5D45E` |
| GameContextText | `#176B80` | `#704089` | `#678AB8` |
| GameContextBorder | `#176B80` | `#704089` | `#678AB8` |

Die Spielkontext-Capsule verwendet in jedem Theme eine bewusst eigenständige Akzentfarbe, die nicht einfach die allgemeine `Primary`- oder `SelectedBorder`-Farbe übernimmt: Petrolblau (`#176B80`) in Deep Forest, Aubergine (`#704089`) in Königsblau und dezentes Blau (`#678AB8`) in Dark (Arche Nova). `GameContextText` und `GameContextBorder` verwenden innerhalb eines Themes denselben Farbwert. Diese semantischen Rollen dienen der nicht-interaktiven Spielkennzeichnung und dürfen nicht als Auswahlzustand interpretiert werden.

Deep Forest bleibt ruhig/natürlich ohne beige Zusatzflächen; Königsblau ist das
kühlere helle Theme. Dark (Arche Nova) verwendet die Farbwelt **Ark Nova Midnight**: dunkelgrünliche Grundfläche, blaugraue Inhaltskarten und gedämpftes Salbeigrün für Hauptaktionen. Seine gespeicherte Identität bleibt `AppTheme.Dark = 2`; es entstehen keine zusätzliche Theme-Auswahl und keine Migration. Nicht neu festgelegte Sonderrollen behalten ihre bisherigen Werte.

## 4. Flächen und Formulare

- `AppBackground`: Screen-Grundfläche.
- `Surface`: allgemeine Interaktionsfläche, etwa Formularelemente.
- `CardSurface` / `CardOutline`: Hintergrund und Kontur normaler Inhaltskarten über den zentralen `CardBorder`-Stil. Fachlich eingefärbte Spielerflächen und bewusst farbcodierte Elemente behalten ihre eigenen Ressourcen; ausgewählte Elemente ihre Auswahlfarben. Dialoge behalten ihre Dialogressourcen.
- `NavigationBackground`: eigene Bottom-Navigationsfläche, unabhängig von Inhaltskarten.
- `SelectedSurface`: tatsächlich ausgewählter Zustand.
- `SurfaceSecondary`: leichte Fläche sekundärer Aktionen, keine weitere Kartenebene.

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

Sichtbare Aktionsicons sind einheitlich zurückhaltend (etwa **20–24 dp**);
ihre unabhängig davon mindestens **44–48 dp** großen Touchflächen bleiben erhalten.
Zurück, Info, Bearbeiten und Farbpalette behalten appweit dieselbe Bedeutung.
Bildressourcen statt fontabhängiger Unicode-Icons. Iconbuttons sind transparent
und haben eine semantische Beschreibung; Pointer darf zusätzlich Tooltip nutzen.
Die Größe der Touchfläche darf nicht unbeabsichtigt die sichtbare Icongrafik
vergrößern.

Navigierbare ganze Listenzeilen/Karten zeigen rechts eine dekorative Chevron-Grafik. Die gesamte vorgesehene Navigationsfläche öffnet genau ein Ziel; der Chevron erhält keine eigene Tap- oder Fokusaktion. Separate Aktionen (Bearbeiten, Kamera, Info, Palette, Hoch/Runter, Spezialkarten) behalten eigene, nicht überlappende Touchflächen. Rein informative Karten haben weder Chevron noch Tap-Verhalten. In der Spieler-/Gruppenverwaltung öffnet weiterhin ausschließlich der Stift den Editor. Auswahlkacheln erhalten keinen Navigations-Chevron.

Einstellungsübersichten sind flach: deutlich größere, markante Gruppenüberschriften gliedern die Inhalte ohne dekorative Karten. Allgemeine und spielspezifische Einstellungen verwenden dieselben wiederverwendbaren Styles `SettingsPageBody` (Inhaltsrand 20 dp horizontal), `SettingsContent` (12 dp Abstand), `SettingsRow` (mindestens 48 dp Höhe), `SettingsSectionTitle` und `SettingsLookup`. Logische Gruppen werden ohne zusätzliche Zwischen-Seiten gebildet. `SettingsItemLabel` verwendet 16 dp und `TextPrimary`; Abschnittsüberschriften bleiben 21 dp. Auswahlwerte verwenden 15 dp, sekundäre Hinweise über `SettingsHintLabel` 13 dp. Vergleichbare Auswahlen sind rechtsbündig und 132 dp breit. `SettingsLookupHost` bewahrt eine mindestens 44 dp hohe gemeinsame Button-Touchfläche. Die einzige sichtbare Kontur (`SettingsLookupFrame`, Rundung 8 dp) liegt mit 2 dp vertikalem Innenabstand dahinter: bei 44 dp Gesamthöhe ist sie 40 dp hoch. Der transparente native Button hat keinen eigenen Rahmen, 8/2 dp Padding und enthält den kleinen SVG-Chevron nach unten (14 × 10 dp, Abstand zum Text 4 dp). Die dekorative Kontur und der Pfeil erhalten keine eigenen Touch-/Accessibility-Ziele. Werte und Beschriftungen dürfen bei langen Texten oder größerer Systemschrift umbrechen; die Höhe darf dabei wachsen, ohne feste Maximalhöhe oder Auslassungspunkte. Navigierbare Zeilen zeigen dagegen einen Chevron nach rechts. Switches sind booleschen Einstellungen vorbehalten und verwenden den bestehenden gemeinsamen Switch-Stil. Allgemeine Erklärungen zu Einstellungen stehen in App-Informationsdialogen hinter dem gemeinsamen Info-Icon neben der Beschriftung. Info- und Switch-Touchflächen sind mindestens 44 dp groß, liegen in getrennten Spalten und überlappen nicht. Konkrete Sperrbegründungen bleiben dagegen sichtbar, solange die Bedienung tatsächlich gesperrt ist.

Sichtbare echte Zeitpunkte werden lokal im kulturabhängigen kurzen Datum-/Uhrzeitformat dargestellt (bei deutscher Kultur etwa 31.07.2026 13:55). Reine Kalenderdaten und Monatsüberschriften behalten ihr fachlich passendes Format. Persistenz, Offset-/UTC-Behandlung und Serialisierung werden dadurch nicht verändert.

## 6. Navigation und Filter

Bottom-Navigation liegt auf `Surface`, hat eine obere `Outline` und markiert den
aktiven Tab mit `NavigationSelectedSurface` + `NavigationSelectedText` hinter Icon
und Beschriftung. Aktivität wird nie nur über Textfarbe vermittelt.

**Spielen-Tab und Aktiv-Symbol sind verschiedene Zeichen:** Das Spielen-Tab-Icon
zeigt das abstrahierte **App-Logo**. Das nicht interaktive Aktiv-Symbol an einem
Spiel- oder Gruppeneintrag signalisiert ausschließlich eine aktuell offene
Partie/Wertung; es ist **nicht** das Spielen-Tab-Logo und darf nicht durch dieses
ausgetauscht werden. Das Aktiv-Symbol muss in allen Themes über eine geeignete
semantische Vordergrundfarbe deutlich sichtbar sein; ein fest schwarzes Symbol
auf dunklem Hintergrund ist unzulässig.

Kurze Filter sind kompakte Chips:

- normal `Surface` + `Outline`;
- ausgewählt `SelectedSurface` + `SelectedBorder` + sichtbare Markierung.

Sie sind nicht unnötig vollbreit; lange Auswahllisten dürfen bei realem Bedarf
suchbar werden.

### Segmente und gemeinsamer Seitenheader

**Mitspieler** verwendet den Segment-Umschalter **Spieler | Gruppen** für gleichrangige Verwaltungsansichten. Er steht **unterhalb** des Headers und seines Dividers; Segmente sind keine Filterchips und beeinflussen die Titelposition nicht. Für Segmente gelten in jedem Theme drei unterscheidbare, lesbare Zustände: ausgewählt (`SelectedSurface`/`SelectedBorder` mit kontrastreicher Schrift), nicht ausgewählt (`SurfaceSecondary` mit `TextPrimary`) und deaktiviert (sichtbar gesperrt, aber weiterhin lesbar). Die Hintergrund- und Textfarben müssen **zusammen** für den jeweiligen Zustand definiert und auf Kontrast geprüft werden; bloßes Herabsetzen der gesamten Opazität darf Beschriftungen nicht unleserlich machen. Ein eigener gemeinsamer Segment-Stil ist gegenüber seitenlokalen Trigger-Kombinationen vorzuziehen.

Alle regulären Pages verwenden **eine wiederverwendbare allgemeine Header-Komponente** mit zwei Varianten:

```text
Hauptbereich (Spielen, Historie, Mitspieler, Einstellungen):
[Titel]                                              [Aktionsicons]
HeaderDivider

Unterseite:
[Zurück] [Titel]                                     [Aktionsicons]
[Spielkontext-Capsule] [erste Unterüberschrift]
[zweite Unterüberschrift]
HeaderDivider
```

- **Feste Geometrie:** Der gemeinsame sichtbare linke Anker liegt bei **16 dp**: Hauptseitentitel, sichtbare linke Zurück-Pfeilspitze, Badge-Außenkante und Metadatenbeginn. Auf Unterseiten beginnt der Titel **12 dp nach dem sichtbaren rechten Pfeilende**; der Platz bleibt bei verborgenem Pfeil erhalten. Innerhalb jeder Variante beginnen Titel immer an exakt derselben horizontalen und vertikalen Position, mit identischer Schriftgröße/-stärke, Titelzeilenhöhe und oberem Abstand. Die Titel der beiden Varianten stehen vertikal auf derselben Höhe; horizontal unterscheidet sich die Unterseite durch den stets reservierten Zurück-Bereich, selbst wenn der Pfeil ausgeblendet ist. Unterschiedliche Aktionsicons oder optionale Kontextzeilen dürfen den Titel nicht verschieben.
- **Zurück und Aktionen:** Zurück, Titel und Aktionsgrafiken sind auf einer gemeinsamen vertikalen Mittelachse ausgerichtet. Die **44-dp-Touchflächen** der rechten Icons liegen ohne Zwischenraum nebeneinander (44 dp Mittelpunktabstand); sichtbare Grafiken bleiben etwa 24 dp groß. Zurück-Pfeil und rechte Aktionsicons haben feste Größe, Touchfläche und Anker. Der rechte Icon-Bereich ist rechtsbündig: Das äußerste rechte Icon sitzt stets an derselben Stelle, zusätzliche Icons wachsen mit festem Abstand **nach links**. Fällt ein linkes Icon weg, bleiben rechts davon verankerte Icons an ihrem Platz; entfällt das äußerste rechte Icon, besetzt das nun rechte Icon dessen Anker. Der Titel behält seine Startposition und darf nicht mit Aktionen überlappen.
- **Kontextzeilen:** Die Spielkontext-Capsule steht in jeder Unterseite mit Spielkontext **ganz links am festen Header-Inhaltsrand**, unabhängig von der eingerückten Titelposition und ohne reservierte Zurück-Pfeil-Spalte davor. Die erste Unterüberschrift beginnt unmittelbar **6 dp rechts neben der tatsächlich gemessenen Capsule-Breite**; keine feste Badge-Spaltenbreite oder künstliche Leerspalte. Badge und erste Unterüberschrift sind **vertikal mittig auf derselben Zeile** ausgerichtet. Die horizontale Position der ersten Unterüberschrift ist damit bewusst relativ zur Capsule, nicht absolut fixiert. Die optionale zweite Unterüberschrift beginnt am linken Header-Inhaltsrand in der dritten Zeile. Titel, Zurück, Aktionsicons und linker Badge-Anker bleiben über Seitenwechsel unverändert. Fehlt der Badge, wird keine unsichtbare Badge-Breite reserviert; der erste vorhandene Kontexttext beginnt am linken Inhaltsrand. Es werden nur tatsächlich benötigte Unterüberschriftszeilen angezeigt; lange Namen dürfen innerhalb des vorgesehenen Bereichs verständlich umbrechen.
- **Kompakte Abstände und Padding:** Die sichtbare Titelzeile hat eine kompakte Mindesthöhe von **36 dp** und wächst bei größeren Schriftmetriken. **44-dp-Touchflächen bestimmen diese Zeilenhöhe nicht**: Sie dürfen in freie Bereiche ragen, enden aber vor der Badge-/Kontextzeile und blockieren keine anderen Controls. Titel und Icons werden mittig angeordnet, ohne lokale Titel-Margins. Der Abstand zur ersten Kontextzeile beträgt **4 dp**, ebenso zwischen den Kontextzeilen; die Header-Komponente besitzt einen zentral definierten Inhaltsrand und Abstand. Seiten-Padding für den übrigen Inhalt darf davon abweichen, ohne die Header-Geometrie zu beeinflussen. Seitenlokale negative Ausgleichs-Margins zur Korrektur unterschiedlicher Page-Paddings sind zu vermeiden; vorhandene auf diesen Zweck zurückgehende Ausgleichswerte werden bei der Umstellung durch eine konsistente Einbettung ersetzt.
- **Divider:** Nur seine **vertikale Position** hängt von der Anzahl und tatsächlichen Höhe der vorhandenen Unterüberschriftszeilen ab. Er folgt mit einheitlichem Abstand auf die letzte verwendete Zeile. Anker, Titel und Icons bleiben unverändert.
- **Verwandte Screens:** Beim Wechsel zwischen Screens desselben Ablaufs, insbesondere Wertung, Gleichstände, Ergebnis und Korrektur, dürfen Titel, Zurück, Badge und unveränderte Icons nicht springen. Kein Page-eigenes Header-Layout mit abweichenden lokalen Fontgrößen oder Paddingwerten.
- **Zuständigkeit:** Die Komponente regelt Darstellung und Slots, nicht Rücknavigation, Screenzustand oder konkrete Spieltypen. Pages liefern Handler/Aktionen; Spielmodule liefern Badge-Text und vollständigen Spielnamen. Modal- und Auswahlheader verwenden dieselbe Geometrie, soweit sie als reguläre Seitenheader auftreten; Dialoge behalten ihre dokumentierten Dialogmuster.

Die **Spielkontext-Capsule** ist ein nicht-interaktives Kontextlabel, kein Button oder Auswahlchip. Einheitlich für alle Spiele innerhalb desselben Themes: **24 dp Gesamthöhe einschließlich Kontur und Innenabstand**, transparenter Hintergrund, vollständig gerundete Pill-Form, **2 dp** Kontur, **8 dp** horizontaler Innenabstand, keine zusätzliche Mindestbreite, **13 dp** Textgröße und **fette Schrift (Bold)**. Vertikales Padding darf die 24-dp-Gesamthöhe nicht vergrößern; der Text sitzt mittig in der Pille. Die Capsule-Breite ergibt sich aus dem Text und dem Innenabstand, nicht aus einer festen Spalte. Die Schrift wird nicht vergrößert, um die stärkere Betonung zu erreichen. Nur der explizit pro Spiel registrierte Badge-Text variiert. Ein Screenreader erhält stets den vollständigen Spielnamen, auch wenn die Capsule visuell ein Kürzel zeigt.

**Badge-Regel:** Jede Unterseite mit genau einem konkreten Spielkontext zeigt die Capsule; rein allgemeine Unterseiten (etwa Spieler oder Gruppe anlegen/bearbeiten, Spiele verwalten) sowie die Spieleübersicht zeigen keine. Der Titel benennt die **Funktion** (z. B. „Einstellungen“), die Capsule das **Spiel**. Der allgemeine Header kennt keine konkreten Spiele.

## 7. Dialoge und modale Auswahl

Jede reine Auswahlansicht zeigt oben links den gemeinsamen Zurück-/Exit-IconButton und keinen zusätzlichen Schließen-Textbutton unten. Auf normalen Pages navigiert er zurück, bei modaler Auswahl bricht er ab. Dies gilt auch für Single-Choice-Listen über `AppDialogPage.ChooseAsync`. Informations-, Bestätigungs-, Sicherheits- und destruktive Dialoge behalten ihre expliziten Textaktionen.

### SelectionDialog

Page-interne Auswahl verwendet `SelectionDialog`:

```text
Root
├─ deaktivierter Page-Inhalt mit Screen-Padding
├─ modales Auswahl-Overlay (ModalOverlay)
└─ Busy-Layer (BusyOverlay)
```

Modale und Busy-Overlays decken die gesamte ContentPage-Fläche ab. Der gemeinsame
Root-Container besitzt kein Screen-Padding; dieses liegt auf dem normalen
Seiteninhalt. Overlays liegen direkt im Root, Dialogcontainer erhalten ihren
eigenen Abstand zum Bildschirmrand.

Hintergrundinhalt ist auch für Fokus/Accessibility deaktiviert. Globale
Shell-Navigation bleibt unberührt. Eine offene Auswahl ist kein Busy-Zustand; Busy
bleibt separat und sperrt gegebenenfalls auch Dialogaktionen. System-Zurück schließt
zuerst die Auswahl.

Darstellung: `AppBackground` außen, `Surface`-Einträge, ausgewählt
`SelectedSurface` + `SelectedBorder` + sichtbare Markierung.

Zum Schließen dient der standardisierte Zurück-Pfeil im `IconButton`-Stil, kein
zusätzliches X/Text-Schließen.

### AppDialogPage

Informationen, Fehler, Sicherheitsabfragen und einfache App-Auswahlen verwenden
`AppDialogPage`. System-Zurück bricht ab und bestätigt niemals implizit.
Längere Inhalte sind scrollbar. In Informations- und Bestätigungsdialogen nutzt Bestätigung Primary bzw. Danger, Abbruch eine
getrennte Textaktion.

Dialoge verändern keine ungespeicherten Entwürfe der darunterliegenden Page.

## 8. Info, Busy und Fehler

Längere Sektionserklärungen liegen hinter dem gemeinsamen Info-Icon statt als
dauerhafte Infokarten oder wiederholte Hilfetexte.

Nur bei **wirklich global null Spielern oder Gruppen** darf ein kurzer sichtbarer Einstiegshinweis den ersten notwendigen Schritt erklären. Gefilterte Listen und Spiele ohne zugeordnete Gruppen trotz vorhandener globaler Gruppen zeigen keinen solchen Onboarding-Text.

Busy blockiert den betroffenen Inhalt und offene Dialogaktionen über
`BusyOverlay` + `BusyIndicator`. Fehler werden nur angezeigt, wenn tatsächlich
Fehlertext vorhanden ist.

Technische Mehrfachaufruf-/Async-Regeln stehen in
[CODE_GUIDELINES.md](CODE_GUIDELINES.md).

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

### Spielabhängige Farbauswahl in der Partievorbereitung

Wenn ein Spiel `PlayerColor` verwendet:

- nur seine erlaubte Teilmenge anbieten;
- optional **Keine Farbe**;
- Spiel + Spieler im Dialog nennen;
- Änderungen im Formularentwurf sofort sichtbar machen;
- bei Auswahl einer bereits belegten Farbe die sichtbaren Zuordnungen tauschen
  statt eine doppelte Zuordnung darzustellen;
- ohne Farbunterstützung Farbstatus und Palettenaktion ausblenden.

Diese Auswahl steht nicht in der allgemeinen Gruppe-/Spielerverwaltung unter **Mitspieler**. Änderungen im Vorbereitungsentwurf dürfen den Gruppendefault erst beim erfolgreichen Partiestart verändern.

Speicher-/Validierungsregeln: [PRODUCT_SPEC.md](PRODUCT_SPEC.md) und
[DATA_MODEL.md](DATA_MODEL.md).

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
- Zielbaseline: 6,1 Zoll, Hochformat, **1170 × 2532 px** Referenzauflösung;
  konkrete Screenmaße stehen im Wireframe.
- Bevorzugte Abstände: `4/8/12/16/20/24/32 dp`.
- Moderate Rundungen, keine starken Standardschatten.
- Große Schrift/Tastatur dürfen Kernaktionen nicht verdecken; betroffene Inhalte
  sind scrollbar.
- Android/iOS teilen Informationsarchitektur, respektieren native Bedienerwartungen.
- Alle sichtbaren App-Texte laufen über Lokalisierung.
- Deutsch verwendet sichtbar konsequent **Spieler**; technische Typen bleiben
  `Player`.
- Für sichtbare Aktionen einheitliches Vokabular: **anlegen** für neue eigenständige Entitäten (Spieler, Gruppe, Spezialkarte), **hinzufügen** für die Zuordnung zu einer Liste/einem Entwurf, **auswählen** für die Wahl eines bestehenden Eintrags, **bearbeiten** für Änderungen an Bestehendem, **speichern** für deren Übernahme. **Fertig** beendet einen vorgesehenen Ablauf. Kürzere Beschriftungen wie „Neuer Spieler“ sind im engen Aktionskontext zulässig; der zugehörige Seitentitel lautet „Spieler anlegen“. Fachaktionen wie „Partie starten“, „Wertung abschließen“, „Gruppe übernehmen“ bleiben präzise und werden nicht mechanisch umbenannt. Dieselbe Aktion erhält auf unterschiedlichen Screens dieselbe Bezeichnung.

## 12. Gemeinsame Styles

Vorhandene semantische Styles wiederverwenden, insbesondere:

```text
PrimaryCompactButton SecondaryActionButton TextActionButton DangerButton
HeaderDivider InlineFormEntry IconButton CardBorder SelectionDialog FilterChip
GroupPlayerCard GroupPlayerText GroupPlayerAction GroupPlayerRemoveAction
GroupPlayerPalette
```

Neue globale Styles nur bei realem mehrfachen Bedarf.
