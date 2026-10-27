# Boardgame Tracker – UI / Wireframes

Dieses Dokument beschreibt den **aktuellen Zielaufbau** der Screens. Es ist kein
Versionsprotokoll.

## 1. Hauptnavigation

```text
┌──────────────────────────────────────┐
│              Seiteninhalt            │
├──────────────────────────────────────┤
│ Spiele | Gruppen | Historie | Einst. │
└──────────────────────────────────────┘
```

Reihenfolge:

```text
Spiele · Gruppen · Historie · Einstellungen
```

## 2. App-Start

### Kein Kontext, kein Standardspiel

```text
Spiele
```

### Kein Kontext, Standardspiel gesetzt

```text
Gruppen
Arche Nova
```

### Letzte offene Session

Direkt die konkrete laufende Partie oder Wertung.

### Letzter Hauptbereich

Wiederherstellen, sofern der nötige Spielkontext gültig ist.

## 3. Spiele

```text
Spiele                                      [i]

┌──────────────────────────────────────┐
│ Arche Nova                           │
│ vollständig unterstützt             │
└──────────────────────────────────────┘
```

Tap:

```text
Spielkontext setzen -> Gruppen
```

Längere Erklärungen stehen hinter `[i]`.

## 4. Gruppen

```text
Gruppen                             [ + ]
Arche Nova

[Temporär] [Archiviert]

┌──────────────────────────────────────┐
│ Freitagabend              ●      [✎]│
│ Anna · Ben · Chris                  │
│ Letzte Partie: 24.10.2026           │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│ Sonntag                         [✎]  │
│ Claudi · Gernot                     │
│ Keine Partie vorhanden              │
└──────────────────────────────────────┘
```

`●` = offene Session im **aktuellen Spiel**.

Tap Gruppenkarte:

```text
offene Session? -> fortsetzen
sonst           -> Partie vorbereiten
```

Tap `[✎]`:

```text
Gruppe bearbeiten
```

## 5. Gruppe anlegen

```text
← Gruppe anlegen [i]              Speichern

Gruppenname        [ Freitagabend      ]

Temporäre Gruppe   [ ○ ]

Gruppenmitglieder [i]

1. Anna                         ↑ ↓ ×
2. Ben                          ↑ ↓ ×

Vorhandene Person hinzufügen
[ Person auswählen ▼ ]             [+]

Neue Person
[ Name                              ]
Gast [○]                  [Hinzufügen]
```

Ohne weitere vorhandene Person:

```text
Vorhandene Person hinzufügen
Keine weiteren Personen vorhanden.
```

## 6. Gruppe bearbeiten

```text
← Gruppe bearbeiten [i]           Speichern

Gruppenname        [ Freitagabend ]

Status / Archivierung

Gruppenmitglieder [i]
1. Anna                         ↑ ↓
2. Ben                          ↑ ↓

Arche-Nova-Defaultfarben
Anna   Blau
Ben    Rot
```

Die Mitgliedermenge wird nicht beliebig verändert.

## 7. Partie vorbereiten

```text
← Partie vorbereiten                 [🎲]
   Freitagabend

Letzte Partie: 24.10.2026
1. Anna
2. Ben
3. Chris

[ Zoopläne auslosen ] [ Reihenfolge rotieren ]

┌──────────────────────────────────────┐
│ 1 Anna                         [🎨] │
│   Blau                              │
│ ┌────────────┐ ┌────────────┐       │
│ │  Plan 3    │ │ Plan 11    │       │
│ │  Skizze    │ │ Skizze     │       │
│ └────────────┘ └────────────┘       │
└──────────────────────────────────────┘

... weitere Spieler ...

[ Starten – deaktiviert ]
```

Aktiv erst wenn:

- alle Spieler einen Zooplan gewählt haben,
- finale Farben eindeutig sind,
- alle fachlichen Startbedingungen erfüllt sind.

## 8. Laufende Partie

```text
Laufende Partie
Freitagabend
Dauer: 42 min

┌──────────────────────────────────────┐
│ 1 Anna · Blau              Plan 3   │
└──────────────────────────────────────┘
┌──────────────────────────────────────┐
│ 2 Ben · Rot                Plan 9   │
└──────────────────────────────────────┘

[ Partie beenden ]

Verwerfen
```

Spielerkarten dezent in der tatsächlichen Spielerfarbe.

## 9. Wertungsübersicht

