# Boardgame Tracker – Produktspezifikation

## 1. Ziel

Boardgame Tracker erfasst, setzt fort und wertet Brettspielpartien aus. Spieler, Gruppen, Spielkontext, Partielebenszyklus und Historie sind spielübergreifend; Vorbereitung, Wertung und weitere Fachfunktionen werden von Spielmodulen ergänzt.

Aktuell vollständig unterstütztes Spiel: **Arche Nova**. Dessen Anforderungen stehen in [Games/ArkNova/PRODUCT_SPEC.md](Games/ArkNova/PRODUCT_SPEC.md).

Diese Datei beschreibt Produktverhalten. Datenstrukturen, UI-Details, Architektur sowie Import-/Lizenzdetails stehen in ihren eigenen kanonischen Dokumenten.

## 2. Spieler

Spieler sind appweit wiederverwendbar und können regulär oder Gast sein. Umbenennen und Archivieren erhalten die Identität und Historie; Archivierung löscht keine Gruppen oder Partien.

Unter **Mitspieler → Spieler** werden Spieler angelegt und verwaltet. Standardmäßig erscheinen aktive Nicht-Gäste; **Gäste anzeigen** und **Archivierte anzeigen** sind unabhängige Screenfilter. Der Gaststatus wird beim Anlegen festgelegt und im regulären Spielereditor nicht verändert.

Spielerverwaltung ist keine Spielnavigation. Es gibt dort weder „Zuletzt gespielt“ noch Start-/Fortsetzen-Aktionen.

Bei appweit **null Spielern** startet die App ohne Onboarding-Wizard in **Mitspieler → Spieler** mit einem kurzen sichtbaren Hilfetext. Das Segment **Gruppen** ist zunächst deaktiviert. Historie und Einstellungen bleiben erreichbar, damit insbesondere ein History-Import auf einer frischen Installation möglich bleibt. Auch nach Anlage des ersten Spielers bleibt die App unter **Mitspieler → Spieler**. In der eigenständigen Spieleranlage speichert **Weiteren hinzufügen** den Spieler und öffnet einen leeren Entwurf mit zurückgesetztem Gaststatus. **Fertig** speichert und kehrt zur Spielerübersicht zurück. Zurück verwirft nur den aktuellen ungespeicherten Entwurf; bei Speicherfehler bleiben die Eingaben erhalten.

## 3. Gruppen

Eine Gruppe ist eine appweit wiederverwendbare Identität mit festem Mitgliedersatz, globaler Default-Reihenfolge, Namen und Archivstatus. Sie kann mit **null bis beliebig vielen** Spielen verknüpft sein. Ein temporärer Gruppenstatus entfällt.

Eine Gruppe darf einem Spiel nur zugeordnet werden, wenn ihre Mitgliederzahl zwischen dessen `MinPlayers` und `MaxPlayers` liegt. Die Prüfung gilt zentral für jede neue Zuordnung, einschließlich Übernahme, Reaktivierung und Import. Historische Partien dürfen trotz abweichendem heutigem Limit bestehen; beim Import wird in solchen Fällen keine ungültige aktuelle Zuordnung angelegt. Integrierte Spiele haben feste registrierte Grenzen, manuelle Spiele konfigurierbare Grenzen (Details unter `Games/ManualGames/`).

Eine andere Spielerzusammensetzung benötigt eine neue Gruppe. Spielmodule dürfen optional spielabhängige Gruppendefaults ergänzen, ohne allgemeine Gruppendaten zu duplizieren.

**Eine Gruppe mit mindestens einer offenen Partie oder Wertung (Running/Scoring) in irgendeinem Spiel darf nicht archiviert werden.** Abgeschlossene Partien verhindern eine Archivierung nicht. Spielzuordnungen dürfen entfernt werden, solange für die konkrete Gruppe-Spiel-Kombination keine offene Partie/Wertung besteht; die Historie bleibt erhalten.

