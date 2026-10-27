# Boardgame Tracker – Produktspezifikation

## 1. Ziel

Boardgame Tracker ist eine mobile App zum Erfassen, Fortsetzen und Auswerten von
Brettspielpartien.

Die Anwendung unterstützt:

- allgemeine spielübergreifende Konzepte wie Personen, Gruppen, Partien und
  Historie,
- stark typisierte spielspezifische Regeln und Wertungen,
- mehrere Spiele ohne generische Game Engine,
- später zusätzlich manuell angelegte Spiele mit konfigurierbarer einfacher
  Ergebnisstruktur.

Erstes vollständig unterstütztes Spiel ist **Arche Nova**.

## 2. Spielübergreifende Konzepte

### Person

Eine Person ist wiederverwendbar und kann Mitglied mehrerer Gruppen sein.

Eigenschaften:

- Name
- Gaststatus
- aktiv/inaktiv

Ein Gast ist keine separate Entität. Eine Person kann später von Gast zu regulär
werden, ohne ID oder Historie zu verlieren.

### Gruppe

Eine Gruppe repräsentiert eine feste Personenzusammensetzung.

Eigenschaften:

- Name
- feste Mitglieder
- Default-Reihenfolge
- temporär/permanent
- archiviert/aktiv

Regeln:

- mindestens eine Person,
- eine Person höchstens einmal,
- lückenlose eindeutige Default-Reihenfolge,
- eine andere Personenzusammensetzung ist fachlich eine andere Gruppe,
- `temporär -> permanent` ist erlaubt,
- `permanent -> temporär` ist nicht erlaubt,
- Archivierung ist reversibel und kein Löschen.

Gruppen sind fachlich **spielunabhängig**. Spielspezifische Gruppendaten wie
Arche-Nova-Defaultfarben werden separat gespeichert.

### Spiel

`Game` besitzt eine stabile ID und einen sprachneutralen Schlüssel.

Beispiel:

```text
ark-nova
```

Anzeigenamen werden lokalisiert.

Perspektivisch können Benutzer eigene Spiele anlegen. Für manuelle Spiele soll
später konfigurierbar sein, ob nur eine gespielte Partie, Gewinner, Punkte
und/oder Platzierungen erfasst werden.

## 3. Hauptnavigation und Spielkontext

Reihenfolge:

```text
Spiele · Gruppen · Historie · Einstellungen
```

### Spiele

Der Spiele-Tab wählt den aktuellen Spielkontext.

Im aktuellen MVP ist Arche Nova das einzige vollständig unterstützte Spiel.

Ein Tap auf ein Spiel:

1. setzt den aktuellen Spielkontext,
2. öffnet `Gruppen` für dieses Spiel.

### Gruppen

Die Gruppenübersicht besitzt im normalen Bedienfluss **immer** einen gültigen
Spielkontext.

Es gibt keine allgemeine Gruppenübersicht ohne ausgewähltes Spiel.

Darstellung:

- Seitentitel `Gruppen`,
- direkt darunter der aktuelle Spielname,
- reguläre Gruppen standardmäßig sichtbar,
- temporäre und archivierte Gruppen optional einblendbar,
- `Neue Gruppe`,
- Bearbeiten-Icon je Gruppe,
- Mitglieder,
- letzte abgeschlossene Partie für das aktuelle Spiel,
- Aktiv-Icon bei einer offenen Session für das aktuelle Spiel.

Tap auf Gruppenkarte:

- existiert für dieses Spiel und diese Gruppe eine offene Session, wird sie
  fortgesetzt,
- sonst öffnet sich die spielspezifische Partievorbereitung.

Bearbeiten bleibt eine getrennte Touch-Fläche.

### Historie

Die Historie soll abgeschlossene Partien spielübergreifend anzeigen:

- Spiel
- Gruppe
- Datum
- Spieler
- Dauer
- Gewinner
- kurze Zusammenfassung

Historische Partien sollen später über eine explizite Bearbeiten-Aktion
korrigierbar sein.

Der Tab existiert bereits; die vollständige Historienimplementierung ist noch
offen.

### Einstellungen

Allgemeine Einstellungen enthalten insbesondere ein optionales
**Standardspiel**.

Spielspezifische Einstellungen liegen unter dem jeweiligen Spielbereich bzw.
sind von den allgemeinen Einstellungen erreichbar.

## 4. Start- und Wiederherstellungslogik

Lokal gespeichert werden:

- optionales Standardspiel,
- zuletzt ausgewähltes Spiel,
- letzter Hauptbereich,
- zuletzt tatsächlich betrachtete offene Session.

Startlogik:

1. War zuletzt eine konkrete Session aktiv und ist sie noch `Running` oder
   `Scoring`, wird genau diese Session geöffnet.
2. Sonst wird der zuletzt verwendete Hauptbereich wiederhergestellt.
3. Benötigt `Gruppen` einen Spielkontext, wird das zuletzt ausgewählte Spiel
   verwendet.
