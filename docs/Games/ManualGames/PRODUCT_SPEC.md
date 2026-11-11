# Manuelle Spiele – Produktspezifikation

## 1. Geltungsbereich

`ManualGames` verwaltet beliebig viele vom Benutzer angelegte Spiele. Jedes hat eine eigene Konfiguration, aber alle verwenden denselben ManualGames-Erfassungs- und Ergebnisablauf. Es ist **kein** vereinfachter Modus eines integrierten Spiels und keine generische Spielregeln-Engine.

Gemeinsame Identitäten, Gruppen, Sessions, Farben, Historie, Navigation und Sichtbarkeit: [../../PRODUCT_SPEC.md](../../PRODUCT_SPEC.md). Fachliche Werte: [DATA_MODEL.md](DATA_MODEL.md).

## 2. Anlegen und Verwalten

- **Spiel anlegen** ist über die Spieleübersicht erreichbar; dort gibt es keine Bearbeiten-Aktion an Spielkacheln.
- Bearbeiten erfolgt über **Einstellungen → Spiele verwalten → [Spiel]**, wie bei integrierten Spielen. Dort steht **Spiel anzeigen** zuerst. Liste: Name, Kennzeichnung „Manuelles Spiel“ und bei ausgeblendeten Spielen „Deaktiviert“.
- Spielname und kurzer Capsule-Tag sind beim Anlegen verpflichtend und später änderbar; der Name ist unter manuellen Spielen nach `Trim` und `OrdinalIgnoreCase` eindeutig. Ein gleichnamiges integriertes Spiel ist zulässig.
- `MinPlayers` ist zunächst 1, `MaxPlayers` frei wählbar bis 12. Das Maximum bleibt nach Anlage unveränderlich. Eine Änderung des Minimums darf bestehende Gruppenzuordnungen nicht ungültig machen.
- Ein manuelles Spiel darf ausschließlich gelöscht werden, wenn keine Session irgendeines Status existiert. Zugehörige `GroupGame`-Zuordnungen, Konfigurationen, Farbdefaults und Sichtbarkeits-Preferences werden entfernt, nie aber die referenzierten Gruppen oder Spieler. Andernfalls ist die Löschaktion gesperrt; **Spiel anzeigen** kann stattdessen deaktiviert werden. Löschen benötigt eine Sicherheitsabfrage.

## 3. Konfigurierbare Erfassung

- **Punkte erfassen**: ganze Zahlen; darunter ausschließlich bei Aktivierung **Negative Punkte erlauben**.
- **Sieger erfassen**: darunter ausschließlich bei Aktivierung **Platzierungen erfassen** und **Gleichstände erlauben**. Sieger können allein oder mit vollständigen Platzierungen erfasst werden.
- Nur bei **Punkte + Sieger**: Ermittlung **manuell**, **höchste Punktzahl gewinnt** oder **niedrigste Punktzahl gewinnt**. Bei automatischer Ermittlung entscheidet „Platzierungen erfassen“, ob nur Sieger oder vollständige Platzierungen abgeleitet werden.
- **Spielerreihenfolge erfassen** und **Spielerfarben erfassen** sind unabhängige Schalter. Bei aktiver Farberfassung benötigt jeder Teilnehmer eine eindeutige Farbe aus dem gemeinsamen 12er-Katalog; eine spielindividuell eingeschränkte Farbpalette ist nicht Teil dieses Vertrags.
- Ohne Siegererfassung gibt es keine Platzierungserfassung oder automatische Siegerermittlung. Jede Kombination ohne Ergebniswerte ist zulässig („gespielt“).

Änderungen der Erfassungsregeln gelten nur für neue Partien. Laufende Partien behalten die für ihren Start gültigen Regeln. Historische Ergebnisse, Wertungsarten und Farben werden weder gelöscht noch automatisch umgerechnet. Beim Bearbeiten historischer Partien bleiben die vorhandenen Ergebnisarten bearbeitbar, unabhängig von der aktuellen Konfiguration; nicht historisch vorhandene Ergebnisarten werden dadurch nicht nachträglich angelegt.

## 4. Partievorbereitung und Laufzeit

Die Vorbereitung startet mit den Teilnehmern der gewählten Gruppe. Die zuletzt abgeschlossene Partie dieser Spiel-Gruppe-Kombination erscheint kompakt mit Datum und – nach tatsächlich vorhandenen historischen Daten – vollständigen Platzierungen, sonst Sieger(n) mit Pokal, sonst allen Punkten, sonst nur Datum. Längere Zeilen dürfen umbrechen.

