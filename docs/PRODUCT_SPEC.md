# Boardgame Tracker – Produktspezifikation

## Ziel

Mobile App zur Erfassung und Auswertung von Brettspielpartien.

Die Grundlage soll mehrere Spiele unterstützen. Spieler, Gruppen,
Partien und Historie sind spielübergreifend; Wertung, Eingabefelder,
Bildauswertung und detaillierte Statistiken sind spielspezifisch.

Erstes vollständig unterstütztes Spiel: **Arche Nova**.

## Spielübergreifende Konzepte

### Player

Ein Spieler besitzt einen Namen, einen Gaststatus (`IsGuest`) und einen
Archivstatus (`IsArchived`). Das technische Modell steht in `DATA_MODEL.md`.

Ein Gast ist ein normaler Spieler mit `IsGuest = true`. Wird der Spieler
später regulär gepflegt, wird lediglich `IsGuest = false` gesetzt.
Historische Partien bleiben dadurch automatisch erhalten.

Unter allgemeinen Einstellungen → `Spieler` können Namen und Archivstatus
bestehender Spieler bearbeitet werden. Standardmäßig erscheinen nur aktive
Nicht-Gäste. `Gäste anzeigen` und `Archivierte anzeigen` sind unabhängig kombinierbar
und bleiben Screen-State. Gaststatus ist in diesem Editor nicht veränderbar.
Archivieren ändert ausschließlich den Archivstatus; Gruppenmitgliedschaften,
Partien und Auswertungen bleiben erhalten. Historische Namen werden weiterhin
aus der aktuellen Player-Entität gelesen.

Spielerkarten zeigen Name, Edit-Icon, echten Anlagezeitpunkt und einen eigenen
`Zuletzt gespielt`-Button. Maßgeblich ist wie bei Gruppen die Partie mit dem
neuesten Startzeitpunkt, einschließlich laufender Partien. Die Navigation öffnet
je nach Status laufende Partie oder Wertung über denselben Spiel-Handler wie die
Historie. Ohne Partie erscheint ein nicht klickbarer Leerzustand. Gast-/Archiv-Chips
erscheinen nur bei zutreffendem Status; ausschließlich das Edit-Icon öffnet den Editor.

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

## App-Sprache und Spielmaterial-Sprache

App-Texte (Navigation, Buttons, Hilfen, Wertungsbegriffe und Accessibility) folgen
weiterhin der App-Sprache über AppResources/.resx. Davon unabhängig besitzt jedes
Spiel eine lokale Spielmaterial-Sprachpräferenz. Ohne gespeicherte Auswahl wird die
App-Sprache gewählt, sofern vom Spiel unterstützt, sonst dessen Default. Diese
Auswahl wird anschließend gespeichert; ein späterer App-Sprachwechsel setzt sie nicht zurück.

Arche Nova deklariert derzeit ausschließlich Deutsch (de), zugleich Default, da nur
diese Materialnamen im Projekt belegt sind. Es werden keine Übersetzungen oder
sprachabhängigen Assets erfunden. Der Selektor in den Spieleinstellungen zeigt
nur unterstützte Sprachen; „Nutzer“ ist eine Content-Quelle, keine Sprache.

Spielmaterialnamen werden in genau dieser Reihenfolge aufgelöst: Nutzerressource →
gewählte Materialsprache → unterstützte aktuelle App-Sprache → Spiel-Default →
deterministischer Missing-Content-Hinweis. Nutzerressourcen gewinnen unabhängig
von der Sprache. Die Materialsprache ändert weder CurrentUICulture noch Karten,
Symbole, Partien oder Ergebnisse. Sie wird nicht mit Partien exportiert.

Mitgelieferte Spezialkartennamen und Zooplannamen liegen im versionierten
Ark-Nova-Content. Karten-GUID und Zooplannummer bleiben ihre Identität. Bestehende
sprachneutrale Zooplanskizzen bleiben unverändert. Historische Ansichten verwenden
aktuelle Content-Auflösung und aktuellen Karten-Fachzustand; keine Namens- oder
Sprachsnapshots in Partien.

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

### Archivierung

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

## Gruppe anlegen

Die Maske priorisiert die Wiederverwendung vorhandener Spieler vor dem
Anlegen neuer Spieler.

### Kopfzeile

- links: Zurück-Pfeil
- Titel: **Gruppe anlegen**
- direkt beim Titel: Informations-Icon
- rechts: gut sichtbare Primary-Aktion **Speichern**
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