4. Fehlt dieser Kontext, kann das Standardspiel verwendet werden.
5. Ohne gespeicherten Kontext und ohne Standardspiel startet die App bei
   `Spiele`.
6. Ohne gespeicherten Kontext und mit Standardspiel startet die App bei
   `Gruppen` für dieses Spiel.
7. Ein Gruppenbereich ohne gültigen Spielkontext fällt auf `Spiele` zurück.

Das Standardspiel ist ein Fallback für den Einstieg, nicht die globale
Überschreibung jedes späteren Navigationszustands.

## 5. Offene Partien

Fachliche Zielregel:

> Pro Kombination aus **Spiel + Gruppe** darf höchstens eine offene Session
> existieren.

Offen bedeutet:

```text
Running
Scoring
```

Mehrere offene Sessions sind erlaubt, wenn sich Spiel und/oder Gruppe
unterscheiden.

Beispiele:

- Arche Nova + Freitagabend: maximal eine offene Session.
- Terraforming Mars + Freitagabend: zusätzlich möglich.
- Arche Nova + Sonntagsrunde: zusätzlich möglich.

Beim erneuten Öffnen derselben Gruppe im selben Spiel wird die offene Session
fortgesetzt statt eine zweite zu erzeugen.

## 6. Arche Nova – Gruppendaten

Für jedes Mitglied einer Gruppe kann eine Arche-Nova-Defaultfarbe gespeichert
werden:

- Blau
- Gelb
- Rot
- Schwarz

Die Defaultfarbe hängt von:

- Gruppe,
- Spiel,
- Person

ab.

Innerhalb einer Arche-Nova-Gruppe müssen gesetzte Defaultfarben eindeutig sein.

## 7. Arche Nova – Partievorbereitung

Flow:

```text
Spiel auswählen
→ Gruppe auswählen
→ Partie vorbereiten
→ Zoopläne auslosen
→ je Spieler Zooplan wählen
→ ggf. Farbe/Reihenfolge ändern
→ Starten
```

Wenn das Spiel bereits über Standard-/letzten Kontext feststeht, beginnt der
sichtbare Flow direkt bei `Gruppe auswählen`.

### Vorbereitung

Die Vorbereitung ist bis `Starten` reiner UI-Zustand und wird nicht als
unfertige Session gespeichert.

Übernommen werden:

- feste Gruppenmitglieder,
- Default-Reihenfolge,
- Arche-Nova-Defaultfarben,
- letzte abgeschlossene Arche-Nova-Partie dieser Gruppe.

### Startspieler

Der Verlierer der letzten abgeschlossenen Partie wird auf Position 1 rotiert.

Bei geteiltem letzten Platz entscheidet der Name alphabetisch.

Eine Zufallsaktion kann die bestehende Reihenfolge zyklisch rotieren, ohne die
relative Reihenfolge beliebig neu zu mischen.

### Zoopläne

Pläne 1–14:

1. Aussichtsplattform
2. Freilaufflächen
3. Silbersee
4. Handelshafen
5. Parkrestaurant
6. Forschungsinstitut
7. Eisdielen
8. Hollywood Hills
9. Geozoo
10. Auffangstation
11. Höhlen
12. Künstliche Intelligenz
13. Reißbrett
14. Lagune

Regeln:

- jeder Spieler erhält zwei verschiedene Pläne,
- jeder angebotene Plan ist sessionweit eindeutig,
- jeder Spieler wählt genau einen der beiden Pläne,
- `Starten` ist bis zur vollständigen Auswahl sichtbar deaktiviert.

### Farben

Doppelte Farben dürfen während der Bearbeitung vorübergehend vorkommen, blockieren
aber `Starten`.

Beim Start muss jede Spielerfarbe innerhalb der Session eindeutig sein.

### Persistenz beim Start

Erst bei `Starten` werden gespeichert:

- Startzeit,
- Spiel,
- Gruppe,
- Teilnehmer,
- Reihenfolge,
- tatsächliche Farben,
- beide angebotenen Zoopläne,
- ausgewählter Zooplan.

## 8. Laufende Arche-Nova-Partie

Status:

```text
Running
```

Darstellung:

- Gruppe
- Spieler
- Reihenfolge
- Spielerfarbe
- gewählter Zooplan
- laufende Dauer

Primäraktion:

```text
Partie beenden
```

Dadurch:

```text
Running -> Scoring
```

Endzeit wird gesetzt.

Sekundäre destruktive Aktion:

```text
Verwerfen
```

nur nach Sicherheitsabfrage.

## 9. Arche Nova – Wertung

Status:

```text
Scoring
```

Zwischenstände werden automatisch persistiert. Eine Session in `Scoring` ist
noch nicht historisch abgeschlossen.

### Spielerwerte

Pflicht für Abschluss:

- Attraktivität `0..113`
- Artenschutz `0..41`

Optional:

- Ruf `1..15`
- besondere Karten

Ruf wird nur angeboten, wenn die globale Arche-Nova-Einstellung
`Ruf erfassen` aktiv ist.

