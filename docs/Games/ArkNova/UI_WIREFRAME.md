# Arche Nova – Screens und Navigation

## 1. Dokumentgrenze

Diese Datei ergänzt `docs/UI_WIREFRAME.md` nur um **Arche-Nova-spezifische Screenstruktur und Navigation**. Scoring-/Speicherregeln stehen in `PRODUCT_SPEC.md` und `DATA_MODEL.md`, Darstellung in `UI_DESIGN_GUIDE.md`, CV-Forschungsstatus in `CV_STATUS.md`.

## 2. Partie vorbereiten

Header: Zurück · **Partie vorbereiten** · Gruppenname; rechts Würfelbutton.

Danach in dieser Reihenfolge:
1. **Letzte Partie** mit Datum/Placements oder Leerzustand, ohne Karte;
2. Secondary-Aktionen **Zoopläne auslosen** und **Reihenfolge rotieren**;
3. Spielerkacheln in aktueller Partiereihenfolge;
4. unten **Starten** als Primary.

Spielerkachel: Positionsnummer, Name, Palette/Farbstatus, zwei Zooplankacheln nebeneinander sowie Hoch/Runter. Hoch/Runter verschiebt eine Position; die unmögliche Richtung am Listenende ist deaktiviert.

Zooplankacheln sind quadratisch, verwenden die Skizze mit `Fill`, zeigen die Nummer und keinen zusätzlichen Namen. Initial ist keine ausgewählt; Tap markiert die ganze Kachel.

Die Partiefarbauswahl öffnet sich über Palette; die aktuell gesetzte Farbe wird
darin nicht erneut angeboten. Doppelte Farben dürfen im Entwurf vorübergehend
sichtbar sein, blockieren aber **Starten**. Weitere Validierungsregeln folgen
`PRODUCT_SPEC.md`.

## 3. Laufende Partie

Header zeigt Gruppe und bisherige Dauer. Darunter Spieler in Startreihenfolge mit Name, Farbe und gewähltem Zooplan. Unten **Partie beenden** als Primary und **Verwerfen** als nachgeordnete destruktive Aktion.

## 4. Wertungsübersicht

Pro Spielerkarte vier Informationszeilen: **Name/Punkte · Farbe/Zooplan · Attraktivität/Artenschutz · Spezialkartenanzahl/optional Ruf**. Das Spezialkarten-Icon selbst trägt keinen Zähler.

Navigation: Kartenfläche → Spielereingabe; Spezialkarten-Icon → Kartenwahl; Kamera → Fotoquelle/Review während `Scoring`; Info → Vorschlagsstatus; Einstellungen → Arche-Nova-Einstellungen.

## 5. Spielereingabe

Unter Header/Spielername liegen **Attraktivität** und **Artenschutz** in zwei gleich breiten Spalten.

Verbindliche Maße:
| Element | Maß |
|---|---:|
| Abstand der Spalten | 32 dp |
| Zahlenrad-Höhe | 176 dp |
| Zahlenfeld-Höhe | 48 dp |
| Rundung Rad/Feld | 16 dp |
| Rad → Zahlenfeld | 16 dp |
| Eingabeblock → Punkte | 32 dp |

Rad und Zahlenfeld einer Spalte sind gleich breit; Bezeichnungen, Wert und
Vorschlagsstatus stehen zentriert. Außenkanten fluchten mit Punktefläche und
**Fertig**. Bei aktivem **Ruf erfassen** folgt unter den Punkten ein horizontaler
Ruf-Slider mit editierbarem Zahlenfeld, ohne zusätzlichen Erfassungsschalter. Bei
Tastatur/großer Schrift bleibt der Inhalt scrollbar und **Fertig** unten erreichbar.
Historische Korrektur verwendet denselben Aufbau. Ist Ruf dort aktuell verborgen,
bleibt ein bereits gespeicherter Ruf im Korrekturentwurf erhalten.

## 6. Spezialkartenwahl

Page-interner `SelectionDialog`; jeder Eintrag zeigt `Symbol1 → Symbol2 → Symbol3 → Name`.

