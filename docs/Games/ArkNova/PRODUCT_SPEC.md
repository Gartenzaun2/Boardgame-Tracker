# Arche Nova – Produktspezifikation

## 1. Geltungsbereich

Diese Datei ergänzt [docs/PRODUCT_SPEC.md](../../PRODUCT_SPEC.md) nur um Arche-Nova-spezifisches Produktverhalten. Allgemeine Regeln werden nicht wiederholt.

Weitere kanonische Quellen:
- [DATA_MODEL.md](DATA_MODEL.md) für Werte, Identitäten und Invarianten;
- [UI_DESIGN_GUIDE.md](UI_DESIGN_GUIDE.md) für Darstellung und Interaktion;
- [CV_STATUS.md](CV_STATUS.md) für CV-Forschungs-/Integrationsstatus;
- [../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md) für den allgemeinen Transfervertrag;
- [IMPORT_EXPORT.md](IMPORT_EXPORT.md) für das Arche-Nova-Transferschema.

## 2. Spielumfang und Einstellungen

Arche Nova unterstützt Vorbereitung, laufende Partie, persistente Wertung, Ergebnis/Korrektur, Spezialkartenverwaltung, assistierte Fotoerkennung sowie Import/Export.

Materialsprache ist aktuell ausschließlich **Deutsch (`de`)** und zugleich Default.

Lokale Arche-Nova-Einstellungen umfassen insbesondere:
- **Ruf erfassen**;
- **Neue Wertung / Alte Wertung**;
- Spezialkartenverwaltung;
- Materialsprache.

Diese Einstellungen sind keine historischen Partiedaten.

## 3. Partievorbereitung

Aus einer Gruppe wird ein unpersistierter Arche-Nova-Entwurf erzeugt:
1. Reihenfolge und Farbdefaults übernehmen;
2. fehlende eindeutige Farben ergänzen;
3. zwei Zooplanoptionen je Spieler bestimmen;
4. je Spieler einen Plan wählen;
5. Farbe/Reihenfolge bei Bedarf nur für diese Partie ändern;
6. Partie starten.

Bis zum Start wird nichts davon als Partie gespeichert.

### Farben

Arche Nova erlaubt genau:

```text
Blue · Yellow · Red · Black
```

Gruppendefaults werden übernommen; Änderungen in der Vorbereitung verändern sie nicht.
Vor dem Start muss jeder Partieteilnehmer eine unterschiedliche erlaubte Farbe besitzen.
Doppelte Farben dürfen im unpersistierten Entwurf vorübergehend vorkommen; solange
sie bestehen, kann die Partie nicht gestartet werden.

### Zoopläne

Jeder Spieler erhält zwei verschiedene Optionen und wählt genau eine. Innerhalb derselben Vorbereitung wird kein Plan zweimal angeboten. Optionen und Auswahl werden erst beim Start historisch gespeichert.

### Startreihenfolge

Ausgangspunkt ist die Gruppenreihenfolge. Existiert für dieselbe Gruppe eine abgeschlossene letzte Arche-Nova-Partie, wird deren letztplatzierter Spieler durch zyklische Rotation Startspieler; bei geteiltem letzten Platz entscheidet der alphabetisch erste Name.

Die Vorbereitung erlaubt zyklisches Rotieren, Verschieben einzelner Spieler und zufällige Startspielerwahl. Diese Aktionen bewahren soweit anwendbar die relative Reihenfolge und gelten nur für die konkrete Partie.

## 4. Laufende Partie

Beim Start werden zusätzlich zu den allgemeinen GameSession-Daten Spielerfarben, beide angebotenen Zoopläne und die Auswahl gespeichert.

Die laufende Ansicht zeigt die für das Weiterspielen nötigen Spieler-, Farb-, Zooplan- und Reihenfolgedaten sowie die Dauer.

**Partie beenden** wechselt zu `Scoring`. **Partie verwerfen** löscht die offene Partie nach Sicherheitsabfrage.

## 5. Wertung

Pro Spieler werden erfasst:
- Attraktivität;
- Artenschutz;
- optional Ruf;
- Spezialkarten.

Attraktivität und Artenschutz dürfen während der Eingabe fehlen, sind aber zum Abschluss erforderlich. Ruf bleibt optional und wird nur angeboten, wenn **Ruf erfassen** aktiv ist.

Gültige Änderungen werden während `Scoring` automatisch gespeichert. Es gibt keinen zusätzlichen „Spieler fertig“-Status. **Wertung abschließen** wird möglich, sobald alle Pflichtwerte vorliegen. Eine offene Wertung kann nach Sicherheitsabfrage verworfen werden.

Wertebereiche und Berechnung stehen in [DATA_MODEL.md](DATA_MODEL.md).

### Eingabevorschläge

Leere Pflichtwerte zeigen zunächst unverbindliche Vorschläge:

```text
Attraktivität 57
Artenschutz   21
```

Für Ruf ist der unverbindliche Vorschlag `8`.

Vorschläge zählen noch nicht als Eingabe. Benutzerinteraktion übernimmt den jeweiligen Wert. **Fertig** übernimmt noch offene Vorschläge der beiden Pflichtwerte; Zurück nicht. Ruf bleibt optional und kann geleert werden.

## 6. Punkteanzeige

Gespeichert werden Rohwerte, nicht die daraus berechneten Punkte. Die Einstellung **Neue Wertung / Alte Wertung** ändert nur die Darstellung und niemals historische Daten.

Berechnung: [DATA_MODEL.md](DATA_MODEL.md).

