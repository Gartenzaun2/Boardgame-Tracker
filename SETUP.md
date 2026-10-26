# Boardgame Tracker v0.6.4 – Setup

## Upgrade von v0.6.3.1

1.  v0.6.4 über das vorhandene Projekt kopieren.
2.  Cleanup-/Hinweisscript ausführen:

``` cmd
tools\upgrade-from-v0631.cmd
```

3.  Wenn dein Projekt bereits eine bestehende `InitialCreate`-Migration
    besitzt, neue Migration erzeugen:

``` cmd
tools\create-v064-migration.cmd
```

4.  Clean Solution.
5.  Rebuild Solution.
6.  Android-App starten. Die Migration wird beim Startup über
    `MigrateAsync` angewendet.

## Erwartete Schemaänderung

Die Tabelle `Groups` erhält eine nicht-nullbare Bool-Spalte `IsArchived`
mit dem für bestehende Datensätze effektiven Standardwert `false`.

## Funktionstest

- Gruppenübersicht: normale permanente, nicht archivierte Gruppen sind
  sichtbar.
- `Temporäre einblenden` einschalten: temporäre Gruppen erscheinen.
- `Archivierte einblenden` einschalten: archivierte Gruppen erscheinen.
- Temporäre Gruppe bearbeiten: `Als permanente Gruppe übernehmen` ist
  verfügbar.
- Permanente Gruppe bearbeiten: keine Möglichkeit, sie temporär zu
  machen.
- Gruppe archivieren und speichern: bei deaktiviertem Archivfilter
  verschwindet sie.
- Archivfilter einschalten, Gruppe bearbeiten und Archivierung
  ausschalten: Gruppe ist wieder normal sichtbar.
- `Speichern` ist eindeutig grün.
- Gruppenflächen und `Neue Gruppe` verwenden `PaleGoldenrod`.

## Hinweis zur Paketprüfung

In der Paket-Erstellungsumgebung steht kein .NET SDK zur Verfügung.
Deshalb konnte kein echter `dotnet build` und keine
EF-Migrationserzeugung ausgeführt werden. XAML/XML/RESX und
Ressourcenreferenzen wurden statisch geprüft.
