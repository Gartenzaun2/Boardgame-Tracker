# Boardgame Tracker – Produktspezifikation v0.2

## Ziel

Mobile App zur Erfassung und Auswertung von Brettspielpartien.

Die Grundlage soll mehrere Spiele unterstützen. Spieler, Gruppen,
Partien und Historie sind spielübergreifend; Wertung, Eingabefelder,
Bildauswertung und detaillierte Statistiken sind spielspezifisch.

Erstes vollständig unterstütztes Spiel: **Arche Nova**.

## Spielübergreifende Konzepte

### Player

- `PlayerId`
- `Name`
- `IstGast`
- optional `IstAktiv`

Ein Gast ist ein normaler Spieler mit `IstGast = true`. Wird der Spieler
später regulär gepflegt, wird lediglich `IstGast = false` gesetzt.
Historische Partien bleiben dadurch automatisch erhalten.

### Gruppe

- feste Zusammensetzung von Spielern
- kann regulär oder temporär sein
- temporäre Gruppen bleiben in der Historie erhalten, werden aber
  standardmäßig nicht in der normalen Gruppenübersicht/-statistik
  angezeigt

### GroupPlayer

n:m-Zuordnung zwischen `Group` und `Player`.

### Sitzreihenfolge in der Gruppe

Für jede Gruppe wird eine Default-Reihenfolge der Spieler gepflegt.
Diese Reihenfolge wird bei einer neuen Partie übernommen.

Sie kann im Gruppenmenü bzw. bei der Gruppenerstellung per Verschieben
festgelegt werden.

### Spiel

Im MVP existiert zunächst nur **Arche Nova**. Weitere Spiele können
später eigene typisierte Datenmodelle erhalten.

## App-Start und Navigationskontext

Die Hauptnavigation lautet **Spiele · Gruppen · Historie · Einstellungen**.
Die Gruppenübersicht besitzt immer einen ausgewählten Spielkontext; eine
spielunabhängige Gruppenübersicht gibt es im normalen Startflow nicht.

Lokale Navigationseinstellungen speichern:

- optionales `Standardspiel`,
- zuletzt ausgewähltes Spiel,
- zuletzt verwendeten Hauptbereich,
- falls zuletzt eine laufende Partie oder Wertung geöffnet war: genau diese offene Partie.

Startverhalten:

1. War zuletzt eine konkrete noch offene Partie aktiv, wird genau diese wieder geöffnet.
2. Andernfalls wird der zuletzt verwendete Hauptbereich wiederhergestellt.
3. Gibt es noch keinen gespeicherten Navigationskontext und ist ein Standardspiel gesetzt, startet die App in **Gruppen** für dieses Spiel.
4. Ohne gespeicherten Kontext und ohne Standardspiel startet die App in **Spiele**.

Je Spiel und Gruppe ist maximal eine offene Partie erlaubt. Andere Gruppen oder Spiele dürfen parallel offene Partien haben. Damit können mehrere Partien gleichzeitig offen sein, ohne beim
App-Start willkürlich die zuletzt geänderte Partie auszuwählen.

## Gruppenübersicht

- immer im Kontext des aktuell gewählten Spiels,
- Spielname direkt unter der Überschrift `Gruppen`,
- Gruppen nach letzter Nutzung **für dieses Spiel** sortiert,
- reguläre Gruppen standardmäßig sichtbar,
- temporäre Gruppen standardmäßig verborgen,
- Button für neue Gruppe,
- jede Gruppe besitzt rechts ein kleines Bearbeiten-Icon,
- besitzt eine Gruppe für das ausgewählte Spiel eine laufende Partie oder offene Wertung, steht ein Aktiv-Icon direkt neben dem Gruppennamen.

Beim Antippen einer Gruppe wird für das ausgewählte Spiel entweder die offene
Partie fortgesetzt oder die entsprechende Partievorbereitung geöffnet. Ein
separater Gruppendetail-Screen ist im MVP nicht notwendig.

Das Bearbeiten-Icon ist eine getrennte Touch-Fläche. Es öffnet **Gruppe
bearbeiten** und darf nicht die Partievorbereitung auslösen.

## Gruppe anlegen

Die Maske priorisiert die Wiederverwendung vorhandener Spieler vor dem
Anlegen neuer Spieler.

### Kopfzeile

- links: Zurück-Pfeil
- Titel: **Gruppe anlegen**
- direkt beim Titel: Informations-Icon
- rechts: gut sichtbare grüne Aktion **Speichern**
- kein zweiter Seitentitel „Gruppe anlegen“ im Inhaltsbereich

