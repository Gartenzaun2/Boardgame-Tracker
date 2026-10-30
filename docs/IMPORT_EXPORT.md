# Lokaler Import und Export

## Umfang und Nicht-Ziele

.bgt-Dateien tauschen gezielt abgeschlossene GameSessions zwischen Installationen aus.
Export startet mit exakt den in der Historie gefilterten GameSession-IDs. Import ist
filterunabhängig. Aktuell wird ausschließlich das bekannte Spiel Arche Nova unterstützt.
Custom Games werden vollständig abgelehnt; ihre Konfiguration und Ergebnisschemas gehören
zum späteren Custom-Game-Feature. Die Modulgrenze erlaubt dessen spätere Ergänzung.

Kein Cloud-/Geräte-Sync, vollständiges Backup, Partial Import, Löschabgleich,
Foreign-ID-Aliase, persistentes Mapping, Fuzzy Matching oder GameSession-Merge.
Zuordnungen gelten ausschließlich innerhalb eines Importvorgangs.

## Format v1

UTF-8-JSON ohne BOM, Dateiendung `.bgt`, kein ZIP und keine Anhänge. Der Export schreibt
immer die aktuell unterstützte Version. `formatVersion = 1`, Arche-Nova-`schemaVersion = 1`.
Versionen sind positive Integer. `appVersion` und `exportedAtUtc` sind reine Metadaten.
Format- und Spielversionen sind unabhängig; Spieländerungen ändern nicht automatisch das
Envelope. Es existiert noch keine veröffentlichte ältere Version und daher kein älterer
Migrationsschritt. Künftige unterstützte Altversionen werden vor Matching schrittweise in
DTOs migriert; Quelldateien bleiben unverändert. Höhere/unbekannte Versionen sowie unbekannte
Known-Game-Keys erfordern ein App-Update und werden vollständig abgelehnt. Kein Best Effort.

Envelope (camelCase; alle aufgeführten Konstruktorfelder sind Pflicht):

```text
formatVersion, appVersion, exportedAtUtc
 games: [{ id, key, schemaVersion, data }]
 players: [{ id, name, isGuest, isArchived, createdAt }]
 groups: [{ id, name, isTemporary, isArchived,
            groupPlayers: [{ playerId, defaultOrder }] }]
 gameSessions: [{ id, gameId, groupId, startedAt, endedAt, modifiedAt,
                  players: [{ playerId, order, placement }], data }]
```

GameSessions sind ausschließlich Completed; der Status wird deshalb nicht redundant
transportiert. Zeitstempel sind DateTimeOffset-Werte in JSON-ISO-Darstellung. Erstellt wird
als echter Anlagezeitpunkt transportiert und niemals aus Partien/Gruppen abgeleitet.
Alle IDs sind GUIDs ungleich Guid.Empty; UUIDv7 ist keine Importvoraussetzung.

Arche-Nova-Game.data:

```text
specialCards: [{ id, name, symbol1, symbol2, symbol3, isArchived }]
```

Arche-Nova-GameSession.data:

```text
players: [{ playerId, color, zooPlanOption1Number, zooPlanOption2Number,
            selectedZooPlanNumber, attractiveness, conservation, reputation,
            specialCardIds }]
```

Farben und Kartensymbole verwenden die numerischen Domain-Enumwerte. Nullable Symbole und
Ruf werden explizit als null geschrieben. Berechnete Punkte werden nicht transportiert.

## Export und Abhängigkeiten

Nur selektierte Completed-GameSessions; ändert sich deren Verfügbarkeit vor Export,
schlägt Export fehl. Ein lesender Transaktionssnapshot liefert Games, Groups, die
vollständigen festen GroupPlayer-Mengen, deren Player, getrennte GameSessionPlayer,
Spielwerte und ausschließlich referenzierte Spezialkarten. Keine zukünftigen
Gruppenkomfortdefaults (z. B. Farben), Preferences oder unreferenzierten Stammdaten.
Modulprojektionen lesen Werte gebündelt statt pro Partie abzufragen.

## Validierung und Pipeline

Native Dateiauswahl → begrenztes Lesen → striktes UTF-8 → JSON/Tiefe → Envelope/Versionen
→ strukturierte DTO-/Domainvalidierung → bekannte GameSession-IDs überspringen
→ Dependency-Pruning → Analyze → Resolve → Preview → Commit.

Zentrale Limits in BgtFormat:

