# Boardgame Tracker – Lokaler History-Transfer

## 1. Zweck und Umfang

Diese Datei ist die kanonische Quelle für den **spielübergreifenden `.bgt`-
Transfervertrag**: Envelope, Versionierung, allgemeine Validierung, Matching,
Konfliktauflösung, Commit und native Ausgabeziele.

Spielspezifische Schemas und Referenzregeln stehen unter
`docs/Games/<GameName>/IMPORT_EXPORT.md`.

`.bgt` überträgt gezielt **abgeschlossene `GameSession`s** zwischen Installationen.
Export startet mit genau den aktuell in der Historie ausgewählten/gefilterten
Session-IDs; Import ist davon unabhängig.

Nicht-Ziele:

- Cloud-/Gerätesynchronisation;
- vollständiges Backup;
- Partial Import;
- Löschabgleich;
- persistente Foreign-ID-Aliase oder Mappings;
- Fuzzy Matching;
- Merge zweier `GameSession`s.

Importzuordnungen gelten nur für den jeweiligen Importvorgang.

## 2. Format v1

Datei:

```text
UTF-8 JSON ohne BOM
Endung .bgt
kein ZIP
keine Anhänge
formatVersion = 1
```

Envelope, camelCase; alle aufgeführten Konstruktorfelder sind Pflicht:

```text
formatVersion, appVersion, exportedAtUtc

games: [{
  id, key, schemaVersion, data
}]

players: [{
  id, name, isGuest, isArchived, createdAt
}]

groups: [{
  id, name, isTemporary, isArchived,
  groupPlayers: [{ playerId, defaultOrder }]
}]

gameSessions: [{
  id, gameId, groupId, startedAt, endedAt, modifiedAt,
  players: [{ playerId, order, placement }],
  data
}]
```

`gameSessions` enthält ausschließlich `Completed`-Partien; der Status wird nicht
redundant transportiert.

Zeitstempel sind `DateTimeOffset` in JSON-ISO-Darstellung. `createdAt` ist der echte
Anlagezeitpunkt und wird nicht aus Gruppen oder Partien abgeleitet.

Alle IDs sind nichtleere GUIDs. UUIDv7 ist keine Importvoraussetzung.

`appVersion` und `exportedAtUtc` sind Metadaten.

### Legacy-Kompatibilität von `groups[].isTemporary`

Das Feld bleibt im bestehenden **Wireformat v1** ein erforderlicher boolescher Wert, obwohl das aktuelle Gruppen-Domainmodell keinen temporären Status mehr kennt. Der neue Export schreibt stets `isTemporary: false`. Beim Import werden alte Werte `true` und `false` akzeptiert, aber **nicht** ins Domainmodell übertragen. Unbekannte Felder bleiben weiterhin verboten.

## 3. Format- und Spielversionen

`formatVersion` und `games[].schemaVersion` sind unabhängige positive
Versionsverträge. Eine Änderung eines Spielschemas erhöht nicht automatisch die
allgemeine Formatversion.

Der Export schreibt immer die aktuell unterstützten Versionen.

Nicht unterstützte Format- oder Spielschema-Versionen und Spielschlüssel außerhalb
unterstützter Modul-Namensräume werden als **gesamte Datei** abgelehnt. Feste
registrierte Keys gehören integrierten Spielen; `manual:<GUID>` wird vom
ManualGames-Transferschema aufgelöst. Es gibt kein Best Effort.

Unterstützte ältere Versionen werden vor Matching in aktuelle DTOs
migriert; die Quelldatei bleibt unverändert. Der strukturelle Format-v1-Vertrag
bleibt bestehen; eine neue allgemeine Formatversion ist nicht erforderlich.
Der Legacy-Wert `isTemporary` wird beim Transfer in das aktuelle Gruppenmodell
ignoriert. Die Unterstützung früherer spielspezifischer Schemas wird ausschließlich
im jeweiligen Spielschema festgelegt; für Arche Nova gelten die explizit
unterstützten und abgelehnten Versionen in
[Games/ArkNova/IMPORT_EXPORT.md](Games/ArkNova/IMPORT_EXPORT.md).

Manuelle Spiele verwenden ein gemeinsames versioniertes ManualGames-Schema;
konkrete lokale manuelle Keys sind keine eigenständigen registrierten Schemata.
Details: [Games/ManualGames/IMPORT_EXPORT.md](Games/ManualGames/IMPORT_EXPORT.md).

## 4. Export-Snapshot

