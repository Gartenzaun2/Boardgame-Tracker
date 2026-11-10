# Arche Nova – Import / Export

## 1. Geltungsbereich

Diese Datei ergänzt
[docs/IMPORT_EXPORT.md](../../IMPORT_EXPORT.md) ausschließlich um den
**Arche-Nova-spezifischen Transfervertrag**.

Allgemeines Envelope, Player-/Group-Matching, Resolve, Preview, Commit,
Atomarität und native Ausgabeziele werden dort definiert.

Aktueller Exportvertrag:

```text
BGT formatVersion = 1
Arche Nova schemaVersion = 3
```

Arche Nova importiert Schema 3 direkt und unterstützt bestehende Exporte mit
Schema 2 durch eine explizite, verlustfreie Übernahme des tatsächlich gespielten
Zooplans in das aktuelle Modell (siehe Abschnitt 4). Das frühere Arche-Nova-Schema
1 bleibt abgelehnt. Die allgemeine BGT-Formatversion bleibt unverändert.

## 2. `Game.data`

```text
specialCards: [{
  id,
  userName,
  symbol1,
  symbol2,
  symbol3,
  isArchived
}]
```

`userName` ist Pflichtfeld mit nullable Wert:

- `null`: keine Nutzerressource;
- nicht-null: Nutzername dieser Karte.

Bei einer selbst erstellten Karte ist ein Nutzername erforderlich.

Mitgelieferte Namen und Sprachvarianten aus `content.json` werden nicht exportiert.
Ein importierter Name wird ausschließlich als Nutzerressource behandelt und niemals
als Änderung einer mitgelieferten Übersetzung.

Die lokale Materialsprach-Preference wird nicht übertragen.

## 3. `GameSession.data`

```text
players: [{
  playerId,
  color,
  selectedZooPlanNumber,
  attractiveness,
  conservation,
  reputation,
  specialCardIds
}]
```

`Attractiveness` und `Conservation` sind bei transferierten `Completed`-Sessions
vorhanden; `Reputation` bleibt nullable.

Farben und Kartensymbole verwenden ihre numerischen Domain-Enumwerte.

Nullable Werte werden explizit als `null` geschrieben.

Berechnete Punkte werden nicht transportiert.

Arche-Nova-Gruppendefaults wie `ArkNovaGroupPlayer.DefaultColor` sind **nicht**
Teil von Format v1. Beim Match bleibt der lokale Default erhalten; bei einer neu
importierten Gruppe entsteht dadurch kein importierter Arche-Nova-Farbdefault.

## 4. Validierung

Zusätzlich zur allgemeinen `.bgt`-Validierung gilt:

### Spieler und Zoopläne

- höchstens vier Spieler;
- `GameSession.data.players` enthält exakt dieselbe Player-Menge wie die allgemeine
  Session;
- Farben sind für Arche Nova gültig und innerhalb der Session eindeutig;
- `selectedZooPlanNumber` liegt im gültigen Katalog `1..17`;
- mehrere Spieler dürfen denselben `selectedZooPlanNumber` verwenden;
- das Schema 3 enthält keine ausgelosten, nicht gewählten Zooplanoptionen.

### Übernahme aus Schema 2

Schema-2-Dateien verwenden weiterhin die damals vorgeschriebenen Felder
`zooPlanOption1Number`, `zooPlanOption2Number` und `selectedZooPlanNumber`.
Für diese Eingabe gilt die bisherige strenge Validierung: gültige verschiedene
Optionen aus `1..14`, keine doppelt angebotenen Optionen in der Session und
Auswahl ausschließlich aus den beiden Optionen. Nach erfolgreicher Validierung
wird nur `selectedZooPlanNumber` in das aktuelle Sessionmodell übernommen; die
zwei historischen Angebote werden verworfen. Der Import schreibt anschließend
keine Legacy-Optionsfelder in die Datenbank. Export erfolgt immer mit Schema 3.

### Wertung und Placement

Die Domainvalidierung gilt unverändert:

```text
Attractiveness 0..113
Conservation   0..41
Reputation     null oder 1..15
```

Die importierten Placements müssen zu den aus den importierten Rohwerten
berechneten Score-/Gleichstandsblöcken passen.

### Spezialkarten

- Karten-IDs sind nichtleer und innerhalb der Referenzmenge eindeutig;
- Symbole müssen gültige `ArkNovaCardSymbol`-Werte bilden;
- null bis drei geordnete Symbole, keine Lücken;
- Wiederholungen sind erlaubt;
- Nutzername, wenn vorhanden: nach Domainregel 1–120 Zeichen;
- jede nicht mitgelieferte Karte benötigt einen Nutzernamen;
- `specialCardIds` je Spieler sind eindeutig;
- jede referenzierte Karten-ID muss in `Game.data.specialCards` vorhanden sein.

