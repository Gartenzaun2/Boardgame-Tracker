# Boardgame Tracker

## Aktueller Entwicklungsstand

Boardgame Tracker ist eine mobile .NET-MAUI-App zur Erfassung und Auswertung von
Brettspielpartien. Die Anwendung ist noch in der Pre-Alpha-Entwicklung.

Erstes vollständig unterstütztes Spiel ist **Arche Nova**. Die Architektur und
das allgemeine Datenmodell sind darauf ausgelegt, weitere speziell unterstützte
und später auch manuell konfigurierte Spiele aufzunehmen, ohne eine generische
Game Engine zu bauen.

Der aktuelle Code-Stand entspricht funktional **v0.8.2.1**. Die
Projektdatei `BoardgameTracker.App.csproj` trägt derzeit noch die veraltete
`ApplicationDisplayVersion` `0.8.1.6`; das wird beim nächsten ausgelieferten
Code-Update korrigiert. Die konsolidierte Dokumentation selbst erzeugt keine neue
App-Version.

## Hauptnavigation

Die permanente untere Navigation lautet:

```text
Spiele · Gruppen · Historie · Einstellungen
```

`Gruppen` ist immer an ein ausgewähltes Spiel gebunden. Eine
spielunabhängige Gruppenübersicht gibt es im normalen Bedienfluss nicht.

Die App speichert lokal:

- optionales Standardspiel,
- zuletzt ausgewähltes Spiel,
- zuletzt verwendeten Hauptbereich,
- zuletzt tatsächlich betrachtete offene Session.

Beim Start wird zuerst eine noch offene zuletzt betrachtete Session
wiederhergestellt. Andernfalls wird der letzte Hauptbereich wieder geöffnet.
Existiert noch kein Navigationskontext, startet die App mit Standardspiel bei
`Gruppen` für dieses Spiel und ohne Standardspiel bei `Spiele`.

## Aktuell implementierte Funktionen

### Personen und Gruppen

- Personen können wiederverwendet oder als Gäste innerhalb der Gruppenerstellung
  angelegt werden.
- Gruppen besitzen eine feste Personenzusammensetzung und Default-Reihenfolge.
- Gruppen können temporär oder permanent sein.
- Temporäre Gruppen können einmalig permanent gemacht werden.
- Gruppen können archiviert und reaktiviert werden.
- Arche-Nova-Defaultfarben werden pro Gruppe und Person gepflegt.
- Die Gruppenübersicht kann temporäre und archivierte Gruppen einblenden.
- Gruppen werden in einem ausgewählten Spielkontext angezeigt.
- Für den aktiven Spielkontext werden letzte Partie und offene Session
  berücksichtigt.

### Arche Nova – Partievorbereitung und laufende Partie

- Die Partievorbereitung ist bis `Starten` nicht persistent.
- Pro Spieler werden zwei sessionweit eindeutige Zoopläne ausgelost.
- Jeder Spieler muss genau einen angebotenen Zooplan auswählen.
- Farben dürfen während der Vorbereitung vorübergehend doppelt sein, blockieren
  aber den Start.
- Die Default-Reihenfolge stammt aus der Gruppe und kann für die konkrete Partie
  verändert werden.
- Der Verlierer der letzten abgeschlossenen Arche-Nova-Partie wird auf Position 1
  rotiert; bei gemeinsamem letzten Platz entscheidet der Name alphabetisch.
- Beim Start werden die allgemeine Session und die Arche-Nova-Snapshots atomar
  gespeichert.
- Laufende Partien zeigen Reihenfolge, Spielerfarbe, Zooplan und Dauer.
- Eine laufende Partie kann beendet (`Running -> Scoring`) oder nach
  Sicherheitsabfrage verworfen werden.

### Arche Nova – Wertung

- Wertungsrohwerte werden während `Scoring` automatisch gespeichert.
- Attraktivität: `0..113`.
- Artenschutz: `0..41`.
- Ruf: optional `1..15`.
- Ruf-Erfassung ist eine globale lokale Arche-Nova-Einstellung.
- Neue und alte Punkteanzeige sind reine Darstellung; berechnete Punkte werden
  nicht gespeichert.
- Die fünf besonderen Karten werden als konkrete Karten gespeichert.
- Dieselbe konkrete Karte darf ausnahmsweise mehreren Spielern derselben Partie
  zugeordnet sein; beim zweiten Spieler wird explizit nachgefragt.
- Dieselbe Karte kann demselben Spieler nicht doppelt zugeordnet werden.
- `Wertung abschließen` wird erst möglich, wenn bei allen Spielern Attraktivität
  und Artenschutz erfasst sind.
- Punktgleichstände verwenden Competition Ranking und können innerhalb des
  ursprünglichen Gleichstandsbereichs beibehalten oder aufgelöst werden.