Exportiert werden nur die ausgewählten `Completed`-Sessions. Ist eine ausgewählte
Session beim tatsächlichen Export nicht mehr verfügbar oder nicht mehr abgeschlossen,
schlägt der Export fehl.

Ein gemeinsamer lesender Snapshot enthält nur die für diese Sessions benötigten:

- `Game`-Datensätze;
- `Group`s;
- vollständigen festen `GroupPlayer`-Mitgliedschaften samt `DefaultOrder`;
- referenzierten `Player`;
- `GameSessionPlayer`;
- spielabhängigen Sessiondaten;
- vom Spielmodul für diese Sessions benötigten Referenzdaten.

Nicht exportiert werden Preferences, nicht benötigte Stammdaten oder
spielabhängige Gruppendefaults. Format v1 besitzt dafür kein spielabhängiges
Group-Payload.

Aktuelle `GroupGame`-Zuordnungen werden im History-Transfer nicht separat exportiert. Der Export ist kein Backup der aktuellen Spieleauswahl oder Gruppenverwaltung.

Spielmodule lesen ihre Exportwerte gebündelt, nicht per N+1-Abfrage pro Partie.

## 5. Lesen und allgemeine Validierung

Pipeline:

```text
native Dateiauswahl
→ begrenztes Lesen
→ striktes UTF-8
→ JSON / Tiefe
→ Envelope / Versionen
→ allgemeine DTO-/Domainvalidierung
→ Spielschema-Validierung
→ bekannte GameSession-IDs überspringen
→ Dependency-Pruning
→ Analyze
→ Resolve
→ Preview
→ Commit
```

Dateiname, MIME-Type und Endung dienen nur der Auswahl. Die Inhaltsprüfung entscheidet
über die Verwendbarkeit.

Zentrale Grenzen:

- maximal **16 MiB** gesamte Datei, auch bei Streams ohne Längeninformation;
- JSON-Tiefe maximal **32**;
- maximal **10.000** Elemente pro JSON-Array/Entitymenge;
- JSON-Strings maximal **1.000** Zeichen;
- `appVersion` maximal **100** Zeichen;
- keine doppelten JSON-Properties;
- keine unbekannten DTO-Felder;
- Pflichtfelder und Nichtnull-Collections müssen vorhanden sein;
- IDs pro Entitymenge: nichtleer und eindeutig;
- `Player.Name`/`Group.Name`: nach `Trim()` 1–100 Zeichen und je Typ
  `OrdinalIgnoreCase` eindeutig;
- `Game.Key` innerhalb der Datei eindeutig;
- alle allgemeinen Referenzen müssen auflösbar sein;
- `GroupPlayer.DefaultOrder` lückenlos `1..n`; `GameSessionPlayer.Order` entweder für alle Teilnehmer `null` oder vollständig lückenlos `1..n`;
- Session-Spielermenge muss exakt der Spieler-Menge ihrer Gruppe entsprechen;
- `placement` ist ein erforderliches Feld mit nullable Wert; vorhandene Plätze liegen in `1..n`. Spielmodule prüfen die jeweils erforderliche Ergebnisform;
- `EndedAt >= StartedAt`;
- `ModifiedAt >= EndedAt`.

Spielspezifische `data`-Felder und Invarianten validiert das registrierte
`IGameTransferSchema`.

Ungültige Eingaben verändern keine produktiven Daten.

## 6. Bereits bekannte Sessions und Dependency-Pruning

Eine lokal bereits vorhandene `GameSession.Id` wird ohne Inhaltsvergleich
übersprungen und niemals überschrieben.

Danach werden alle ausschließlich von übersprungenen Sessions benötigten Groups,
Players, Games und Modulreferenzen aus dem Importplan entfernt.

Matching und Preview arbeiten nur noch auf den tatsächlich neu zu importierenden
Abhängigkeiten.

## 7. Player-Matching

Priorität:

```text
gleiche ID
→ gleicher normalisierter Name
→ neuer Player
```

Normalisierung entspricht dem allgemeinen Datenmodell: `Trim()` +
`OrdinalIgnoreCase`, ohne Fuzzy-, Akzent-, Satzzeichen- oder
innere-Leerzeichen-Normalisierung.

Bei einem Match gewinnt der lokale Player vollständig: Name, Gast-/Archivstatus und
Anlagezeitpunkt werden nicht überschrieben.

Bei Neuanlage werden importierte ID und importierte Stammdaten übernommen.

`ImportPlan.PlayerMappings` ist global und eins-zu-eins:

- jeder importierte Player höchstens einem lokalen Player;
- jeder lokale Player höchstens einem importierten Player.