### UI der Wertungsübersicht

Pro Spieler:

```text
Name · berechnete Punkte
Farbe · Zooplan
Attraktivität: n · Artenschutz: n
Besondere Karten: n · Ruf: n
```

Falls Ruf-Erfassung deaktiviert ist:

```text
Besondere Karten: n
```

Fehlende Pflichtwerte werden direkt als `–` dargestellt.

Es gibt keinen zusätzlichen konkurrierenden Text
`Wertung nicht vollständig`.

Berechnete Punkte erscheinen erst, wenn Attraktivität und Artenschutz
vorliegen.

Das Datum der Partie wird im Kopf der Wertung angezeigt.

Der Einstieg zu besonderen Karten ist ein neutraler kompakter Icon-Button mit
komfortabler Touch-Fläche. Die Anzahl steht **nur in der Informationszeile**,
nicht als Counter-Badge am Icon.

Nach Auswahl oder Entfernen einer Karte schließt sich die Auswahl direkt.

Spieler-Taps werden gegen schnelle Mehrfachtaps geschützt.

### Punkte

Die Wertung verwendet ein festes Artenschutz-Lookup.

```text
Neue Punkte = Attraktivität + Artenschutz-Addend
Alte Punkte = Neue Punkte - 100
```

Berechnete Punkte werden nicht gespeichert.

Die Anzeige `neu/alt` ist eine globale lokale Einstellung und verändert keine
historischen Rohdaten.

### Besondere Karten

Gespeichert werden konkrete Karten:

- Afrikanischer Elefant
- Asiatischer Elefant
- Weißkopfseeadler
- Steinadler
- Malaienbär

Für spätere Statistik gehört der Malaienbär zur Adler-Kategorie.

Regeln:

- dieselbe konkrete Karte darf demselben Spieler nicht doppelt zugeordnet sein,
- dieselbe Karte darf in seltenen Fällen mehreren Spielern derselben Session
  gehören,
- eine Mehrfachzuordnung an einen weiteren Spieler erfordert explizite
  Bestätigung.

### Wertung abschließen

`Wertung abschließen` ist sichtbar deaktiviert, solange bei mindestens einem
Spieler Attraktivität oder Artenschutz fehlt.

Nach vollständiger Pflichtwertung:

1. Punkte berechnen,
2. Default-Platzierungen bestimmen,
3. bei Punktgleichstand ggf. `Gleichstände bearbeiten`,
4. Platzierungen speichern,
5. Session auf `Completed` setzen,
6. Ergebnis anzeigen.

## 10. Gleichstände

Competition Ranking:

```text
1,1,3
1,2,2,4
```

Nur Spieler desselben ursprünglichen Punktegleichstands dürfen innerhalb des
zugehörigen ursprünglichen Rangbereichs verschoben werden.

Ein echter Gleichstand darf bestehen bleiben.

Bei einem Dreifachgleichstand sind innerhalb des gültigen Rangbereichs z. B.
möglich:

```text
2,2,2
2,2,4
2,3,3
2,3,4
```

## 11. Ergebnis

Nach Abschluss bleibt der Ergebnis-Screen sichtbar, bis `Fertig` gewählt wird.

Darstellung:

- Platzierung
- Name
- Punkte
- Spielerfarbe
- Zooplan
- Attraktivität
- Artenschutz
- Ruf, falls erfasst
- besondere Karten
- Partiedauer

Spielerkacheln sind dezent in der tatsächlichen Spielerfarbe getönt.

**Verbindliche Kontrastregel:** Auf spielerfarbig getönten Kacheln werden
fachliche Inhalte niemals in grauer Sekundärtextfarbe dargestellt.

## 12. Kamera-/Wertungserkennung

Der Kamera-Button besitzt bereits einen prominenten Platz im Wertungsscreen.

Später soll eine Bildanalyse unterstützte Wertungsmatten erkennen und je
Spielerfarbe:

- Attraktivität,
- Artenschutz,
- Ruf

vorbelegen.

Erkannte Werte bleiben manuell korrigierbar. Es gibt keinen separaten
fachlichen Kamera-Wertungspfad; die Kamera befüllt dieselben Wertungsfelder.

## 13. Statistik

Für reguläre Gruppen mindestens:

- Anzahl Partien
- Siege pro Spieler
- Gewinnquote
- durchschnittliche Platzierung

Spätere Auswertungen können nach Spiel, Gruppe und Person gefiltert werden.

Besondere Karten ermöglichen perspektivisch zusätzliche Arche-Nova-Statistiken,
u. a. Elefanten-/Adler-bezogene Auswertungen.

## 14. Zielgerät und Bedienprinzipien

Primärer Prototyp:

- Smartphone
- 6,1 Zoll
- Hochformat
- Referenzauflösung `1170 × 2532`

Große Touch-Flächen und einhändige Bedienung haben Vorrang vor hoher
Informationsdichte.

Visuelle Detailregeln stehen in `UI_DESIGN_GUIDE.md`.