| Zustand | Fläche | Rahmen |
|---|---|---|
| nicht zugeordnet | `Surface` | normale `Outline` |
| Zielspieler | `SelectedSurface` | normale `Outline` |
| anderer Spieler | `Surface` | gestrichelt `Danger` |
| Zielspieler + anderer | `SelectedSurface` | gestrichelt `Danger` |

Danger-Rahmen: **2 dp**, Muster **4/3**. Archivierte Karten erscheinen nur, wenn sie für den Zielspieler bereits relevant sind. Ein Tap führt die zugehörige Zuordnungsaktion aus und schließt; nötige Bestätigung liegt darüber als App-Dialog.

## 7. Gleichstände bearbeiten

Jeder Platz `1..n` besitzt eine neutrale Überschrift; auch leere zulässige Zielgruppen bleiben sichtbar. Darunter Spielerzeilen mit Name, erlaubten Platz-Chips und rechtsbündigen Punkten. Die Platznummer wird nicht nochmals am Namen angezeigt.

Platz-Chips sind ca. **32 dp**, zeigen nur die Zahl und markieren den eigenen Zustand sichtbar. Tap verschiebt nur diese Spielerzeile in die gewählte Platzgruppe. Keine Pfeile, kein Drag & Drop.

## 8. Ergebnis und historische Wertung

Ergebnis zeigt je Spieler **Placement/Name/Punkte**, Zooplan, Rohwerte und Spezialkarten. Der Screen bleibt bis **Fertig** geöffnet.

Historische Wertung verwendet denselben Aufbau und ergänzt
**Korrektur speichern** sowie **Partie löschen**. Bearbeiten führt zur
Spielereingabe; Zurück verwirft ungespeicherte Korrekturen. Falls nötig öffnet
**Korrektur speichern** anschließend denselben Gleichstandsscreen.

## 9. Arche-Nova-Einstellungen

Sichtbar: **Ruf erfassen · Neue Wertung/Alte Wertung · Spielmaterial/Sprache + Info · Spezialkarten**. Der Sprachdialog zeigt aktuell nur **Deutsch**; `Nutzer` ist keine Sprachoption.

Info bei Spielmaterial: „Betrifft nur sprachabhängiges Spielmaterial. Die App-Sprache bleibt unverändert.“

## 10. Spezialkartenverwaltung

Liste: Header Zurück · **Spezialkarten** · Info; darunter **Archivierte anzeigen**, scrollbare Kartenliste und unten **Neue Spezialkarte**. Eintrag zeigt 0–3 Symbole in Reihenfolge, Name, optional **Archiviert** und direkt das Bearbeiten-Icon. Keine Lösch-/Archivaktion in der Liste.

Create/Edit: Name, Vorschau `Symbol1 → Symbol2 → Symbol3 → Name`, drei nummerierte Symbolpositionen, bei Edit zusätzlich Archiv-Switch, unten **Speichern**. Symbolwahl öffnet `SelectionDialog` mit Grafik, Name und sichtbarer aktueller Auswahl; Wiederholungen sind möglich. Nach Leeren einer Position schließen sichtbare Folgepositionen lückenlos auf.

## 11. Fotoquelle und Review

Kamera-Aktion öffnet **Foto aufnehmen** oder **Vorhandenes Foto auswählen**. Danach liegt ein isolierter Review über der deaktivierten Wertung.

Jede Zeile zeigt Spieler/Wert plus `✓ Sicher`, `! Prüfen` oder `? Unsicher`; Werte sind editierbar. Unten **Bestätigen/Speichern · Abbrechen**.

## 12. Back-Navigation

System-/Android-Zurück schließt zuerst die oberste modale Ebene. Bei Spezialkarten also: Bestätigungs-/Fehlerdialog → Spezialkartenwahl → Wertungsseite. Zurück bestätigt keine Auswahl oder destruktive Aktion.

## 13. Quellen

Allgemeine Screens: `docs/UI_WIREFRAME.md` · Produkt: `docs/Games/ArkNova/PRODUCT_SPEC.md` · UI: `docs/Games/ArkNova/UI_DESIGN_GUIDE.md` · Datenmodell: `docs/Games/ArkNova/DATA_MODEL.md` · CV: `docs/Games/ArkNova/CV_STATUS.md`.