```text
Wertung
Freitagabend
27.10.2026

┌──────────────────────────────────────────┐
│ Claudi · 93 Punkte            [Karten]  │
│ Schwarz · Auffangstation                 │
│ Attraktivität: 121 · Artenschutz: 35     │
│ Besondere Karten: 1 · Ruf: 4             │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│ Gernot                         [Karten]  │
│ Gelb · Reißbrett                        │
│ Attraktivität: – · Artenschutz: –        │
│ Besondere Karten: 0 · Ruf: –             │
└──────────────────────────────────────────┘

[ Kamera ]

[ Wertung abschließen – deaktiviert ]

Verwerfen
```

Hinweis: Attraktivitätswerte oberhalb 113 sind fachlich ungültig; die Skizze
zeigt nur die Informationshierarchie, nicht einen gültigen Beispieldatensatz.

`Wertung abschließen` wird erst bei vollständigen Pflichtwerten aktiv.

Keine Counter-Badge-Zahl am Karten-Icon.

## 10. Spielerwertung

```text
← Claudi

Attraktivität
[ 72 ]

Artenschutz
[ 16 ]

Ruf
[ 4 ]
```

Ruf nur sichtbar, wenn `Ruf erfassen` aktiv ist.

Gültige Änderungen werden debounced automatisch gespeichert.

## 11. Besondere Karten

Tap auf das Karten-Icon öffnet eine kompakte Auswahl:

```text
Besondere Karten – Claudi

┌──────────────────────────────────────┐
│ Afrika · 🐘                         │
│ Afrikanischer Elefant               │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│ Asien · 🐘                          │
│ Asiatischer Elefant                 │
└──────────────────────────────────────┘
```

Bereits anderweitig zugewiesene Karten werden gekennzeichnet.

Bei Auswahl für einen zweiten Spieler:

```text
Diese Karte ist bereits zugeordnet.
Wirklich auch Claudi zuordnen?
```

Nach Zuordnung oder Entfernen schließt die Auswahl.

## 12. Gleichstände bearbeiten

Alle Platzierungsrahmen bleiben sichtbar.

```text
1.
┌ Anna ┐

2.
┌ Ben   ┐
┌ Claudi┐

4.
┌ Gernot┐
```

Nur Spieler eines echten Punktegleichstands sind innerhalb des ursprünglichen
Rangbereichs verschiebbar.

Echter Gleichstand kann erhalten bleiben.

## 13. Ergebnis

```text
Ergebnis
Freitagabend
27.10.2026
Dauer: 2 h 14 min

┌──────────────────────────────────────┐
│ 1. Claudi · 93 Punkte               │
│ Schwarz · Auffangstation             │
│ Attraktivität: 72 · Artenschutz: 35  │
│ Besondere Karten: 1 · Ruf: 4         │
└──────────────────────────────────────┘

┌──────────────────────────────────────┐
│ 2. Gernot · 87 Punkte               │
│ Gelb · Reißbrett                    │
│ Attraktivität: 68 · Artenschutz: 33  │
│ Besondere Karten: 0 · Ruf: 3         │
└──────────────────────────────────────┘

[ Fertig ]
```

Jede Kachel ist dezent in der tatsächlichen Spielerfarbe getönt.

Alle fachlichen Inhalte auf diesen Kacheln verwenden kontrastreichen Primärtext,
niemals grauen Sekundärtext.

## 14. Einstellungen

```text
Einstellungen                              [i]

Standardspiel
[ Arche Nova ▼ ]

Arche Nova
[ Arche-Nova-Einstellungen ]
```

Längere Erklärungen stehen hinter `[i]`.

### Arche-Nova-Einstellungen

```text
← Arche Nova

Ruf erfassen          [○]

Punkteanzeige
(●) Neue Wertung
( ) Alte Wertung
```

## 15. Historie

Aktuell Platzhalter-Hauptbereich.

Ziel:

```text
Historie

┌──────────────────────────────────────┐
│ Arche Nova · Freitagabend           │
│ 24.10.2026 · 2 h 14 min             │
│ Gewinner: Claudi                    │
└──────────────────────────────────────┘
```

## 16. Kameraerkennung – späterer Slice

Wertungsscreen behält einen prominenten Kamera-Einstieg.

Später:

```text
Kamera -> Wertungsmatte erkennen
       -> Spielerfarben zuordnen
       -> Attraktivität/Artenschutz/Ruf vorbefüllen
       -> manuell korrigieren
```

Kein eigener fachlicher Ergebnisweg.