Ein einzelner Secondary-Button öffnet ein einheitlich gestaltetes App-Auswahlfenster. Ein Tap auf einen verfügbaren Spieler fügt ihn unmittelbar hinzu und schließt die Auswahl; keine weitere Bestätigung. Bereits enthaltene Spieler werden nicht angeboten.

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

Bearbeitbar sind Gruppenname, Default-Reihenfolge, Archivstatus und die Farbdefaults des aktuellen Spiels, sofern dieses Farben unterstützt. Temporäre Gruppen können permanent gemacht werden; die umgekehrte Richtung ist ausgeschlossen.

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

Farben sind gemeinsame semantische Kategorien wie Rot oder Blau. Ein Spiel legt fest, ob Spielerfarben existieren und welche Kategorien erlaubt sind. Arche Nova erlaubt Blau, Gelb, Rot und Schwarz. Andere definierte Farben werden dadurch nicht automatisch für dieses Spiel zugelassen.

In Gruppe anlegen und Gruppe bearbeiten kann je Mitglied eine optionale Defaultfarbe für das aktuelle Spiel festgelegt werden. Ohne Farbunterstützung erscheint keine Farbauswahl. Der Palettenbutton öffnet einen gemeinsamen Dialog mit explizitem Spielbezug („Farbe für Arche Nova festlegen“), Spielername und erlaubten Farben. Eine belegte Farbe tauscht die bisherigen Zuweisungen; „Keine Farbe“ entfernt den Default. Die Auswahl bleibt bis Speichern im Entwurf. Gruppe, neue Spieler und Spieldefaults werden atomar gespeichert. Die Defaults eines anderen Spiels bleiben unberührt. Partievorbereitung übernimmt die gespeicherten Defaults und ergänzt fehlende Farben mit eindeutigen Vorschlägen. Änderungen dort gelten nur für diese Partie.

## Arche Nova – Zoopläne

Arche Nova unterstützt 14 nummerierte Zoopläne; der verbindliche Katalog steht
in `DATA_MODEL.md`, Abschnitt „Arche-Nova-Zoopläne“. Jeder Spieler erhält zwei
verschiedene Optionen und wählt genau eine. Ein Plan darf innerhalb einer
Vorbereitung insgesamt nur einmal angeboten werden. Die Auswahl bleibt bis
zum Start ungespeichert. Der sichtbare Aufbau steht in `UI_WIREFRAME.md`.

## Startreihenfolge

Die Spieler werden in Sitz-/Spielreihenfolge als Kacheln angezeigt.

Ausgangspunkt ist die Default-Reihenfolge aus der Gruppenverwaltung. Gibt es
für diese Gruppe und dieses Spiel eine abgeschlossene letzte Partie, wird deren
letztplatzierter Spieler durch zyklische Rotation Startspieler. Bei geteiltem
letzten Platz entscheidet der alphabetisch erste Name. Ohne Historie bleibt
die Gruppenreihenfolge erhalten. Manuelle Rotation und Auslosung ändern nur
diese Vorbereitung; die relative Reihenfolge bleibt erhalten.

### Reihenfolge ändern

„Reihenfolge rotieren“ verschiebt den ersten Spieler nach hinten und erhält
die relative Reihenfolge. Zusätzlich verschieben Hoch-/Runter-Pfeile einzelne
Spieler sofort um eine Position. An den Listenenden ist die jeweilige Richtung
deaktiviert. Diese Änderungen gelten nur für die konkrete Partie und überschreiben
nicht die Default-Reihenfolge der Gruppe.

### Startspieler auslosen

Ein kleiner Icon-Button bestimmt zufällig den Startspieler.

Verhalten:

1. kurze dezente Animation des Würfelbuttons,
2. zufälliger Startindex der bestehenden Reihenfolge,
3. zyklische Rotation, sodass der ausgeloste Spieler auf Position 1 landet,
4. unveränderte relative Reihenfolge der übrigen Spieler.

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

War diese offene Wertung der zuletzt gespeicherte Navigationskontext, wird
sie beim nächsten Start gemäß „App-Start und Navigationskontext“ wieder geöffnet.

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

