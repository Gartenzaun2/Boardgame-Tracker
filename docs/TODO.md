# Boardgame Tracker – Offene technische Schulden

Diese Datei hält **konkrete bekannte Abweichungen oder Follow-ups** fest, die nicht
Teil der gewünschten Architektur oder Produktanforderung sind.

Sie ist kein allgemeiner Backlog und wird nicht bei normalen Aufgaben vorsorglich
gelesen. Neue Einträge nur aufnehmen, wenn ein Problem bewusst offen bleibt und
später gezielt bearbeitet werden soll. Erledigte Einträge entfernen statt eine
Historie aufzubauen; Historie gehört in Git/CHANGELOG, wenn relevant.

## Plattformnachweise

### Android ARM64: nativer Gerätelauf

**Status:** offen

**Ist:** Der native Mobile-CV-Pfad ist fachlich freigegeben. Ein tatsächlicher
Android-API-30-x64-App-/Interop-/Review-/Persistenzlauf ist vorhanden. Der
vollständige iOS-Simulator-Build-/Runtime-/CV-Ergebnisnachweis ist ebenfalls
erbracht und fachlich akzeptiert. Ein echter Android-ARM64-Gerätelauf der nativen
Integration fehlt weiterhin.

**Ziel:** Den bestehenden produktiven Android-ARM64-Pfad einmal auf einem echten
geeigneten Gerät technisch ausführen und bestätigen, dass App-/Interop-/native
Engine-Lauf ohne plattformspezifischen technischen Fehler funktioniert. Dies ist
kein neues CV-Forschungs- oder Produktfreigabegate und darf keine Optimierung oder
Parameteränderung auslösen.

## Arche Nova – CV-Datensammlung

### Lokaler Foto-/Ground-Truth-Datensatz für spätere Bilderkennungsverbesserung

**Status:** Konzept festgelegt, Umsetzung offen

**Motivation:** Die produktive Arche-Nova-Fotoerkennung bleibt auf ihrer aktuell
freigegebenen nativen Baseline. Für eine mögliche spätere neue Forschungs- oder
Optimierungsrunde sollen reale Fotoaufnahmen aus dem normalen assistierten
Wertungsworkflow lokal gesammelt und mit dem damaligen Erkennungsergebnis sowie
später bekannten korrekten Werten als Ground Truth verknüpft werden. Ziel ist ein
exportierbarer Datensatz, mit dem Fehlerfälle reproduziert, gruppiert und gegen
zukünftige Erkennungsvarianten ausgewertet werden können.

Die Datensammlung selbst ändert die produktive Erkennungslogik nicht und ist kein
neues Freigabegate für die aktuelle CV-Baseline.

**Einstieg und Bedienung:**

- Die Funktion gehört ausschließlich zu Arche Nova.
- Einstieg unter **Arche-Nova-Einstellungen → Debug → Bilderkennungsdaten**.
- Der Debug-Bereich liegt innerhalb der Arche-Nova-Einstellungen, nicht in den
  allgemeinen App-Einstellungen.
- Bis unmittelbar vor dem ersten `1.0`-Build ist die Datensammlung für die interne
  Entwicklungs-/Eigen-Testphase standardmäßig aktiviert.
- Spätestens für den ersten `1.0`-Build muss der Default auf deaktiviert wechseln,
  bevor die App extern regulär getestet oder verteilt wird.
- Der Screen zeigt mindestens, ob die Sammlung aktiv ist, Anzahl gespeicherter
  Aufnahmen und den belegten Speicher sowie Aktionen für Export und vollständiges
  Löschen.
- Beim Deaktivieren mit bereits vorhandenen Daten folgt eine eigene Auswahl mit
  **Behalten** und **Löschen**. Abbruch erfolgt ausschließlich über den gemeinsamen
  Zurück-Pfeil im Header; es gibt keinen zusätzlichen unteren
  **Abbrechen**-Button.
- Export löscht keine lokalen Daten. Löschen ist eine getrennte bewusste Aktion.

**Zu sammelnde Foto-Versuche:**

Jedes tatsächlich aufgenommene oder ausgewählte Foto kann ein eigenes Dataset-Sample
bilden, solange die Datensammlung aktiv ist. Das gilt ausdrücklich auch bei mehreren
Foto-Versuchen innerhalb derselben Wertung.

