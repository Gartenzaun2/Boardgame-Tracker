# Boardgame Tracker – Screens und Navigation

## 1. Dokumentgrenze

Diese Datei ist die kanonische Quelle für **spielübergreifende Screenstruktur und Navigation**.

Hier stehen nur:
- vorhandene Screens/Bereiche und ihre Reihenfolge;
- sichtbare Controls/Inhalte;
- konkrete verbindliche Layoutmaße;
- Navigation zu Screens, Dialogen und Systemflows;
- sichtbare Screenzustände sowie Back-/Modal-Navigation.

Nicht hierher gehören Fach-/Speicherregeln (`PRODUCT_SPEC.md`), allgemeine Darstellung/Accessibility (`UI_DESIGN_GUIDE.md`), Persistenz (`DATA_MODEL.md`), Importlogik (`IMPORT_EXPORT.md`) oder technische Schulden (`TODO.md`).

Spielspezifische Screens: `docs/Games/<GameName>/UI_WIREFRAME.md`.

## 2. Hauptnavigation

Permanente Tabs: **Spiele · Gruppen · Historie · Einstellungen**. Unter **Gruppen** bleibt der aktuelle Spielname sichtbar. Spiel-Tap setzt den Spielkontext und öffnet Gruppen; Gruppen-Tab öffnet die Übersicht im aktuellen Kontext. App-Start/Wiederherstellung folgen `PRODUCT_SPEC.md`.

Ein expliziter Tab-Tap öffnet immer den jeweiligen Root: `GamesPage`, `GroupListPage` im aktuellen Spielkontext, `HistoryPage` oder `AppSettingsPage`; tiefere Seiten des gewählten Tabs werden verworfen.

Reine Auswahllisten (auch Darstellung, Standardspiel und History-Filter) zeigen oben links den gemeinsamen Zurück-/Exit-IconButton und keinen unteren Schließen-Textbutton.

## 3. Gruppen

### Übersicht

Reihenfolge: Titel **Gruppen** → aktueller Spielname → `HeaderDivider` → Filter **Temporär** und **Archiviert** nebeneinander → Gruppenliste → **Neue Gruppe**.

Eine Gruppenkarte zeigt Name, Mitglieder, letzte Nutzung im aktuellen Spiel, optional das Aktiv-Icon für offene Partie/Wertung und rechts eine eigene Bearbeiten-Touchfläche. Karten-Tap öffnet offene Partie/Wertung oder die Vorbereitung des Spielmoduls; Bearbeiten öffnet nur das Gruppenformular. Kein allgemeiner Gruppendetail-Screen.

### Anlegen

Header: Zurück · **Gruppe anlegen** · Info · rechts **Speichern**.

Inhaltsreihenfolge:
1. Gruppenname;
2. Temporär-Switch;
3. Gruppenmitglieder;
4. **Vorhandenen Spieler hinzufügen**;
5. neuer Spieler mit Name, Gast-Switch und **Spieler hinzufügen**.

Mitglieder erscheinen als Kacheln in aktueller Reihenfolge mit Hoch/Runter/Entfernen; bei `PlayerColor` zusätzlich Palette/Farbstatus. Drag & Drop ist nicht Teil des aktuellen Screens.

**Vorhandenen Spieler hinzufügen** öffnet `SelectionDialog`; Tap fügt den Eintrag dem Entwurf hinzu und schließt. Sind keine Spieler verfügbar, ersetzt ein Leerzustand den Button.

### Bearbeiten

Gleiche Formularsprache wie beim Anlegen. Sichtbar: Name, Archiv-Switch, bei temporärer Gruppe **Permanent machen**, Mitglieder mit Reihenfolge und optionaler Farbe, **Speichern**. Keine Spieler-Hinzufügen-/Entfernen-Aktion.

### Gruppenfarbe

Palette öffnet `SelectionDialog` mit Titel **Farbe für <Spiel> festlegen**, Spielername, erlaubten Farben und **Keine Farbe**. Aktuelle Auswahl ist sichtbar markiert. Ohne Farbunterstützung fehlen Palette und Farbstatus.

## 4. Historie