Das Informations-Icon zeigt den Erklärungstext auf Geräten mit Maus per
Hover und auf Touch-Geräten per Tippen.

### Reihenfolge der Inhalte

1.  Gruppenname
2.  Temporäre Gruppe
3.  bereits hinzugefügte Spieler
4.  vorhandene Spieler hinzufügen
5.  neue Spieler anlegen

Der Gruppenname wird kompakt dargestellt: Label links, Eingabefeld
rechts.

### Spieler

Bereits hinzugefügte Spieler werden in ihrer Default-Reihenfolge
angezeigt. Die Reihenfolge kann verändert werden. Für den ersten
funktionalen Stand sind Hoch-/Runter-Aktionen ausreichend; Drag & Drop
kann später ergänzt werden.

### Vorhandene Spieler hinzufügen

Vorhandene aktive Spieler sind der primäre Weg, Spieler
hinzuzufügen.

Die Auswahl erfolgt kompakt über ein Dropdown/Picker. Spieler, die
bereits Spieler der aktuellen Gruppe sind, werden nicht erneut
angeboten.

### Neue Spieler

Das Anlegen eines neuen Spielers ist eine sekundäre Aktion unterhalb der
Auswahl vorhandener Spieler.

Erfasst werden:

- Name
- Gaststatus

Neue Spieler werden erst zusammen mit dem Speichern der Gruppe
persistiert. Ein Abbruch der Gruppenerstellung erzeugt daher keine
verwaisten Spieler.

## Gruppe bearbeiten

Jede regulär angezeigte Gruppe kann über das Bearbeiten-Icon der
Gruppenübersicht geöffnet werden.

Bearbeitbar sind Gruppenname, Default-Reihenfolge, Archivstatus und Arche-Nova-Defaultfarben. Temporäre Gruppen können permanent gemacht werden; die umgekehrte Richtung ist ausgeschlossen.

Die Spielerzusammensetzung einer bestehenden Gruppe wird nicht beliebig
verändert. Eine andere Zusammensetzung repräsentiert fachlich eine
andere Gruppe.

## Neue Arche-Nova-Partie

Flow:

**Spiel auswählen → Gruppe auswählen → Zoopläne auslosen → je Spieler Zooplan wählen →
ggf. Farbe/Reihenfolge ändern → Partie starten**

Ist bereits ein Spielkontext aktiv (z. B. durch Standardspiel oder zuletzt gewähltes
Spiel), beginnt der sichtbare Flow direkt bei der Gruppenübersicht.

### Spielerkacheln

Einheitliches Designkonzept:

- jeder Spieler erhält eine eigene Kachel
- Kachel dezent in Spielerfarbe eingefärbt
- gleiche Grundform bei Vorbereitung, laufender Partie, Wertung und
  Ergebnis
- kleiner Farb-/Regenbogen-/Paletten-Button zum Ändern der Farbe für
  diese Partie

### Default-Farben

Die Standardfarbe ist abhängig von:

- Gruppe
- Spiel
- Spieler

Änderungen auf dem Start-/Partievorbereitungsbildschirm gelten nur für
die konkrete Partie.

Nur Änderungen im Gruppenmenü ändern den Default.

## Arche Nova – Zoopläne

1.  Aussichtsplattform
2.  Freilaufflächen
3.  Silbersee
4.  Handelshafen
5.  Parkrestaurant
6.  Forschungsinstitut
7.  Eisdielen
8.  Hollywood Hills
9.  Geozoo
10. Auffangstation
11. Höhlen
12. Künstliche Intelligenz
13. Reißbrett
14. Lagune

Jeder Spieler erhält zwei Pläne. Jeder Zooplan darf innerhalb einer
Partie insgesamt nur einmal angeboten werden.

Darstellung bevorzugt als große quadratische Buttons mit Nummer und
Ikonografie.

## Startreihenfolge

Die Spieler werden in Sitz-/Spielreihenfolge als Kacheln angezeigt.

Die Default-Reihenfolge stammt aus der Gruppenverwaltung.

Sie kann für die Partie geändert werden.

### Reihenfolge ändern

Die relative Reihenfolge soll erhalten bleiben. Ein Durchrotieren
verschiebt lediglich den ersten Spieler nach hinten.

### Startspieler auslosen

Ein kleiner Icon-Button bestimmt zufällig den Startspieler.

Verhalten:

1.  kurze dezente Animation / Aufglühen
2.  zufällige Anzahl Rotationen der bestehenden Reihenfolge
3.  der ausgeloste Spieler landet auf Position 1
4.  relative Reihenfolge der übrigen Spieler bleibt erhalten

Damit verhält sich die Auslosung so, als wäre eine zufällige Anzahl Male
„Reihenfolge ändern“ gedrückt worden.

## Partie starten

Beim Start werden gespeichert:

- Startzeit
- Spieler
- Reihenfolge
- tatsächliche Spielerfarben
- beide angebotenen Zoopläne
- gewählter Zooplan

## Laufende Partie

Minimalistische Ansicht mit:

- Spielern
- Farben
- Zooplänen
- Reihenfolge
- bisheriger Dauer
- „Partie beenden“
- weniger prominent: „Partie verwerfen“

Verwerfen nur nach Sicherheitsabfrage.

## Partie beenden

Beim Drücken wird die Endzeit gespeichert und die Partie wechselt in den
Zustand **Wertung**.

Wird die App geschlossen, wird die offene Wertung beim nächsten Start
automatisch wieder geöffnet.

## Wertung

Die Wertung ist ein eigener persistenter Arbeitszustand der `GameSession`.
Zwischenstände dürfen während `Scoring` automatisch gespeichert werden, zählen
aber noch nicht als abgeschlossen und erscheinen weder in Historie noch
Statistik. Erst `GameSession.Status = Completed` macht das Ergebnis fachlich
fertig.

Pro Spieler werden erfasst:

- Attraktivität: optional während der Eingabe, für den Abschluss erforderlich,
  Wertebereich 0–113,
- Artenschutz: optional während der Eingabe, für den Abschluss erforderlich,
  Wertebereich 0–41,
- Ruf: optional, Wertebereich 1–15; ob die Eingabe angeboten wird, ist eine
  globale Arche-Nova-Einstellung,
- konkrete Spezialkarten.

Die Spielerwerte werden auf einem eigenen kompakten Eingabescreen mit
numerischer Tastatur erfasst. Gültige Änderungen werden automatisch
zwischengespeichert. Es gibt keinen zusätzlichen „Spieler fertig“-Status.
`Wertung abschließen` wird möglich, sobald für alle Spieler Attraktivität und
Artenschutz vorliegen.

Eine offene Wertung kann über eine weniger prominente Aktion **Verwerfen** nach
Sicherheitsabfrage vollständig entfernt werden.

Ruf und Punkteanzeige sind globale lokale Arche-Nova-Einstellungen. Ist
`Ruf erfassen` deaktiviert, wird für neue Eingaben kein Ruf verlangt und `null`
ist ein gültiger Wert. Bereits abgeschlossene Partien werden durch spätere
Einstellungsänderungen nicht umgeschrieben; fehlender Ruf wird als „nicht
erfasst“ dargestellt.

### Fotoerkennung

Der Kamera-Button erhält bereits im Wertungsscreen seinen endgültigen
prominenten Platz. Die spätere Bildauswertung soll über Spielerfarben:

- Attraktivität,
- Artenschutz,
- Ruf

erkennen und in exakt dieselben Wertungsfelder wie die manuelle Eingabe
vorausfüllen. Alle erkannten Werte bleiben manuell korrigierbar. Es gibt keinen
separaten fachlichen Kamera-Wertungspfad.

Unterstützte Wertungstafeln werden perspektivisch in den
Arche-Nova-Einstellungen ausgewählt, z. B. Original oder Custom kompakt. Die
eigentliche Fotoerkennung ist ein eigener nachfolgender Implementierungsslice.

## Punkteanzeige

Dauerhaft gespeichert werden die Rohwerte Attraktivität und Artenschutz, nicht
die daraus berechneten Punkte.

Für die neue Wertung gilt ein festes Lookup `Artenschutz -> Addend`; die Punkte
sind:

`Punkte_neu = Attraktivität + Addend(Artenschutz)`

Die alte Darstellung ist exakt um 100 Punkte verschoben:

`Punkte_alt = Punkte_neu - 100`

Die globale Arche-Nova-Einstellung `Neue Wertung / Alte Wertung` verändert nur
die Darstellung und niemals historische Rohdaten. Die Arche-Nova-Einstellungen
sind sowohl aus der Wertung als auch über den allgemeinen Bereich
`Einstellungen` der unteren Hauptnavigation erreichbar.

## Spezialkarten

### Elefanten

