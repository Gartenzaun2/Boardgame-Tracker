# Boardgame Tracker – Allgemeines Datenmodell

## 1. Geltungsbereich

Diese Datei beschreibt ausschließlich das **spielübergreifende fachliche Datenmodell**
und seine allgemeinen Persistenzinvarianten.

Spielspezifische Entities, Ergebniswerte, Referenzdaten und Invarianten stehen unter:

```text
docs/Games/<GameName>/DATA_MODEL.md
```

Bei Arbeiten an einem konkreten Spiel gelten dieses allgemeine Datenmodell und
ergänzend das Datenmodell des Spiels.

Grundprinzipien:

- allgemeine Daten bleiben spielunabhängig;
- spielabhängige Daten ergänzen allgemeine Aggregate über Composition;
- berechenbare Werte werden nicht redundant gespeichert;
- historische Datensätze referenzieren stabile Identitäten statt lokalisierter
  Anzeigenamen oder anderer ableitbarer Snapshots.

Architektur und Modulgrenzen: [ARCHITECTURE.md](ARCHITECTURE.md).

## 2. Identitäten und Namen

### GUIDs

Neue reguläre Benutzer-/Partie-Entities erhalten ihre ID in C# über
`Guid.CreateVersion7()`:

- `Player`
- `Group`
- `GameSession`

Wo Import die Identität einer solchen Entity übernimmt, bleibt jede gültige,
nichtleere importierte GUID erhalten.

Feste Referenzdaten eines Spielmoduls dürfen deterministische Identitäten besitzen.
Diese werden im jeweiligen Spiel-Datenmodell dokumentiert.

### Entity-Namen

`Player.Name` und `Group.Name` verwenden dieselbe Semantik:

```text
Trim()
Vergleich: StringComparer.OrdinalIgnoreCase
Länge: 1..100
```

Es gibt keine Fuzzy-, Akzent-, Satzzeichen- oder
innere-Leerzeichen-Normalisierung.

Player-Namen sind unter allen Playern einschließlich archivierter Player eindeutig.
Group-Namen sind unter allen Groups einschließlich archivierter Groups eindeutig.
Die Eindeutigkeit gilt **je Entity-Typ**; Player- und Group-Namen teilen keinen
gemeinsamen Namensraum.

SQLite schützt dieselbe Semantik über eindeutige Namensindizes mit der
`BGT_NAME`-Collation.

## 3. Player

`Player` ist ein eigenständiges Aggregate Root.

```text
Player
- Id
- Name
- IsGuest
- IsArchived
- CreatedAt
```

Regeln:

- Gast und regulärer Spieler verwenden denselben Datentyp.
- `IsGuest` kann geändert werden, ohne ID oder Historie zu verändern.
- `IsArchived` beschreibt den Archivzustand.
- `CreatedAt` ist der tatsächliche UTC-Anlagezeitpunkt.
- `CreatedAt` ändert sich durch Bearbeiten, Archivieren oder Reaktivieren nicht.
- Spieler werden nicht physisch gelöscht, nur weil eine Gruppe oder Partie
  entfernt wird.

## 4. Group und GroupPlayer

`Group` ist Aggregate Root für eine feste Gruppenzusammensetzung.

```text
Group
- Id
- Name
- IsTemporary
- IsArchived
- Players
```

`GroupPlayer` gehört zum Aggregate:

```text
GroupPlayer
- GroupId
- PlayerId
- DefaultOrder
```

Invarianten:

- eine Gruppe besitzt mindestens einen Spieler;
- derselbe Spieler kommt höchstens einmal vor;
- `DefaultOrder` ist positiv;
- `DefaultOrder` ist innerhalb der Gruppe eindeutig;
- die Reihenfolge ist lückenlos `1..n`;
- eine andere Spielerzusammensetzung ist fachlich eine andere Gruppe.

Eine bestehende Gruppe ändert ihre Mitgliedermenge daher nicht.

Bearbeitbar sind:

- Name,
- Default-Reihenfolge der bestehenden Spieler,
- Archivstatus,
- einmalig `IsTemporary: true -> false`.

Eine permanente Gruppe wird nicht wieder temporär.

`GroupPlayer` referenziert `Player` mit Restrict-Verhalten; Löschen der Group
entfernt ihre `GroupPlayer`-Einträge kaskadierend, nicht die Player.

## 5. Game

```text
Game
- Id
- Key
```

`Game.Key` ist die stabile, sprachneutrale technische Kennung eines unterstützten
Spiels. Sie ist eindeutig und maximal 100 Zeichen lang.

`Game.Id` ist die lokale relationale Identität. Der Anzeigename ist keine Identität
und wird nicht in `Game` gespeichert.

Bekannte Spielmodule verwenden ihren stabilen `Key` für Modulzuordnung,
Navigation, Preferences und Austauschlogik. Lokalisierte Anzeigenamen bleiben
Presentation-/Contentdaten.

Custom Games sind noch nicht implementiert. Das Zielbild steht in
[PRODUCT_SPEC.md](PRODUCT_SPEC.md).

## 6. GameSession und GameSessionPlayer