Beim Anlegen prüft die App, ob bereits eine Gruppe mit exakt der gleichen Menge an Spieler-IDs existiert; deren Reihenfolge und Name zählen dabei nicht. Die bestehende Gruppe darf übernommen, im archivierten Fall reaktiviert oder bewusst eine zweite Gruppe mit gleicher Mitgliedermenge angelegt werden. Bei Übernehmen/Reaktivieren bleiben Identität, Name, Mitglieder, Default-Reihenfolge und Spieldefaults erhalten. Die im Entwurf gewählten Spielzuordnungen werden vereinigt, bereits bestehende Zuordnungen nicht entfernt.

## 4. Spielkontext und Navigation

Die Hauptbereiche sind:

```text
Spielen · Historie · Mitspieler · Einstellungen
```

Ein expliziter Tap auf einen Tab öffnet stets dessen Root und verwirft tiefere Navigationsstacks.

Der Spielfluss lautet **Spieleübersicht → Gruppenübersicht im Spielkontext → Vorbereitung oder offene Partie/Wertung**. Zurück aus der Gruppenübersicht führt immer zur Spieleübersicht, auch beim Direkteinstieg über den Spielen-Tab.

Bei explizitem Tap auf **Spielen** gilt:
1. Existieren offene Sessions (Running/Scoring) ausschließlich für **ein** Spiel, wird dessen Gruppenübersicht geöffnet – auch wenn mehrere Gruppen dieses Spiels offene Sessions besitzen.
2. Bei keinen oder bei auf mehrere Spiele verteilten offenen Sessions wird die Spieleübersicht geöffnet.

Eine bestehende Partie behält den konkret aufrufenden Screen (Gruppenübersicht oder Historie) als Rückziel. Dieses Rückziel bleibt beim Wechsel zwischen Running, Scoring, Ergebnis und historischer Korrektur sowie bei erfolgreichem Verwerfen erhalten. Ein expliziter Haupttab-Tap überschreibt den Rückkontext.

Der lokale Navigationszustand darf zuletzt ausgewähltes Spiel, letzten Hauptbereich und die zuletzt explizit geöffnete offene Partie/Wertung merken. Beim App-Start wird eine weiterhin gültige gemerkte offene Partie/Wertung zuerst wiederaufgenommen, andernfalls der letzte Hauptbereich geöffnet. Für dessen Spielen-Root gilt die obige Auflösung. Ohne vorherigen Kontext startet **Spielen**, vorbehaltlich des Erststarts ohne Spieler.

Mehrere Gruppen und Spiele dürfen gleichzeitig offene Sessions besitzen, pro Gruppe-Spiel-Kombination höchstens eine. Es gibt **kein Standardspiel**.

## 5. App- und Spielmaterialsprache

App-Sprache und Spielmaterialsprache sind getrennt. App-Sprache steuert allgemeine UI-Texte; ein Spiel kann eine eigene Materialsprache anbieten.

Ohne gespeicherte Materialsprache wird die App-Sprache verwendet, wenn unterstützt, sonst die Defaultsprache des Spiels. Die Auswahl wird pro Spiel gespeichert und durch spätere App-Sprachwechsel nicht automatisch überschrieben.

Materialsprache verändert weder App-Kultur noch historische Partiedaten und wird nicht mit Partien exportiert. Nutzerinhalt hat bei der Namensauflösung Vorrang. Weitere Contentregeln stehen in Architektur und Datenmodell.

## 6. Spielen: Spiele- und Gruppenübersicht

Die Spieleübersicht zeigt alle sichtbaren unterstützten Spiele mit ihrem gespeicherten vollständigen Namen; der separat festgelegte Capsule-Tag wird im gemeinsamen Spielkontext verwendet. Eine offene Session/Wertung wird mit dem bestehenden nicht-interaktiven Aktiv-Symbol am Spielnamen gekennzeichnet. Unten steht **Spiel anlegen** zum Erstellen eines manuellen Spiels; vorhandene Spiele werden hier nicht bearbeitet.

