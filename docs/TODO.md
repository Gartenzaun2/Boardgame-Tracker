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

## Arche Nova – Zooplanwahl in der Partievorbereitung

### Manuelle Zooplanwahl und Session-/Transferschema auf den Sollzustand bringen

**Status:** offen – vereinbarter Sollzustand ist in den Arche-Nova-Spezifikationen dokumentiert, Umsetzung steht aus.

**Ist:** Der bestehende Startpfad speichert je Teilnehmer zwei angebotene Zoopläne
und einen gewählten Plan; manuelle Einzelzuweisung, doppelte Pläne und die drei
Sonderpläne sind im aktuellen Persistenzvertrag nicht vorgesehen.

**Ziel:** Manuelle Planwahl vor der ersten Auslosung, danach ausschließlich die
bestehende Zwei-Optionen-Auslosung. Turnier/0/A ausschließlich im manuellen
Katalog mit den freigegebenen, unveränderten Grafiken. Die Session persistiert nur
den gewählten Plan; Datenbankmigration erhält vorhandene historische Wahlen,
Import/Export verwendet Schema 3 und übernimmt bisherige Schema-2-Exporte
kompatibel. Betroffene Startvalidierung, Content-Katalog, UI, Transfer und Tests
werden gezielt angepasst. Details liegen in den kanonischen
`docs/Games/ArkNova/{PRODUCT_SPEC,DATA_MODEL,UI_DESIGN_GUIDE,UI_WIREFRAME,IMPORT_EXPORT}.md`.

## Arche Nova – CV-Datensammlung

### Default vor dem ersten 1.0-Build bewusst umstellen

**Status:** vor 1.0 erforderlich

Der feste Default der lokalen Bilderkennungsdatensammlung ist in der internen
Pre-1.0-Testphase aktiv. Unmittelbar vor dem ersten 1.0-Build und vor regulärer
externer Verteilung muss dieser Default bewusst auf deaktiviert geändert werden.
Keine Laufzeitentscheidung anhand der App-Version einführen; bestehende ausdrücklich
gespeicherte Nutzerauswahlen erhalten.

## Architektur

### Ark-Nova-Buildverdrahtung in der allgemeinen App-Projektdatei

**Status:** beobachten / bei weiterem Wachstum auslagern

`src/BoardgameTracker.App/BoardgameTracker.App.csproj` bindet Ark-Nova-native Assets,
Buildverzeichnisse und native Bibliotheken direkt ein.

**Ziel:** Die App-Projektdatei darf als Hostintegrationspunkt Spielassets paketieren.
Wenn die spielabhängige Buildkonfiguration weiter wächst, soll sie in eine
modul-eigene importierte Builddatei unter `Games/ArkNova/` ausgelagert werden,
damit die allgemeine Projektdatei übersichtlich bleibt.
