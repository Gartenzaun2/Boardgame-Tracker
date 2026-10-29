# Boardgame Tracker – Screens und Navigation

Dieses Dokument beschreibt Screenaufbau, Interaktionen, Dialogbestand und offene
UI-Codeabweichungen. Produktregeln stehen in `PRODUCT_SPEC.md`, gemeinsame
Gestaltung und Farbwerte in `UI_DESIGN_GUIDE.md`. Referenzgerät und Touch-Größen
werden dort verbindlich festgelegt; der frühere HTML-Prototyp ist keine weitere Quelle.

## Hauptnavigation und App-Start

Die Shell besitzt `Spiele · Gruppen · Historie · Einstellungen`. Unter „Gruppen“
steht der aktuelle Spielname; es gibt dort keinen spielunabhängigen Kontext.
Ein Spiel-Tap setzt den Kontext und öffnet die Gruppenübersicht. Der Gruppen-Tab
führt ebenfalls zur Übersicht im gewählten Kontext.

Der gespeicherte Hauptbereich bzw. die konkrete noch offene Partie wird gemäß
`PRODUCT_SPEC.md` wiederhergestellt. Ohne früheren Kontext führt ein Standardspiel
zu dessen Gruppen, andernfalls startet die App in „Spiele“. Die Auswahl einer
konkreten offenen Partie folgt nicht pauschal dem letzten Änderungsdatum.

## Gruppenverwaltung

### Gruppenübersicht

Kopf: „Gruppen“, aktueller Spielname, HeaderDivider. Darunter die beiden Filter
„Temporär“ und „Archiviert“ nebeneinander sowie die Gruppenliste. Beide Kategorien
sind anfangs ausgeblendet. „Neue Gruppe“ ist die Primary-Aktion.

Eine Surface-Karte zeigt Gruppenname, Mitglieder und letzte Nutzung für dieses
Spiel. Ein transparentes, im Theme-Textton getöntes Aktiv-Icon neben dem Namen
kennzeichnet eine laufende Partie oder offene Wertung. Rechts steht das
zurückhaltende Bearbeiten-Icon als eigene Touch-Fläche.

- Karten-Tap: offene Partie/Wertung fortsetzen; ohne offene Partie Vorbereitung öffnen.
- Bearbeiten-Tap: Gruppenformular öffnen; keine Partievorbereitung auslösen.
- Keine separate Gruppendetailseite im normalen Bedienfluss.

### Gruppe anlegen

Kopf auf AppBackground: Zurück, Titel, Info und Primary-Speichern; feine Unterkante.

```text
Gruppenname     [z. B. Freitagabend]
Temporäre Gruppe                 ○

Gruppenmitglieder  [Info]
Noch keine Spieler in der Gruppe.
[+ Vorhandenen Spieler hinzufügen]

Neuen Spieler anlegen
Name            [z. B. Alex]
Gast                             ○
[Spieler hinzufügen]
```

Stammdaten und neue Spieler sind flache Inline-Formulare mit gleichem
Label-links/Eingabe-rechts-Muster. Keine Karten um Formularsektionen, keine
zusätzlichen Divider. Bereits hinzugefügte Spieler sind eigene interaktive Kacheln
mit Hoch-/Runter-/Entfernen-Aktionen; Drag & Drop ist kein aktueller Bestandteil.

Bei Spielen mit Farben ist ein Palettenbutton sichtbar. Zugewiesene Farben tönen
die ganze Spielerkachel; Texte und Icons verwenden die gekoppelte Spieler-Textfarbe.
Symbolgrößen und komfortable Touch-Flächen stehen im Design Guide.

„Vorhandenen Spieler hinzufügen“ öffnet einen SelectionDialog. Ein Tap fügt hinzu
und schließt; bereits enthaltene Spieler fehlen in der Auswahl. Gibt es keine
verfügbaren Spieler, ersetzt ein Leerzustand den Button. Neue Spieler werden
erst mit der Gruppe gespeichert. Hinweise zu Gruppe/Reihenfolge stehen hinter Info-Icons.

### Gruppe bearbeiten

Gleicher Header und dieselbe Formularsprache wie bei der Anlage. Bearbeitbar sind
Name, Reihenfolge, Archivstatus und optionale Farbdefaults des aktuellen Spiels;
temporäre Gruppen dürfen permanent werden. Die Mitgliedermenge bleibt fest:
keine Spieler hinzufügen oder entfernen. Änderungen bleiben bis Speichern im Entwurf.

### Gruppen-Spielerfarbe auswählen

Gemeinsamer SelectionDialog mit ausdrücklichem Spielbezug, z. B. „Farbe für
Arche Nova festlegen“, und Spielername. Die Liste enthält erlaubte Farben und
„Keine Farbe“. Aktuelle Auswahl: SelectedSurface/SelectedBorder plus Häkchen.
Ein Tap übernimmt in den Entwurf und schließt; Schließen/System-Zurück bricht ab.
Tausch belegter Defaultfarben und atomare Speicherung sind Produktregeln.