Die Spieleübersicht sortiert zunächst Spiele mit offener Session/Wertung, dann nach der **zuletzt gestarteten** Partie und schließlich Spiele ohne Partien. Verborgene Spiele erscheinen in keiner regulären spielbezogenen Liste oder Auswahl. Die administrative Gesamtliste unter Einstellungen bleibt zur Reaktivierung erreichbar.

Die Gruppenübersicht eines Spiels zeigt nur **aktive und diesem Spiel zugeordnete** Gruppen. Sie sortiert zuerst Gruppen mit offener Session/Wertung dieser Spiel-Gruppe-Kombination, dann nach der zuletzt gestarteten Partie in diesem Spiel; Gruppen ohne entsprechende Partie folgen zuletzt. Tap auf eine Gruppe setzt die offene Partie/Wertung fort oder öffnet die Vorbereitung.

Unten stehen **Neue Gruppe** (links, Secondary) und **Vorhandene auswählen** (rechts, Primary). Die zweite Aktion öffnet eine Auswahl aktiver, dem Spiel noch nicht zugeordneter Gruppen innerhalb seines Spielerlimits. Tap fügt die Spielzuordnung hinzu und öffnet **ohne weitere Bestätigung direkt die Vorbereitung**. Ein Abbruch der Vorbereitung entfernt die bereits gespeicherte Zuordnung nicht. Sind keine geeigneten Gruppen vorhanden, ist diese Aktion deaktiviert.

Echte globale Leerzustände dürfen einen kurzen sichtbaren Einstiegshinweis haben:
- bei null Spielern verweist er auf die Spieleranlage; Gruppenaktionen bleiben deaktiviert;
- bei mindestens einem Spieler, aber null Gruppen verweist er auf **Neue Gruppe**; **Vorhandene auswählen** ist deaktiviert.

Existieren bereits Gruppen, aber keine für dieses Spiel zugeordnete, erscheint nur ein neutraler Leerzustand ohne Onboarding-Hilfetext. Ein allgemeiner Gruppendetail-Screen ist nicht erforderlich.

## 7. Gruppen anlegen und verwalten

Unter **Mitspieler** werden die beiden Segmente **Spieler** und **Gruppen** verwaltet; es gibt dort keine Spielstartaktionen. Gruppen werden nur über die separate Bearbeiten-Aktion editiert. In der Gruppenverwaltung werden archivierte Gruppen optional angezeigt.

Das Gruppenformular enthält in dieser Reihenfolge: **Name → feste Mitglieder mit Reihenfolge → vorhandene/neue Spieler hinzufügen → Spielezuordnungen**. Neue Spieler können im selben Entwurf mit Name und Gaststatus ergänzt werden. Bereits enthaltene Spieler werden nicht erneut angeboten. Spielauswahlen bieten nur Spiele für die aktuelle Mitgliederzahl an. Wird ein bereits gewähltes Spiel durch zusätzliche Mitglieder ungeeignet, bleibt die Auswahl sichtbar und der ungültige Entwurf wird mit lokalisierter Fehlermeldung vom Speichern ausgeschlossen.

Aus **Spielen → Spiel → Neue Gruppe** ist das aktuelle Spiel vorausgewählt. Nach Speichern oder Übernehmen einer vorhandenen Gruppe öffnet direkt die Vorbereitung dieses Spiels. Aus **Mitspieler → Gruppen** ist keine Spielzuordnung vorausgewählt und eine Gruppe ohne Zuordnung zulässig; nach Speichern/Übernehmen erfolgt die Rückkehr zur Gruppenverwaltung.