Attraktivität und Artenschutz werden auf einem kompakten Eingabescreen über
begrenzte Zahlenräder erfasst; eine Zahl bildet jeweils den gesamten Wert ab.
Vorhandene Werte sind vorausgewählt. Bei leeren Feldern zeigt das Rad einen
Vorschlag in der Bereichsmitte (Attraktivität 57, Artenschutz 21), ohne ihn bereits
zu erfassen oder zu speichern. Wischen erfasst den gewählten Wert; die Zahl
unter dem Rad kann alternativ mit der numerischen Tastatur eingegeben werden.
„Fertig“ übernimmt auch noch offene Vorschläge für Attraktivität und Artenschutz.
Zurück übernimmt keine Vorschläge. Ruf erscheint ausschließlich gemäß der
Arche-Nova-Einstellung „Ruf erfassen“ als horizontaler Regler unter den Punkten;
einen zusätzlichen Erfassungsschalter gibt es hier nicht. Der Regler zeigt bei
leerem Ruf zunächst 8, ohne den Wert zu speichern. Verschieben erfasst ganze
Werte von 1 bis 15; das zugehörige Zahlenfeld erlaubt Eingabe und Leeren.
Ruf bleibt optional. Bedienhinweise stehen hinter dem Info-Symbol im Seitenkopf.
Die Punktevorschau bezieht angezeigte Vorschläge ein und ist bis zur Erfassung entsprechend benannt.
Die Räder enden an ihren Wertebereichsgrenzen, ohne Umlauf.
Gültige Änderungen werden automatisch zwischengespeichert. Es gibt keinen zusätzlichen „Spieler fertig“-Status.
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

Die Erkennungsforschung ist **ABGESCHLOSSEN / FROZEN**. Maßgeblich sind
[CV-Status und Integrationsstand](ARK_NOVA_CV_STATUS.md) sowie der dort verlinkte
Abschlussreport. Gehärtetes A bleibt Basis; C1/C2 verworfen, C3 nur Forschung,
keine produktive Fusion. V2/V3 werden nicht weiter optimiert.

Über Spielerfarben sollen Attraktivität, Artenschutz und Ruf in exakt dieselben
Wertungsfelder wie bei manueller Eingabe vorausgefüllt werden. Kein separater
fachlicher Kamera-Wertungspfad. Unterstützte Wertungstafeln sollen perspektivisch
in den Arche-Nova-Einstellungen auswählbar sein, z. B. Original oder Custom kompakt.

Verbindlicher Zielablauf: Foto aufnehmen **oder** vorhandenes Foto auswählen →
gemeinsame Orientierung/Analyse → Werte vorausfüllen → Grün/Gelb/Rot mit Text/Icon
→ Benutzer prüft/korrigiert → bestätigt → erst danach speichern. Auch Grün ist
keine Korrektheitsgarantie. Unbestätigte Fotovorschläge sind vom manuellen Autosave
ausgenommen. Fehler erhalten bestehende Werte; Systempicker-Abbruch ist kein Fehler.

Native Systempicker verwenden, wenn möglich eingebettet, andernfalls normale
Systemauswahl. Keine eigene Galerie und keine unnötige Vollbibliotheksberechtigung.
Info-Icon im Wertungsheader erklärt Sicher/Prüfen/Unsicher und Bestätigungspflicht
im vorhandenen App-Dialog. Ruf bleibt gemäß globaler Einstellung optional.
CV liefert für Attraktivität/Artenschutz 0 weiterhin keinen Kandidaten; manuelle
0-Eingabe bleibt gültig. Keine automatischen Nachbarwertkorrekturen.

**Noch keine mobile Produktintegration:** Auf ausdrückliche Folgeanweisung geht
zuerst der gemeinsame native OpenCV-C++-Spike vor. Android ARM64 baut, Android-x64-
Interop läuft, numerische Parität zur Windows-Referenz ist aber nicht nachgewiesen.
iOS ist vorerst nur dokumentarisch geprüft. Daher bleiben Kamera-/Picker-/Review-
Anbindung und deren UI-/Persistenztests offen. Der bestehende Kameraeinstieg ist
weiterhin ein Platzhalter; der gewünschte Produktablauf wird nicht als fertig
implementiert ausgegeben.

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

Spezialkarten sind global für Arche Nova benutzerverwaltbare Referenzdaten.
Unter den Arche-Nova-Einstellungen öffnet „Spezialkarten“ eine eigene Liste:
standardmäßig aktive Karten, optional „Archivierte anzeigen“. Nutzer können
Karten anlegen, bearbeiten, archivieren und wieder entarchivieren; keine normale
Löschaktion. Create und Edit verwenden denselben Editor. Die Liste zeigt das Bearbeiten-Icon direkt neben der Kartenansicht und keine Archivierungsaktion. Der Switch „Archiviert“ ist nur beim Bearbeiten vorhanden; Speichern übernimmt den gewählten Status über den bestehenden Archivierungsanwendungsfall, Verlassen ohne Speichern verwirft die Statusänderung. Das Info-Icon im Listenkopf erklärt die Zuordnung zu Spielern und Partien sowie die spätere Auswertung.