`GameSession` ist Aggregate Root für eine gestartete Partie.

```text
GameSession
- Id
- GameId
- GroupId
- Status
- StartedAt
- EndedAt?
- ModifiedAt
- Players
```

`GameSessionPlayer` gehört zum Aggregate:

```text
GameSessionPlayer
- GameSessionId
- PlayerId
- Order
- Placement?
```

Invarianten:

- eine gestartete Partie besitzt mindestens einen Spieler;
- jeder Spieler kommt höchstens einmal vor;
- `Order` ist positiv, eindeutig und lückenlos;
- `Order = 1` ist der Startspieler;
- `Placement` ist nullable und muss gesetzt positiv sein;
- mehrere Spieler dürfen dieselbe Platzierung besitzen, wenn das Spiel dies
  fachlich zulässt;
- `Placement = 1` bedeutet Gewinner.

Statusübergänge:

```text
Running -> Scoring -> Completed
```

Andere Übergänge sind nicht erlaubt.

`BeginScoring` setzt `EndedAt` und `ModifiedAt`. `Completed` ist der fachlich
abgeschlossene Zustand. `ModifiedAt` darf nicht vor `StartedAt` und beim Abschluss
nicht vor `EndedAt` liegen.

Die Partievorbereitung wird allgemein nicht als `GameSession` persistiert.
Eine `GameSession` entsteht erst beim tatsächlichen Start.

Während `GameSession.Status = Scoring` dürfen spielabhängige Ergebnisdaten
bereits unvollständig persistiert werden. Welche Felder für `Completed`
verpflichtend sind und welche Wertebereiche gelten, entscheidet das jeweilige
Spielmodul.

`GameSession` referenziert `Game` und `Group` mit Restrict-Verhalten.
`GameSessionPlayer` referenziert `Player` mit Restrict-Verhalten.
Löschen einer `GameSession` entfernt ihre `GameSessionPlayer`-Einträge
kaskadierend, niemals Player, Group oder Game.

## 7. Cross-Aggregate-Referenzen

Aggregate referenzieren andere Aggregate bevorzugt über stabile IDs.

Beispiele:

```text
GroupPlayer -> PlayerId
GameSession -> GameId
GameSession -> GroupId
GameSessionPlayer -> PlayerId
```

Öffentliche Navigation Properties werden nicht nur für EF-Core-Bequemlichkeit
angelegt.

Navigationen bleiben dort erhalten, wo sie Teil des Aggregats sind:

```text
Group -> Players
GameSession -> Players
```

Spielspezifische Ergänzungen referenzieren den allgemeinen Datensatz über dessen
Schlüssel und duplizieren keinen allgemeinen fachlichen Zustand. Das genaue
Composition-Muster steht in [ARCHITECTURE.md](ARCHITECTURE.md).


## 8. Game Content, Materialsprache und historische Identitäten

App-Lokalisierung und physischer Spielinhalt sind getrennte Bereiche.

Allgemeine Datenmodellregeln:

- spielabhängige Built-in-Inhalte werden über stabile, sprachneutrale Identitäten
  adressiert;
- lokalisierte Anzeigenamen sind keine historische Identität;
- historische Partiedaten speichern stabile IDs, keine lokalisierten Namenssnapshots;
- Built-in-Content wird nicht allein für Lokalisierung in SQLite gespiegelt;
- veränderliche Nutzerinhalte dürfen separat persistent gespeichert werden;
- die Materialsprache ist eine lokale Preference pro stabilem `Game.Key`;
- es gibt keine Content-Sprach-Entity nur zur Abbildung dieser Preference;
- die Materialsprache wird nicht als Feld einer `GameSession` oder eines
  spielabhängigen Ergebnisdatensatzes gespeichert.

Welche Identitäten ein Spiel verwendet und welche Nutzerinhalte es erlaubt, steht
im jeweiligen Spiel-Datenmodell.


## 9. Gruppenanlage und -bearbeitung

### Anlage

Der allgemeine Create-Group-Use-Case darf:

- vorhandene aktive Player auswählen;
- neue Player innerhalb desselben Workflows anlegen;
- neue Player als Gast markieren;
- die Default-Reihenfolge festlegen;
- optionale spielabhängige Defaults über allgemeine Erweiterungspunkte
  entgegennehmen.

Neue Player, Group, GroupPlayer und registrierte spielabhängige Gruppendefaults
werden atomar gespeichert.

Daraus folgt:

- Abbruch erzeugt keinen verwaisten neuen Player;
- bestehende Player können in mehreren Groups wiederverwendet werden;
- der Gruppen-Use-Case darf Player-Daten verwenden, ohne selbst ein
  Players-Feature zu werden.

### Bearbeitung

`EditGroup` verändert nur den erlaubten Zustand der bestehenden Group:

```text
Name
IsArchived
DefaultOrder
optional: IsTemporary true -> false
```

Die Mitgliedermenge bleibt unverändert.

Spielspezifische Gruppendaten werden separat modelliert und über allgemeine
Modulgrenzen gelesen bzw. gespeichert.

## 10. Gemeinsame Spielerfarben