Bei Dublette gibt es eine eigene, abbrechbare Auswahl: **Gruppe übernehmen**, bei archivierter Gruppe **Gruppe reaktivieren**, jeweils Primary, oder **Neue Gruppe anlegen** als Secondary. Die Übernahme verwirft den neuen Entwurfsnamen und behält die bereits gespeicherte globale Reihenfolge.

Bei bestehenden Gruppen sind **Name, Archivstatus, globale Default-Reihenfolge und Spielzuordnungen** bearbeitbar; die Mitgliedermenge bleibt unveränderlich. Spielabhängige Farben werden hier nicht editiert.

Bei Archivierung und beim Entfernen einer Spielzuordnung werden die jeweiligen offenen Sessions **beim tatsächlichen Speichern erneut geprüft**; zwischenzeitlich gestartete Partien dürfen nicht durch einen veralteten Editorzustand umgangen werden.

Neue Spieler, Gruppe und Spielzuordnungen werden beim Speichern atomar angelegt. Abbruch hinterlässt keine neuen Spieler.

## 8. Spielerfarben als optionales Spielkonzept

Verwendet ein Spiel die gemeinsame `PlayerColor`-Semantik, definiert es seine erlaubte Teilmenge. Gruppen können optionale spielabhängige Defaultfarben halten; ohne Farbunterstützung gibt es keine Farbauswahl.

Die Partievorbereitung übernimmt bestehende gruppen-/spielabhängige Farbdefaults und darf sie im Entwurf verändern. Fehlende eindeutige Farben können vom Spiel vorbelegt werden. **Erst beim erfolgreichen Partiestart** werden die tatsächlichen Farben in der Session und als neue Defaultfarben für Gruppe und Spiel gemeinsam gespeichert. Der Abbruch der Vorbereitung ändert keine Defaults.

Farben gehören nicht zum allgemeinen Spielerprofil und werden nicht in **Mitspieler** bearbeitet.

UI-Verhalten: [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md). Datenmodell: [DATA_MODEL.md](DATA_MODEL.md).

## 9. Partielebenszyklus

Eine neue Partie beginnt als nicht persistierter spielabhängiger Entwurf. Erst beim Start entsteht die `GameSession`.

```text
Vorbereitung → Running → Scoring → Completed
```

Die Partievorbereitung beginnt mit der globalen Default-Reihenfolge der Gruppe. **Nur bei tatsächlich erfasster Startreihenfolge** werden deren Positionen beim erfolgreichen Partiestart in `GameSessionPlayer.Order` und als neuer globaler `GroupPlayer.DefaultOrder` gespeichert. Sonst bleibt die historische `Order` für alle Teilnehmer `null`, und der Gruppen-Default unverändert. Abgebrochene Entwürfe verändern keine Defaults.

Eine laufende Partie kann fortgesetzt, regulär beendet oder nach Sicherheitsabfrage verworfen werden. Beim Übergang zu `Scoring` wird die Spielendzeit festgehalten.

Während `Scoring` dürfen unvollständige spielabhängige Zwischenstände gespeichert werden; sie zählen noch nicht als abgeschlossene Historie oder Statistik. Erst `Completed` ist das fertige Ergebnis.

Welche Daten beim Start und Abschluss erforderlich sind, bestimmt das Spielmodul.

## 10. Historie und Korrektur

Die Historie zeigt ausschließlich `Completed`-Partien, neueste zuerst, mit Filtern
nach Spiel und Gruppe. Ein sinnvoll vorhandener Spielkontext darf vorausgewählt
sein; sonst gilt zunächst „alle Spiele“. Namen werden aktuell aus Player/Group
aufgelöst; gespeicherte Placements bleiben maßgeblich.

Ein Tap öffnet die spielabhängige Ergebnisansicht. Unterstützt das Spiel historische Korrekturen, bleibt die Partie dabei `Completed`; das Spielmodul definiert die korrekturfähigen Ergebnisdaten.

Partien können nach Sicherheitsabfrage gelöscht werden. Spieler und Gruppe bleiben erhalten.