Mehrere Versuche werden nicht dedupliziert oder auf den zuletzt bestätigten Versuch
reduziert. Gerade ein Ablauf wie

```text
Foto 1 → Review verworfen
Foto 2 → Registrierung fehlgeschlagen
Foto 3 → korrigiert und bestätigt
```

ist für spätere Fehleranalyse relevant und soll als drei getrennte Samples erhalten
bleiben.

Pro Sample wird der Ausgang des damaligen Versuchs festgehalten, mindestens
sinngemäß als:

```text
confirmed
cancelled
registrationFailed
```

Ein abgebrochener Review bedeutet nur, dass dieser konkrete Vorschlag nicht
übernommen wurde. Daraus darf nicht abgeleitet werden, dass jeder einzelne erkannte
Wert falsch war.

**Gespeichertes Bild:**

- Der bestehende produktive Native-/C++-Erkennungspfad soll für diese Funktion
  zunächst nicht verändert werden.
- Die Originalbytes für die produktive Analyse werden weiterhin unverändert an den
  bestehenden Erkennungspfad gegeben.
- Parallel erzeugt der App-/Managed-Pfad eine eigene Dataset-Bildkopie.
- Diese Kopie wird orientierungsbereinigt, Metadaten/EXIF einschließlich möglicher
  Standortdaten werden entfernt und das Bild wird platzsparend verkleinert.
- Aktuelles Ziel für die Dataset-Kopie: Seitenverhältnis erhalten und die lange
  Kante auf höchstens etwa `1600 px` begrenzen, damit für spätere
  Registrierungs-/Erkennungsversuche noch ausreichend Bildinformation erhalten
  bleibt, ohne vollständige Kameraoriginale dauerhaft zu speichern.
- Das Dataset-Bild ist unabhängig vom Eingabebild der produktiven Erkennung; seine
  Erzeugung darf die aktuelle CV-Baseline nicht verändern.
- Fehler beim zusätzlichen Dataset-Speichern dürfen den normalen Fotoerkennungs-,
  Review- oder Wertungsworkflow nicht fehlschlagen lassen.

**Erkennungsergebnis / Prediction:**

Das ursprüngliche Ergebnis des konkreten Analyseversuchs wird beim Sample
unveränderlich eingefroren. Spätere Benutzeränderungen oder historische Korrekturen
dürfen die damalige Prediction nicht überschreiben.

Zu exportieren sind mindestens:

- Registrierung erfolgreich/fehlgeschlagen;
- erkannter Wert je Farbe und Wertungsfeld, soweit vorhanden;
- damalige Confidence-/Sicherheitsinformation;
- die bereits ohne Änderung des C++-Codes in der C#-Schnittstelle vorhandenen
  technischen Diagnosewerte.

Soweit bereits durch den aktuellen Native-Wrapper verfügbar, gehören dazu
insbesondere Registrierungsmetriken wie Matches, Inliers, Reprojection-P95,
Minimum Visibility, Spread und Homographie sowie vorhandene feldbezogene
Evidence-/Visibility-Werte.

Für diese Datensammlung wird der C++-Vertrag nicht allein deshalb erweitert, um
zusätzliche interne Debugwerte herauszugeben. Falls später für eine konkrete neue
Forschungsfrage ein bisher nur nativ vorhandener Messwert benötigt wird, ist das
eine eigene bewusste Erweiterung.

**Ground Truth:**

Der Datensatz soll ausdrücklich den Vergleich zwischen damaliger Erkennung und
korrektem Wert ermöglichen.

Ground Truth umfasst je beteiligter Spielerfarbe die tatsächlich korrekten
Arche-Nova-Wertungswerte:

- Attraktivität;
- Artenschutz;
- optional Ruf, sofern für die betreffende Wertung relevant.

Spielername, Gruppenname oder andere für die CV-Auswertung unnötige persönliche
Bezeichnungen sollen nicht Bestandteil des exportierten Forschungsdatensatzes sein.

Ground Truth kann auf zwei Wegen entstehen:

1. Ein Foto-Review wird korrigiert und bestätigt. Die bestätigten Werte sind für
   diesen Versuch unmittelbar Ground Truth.
