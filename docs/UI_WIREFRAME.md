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
separate Rotationsaktion verschiebt um eine Position und erhält die relative Reihenfolge.
Kompakte Hoch-/Runter-Iconbuttons verschieben einzelne Spieler innerhalb der
konkreten Partiereihenfolge. An den Listenenden ist die jeweilige Richtung deaktiviert;
Gruppen-Defaults werden nicht geändert.

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
öffnet in der laufenden Wertung die Auswahl Kamera/Systempicker. Ein Info-Icon
erklärt Vorschlagsstatus und Bestätigungspflicht. Einstellungen bleiben erreichbar.

Die Kartenwahl zeigt geordnete Symbolgrafiken vor dem aktuellen Namen ohne
sichtbare Zuordnungszeile. Für die Darstellung gilt:

| Zuordnung | Hintergrund | Rahmen |
|---|---|---|
| Nicht zugeordnet | Surface, keine SelectedSurface-Markierung | normale Outline, kein gestrichelter Danger-Rahmen |
| Nur dem aktuell ausgewählten Spieler zugeordnet | SelectedSurface | normale Outline, kein gestrichelter Danger-Rahmen |
| Einem anderen Spieler zugeordnet | Surface, keine SelectedSurface-Markierung | gestrichelter Danger-Rahmen |
| Dem aktuellen und mindestens einem anderen Spieler zugeordnet | SelectedSurface | gestrichelter Danger-Rahmen |

SelectedSurface bedeutet „gehört dem aktuell ausgewählten Spieler“; der gestrichelte
Danger-Rahmen bedeutet „mindestens einem anderen Spieler zugeordnet“.
Der Zuordnungsstatus bleibt für Screenreader beschrieben.
Der Zurück-Pfeil im bestehenden IconButton-Stil verlässt nur die Kartenwahl.
Ein Tap ordnet zu oder entfernt und schließt. Bei Mehrfachzuordnung
an andere Spieler bleibt die ausdrückliche Bestätigung erhalten. Archivierte
Karten erscheinen nur beim bereits zugeordneten Zielspieler zum Entfernen.

Bei Punktgleichstand führt der Abschluss in die Gleichstandsansicht mit neutralen
Platzüberschriften und Dividern für `1..n`. Unter jeder Überschrift stehen null,
ein oder mehrere aktuell zugeordnete Spieler; leere Zielgruppen bleiben sichtbar.
Eine Spielerzeile zeigt in einer Reihe Name, kleine Platz-Chips und rechtsbündige
Punkte. Die Platznummer wird nicht am Namen wiederholt. Der Zeilenhintergrund und
die Texte verwenden das bestehende Spielerfarbpaar, ohne Farbe die neutrale
Theme-Darstellung. Platzüberschriften und Chips übernehmen keine Spielerfarbe.
Die Chips verwenden Surface/Outline bzw. SelectedSurface/SelectedBorder.

Nur Mitglieder eines Gleichstandsblocks erhalten dessen erlaubte Platz-Chips.
Die Auswahl verschiebt ausschließlich diesen Spieler in die gewählte Gruppe,
ohne Tausch oder Neuverteilung. Geteilte Plätze und leere Gruppen sind auch beim
Speichern erlaubt, sofern die Zuordnung innerhalb des jeweiligen Tie-Blocks liegt.
Pfeile und Drag & Drop entfallen. Das Info-Icon öffnet die Erklärung im vorhandenen
App-Dialog; kein permanenter Hilfetext. Gespeicherte Zuordnungen werden beim
Wiederöffnen erneut in dieselben Platzgruppen projiziert.

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
oberhalb der Liste. Ein Häkchen kennzeichnet gesetzte Filter. Die Chips öffnen
die App-Auswahl direkt; aktuelle Einträge sind markiert.

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

