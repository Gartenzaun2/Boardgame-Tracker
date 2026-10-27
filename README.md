# Boardgame Tracker – v0.8.2.0

v0.8.2.0 führt einen expliziten Spiel- und Navigationskontext ein. Gruppen werden immer im Kontext eines ausgewählten Spiels angezeigt; Standardspiel, letzter Hauptbereich und die zuletzt betrachtete offene Session werden lokal gespeichert. Das Datenbankschema bleibt unverändert.

## Aktueller Funktionsstand

- Gruppen können angelegt, bearbeitet, permanent gemacht und archiviert werden.
- Arche-Nova-Defaultfarben werden pro Gruppe und Person gepflegt.
- Die Partievorbereitung bleibt bis `Starten` nicht persistent.
- Zoopläne werden sessionweit eindeutig ausgelost; jeder Spieler wählt genau
  einen der beiden angebotenen Pläne.
- Der Verlierer der letzten abgeschlossenen Partie startet die nächste Partie;
  bei gemeinsamem letzten Platz entscheidet der Name alphabetisch.
- Beim Start werden allgemeine Sessiondaten und Arche-Nova-Snapshots atomar
  persistiert.
- Laufende Partien können beendet oder nach Bestätigung verworfen werden.
- Eine offene Partie bzw. Wertung wird beim App-Start wieder aufgenommen.
- Wertungsrohwerte werden während `Scoring` automatisch zwischengespeichert;
  erst `Completed` zählt für Historie und Statistik.
- Attraktivität und Artenschutz werden validiert und über das feste
  Artenschutz-Lookup in alte/neue Punkteanzeige umgerechnet.
- Die fünf Spezialkarten können Spielern zugeordnet und wieder entfernt werden;
  seltene Mehrfachzuordnungen derselben Karte werden explizit bestätigt.
- Punktgleichstände können innerhalb ihres ursprünglichen Rangbereichs per
  Drag-and-drop beibehalten oder aufgelöst werden.
- Ruf-Erfassung und alte/neue Punkteanzeige sind globale lokale
  Arche-Nova-Präferenzen.
- Der Kamera-Button ist im Wertungsscreen bereits an seinem vorgesehenen Platz;
  die eigentliche Fotoerkennung folgt als eigener Slice.
- Die Shell besitzt eine permanente untere Hauptnavigation für Gruppen, Spiele
  und Einstellungen; die Arche-Nova-Einstellungen sind dort erreichbar.
- Gruppen mit laufender Partie oder offener Wertung zeigen ein Aktiv-Icon neben
  dem Gruppennamen.
- Die Wertung kann nach Sicherheitsabfrage verworfen werden.
- Spielerkacheln sind wieder zuverlässig antippbar; Spezialkarten werden über
  eine gut lesbare Auswahl je Spieler zugeordnet.
- Spezialkartennamen und -symbole liegen in den Localization-Ressourcen.
- Die Spielerkachel zeigt Spielerfarbe und lokalisierten Zooplannamen statt der
  bloßen Zooplan-Nummer.

- Die untere Hauptnavigation lautet jetzt `Spiele · Gruppen · Historie · Einstellungen`;
  Gruppen bleibt trotz der sichtbaren Reihenfolge der Startbereich.
- Beim Wechsel auf den Gruppen-Tab wird der Gruppenbereich auf seine Übersicht
  zurückgesetzt, statt auf einem zuvor geöffneten Detail-Screen zu verbleiben.
- Doppeltaps auf eine Spielerkachel können den Wertungsdialog nicht mehr mehrfach
  öffnen; die Navigation nutzt denselben Busy-Schutz wie andere kritische Aktionen.
- Der Spezialkarten-Einstieg verwendet einen kompakten Arche-Nova-Kartenrücken
  statt des gelben Textbuttons und bleibt mit mindestens 44 dp gut antippbar.
- Spezialkarten werden in der Wertungsübersicht nur noch über einen Zähler am
  Karten-Button zusammengefasst; die konkreten Karten stehen im Auswahl-Flyout.