## 11. Statistik – Zielbild

Statistik ist ein späterer, von der Partienliste unabhängiger Slice. Allgemeine Zielmetriken sind Anzahl Partien, Siege, Gewinnquote und durchschnittliche Platzierung. Filter können Spiel, Gruppe, Spieler und Zeitraum umfassen. Die jeweilige Auswertung berücksichtigt nur Partien mit dafür tatsächlich vorhandenen historischen Informationen: Sieger für Siegstatistiken, vollständige Plätze für Platzierungsdurchschnitte, erfasste `Order` für Startposition/Startspieler. Fehlende Werte sind keine Niederlagen oder Nullwerte. Stichprobengrößen bleiben erkennbar; Spielmodule dürfen Dimensionen ergänzen.

## 12. Einstellungen

Allgemeine Einstellungen enthalten die App-Sprache (derzeit nur Deutsch), die Auswahl bestehender Themes, **Spiele verwalten**, die installierte Version/Build und Open-Source-Lizenzen. Spieler und Gruppen werden unter **Mitspieler** verwaltet. Ein Standardspiel gibt es nicht. Die App-Sprache und die Sprache des Spielmaterials sind getrennte Einstellungen.

Unter **Spiele verwalten** sind alle unterstützten integrierten und manuellen Spiele unabhängig von ihrer Sichtbarkeit erreichbar und standardmäßig sichtbar. Die Liste kennzeichnet manuelle und ausgeblendete Spiele. Der Eintrag öffnet die Spieleinstellungen mit **Spiel anzeigen** an erster Stelle. Verbergen ist bei einer offenen Partie/Wertung (Running/Scoring) dieses Spiels gesperrt und wird beim Speichern erneut validiert. Verborgene Spiele verschwinden aus Spielen, den Spiel-Gruppenansichten, regulären Gruppen-Spielauswahlen sowie der Historienliste und ihrem Spiel-Filter. Sessions, Ergebnisse, Zuordnungen und Defaults bleiben unverändert gespeichert; Reaktivierung macht sie wieder sichtbar. Import und Export bleiben unabhängig von dieser UI-Sichtbarkeit vollständig; die administrative Gesamtliste bleibt die Ausnahme der regulären Ausblendung.

Verfügbare Themes, Default, visuelle Tokens und Wechselverhalten stehen ausschließlich in [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md).
Drittanbieter-Lizenzanforderungen stehen ausschließlich in [THIRD_PARTY_LICENSE_POLICY.md](THIRD_PARTY_LICENSE_POLICY.md).

## 13. Import und Export

Die Historie kann abgeschlossene Partien importieren und exportieren. Analyse/Vorschau schreibt nichts; erst die ausdrückliche Importbestätigung persistiert atomar. Abbruch oder Fehler hinterlassen keine Teildaten. Spielmodule liefern ihre zusätzlichen Daten über allgemeine Transfer-Erweiterungspunkte.

Historische Sessions bleiben auch dann erreichbar, wenn ihre Gruppe dem Spiel aktuell nicht mehr zugeordnet ist. Der Transfer importiert aktuelle Spielzuordnungen nur für **neu angelegte Gruppen**; Zuordnungen bereits vorhandener Gruppen bleiben unverändert.

Format, Matching, Konfliktauflösung und Transportdetails: [IMPORT_EXPORT.md](IMPORT_EXPORT.md).

## 14. Spielarten

Integrierte Spiele haben feste Modulregeln; benutzerdefinierte Spiele gehören zum eigenständigen Modul [ManualGames](Games/ManualGames/PRODUCT_SPEC.md). Alle verwenden die gemeinsamen Spiel-, Gruppen-, Session- und Historienverträge. Integrierte Spiele erhalten keinen vereinfachten manuellen Erfassungsmodus.

Dokumentationszuständigkeiten und Generalisierungstest: [README.md](README.md).