Die allgemeinen Einstellungen zeigen `Darstellung`, `Standardspiel`, `Spieler`
und anschließend die spielspezifischen Einstellungen. Die Spielerübersicht
verwendet Header mit Zurück/Info, Gäste-/Archiv-Switchfilter (anfangs beide aus) und
kompakte Surface-Karten. Name und Edit-Icon stehen oben, darunter klein `Erstellt …`.
Ein eigener Secondary-Button `Zuletzt gespielt …` öffnet die letzte Partie,
einschließlich laufender Partien. Ohne Partie steht dort ein neutraler, nicht
klickbarer Bereich `Noch keine Partie`. Gast-/Archiv-Chips folgen nur bei Bedarf.
Die übrige Karte besitzt keine Bearbeitungsgeste. Der Editor enthält Name,
Archiv-Switch und Speichern; Änderungen bleiben bis Speichern ein Entwurf.
Hilfetext liegt ausschließlich hinter dem Info-Icon.

Allgemeine Einstellungen enthalten Standardspiel und „Darstellung“ mit Deep
Forest, Königsblau und Dunkel. Struktur bleibt über alle Themes gleich. Farbe,
Shell-TabBar und aktive Auswahl richten sich nach dem Design Guide.
Ohne gespeicherte Auswahl ist Dunkel voreingestellt; eine gespeicherte Wahl bleibt erhalten.

Arche-Nova-Einstellungen enthalten „Ruf erfassen“, alte/neue Punkteanzeige und
„Spezialkarten“. Ein eigener Abschnitt „Spielmaterial“ bietet den Sprachselektor
„Sprache“ (aktuell ausschließlich Deutsch). Die Auswahl wird sofort als allgemeine
Game-Preference gespeichert, unabhängig von den Ark-Nova-Wertungseinstellungen.
„Nutzer“ ist keine Auswahloption. Das Info-Icon im Sektionsheader erklärt knapp:
„Betrifft nur sprachabhängiges Spielmaterial. Die App-Sprache bleibt unverändert.“
Vorhandene Einstellungskarten, Theme-Ressourcen und compiled bindings bleiben maßgeblich.

Unter „Spezialkarten“: Header mit Zurück und Info-Icon, „Archivierte anzeigen“, scrollbare
Kartenliste und „Neue Spezialkarte“ unten. Zeilen zeigen 0–3 Symbole in gespeicherter
Reihenfolge vor dem Namen und das Bearbeiten-Icon direkt daneben ohne eigene
Aktionszeile. Archiviert wird zusätzlich ausgeschrieben; keine Löschaktion oder
Archivierungssteuerung in der Liste. Das Info-Icon öffnet den lokalisierten
App-Hinweis zur Zuordnung von Karten zu Spielern/Partien und späteren Auswertung.

Gemeinsamer Create-/Edit-Screen: Kopf, Namenseingabe mit aktuell aufgelöstem Namen, Symbol-/Namensvorschau,
drei nummerierte Symbolpositionen, Speichern. Neue oder geänderte Namen werden als
Nutzerressource der Karten-GUID gespeichert; reine Symboländerungen erzeugen keine
Nutzerressource. Es gibt keinen vollständigen Kartenreset. Nur beim Bearbeiten einer bestehenden
Karte erscheint zusätzlich „Archiviert“ mit Switch; Speichern übernimmt den Status,
Zurück verwirft ungespeicherte Änderungen. Folgepositionen werden lückenlos
angeboten; Leeren rückt nachfolgende Symbole nach links. Der Symbolwähler zeigt
echte Grafiken, lokalisierte Namen und aktuelle Auswahl mit Kontur/Farbe plus
Häkchen im SelectionDialog. Schließen/System-Zurück verwirft nur die Auswahl.
Wiederholungen sind wählbar. Busy-Overlays blockieren Eingaben; Fehler sind nur
bei vorhandenem Text sichtbar.

## Fotoerkennung – implementierter Review, Produktfreigabe offen