2. Ein Review wurde abgebrochen oder die Registrierung/Erkennung ist fehlgeschlagen.
   Sobald für dieselbe laufende Wertung später zuverlässige manuelle/finale Werte
   vorliegen, können diese als Ground Truth den zuvor aufgenommenen Samples derselben
   Wertung zugeordnet werden.

Mehrere Foto-Versuche derselben Wertung dürfen damit dieselbe spätere Ground Truth
erhalten, behalten aber jeweils ihre eigene ursprüngliche Prediction und ihren
eigenen Review-/Fehlerausgang.

Die Herkunft der Ground Truth wird im Datensatz kenntlich gemacht, mindestens
sinngemäß als:

```text
photoReview
manualScoring
```

Eine spätere historische Korrektur einer bereits abgeschlossenen Partie verändert
die zu einem früheren Foto-Sample eingefrorene Ground Truth nicht automatisch. Der
Datensatz soll reproduzierbar dokumentieren, was im damaligen Erfassungsablauf als
korrekt bestätigt wurde.

Samples, für die nie zuverlässige Ground Truth verfügbar wird, dürfen als
unvollständige Samples erhalten bleiben; Prediction und Fehler-/Reviewausgang
bleiben für Registrierungs- und Fehleranalyse trotzdem nutzbar.

**Lokale Zuordnung und Datenschutz:**

- Zur späteren Ground-Truth-Zuordnung darf ein Sample intern mit der zugehörigen
  laufenden `GameSession` verbunden werden.
- Der Export soll dafür keine produktive `GameSessionId`, Spieler-ID oder andere
  unnötige App-Identität benötigen; Export-Samples erhalten eine eigene
  Dataset-/Sample-ID.
- Es findet kein automatischer Upload statt. Die Daten bleiben lokal, bis der
  Nutzer sie ausdrücklich exportiert oder löscht.
- Bildmetadaten werden vor dauerhafter Dataset-Speicherung entfernt.

**Separater Dataset-Export:**

Der Forschungsdatensatz ist ausdrücklich **nicht** Teil des normalen `.bgt`-
History-Transfers. `.bgt` bleibt unverändert für abgeschlossene Partien und erhält
keine Bildanhänge.

Der Debug-Screen bietet einen eigenen Dataset-Export als versioniertes ZIP an,
konzeptionell etwa:

```text
arknova-cv-dataset.zip
├─ dataset.json
└─ images/
   ├─ <sample-id>.jpg
   ├─ <sample-id>.jpg
   └─ ...
```

Das Manifest enthält mindestens:

- Dataset-Formatversion;
- App-Version;
- Erkennungs-/Pipeline-Version bzw. eindeutige Baseline-Kennung;
- Sample-ID;
- Bilddatei;
- Fotoquelle `camera` / `library`;
- Review-/Analyseausgang;
- unveränderte damalige Prediction;
- vorhandene technische Diagnosewerte;
- Ground Truth, falls vorhanden;
- Herkunft der Ground Truth.

Die Formatversion muss unabhängig von der normalen `.bgt`-Versionierung sein.

**Persistenzrichtung:**

Bilder sollen nicht als große SQLite-BLOBs im normalen Partiedatenmodell landen.
Bevorzugt werden app-interne Dataset-Dateien plus kleine persistierte
Metadaten/Referenzen, sodass Größenanzeige, ZIP-Export und vollständiges Löschen
kontrolliert möglich bleiben. Die konkrete technische Persistenzform wird erst bei
Umsetzung gegen die dann aktuelle Architektur festgelegt.

**Noch vor Umsetzung festzulegen:**

- verbindliche Speicher-/Retention-Regel für die interne Pre-1.0-Phase:
  unbegrenzt bis manueller Löschung oder ein definiertes lokales Größenlimit;
- exaktes versioniertes `dataset.json`-Schema und Dateinamenskonvention;
- Verhalten von Samples ohne Ground Truth beim Export, sofern nicht einfach alle
  vorhandenen Samples exportiert werden;
- genauer Zeitpunkt, zu dem manuell erfasste Scoring-Werte als ausreichend
  zuverlässig gelten, um sie als Ground Truth an frühere Foto-Versuche anzuhängen.