Eine Karte besitzt eine stabile GUID und null bis drei geordnete Symbole aus dem
festen Katalog. Ihr sichtbarer Name wird über Game Content aufgelöst; eingegebene
Nutzernamen haben 1–120 Zeichen nach Trim. Die Anzeige lautet immer Symbol1 → Symbol2 →
Symbol3 → Name. Wiederholungen sind ausdrücklich erlaubt, z. B. Africa · Africa
· Herbivore. Das Leeren einer Position rückt nachfolgende Symbole nach links;
neue Positionen werden lückenlos angeboten. Die Auswahl zeigt echte Symbolgrafiken.

Feste Symboltypen: Africa, America, Asia, Australia, Europe, Bear, Bird,
Herbivore, PettingAnimal, Predator, Primate, Reptile, SeaAnimal und Science. Nutzer legen
keine neuen Symboltypen an. Fünf aktive Startkarten sind Afrikanischer Elefant,
Asiatischer Elefant, Weißkopfseeadler, Steinadler und Malaienbär; die anfänglichen
Symbolkombinationen und stabilen IDs stehen in `DATA_MODEL.md`. Auch diese Karten
sind danach frei editierbar und archivierbar; Initialisierung setzt sie nicht zurück.

Neue Karten erhalten eine neue GUID und einen persistenten Nutzernamen. Der Editor
zeigt den aufgelösten Namen; eine Namensänderung speichert eine Nutzerressource für
dieselbe GUID. Reine Symboländerungen erzeugen keine Nutzerressource und verändern
keine ID. Archivstatus bleibt Kartenzustand. Die mitgelieferte Content-Datei wird
durch Bearbeiten nicht geändert. Es gibt keinen vollständigen Kartenreset.

Die Wertungsauswahl lädt persistierte Karten und zeigt ihre Symbole vor dem
aktuell aufgelösten Namen. Der Zuordnungsstatus wird ohne sichtbare Statuszeile dargestellt:
SelectedSurface kennzeichnet die Zuordnung zum Zielspieler, ein gestrichelter
Theme-Rahmen in Danger die Zuordnung zu einem anderen Spieler. Ist die Karte
beiden zugeordnet, gelten beide Markierungen. Für Screenreader
bleibt der Status beschrieben. Der Zurück-Pfeil verlässt nur die Auswahl.
Neue Zuordnungen sind ausschließlich für aktive
Karten erlaubt. Dieselbe Karte kann mehreren Spielern derselben Partie gehören;
die bestehende Sicherheitsabfrage für Mehrfachzuordnungen bleibt erhalten.
Doppelte Zuordnung an denselben Spieler ist ausgeschlossen.

Historische Partien lösen Kartenname und Symbole immer aktuell über die Karten-ID
auf. Archivieren verändert keine Partiedaten. Bereits zugeordnete archivierte
Karten bleiben sichtbar und dürfen entfernt werden; auch bei historischen
Korrekturen dürfen sie nicht neu hinzugefügt werden. Beim Speichern wird dies
gegen den aktuellen Datenbankzustand geprüft. Es gibt keine Namens-/Symbol-Snapshots.
Zukünftige Statistikfilter bieten ausschließlich aktive Karten an; Statistik
selbst bleibt ein späterer Slice.

## Gleichstand

Bei Punktgleichstand öffnet `Wertung abschließen` den Screen
`Gleichstände bearbeiten`. Jede Gruppe mit gleicher fachlicher Punktzahl bildet
einen unabhängigen Gleichstandsblock. Sein Platzbereich beginnt hinter allen
besser bewerteten Spielern und enthält genau einen aufeinanderfolgenden Platz
je beteiligtem Spieler. Mehrere Blöcke dürfen sich nicht überschneiden.

Platzgruppen sind die primäre Struktur: Jeder Platz von `1..n` erhält eine neutrale
Überschrift mit Divider. Darunter stehen die aktuell zugeordneten Spieler. Auch
leere mögliche Zielgruppen bleiben sichtbar. Ein Klick auf einen Platz-Chip
verschiebt ausschließlich diesen Spieler sofort in die entsprechende Gruppe.
Mehrere Spieler dürfen dieselbe Gruppe belegen; es gibt keine exklusive
Platzreservierung, keinen Tausch und keine automatische Neuverteilung.