Foto aufnehmen oder vorhandenes Foto wählen → gemeinsame Analyse → editierbare
Vorschläge mit `✓ Sicher`, `! Prüfen`, `? Unsicher` und Grün/Gelb/Rot → explizit
bestätigen → speichern. Info-Icon im Wertungsheader erklärt die Status im
AppDialogPage. Native eingebettete Picker bei stabiler Plattformunterstützung,
sonst normale Systemauswahl; keine eigene Galerie. Analysefehler erhalten die
gespeicherten Werte, Abbruch bleibt neutral. Quellenauswahl und Review deaktivieren
den darunterliegenden Wertungsinhalt; separate Entwurfsfelder verhindern Autosave.
Die Produktfreigabe hängt weiter an nativer Backend-Parität. Der implementierte
iOS-Pfad benötigt manuelle Apple-Verifikation. Details: [CV-Status](ARK_NOVA_CV_STATUS.md).

## Dialogübersicht

Page-interne Auswahlen (besondere Karten, Kartensymbole, vorhandene Spieler und
Gruppenfarben) deaktivieren während ihrer Anzeige den gesamten darunterliegenden
Page-Inhalt einschließlich Header und Aktionen. Nur die Auswahl bleibt bedienbar;
die globale Shell-/Bottom-Navigation bleibt erreichbar. System-Zurück schließt
zunächst die Auswahl. Die gemeinsame Modalitätsregel steht im UI Design Guide.

| Dialog / Auswahl | Ort | Stand / verbleibende Anpassung |
|---|---|---|
| Vorhandenen Spieler auswählen | Gruppe anlegen | Page-interner SelectionDialog mit Zurück-Pfeil links im Header und Surface-Einträgen; umgesetzt. |
| Besondere Karten auswählen | Wertungsübersicht, einschließlich Historie | SelectionDialog mit Zurück-Pfeil; kompakte Karten ohne sichtbare Statuszeile. Gestrichelter Danger-Rahmen nur bei Zuordnung zu mindestens einem anderen Spieler, SelectedSurface bei Zuordnung zum aktuell ausgewählten Spieler. Bei Zuordnung zu beiden gelten beide Markierungen; ausschließlich eigene Zuordnung erhält keinen gestrichelten Rahmen. |
| Spezialkarten-Symbol wählen | Spezialkarten-Editor | Page-interner SelectionDialog mit Zurück-Pfeil links im Header, Grafiken, Namen und sichtbarer aktueller Auswahl; umgesetzt. |
| Gruppen-Spielerfarbe wählen | Gruppe anlegen / bearbeiten | Gemeinsamer Page-Dialog mit Zurück-Pfeil links im Header, „Farbe für {Spiel} festlegen“, Spielername, erlaubten Farben und „Keine Farbe“; entfällt bei Spielen ohne Farben. |
| Partiefarbe wählen | Vorbereitung | Bestehende Inline-Auswahl für diese Partie; verwendet dieselben semantischen Farben und die Arche-Nova-Teilmenge. |
| Spiel-/Gruppenfilter | Historie | Chips öffnen AppDialogPage mit aktueller Auswahl und Text-Schließen. |
| Darstellung / Standardspiel | Allgemeine Einstellungen | AppDialogPage mit aktuellem Wert, Auswahlmarkierung und Text-Schließen. |
| Informationen zu Spielen | Spiele | AppDialogPage mit AppBackground, Outline und Textaktion. |
| Informationen zur Gruppe und Reihenfolge | Gruppe anlegen / bearbeiten | Derselbe App-Informationsdialog; Formulareingaben bleiben erhalten. |
| Informationen zu Einstellungen / Standardspiel | Allgemeine Einstellungen | Derselbe App-Informationsdialog. |
| Doppelte Spielerfarben / Start nicht möglich | Vorbereitung | App-Hinweis; Entwurf bleibt erhalten. |
| Laufende Partie verwerfen | Laufende Partie | App-Sicherheitsabfrage mit DangerButton und Text-Abbrechen. |
| Wertung verwerfen / abgeschlossene Partie löschen | Wertungsübersicht / Historie | App-Sicherheitsabfrage mit expliziter destruktiver Bestätigung und Text-Abbrechen. |
| Mehrfache Spezialkarte bestätigen | Besondere-Karten-Auswahl | App-Bestätigung mit Ja-/Abbrechen; Kartenpicker und Entwurf bleiben erhalten. |
| Speichern / Korrektur fehlgeschlagen | Spielereingabe, Wertungsübersicht, Gleichstandsauflösung | App-Fehlermeldung; Entwurf bleibt erhalten. |
| Fotoquelle / Fotowerte prüfen | Laufende Wertung | Page-interne Quelle Kamera/Systempicker; editierbarer Review mit Confidence und explizitem Bestätigen/Speichern oder Abbrechen. |
| Fotoerkennung erklären | Wertungsübersicht | Info-Icon öffnet AppDialogPage; Grün bleibt prüfpflichtiger Vorschlag. |