## 5. Export der Spezialkarten

Exportiert werden ausschließlich Spezialkarten, die von den tatsächlich
ausgewählten Sessions referenziert werden.

Für jede Karte werden transportiert:

- stabile GUID;
- aktueller Nutzername, falls vorhanden;
- Symbol1–3;
- Archivstatus.

Ein Built-in-Kartendatensatz ist selbst keine Nutzerressource. Ohne lokalen
Nutzernamen wird daher bei Built-in-Karten `userName: null` exportiert.

Nicht referenzierte Spezialkarten werden nicht exportiert.

## 6. Import und Matching der Spezialkarten

Es gibt **kein Namensmatching** für Spezialkarten.

Priorität:

```text
gleiche GUID → lokale Karte
sonst → neue Karte mit importierter GUID
```

Nur Karten, die von einer tatsächlich neu importierten Session benötigt werden,
werden verarbeitet.

### Bereits vorhandene Karte

Bei gleicher GUID gewinnen lokale Kartendaten:

- Symbole werden nicht überschrieben;
- Archivstatus wird nicht überschrieben.

Für Nutzernamen gilt:

- vorhandener lokaler Nutzername gewinnt;
- existiert noch keine lokale Nutzerressource und der Import enthält `userName`,
  wird dieser Nutzername angelegt;
- das gilt auch für eine bereits lokal vorhandene Built-in-Karte.

### Neue Karte

Eine neue Karte übernimmt:

- importierte GUID;
- Symbole;
- Archivstatus;
- importierten Nutzernamen.

Karte und Nutzername werden innerhalb derselben allgemeinen Importtransaktion
gespeichert.

## 7. Snapshot und Preview

Der Arche-Nova-Modulsnapshot berücksichtigt:

- Spezialkarten-ID;
- Symbolwerte;
- Archivstatus;
- persistierte Spezialkarten-Nutzernamen.

Ändert sich dieser Zustand zwischen Analyze und Commit, schlägt der allgemeine
Snapshotvergleich fehl und eine erneute Analyse ist erforderlich.

Preview zählt nur die für neue Sessions benötigten Spezialkarten als:

```text
vorhanden
neu
```

## 8. Nicht übertragene Arche-Nova-Daten

Nicht Bestandteil des aktuellen Transfers:

- Materialsprache;
- Built-in-Contentnamen/-übersetzungen;
- Gruppendefaultfarben;
- ausgeloste, nicht gewählte Zooplanoptionen;
- berechnete Punkte;
- Foto-/CV-Daten oder Confidence;
- Bilder;
- nicht referenzierte Spezialkarten.

Fachliche Dateninvarianten:
[DATA_MODEL.md](DATA_MODEL.md).

Produktverhalten:
[PRODUCT_SPEC.md](PRODUCT_SPEC.md).

## Separater Bilderkennungsdaten-Export

Der Ark-Nova-Dataset-Export ist unabhängig vom `.bgt`-History-Transfer und dessen
Versionsvertrag. Die ausdrückliche Exportaktion erzeugt eine ZIP-Kopie und öffnet
das System-Share-Sheet zur vom Nutzer gewählten Ausgabe. Lokale Samples bleiben
erhalten. Auch verwertbar erkannte Samples ohne Ground Truth werden exportiert.

ZIP-Struktur: `dataset.json` und `images/<sample-id>.jpg`.
Das JSON-Manifest verwendet camelCase und aktuell `datasetVersion: 1` sowie
`schemaVersion: 1`. `samples` enthält je Eintrag:

- `sampleId`: eigene zufällige Dataset-GUID;
- `game`: `ark-nova`;
- `photoSource`: `camera` oder `library`;
- `appVersion`, `recognitionVersion`, `baselineVersion`;
- `prediction`: unveränderter Recognition-Snapshot mit `registration`, `colors`
  (Werte/Confidence/Evidence je Track) und den bereits in C# vorhandenen
  `evidence`-Feldbeobachtungen;
- `groundTruth`: null oder finale Rohwerte je `color`, `attractiveness`,
  `conservation`, `reputation`, ausschließlich aus dem regulären Partieabschluss;
- `reviewStatus`: `pending`, `confirmed` oder `cancelled`;
- `imagePath`: relativer JPEG-Pfad innerhalb des ZIPs.

Nicht-endliche technische Messwerte können als JSON-Strings `NaN`, `Infinity`
oder `-Infinity` dargestellt sein. Es werden ausschließlich neu kodierte,
metadatenfreie Dataset-Bildkopien exportiert. Spieler-, Gruppen- und Sessionnamen,
produktive interne IDs, lokale Zuordnungen, Bildhashes und Originalmetadaten sind
kein Bestandteil des öffentlichen Manifests.