Jede Spielerzeile enthält Name, kompakte Platz-Chips und rechtsbündige Punkte
vertikal mittig in einer einzigen Reihe. Der Rang wird nur in der Gruppenüberschrift
angezeigt. Nur Teilnehmer eines Gleichstandsblocks erhalten dessen Platz-Chips.
Die Spielerzeile verwendet ihre bestehende Spielerfarbe mit zugehörigem Textkontrast;
Gruppen und Chips bleiben neutral bzw. verwenden Surface-/Selected-Ressourcen.
Ohne Spielerfarbe gilt die neutrale Darstellung. Erklärungen stehen hinter dem Info-Icon.

Geteilte Plätze bleiben beim Öffnen erhalten und sind innerhalb des eigenen
Tie-Blocks beliebig speicherbar, auch `1,3,3,4`. Ranglücken werden nicht automatisch
geschlossen. Die automatische Ausgangsplatzierung folgt weiterhin Competition
Ranking. Nicht punktgleiche Spieler und die Grenzen anderer Blöcke bleiben unverändert.
Pfeile und Drag & Drop werden hier nicht verwendet.

Beispiel: Bei Punkten `120,120,110,100,100,90` erhält der erste Block ausschließlich
Plätze `1,2`, der zweite ausschließlich `4,5`. Die übrigen Spieler bleiben auf
`3` und `6`. Die Score-Berechnung wird durch die Platzwahl nicht verändert.
Bedienhinweise stehen ausschließlich hinter dem Info-Icon im Kopf.

Nach der Auflösung werden die finalen `Placement`-Werte gespeichert und die
`GameSession` auf `Completed` gesetzt. Dieselben Platzierungen erscheinen beim
erneuten Öffnen und in der Historie. Der Ergebnis-Screen bleibt sichtbar, bis
der Nutzer explizit `Fertig` wählt. Die Anzahl unterstützter Artenschutzprojekte
wird nicht gespeichert.

## Historie

Der Hauptbereich Historie bündelt Partien und künftig Statistik. Statistik ist ein eigener nachfolgender Slice und darf eigene umfangreichere Filter nach Spiel, Gruppe, Spieler, Zooplan, besonderen Karten oder Zeitraum erhalten. Ihre Filter sind unabhängig von der Partienliste.

### Partienliste

Ausschließlich `Completed`-Partien, nach Startdatum absteigend (neueste zuerst), mit Filtern nach Spiel und Gruppe. Ein sinnvoller vorhandener Spielkontext darf vorausgewählt sein; sonst alle Spiele. Die Liste löst Spieler- und Gruppennamen immer aktuell auf; keine Namens-Snapshots.

Kompakte Karten: erste Zeile Spiel, Dauer und Datum; zweite Zeile Gruppe; dritte Zeile gespeicherte Platzierungen und Spielernamen. Tatsächliche Gleichstände behalten identische Plätze, z. B. `1. Anna · 1. Ben · 3. Chris`.

### Historische Wertungsübersicht

Ein Tap öffnet die spielabhängige Wertungsübersicht, für Arche Nova die vorhandene Wertungsoberfläche. Diese ist Ergebnis-, Detail- und Korrekturansicht. Spieler werden nach gespeichertem `Placement` aufsteigend sortiert, bei gleichem Platz nach Startreihenfolge; Punktzahl ersetzt die gespeicherte Platzierung nicht.

Bearbeitbar sind ausschließlich Attraktivität, Artenschutz, Ruf und besondere Karten. Spiel, Gruppe, Teilnehmer, Farben, Zoopläne, Startreihenfolge und Start-/Endzeit bleiben unverändert.

Bleiben die berechneten Punkte aller Spieler unverändert, werden gültige gespeicherte Placements beibehalten. Andernfalls werden Platzierungen neu bestimmt und nötige Gleichstände aufgelöst.

Korrekturen erfolgen als expliziter Completed-Anwendungsfall in einem nicht persistenten Entwurf. Erst `Korrektur speichern` übernimmt Rohwerte, Karten und finale Platzierungen atomar. Punkte werden neu berechnet, Platzierungen neu bestimmt und Gleichstände mit der bestehenden Gleichstandsauflösung bearbeitet. Die Partie bleibt durchgehend `Completed`; nach Speichern wird nach aktualisiertem Placement sortiert. Ungespeicherte Entwürfe werden beim Verlassen verworfen. Der normale Scoring-Use-Case bleibt auf `Scoring` beschränkt.