- 16 MiB gesamte Datei, auch bei Streams ohne Längeninformation.
- JSON-Tiefe 32 und höchstens 10.000 Elemente pro Collection/Entitymenge.
- JSON-Strings maximal 1.000 Zeichen, Appversion maximal 100.
- Player-/Group-Namen nach Trim 1–100 Zeichen; Spezialkarten 1–120.
- Keine doppelten JSON-Properties oder unbekannten DTO-Felder.
- Pflichtfelder, Nichtnull-Collections, gültige GUIDs, eindeutige IDs je Entitymenge.
- Eindeutige Player-/Group-Namen nach Trim + OrdinalIgnoreCase; eindeutige Known Keys.
- Auflösbare Referenzen und feste Player-Menge(GameSession) = Player-Menge(Group).
- Getrennte, lückenlose Ordnungen 1..n für GroupPlayer und GameSessionPlayer.
- Endzeit nicht vor Start, ModifiedAt nicht vor Ende, gültige Placements.
- Arche Nova: höchstens vier Spieler, eindeutige erlaubte Farben, 1–14-Zoopläne,
  zwei unterschiedliche Angebote je Spieler, insgesamt keine doppelten Angebote,
  Auswahl aus dem Angebot, Domainwertebereiche für Punkte/Ruf und gültige Tie-Blöcke.
- Eindeutige Spezialkartenzuordnungen pro importiertem Spieler und auflösbare Karten.

Ungültige Eingaben verändern keine produktiven Daten. Dateiname/MIME/Endung dienen
nur der Auswahl; die Datenprüfung entscheidet über die Verwendbarkeit.

## Analyze und globale Player-Zuordnungen

Player-Matching: gleiche ID → sonst gleicher normalisierter Name → sonst zunächst neu.
Lokale Stammdaten gewinnen vollständig (Name, Gast-/Archivstatus, Anlagezeitpunkt).
Bei Neuanlage bleiben importierte IDs und Werte erhalten.

Der ImportPlan hält PlayerMappings global: jeder ImportedPlayer hat maximal einen
LocalPlayer und jeder LocalPlayer maximal einen ImportedPlayer. Das gilt auch für
automatische Zuordnungen. Keine Alias-Persistenz. Keine kulturabhängige, Akzent-,
Satzzeichen- oder innere-Leerzeichen-Normalisierung.

Vorhandene GameSession-IDs werden ohne Inhaltsvergleich übersprungen. Alle ausschließlich
davon abhängigen Groups, Players, Games und Spezialkarten werden nicht angelegt.

## Gruppen und Resolve

Gleiche GroupId identifiziert dieselbe Gruppe; die endgültige Player-Menge muss identisch
sein. Jede Abweichung ist ein Importfehler. Ohne ID-Match gilt ein lokaler Datensatz nur
bei gleichem normalisiertem Namen und gleicher Spieleranzahl als Kandidat.

Identische endgültige Player-Mengen werden automatisch zusammengeführt. Offene zulässige
Zuordnungen werden im Resolve angeboten. Sichere Identitäten sind nicht editierbar.
Manuelles Mapping verschieden benannter Spieler existiert nur in diesem Gruppenkontext.
Nach jeder Entscheidung werden ALLE offenen Kandidaten mit globalen Mappings neu bewertet.
Ein durch Tom → Thomas unmöglich gewordenes Tom → Tobias wird nicht mehr angeboten.

Gruppen können ausdrücklich getrennt werden. Name bei Kollision: erster freier Name
`Montag Import`, `Montag Import 2`, usw., deterministisch in Group-ID-Reihenfolge.
Bei maximal langen Namen wird nur der Stamm gekürzt, damit der Suffix hineinpasst.
Ein Split verändert bereits globale Spieleridentitäten nicht. Unterschiedliche
Spieleranzahl verhindert Match und führt ebenfalls zu einer eigenständigen Importgruppe.

Bei Group-Match bleiben Name, Reihenfolge, Temporär-/Archivstatus und lokale Spieldefaults
vollständig erhalten. Mitgliedschaften werden nie geändert. Neue Gruppen übernehmen
Zusammensetzung, importierte Reihenfolge und Flags.

## Spezialkarten