## Partievorbereitung

```text
←  Partie vorbereiten             [Würfel]
   <Gruppenname>
──────────────────────────────────────────
LETZTE PARTIE
<Datum und alle Platzierungen oder Leerzustand>
[Zoopläne auslosen] [Reihenfolge rotieren]

1  Anna                           [Palette]
   Blau
   [Plan 3 / Skizze] [Plan 11 / Skizze]
...
                               [Starten]
```

„Letzte Partie“ ist flache Metainformation ohne Karte. Ausgangsreihenfolge,
automatische Rotation zum letzten Verlierer und Farbdefaults folgen der Produktspezifikation.
Ohne gespeicherte Defaults werden eindeutige Farben vorgeschlagen. Der Würfel
animiert kurz und lost den Startspieler durch zyklische Rotation aus; die
separate Rotationsaktion verschiebt um eine Position. Relative Reihenfolge bleibt erhalten.

Je Spieler stehen zwei quadratische Zooplanskizzen mit Nummer nebeneinander,
ohne zusätzlichen Plannamen. Aktuelle Skizzen verwenden `Fill`. Initial ist kein
Plan ausgewählt. Ein Skizzen-Tap markiert die ganze Kachel mit Auswahlkontur und
zusätzlicher sichtbarer Auswahlmarkierung.

Die Partiefarbe wird über die vorhandene Inline-Auswahl gewählt; die aktuelle
Farbe wird dort nicht nochmals angeboten. Farbänderungen gelten nur für diese
Partie. Doppelte Farben können im Entwurf vorkommen, verhindern aber den Start.
„Starten“ bleibt unten; Auslosen und Rotieren sind Secondary-Aktionen.
Fehlerzeilen reservieren nur bei vorhandenem Text Platz.

## Laufende Partie

Kopf mit Gruppe und bisheriger Dauer. Spieler erscheinen in Startreihenfolge
mit Farbe und gewähltem Zooplan. Die Karten verwenden die gemeinsame
Spielerfarben-Palette. „Partie beenden“ ist Primary und führt in die Wertung;
„Verwerfen“ ist sekundär destruktiv und verlangt Bestätigung.

## Wertungsübersicht und Ergebnis

Wertungskarten reservieren eigene Zeilen für Name/Punkte, Farbe/Zooplan,
Attraktivität/Artenschutz und Kartenanzahl/optionalen Ruf. Das neutrale
Besondere-Karten-Icon besitzt keinen Zähler; es öffnet die konkrete Kartenwahl.
Ein Tap auf den übrigen Kartenbereich öffnet die Spielereingabe. Der Kamera-Button
ist in der laufenden Wertung sichtbar und zeigt derzeit den Hinweis zur noch
nicht implementierten Fotoerkennung. Einstellungen sind aus der Wertung erreichbar.

Die Kartenwahl zeigt geordnete Symbolgrafiken vor dem aktuellen Namen und den
Zuordnungsstatus. Bereits dem Zielspieler zugeordnete Karten sind zusätzlich
hervorgehoben. Ein Tap ordnet zu oder entfernt und schließt. Bei Mehrfachzuordnung
an andere Spieler bleibt die ausdrückliche Bestätigung erhalten. Archivierte
Karten erscheinen nur beim bereits zugeordneten Zielspieler zum Entfernen.

Der Ergebnis-Screen zeigt gespeicherte Platzierungen, Punkte und Rohwerte sowie
Spezialkarten mit aktuellen Symbolen/Namen. Spielerflächen behalten ihre Farbpaare;
Kartenbezeichnungen mit Symbolen erhalten eine neutrale Surface-Unterlage.
Er bleibt bis zum expliziten „Fertig“ geöffnet.

## Spielereingabe

Unter Spielername und Headerlinie stehen Attraktivität und Artenschutz als zwei
Zahlenräder nebeneinander. Bezeichnungen, Wertebereiche und Vorschlagsstatus
stehen mittig. Räder: 176 dp hoch; direkt editierbare Zahlenfelder: 48 dp hoch.

Die Spalten sind durch 32 dp getrennt. Ihre Außenkanten fluchten mit Punktefläche
und Fertig-Button, ohne zusätzliche seitliche Einrückung. Rad und Zahlenfeld haben
dieselbe Breite und 16-dp-Rundung über CardBorder. Dazwischen liegen 16 dp, damit
das Drehen nicht versehentlich die Tastatur öffnet. Vor den Punkten folgen 32 dp Abstand.