## 7. Assistierte Fotoerkennung

Fotoerkennung füllt dieselben Wertungsfelder wie die manuelle Eingabe; es gibt
keinen separaten Kamera-Wertungspfad. Erkannte Spielerwerte werden über die
Spielerfarben den Partieteilnehmern zugeordnet.

Ablauf:
1. Foto aufnehmen oder auswählen;
2. analysieren;
3. Vorschläge isoliert prüfen;
4. Sicherheit mit Farbe plus Text/Icon darstellen;
5. korrigieren;
6. ausdrücklich bestätigen;
7. erst dann atomar speichern.

Unbestätigte Vorschläge nehmen nicht am normalen Autosave teil. Auch ein als sicher
markierter Wert bleibt nur ein prüfpflichtiger Vorschlag. Analysefehler überschreiben
keine vorhandenen manuellen Werte. Picker-Abbruch ist kein Fehler. Ruf bleibt optional.
Es erfolgen keine automatischen Nachbarwertkorrekturen.

Kamera/Fotowahl nutzt native Systempicker; es gibt keine eigene Galerie und keine
unnötige Vollbibliotheksberechtigung allein für die Fotoauswahl.

Für Attraktivität und Artenschutz liefert der CV-Pfad bei `0` weiterhin keinen
Kandidaten; manuelle `0`-Eingabe bleibt gültig.

Zielbild: Unterstützte Wertungstafeln können später in den Arche-Nova-Einstellungen
auswählbar werden; konkrete Varianten werden erst bei Umsetzung festgelegt.

Technischer/research Status: [CV_STATUS.md](CV_STATUS.md).

## 8. Spezialkarten

Spezialkarten sind Arche-Nova-weite, benutzerverwaltbare Referenzdaten.

Nutzer können Karten anlegen, bearbeiten, archivieren und entarchivieren. Es gibt keine normale Löschaktion und keinen vollständigen Reset.

Eine Karte besitzt stabile GUID, null bis drei geordnete Symbole, aktuellen Contentnamen und Archivstatus. Nutzer können keine neuen Symboltypen anlegen. Seedkarten, Symbolkatalog und IDs stehen in [DATA_MODEL.md](DATA_MODEL.md).

Bearbeiten verändert nicht die mitgelieferte Contentdatei.

### Zuordnung in der Wertung

Neue Zuordnungen sind nur für aktive Karten erlaubt. Eine Karte darf mehreren Spielern derselben Partie zugeordnet sein, aber nicht doppelt demselben Spieler. Die bestehende Sicherheitsabfrage für neue Mehrfachzuordnungen bleibt erhalten.

Archivierte Karten bleiben historisch sichtbar und können aus bestehenden Zuordnungen entfernt, aber nicht neu hinzugefügt werden. Beim Speichern wird der aktuelle Archivstatus erneut geprüft.

Historische Partien speichern keine Namens- oder Symbolsnapshots.

## 9. Gleichstand und Abschluss

Bei punktgleichen Spielern öffnet vor dem Abschluss die Gleichstandsauflösung.

Jede Gruppe gleicher berechneter Punktzahl bildet einen unabhängigen Block. Innerhalb seines ursprünglichen Platzbereichs dürfen Plätze frei auf die Blockteilnehmer verteilt werden.

Es gilt:
- Ausgangsplatzierung nach Competition Ranking;
- geteilte Plätze erlaubt;
- Ranglücken werden nicht automatisch geschlossen;
- mehrere Spieler dürfen denselben zulässigen Platz erhalten;
- andere Scoreblöcke bleiben unverändert;
- Platzwahl verändert nicht den Score.

Nach gültiger Auflösung werden Placements gespeichert und die Partie `Completed`. Die Ergebnisansicht bleibt sichtbar, bis der Nutzer sie ausdrücklich beendet.

## 10. Historische Ergebnisse und Korrektur

Abgeschlossene Arche-Nova-Partien werden nach gespeichertem Placement sortiert; bei gleichem Placement entscheidet die ursprüngliche Startreihenfolge. Der Score ersetzt niemals das Placement.

Historisch korrigierbar sind ausschließlich:
- Attraktivität;
- Artenschutz;
- Ruf;
- Spezialkarten;
- daraus neu zu validierende Placements.

Spiel, Gruppe, Teilnehmer, Farben, Zoopläne, Startreihenfolge sowie Start-/Endzeit bleiben unverändert.

Die Bearbeitung erfolgt zunächst in einem Entwurf. **Korrektur speichern** übernimmt die Änderungen atomar; die Partie bleibt durchgehend `Completed`. Ändern sich Scores, werden Placements neu bestimmt und nötige Gleichstände über denselben Flow aufgelöst.

Ist **Ruf erfassen** während einer Korrektur deaktiviert, bleibt ein bereits
gespeicherter Ruf unverändert; das ausgeblendete Feld wird nicht geleert.

## 11. Statistik – Zielbild

Spätere Arche-Nova-Statistik darf zusätzlich nach Zooplan und Spezialkarte filtern. Neue Spezialkartenfilter bieten nur aktive Karten an; historische Partien mit inzwischen archivierten Karten bleiben auswertbar.

## 12. Import / Export

Arche-Nova-Partien nehmen am allgemeinen History-Transfer teil.

Allgemeines Format, Matching und Commit:
[../../IMPORT_EXPORT.md](../../IMPORT_EXPORT.md).

Arche-Nova-Schema, Validierung und Spezialkartenreferenzen:
[IMPORT_EXPORT.md](IMPORT_EXPORT.md).
