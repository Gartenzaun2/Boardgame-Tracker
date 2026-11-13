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

## 2. Hauptnavigation und gemeinsamer Spielkontext

**Direkte Zielanzeige:** Jeder Seitenwechsel zeigt unmittelbar die beabsichtigte Zielseite, ohne sichtbare Zwischenstationen. Zusammengehörige Weiterleitungen werden als ein Shell-Ziel geöffnet; technische Stackseiten dürfen nicht zwischenrendern. Auch ein erneuter Haupttab-Tap verwirft den Unterstack und den bisherigen Session-Rückkontext. Die logische Kette Spieleübersicht → Gruppenübersicht → Session sowie tatsächliche Aufrufer (insbesondere Historie) bleiben als Rückziele erhalten.

Permanente Tabs: **Spielen · Historie · Mitspieler · Einstellungen**.

Ein expliziter Tab-Tap öffnet stets den Root des gewählten Hauptbereichs; tiefere Seiten werden verworfen.

**Spielen** öffnet:
- bei offenen Sessions/Wertungen für ausschließlich ein Spiel direkt dessen `GroupListPage` – auch bei mehreren offenen Gruppen desselben Spiels;
- sonst die `GamesPage`.

`GroupListPage` hat immer `GamesPage` als direktes Rückziel, auch nach einem Shortcut.

Der App-Start darf eine zuletzt explizit geöffnete offene Session/Wertung wiederherstellen; ein späterer expliziter Tab-Tap folgt trotzdem der Root-Regel.

Öffnet eine bestehende GameSession aus Gruppenübersicht oder Historie, bleibt der aufrufende Screen als Rückziel erhalten. Interne Wechsel zwischen laufender Partie, Wertung, Ergebnis und Korrektur verändern dieses Rückziel nicht. Header-/System-Zurück und erfolgreiches Verwerfen führen dorthin; ein Haupttab-Tap hat Vorrang.

### Gemeinsame Seitenheader

Es gibt zwei Varianten **einer gemeinsamen Header-Komponente**:

```text
Hauptbereich: [Titel]                              [Icons]
              HeaderDivider

Unterseite:   [Zurück] [Titel]                      [Icons]
                         [Spielbadge] [erste Unterüberschrift]
                         [zweite Unterüberschrift]
              HeaderDivider
```

Die vier Hauptbereiche **Spielen, Historie, Mitspieler, Einstellungen** nutzen den Hauptbereichsheader ohne Zurück. Segmentbuttons, Filter und sonstige Bedienelemente stehen erst **unter** dessen Divider.

Alle anderen regulären Screens nutzen den Unterseitenheader mit fest reserviertem, gegebenenfalls ausgeblendetem Zurück-Pfeil. Bei einem konkreten Spielkontext ist die Spielkontext-Capsule verpflichtend, insbesondere auf spielbezogenen Einstellungen, Spezialkarten- und Wertungsseiten. Allgemeine Spieler-, Gruppen-, Einstellungs- und Lizenz-Unterseiten haben keinen Spielbadge.

Titel, Zurück und rechts verankerte Aktionsicons stehen in jeder jeweiligen Variante stets an denselben festen Positionen. Rechts wächst die Iconfolge von rechts nach links; nur die Höhe des Dividers richtet sich nach den vorhandenen optionalen Unterüberschriften. Die geometrischen Regeln stehen ausschließlich im `UI_DESIGN_GUIDE.md`.

Reine Auswahlseiten verwenden den gemeinsamen Zurück-/Exit-IconButton ohne unteren Schließen-Textbutton; Auswahl- und Dialogverhalten folgt dem `UI_DESIGN_GUIDE.md`.

Laufende Spielseiten zeigen die Dauer kompakt im gemeinsamen Spielkontext-Header. Ihre Abschlussaktionen stehen **nebeneinander**: links **Partie verwerfen** als destruktive Nebenaktion, rechts **Partie beenden** als Primary. Verwerfen verlangt eine Sicherheitsabfrage.

## 3. Spielen: Spiele und Gruppen

### Spieleübersicht

Header **Spiele**; darunter die scrollende Liste sichtbarer Spiele mit **vollständigem Namen**. Unten **Spiel anlegen** als Primary-Aktion zur Neuanlage eines manuellen Spiels. Keine Bearbeiten-Stifte an Spieleinträgen.

Spiele mit offenen Partien/Wertungen stehen zuerst und tragen direkt am Namen das gemeinsame Aktiv-Symbol. Danach folgen Spiele nach zuletzt gestartetem Spielbeginn, ungenutzte zuletzt. Das Statussymbol hat keine eigene Touchfläche. Die gesamte Spieleintrag-Fläche mit rechtem Chevron öffnet die Gruppenübersicht.