Bei Umsetzung werden die dauerhaften Produkt-, UI-, Persistenz- und Exportregeln in
die jeweils zuständigen kanonischen Arche-Nova-Dokumente überführt. Dieser TODO wird
nach vollständiger Umsetzung entfernt. `CV_STATUS.md` bleibt bis zu einer tatsächlich
gestarteten neuen CV-Forschungsrunde beim aktuellen Freeze und wird durch die reine
Datensammlung nicht auf „Forschung aktiv“ geändert.

## iOS-Navigation

### Runtime-Bestätigung der korrigierten Navigationspfade

**Status:** gezielte manuelle Verifikation ausstehend

Die Implementierung erhält den tatsächlichen GameSession-Aufrufer, behandelt
geleerte Shell-Query-Parameter beim Zurückkehren und bindet iOS-Tab-Taps ohne
eigenen ShellItemRenderer an die bestehende Root-Navigation an. Nach unmittelbarer
ausdrücklicher Entwicklerfreigabe sind auf iOS noch die bisherigen Repros zu
bestätigen: Haupttab-Tap/Retap nach tiefer Navigation, Zurück aus historischer
Korrektur sowie bestätigtes Verwerfen aus Gruppen, Historie und Spielerübersicht.
Insbesondere App-Ende/Black Screen und die Rückkehr zum richtigen Screen prüfen.
Diese Interaktionsprüfung geht über den minimalen Startup-Smoke-Test hinaus und
benötigt einen entsprechend ausdrücklichen Auftrag. Kein automatischer Gerätelauf.

## Architektur

### Spielmodulgrenze in allgemeinen App-Features

**Status:** offen

`src/BoardgameTracker.App/Features/Games/ListGames/GamesPage.xaml.cs` kennt aktuell
`ArkNovaGame` direkt und besitzt einen konkreten `OnArkNovaTapped`-Pfad.

**Ziel:** Das allgemeine Games-Feature darf keine konkrete Spielabhängigkeit
besitzen. Spielkatalog/Navigation sollen über eine allgemeine Registrierung oder
ein kleines allgemeines Read Model erfolgen; die konkrete Ark-Nova-Implementierung
bleibt im Spielmodul.

### Spielabhängige Einstellungen im allgemeinen Settings-Feature

**Status:** offen

`src/BoardgameTracker.App/Features/Settings/AppSettings/AppSettingsPage.xaml.cs`
kennt aktuell `ArkNovaGame` und `ArkNovaSettingsPage` direkt und baut die
Default-Spielauswahl mit Ark Nova hart auf.

**Ziel:** Allgemeine App-Einstellungen dürfen keine konkreten Spieltypen oder
Spielseiten kennen. Registrierte Spiele und deren Settings-Einstieg sollen über
eine kleine allgemeine Modulgrenze bereitgestellt werden.

### Ark-Nova-Buildverdrahtung in der allgemeinen App-Projektdatei

**Status:** beobachten / bei weiterem Wachstum auslagern

`src/BoardgameTracker.App/BoardgameTracker.App.csproj` bindet Ark-Nova-native Assets,
Buildverzeichnisse und native Bibliotheken direkt ein.

**Ziel:** Die App-Projektdatei darf als Hostintegrationspunkt Spielassets paketieren.
Wenn die spielabhängige Buildkonfiguration weiter wächst, soll sie in eine
modul-eigene importierte Builddatei unter `Games/ArkNova/` ausgelagert werden,
damit die allgemeine Projektdatei übersichtlich bleibt.

### Globaler Architekturtest für Spielmodulgrenzen

**Status:** offen

Die vorhandenen Architekturtests schützen bereits einzelne Grenzen, insbesondere
allgemeine Gruppenverträge und allgemeine Group-UI vor konkreten Spieltypen. Ein
repositoryweiter Schutz aller allgemeinen App-/Application-Bereiche vor
`Games.<ConcreteGame>`-Abhängigkeiten fehlt jedoch noch.

**Ziel:** Einen kleinen Architekturtest ergänzen, der die allgemeine
`Games/<GameName>/`-Ownership projektweit schützt und dabei erlaubte
Integrationspunkte wie Composition Root, Host-Projektdatei und technisch notwendige
Partial-Type-Erweiterungen ausdrücklich berücksichtigt.