System-Zurück schließt zunächst den modalen Dialog und bestätigt keine Aktion.
Bei darunter geöffnetem Spezialkarten-Picker schließt erst das nächste Zurück
auch diesen; die Wertungsseite bleibt bis dahin sichtbar.

## Aktuelles UI-Verhalten

- Spielereingabe: Bereiche und Vorschläge verwenden TextPrimary auf AppBackground.
- Ergebnisanzeige: Zooplan, Rohwerte, Ruf und Punkte verwenden Lokalisierungsressourcen.
- Android-Zurück schließt zuerst den geöffneten Spezialkarten-Picker; erst ohne Auswahl wird die Wertungsseite verlassen.

## Bekannte Codeabweichung – separat zu beheben

Beim Tabwechsel auf eine bereits geöffnete Unterseite speichert
`AppShell.OnShellNavigated` den neuen Hauptbereich nicht zuverlässig, weil es nur
auf Routen prüft, die mit einer Hauptseite enden. Reproduktion: Historische
Wertungsübersicht geöffnet lassen, zu Einstellungen wechseln, zurück zur
historischen Wertungsübersicht wechseln und App neu starten. Die App öffnet dann
Einstellungen statt Historie. Die Wiederherstellung nach Besuch der eigentlichen
Historien-Hauptseite funktioniert. Der Fehler wurde im Emulator bestätigt und
ist nicht Teil der Dialog- oder Altkompatibilitätsbereinigung.

Die Anleitung für ausdrücklich beauftragte Geräteprüfungen steht in `SETUP.md`.

## Historie – lokaler Import/Export

Feste untere Aktionsleiste außerhalb der scrollenden Partienliste:

```text
[Importieren] [23 Partien exportieren]
 Secondary            Primary
```

Bei null Ergebnissen ist Export deaktiviert. Import öffnet die native Dateiauswahl,
Export erzeugt zuerst einmal den validierten Inhalt und öffnet dann den App-Dialog:

```text
Exportieren
[Speichern]
[Teilen]
[Abbrechen]
```

Speichern öffnet den nativen Dokument-Picker mit vorgeschlagenem .bgt-Dateinamen und
Speicherortwahl; Teilen öffnet den nativen Share-Sheet. Die Auswahl verändert die
exportierte Partiemenge nicht. Jeder Abbruch beendet den Flow ohne Fehler oder zweite
Ausgabeaktion. Busy-Overlay und Aufrufschutz sichern den Flow.
Es gibt keinen zusätzlichen Einstieg unter Einstellungen.

Resolve verwendet App-Auswahldialoge: importierte Gruppe mit sicheren Zuordnungen,
„Spieler zuordnen und Gruppen zusammenführen“ oder „Gruppen trennen“. Nur noch
zulässige lokale Spieler sind auswählbar. Schließen bricht den Import ab. Danach
folgt die vollständige Importvorschau mit Zusammenfassung, neuen Gruppennamen und
„Jetzt importieren“/Abbrechen. Erst Bestätigen schreibt. Fehler/Erfolg verwenden den
bestehenden AppDialogPage. Nach Erfolg wird die Partienliste aktualisiert.