Es gibt keine persistente Alias- oder Mappingtabelle.

## 8. Gruppenanalyse und Resolve

### Kandidat

Eine gleiche `Group.Id` bezeichnet dieselbe Identität. Nach den globalen
Player-Mappings muss die endgültige Mitgliedermenge exakt identisch sein; sonst ist
der Import ungültig.

Ohne ID-Match ist eine lokale Gruppe nur Kandidat, wenn:

- der normalisierte Name übereinstimmt;
- die Spieleranzahl übereinstimmt.

Ist nach den globalen Player-Mappings die endgültige Mitgliedermenge eindeutig
identisch, wird automatisch zusammengeführt.

### Manuelle Zuordnung

Offene zulässige Zuordnungen erscheinen im Resolve-Schritt. Sichere Identitäten sind
nicht editierbar.

Manuelle Player-Zuordnungen wirken **global** für den gesamten Import. Nach jeder
Entscheidung werden alle noch offenen Gruppenkandidaten neu bewertet; unmöglich
gewordene Zuordnungen werden nicht weiter angeboten.

### Gruppe trennen

Eine importierte Gruppe kann ausdrücklich getrennt als neue Gruppe übernommen
werden, sofern keine sichere gleiche ID dagegensteht.

Bei Namenskollision wird deterministisch nach `Group.Id` der erste freie Name
gebildet:

```text
Montag Import
Montag Import 2
Montag Import 3
...
```

Bei 100-Zeichen-Namen wird nur der Stamm so gekürzt, dass der Suffix hineinpasst.

Eine unterschiedliche Spieleranzahl verhindert ein Match und erzeugt ebenfalls
eine neue Importgruppe.

### Lokale vs. importierte Werte

Bei zusammengeführter Gruppe bleiben lokal:

- Name;
- Mitgliedschaft;
- `DefaultOrder`;
- `IsArchived`;
- bestehende `GroupGame`-Zuordnungen;
- spielabhängige lokale Gruppendefaults.

Bei neuer Gruppe werden importiert:

- endgültige Mitgliedschaft;
- `DefaultOrder`;
- `IsArchived`.

Bestehende Gruppenmitgliedschaften werden niemals verändert.

### Gruppen-Spiel-Zuordnungen beim Import

Nur für eine **tatsächlich neu importierte Gruppe** werden `GroupGame`-Zuordnungen aus ihren tatsächlich neu importierten `GameSession`-Datensätzen abgeleitet und innerhalb derselben Transaktion angelegt, soweit die lokalen `Game.MinPlayers..Game.MaxPlayers`-Grenzen eingehalten sind. Andernfalls wird die aktuelle Zuordnung ausgelassen, die historisch gültige Session aber importiert. Die zulässigen Zuordnungen werden je `(GroupId, GameId)` dedupliziert.

Wird eine importierte Gruppe dagegen auf eine **lokal bereits vorhandene Gruppe** gemappt (per ID, Name oder manueller Resolve-Entscheidung), bleiben deren aktuelle Spielzuordnungen vollständig **unverändert**. Auch neu importierte historische Sessions eines bisher nicht zugeordneten Spiels erzeugen für diese lokale Gruppe **keine** neue Zuordnung. Damit darf eine bewusst entfernte Verknüpfung nicht durch einen späteren History-Import zurückkehren.

Bereits bekannte/übersprungene Sessions lösen keine neuen Zuordnungen aus. Eine historische Session darf auch ohne aktuelle `GroupGame`-Zuordnung bestehen.

## 9. Preview

Preview schreibt nichts.

Es zeigt mindestens:

- betroffene vorhandene/bekannte Games;
- automatische, manuelle und neue Player;
- zusammengeführte und neue Groups einschließlich neuer Kollisionsnamen;
- zu importierende und bereits vorhandene/übersprungene `GameSession`s;
- neue und vorhandene spielabhängige Referenzen.

Spielspezifische Preview-Zählung kommt vom registrierten Transfermodul.

## 10. Commit und Atomarität

Commit verwendet einen **neuen DbContext** und eine **gemeinsame Transaktion** für
allgemeine Stammdaten, Sessions und sämtliche Moduldaten.

Vor dem Schreiben wird der analysierte Snapshot erneut gegen den aktuellen Zustand
geprüft. Dazu gehören insbesondere:

- Player-/Group-Namen;
- Gruppenmitgliedschaften;
- vorhandene Session-IDs;
- bei neu anzulegenden Gruppen die daraus abgeleiteten `GroupGame`-Zuordnungen;
- spielabhängiger Referenzzustand.