Bei aktivierter Reihenfolge: nummerierte Kreise, Hoch/Runter, Rotation und zufälliger Startspieler (zyklische Rotation, relative Reihenfolge erhalten). Ohne Erfassung: leere Kreise, alphabetische Anzeige, keine Reihenfolgeaktionen; es wird keine Startreihenfolge gespeichert und der globale Gruppen-Default nicht geändert.

Bei aktiven Farben: vollflächig eingefärbte Spielerkacheln, Palette und eindeutige Farbvergabe. Bestehende Gruppen-/Spieldefaults übernehmen, fehlende Farben in der Enum-Reihenfolge (`Blue`, `Yellow`, `Red`, …) ergänzen; Farbwechsel bei bereits belegter Farbe tauscht die Zuordnung. Defaults ändern sich erst beim erfolgreichen Partiestart. Ohne Farbunterstützung bleiben Kacheln neutral.

Die laufende Partie zeigt nur die Spieler, ihre gegebenenfalls erfassten Farben und Reihenfolge sowie die kompakte Spieldauer im gemeinsamen Header. Kein Spieler-Timer, keine Punkte-, Runden- oder Zug-Eingabe. **Partie beenden** fixiert die Endzeit; **Partie verwerfen** erfordert eine Sicherheitsabfrage. Beide Aktionen stehen nebeneinander gemäß gemeinsamem UI-Vertrag.

## 5. Wertungsabläufe

Nach dem Beenden erscheinen **nur** benötigte Schritte, ohne zusätzliche Prüfseite:

1. **Punkte erfassen** ausschließlich bei aktivierter Punkteerfassung: ein einfaches Ganzzahl-Eingabefeld je Spielerkarte, ohne Spinner. Zum Abschluss müssen alle Punkte gesetzt sein.
2. Danach höchstens **eine** der Alternativen:
   - **Sieger auswählen** bei manueller Siegererfassung ohne vollständige Plätze: genau ein Sieger, bei erlaubten Gleichständen auch mehrere.
   - **Plätze zuordnen** bei manueller vollständiger Platzierung: jeder Teilnehmer bekommt einen Platz; Gleichstände nur bei entsprechender Einstellung.
   - **Gleichstand auflösen** bei automatischer Ableitung und tatsächlich relevanten Punktgleichständen.
3. Die letzte benötigte Eingabe endet mit **Wertung abschließen** und öffnet unmittelbar **Ergebnis**. Fehlt jeder Wertungsschritt, erscheint direkt das Ergebnis; der technische Statusweg `Running → Scoring → Completed` bleibt erhalten.

Bei automatisch **nur** ermittelten Siegern ist ausschließlich ein Gleichstand um die beste Punktzahl relevant. Der Nutzer wählt einen Gewinner oder – falls erlaubt – mehrere Gewinner aus den punktgleichen Kandidaten. Bei automatisch **vollständig** ermittelten Platzierungen wird jeder Gleichstandsblock nur innerhalb seines eigenen aus den Scores bestimmten Platzbereichs aufgelöst, wie bei Arche Nova; unbeteiligte Spieler und Punkte bleiben unverändert. Geteilte Plätze und Ranglücken sind möglich.

Unvollständige Eingaben dürfen während `Scoring` zwischengespeichert und fortgesetzt werden; sie gelten noch nicht als abgeschlossene Wertung. Vor `Completed` müssen alle für **diese Partie** konfigurierten Angaben vollständig und gültig sein. Auch eine begonnene Wertung kann nach Sicherheitsabfrage verworfen werden.

## 6. Ergebnis, Historie und Statistik

Das Ergebnis zeigt ausschließlich die tatsächlich vorhandenen Daten: Datum, Dauer, optionale historische Reihenfolge, optionale Farben, gegebenenfalls Punkte und vollständige Plätze bzw. Sieger mit Pokal. Kein nachgeschalteter Prüfschritt. Historische Korrekturen folgen dem gemeinsamen Ergebnis-/Bearbeitungsmuster.

Auswertungen berücksichtigen nur Partien mit den jeweils benötigten historischen Informationen: Teilnahme benötigt nur `Completed`; Siegerstatistiken benötigen tatsächlich erfasste Sieger; durchschnittliche Platzierung benötigt vollständige Platzierung; Punktauswertungen benötigen vollständige Punktdaten. Fehlende Werte sind **nicht** als Niederlage, Platz oder null Punkte zu interpretieren.

Import und Export: [IMPORT_EXPORT.md](IMPORT_EXPORT.md). Die aktuelle Spielkonfiguration darf historisch gültige importierte Ergebnisse nicht einschränken.