- Afrikanischer Elefant
- Asiatischer Elefant

### Adler-Kategorie

- Weißkopfseeadler
- Steinadler
- Malaienbär

Der Malaienbär gehört für die Statistik zur Adler-Kategorie.

Bedienung: Jede Spielerkachel bietet **Spezialkarte hinzufügen**. Die Auswahl
öffnet eine kompakte Kachelansicht mit Kontinent-/Tiersymbol und lokalisiertem
Kartennamen; noch nicht zugewiesene Karten werden hervorgehoben. Durch Antippen
einer bereits beim Spieler angezeigten Spezialkarte wird die Zuordnung wieder
entfernt.

Es wird die konkrete Karte gespeichert, nicht nur eine Anzahl. Dieselbe
konkrete Karte darf in seltenen Fällen mehreren Spielern derselben Partie
zugeordnet sein. Beim Versuch einer solchen Mehrfachzuordnung fragt die App
explizit nach, ob wirklich beide Spieler diese Karte hatten. Dieselbe Karte
kann demselben Spieler nicht zweimal zugeordnet werden.

## Gleichstand

Bei Punktgleichstand öffnet `Wertung abschließen` einen eigenen Screen
`Gleichstände bearbeiten`. Alle Platzierungsrahmen bleiben sichtbar; nur die
Spieler eines tatsächlichen Punktegleichstands sind innerhalb ihres
ursprünglichen Rangbereichs verschiebbar. Nicht punktgleiche Spieler bleiben
fest.

Bleiben punktgleiche Spieler im selben Rahmen, bleibt der tatsächliche
Gleichstand bestehen. Competition Ranking wird verwendet, z. B.:

- `1, 1, 3`
- `1, 2, 2, 4`

Bei einem Dreifachgleichstand sind auch Aufteilungen wie `2,2,2`, `2,2,4`,
`2,3,3` oder `2,3,4` möglich, sofern sie dem Competition Ranking entsprechen.
Die Anzahl unterstützter Artenschutzprojekte wird nicht gespeichert.

Nach der gegebenenfalls nötigen Gleichstandsauflösung werden die finalen
`Placement`-Werte gespeichert und die `GameSession` auf `Completed` gesetzt. Der
Ergebnis-Screen bleibt anschließend sichtbar, bis der Nutzer explizit `Fertig`
wählt.

## Historie

Der Hauptbereich Historie bündelt Partien und künftig Statistik. Statistik ist ein eigener nachfolgender Slice und darf eigene umfangreichere Filter nach Spiel, Gruppe, Spieler, Zooplan, besonderen Karten oder Zeitraum erhalten. Ihre Filter sind unabhängig von der Partienliste.

### Partienliste

Ausschließlich `Completed`-Partien, nach Startdatum absteigend (neueste zuerst), mit Filtern nach Spiel und Gruppe. Ein sinnvoller vorhandener Spielkontext darf vorausgewählt sein; sonst alle Spiele. Die Liste löst Spieler- und Gruppennamen immer aktuell auf; keine Namens-Snapshots.

Kompakte Karten: erste Zeile Spiel, Dauer und Datum; zweite Zeile Gruppe; dritte Zeile gespeicherte Platzierungen und Spielernamen. Tatsächliche Gleichstände behalten identische Plätze, z. B. `1. Anna · 1. Ben · 3. Chris`.

### Historische Wertungsübersicht

Ein Tap öffnet die spielabhängige Wertungsübersicht, für Arche Nova die vorhandene Wertungsoberfläche. Diese ist Ergebnis-, Detail- und Korrekturansicht. Spieler werden nach gespeichertem `Placement` aufsteigend sortiert, bei gleichem Platz nach Startreihenfolge; Punktzahl ersetzt die gespeicherte Platzierung nicht.

Bearbeitbar sind ausschließlich Attraktivität, Artenschutz, Ruf und besondere Karten. Spiel, Gruppe, Teilnehmer, Farben, Zoopläne, Startreihenfolge und Start-/Endzeit bleiben unverändert.

Korrekturen erfolgen als expliziter Completed-Anwendungsfall in einem nicht persistenten Entwurf. Erst `Korrektur speichern` übernimmt Rohwerte, Karten und finale Platzierungen atomar. Punkte werden neu berechnet, Platzierungen neu bestimmt und Gleichstände mit der bestehenden Gleichstandsauflösung bearbeitet. Die Partie bleibt durchgehend `Completed`; nach Speichern wird nach aktualisiertem Placement sortiert. Ungespeicherte Entwürfe werden beim Verlassen verworfen. Der normale Scoring-Use-Case bleibt auf `Scoring` beschränkt.