Bei Änderung wird der Import abgebrochen und muss neu analysiert werden.

Danach wird der Plan mit demselben Resolve-Stand erneut berechnet. Nur für neu anzulegende Gruppen werden dabei die aus den zu importierenden Sessions abgeleiteten Spielzuordnungen berücksichtigt. Offene Konflikte
verhindern den Commit.

Neu angelegte Player und Groups behalten ihre importierten IDs. Integrierte Spiele
werden über ihren festen `Game.Key` zugeordnet; manuelle Spiele ausschließlich über
Namensmatching und die ausdrückliche Importentscheidung **innerhalb** des
ManualGames-Moduls. Ihre Quell-Keys dienen nur der Dateireferenzierung, nicht dem
lokalen Matching. Neue manuelle Spiele erhalten eine lokale Identität.

Nur für neue Gruppen werden `GroupGame`-Zuordnungen in derselben Transaktion
angelegt, sofern die jeweilige lokale `Game.MinPlayers..Game.MaxPlayers`-Grenze
eingehalten ist; andernfalls bleibt die historische Session ohne aktuelle
Zuordnung. Vorhandene Gruppen behalten ihre Zuordnungen unverändert.

Alle Inserts und Spielmoduldaten werden gemeinsam gespeichert. Jede Exception rollt
die Transaktion zurück.

Es gibt:

- kein automatisches Löschen;
- kein Überschreiben bestehender Sessions;
- keine Teilübernahme.

## 11. Spielmodule

Allgemeiner Transfer kennt konkrete Spiele nur über registrierte Verträge.

`IGameTransferSchema` besitzt:

- Zuständigkeit für einen festen registrierten Spiel-Key oder einen definierten
  dynamischen Key-Namensraum;
- `SchemaVersion` sowie explizit unterstützte Eingabeversionen;
- Normalisierung älterer `GameSession.data` vor dem Matching;
- Validierung von `Game.data` und `GameSession.data`.

`IGameTransferPersistence` besitzt für dasselbe Spiel:

- Snapshot des relevanten lokalen Referenzzustands;
- Export der benötigten Referenzen;
- Export der Sessiondaten;
- Preview der Modulreferenzen;
- Import der Moduldaten.

Das Spielmodul arbeitet bei Import auf demselben DbContext und derselben Transaktion
wie der allgemeine Transfer.

Spielschemata: [Arche Nova](Games/ArkNova/IMPORT_EXPORT.md) und
[ManualGames](Games/ManualGames/IMPORT_EXPORT.md).

## 12. Exportziele

Zu Beginn eines Exports werden die aktuell ausgewählten/gefilterten Session-IDs
festgehalten.

`HistoryTransferUseCase.CreateExportContentAsync` erzeugt und validiert den
`BgtExportContent` **genau einmal**: vorgeschlagener Dateiname plus UTF-8-Bytes.

Danach wählt der Benutzer genau ein Ziel:

```text
Speichern
Teilen
Abbrechen
```

Alle Ziele erhalten denselben bereits erzeugten Inhalt. Ausgabeadapter besitzen
keine eigene Format-, Matching- oder Serialisierungslogik.

### Speichern

Android verwendet `ACTION_CREATE_DOCUMENT` + `CATEGORY_OPENABLE` mit
vorgeschlagenem `.bgt`-Dateinamen. Die Bytes werden direkt in das ausgewählte
Dokument geschrieben; keine allgemeine Speicherberechtigung ist erforderlich.

iOS verwendet `UIDocumentPickerViewController` als Exportkopie. Die temporäre
Quelldatei wird anschließend entfernt.

Speichern ist ein nativer Dokumentflow, kein Share-Sheet.

### Teilen

Teilen verwendet den MAUI-Share-Flow mit einer `.bgt`-Datei im App-Cache.
Die Datei bleibt für empfangende Apps ausreichend lange lesbar; die installierten
Ziele bestimmt das native Share-Sheet.

### Abbruch und Fehler

Abbruch von Zielauswahl, Dokument-Picker oder Share-Sheet ist ein normaler Abbruch.
Es folgt keine automatische zweite Aktion.

Tatsächliche I/O-Fehler werden über den bestehenden Fehlerdialog gemeldet.

Weitere `IHistoryExportTarget`-Implementierungen dürfen später denselben
versionierten Inhalt transportieren. QR-Code, Kompression und Segmentierung sind
derzeit nicht implementiert und ändern den `.bgt`-Vertrag nicht.

Konkrete History-Screenanordnung: [UI_WIREFRAME.md](UI_WIREFRAME.md).