Wenn „Ruf erfassen“ in den Spieleinstellungen aktiv ist, steht unter den Punkten
ein horizontaler 1–15-Slider mit direkt editierbarem Zahlenfeld; kein zusätzlicher
Erfassungsschalter. Wertebereiche, Mittelwertvorschläge, „Fertig“ und Zurück
folgen `PRODUCT_SPEC.md`, Abschnitt „Wertung“.

Bedienhinweise sind ausschließlich hinter dem Info-Symbol im Kopf erreichbar.
Vorschlagskennzeichnung und Fehler bleiben Statusinformationen. Bei großer Schrift
und eingeblendeter Tastatur ist der Inhalt scrollbar; „Fertig“ bleibt unten.
Historische Korrekturen verwenden denselben Screen und den bestehenden Entwurf.
Bei verborgener Rufeingabe bleiben gespeicherte Rufwerte im Korrekturentwurf erhalten.

## Historie

Spiel- und Gruppenfilter stehen als kompakte, bei Platzmangel umbrechende Chips
oberhalb der Liste. Ein Häkchen kennzeichnet gesetzte Filter. Dahinter öffnen
native Picker; unsichtbare Picker reservieren keinen zusätzlichen vertikalen Platz.

Die Liste gruppiert Partien nach lokalem Kalendermonat, z. B. „Oktober 2026“.
Monatsüberschriften sind einfache Texte ohne Karte und ohne Sticky-Verhalten.
Jede Partiekarte enthält ungefähr drei umbrechende Zeilen:

```text
Arche Nova · 2 h 14 min · 26.10.26
Freitagabend
1. Anna · 1. Ben · 3. Chris
```

Filter, Completed-Auswahl, Datumssortierung und aktuelle Namen folgen der
Produktspezifikation. Leer-/Fehlerzustand und Busy-Schutz gehören zum Screen.
Tap öffnet die spielabhängige Wertung, keine generische Detailseite.

In der historischen Wertung steht die gespeicherte Platzierung vor dem Namen;
maßgeblich ist Placement, nicht die Punktzahl. „Korrektur speichern“ übernimmt
atomar und öffnet eine Gleichstandsauflösung, wenn die Neuberechnung dies
verlangt. Unveränderte Punkte behalten gültige gespeicherte Placements.
Zurück verwirft ungespeicherte Entwürfe. „Partie löschen“ ist sekundär destruktiv
und erfordert Bestätigung. Statistik und deren eigene Filter folgen separat.

## Einstellungen und Spezialkartenverwaltung

Allgemeine Einstellungen enthalten Standardspiel und „Darstellung“ mit Deep
Forest, Königsblau und Dunkel. Struktur bleibt über alle Themes gleich. Farbe,
Shell-TabBar und aktive Auswahl richten sich nach dem Design Guide.

Arche-Nova-Einstellungen enthalten „Ruf erfassen“, alte/neue Punkteanzeige und
„Spezialkarten“. Dort: Header mit Zurück, „Archivierte anzeigen“, scrollbare
Kartenliste und „Neue Spezialkarte“ unten. Zeilen zeigen 0–3 Symbole in gespeicherter
Reihenfolge vor dem Namen, darunter Archivstatus und Bearbeiten/Archivieren bzw.
Entarchivieren. Archiviert wird zusätzlich ausgeschrieben; keine Löschaktion.

Gemeinsamer Create-/Edit-Screen: Kopf, Namenseingabe, Symbol-/Namensvorschau,
drei nummerierte Symbolpositionen, Speichern. Folgepositionen werden lückenlos
angeboten; Leeren rückt nachfolgende Symbole nach links. Der Symbolwähler zeigt
echte Grafiken, lokalisierte Namen und aktuelle Auswahl mit Kontur/Farbe plus
Häkchen im SelectionDialog. Schließen/System-Zurück verwirft nur die Auswahl.
Wiederholungen sind wählbar. Busy-Overlays blockieren Eingaben; Fehler sind nur
bei vorhandenem Text sichtbar.

## Dialogübersicht