- Das Spezialkarten-Flyout schließt nach Zuordnung oder Entfernen direkt.
- Berechnete Punkte stehen nach vollständiger Eingabe direkt hinter dem Spielernamen.
- Der Ergebnis-Screen zeigt zusätzlich die aus Start- und Endzeit berechnete Dauer.
- Der Historie-Tab ist als Hauptnavigationsziel vorbereitet; die eigentliche
  Historienliste bleibt ein späterer Feature-Slice.

## Technischer Review v0.8.1

Der Review hat die bestehenden Architekturgrenzen beibehalten und mehrere
konkrete Qualitätsprobleme bereinigt:

- kein `LangVersion=latest`; `net10.0` bestimmt die unterstützte C#-Version,
- C#-Collection-Expressions dort, wo der Zieltyp eindeutig und der Code klarer
  wird,
- `StartAsync` erzeugt seinen Setup-Input explizit als
  `IReadOnlyList<ArkNovaSessionSetupPlayer>`,
- Shell-Navigation verwendet `IQueryAttributable` statt `QueryProperty`,
- einmalige Navigationsdaten verwenden `ShellNavigationQueryParameters`,
- XAML-Bindings deklarieren `x:DataType` auf Seiten- und Template-Ebene, damit
  normale Binding-Pfade kompiliert und beim Build typgeprüft werden können,
- der Running-Session-Persistenzport ist zustandslos und hält keinen
  `DbContext` zwischen Use-Case-Aufrufen offen,
- wiederverwendete Ark-Nova-Farbdarstellung liegt an einer gemeinsamen
  App-Präsentationsstelle,
- sichtbare UI-Texte des neuen Session-Flows liegen in den Localization-
  Ressourcen,
- bedingte Fehlerlabels reservieren ohne Fehler keinen Platz,
- interaktive Farbfelder und kompakte Aktionen erfüllen mindestens 44 dp,
- öffentliche Projekt-Typen besitzen kurze XML-Summaries, die insbesondere bei
  Ports, Commands und Read Models ihre Rolle erklären,
- die Testprojekte verwenden xUnit v3 mit Microsoft Testing Platform.

## Architektur

Die Projektgrenzen bleiben unverändert:

```text
App -> Application -> Core
          ^
          |
Infrastructure -> Core
```

`App` darf `Infrastructure` ausschließlich im Composition Root bzw. beim
technischen Startup verwenden. Feature-ViewModels kennen keine konkrete
SQLite-/EF-Core-Implementierung.

Es werden weiterhin keine generischen Repositories, MediatR-Handler,
Navigation-Service-Abstraktionen oder `Common`-/`Utils`-Sammelbereiche
verwendet.

## Datenbank in der aktuellen Entwicklungsphase

Solange noch keine produktiven Nutzerdaten erhalten werden müssen, verwendet der
Runtime-Start bewusst `Database.EnsureCreatedAsync()`. Eine frische lokale
SQLite-Datenbank wird damit direkt aus dem aktuellen EF-Core-Modell erzeugt.

Anschließend stellt ein idempotentes Seeding sicher, dass das Spiel `ark-nova`
und alle 14 Ark-Nova-Zoopläne als verpflichtende Referenzdaten vorhanden sind.
Damit kann eine strukturell aktuelle Bestandsdatenbank mit fehlenden
Referenzdaten ebenfalls repariert werden.

Bei einer künftigen Schemaänderung wird die lokale App-Datenbank in dieser
Entwicklungsphase gelöscht und neu erzeugt. Erst sobald Datenbank-Upgrades
zwischen veröffentlichten Versionen erhalten werden müssen, wird wieder ein
kanonischer EF-Core-Migrationsverlauf eingeführt.

## Review-Grenze

In der Erstellungsumgebung ist kein .NET-SDK bzw. keine MAUI-Workload
installiert. XML/XAML, Abhängigkeiten und Quelltext wurden statisch geprüft;
der verbindliche Abschlusscheck bleibt deshalb ein `Rebuild Solution` sowie
das Ausführen beider Testprojekte in der lokalen Visual-Studio-Umgebung.
## v0.8.1.8 – Wertungsübersicht und Spezialkarten-Icon