Nur referenzierte Karten der tatsächlich neu importierten Partien werden verarbeitet.
Gleiche ID → lokale Karte. Sonst genau ein normalisierter Namensmatch → lokale Karte.
Mehrere lokale Namensmatches ohne ID-Match → vollständiger Importfehler schon vor Commit.
Kein Match → neue Karte mit importierter ID, Namen, Symbolen und Archivstatus.
Lokale Kartenwerte werden niemals überschrieben. Mehrere auf dieselbe lokale Karte
aufgelöste Zuordnungen desselben Spielers werden als eine Zuordnung gespeichert.

## Preview und Commit

Preview zählt neue/vorhandene Games (aktuell nur Known Games), automatische/manuelle/neue
Player, zusammengeführte/neue Groups mit neuen Namen, importierte/übersprungene GameSessions
und neue/vorhandene Modulreferenzen. Keine Writes vor dem abschließenden Importbutton.
Es werden aktuell keine historischen Werte verworfen; deshalb entstehen keine der später
für Custom Games benötigten Warnungen über entfernte Scores.

Commit verwendet einen neuen Context und eine gemeinsame Transaktion für Stammdaten,
GameSessions und sämtliche Moduldaten. Ein erneuter Snapshotvergleich prüft Namen,
Mitgliedschaften, Partie-IDs und Modulreferenzdaten gegen den analysierten Stand. Bei
Änderungen muss erneut analysiert werden. Der Plan wird nochmals berechnet. Alle Inserts
und Moduldaten werden gemeinsam gespeichert; jede Exception rollt die Transaktion zurück.
Kein automatisches Löschen, kein Überschreiben bestehender Partien, keine Teilübernahme.

## Ausgabeziele und Erweiterbarkeit

Nach Tap auf den History-Exportbutton werden die aktuell gefilterten GameSession-IDs
festgehalten. HistoryTransferUseCase.CreateExportContentAsync erzeugt, validiert und
serialisiert den Export genau einmal in BgtExportContent (Dateiname und UTF-8-Bytes).
Erst dann wählt der Benutzer im App-Dialog Speichern, Teilen oder Abbrechen.
HistoryExportFlow übergibt denselben Inhalt ausschließlich an das gewählte
IHistoryExportTarget. Die Ausgabeadapter haben keine eigenen Format-, Matching- oder
Serialisierungsregeln. Sie gehören zur App; die konkrete HistoryTransferPersistence
im Application-Slice übernimmt die EF-Persistenz. IGameTransferPersistence bleibt
als Spielmodulgrenze erhalten und arbeitet auf demselben Context und derselben Transaktion.

Speichern verwendet auf Android ACTION_CREATE_DOCUMENT mit CATEGORY_OPENABLE,
vorgeschlagenem .bgt-Dateinamen und einem beschreibbaren Dokument-URI. Die Bytes werden
direkt in das vom Benutzer ausgewählte Dokument geschrieben; keine Speicherberechtigung
für allgemeine Verzeichnisse nötig. Auf iOS exportiert UIDocumentPickerViewController
mit asCopy eine temporäre Datei in den gewählten Speicherort; die temporäre Quelldatei
wird danach entfernt. Speichern ist ein echter nativer Dokumentflow, kein Share-Sheet.

Teilen verwendet den vorhandenen MAUI-Share-Flow mit einer .bgt-Datei im App-Cache.
Die Cache-Datei bleibt für empfangende Apps lesbar, auch wenn das Share-Sheet bereits
geschlossen ist. Der native Share-Sheet stellt die installierten Ziele bereit.

Abbrechen der Zielauswahl, des Dokument-Pickers oder Share-Sheets beendet den Export
normal. Es gibt keine automatische Doppelaktion und keinen Wechsel zum anderen Ziel.
Tatsächliche I/O-Fehler werden über den bestehenden Fehlerdialog angezeigt.

Weitere Ziele können später über IHistoryExportTarget registriert werden. Ein QR-Code
wäre ausschließlich ein Transport desselben versionierten BGT-Inhalts bzw. einer eindeutig
daraus abgeleiteten Transportrepräsentation. Formatversionen, Spielschemas, Validierung,
Matching und ImportPlan bleiben davon unberührt. QR-Code, Kompression und Segmentierung
sind in diesem Stand nicht implementiert.

Native API-Verträge: [Android ACTION_CREATE_DOCUMENT](https://learn.microsoft.com/en-us/dotnet/api/android.content.intent.actioncreatedocument)
und [iOS Dokumentexport als Kopie](https://developer.apple.com/documentation/uikit/uidocumentpickerviewcontroller/init(forExporting:asCopy:)).