`Partie löschen` ist eine sekundäre destruktive Aktion. Zwingende Sicherheitsabfrage: „Partie löschen?“ / „Möchtest du diese Partie wirklich löschen? Die gespeicherte Wertung und alle zugehörigen Daten werden dauerhaft entfernt.“ / „Abbrechen · Partie löschen“. Bestätigung löscht GameSession und alle zugehörigen spielspezifischen Daten vollständig; Spieler und Gruppe bleiben erhalten.

## Statistik – geplanter Slice

Je regulärer Gruppe:

- Anzahl Partien
- Siege pro Spieler
- Gewinnquote
- durchschnittliche Platzierung

Die Filter sind unabhängig von der Partienliste und dürfen Spiel, Gruppe,
Spieler, Zooplan, Spezialkarte oder Zeitraum berücksichtigen. Als
Spezialkarten-Filteroptionen werden ausschließlich aktive Karten angeboten.

## UI-Zielgerät

Zielgerät, Touch-Flächen und visuelle Regeln stehen verbindlich in
`UI_DESIGN_GUIDE.md`; Screenaufbau und Navigation in `UI_WIREFRAME.md`.

------------------------------------------------------------------------

## Manuell erfasste Spiele – Zielbild

Neben speziell unterstützten Spielen wie Arche Nova sollen Benutzer später eigene Spiele anlegen können. Für solche manuellen Spiele soll konfigurierbar sein, welche Ergebnisdaten erfasst werden: nur dass eine Partie gespielt wurde, Gewinner, Punkte und/oder Platzierungen. `Game` ist deshalb eine eigenständig identifizierte, benutzererweiterbare Entität; der Anzeigename ist nicht ihre Identität. Die genaue Konfiguration und der Eingabeflow werden erst bei Umsetzung dieses Features spezifiziert.

## App-Darstellung

Allgemeine Einstellungen bieten Deep Forest (Standard), Königsblau und Dunkel. Die Auswahl wird lokal in Preferences gespeichert und sofort angewendet. Sie ist spielunabhängig, verändert keine historischen Daten und folgt der verbindlichen Palette in `UI_DESIGN_GUIDE.md`. Historie zeigt Monatsgruppen ohne notwendige Sticky-Überschriften und kompakte Spiel-/Gruppenfilter.

## Partien importieren und exportieren

Am unteren Rand der Historie stehen dauerhaft „Importieren“ (Secondary) und
„N Partien exportieren“ (Primary). Export verwendet exakt die aktuell gefilterten
abgeschlossenen Partien und deren notwendige Abhängigkeiten. Ohne Ergebnis ist
Export deaktiviert. Nach der Exportaktion wird der vollständige validierte .bgt-Inhalt
für genau den aktuellen Filterstand einmal erzeugt und serialisiert. Erst danach öffnet
sich der App-Dialog „Exportieren“ mit „Speichern“, „Teilen“ und „Abbrechen“.
„Speichern“ öffnet einen nativen Dokument-Picker mit Speicherort- und Dateinamenwahl;
der vorgeschlagene Name endet auf .bgt. „Teilen“ öffnet ausschließlich den nativen
Share-Sheet. Beide Ziele verwenden denselben zuvor erzeugten Inhalt. Abbruch im Dialog
oder nativen Picker/Share-Sheet ist kein Fehler und führt zu keiner weiteren Aktion.
Import öffnet die native Dateiauswahl unabhängig von History-Filtern.

Nach Analyse werden ausschließlich offene Gruppenzuordnungen aufgelöst. Sichere
Spielerzuordnungen sind fest; manuelle Zuordnungen gelten global im Import. Nach jeder
Entscheidung werden alle offenen Gruppen neu bewertet. Alternativ werden Gruppen
getrennt und erhalten deterministisch den ersten freien „Name Import [N]“-Namen.
Die Vorschau zeigt neue/vorhandene Spiele, automatisch/manuell zugeordnete/neue Spieler,
zusammengeführte/neue Gruppen, Partien zum Import/Überspringen und Referenzdaten.
Erst „Jetzt importieren“ schreibt sämtliche Daten atomar. Abbrechen schreibt nichts.
Lokaler Zustand gewinnt bei allen Zuordnungen; vorhandene Partie-IDs werden ohne
Inhaltsvergleich übersprungen. Änderungen während des Importflows erfordern erneute
Analyse. Fehler übernehmen keine Teildaten. Erfolgreicher Import lädt die Historie neu.
Unterstützt ist aktuell Arche Nova; Custom Games sind ein späteres Feature.