| Dialog / Auswahl | Ort | Stand / verbleibende Anpassung |
|---|---|---|
| Vorhandenen Spieler auswählen | Gruppe anlegen | App-Dialog mit SelectionDialog, Surface-Einträgen und Text-Schließen; umgesetzt. |
| Besondere Karten auswählen | Wertungsübersicht, einschließlich Historie | Derselbe SelectionDialog und Text-Schließen; umgesetzt. Auswahl zusätzlich durch Zuordnungstext sichtbar. |
| Spezialkarten-Symbol wählen | Spezialkarten-Editor | App-SelectionDialog mit Grafiken, Namen und sichtbarer aktueller Auswahl; umgesetzt. |
| Gruppen-Spielerfarbe wählen | Gruppe anlegen / bearbeiten | Gemeinsamer App-Dialog „Farbe für {Spiel} festlegen“ mit Spielername, erlaubten Farben und „Keine Farbe“; entfällt bei Spielen ohne Farben. |
| Partiefarbe wählen | Vorbereitung | Bestehende Inline-Auswahl für diese Partie; verwendet dieselben semantischen Farben und die Arche-Nova-Teilmenge. |
| Spiel-/Gruppenfilter | Historie | Chips umgesetzt; dahinter native Picker, deren Auswahlliste noch separat zu gestalten wäre. |
| Darstellung / Standardspiel | Allgemeine Einstellungen | Inhaltskarten umgesetzt; native Picker-Auswahl noch ohne appidentische Dialoggestaltung. |
| Informationen zu Spielen | Spiele | Native Info-Abfrage; AppBackground, Kontur und Textaktion noch nicht zentral kontrolliert. |
| Informationen zur Gruppe und Reihenfolge | Gruppe anlegen / bearbeiten | Native Info-Abfragen; gleiche offene Gestaltung. |
| Informationen zu Einstellungen / Standardspiel | Allgemeine Einstellungen | Native Info-Abfragen; gleiche offene Gestaltung. |
| Doppelte Spielerfarben / Start nicht möglich | Vorbereitung | Nativer Hinweis; gleiche offene Gestaltung. |
| Laufende Partie verwerfen | Laufende Partie | Native Sicherheitsabfrage; explizite Bestätigung vorhanden, appidentische Danger-/Abbrechen-Gestaltung noch offen. |
| Wertung verwerfen / abgeschlossene Partie löschen | Wertungsübersicht / Historie | Native Sicherheitsabfrage; explizite Bestätigung vorhanden, gleiche offene Danger-Gestaltung. |
| Mehrfache Spezialkarte bestätigen | Besondere-Karten-Auswahl | Native Ja-/Abbrechen-Abfrage; gleiche offene Button-/Flächengestaltung. |
| Speichern / Korrektur fehlgeschlagen | Spielereingabe, Wertungsübersicht, Gleichstandsauflösung | Native Fehlermeldung; gleiche offene Gestaltung. |
| Fotoerkennung noch nicht verfügbar | Wertungsübersicht | Nativer Hinweis; gleiche offene Gestaltung. |

Native Dialoge folgen derzeit dem Plattformdialog und erhalten nicht die zentralen XAML-Buttonstyles. Sie werden hier bewusst als verbleibende Arbeit aufgeführt. Eine spätere Vereinheitlichung muss Informations-, Fehler-, Auswahl- und destruktive Bestätigungsrollen sowie System-Zurück/Abbrechen erhalten. Gerätesichtprüfung in allen drei Themes bleibt erforderlich.

## Bekannte Codeabweichungen – separat zu beheben

Die folgenden Punkte wurden beim Dokumentationsabgleich bestätigt und mit dem
Benutzer ausdrücklich zur späteren Behebung festgehalten. Die geltenden Regeln
bleiben bestehen; diese Liste erteilt keinen automatischen Änderungsauftrag.

| Punkt | Ist-Stand und betroffene Stelle | Geltende Regel / spätere Korrektur |
|---|---|---|
| Kleiner Text auf AppBackground | [Spielereingabe](../src/BoardgameTracker.App/Games/ArkNova/Features/GameSessions/Scoring/ArkNovaPlayerScoringPage.xaml): Bereiche/Vorschläge verwenden TextSecondary direkt auf AppBackground. Deep Forest ca. 3,67:1, Königsblau ca. 3,68:1. | Kleiner Text auf AppBackground verwendet TextPrimary; Paletten unverändert lassen. Siehe Kontrastregel im Design Guide. |
| Lokalisierung der Ergebnisanzeige | [Ergebnis-ViewModel](../src/BoardgameTracker.App/Games/ArkNova/Features/GameSessions/Result/ArkNovaResultViewModel.cs): Zooplan-, Rohwert-, Ruf- und Punktetexte stehen teilweise direkt im Code. | Sichtbare Sprachtexte über die vorhandenen Lokalisierungsressourcen führen. |
| Android-Zurück im Spezialkarten-Picker | [Wertungsseite](../src/BoardgameTracker.App/Games/ArkNova/Features/GameSessions/Scoring/ArkNovaScoringPage.xaml.cs): OnBackButtonPressed navigiert zur Gruppen-/Historienübersicht, auch wenn die Auswahl geöffnet ist. | Zunächst nur den geöffneten Picker schließen; erst ohne Auswahl die Seite verlassen. |

Die Sichtprüfung auf dem Zielgerät in allen drei Themes ist noch nicht als
abgeschlossen bestätigt. Bei inkompatiblen Änderungen wird der Start nach
Neuinstallation mit frischer Datenbank geprüft. Prüfablauf:
`SETUP.md`. Diese Dokumentationsbereinigung verändert keinen App-Code.
