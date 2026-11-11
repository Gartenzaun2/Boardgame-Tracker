# Manuelle Spiele – Screens und Navigation

Gemeinsame Header, Capsule-Geometrie, Themes, PlayerColor-Flächen, Textkontraste, Touchflächen, Buttons, `SelectionDialog`, Sicherheitsdialoge und Zurück-Navigation gelten unverändert gemäß [../../UI_WIREFRAME.md](../../UI_WIREFRAME.md) und [../../UI_DESIGN_GUIDE.md](../../UI_DESIGN_GUIDE.md). Keine eigenen Theme-Tokens. Wertungsregeln stehen in [PRODUCT_SPEC.md](PRODUCT_SPEC.md), nicht hier.

## 1. Spiel anlegen / Einstellungen

**Spiel anlegen** öffnet den ManualGames-Editor mit Name, kurzem Capsule-Tag, Min-/Max-Spielerzahl und den Wertungseinstellungen. Der Dialog ist kein separater Arche-Nova-Modus. Abhängige Optionen werden **ausgeblendet**, nicht deaktiviert stehen gelassen:

- **Punkte erfassen** → **Negative Punkte erlauben**.
- **Sieger erfassen** → **Platzierungen erfassen**, **Gleichstände erlauben**.
- **Punkte + Sieger** → **Wertung aus Punkten**: manuell / höchste / niedrigste Punktzahl.
- Unabhängig: **Spielerreihenfolge erfassen**, **Spielerfarben erfassen**.

Die Seite nutzt flache gemeinsame Einstellungszeilen, Switches und Lookups. Keine dekorativen Erläuterungstexte; Zusatzinformationen über `Info`, echte Fehler und Sperrgründe bleiben sichtbar.

Bearbeiten nur unter **Einstellungen → Spiele verwalten → [manuelles Spiel]**; dort **Spiel anzeigen** zuerst, anschließend Name, Capsule-Tag und bearbeitbare Konfiguration sowie **Spiel löschen** (nur ohne Partien, sonst gesperrt). Die Spieleübersicht erhält keinen Bearbeiten-Stift.

## 2. Partie vorbereiten

Unterseitenheader **Partie vorbereiten**, Capsule mit konfiguriertem Tag, Gruppenname, Divider; Würfelicon rechts **nur** bei erfasster Reihenfolge.

1. **Letzte Partie** ohne Ergebniskarte, immer Datum; falls vorhanden vollständige Plätze, sonst Pokal + Gewinner, sonst Spieler + Punkte. Bei vielen Teilnehmern dürfen die Textbestandteile umbrechen. Bei fehlender Historie „Noch keine Partie“.
2. **Reihenfolge rotieren** nur bei aktiver Reihenfolgeerfassung; Würfel rotiert zu einem zufälligen Startspieler.
3. Spieler in Karten: Name, Kreis mit Positionsnummer bzw. leer bei nicht erfasster Reihenfolge, optional Palette und Hoch/Runter (Grenzaktionen deaktiviert). Ohne Reihenfolgeerfassung alphabetisch, keine Änderungsknöpfe. Bei Farboption sind **ganze Karten** mit der jeweiligen `PlayerColor`-Fläche und passendem Kontrasttext eingefärbt; ansonsten neutrale Karten. Farbpalette nach gemeinsamem Muster einschließlich Farbtausch.
4. Unten **Starten** als Primary.

## 3. Laufende Partie

Unterseitenheader **Partie**, Capsule, Gruppe, kompakte Spieldauer im gemeinsamen Header; Startzeit höchstens als kompakte Metainformation. Darunter dieselben Spieler-Karten ohne Aktionen (optionale Farben und Reihenfolgenummern). Unten **Partie verwerfen** (Danger/Secondary) **links neben** **Partie beenden** (Primary) rechts; Verwerfen nur nach Sicherheitsdialog.

## 4. Punkte erfassen (optional)

Unterseitenheader **Wertung**, Capsule, Gruppe. Nur bei aktivierter Punkteerfassung: Spieler-Karten mit Name und direktem numerischem Ganzzahlfeld je Teilnehmer, **ohne** Hoch/Runter, Spinner oder Reihenfolgekreise. Optional vollflächige PlayerColor-Flächen. Die Primäraktion lautet **Weiter**, falls ein weiterer Schritt benötigt wird, sonst **Wertung abschließen**; deaktiviert, bis die Pflichtwerte vollständig sind.

## 5. Genau eine bedingte Auswahlseite

- **Sieger auswählen:** Nur bei manueller Siegerermittlung ohne Platzierung. Alle Spieler als gemeinsames Mehrfachauswahlmuster (Checkbox/ausgewählte Fläche), kein Reihenfolgekreis, kein zusätzlicher Platz-Dialog. Genau ein markierter Sieger, bei erlaubtem Gleichstand mehrere.
- **Plätze zuordnen:** Nur bei manueller vollständiger Platzierung. Alle Spieler und wählbare Platz-Chips `1..n`, keine Reihenfolgekreise. Alle Spieler benötigen einen Platz; Gleichstände nur falls erlaubt.
- **Sieger-Gleichstand auflösen:** Nur bei automatisch ermitteltem Sieger und punktgleicher bester Punktzahl. Eine oder – falls erlaubt – mehrere Personen **nur aus diesem Kandidatenkreis** wählen. Andere Spieler sind nicht bearbeitbar; keine allgemeine Rangmaske.
- **Platz-Gleichstände auflösen:** Nur bei automatisch vollständiger Platzierung und Punktgleichstand. Arche-Nova-Muster mit Platzgruppen und ca. 32-dp-Chips; je Scoreblock nur seine ursprünglichen Platzpositionen verfügbar, Punkte rechts, keine Pfeile/Drag-and-drop/Reihenfolgekreise. Andere Scoreblöcke unverändert.

Die letzte Auswahl bietet **Wertung abschließen** und führt unmittelbar zum Ergebnis. Ohne benötigten Wertungsschritt folgt direkt das Ergebnis. Eine offene Wertung kann sicher verworfen oder später fortgesetzt werden.

## 6. Ergebnis und Korrektur

Header **Ergebnis**, Capsule, Gruppe, Datum und Dauer; bei historisch erfasster `Order` die gemeinsame Startreihenfolge-Metazeile, sonst keine. Ergebnisliste zeigt alle tatsächlich gespeicherten Punkte und volle Plätze oder eine reine Siegerkennzeichnung mit Pokal; ohne Werte nur Teilnehmer. Keine Reihenfolgekreise in der Wertung oder Ergebnisliste. Optional eingefärbte Spielerflächen. **Fertig** verlässt das Ergebnis; historisch geöffnet erscheint dieselbe Seite mit gemeinsamem Edit-Icon. Historische Korrektur verwendet denselben Wertungsablauf für die vorhandenen Ergebnisarten, anschließend **Korrektur speichern**.