`PlayerColor` ist ein gemeinsamer semantischer Farbtyp:

```text
Blue = 1
Yellow = 2
Red = 3
Black = 4
Green = 5
White = 6
Orange = 7
Brown = 8
Purple = 9
Pink = 10
Turquoise = 11
Gray = 12
```

Die Integerwerte sind persistierte semantische Identitäten und bleiben stabil.

`PlayerColor` enthält keine UI-Hexwerte und keine Erkennungsparameter.

Es gibt keine globale Farbe auf `Player`. Ein Spiel, das diese gemeinsamen Farben
verwendet, definiert seine erlaubte Teilmenge über `GamePlayerColorRules`.
Eine leere Teilmenge bedeutet: Das Spiel verwendet keine PlayerColor-Auswahl.

Spielabhängige Farbdefaults gehören fachlich zu Spiel + Group + Player und werden
in einem spielabhängigen Modell gespeichert. Konkrete Teilmengen und Entities
stehen im jeweiligen Spiel-Datenmodell.

## 11. Nicht redundant gespeicherte allgemeine Daten

Nicht als autoritative Felder speichern, wenn sie aus Rohdaten zuverlässig
berechnet werden können, beispielsweise:

- Partiedauer,
- Gewinner,
- Anzahl Siege,
- Gewinnquote,
- durchschnittliche Platzierung.

Spielspezifische abgeleitete Werte werden im jeweiligen Spiel-Datenmodell
dokumentiert.

## 12. Historische Korrekturen

Wenn ein Spiel historische Korrekturen unterstützt, bleibt die `GameSession`
fachlich `Completed`. Es gibt keine Rücktransition zu `Scoring`.

Allgemein stabil bleiben:

- `GameSession.Id`,
- `GameId`,
- `GroupId`,
- Teilnehmermenge,
- `StartedAt`,
- `EndedAt`.

Nur ausdrücklich korrekturfähige Ergebnisdaten werden atomar geändert.
Welche Ergebnisfelder dazu gehören und welche Validierung gilt, entscheidet das
jeweilige Spielmodul.

Aktuelle Player-, Group- und Contentnamen werden weiterhin über ihre stabilen
Identitäten aufgelöst; historische Korrektur erzeugt keine Namenssnapshots.

## 13. Nicht-historische Spielpräferenzen

Nicht-historische spielabhängige Einstellungen gehören nicht in `GameSession`,
Ergebnisdaten oder andere historische Tabellen.

Sie werden als Host-/Preference-Zustand pro Spiel gehalten, sofern keine
fachliche Historisierung ausdrücklich erforderlich ist.

Konkrete Preferences stehen im jeweiligen Spiel-Datenmodell oder Produktdokument.

## 14. Referenzdaten-Seeding

Feste Referenzdaten eines Spielmoduls dürfen beim Datenbankstart idempotent
ergänzt werden.

Allgemeine Regeln:

- fehlende feste Referenzdatensätze dürfen angelegt werden;
- stabile Referenz-IDs bleiben unverändert;
- vorhandener veränderlicher fachlicher Zustand wird beim Start nicht auf
  Defaultwerte zurückgesetzt;
- lokalisierte Built-in-Namen werden nicht allein für Anzeigezwecke in
  Referenztabellen gespiegelt.

Konkrete Referenzdaten und Seed-IDs gehören in das jeweilige Spiel-Datenmodell.

## 15. Allgemeine Persistenzregeln

Die Datenbank ergänzt Domain-Invarianten als zusätzliche Schutzlinie.

Wichtige relationale Regeln:

- `Player.Name`: eindeutiger Index mit `BGT_NAME`;
- `Group.Name`: eindeutiger Index mit `BGT_NAME`;
- `Game.Key`: eindeutiger Index;
- `GroupPlayer`: PK `(GroupId, PlayerId)`;
- `GroupPlayer`: eindeutiger Index `(GroupId, DefaultOrder)`;
- `GameSessionPlayer`: PK `(GameSessionId, PlayerId)`;
- `GameSessionPlayer`: eindeutiger Index `(GameSessionId, Order)`;
- positive Checks für `DefaultOrder`, `Order` und gesetztes `Placement`;
- Restrict für Referenzen auf langlebige Stammdaten;
- Cascade nur für echte Aggregate-/Composition-Kinder.

Datenbank-Constraints ersetzen Domain- und Use-Case-Regeln nicht.

Provider-, DbContext- und Initialisierungsarchitektur:
[ARCHITECTURE.md](ARCHITECTURE.md).

## 16. Spielmodule

Ein Spiel ergänzt das allgemeine Modell ausschließlich in seinem eigenen Modul:

```text
Core/Games/<GameName>/
Application/Games/<GameName>/
```

Das zugehörige Datenmodell steht unter:

```text
docs/Games/<GameName>/DATA_MODEL.md
```

Für Arche Nova:
[Games/ArkNova/DATA_MODEL.md](Games/ArkNova/DATA_MODEL.md).

Austauschmodell, Matching und Import-Referenzintegrität:
[IMPORT_EXPORT.md](IMPORT_EXPORT.md).