- Erst `Completed` zählt für Historie und Statistik.
- Der Ergebnis-Screen bleibt nach Abschluss sichtbar, bis `Fertig` gewählt wird.
- Die Partiedauer wird aus Start- und Endzeit abgeleitet.
- Der Kamera-Button ist bereits vorgesehen; die eigentliche
  Wertungsmatten-/Fotoerkennung ist noch nicht implementiert.

### Einstellungen

Arche-Nova-Einstellungen:

- `Ruf erfassen`
- `Neue Wertung / Alte Wertung`

Allgemeine Navigationseinstellungen:

- optionales Standardspiel
- gespeicherter Spiel-/Navigationskontext

Diese Einstellungen liegen in nativen MAUI Preferences und nicht in SQLite.

## Noch offene bzw. bekannte Punkte

Die folgenden Punkte sind fachlich entschieden, aber im aktuellen Code-Stand
noch nicht vollständig umgesetzt oder noch zu prüfen:

1. **Offene Sessions:** Fachlich gilt künftig maximal eine offene Session je
   Kombination aus **Spiel + Gruppe**. Mehrere offene Sessions sind erlaubt,
   wenn Spiel oder Gruppe unterschiedlich sind. Der aktuelle
   Arche-Nova-Startpfad blockiert noch global, sobald irgendeine Session
   `Running` oder `Scoring` ist.
2. **Standardspiel-Start:** Das automatische Öffnen des favorisierten Spiels
   wurde stabilisiert, muss aber weiterhin auf realen Geräten geprüft werden.
3. **Ergebnisfarben:** Ergebnis-Spielerkacheln sollen in der tatsächlichen
   Spielerfarbe getönt sein. Der aktuelle Stand zeigt dies noch nicht in allen
   Fällen korrekt.
4. **Kontrast auf Spielerfarben:** Fachliche Inhalte auf farbig getönten
   Spielerkacheln dürfen niemals die graue Sekundärtextfarbe verwenden. Diese
   Regel ist verbindlich und muss im Ergebnis-Screen noch vollständig
   durchgesetzt werden.
5. **Historie:** Der Tab existiert, die vollständige Historienliste ist noch ein
   eigener Feature-Slice.
6. **Fotoerkennung:** Kamera-/Wertungsmattenerkennung ist noch nicht
   implementiert.
7. **Versionierung:** Beim nächsten ausgelieferten Code-Update müssen
   `ApplicationDisplayVersion` und die technische `ApplicationVersion`
   mitgeführt werden.

## Architektur in Kurzform

```text
App -> Application -> Core
          ^
          |
Infrastructure -> Application + Core
```

- `Core` enthält Domainmodell und reine Fachregeln.
- `Application` enthält Use Cases, Read Models und technische Ports.
- `Infrastructure` enthält EF Core, SQLite und Implementierungen technischer
  Ports.
- `App` enthält MAUI, XAML, ViewModels, Navigation, Presentation und den
  Composition Root.
- Feature-ViewModels kennen keine konkrete EF-/SQLite-Implementierung.
- Es gibt keine generischen Repositories, MediatR-Handler,
  Navigation-Service-Abstraktion aus Prinzip oder `Common`-/`Utils`-Sammelordner.

Details stehen in `docs/ARCHITECTURE.md`.

## Datenbank während der Pre-Alpha-Entwicklung

Der Runtime-Start verwendet aktuell:

```text
Database.EnsureCreatedAsync()
```

Danach werden `ark-nova` und die 14 Zoopläne idempotent als Referenzdaten
sichergestellt.

`EnsureCreated` migriert ein bestehendes älteres Schema nicht. Bei einer echten
Schemaänderung wird die lokale Entwicklungsdatenbank deshalb derzeit gelöscht
und aus dem aktuellen Modell neu erzeugt. Sobald veröffentlichte Nutzerdaten
über Schemaänderungen erhalten werden müssen, wird auf einen kanonischen,
versionierten EF-Core-Migrationsverlauf umgestellt.

Im Repository vorhandene ältere Migrationsdateien sind für den aktuellen
Runtime-Pfad nicht autoritativ.

## Dokumentationsstruktur

Diese Dateien beschreiben immer den **aktuellen gültigen Stand**:

- `README.md`
- `SETUP.md`
- `docs/PRODUCT_SPEC.md`
- `docs/ARCHITECTURE.md`
- `docs/DATA_MODEL.md`
- `docs/UI_DESIGN_GUIDE.md`
- `docs/UI_WIREFRAME.md`

Versionshistorie gehört ausschließlich in `CHANGELOG.md`. Überholte
Versionsabschnitte werden nicht dauerhaft an die aktuellen Spezifikationen
angehängt.

## Quellen

Die mit dem Projekt hinterlegten deutschen Arche-Nova-Grundregeln und
Wasserwelten-Regeln dienen als fachliche Referenz für Spielbegriffe und spätere
Foto-/Wertungserkennung. Die App bildet nicht die vollständigen Spielregeln ab,
sondern nur die für Tracking, Vorbereitung, Wertung und Statistik benötigten
Daten und Regeln.