### Gruppenübersicht eines Spiels

Header: Zurück · **Gruppen**; darunter die Spielkontext-Capsule, dann `HeaderDivider`.

Eintrag: Gruppenname, Mitglieder, letzte Nutzung im aktuellen Spiel (wenn vorhanden), optional Aktiv-Symbol bei offener Partie/Wertung. Die gesamte Gruppenfläche mit rechtem Chevron öffnet offene Partie/Wertung oder Vorbereitung. **Kein Bearbeiten-Stift** in der Spielliste.

Die Gruppenliste zeigt ausschließlich aktive Gruppen mit aktueller Zuordnung zum Spiel. Offene Partie/Wertung steht oben, anschließend nach zuletzt gestarteter Partie der Spiel-Gruppe-Kombination; ungenutzte Gruppen zuletzt.

Fest unten:

```text
[ Neue Gruppe ]      [ Vorhandene auswählen ]
   Secondary                  Primary
```

`Vorhandene auswählen` ist deaktiviert, wenn keine aktive, noch nicht zugeordnete Gruppe innerhalb des Spielerlimits verfügbar ist.

Bei global null Spielern sind beide Aktionen deaktiviert und ein kurzer sichtbarer Hilfetext verweist auf die Spieleranlage. Bei mindestens einem Spieler, aber global null Gruppen, ist **Neue Gruppe** verfügbar und die zweite Aktion deaktiviert; ein kurzer sichtbarer Hilfetext erklärt den ersten Schritt. Existieren Gruppen, aber keine für das Spiel, erscheint nur **Keine Gruppen für dieses Spiel** ohne Onboarding-Text.

**Vorhandene auswählen** öffnet einen `SelectionDialog` mit allen aktiven, dem Spiel noch nicht zugeordneten Gruppen innerhalb des Spielerlimits. Tap auf einen Eintrag stellt die Zuordnung her, schließt den Dialog und öffnet direkt die Partievorbereitung ohne zusätzliche Bestätigung. Zurück aus der Vorbereitung führt zur Gruppenübersicht; die gespeicherte Zuordnung bleibt erhalten.

## 4. Historie

Oben: Filterchips **Spiel** und **Gruppe**. Sie dürfen bei Platzmangel umbrechen;
eine gesetzte Auswahl ist zusätzlich durch die gemeinsame sichtbare Markierung
gekennzeichnet. Darunter nach lokalem Kalendermonat gruppierte Partien;
Monatsüberschriften sind einfache, nicht-sticky Texte.

Partiekarte mit ungefähr drei umbrechenden Zeilen:
```text
<Spiel> · <Dauer> · <lokales Datum mit Uhrzeit>
<Gruppe>
1. Anna · 1. Ben · 3. Chris
```

Die gesamte Partiekarte mit rechtem Chevron öffnet die Ergebnisansicht des zuständigen Spielmoduls. Verborgene Spiele fehlen in der Liste und den Spiel-Filteroptionen. Die Liste besitzt
einen eigenen Leer-/Fehlerzustand; Busy blockiert den betroffenen Screen gemäß
`UI_DESIGN_GUIDE.md`.

Unter der scrollenden Liste liegt fest: **Importieren** (Secondary) und **N Partien exportieren** (Primary); Export ist bei `N = 0` deaktiviert.

Export berücksichtigt weiterhin alle gespeicherten Partien im gewählten Filterumfang, auch die regulär ausgeblendeten Spiele bei „Alle Spiele“. Wenn der Export mehr Partien als die sichtbare Liste umfasst, erklärt ein kurzer Hinweis diesen Umfang.

Export öffnet den App-Dialog **Exportieren** mit **Speichern · Teilen · Abbrechen**. Speichern öffnet den System-Dokumentflow, Teilen den Share-Sheet.

Import öffnet die System-Dateiauswahl. Falls nötig folgen App-Auswahlen für
Zuordnungen, insbesondere **Spieler zuordnen und Gruppen zusammenführen** oder
**Gruppen trennen**. Danach erscheint die Importvorschau mit Zusammenfassung,
neuen Gruppennamen sowie **Jetzt importieren · Abbrechen**.

Import/Export besitzt keinen zweiten Einstieg unter Einstellungen. Fachliche
Importregeln stehen ausschließlich in `IMPORT_EXPORT.md`.

## 5. Mitspieler: Spieler und Gruppen

Root **Mitspieler** mit zwei Segmenten:

```text
[ Spieler | Gruppen ]
```

### Spielerübersicht

Initial Segment **Spieler**. Bei global null Spielern ist **Gruppen** deaktiviert; ein kurzer Hilfetext fordert zum Anlegen des ersten Spielers auf. Die Historie bleibt über die Hauptnavigation erreichbar.

