# Boardgame Tracker – v0.8.1.1

v0.8.1.1 ist ein kleiner technischer Folgestand auf Basis des v0.8.1-Reviews.
Er korrigiert die lokale Datenbankinitialisierung, vereinheitlicht Shell-
Navigationsparameter und ordnet die beiden Gruppenlistenfilter horizontal an.

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
- Eine offene Partie wird beim App-Start wieder aufgenommen.
- Die eigentliche Wertungserfassung ist der nächste größere Feature-Slice.

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
