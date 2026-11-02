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
darin nicht erneut angeboten. Ob **Starten** verfügbar ist, folgt den
Vorbereitungsregeln aus `PRODUCT_SPEC.md`.

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
Historische Korrektur verwendet denselben Aufbau. Sichtbarkeit und Erhalt eines
vorhandenen Rufwerts folgen `PRODUCT_SPEC.md`.

## 6. Spezialkartenwahl

Page-interner `SelectionDialog`; jeder Eintrag zeigt `Symbol1 → Symbol2 → Symbol3 → Name`.
Zuordnungen zum Zielspieler und zu anderen Spielern besitzen getrennt sichtbare
Zustände gemäß `UI_DESIGN_GUIDE.md`.

Archivierte Karten erscheinen nur, wenn sie für den Zielspieler bereits relevant
sind. Ein Tap führt die zugehörige Zuordnungsaktion aus und schließt; nötige
Bestätigung liegt darüber als App-Dialog.

## 7. Gleichstände bearbeiten

Jeder Platz `1..n` besitzt eine neutrale Überschrift; auch leere zulässige Zielgruppen bleiben sichtbar. Darunter Spielerzeilen mit Name, erlaubten Platz-Chips und rechtsbündigen Punkten. Die Platznummer wird nicht nochmals am Namen angezeigt.

Platz-Chips sind ca. **32 dp**, zeigen nur die Zahl und markieren den eigenen Zustand sichtbar. Tap verschiebt nur diese Spielerzeile in die gewählte Platzgruppe. Keine Pfeile, kein Drag & Drop.

## 8. Ergebnis und historische Wertung

Ergebnis zeigt je Spieler **Placement/Name/Punkte**, Zooplan, Rohwerte und Spezialkarten. Nach regulärem Abschluss bleibt der Screen bis **Fertig** geöffnet; **Fertig** führt zu Gruppen.

Historisch geöffnet ist dieselbe Ergebnisansicht read-only. Header: Zurück · **Ergebnis** · Edit-Icon (semantisch **Ergebnis bearbeiten**), kein Bearbeiten-Textbutton. Header-/System-Zurück führt zum aufrufenden Screen: Historie oder Einstellungen → Spieler.

Edit öffnet die bestehende historische Wertung mit **Korrektur speichern** und **Partie löschen**. Spieler-Tap öffnet die Spielereingabe; Zurück verwirft ungespeicherte Korrekturen und zeigt wieder das Ergebnis. **Korrektur speichern** führt zum aktualisierten Ergebnis derselben Partie zurück, bei nötiger Gleichstandsauflösung über denselben Gleichstandsscreen. Das ursprüngliche Rückziel des Ergebnisses bleibt dabei erhalten.

## 9. Arche-Nova-Einstellungen

Sichtbar: **Ruf erfassen · Neue Wertung/Alte Wertung · Spielmaterial/Sprache + Info · Spezialkarten**. Der Sprachdialog zeigt aktuell nur **Deutsch**; `Nutzer` ist keine Sprachoption.

Info bei Spielmaterial: „Betrifft nur sprachabhängiges Spielmaterial. Die App-Sprache bleibt unverändert.“

## 10. Spezialkartenverwaltung

Liste: Header Zurück · **Spezialkarten** · Info; darunter **Archivierte anzeigen**, scrollbare Kartenliste und unten **Neue Spezialkarte**. Eintrag zeigt 0–3 Symbole in Reihenfolge, Name, optional **Archiviert** und direkt das Bearbeiten-Icon. Keine Lösch-/Archivaktion in der Liste.

Create/Edit: Name, Vorschau `Symbol1 → Symbol2 → Symbol3 → Name`, drei nummerierte Symbolpositionen, bei Edit zusätzlich Archiv-Switch, unten **Speichern**. Symbolwahl öffnet `SelectionDialog` mit Grafik, Name und sichtbarer aktueller Auswahl; Wiederholungen sind möglich. Nach Leeren einer Position schließen sichtbare Folgepositionen lückenlos auf.

## 11. Fotoquelle und Review

Kamera-Aktion öffnet **Foto aufnehmen** oder **Vorhandenes Foto auswählen**. Danach liegt ein isolierter Review über der deaktivierten Wertung.

Der Review-Header zeigt Zurück · **Fotowerte prüfen**. Direkt darunter steht eine kompakte Hinweiszeile aus Info-Icon und anschließendem Hinweistext; es gibt keinen separaten Infotextblock.

Pro Spieler stehen die erkannten Werte in kompakten Zeilen. Jede Wertzeile zeigt das Label, direkt daneben den Sicherheitsstatus `✓ Sicher`, `! Prüfen` oder `? Unsicher` und rechts den editierbaren Wert. Der Sicherheitsstatus erhält keine eigene Zeile. Bei aktivem **Ruf erfassen** gilt derselbe Aufbau für Ruf.

Unten steht nur **Bestätigen/Speichern**. Header-Zurück oder System-Zurück beendet den Review ohne Bestätigung, verwirft die unbestätigten Fotovorschläge und zeigt wieder die Wertung. Im Review gibt es keinen unteren **Abbrechen**-Button.

## 12. Back-Navigation

System-/Android-Zurück schließt zuerst die oberste modale Ebene. Bei Spezialkarten also: Bestätigungs-/Fehlerdialog → Spezialkartenwahl → Wertungsseite. Beim Foto-Review entspricht Zurück der Header-Zurück-Aktion aus Abschnitt 11. Zurück bestätigt keine Auswahl oder destruktive Aktion.

## 13. Quellen

Allgemeine Screens: `docs/UI_WIREFRAME.md` · Produkt: `docs/Games/ArkNova/PRODUCT_SPEC.md` · UI: `docs/Games/ArkNova/UI_DESIGN_GUIDE.md` · Datenmodell: `docs/Games/ArkNova/DATA_MODEL.md` · CV: `docs/Games/ArkNova/CV_STATUS.md`.

## 14. Debug → Bilderkennungsdaten

Unter Arche-Nova-Einstellungen steht ein Abschnitt **Debug** mit dem Einstieg
**Bilderkennungsdaten**. Der Screen besitzt Header-Zurück, Titel und Divider sowie
scrollbaren Inhalt: lokaler Datenschutzhinweis, **Sammlung aktiv** als Switch,
Anzahl gespeicherter Aufnahmen und Speicher in MB, **Dataset als ZIP exportieren**
und die getrennte Danger-Aktion **Aufnahmen löschen**. Bei leerer Sammlung sind
Export und Löschen deaktiviert. Laufende Operationen sperren die Aktionen;
Fehler bleiben als verständliche Meldung auf dem Screen.

Beim Deaktivieren mit Daten öffnet eine App-Auswahl **Sammlung deaktivieren** mit
**Aufnahmen behalten** und **Aufnahmen löschen**. Header-Zurück bricht die Änderung
ab; es gibt keinen unteren Abbrechen-Button. Separates Löschen benötigt einen
Bestätigungsdialog. Export öffnet den System-Share-Sheet für das ZIP.