Filter **Gäste anzeigen** und **Archivierte anzeigen**, initial aus. Spielerkarte: Name, Erstelldatum, optionale Gast-/Archiv-Chips sowie Bearbeiten-Stift. **Keine** Zuletzt-gespielt- oder andere Spielen-Aktion. Nur der Stift öffnet den Editor.

Unten **Spieler anlegen** (neue eigenständige Entität, keine Zuordnung zu einer Gruppe).

**Spieler anlegen:** Header Zurück · **Spieler anlegen**; Name und Gast-Switch. Unten links **Weiteren hinzufügen** (Secondary), rechts **Fertig** (Primary). Kein automatischer Wechsel zu Spielen. Verhalten beim Speichern und Abbrechen siehe `PRODUCT_SPEC.md`.

**Spieler bearbeiten:** Header **Spieler bearbeiten**; Name, Archiv-Switch und **Speichern**, kein Gast-Control.

Nach erfolgreichem Speichern im Spielereditor oder in der Gruppenbearbeitung bleiben die Eingaben gesperrt. Ein sichtbarer Hinweis bestätigt die Speicherung; die bisherige Speicheraktion heißt dann **Fertig**. Schlägt die anschließende Navigation fehl, führen Fertig und Zurück nur den Abschluss erneut aus. **Weiteren hinzufügen** öffnet nach erfolgreicher Spieleranlage weiterhin einen neuen leeren Entwurf.

### Gruppenübersicht (Verwaltung)

Die Spielauswahl in Gruppenanlage und Gruppenbearbeitung bietet nur zur Mitgliederzahl passende Spiele an. Bereits gewählte, durch weitere Mitglieder unpassende Spiele bleiben im Entwurf sichtbar; eine lokalisierte Fehlermeldung verhindert das Speichern.

Segment **Gruppen**. Filter **Archivierte anzeigen**; Karten mit Gruppenname, Mitgliedern und Bearbeiten-Stift. Kein Spielstart und kein **Zuletzt gespielt**. Unten **Gruppe anlegen**.

### Gruppe anlegen

Header: Zurück · **Gruppe anlegen** · Info; rechts **Speichern**.

Inhaltsreihenfolge:

1. **Gruppenname** als normale Abschnittsüberschrift, darunter ein eigenes Eingabefeld;
2. **Gruppenmitglieder** mit Positionsnummer, Name, Hoch/Runter (Randaktionen deaktiviert) und kleinem Entfernen-X; darunter nebeneinander **Vorhandenen Spieler hinzufügen** und **Neuer Spieler**, gleichwertig Secondary;
3. **Spiele** mit vollständigen lokalisierten Namen der ausgewählten Spiele und Entfernen-X; darunter **Spiel hinzufügen**. Die Auswahl zeigt nur noch nicht enthaltene Spiele und übernimmt einen Tap direkt in den Entwurf.

Vorhandene Spieler werden per `SelectionDialog` gewählt, bereits enthaltene Spieler fehlen in der Auswahl. **Neuer Spieler** öffnet ein Overlay mit gemeinsamem Zurück-Pfeil, Titel **Spieler anlegen**, Name, Gast-Switch und **Hinzufügen**. Hinzufügen übernimmt nur in den Gruppenentwurf; Zurück verwirft nur den Overlay-Entwurf. Das Gruppenformular bleibt erhalten. Spieler und Gruppe werden erst beim Gruppenspeichern gemeinsam angelegt.

Im allgemeinen Gruppenformular gibt es keine Spiel-Farbpalette.

Aus **Spielen** ist das aufrufende Spiel vorausgewählt; nach Speichern öffnet direkt dessen Vorbereitung. Aus **Mitspieler** ist kein Spiel vorausgewählt; eine leere Auswahl ist erlaubt und Speichern führt zurück zur Gruppenverwaltung.

### Dublette beim Anlegen

Eigene abbrechbare Auswahlseite mit dem gemeinsamen Zurück-Pfeil oben links und
der Überschrift **Gruppe bereits vorhanden**. Sie erscheint bei einem oder
mehreren Treffern mit exakt identischer Menge an Spieler-IDs.

Alle Treffer werden als scrollende, gleichwertige Einträge angezeigt, mit
**Gruppenname**, **Mitgliedern in gespeicherter Default-Reihenfolge** und bei
Bedarf dem sichtbaren Status **Archiviert**. Jeder Eintrag besitzt genau einen
direkten Aktionsbutton:

- aktive Gruppe: **Gruppe übernehmen**;
- archivierte Gruppe: **Gruppe reaktivieren**.