`Partie löschen` ist eine sekundäre destruktive Aktion. Zwingende Sicherheitsabfrage: „Partie löschen?“ / „Möchtest du diese Partie wirklich löschen? Die gespeicherte Wertung und alle zugehörigen Daten werden dauerhaft entfernt.“ / „Abbrechen · Partie löschen“. Bestätigung löscht GameSession und alle zugehörigen spielspezifischen Daten vollständig; Spieler und Gruppe bleiben erhalten.

## Statistik – geplanter Slice

Je regulärer Gruppe:

- Anzahl Partien
- Siege pro Spieler
- Gewinnquote
- durchschnittliche Platzierung

Spätere Auswertungen können nach Spiel, Gruppe und Spieler gefiltert
werden.

## UI-Zielgerät

Primärer Prototyp:

- Smartphone
- 6,1 Zoll
- Hochformat
- Referenzauflösung: **1170 × 2532 px**

Große Touch-Flächen und einhändige Bedienung haben Vorrang vor
Informationsdichte.

------------------------------------------------------------------------

## Gruppenarchivierung

Gruppen besitzen `IsArchived`. Archivierung löscht keine Gruppe und
keine Historie.

Die normale Gruppenübersicht zeigt standardmäßig weder temporäre noch
archivierte Gruppen. Beide Kategorien können unabhängig per Schalter
eingeblendet werden.

Eine temporäre Gruppe kann im Bearbeitungsmodus permanent gemacht
werden. Eine permanente Gruppe kann nicht nachträglich temporär gemacht
werden.

Archivierte Gruppen können nach dem Einblenden erneut bearbeitet und
wieder aktiviert werden.

---

## Partievorbereitung Arche Nova

Der erste Vorbereitungsscreen ist implementiert. Eine Gruppe öffnet direkt die Arche-Nova-Vorbereitung.

Die Vorbereitung übernimmt Spieler und Default-Reihenfolge der Gruppe sowie, falls vorhanden, spielspezifische Defaultfarben. Ohne gespeicherte Farben werden eindeutige Farben vorgeschlagen.

Vor dem Start können Reihenfolge und Farben geändert werden. `Startspieler auslosen` wählt zufällig einen Spieler und rotiert die bestehende Reihenfolge zyklisch auf diesen Spieler als Position 1.

`Zoopläne auslosen` verteilt pro Spieler zwei Pläne aus dem Katalog der 14 Pläne. Kein Plan wird innerhalb derselben Vorbereitung zweimal angeboten. Pro Spieler muss genau einer der beiden Pläne gewählt werden.

Die Vorbereitung ist bis zum eigentlichen Start UI-Zustand. Beim Start werden `GameSession`, tatsächliche Farben, Reihenfolge sowie angebotene/gewählte Zoopläne atomar persistiert.

## Manuell erfasste Spiele – Zielbild

Neben speziell unterstützten Spielen wie Arche Nova sollen Benutzer später eigene Spiele anlegen können. Für solche manuellen Spiele soll konfigurierbar sein, welche Ergebnisdaten erfasst werden: nur dass eine Partie gespielt wurde, Gewinner, Punkte und/oder Platzierungen. `Game` ist deshalb eine eigenständig identifizierte, benutzererweiterbare Entität; der Anzeigename ist nicht ihre Identität. Die genaue Konfiguration und der Eingabeflow werden erst bei Umsetzung dieses Features spezifiziert.



## Navigationseinstellungen

Das `Standardspiel` ist eine lokale App-Einstellung und keine historische
Spieldatei. Es wird zusammen mit dem zuletzt verwendeten Spiel-/Navigationskontext
über den nativen MAUI-Preferences-Speicher gehalten, nicht in SQLite. Gruppen
bleiben fachlich spielunabhängig; nur ihre Darstellung und Partiemetadaten werden
im aktuell ausgewählten Spielkontext betrachtet.

## App-Darstellung

Allgemeine Einstellungen bieten Deep Forest (Standard), Königsblau und Dunkel. Die Auswahl wird lokal in Preferences gespeichert und sofort angewendet. Sie ist spielunabhängig, verändert keine historischen Daten und folgt der verbindlichen Palette in `UI_DESIGN_GUIDE.md`. Historie zeigt Monatsgruppen ohne notwendige Sticky-Überschriften und kompakte Spiel-/Gruppenfilter.