- Der Spezialkarten-Einstieg verwendet wieder die ruhige monochrome Icon-Sprache
  der App statt des fotografischen Kartenrückens. Das Icon bleibt als große, gut
  erreichbare Touch-Fläche rechts über die Höhe der Spielerinformationen angeordnet.
- Unter Attraktivität und Artenschutz fasst eine zusätzliche Zeile Spezialkarten
  und – bei aktivierter Ruf-Erfassung – den Ruf zusammen, z. B.
  `Spezialkarten: 0 · Ruf: 4`.
- Ist `Ruf erfassen` deaktiviert, zeigt die Übersicht nur die Spezialkartenanzahl.


## v0.8.1.9 – UI-Cleanup und Orientierung

- Primäraktionen verwenden einen gemeinsamen deutlich sichtbaren Disabled-Zustand; insbesondere `Starten` bleibt bis zur vollständigen Zooplanauswahl deaktiviert und `Wertung abschließen` bis alle Pflichtwerte vorliegen.
- Die Wertungskacheln besitzen getrennte Zeilen für Rohwerte und `Spezialkarten · Ruf`; fehlende Pflichtwerte werden direkt als `–` dargestellt. Der Spezialkarten-Button zeigt keinen zusätzlichen Counter-Badge mehr.
- Die Wertungsübersicht zeigt das Partiedatum. Gruppen zeigen das aktuell unterstützte Spiel zur besseren Orientierung.
- Spiele- und Einstellungsübersicht verschieben längere Erklärungstexte gemäß UI-Regel hinter ein Info-Icon.


## v0.8.2.0 – Spielkontext und Navigation

- Gruppenübersichten besitzen immer einen aktuellen Spielkontext. Der Spielname
  steht direkt unter `Gruppen` und wird nicht auf jeder Karte wiederholt.
- Ein Tap auf `Arche Nova` im Spiele-Tab setzt diesen Kontext und öffnet Gruppen.
- In Einstellungen kann optional ein `Standardspiel` gewählt werden.
- Ohne gespeicherten Kontext startet die App ohne Standardspiel bei `Spiele`, mit
  Standardspiel bei `Gruppen` dieses Spiels.
- Danach werden der zuletzt verwendete Hauptbereich, das gewählte Spiel und – falls
  zuletzt eine noch offene Session aktiv war – genau diese Session lokal gemerkt.
  Dadurch bleibt das Verhalten auch bei mehreren gleichzeitig offenen Partien eindeutig.
- Gruppen bleiben im Domainmodell spielunabhängig; `Letzte Partie` und offene
  Session werden in der Gruppenübersicht nur für das aktuell gewählte Spiel ermittelt.
- Die Wertungsübersicht verwendet für Gruppe, Datum, Rohwerte sowie
  `Besondere Karten · Ruf` dieselbe gut lesbare Textfarbe.
- Die deutschen Wertungslabels lauten konsistent `Besondere Karten` und `Ruf`.
- Keine Datenbankschemaänderung; Navigations-/Standardspielzustand liegt in nativen
  MAUI Preferences.


## v0.8.2.1 – stabiler Spielkontext und Ergebnisfarben

- Jeder automatische Wechsel nach `Gruppen` verwendet jetzt einen expliziten Spielkontext.
  Der `SelectedGameKey` wird vor der Shell-Navigation gespeichert und nicht erst
  nach dem Öffnen der Gruppenansicht ergänzt.
- Defaultspiel-Start, Spielauswahl und Session-Wiederaufnahme verwenden denselben
  Navigationspfad `Gruppen für Spiel öffnen`. Dadurch kann die Gruppenübersicht
  nicht transient ohne Spielkontext erscheinen.
- Der Ergebnis-Screen verwendet für jede Spielerkachel wieder die dezente
  Arche-Nova-Spielerfarbtönung aus Vorbereitung, laufender Partie und Wertung.
- Keine Datenbankschemaänderung.