Oben: Filterchips **Spiel** und **Gruppe**. Sie dürfen bei Platzmangel umbrechen;
eine gesetzte Auswahl ist zusätzlich durch die gemeinsame sichtbare Markierung
gekennzeichnet. Darunter nach lokalem Kalendermonat gruppierte Partien;
Monatsüberschriften sind einfache, nicht-sticky Texte.

Partiekarte mit ungefähr drei umbrechenden Zeilen:
```text
<Spiel> · <Dauer> · <Datum>
<Gruppe>
1. Anna · 1. Ben · 3. Chris
```

Tap öffnet die Ergebnisansicht des zuständigen Spielmoduls. Die Liste besitzt
einen eigenen Leer-/Fehlerzustand; Busy blockiert den betroffenen Screen gemäß
`UI_DESIGN_GUIDE.md`.

Unter der scrollenden Liste liegt fest: **Importieren** (Secondary) und **N Partien exportieren** (Primary); Export ist bei `N = 0` deaktiviert.

Export öffnet den App-Dialog **Exportieren** mit **Speichern · Teilen · Abbrechen**. Speichern öffnet den System-Dokumentflow, Teilen den Share-Sheet.

Import öffnet die System-Dateiauswahl. Falls nötig folgen App-Auswahlen für
Zuordnungen, insbesondere **Spieler zuordnen und Gruppen zusammenführen** oder
**Gruppen trennen**. Danach erscheint die Importvorschau mit Zusammenfassung,
neuen Gruppennamen sowie **Jetzt importieren · Abbrechen**.

Import/Export besitzt keinen zweiten Einstieg unter Einstellungen. Fachliche
Importregeln stehen ausschließlich in `IMPORT_EXPORT.md`.

## 5. Spieler

### Übersicht

Einstieg über **Einstellungen → Spieler**. Header: Zurück · **Spieler** · Info; darunter **Gäste anzeigen** und **Archivierte anzeigen**, initial beide aus.

Spielerkarte: Name + Bearbeiten-Icon, darunter **Erstellt <Datum>**, optionale Gast-/Archiv-Chips und eine eigene Secondary-Aktion **Zuletzt gespielt <…>**. Ohne Partie steht dort **Noch keine Partie** nicht interaktiv. Nur das Bearbeiten-Icon öffnet den Editor.

Neben **Zuletzt gespielt** erscheint bei `Running` und `Scoring` dasselbe Aktiv-Icon wie in der Gruppenübersicht, bei `Completed` keines. Das Icon ist eine Statusanzeige ohne eigene Aktion. Ein abgeschlossenes Ergebnis wird über der Spielerübersicht geöffnet; Zurück führt dorthin zurück, auch nach einer Korrektur über den Ergebnis-Screen.

### Bearbeiten

Header **Spieler bearbeiten**; Felder: Name, Archiv-Switch; unten **Speichern**. Kein Gast-Control.

## 6. Einstellungen

Reihenfolge: **Darstellung · Standardspiel · Spieler · registrierte Spieleinstellungen · Über / Rechtliches**. Darstellung und Standardspiel öffnen App-Auswahldialoge. Allgemeiner Code zeigt Spieleinstellungen nur als registrierte Einstiege.

## 7. Open-Source-Lizenzen

Übersicht: Header mit Zurück/Divider, direkt darunter der erforderliche IJG-Hinweis, dann eine Liste aus **Komponentenname + Version** und **Lizenzbezeichnung**. Tap öffnet die Detailseite.

Detail: Zurück/Divider, Version, Lizenzname, optional SPDX, Attribution/Copyright und scrollbarer lokaler Lizenz-/NOTICE-Text. Kein Browser/WebView und keine Zustimmung/Checkbox. Fehlendes lokales Asset zeigt Fehler + **Wiederholen**.

## 8. Quellen

Produktverhalten: `docs/PRODUCT_SPEC.md` · UI-Regeln: `docs/UI_DESIGN_GUIDE.md` · Spielscreens: `docs/Games/<GameName>/UI_WIREFRAME.md` · Import/Export: `docs/IMPORT_EXPORT.md`.