Die Einträge folgen der vom Application-Read-Model gelieferten stabilen
Reihenfolge (Name, danach ID). Es gibt **keine Radio-Buttons**, keine
vorgeschaltete Auswahl und keine zusätzliche Bestätigung nach dem Tap.
Die Übernahme-/Reaktivierungsaktionen sind innerhalb der Trefferliste
visuell gleichwertig; sie dürfen nicht alle als dominierende globale
Primary-Aktion erscheinen.

Unter der scrollenden Trefferliste steht optisch getrennt und stets erreichbar
**Neue Gruppe anlegen** als Secondary-Aktion. Sie erlaubt ausdrücklich eine
weitere Gruppe mit denselben Mitgliedern. Der gemeinsame Zurück-Pfeil bricht
die Dubletten-Auswahl ohne Speichern ab und führt zum Gruppenentwurf zurück.

Das Muster gilt auch bei genau einem Treffer. Übernehmen/Reaktivieren übernimmt
ausschließlich die ausdrücklich angetippte Gruppenidentität und setzt den
Flow unmittelbar fort: aus **Spielen** zur Vorbereitung, aus **Mitspieler**
zur Gruppenverwaltung. Regeln zur Beibehaltung von Name, Reihenfolge,
Mitgliedschaft und Spieldefaults sowie zur Vereinigung der Spielzuordnungen
stehen in `PRODUCT_SPEC.md`.

### Gruppe bearbeiten

Abschnitte **Gruppenname → Gruppenmitglieder → Spiele**. Mitgliedermenge fest: Positionsnummer und Hoch/Runter, keine Hinzufügen-/Entfernen-Aktionen für Mitglieder. Spiele als ausgewählte Einträge mit Entfernen und darunter **Spiel hinzufügen**. Gesperrte Zuordnungen zeigen den Grund direkt am deaktivierten Entfernen. Die allgemeine Archivierungserklärung steht in der Gruppen-Info; konkrete Archivierungssperren bleiben am Switch sichtbar.

Archiv-Switch und **Speichern** bleiben erhalten; keine spielabhängige Farbauswahl.

Ist irgendeine Session dieser Gruppe Running/Scoring, ist **Archivieren** nicht möglich; der Switch wird entsprechend deaktiviert und erläutert. Die erneute Prüfung beim Speichern liegt im Produkt-/Datenmodell. Eine zugeordnete Spielverknüpfung mit offener Session/Wertung darf ebenfalls nicht entfernt werden und wird verständlich blockiert.

## 6. Einstellungen

Die Root-Seite bleibt flach und scrollt. Reihenfolge der deutlich hervorgehobenen Gruppen:

1. **Allgemein**: **Sprache** (nur Deutsch) und **Darstellung** (bestehende Themes). Beide öffnen denselben App-Auswahldialog über gemeinsame kompakte Lookup-Buttons mit Chevron nach unten.
2. **Spiele**: genau ein navigierbarer Eintrag **Spiele verwalten** mit Chevron.
3. **Über & Rechtliches**: nicht antippbare Runtime-Anzeige **Version &lt;DisplayVersion&gt; (Build &lt;BuildNumber&gt;)**, danach der Eintrag **Open-Source-Lizenzen** mit Chevron direkt zur bestehenden Lizenzübersicht.

**Spiele verwalten** ist eine eigene scrollbare Liste aller integrierten und manuellen Spiele einschließlich verborgener Spiele. Jede Zeile zeigt Name, bei manuellen Spielen **Manuelles Spiel**, bei verborgenen Spielen **Deaktiviert** und den Chevron. Die Zeile öffnet die jeweiligen Spieleinstellungen, dort steht **Spiel anzeigen** an erster Stelle. Bei offener Partie/Wertung ist Abschalten gesperrt und der Grund sichtbar. Name/Tag, manuelle Wertungsoptionen und Löschaktion sind nur in den ManualGames-Einstellungen bearbeitbar. Reaktivierung erfolgt über denselben administrativen Weg.

## 7. Open-Source-Lizenzen

Übersicht: Header mit Zurück/Divider, direkt darunter der erforderliche IJG-Hinweis, dann eine Liste aus **Komponentenname + Version** und **Lizenzbezeichnung**. Die gesamte Zeile mit rechtem Chevron öffnet die Detailseite.

Detail: Zurück/Divider, Version, Lizenzname, optional SPDX, Attribution/Copyright und scrollbarer lokaler Lizenz-/NOTICE-Text. Kein Browser/WebView und keine Zustimmung/Checkbox. Fehlendes lokales Asset zeigt Fehler + **Wiederholen**.

## 8. Quellen

Produktverhalten: `docs/PRODUCT_SPEC.md` · UI-Regeln: `docs/UI_DESIGN_GUIDE.md` · Spielscreens: `docs/Games/<GameName>/UI_WIREFRAME.md` · Import/Export: `docs/IMPORT_EXPORT.md`.
