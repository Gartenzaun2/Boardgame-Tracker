# Changelog

`CHANGELOG.md` ist die **historische Versionschronik** des Boardgame Trackers.
Es beschreibt, was sich in einem Stand geändert hat, nicht den aktuellen
Sollzustand.

Für den aktuellen Stand gelten ausschließlich `AGENTS.md`, `SETUP.md` und die
kanonischen Spezifikationen unter `docs/`. Historische Entscheidungen können durch
spätere Einträge überholt sein.

## Regel für neue Einträge

Ein Eintrag enthält nur historisch relevante Änderungen, insbesondere:

- sichtbares Produktverhalten oder relevante Fehlerbehebungen;
- Architektur-, Datenmodell- oder Vertragsänderungen;
- neue bzw. entfernte Integrationen oder Abhängigkeiten;
- Kompatibilitätsbrüche, notwendige Neuinstallation oder möglicher Datenverlust;
- wesentliche Forschungs-/Freigabeentscheidungen, wenn sie den Projektstand ändern.

Nicht aufnehmen:

- Commit-, Push-, Staging- oder lokalen Arbeitsbaumstatus;
- normale Build-/Testzählungen und wiederholte „alles erfolgreich“-Nachweise;
- Tätigkeitschronologien, ausgeführte Einzelbefehle oder Agentenabläufe;
- temporäre lokale Dateien oder Bereinigungsschritte ohne dauerhafte Projektwirkung;
- ausführliche Messdaten, wenn ein versionierter Report die kanonische Evidenz ist;
- Wiederholungen unveränderter Regeln oder Zustände.

Verifikation wird nur erwähnt, wenn ihr Ergebnis selbst historisch relevant ist,
z. B. ein bewusst fehlgeschlagenes Freigabegate oder ein erstmals bestätigter
Plattformnachweis.

Pro Version möglichst wenige, aussagekräftige Punkte. Details bleiben in den
damals zuständigen Reports oder Dokumenten.

## Historischer Bestand

Die folgenden Einträge entstanden teilweise vor dieser Regel und bleiben als
historische Aufzeichnung unverändert erhalten.

## 0.19.0.0 – 2026-11-11

- ManualGames verwaltet beliebig viele manuell angelegte Spiele mit Name, Capsule-Tag, Spielergrenzen und normalisierter Erfassungskonfiguration; ein Modul löst deren lokale Spielschlüssel dynamisch auf.
- Spielanlage über die Spieleübersicht; Bearbeiten, Anzeigen/Ausblenden und abgesichertes Löschen über Spiele verwalten. Manuelle Namen bleiben auch bei ausgeblendeten Spielen eindeutig, das Spielermaximum bleibt nach Anlage fest.
- Sichtbare manuelle Spiele erscheinen vorübergehend deaktiviert in der Spieleübersicht. Der Partieablauf ist noch nicht implementiert; keine Platzhalterseiten, Dialoge oder Sessions.
- Neues Entwicklungsschema benötigt bei vorhandenen Datenbanken einen bewussten Neuaufbau nach .bgt-Sicherung. Keine automatische Migration oder Datenlöschung; bestehende Arche-Nova-Transferformate bleiben unverändert.

## 0.18.0.2 – 2026-11-11

- Historische Startreihenfolgen sind optional und bleiben entweder vollständig erfasst oder vollständig null; Gruppendefaults ändern sich nur bei tatsächlich erfasster Reihenfolge.
- Gemeinsame Gruppen-/Spiel- und Sessionfarbzuordnungen ersetzen die Arche-Nova-spezifische Farbpersistenz. Arche Nova erfasst weiterhin seine vier Farben und eine vollständige Reihenfolge; Defaults und historische Farben werden beim Start atomar gespeichert.
- BGT-Format 1 sowie Arche-Nova-Export mit Schema 3 und Import mit Schema 2/3 bleiben kompatibel; Transferfarben werden in den gemeinsamen historischen Farbzuordnungen gespeichert.
- Das geänderte Entwicklungsschema erfordert bei vorhandenen Datenbanken einen bewussten Neuaufbau nach .bgt-Sicherung. EnsureCreated bleibt unverändert; kein automatisches Upgrade und keine automatische Datenlöschung.

## 0.18.0.1 – 2026-11-11

- Gemeinsames Game-Modell persistiert Name, Badge-Text und Spielergrenzen; integrierte Stammdaten werden idempotent synchronisiert. Allgemeine Ansichten und neue Gruppenzuordnungen verwenden diese Werte.
- Historische Importe bleiben bei abweichenden heutigen Spielergrenzen erhalten; unzulässige neue Gruppenzuordnungen entfallen. Bestehende Arche-Nova-Transferformate und Schemaversionen bleiben unverändert.
- Neues Entwicklungsschema benötigt einen bewussten lokalen Datenbank-Neuaufbau nach Sicherung abgeschlossener Partien per .bgt-Export. EnsureCreated bleibt bestehen; vorhandene Datenbanken werden weder migriert noch automatisch gelöscht.

## 0.18.0.0 – 2026-11-10

- Arche-Nova-Partien erlauben vor der ersten Auslosung die manuelle Zooplanwahl einschließlich Mehrfachvergabe und der Sonderpläne Turnier, 0 und A. Eine Auslosung ersetzt alle bisherigen Zuordnungen und sperrt die manuelle Wahl für diesen Entwurf.
- Sessions speichern ausschließlich den tatsächlich gewählten Zooplan. Das inkompatible Entwicklungsschema erfordert gemäß Pre-Alpha-Strategie einen bewusst manuell vorgenommenen Datenbank-Neuaufbau; es gibt keine Migration und keinen automatischen Reset. Abgeschlossene Partien lassen sich vorher als .bgt sichern und anschließend importieren.
- Neue Arche-Nova-Exporte verwenden Schema 3. Schema-2-Importe validieren die historischen Angebote und übernehmen anschließend ausschließlich den tatsächlich gewählten Plan; das allgemeine BGT-Format bleibt Version 1.

## 0.17.4.0 – 2026-11-09

- Die Arche-Nova-Vorbereitung verwendet für das maximale Spielerlimit durchgängig das bestehende Modulmetadatum `ArkNovaGame.PlayerLimit`; die redundante Setup-Konstante entfällt.

## 0.17.3.1 – 2026-11-08

- Neue Gruppe-Spiel-Zuordnungen schützen ihr Spielerlimit über eine Core-Factory; die Application stellt vollständige Mitgliedschaften und registrierte Limits bereit. Die globale DbContext-Validierung entfällt, bestehende Transaktionsgrenzen und UI-Fehlermeldungen bleiben erhalten.

## 0.17.3.0

- Gruppe-Spiel-Zuordnungen beachten verbindlich die maximale Spielerzahl des Spielmoduls, auch bei Übernahme, Reaktivierung und Import. Gruppenauswahl und Spieleauswahl berücksichtigen das Limit; Arche Nova unterstützt maximal vier Mitglieder.

## 0.17.2.0

- Globaler Architekturtest schützt die Spielmodulgrenzen in Core, Application und App einschließlich Modul-Ownership, Spielrouten und Spielidentitätsentscheidungen; öffentliche Modulregistrierung und gemeinsame Partial-Typen bleiben zulässig.

## 0.17.1.3

- Königsblau durch TFM (hell) mit Polar-Mars-/Balanced-Copper-Palette ersetzt; interne Theme-ID 1, Default und übrige Paletten erhalten.

## 0.17.1.2

- Deep Forest durch Arche Nova (hell) mit Lagoon-Blue-/Natural-Leaf-Palette ersetzt; Dark-Themes in Arche Nova (Dunkel) und TFM (Dunkel) umbenannt. Gespeicherte IDs und Default erhalten.

## 0.17.1.1

- Dark (TFM) als viertes Theme mit Marsrot-V2-Palette und nativer Dark-Integration ergänzt; bestehende Theme-IDs und Default erhalten.

## 0.17.1.0

- Segmentbuttons auf vollständige, gemeinsam gesteuerte VisualStates umgestellt, damit Farben und Auswahl nach Lade-/Aktivierungswechseln konsistent wiederhergestellt werden. Theme-Paletten und Segmentgeometrie bleiben erhalten.

## 0.17.0.11

- Einstellungseinträge mit gemeinsamer lesbarer Typografie und kompakteren Auswahlfeldern bei erhaltener Touchfläche versehen.
- Erklärungstexte für Ruf und alte Punkteanzeige in Info-Dialoge verschoben; konkrete Sichtbarkeitssperre weiterhin direkt angezeigt.

## 0.17.0.10

- Dunkel durch „Dark (Arche Nova)“ mit der verbindlichen Ark-Nova-Midnight-Palette ersetzt; Theme-ID 2 und Default erhalten.
- Inhaltskarten appweit auf CardSurface/CardOutline umgestellt; helle Themes, fachliche Spielerfarben und bestehende Sonderrollen erhalten.

## 0.17.0.9

- Arche-Nova-Einstellungen mit gemeinsamen flachen Einstellungsstyles gruppiert; Info- und Speicherverhalten erhalten.
- Kompakte Auswahlbuttons für App-Sprache, Darstellung und Spielmaterialsprache mit integriertem Abwärts-Chevron vereinheitlicht; Seiteneinstiege mit rechtem Chevron gekennzeichnet.

## 0.17.0.8

- Navigation mit grafischen Chevrons und getrennten Aktionen konsolidiert; Arche-Nova-Spezialkartenaktion mit weißem Kartenfächer versehen.
- Flache Einstellungen mit Sprache/Darstellung, Spiele verwalten und direktem Lizenzzugang umgesetzt; Spiel-Sichtbarkeit auf reguläre Listen und Auswahlen erweitert, ohne Daten oder Transferumfang zu ändern.
- Sichtbare Zeitpunkte kulturabhängig mit Datum und Uhrzeit vereinheitlicht; kanonische UI-/Produktdokumentation synchronisiert.

## 0.17.0.7

- Pfeile der historischen Startreihenfolge als zentrierte, themeabhängig eingefärbte SVG-Grafik statt Unicode-Zeichen dargestellt; zugängliche Textbeschreibung erhalten.

## 0.17.0.6

- Historische Startreihenfolge mit MAUI-Standardlayouts und einheitlichen 5-dp-Abständen vereinfacht; eigene Layoutmanager entfernt. Verschachtelte FlexLayouts zur Vermeidung ungültiger Layoutkoordinaten beim Öffnen historischer Ergebnisse durch Grid-Paare ersetzt.

## 0.17.0.5

- Historische Startreihenfolge auf der Ergebnis-Seite mit gemessenen, linksbündig umbrechenden Abschnitten und einheitlichen 5-dp-Abständen korrigiert; Pfeile bleiben bei ihrem folgenden Namen.

## 0.17.0.4

- Header auf gemeinsamen sichtbaren 16-dp-Anker, kompakte zentrierte Titelzeile und dichter gesetzte 44-dp-Aktionen abgestimmt; Touchflächen von der sichtbaren Zeilenhöhe entkoppelt.
- Historische Startreihenfolge im Arche-Nova-Ergebnis mit getrennten, zentrierten und umbrechenden Namens-/Pfeilelementen dargestellt; historische Sortierung erhalten.

## 0.17.0.3

- Spielkontext-Badge auf 24 dp Gesamthöhe mit 2-dp-Kontur und 13-dp-Bold-Text komprimiert; Gruppenname mit 6 dp Abstand an die tatsächliche Badge-Breite gekoppelt.
- Theme-Akzente für Badge-Text und Kontur auf Petrolblau, Aubergine und dezentes Dunkel-Blau vereinheitlicht; ohne Badge entfällt der reservierte Platz.

## 0.17.0.2

- Headerkontext am linken Inhaltsrand ausgerichtet, Abstände kompakter und Badge-Text fett dargestellt; Header- und Inhaltsabstände getrennt.
- Headericons bei unveränderten Touchflächen auf 24 dp begrenzt, Aktiv-Symbole themefähig eingefärbt und lesbare Segmentzustände vereinheitlicht.

## 0.17.0.1

- Gemeinsamen spielneutralen Seitenheader mit Hauptbereichs-/Unterseitenvariante, festen Titel-/Aktionsankern und optionalen Kontextzeilen eingeführt.
- Reguläre Seiten und Arche-Nova-Unterseiten vereinheitlicht, fehlende Spielbadges ergänzt und sichtbare deutsche Anlage-/Hinzufügen-Begriffe konsolidiert.

## 0.17.0.0

- Direkte Shell-Zielnavigation für Spielen-Shortcuts und Resume mit erhaltenen Rückzielen; Seitenwechsel ohne Übergangsanimationen.
- Spieleranlage mit „Weiteren hinzufügen“ und „Fertig“; Gruppenformulare mit ausgewählten Mitgliedern/Spielen und unpersistiertem Neue-Spieler-Overlay.
- Arche-Nova-Header um Spielkontext-Capsules und Ergebnis um historische Startreihenfolge ergänzt; Spielen-Tab auf monochromes SVG umgestellt.
- Spielabhängige Einstellungsnavigation im Spielmodul registriert und allgemeine Settings-Abhängigkeit entfernt.

## 0.16.0.0 – Lernende Gruppendefaults

- Arche-Nova-Vorbereitung übernimmt die gespeicherte Reihenfolge ohne automatische Rotation und ergänzt fehlende gültige Farben.
- Ein erfolgreicher Partiestart speichert Session, historische Reihenfolge und Farben sowie neue Gruppen- und Farbdefaults atomar. Abbruch und Fehler verändern keine Defaults.

## 0.15.0.0

- Einstellungen → Meine Spiele ergänzt: registrierte Spiele sind standardmäßig aktiviert und können anhand ihres stabilen Spiel-Keys lokal abgewählt oder reaktiviert werden.
- Deaktivierte Spiele werden in Spielen ausgeblendet; Historie, Gruppenzuordnungen und Spieldefaults bleiben erhalten. Offene Partien/Wertungen sperren die Deaktivierung auch bei erneuter Prüfung im Speicherauftrag.

## 0.14.0.1

- Nicht mehr benötigte Installationskompatibilität entfernt: keine Bereinigung des früheren Standardspiel-Schlüssels beim Lesen der Preferences und keine Zuordnung des ehemaligen Gruppen-Tab-Werts 2. Aktuelle Navigationswerte und der Standard-Fallback bleiben erhalten.

## 0.14.0.0

- Vier Haupttabs Spielen, Historie, Mitspieler und Einstellungen; Spielen löst aktuelle offene Sessions auf und erhält die Spieleübersicht als Rückziel. Alte Navigationswerte werden abgebildet, die Standardspiel-Preference entfällt.
- Registrierte Spiele und aktuell zugeordnete aktive Gruppen werden nach offenen Sessions und letztem Partiebeginn angezeigt. Vorhandene Gruppen können im Spielkontext zugeordnet und direkt vorbereitet werden.
- Erfolgreiche Gruppenanlage oder Übernahme im Spielkontext öffnet die Vorbereitung mit der gespeicherten Gruppen-ID. Navigationsfehler wiederholen keine Speicherung.
- Erststart ohne Spieler öffnet die Spielerverwaltung; nur der allererste erfolgreich angelegte Spieler führt anschließend zu Spielen. Resume- und Historie-Rückziele bleiben erhalten.

## 0.13.0.0

- Mitspieler-Verwaltung mit Spieler-/Gruppensegmenten, Spieleranlage und Bearbeitung sowie spielunabhängiger Gruppenliste ergänzt. Bisherige Spieleinstiege bleiben im Übergangsstand erreichbar.
- Gruppenformulare unterstützen Mehrfach-Spielzuordnungen und zeigen Archivierungs-/Entfernungssperren bei offenen Sessions. Die allgemeine Farbauswahl entfällt.
- Dubletten erscheinen auf einer abbrechbaren Auswahlseite mit allen Treffern und direkter Übernahme/Reaktivierung je Gruppe; eine bewusste zusätzliche Neuanlage bleibt möglich. Zurück erhält den Entwurf.

## 0.12.0.2

- Allgemeine Gruppenverwaltung unterstützt atomare Neuanlage mit mehreren Spielen, exakte Dublettensuche und explizite Übernahme/Reaktivierung mit ergänzenden Spielzuordnungen.
- Gruppenbearbeitung aktualisiert Name, Archivstatus, globale Reihenfolge und Spielzuordnungen atomar; offene Sessions werden beim Speichern erneut geprüft. Read Models liefern Spielauswahl und Sperrinformationen.
- Farbparameter aus allgemeinen Gruppenverträgen entfernt. Vorhandene spielabhängige Defaults bleiben erhalten; die bisherigen App-Aufrufstellen verwenden die neuen Backend-Verträge.

## 0.12.0.1

- Gemeinsame SDK-/Workload-Festlegung aus der Root-`global.json` entfernt und die separate, nicht versionierte Mac-/iOS-Toolchain-Konfiguration unter `.local/ios/global.json` dokumentiert. MAUI 10.0.110 bleibt unverändert.

## 0.12.0.0

- Temporäre Gruppen aus Domain, Persistenz, Application-Verträgen und bestehenden Gruppenformularen entfernt. Alle Gruppen sind dauerhaft gespeichert und optional archiviert.
- History-Transfer bleibt bei Format v1: Das erforderliche Legacy-Feld isTemporary wird beim Import ignoriert und beim Export stets als false geschrieben. Arche Nova bleibt bei Spielschema 2; Schema 1 bleibt abgelehnt.
- Neu importierte Gruppen erhalten deduplizierte Spielzuordnungen nur aus tatsächlich neu importierten Partien; vorhandene Gruppen behalten ihre Zuordnungen. Alle Importdaten werden atomar gespeichert.
- Das entfernte SQLite-Feld erfordert bei bestehenden Entwicklungsdatenbanken einen ausdrücklich freigegebenen Neuaufbau mit möglichem lokalen Datenverlust; vorhandene Datenbanken werden nicht automatisch aktualisiert.

## 0.11.3.1

- Allgemeine aktuelle Gruppe-Spiel-Zuordnungen mit eigenem zusammengesetztem Schlüssel und idempotentem Hinzufügen ergänzt; historische Partien bleiben unabhängig davon erhalten.
- Offene Partien/Wertungen verhindern das Entfernen ihrer Zuordnung und die Archivierung ihrer Gruppe. Gruppenänderungen bleiben atomar.
- Das neue Schema benötigt für bestehende Entwicklungsdatenbanken einen bewusst freigegebenen Neuaufbau; `EnsureCreated` aktualisiert vorhandene Schemas nicht. Dabei können lokale Daten verloren gehen.

## 0.11.3.0

- .NET SDK 10.0.403 und Workload-Set 10.0.401 reproduzierbar gepinnt; MAUI 10.0.110 enthält den iOS-Kamera-Fix ohne zusätzliche Foto-Mediathek-Berechtigung. ApplicationVersion 85.

## 0.11.2.1

- Arche-Nova-Foto-Review zeigt den Erklärungstext erst beim Antippen des Info-Icons in einem Informationsdialog.

## 0.11.2.0

- Allgemeine Einstellungen zeigen unter Über / Rechtliches die installierte App-Version einschließlich Buildnummer aus den Runtime-Metadaten.

- Arche-Nova-Fotoquellen-Auswahl: Zurück-Pfeil im Header statt unterem Abbrechen-Button. Zurück schließt ausschließlich die Auswahl ohne Fotoaktion oder Änderung des Review-Status. ApplicationVersion 83.

## 0.11.1.0

- Arche-Nova-Wertung: Rückkehr aus der Spielerwertung nach erfolgreichem Speichern repariert. Bereits geöffnete Wertungs-, laufende Partie- und Ergebnisseiten behalten ihren Partiekontext bei fehlendem Single-Use-Shell-Parameter; explizit ungültige Parameter werden weiterhin abgelehnt. ApplicationVersion 82.

## 0.11.0.0

- Arche Nova sammelt in der Pre-1.0-Testphase standardmäßig verwertbar erkannte Fotos lokal als normalisierte, metadatenfreie JPEG-Kopien mit unveränderlicher Prediction und vorhandenen C#-Diagnosen. Identischer normalisierter Bildinhalt wird innerhalb derselben Wertung dedupliziert; erkannte Retakes und abgebrochene Reviews bleiben erhalten.
- Erst der erfolgreiche Partieabschluss ordnet allen Foto-Versuchen derselben Wertung eingefrorene Ground Truth zu. Historische Korrekturen verändern diese Labels nicht.
- Unter Arche-Nova-Einstellungen → Debug → Bilderkennungsdaten stehen Sammlung, Anzahl/Speicher, separater ZIP-Export ohne produktive IDs und manuelles Löschen bereit. Keine automatische Löschung, kein Upload, Ausschluss vom Systembackup; `.bgt` und die akzeptierte CV-Baseline bleiben unverändert.

## 0.10.0.43

- Bewusstes Verlassen des GameSession-Flows löscht das persistierte Resume-Ziel auch bei tieferen Aufrufern wie der Spielerübersicht; Navigation innerhalb einer geöffneten Partie behält es bei. App-Version 0.10.0.43 / ApplicationVersion 80.

## 0.10.0.42

- Bestehende Partien behalten ihren tatsächlichen Aufrufer über Running, Wertung und Ergebnis; Zurück und Verwerfen kehren dorthin zurück. Historische Rückkehr verarbeitet geleerte Shell-Parameter ohne erneute Initialisierung. iOS-Haupttab-Taps einschließlich Retap öffnen den Root über die bestehende Shell, ohne eigene ViewController-Hierarchie.
- Foto-Review mit Status neben dem Wertlabel, Infozeile und Zurück im Header; nur die Bestätigung bleibt unten. Aktivsymbol der zuletzt gespielten offenen Partie in die gemeinsame Buttonfläche integriert. App-Version 0.10.0.42 / ApplicationVersion 79.

## 0.10.0.41

- Gezielten iOS-Simulator-Smoke-Test bei plausiblem Startup-/Initial-UI-Risiko mit minimalem Umfang und Abbruchkriterien in `SETUP.md` verankert; keine Standardverifikation und weiterhin nur nach unmittelbarer ausdrücklicher Freigabe. App-Version 0.10.0.41 / ApplicationVersion 78.

## 0.10.0.40

- Schwarzen iOS-Startbildschirm behoben: Der eigene `MainTabRenderer` verursachte den Black-Screen. iOS verwendet wieder MAUIs Standard-`ShellItemRenderer`; das projektspezifische Tab-Bar-Appearance bleibt erhalten. Auf dem physischen iPhone wurde die sichtbare Hauptoberfläche bestätigt. Der Root-Reset bei expliziten Haupttab-Taps einschließlich Retap bleibt auf iOS eine separate Folgeaufgabe. App-Version 0.10.0.40 / ApplicationVersion 77.
- Allgemeine Datenschutz-/Secret-Regeln für Repository-Dateien und die Weitergabe redigierter Diagnoseausgaben in `AGENTS.md` und `SETUP.md` verankert.

## 0.10.0.39

- iOS-Scene-Lifecycle für Builds mit dem Xcode-27-SDK ergänzt: `SceneDelegate` und `UIApplicationSceneManifest` verwenden die MAUI-Standard-Szenenkonfiguration mit genau einer Scene. Bestehende EF-Core-Interpreter-, Symbol-Strip-, Datenbank- und Bilderkennungslogik bleiben unverändert. App-Version 0.10.0.39 / ApplicationVersion 76.

## 0.10.0.38

- iOS-App-Startup auf den aktivierten Window-Lifecycle umgestellt: Die Initialisierung startet einmalig bei `Window.Activated`, die bereits angezeigte Startseite wird nicht erneut während des Appearance-Übergangs ersetzt, und ein Retry setzt die Startseite nur bewusst vor einem neuen Initialisierungsversuch. EF-Interpreter- und Symbol-Strip-Konfiguration bleiben unverändert. App-Version 0.10.0.38 / ApplicationVersion 75.

## 0.10.0.37

- iOS-Release-Start stabilisiert: Native Symbol-Stripping für iOS Release deaktiviert, während der gezielte EF-Core-Interpreter erhalten bleibt. Datenbank-, Schema- und Bilderkennungslogik unverändert. App-Version 0.10.0.37 / ApplicationVersion 74.

## 0.10.0.36

- iOS-Release-Persistenz auf gezielten Interpreterbetrieb umgestellt: App, MAUI und native Bilderkennung bleiben AOT-kompiliert; nur die EF-Core-/Relational-/SQLite-Assemblies werden interpretiert, damit der beim Modellaufbau per Reflection auftretende `InvalidProgramException` in `ValueComparer.CreateDefault` vermieden wird. App-Version 0.10.0.36 / ApplicationVersion 73.

## 0.10.0.33

- iOS-Release-Start korrigiert: Für iOS wird der Interpreter aktiviert, damit EF Core sein Modell zur Laufzeit aufbauen kann und `EnsureCreatedAsync()` nicht mit `Model building is not supported when publishing with NativeAOT` abbricht. App-Version 0.10.0.33 / ApplicationVersion 70.

## 0.10.0.32

- iOS-App-Icon-Auswahl auf den tatsächlich erzeugten MAUI-Asset-Katalog `appicon_ios.appiconset` korrigiert. App-Version 0.10.0.32 / ApplicationVersion 69.

## 0.10.0.31

- iOS-Native-Build korrigiert: OpenCV-/Codec-Archive werden vor dem Zusammenführen aus einem dedizierten Install-Staging übernommen, statt rekursiv alle statischen Archive aus dem Xcode-Buildbaum einzusammeln. Dadurch werden doppelte libjpeg-turbo-Objekte und die daraus resultierenden Duplicate-Symbol-Linkfehler vermieden. App-Version 0.10.0.31 / ApplicationVersion 68.

## 0.10.0.30

- iOS-App-Icon-Build korrigiert: veraltete `XSAppIconAssets`-Referenz entfernt, damit die MAUI-`MauiIcon`-Pipeline das App-Icon vollständig erzeugt und `actool` kein nicht vorhandenes `appicon.appiconset` mehr erwartet. App-Version 0.10.0.30 / ApplicationVersion 67.

## 0.10.0.29

- Dialoge und modale Auswahlen verwenden eine leichtere, vom Busy-Zustand getrennte Abdunklung. Modale und Busy-Overlays reichen über die gesamte Seitenfläche; der bisher sichtbare Rand durch äußeres Screen-Padding entfällt.

## 0.10.0.28

- Abgeschlossene Ergebnisse aus „Zuletzt gespielt“ behalten die Spielerübersicht als Rückziel, auch nach historischer Korrektur; aus der Historie führt Zurück weiterhin zur Historie.
- „Zuletzt gespielt“ zeigt bei laufender Partie und offener Wertung das gemeinsame Aktiv-Icon. Der Sessionstatus wird im bestehenden Spieler-Query mitgeladen.

## 0.10.0.27

- Abgeschlossene Partien öffnen das Ergebnis; historische Ergebnisse bieten eine Korrektur per Header-Icon und kehren nach dem Speichern einschließlich Gleichstandsauflösung zum aktualisierten Ergebnis zurück.
- Zuletzt gespielte Partien öffnen den passenden Hauptbereich; Haupttab-Taps führen stets zum Root und speichern den tatsächlich aktiven Bereich.
- Reine Auswahldialoge verwenden einheitlich den Zurück-IconButton. Das iOS-App-Icon erhält den dunklen Default-Hintergrund und ein etwas größeres Logo.

## 0.10.0.26

- Scope-/Opt-in-Regeln und Verifikationsbudget in AGENTS, Setup und Third-Party-License-Policy präzisiert: normale Verifikation einmal Solution, Core und Application; keine automatischen Audits, Release-Matrizen oder manuellen UI-Prüfungen.
- Lokalen Lizenzstand bereinigt: Audit-/Herkunfts-/Prüfsummenberichte, Download-/Inventarisierungswerkzeug und temporäre Analyse-/Paketdateien entfernt. Katalog auf 19 nutzerseitige Einträge mit Bibliotheksfamilien reduziert; sämtliche benötigten Lizenz-/NOTICE-Texte und Attributionen erhalten. App-Version 0.10.0.26 / ApplicationVersion 63. Kein Commit oder Push.
- Genau einmal Standardverifikation: Solution-Build ohne Warnungen/Fehler, 73 Core- und 216 Application-Tests bestanden. Keine weiteren Builds, Audits oder manuellen Prüfungen.

## 0.10.0.25

- Offline-Lizenzanzeige unter Einstellungen → Über / Rechtliches → Open-Source-Lizenzen mit Übersicht, scrollbarer Detailseite, sichtbarem IJG-Hinweis und lokalisierten Fehler-/Retry-Zuständen implementiert. Zentraler Katalog und MAUI-Package-Reader, keine neue Dependency oder Runtime-Webabhängigkeit.
- Native Originaltexte unverändert; erforderliche SoftFloat-/FDLIBM-/FLANN-, statische LLVM-C++-Runtime- und vorhandene Bibliotheks-Lizenz-/NOTICE-Inhalte offline mitgeliefert. Bestehende Themes, Styles, Navigation und notwendige Code-Tests wiederverwendet. Eigene App-Lizenz und Ark-Nova-CV unverändert. App-Version 0.10.0.25 / ApplicationVersion 62.

## 0.10.0.24

- Ausdrückliches fachliches GO: Native Mobile-CV ist Produktionspfad und für assistierte Ark-Nova-Wertung freigegeben. Plattformabweichungen gegenüber Windows A bewusst akzeptiert: keine neuen False-High-Fälle/High-Phantome, verpflichtende Nutzerprüfung/Korrektur und explizite Bestätigung vor Speicherung. Keine automatische Wertung.
- Historische Paritäts-/GT-Gate-FAILs und alle Messwerte unverändert erhalten; fachliche Produktentscheidung hebt sie als Freigabeblocker auf. Native nach E 294/411; 115 High (107 richtig/8 falsch), 77 Medium (72 richtig/5 falsch), 219 Uncertain einschließlich 68 ohne Wert; Empty Phantom E 11/120. Windows/OpenCvSharp bleibt Referenz-/Regressionstool.
- CV-Status und relevante README/Produkt-/Architekturdokumentation abgeschlossen. Keine weitere CV-Forschung, Optimierung oder Ursachenforschung geplant; keine Threshold-, Polygon-, Confidence- oder andere Erkennungsänderung. Offen sind nur manuelle Runtime-Nachweise: echter Android-ARM64-Gerätelauf und `iOS build/link/runtime verification pending manual user verification`.
- App-Version 0.10.0.24 / ApplicationVersion 61. Commit und Push auf ausdrücklichen Benutzerauftrag; Ground-Truth-Bericht aus dem vorherigen Auftrag bleibt Teil des dokumentierten Stands.
- Verifikation: Solution-Build ohne Warnungen/Fehler, 73 Core- und 207 Application-Tests bestanden. GT-/Native-Messdateien und deren gespeicherte Quellprüfsummen unverändert; ausschließlich Statusdokumentation und Versionsmetadaten geändert. Keine neuen CV- oder Geräteläufe.

## 0.10.0.23

- Ausschließlich vorhandenen nativen Mobile-CV-Run direkt gegen die originalen Ground Truths tabelliert; alle 648 Expected-Zuordnungen und historischen Nenner geprüft. Kein neuer CV-Lauf, keine Ursachenforschung, Optimierung oder Erkennungsparameteränderung.
- `NativeGroundTruthAcceptance.md` und maschinenlesbarer Nachweis mit allen Entscheidungen: **FAIL**. Gesamt raw 311 statt 312/411; nach E 294 statt 297/411. V3 False High unverändert 8, identische Fehler, keine neuen falschen Highs und kein korrekt-High zu falsch-High. V3 Empty raw 42 statt 40/120, nach E 11 statt 10/120. Weniger E-Treffer und mehr E-Phantome verhindern die Freigabe unabhängig von Windows-Parität.
- Datensatz-, Track-, Farb- und Confidence-Tabellen sowie Quellprüfsummen dokumentiert. Status/README halten Produktionsfreigabe offen und weitere CV-Arbeit gestoppt. ARM64-Gerätelauf separat manuell offen; `iOS build/link/runtime verification pending manual user verification` unverändert.
- App-Version 0.10.0.23 / ApplicationVersion 60 gemäß Dokumentations-Versionsregel. Erkennungslogik und historische Messungen unverändert. Änderungen lokal, ungestaged; kein Commit oder Push.
- Verifikation: alle 648 GT-Zuordnungen, unveränderte Quellprüfsummen, vollständige Nenner und Rekonstruktion der 30 Raw-/27 E-Differenzen geprüft. Solution-Build ohne Warnungen/Fehler; 73 Core- und 207 Application-Tests bestanden. Kein neuer Native-/CV-Lauf.

## 0.10.0.22

- Gemeinsamen nativen OpenCV-Kern in die MAUI-App eingebunden: Decode/EXIF, Registrierung/Gates, Warp, LAB/HSV/Helligkeitsnormalisierung, Farb-/Differenzmasken, Grow und Field/Core-Evidenz hinter einer C-ABI. SafeHandle, serialisierte Lebensdauer und source-generiertes P/Invoke; Ranking A_NoZero und Confidence E unverändert nach Application übernommen und mit dem Windows-Werkzeug geteilt. Alte Referenz/v2-Geometrie, RegistrationEdge 1600 und AnalysisScale 0,5 bleiben aktiv.
- Kamera/Systempicker, gemeinsamer Analyzer und isolierter editierbarer Review mit Sicher/Prüfen/Unsicher implementiert. Kein Autosave unbestätigter Vorschläge; ausdrückliche Bestätigung speichert einen validierten Spielerbatch atomar. Android-Kamera verwendet eine temporäre FileProvider-Datei ohne Bibliotheksberechtigung. Fehler und Abbruch erhalten bestehende Werte; technische Fehler werden auch im Release geloggt.
- Android ARM64/x64 Release mit vollständigem Trimming gebaut. Reflektive EF-/Domain-/Mapping-Metadaten erhalten; EF-/JSON-Trimwarnungen bleiben offen. Tatsächlicher API-30-x64-App-Lauf mit isoliertem Testpaket: Picker-Abbruch, Fotoanalyse/Prefill, Review-Abbruch ohne Speicherung, Benutzerkorrektur/Bestätigung und erneutes Laden erfolgreich; Kameraaufnahme/Registrierungsfehler und Kameraabbruch erhalten die gespeicherten Werte. Kein ARM64-Gerätelauf.
- iOS-Pfad vollständig im Code vorbereitet: gemeinsame C++-Quellen, ARM64-Device-/Simulator-Buildskript, zusammengeführtes statisches Archiv, NativeReference/__Internal, Runtime-/Framework-Linkangaben, Assets/Lizenzen, Kamera/Systempicker und UIImage-Normalisierung. C#-Target kompiliert auf Windows, Shell-Syntax geprüft. **iOS build/link/runtime verification pending manual user verification**.
- **Fachliches Paritätsgate nicht bestanden, Gesamtauftrag nicht abgeschlossen:** 54/54 Registrierungen, aber 30 unterschiedliche Rohwerte und 27 abweichende Wert-/Confidence-E-Ergebnisse bei 648 Entscheidungen. Technische Feature-/Gleitkomma-Untersuchung hat dies nicht behoben; keine Threshold-, Polygon-, Confidence- oder Einzelfalländerungen. Neuer kompakter Nachweis `NativeVisionSpike/mobile-parity.json`, historische Reports unverändert; keine Produktfreigabe.
- Solution und Referenzwerkzeug ohne Warnungen/Fehler gebaut; 73 Core- und 207 Application-Tests bestanden, einschließlich Review/Confidence/SQLite-Bestätigung. Native Probe-/Engine-ABI-Fehlerpfade im Android-x64-Emulator bestanden. CV-Status, Architektur, Produkt-/UI-Dokumentation und relevante README aktualisiert. Die Quellenpromotion invalidiert historische Source-Freeze-Hashes; keine Freezes neu signiert oder historische Reports umgeschrieben.
- App-Version 0.10.0.22 / ApplicationVersion 59. Änderungen lokal, ungestaged; kein Commit oder Push.
- Nach ausdrücklicher Freigabe zwei Buildprobleme außerhalb des Foto-Slices minimal korrigiert: `OnMaterialInfoClicked` zeigt den bestehenden lokalisierten Spielmaterial-Hinweis im App-Dialog; die Spielerliste bindet `IncludeGuests` statt `IncludeTemporary`. Solution-Build ohne Warnungen/Fehler, 73 Core-/207 Application-Tests und getrimmte Android-Release-Builds für x64/ARM64 erneut bestanden; Trim-Warnungen bleiben bestehen. Windows-Replay auf allen 54 Bildern unverändert; beide Homographien im neuen Paritätsnachweis ergänzt.

## 0.10.0.21

- Abgeschlossene PhotoRecognition-Referenzexperimente bereinigt: 14 ignorierte lokale Dateien (21.825.261 Bytes) entfernt – leere Abbruchdateien, zu versionierten Reports inhaltlich identische Ausgabekopien, redundante Varianten-Checkpoints und eine Scratchtabelle. Aktive Assets, Testfotos, Ground Truths, historische Reports und finaler C-v2-Nachtrag bleiben erhalten.
- Vergleichswerkzeuge sowie vollständige Handoff-/Transformationsinputs bewusst für dokumentierte Reproduktion behalten; keine Änderung von Produktions-/CV-Logik oder aktiven Assetpfaden. Der historische C-1506-Fehler bleibt im versionierten Report, nur seine redundante lokale Ausgabe entfällt.
- Finalen Status in vorhandener CV-Dokumentation, Werkzeug-README und Reference3WayComparison festgehalten: A bleibt Produktionsbaseline; B ohne Produktgewinn, C v2 technisch valide (54/54) ohne Vorteil nach E und ausschließlich dokumentierter Referenzkandidat. Weitere Referenz-/Heuristikoptimierung gestoppt; nächster Schritt nativer Mobile-CV-Port mit Paritätsnachweis und Produktintegration.
- Solution-Build ohne Warnungen/Fehler; 73 Core- und 198 Application-Tests einschließlich relevanter Ark-Nova-CV-Regressionen bestanden. Alle acht erhaltenen Referenz-/Geometriepaare auflösbar, 21 Handoff-Dateien manifestkonform und aktive Baseline-Prüfsummen unverändert. Keine erneute Fotoauswertung.
- App-Version 0.10.0.21 / ApplicationVersion 58. Keine Produktionsumstellung, kein Commit oder Push.

## 0.10.0.20

- Ausschließlich C mit dem gelieferten v2-Bounds-Fix erneut gemessen: tatsächliche Referenzhöhe 1544 und tatsächliche Analyzer-Höhe 772 vor Fotoauswertung explizit geprüft; Polygonkoordinaten und Quellviereck unverändert. A/B vollständig aus Reference3WayComparison wiederverwendet, Eingabefoto-Hashes und Ergebnisgleichheit geprüft.
- C registriert 54/54 ohne technische Fehler; Negativtests bestanden. Roh A/B/C 312/314/316 von 411, nach E jeweils 297; V3-False-High 8/10/10, Empty-Phantome nach E 10/15/15. Bestehenden Report nur um gültiges C ergänzt, alte JSON-/Detailreports erhalten. Keine Optimierung, keine Produktionsumstellung, A-/E-/Scoringlogik unverändert.
- Solution-Build ohne Warnungen/Fehler, 73 Core- und 198 Application-Tests bestanden. App-Version 0.10.0.20 / ApplicationVersion 57. Änderungen lokal, kein Commit oder Push.

## 0.10.0.19

- Aktualisierten Dreifachvergleich ausschließlich mit den gelieferten unveränderten Paaren A_current, B_new_scaled_4000x2800 und C_rectified_scoring_4000 ausgeführt. A_NoZero, Confidence E, RegistrationEdge 1600 und AnalysisScale 0,5 unverändert; Produktionsstandard nicht umgestellt.
- A/B: roh 312/314 von 411, nach E jeweils 297; V3-False-High 8/10 und Empty-Phantome nach E 10/15. B damit gemischt. C scheitert bei allen 54 Fotos technisch an über die Analysefläche reichenden Ruf-Feldmasken; keine Polygon-/Padding-/Threshold-Korrektur, null im vollständigen Nenner, Registrierungsstabilität nicht messbar.
- Vergleichswerkzeug protokolliert technische OpenCV-Fehler und sichert abgeschlossene Varianten. Kurzer Reference3WayComparison-Report plus Farb-/Track-/Confidence-/Registrierungsmetriken, Einzelmessungen und Auflösungs-/Morphologiehinweisen; historische Reports unverändert. Alle 648 Baseline-Entscheidungen reproduziert.
- Solution-Build ohne Warnungen/Fehler, 73 Core- und 198 Application-Tests bestanden; abschließendes Vergleichswerkzeug in Release gebaut und ausgeführt. App-Version 0.10.0.19 / ApplicationVersion 56. Keine weitere Forschung, kein Commit oder Push.

## 0.10.0.18

- Genau zwei Referenz-Ablationen auf 4000×2800 gemessen: Single-Shot einmal per bestehendem SIFT/RANSAC geometrisch auf die alte Referenz abgebildet und vor Auswertung eingefroren. Test A nutzt unveränderte alte Polygone, Test B dieselbe Homographie für sämtliche Candidate5-Eckpunkte. Keine Änderung von A_NoZero, E, Schwellen oder Polygonformen außerhalb der Koordinatentransformation.
- Baseline/A/B roh 312/308/310 von 411, nach E 297/293/295. Falsche High-Werte auf vorhandenen GT 8/9/10, zusätzlich ein Empty-High bei B; Empty-Phantome nach E 10/15/16. Alten Stand behalten, keine weitere Forschung. Kurzer Reference4000Ablation-Report plus vollständige Einzel-/Farb-/Track-Metriken; historische Berichte und Assets erhalten.
- Solution-Build ohne Warnungen/Fehler, 73 Core- und 198 Application-Tests bestanden; 648 Baseline-Entscheidungen unverändert reproduziert. App-Versionsmetadaten 0.10.0.18 / ApplicationVersion 55. Änderungen lokal, kein Commit oder Push.

## 0.10.0.17

- Reinen Single-Shot-/Candidate-5-Referenzwechsel gegen gehärtetes A_NoZero und unveränderte Confidence E validiert: Calibration roh 94→96/123, V2 45→44/60, V3 113→111/156; V3-High-Fehler 8→11. Entscheidung FAIL, bisherige Referenz/Geometrie bleiben aktiv, keine Nachoptimierung.
- Expliziten gepaarten Validierungsbefehl und minimale Auswahl einer Geometriedatei mit Bildpfad aus deren Metadaten ergänzt. Vollständiger ReferenceV3Validation-Report mit Farb-/Track-/Confidence-Metriken, Negativprüfungen und unveränderten historischen Nennern; 456 alte Entscheidungen exakt reproduziert. Stressoriginal nicht im Repository verfügbar.
- Solution-Build ohne Warnungen/Fehler, 73 Core- und 198 Application-Tests bestanden. App-Versionsmetadaten 0.10.0.17 / ApplicationVersion 54. Keine Schemaänderung, kein Commit oder Push.

## 0.10.0.16

- App-Lokalisierung und Spielmaterial-Content getrennt: eine eingebettete Ark-Nova-JSON-Datei mit bestehenden deutschen Spezialkarten-/Zooplannamen; nur verifiziertes de unterstützt, zugleich Default. Allgemeine lokale Materialsprache pro Game.Key über MAUI Preferences, unabhängig von CurrentUICulture und ArkNovaSettings.
- Einheitliche Auflösung Nutzer → Materialsprache → unterstützte App-Sprache → Default → deterministischer Missing-Marker. Kleine persistente ArkNovaUserContent-Namensabbildung; keine Translation-Tabellen, generische Engine oder zusätzlichen Domain-Content-Keys.
- ArkNovaSpecialCard.Name aus Domain/EF/Seeding entfernt. Neue und geänderte Nutzernamen werden atomar mit den jeweiligen Kartenänderungen gespeichert; reine Symboländerungen erzeugen keinen Override. Stabile GUIDs, ZooPlan.Number, Archivstatus und historische Referenzen bleiben erhalten.
- Materialabschnitt in den Arche-Nova-Einstellungen; Zooplannamen aus .resx in Content verschoben, UI-/Accessibility-Texte bleiben AppResources. Bestehende sprachneutrale Skizzen bleiben unverändert.
- Ark-Nova-Transferschema 2 mit nullable userName, allgemeines Format weiter 1. Kartenmatching ausschließlich über GUID; lokale Fachwerte/Nutzernamen gewinnen, fehlende Nutzerressourcen können übernommen werden. Built-in-Sprachcontent und Preferences werden nicht transportiert.
- Content-, Preference-, relationale Karten- und Transferregressionstests ergänzt bzw. angepasst. Kanonische Produkt-, Architektur-, Datenmodell-, Transfer- und Wireframe-Dokumentation sowie Setup aktualisiert. Schemawechsel erfordert bewussten Neuaufbau der Entwicklungsdatenbank; keine Migration oder automatische Datenlöschung.
- Solution-Build erfolgreich ohne Warnungen/Fehler; 73 Core- und 198 Application-Tests bestanden. Keine manuelle Geräte-/UI-Prüfung. App-Versionsmetadaten 0.10.0.16 / ApplicationVersion 53.

## 0.10.0.15

- Ark-Nova-Erkennungsforschung ausdrücklich abgeschlossen/eingefroren: gehärtetes A behalten, C1/C2 verworfen, C3 nur diagnostische Forschung, keine F1/F2-Fusion oder C3-Warnregel. Historische Reports/Freezes bleiben unverändert.
- Letztes Original-Stressfoto vor Auswertung mit Hash/GT manifestiert und genau einmal analysiert: Registrierung erfolgreich, 5/6 richtige Vorschläge, Schwarz-Artenschutz 2 statt 24 als unsicher. Keine CV-/Confidence-Änderung; kompakte Abschlussmetriken aus gespeicherten V5-Berichten.
- Auf ausdrückliche Folgeanweisung zunächst separater gemeinsamer OpenCV-C++-Build-Spike mit C-ABI, geklärtem Speicherbesitz und source-generiertem IArkNovaVisionEngine-Interop. Android-ARM64-/x64-Release-Builds und getrimmte Test-APKs; Laufzeit-/ABI-Checks im Android-x64-Emulator. Numerische Parität zur Windows-Referenz scheitert; keine Toleranz-/Erkennungsoptimierung und noch kein vollständiger A-Port.
- iOS-ARM64-CMake-/NativeReference-Buildpfad dokumentarisch abgeglichen; echter Apple-Build auf Benutzerwunsch übersprungen. Kamera-/Picker-/Confidence-UI-/Bestätigungsintegration und deren Tests bleiben ausdrücklich offen, bis der Backend-Spike erfolgreich ist. Produktworkflow und diese Sperre kanonisch dokumentiert.
- App-Versionsmetadaten 0.10.0.15 / ApplicationVersion 52. Keine Schemaänderung, kein Commit oder Push.

## 0.10.0.14

- Begrenzter C3-Abschlussversuch V5 ohne neue Detektorfamilien: Attraktivitäts-Feld-Floors aus Empty-LOO, lokaler Sparse-Farbreadout mit bestehender A-Maskenevidenz/seeded Schwarz und unabhängiger Mehrfarbzuordnung sowie zwei feste Track-Fusionsregeln.
- Auswahl nur auf alter Calibration: Feld-Floors entfernen 13 von 111 Attraktivitäts-FP ohne Recallverlust, bleiben aber unter dem vorab festgelegten Auswahlgate. Lokaler Readout gewinnt acht schwarze und drei gemeinsame-Feld-Treffer, erzeugt fünf zusätzliche Phantome und wird nicht ausgewählt. Beste C3-Auswahl bleibt P0; F2 gewinnt den Fusionsvergleich, verfehlt aber bereits auf Calibration das 98-%-Accuracy-Ziel.
- V2/V3 ausschließlich wiederverwendet verglichen; keine Nachoptimierung. Auf V3 F2 104/156 richtige Werte bei 78,2 % Coverage und 85,2 % Ausgabe-Accuracy. Keine automatische Übernahme; echter neuer Holdout erforderlich, aber ohne Lösung bestehender Fehlakzeptanzen allein nicht ausreichend. Drei Diagnosepatches und vollständige Farb-/Track-/View-/Mehrfachbelegungsmetriken dokumentiert, historische Reports und Freezes erhalten.
- Strikt separate technische A-Härtung: ausschließlich Maskenanteil-Rundungsfehler bis 1e−12 an [0,1] begrenzen, echte Bereichsfehler/NaN bleiben Exceptions. Keine Änderung von Masken, Scoreformeln, Ranking, Confidence oder Domain. Alle 48 Warps identisch, 564 zuvor erfolgreiche Entscheidungen erhalten; Fall3Senkrecht läuft nun ohne Exception. A-V3-Rohwerte 103→113/156, separat vom C3-Nutzen ausgewiesen.
- Solution-Build ohne Warnungen/Fehler, 75 Core- und 178 Application-Tests einschließlich drei numerischer Regressionstests sowie acht fokussierte Forschungschecks bestanden. Keine App-/Kamera-/Persistenz-/Scoringintegration. App-Versionsmetadaten 0.10.0.14 / ApplicationVersion 51.

## 0.10.0.13

- Separater klassischer Ark-Nova-CV-Research-Batch V4: geometrisches Directional Chamfer, 5×5-Census ohne/mit auf vier Pixel begrenzter Track-ECC sowie approximative Low-Rank/Sparse-Projektion über Empty-SVD. Keine neuronalen Modelle, externen Gewichte oder Änderungen an Algorithmus A, Produktionslogik, Geometrie und globaler Registrierung.
- Feste Parameter und Empty-LOO-Schwellen; Kombinationen ausschließlich auf alter Calibration gewählt, anschließend 173 Quellen-/Inputhashes versiegelt. Alte V2- und V3-Daten ausdrücklich als wiederverwendete Vergleiche behandelt. Historische Forschungsquellen und Freeze-/Holdout-Dateien unverändert.
- C1/C2 bleiben ohne belegte Feldtreffer; keine Hough-Erweiterung und kein K1/K2/K3-Ausbau. ECC verbessert das Hintergrundmaß, nicht die Occupancy. C3 auf V3: 116 TP/64 FP/32 FN, 78,4 % Recall und 64,4 % Precision; Artenschutz 84,8 % Precision/81,3 % Recall.
- Farbiger C3-Readout korrigiert zwölf V3-A-Fehler, davon sieben außerhalb des bekannten numerischen A-Ausfalls. Oracle 115 statt 103/156. Einfache vorab gewählte K4-Regel A+C3 mit 96/156 richtigen Werten verworfen; C3 bleibt Kandidat für spätere Forschung. Keine produktive Fusion oder automatische Übernahme.
- Einzel-/Kombinationsmetriken, Phantomzählung, Farb-/Track-/View-Gruppen, Laufzeiten, sechs synthetische Methodenchecks und 44 Diagnose-PNGs samt Quell-/Inputnachweis ergänzt. App-Versionsmetadaten gemäß Repositorypflicht auf 0.10.0.13 / ApplicationVersion 50 erhöht; keine App-/Kamera-/Persistenz-/Scoringintegration.

## 0.10.0.12

- Ark-Nova-Research-Batch V3 mit zehn Empty-Referenzen und 13 frischen belegten Holdout-Fotos ausgeführt. Bestehendes Color/Occupancy-A einschließlich CV-0-Ausschluss, Mindest-Evidenz und Confidence unverändert; alte Calibration 94/123 und alter Holdout 45/60 reproduziert.
- Separates Struktur-Forschungswerkzeug mit Graustufen-/Gradientenreferenzen, zwei eingefrorenen Referenzvarianten, kompakten Komponenten und explizitem Polygon-/Core-Contact-Gate. Thresholds ausschließlich aus Empty-LOO, Methodenvergleich ausschließlich auf alter Calibration. Freeze-/Hashnachweise, Top-1/2/3, Farb-/Track-/View-Metriken, Komplementarität, Oracle und begrenzte Kontext-/Heatmap-Exporte ergänzt.
- Ursprünglicher V3-Lauf bei Fall3Senkrecht durch A-Grenzprüfung von coreDifference=1.0000000000000002 abgebrochen. Rein lesende Observer und Fehlerzählung erlauben dokumentierte technische Wiederaufnahme; A-Algorithmus und eingefrorene Detektorparameter unverändert, zwölf Ausfallwerte zählen als Fehler. Original-Freeze und Abbruch erhalten; erste zehn A-Ergebnisse identisch reproduziert.
- V3: A 103/156, B1_pool 2/156, Top1-Oracle 104/156; kein Ensemble empfohlen. B1_view ohne Treffer, kein Vorteil senkrechter Fotos im Fall1/Fall2-Vergleich. A-Confidence akzeptiert 61 richtige und 14 falsche Werte; keine automatische Übernahme. Near-Object-Gate synthetisch geprüft, reale Zuverlässigkeit bei hinreichender Stein-Recall nicht nachgewiesen.
- DINOv3 ohne Gewichte im Repository/autorisierten Archiv nicht ausführbar; kein Ersatzmodell als DINO ausgegeben. B3 bereits vor V3 wegen schwacher B1-Calibration ausgelassen. Historische Auswahlhashes nicht überschrieben; Capture-Observer ändert den Quellhash und blockiert erneute historische Holdout-Aufrufe erwartungsgemäß.
- Solution-Build ohne Warnungen/Fehler, 75 Core- und 175 Application-Tests sowie fünf native Forschungschecks bestanden. Keine Fusion, neue Registrierung, Kamera-/Mobile-/Persistenzintegration oder neue Paketversion. App-Version 0.10.0.12 / ApplicationVersion 49.

## 0.10.0.11

- Rohe Foto-Feldrangfolge mit acht A/B/C-Kombinationen ausschließlich auf Calibration untersucht: CV-Ausschluss von 0 mit Mindest-Evidenz/Enthaltung, relative Schwarz-Evidenz und Kernrangfolge bei gesättigten Farbtermen.
- Nur A übernommen: `analyze` liefert keinen CV-Wert 0 mehr; schwache Ersatzkandidaten werden als null zurückgehalten. Manuelle Nullwerte und Domainregeln unverändert. Zwei relative Schwarzmodelle sowie die Kernrangfolge bleiben dokumentierte, nicht aktivierte Experimente.
- Auswahl vor dem einzigen Holdout-Lauf mit Quellcode-/Eingabe-Hashes eingefroren. Calibration 93→94/123 Rohwerte richtig, Schwarz-Artenschutz 6→8/11; Holdout 42→45/60 und Schwarz-Artenschutz 2→5/5. Rot/Gelb unverändert, Blau auf Calibration +1 und auf Holdout unverändert. Zwei schwache Schwarz-Rufwerte auf Calibration werden zusätzlich zurückgehalten.
- Bestehende Confidence-Policy ohne neue Schwellen erneut ausgeführt; Holdout-Coverage bleibt 38,3 % bei 100 % Accuracy der akzeptierten Werte. Vergleichsbilder, Top-3-/Rangberichte, Negativprüfungen und Regeltests ergänzt. Keine Kamera-/Mobile-/Scoring-Integration oder Paketänderung. App-Version 0.10.0.11 / ApplicationVersion 48.

## 0.10.0.10

- Reine Schwarz-/Artenschutz-Diagnose für IMG_4265–4268: optionaler Observer exportiert unveränderte Warp-, Strict-/Relaxed-/Grown-Masken, kontinuierliche LAB-Differenz, Core-Vergleiche und lokale Feature-Residuen unter dem PhotoRecognition-Tool.
- Feldmessungen und Pixelverteilungen für 19/20/21 sowie den tatsächlichen Fehlgewinner 0 dokumentiert. Feld 21 gewinnt in keinem Bild und enthält keine strengen Schwarzpixel; Feld 0 gewinnt mit dunklem Boarddruck/Referenzabweichung. Blickwinkelabhängige Untererfassung auf Feld 20 und Score-Sättigung aufgeschlüsselt.
- Ground Truth, Polygongeometrie, Erkennungsschwellen, Occupancy-Gewichte und Confidence-Regeln unverändert. Verbesserungsvorschläge nur beschrieben; keine Benchmark-Optimierung. App-Version 0.10.0.10 / ApplicationVersion 47.

## 0.10.0.9

- Confidence-V2-Benchmark mit 25 unverändert übernommenen 2000-px-Fotos, SHA-256-Prüfung und festem Calibration-/Holdout-Split (17/8). Alle 300 Entscheidungen einschließlich 117 absichtlich abwesender Farb-/Track-Kombinationen werden berücksichtigt.
- Confidence-Diagnostik, kleine Ablationen A–E und Auswertungen je Track, Farbe und viewIndex ergänzt. Accuracy akzeptierter Werte zählt Phantomfarben als Fehler; Rohkandidaten und Enthaltung bleiben getrennt nachvollziehbar.
- Variante E ausschließlich anhand von Calibration ausgewählt und vor Holdout per Quellcode-/Split-Hash eingefroren: lokale Feldsichtbarkeit, Kern-Dominanz bei gesättigten Scores und Absicherung der Farbanwesenheit. Artenschutz bleibt aufgrund nicht trennbarer Calibration-Fehler Uncertain; keine automatische Nachbarfeldkorrektur.
- Holdout: 42/60 Rohwerte richtig, 23/60 akzeptiert (38,3 % Coverage), 23/23 akzeptierte Werte richtig, keine Phantomwerte. Sicherheitsgate bestanden; das 75-%-Coverage-Ziel wird ausdrücklich nicht erreicht. Keine weitere Kalibrierung anhand des Holdouts.
- Reproduzierbare Berichte, Perspektivvergleich und Tests ergänzt. Keine Kamera-/Scoring-UI-, Mobile-, Persistenz- oder Paketänderungen. App-Version 0.10.0.9 / ApplicationVersion 46.

## 0.10.0.8

- Technischer Arche-Nova-Fotoanalyseprototyp (Phase 1A) als separates .NET-Entwicklungswerkzeug unter `tools/Games/ArkNova/PhotoRecognition`: SIFT/RANSAC-Registrierung, Sichtbarkeitsprüfung, Farb-/Referenzdifferenz, Field Occupancy und konservative Nachbarfeld-Konflikte.
- Bereitgestellte Referenztafel, Polygongeometrie und sechs Ground-Truth-Fotos als Entwicklungsassets übernommen. Reproduzierbarer Benchmark mit JSON-Diagnostik, Pipelinezeiten und Negativprüfungen; 60/72 Rohwerte korrekt, keine falschen High-Ergebnisse auf diesem Entwicklungsset.
- Bildbibliotheksfreie Occupancy-/Confidence-Regeln in Application mit automatisierten Tests. OpenCvSharp und Windows-Runtime ausschließlich im Entwicklungswerkzeug; die drei Produktionsprojekte erhalten keine CV-Paketabhängigkeit.
- Mobile Bibliotheksauswahl, Gerätebenchmarks und Kamera-/Scoring-Anbindung bleiben offen. Der bestehende Kamera-Platzhalter bleibt bis zu diesen nachfolgenden Phasen bestehen. App-Version 0.10.0.8 / ApplicationVersion 45.

## 0.10.0.7

- Page-interne modale Auswahl-Picker verwenden einheitlich den vorhandenen Zurück-Pfeil links im Header: Gruppenfarben, Kartensymbole und vorhandene Spieler folgen der Spezialkarten-Auswahl.
- Sichtbarer Zurück-Pfeil und System-/Android-Zurück schließen ausschließlich die aktuelle Auswahl. Allgemeine Hinweis-, Bestätigungs- und Entscheidungsdialoge behalten ihre expliziten Textaktionen.
- Bestehende Overlay-Modalität, Busy-Sperren und fachliche Auswahlfunktionen bleiben unverändert. UI-Konvention und Wireframe präzisiert. App-Version 0.10.0.7 / ApplicationVersion 44.

## 0.10.0.6

- Page-interne modale Overlays deaktivieren den darunterliegenden Seiteninhalt ausdrücklich: Spezialkarten-Auswahl in der Wertung, Symbol-Picker im Spezialkarten-Editor sowie Spieler- und Farbauswahl in Gruppenformularen korrigiert.
- Weitere Page-Overlays projektweit geprüft und verbleibende Busy-Sperrlücken geschlossen. Dialoge und Busy-Layer liegen außerhalb des deaktivierten Inhalts; Shell-/Bottom-Navigation bleibt unverändert.
- Modal- und Busy-Zustände bleiben getrennt; System-Zurück schließt offene Auswahlen zuerst. UI-Regeln und Wireframe entsprechend präzisiert.
- Keine Änderung der fachlichen Scoring-, Spezialkarten- oder Persistenzlogik. App-Version 0.10.0.6 / ApplicationVersion 43.

## 0.10.0.5

- Datei-/Ordnergranularität der Vertical Slices konsolidiert: kleine slice-eigene Commands, Inputs und Read Models stehen beim jeweiligen Haupttyp; unnötige Mini-Slice-Unterordner in Application und Tests entfernt.
- Gruppen-, Spieler- und allgemeine Partien-Slices liegen flacher; Ark-Nova-Start/-Running sowie weitere Companion Types zusammengeführt. Umfangreichere Features und gemeinsam verwendete Modelle behalten ihre eigenständige Struktur.
- Architektur- und Agentenregeln formulieren Companion Types beim Haupttyp als Standard und unterscheiden fachliche Slice-Grenzen von Verzeichnisgrenzen.
- Direkter EF-Core-Zugriff in Application bleibt bestehen. Keine Änderung von Nutzerverhalten, Datenmodell oder Persistenzsemantik. App-Version 0.10.0.5 / ApplicationVersion 42.

## 0.10.0.4

- Application-Slices verwenden EF Core direkt; reine EF-Persistenzports und SQLite-Adapter entfernt. Konkrete Queries ersetzen technische Query-Interfaces.
- BoardgameTracker.Infrastructure aufgelöst: DbContext, relationale Konfiguration, Initialisierung und Seeding liegen in Application. SQLite-Providerkonfiguration und BGT_NAME-Registrierung liegen im MAUI-Host.
- Spielmodul-, Transport-, Navigation- und Preferences-Grenzen bleiben erhalten. Scoring und historische Korrektur bleiben getrennte Use Cases mit gemeinsamer Ergebnisprojektion.
- Relationale Slice-Integrationstests ersetzen Store-Fakes; Core bleibt technologieunabhängig. Architekturregeln und kanonische Dokumentation angepasst.
- Keine beabsichtigte Änderung von Nutzerverhalten, Domain, Datenbankschema oder .bgt-Format. App-Version 0.10.0.4 / ApplicationVersion 41.

## 0.10.0.3

- Repositoryregel: Agenten erstellen niemals selbst Commits; Änderungen bleiben lokal und ungestaged. Kein automatisches Staging oder Push, auch nicht aufgrund eingefügter Checklisten.
- App-Version 0.10.0.3 / ApplicationVersion 40.

## 0.10.0.2

- Architektur-/Naming-Konsolidierung: fachliche Stores statt unspezifischer DataAccess-Ports; sprechende Query-/Use-Case-Methoden und passende Slice-Namespaces.
- Scoring und historische Korrektur teilen neutrale Ergebnisprojektionen und eine SQLite-Implementierung bei getrennten Application-Ports/Use-Cases.
- Gemeinsame GameSession-Navigation für Gruppen, Historie, Spielerbereich und Resume; expliziter Modus im wiederverwendeten Wertungs-ViewModel.
- Klarere Startup-Abhängigkeiten, getrennte Importplan-/Orchestrierungsdateien und entfernte Ordnerplatzhalter. Lernorientierung und hilfreiche Kommentare als Projektrichtlinie; Naming-Warnings in .editorconfig.
- Keine beabsichtigte Änderung von Nutzerverhalten, Domain-Invarianten oder Datenbankschema. App-Version 0.10.0.2 / ApplicationVersion 39.

## 0.10.0.1

- App-Version 0.10.0.1 / ApplicationVersion 38; fehlende Versionserhöhung für den aktuellen Entwicklungsstand nachgeholt.
- Versionsregel und Abschlussprüfung gelten für jeden Änderungsauftrag, einschließlich Dokumentation und Konfiguration, unabhängig von Commit oder Verpackung.

- History-Export: Speichern/Teilen/Abbrechen nach einmaliger Exporterzeugung; native Dokument-Picker für Android und iOS, getrennte erweiterbare Ausgabeziele ohne Doppelaktion.

- Lokaler .bgt-Import/Export abgeschlossener Arche-Nova-Partien aus der Historie mit Resolve, Vorschau und atomarem Commit.
- Globale 1:1-Spielerzuordnungen, lokale Stammdatenpriorität, Gruppentrennung und Spezialkartenmatching.
- Player.IsArchived ersetzt den inversen Aktivstatus; eindeutige Player-/Group-Namen nach Trim + OrdinalIgnoreCase.
- Neue reguläre Entities verwenden UUIDv7. Importierte und deterministische IDs bleiben erhalten.
- Inkompatible Entwicklungsdatenbankänderung: Neuaufbau/Neuinstallation erforderlich, lokale Entwicklungsdaten gehen verloren (SETUP.md).
- Custom Games werden noch nicht unterstützt.

## 0.9.0.11

- Einstellungen: „Spieler“ steht nach Darstellung und Standardspiel direkt vor den spielspezifischen Einstellungen.
- App-Version 0.9.0.11 / ApplicationVersion 37.

## 0.9.0.10

- Allgemeine Spielerverwaltung in Einstellungen mit Gäste-/Archivfiltern, echten Erstellungsdaten, letzter Partie und getrenntem Edit-Flow für Name/Archivstatus.
- Historie und Spielerbereich verwenden gemeinsame Spiel-History-Handler; laufende Partien öffnen den passenden Statusflow. Beziehungen und historische Daten bleiben beim Archivieren erhalten.
- `Player.CreatedAt` ergänzt das Schema. Ältere Entwicklungsdatenbanken benötigen Neuinstallation/Neuaufbau mit Verlust lokaler Daten; kein Upgradepfad.
- App-Version 0.9.0.10 / ApplicationVersion 36.

## 0.9.0.9

- Gleichstandsansicht nach neutralen Platzgruppen gegliedert, einschließlich leerer Zielplätze. Kompakte Spielerzeilen behalten ihre Spielerfarbe und passende Textfarbe aus der bestehenden Palette; Platz-Chips bleiben neutral.
- Platzwahl verschiebt nur den gewählten Spieler in die Zielgruppe. Geteilte Plätze einschließlich Ranglücken innerhalb des eigenen Tie-Blocks sind speicherbar; Grenzen zwischen unterschiedlichen Scores bleiben geschützt.
- App-Version 0.9.0.9 / ApplicationVersion 35.

## 0.9.0.8

- Gleichstandszeilen zeigen Rang, Name, Platz-Buttons und Punkte gemeinsam in einer horizontalen Zeile.
- Platzwahl erhält geteilte Ränge und ändert nur den gewählten Spieler. Automatische Vergabe eindeutiger Plätze und erzwungener Tausch entfallen; gültiges Competition Ranking bleibt die Speicherregel.
- App-Version 0.9.0.8 / ApplicationVersion 34.

## 0.9.0.7

- Gleichstandsauflösung verwendet direkte runde Platz-Buttons statt Pfeilen und Drag & Drop. Jeder Punktegleichstand bildet einen unabhängigen Block mit eindeutiger Zuordnung; die Auswahl eines belegten Platzes tauscht ausschließlich innerhalb dieses Blocks.
- Hilfe zur Platzwahl liegt hinter dem bestehenden Info-Icon; finale Platzierungen werden über die vorhandenen Speicherpfade übernommen. Keine Schemaänderung.
- App-Version 0.9.0.7 / ApplicationVersion 33.

## 0.9.0.6

- Partiestart mit Auf/Ab-Aktionen für die konkrete Partierreihenfolge; Gruppenreihenfolge bleibt erhalten. Rotationsaktion heißt „Reihenfolge rotieren“.
- Wertung serialisiert Autosave und abschließendes Speichern, protokolliert Autosave-Fehler und unterscheidet Speicher- von nachgelagerten Navigationsfehlern. Bedienung und Mehrfachabschluss sind beim Speichern gesperrt.
- Dunkel ist Standard ohne gespeicherte Theme-Auswahl; bestehende Präferenzen bleiben erhalten.
- Gleichstände erhalten Auf/Ab-Aktionen innerhalb derselben Punktzahl; Drag & Drop übergibt die Spieler-ID explizit. Gewählte Platzierungen werden für Ergebnis und Historie gespeichert.
- App-Version 0.9.0.6 / ApplicationVersion 32.

## 0.9.0.5

- App-Start verwendet generische DI-Resume-Handler; Arche Nova stellt Wiederaufnahme und explizite Routen im eigenen Navigationsbereich bereit. AddArkNova registriert Handler und Routen, AppShell enthält ausschließlich allgemeine Routen. Fallback und Navigationsspeicher bleiben erhalten.
- App-Version 0.9.0.5 / ApplicationVersion 31.

## 0.9.0.4

- Ark-Nova-spezifische DI-Registrierungen in die explizite AddArkNova-Extension ausgelagert. MauiProgram bleibt zentraler Composition Root; allgemeine Registrierungen, Lifetimes und Modul-Registrierungsreihenfolge bleiben erhalten.
- App-Version 0.9.0.4 / ApplicationVersion 30.

## 0.9.0.3

- Spezialkartenverwaltung mit Bearbeiten-Icon neben der Karte statt eigener Aktionszeile; Archivstatus wird ausschließlich im Bearbeiten-Editor per Switch und Speichern geändert. Info-Hinweis im Kopf über vorhandenes Info-Icon und lokalisierten App-Dialog ergänzt.
- App-Version 0.9.0.3 / ApplicationVersion 29.

## 0.9.0.2

- Gestrichelter Rahmen in der Spezialkarten-Auswahl kennzeichnet ausschließlich Zuordnungen zu anderen Spielern. Eine ausschließlich dem Zielspieler zugeordnete Karte behält nur ihre Hintergrundmarkierung; bei Zuordnung an beide gelten beide Markierungen.
- App-Version 0.9.0.2 / ApplicationVersion 28.

## 0.9.0.1

- Spezialkarten-Auswahl in der Wertung kompakter: Zurück-Pfeil statt Schließen, keine sichtbare Zuordnungszeile, gestrichelter Theme-Rahmen für bereits zugeordnete Karten. Hintergrundmarkierung des Zielspielers und sämtliche Zuordnungsregeln bleiben erhalten.
- App-Version 0.9.0.1 / ApplicationVersion 27.

- Gemeinsamen DbContext per partial nach allgemeinen und Arche-Nova-DbSets aufgeteilt. Datenbankinitialisierung führt generisch registrierte Schritte aus; Arche-Nova-Seeding wird im Composition Root registriert. Schema und Fachverhalten bleiben unverändert.

- Android-Emulatorprüfung (API 35) für Dialoge, drei Themes, große Schrift, Partie-/Wertungsablauf, Korrekturen und Kartenverwaltung ausgeführt. Bestehenden Navigationsspeicherfehler beim Tabwechsel auf Unterseiten im Wireframe dokumentiert; Hardware-/iOS-Prüfung bleibt offen.

- Native Hinweise, Fehler- und Sicherheitsabfragen durch gemeinsame App-Dialoge ersetzt; Historienfilter, Theme und Standardspiel verwenden App-Auswahl mit sichtbarer aktueller Auswahl. Zurück/Abbrechen bestätigt keine Aktion; ungespeicherte Entwürfe bleiben beim Dialogschließen erhalten.
- Altkompatibilität entfernt: Spezialkarten-Schema-Upgrade, alte Navigationswiederherstellung, EF-Migrationswerkzeuge und versionsgebundenes Upgrade-Skript. Initialisierung des aktuellen Schemas und Erhalt bearbeiteter Karten/Zuordnungen bei wiederholten Starts getestet. Installationen vor dem GUID-Kartenschema benötigen eine Neuinstallation mit Verlust lokaler Daten.

- UI-Bereinigung: kontrastreicher Text in der Spielereingabe, lokalisierte Ergebnisanzeige und Android-Zurück schließt zuerst den Spezialkarten-Picker.

- Regel bis zum ersten Deployment ergänzt: keine Migrationsstrategie oder Altkompatibilität; notwendige Neuinstallation und Verlust lokaler Entwicklungsdaten ausdrücklich ankündigen. Upgrade-Prüfvorgaben angepasst; App-Code unverändert.

- Markdown-Dokumentation konsolidiert: doppelte Feature-/Navigationsregeln zusammengeführt, veraltete DB-Reset- und UI-Angaben ersetzt.
- Zuständigkeiten geschärft und Einstieg für neue Chats ergänzt.
- Drei bestätigte UI-Codeabweichungen für spätere Behebung im Wireframe festgehalten; keine App-Codeänderung.

## 0.9.0.0

- Persistierte, benutzerverwaltbare Arche-Nova-Spezialkarten mit stabiler GUID, Name, 0–3 geordneten Symbolen und Archivstatus statt festem Kartenenum.
- Verlustfreier, atomarer SQLite-Upgrade der bisherigen Integer-Zuordnungen 1–5 auf deterministische Startkarten-IDs; Seeding erhält Benutzeränderungen und Archivzustände.
- Verwaltung aus den Arche-Nova-Einstellungen mit gemeinsamem Create-/Edit-Editor und grafischem Symbolwähler.
- Dynamische Wertungs-/Historienauswahl mit aktuellen Namen/Symbolen; archivierte Karten bleiben referenzierbar, sind aber nicht neu zuordenbar.
- 14 Einzelgrafiken aus Icons/Symbole eingebunden; alte vollständige Kartenbilder und statische Karten-Präsentation entfernt.
- App-Version 0.9.0.0 / ApplicationVersion 26.

## 0.8.9.4

- Überschriften der Zahlenräder zentriert; größere Abstände zwischen Rädern und zu den Zahlenfeldern.
- Außenkanten der Eingabegruppe bündig mit Punktefläche und Fertig-Button.
- App-Version 0.8.9.4 / ApplicationVersion 25.

## 0.8.9.3

- Attraktivität und Artenschutz mit schmaleren, zusammengehörigen Rad-/Eingabespalten; Zahlenfelder übernehmen die gerundeten Konturen der Räder.
- Zusätzlicher Abstand vor der Punktefläche.
- App-Version 0.8.9.3 / ApplicationVersion 24.

## 0.8.9.2

- XAML-Laufzeitfehler beim Öffnen der Spielereingabe behoben: Ruf-Slider erhält seine aus den Fachregeln abgeleiteten Grenzen als double statt int.
- App-Version 0.8.9.2 / ApplicationVersion 23.

## 0.8.9.1

- Rufeingabe unter den Punkten als horizontaler Slider gemäß Spieleinstellung; zusätzlicher Erfassungsschalter entfällt.
- Hinweise zur Wertungseingabe hinter einem Info-Symbol im Seitenkopf.
- App-Version 0.8.9.1 / ApplicationVersion 22.

## 0.8.9.0

- Arche-Nova-Spielerwertung mit nativen Zahlenrädern, festen Bereichsgrenzen und direkter Tastatureingabe.
- Nicht erfasste Werte zeigen Mittelwertvorschläge; erst bewusste Eingabe oder „Fertig“ übernimmt die Pflichtwerte. Ruf bleibt separat optional.
- Punktevorschau und bestehende gespeicherte Werte bleiben im Eingabescreen sichtbar.
- App-Version 0.8.9.0 / ApplicationVersion 21.

## 0.8.8.2

- Spielerkacheln in Gruppenanlage und -bearbeitung verwenden zugewiesene Spielerfarbe und gekoppelte Text-/Iconfarbe.
- Hoch-/Runter-/Entfernen-Symbole vergrößert; gemeinsame Aktionsflächen mindestens 48 × 48 dp.
- App-Version 0.8.8.2 / ApplicationVersion 20.

## 0.8.8.1

- Kleine zugehörige Vertrags-/Präsentationstypen gemeinsam mit ihrem Haupttyp abgelegt.
- Historienablauf gegliedert und wiederholte vollqualifizierte Typnamen in der Wertung vereinfacht.
- Verbindliche Regeln zu Begleittypen, expliziten einfachen Typen, var für komplexe Typen sowie mehrzeiligen if-/finally-Blöcken ergänzt.
- App-Version 0.8.8.1 / ApplicationVersion 19.

## 0.8.8.0

- Gemeinsame semantische Spielerfarben statt separatem Arche-Nova-Enum; jedes Spiel definiert seine erlaubte Teilmenge.
- Einheitliche spielabhängige Farbwahl bei Gruppenanlage und -bearbeitung mit explizitem Spielbezug im Dialog.
- Gruppenformulare bleiben allgemein; Spieldefaults werden über Moduladapter atomar mitgespeichert.
- Bestehende Arche-Nova-Farbnummern und Tabellenstruktur bleiben kompatibel.
- App-Version 0.8.8.0 / ApplicationVersion 18.

## 0.8.7.0

- Allgemeine Gruppenbearbeitung von Arche-Nova-Defaults getrennt; spielspezifische Bearbeitungsseite und atomarer Kompositions-Slice liegen im Spielmodul.
- Architektur- und SQLite-Tests sichern neutrale Verträge, Farb-/Reihenfolgetausch und Rollback.
- Fehlende Lokalisierung und Fehlerprotokollierung ergänzt; Debug-Logausgabe aktiviert.
- Gruppenaktionen vereinheitlicht; typisierte XAML-Bindings mit strenger Compilerprüfung abgesichert.
- App-Version 0.8.7.0 / ApplicationVersion 17.

## 0.8.6.1

- Nativer Splashscreen fest dunkel (#141A17); anschließende App im gespeicherten Theme.

- Deutsche Oberflächentexte verwenden durchgängig Spieler statt Player.

- Statussymbol für offene Partien transparent und dynamisch im Theme-Textton statt fester grüner Fläche.

- Gemeinsame Header-Unterkante und Aktionsrollen auf alle bestehenden Seiten angewendet.
- Gruppenbearbeitung und Wertungseingaben flach; Leerzustände ohne dekorative Karte.
- App-Auswahldialoge vereinheitlicht; verbleibende native Dialoge im Wireframe dokumentiert.
- App-Version 0.8.6.1 / ApplicationVersion 16.

## 0.8.6.0

- Klarere UI-Hierarchie in Partievorbereitung, Gruppen und Gruppe anlegen: flache Metainformation/Formulare, Primary-Create-Aktion und gemeinsame Secondary-/Textaktionsstyles.
- Einheitliches vorhandene-Spieler-Auswahlfenster mit direktem Hinzufügen per Tap.
- SurfaceSecondary als leichte Aktionsfläche; App-Version 0.8.6.0 / ApplicationVersion 15.


## 0.8.5.4

- Neutrale UI-Flächen reduziert: SurfaceSecondary und DisabledSurface verwenden Surface.
- Sekundärbuttons transparent mit Kontur; besondere-Karten-Auswahl auf AppBackground mit Surface-Karten und gemeinsamer Auswahlmarkierung.
- App-Version 0.8.5.4 / ApplicationVersion 14.


## 0.8.5.3

- Normale Iconbuttons transparent und themeabhängig getönt; letzte Partie verwendet die gemeinsame Kartenfläche.
- Historienabstand durch unsichtbare Picker-Mindesthöhen korrigiert.
- Android-TabBar mit fester Icon-/Beschriftungsanordnung und kompakten Textmaßen.
- App-Version 0.8.5.3 / ApplicationVersion 13.


## 0.8.5.2

- Globalen Theme-Hintergrund auf abgeleitete ContentPages angewendet.
- Android-Shell: aktive Auswahlfläche umfasst Icon und Beschriftung gemeinsam, ohne abweichende Randfarbe.
- App-Version 0.8.5.2 / ApplicationVersion 12.


## 0.8.5.1

- Finale UI-Spielerfarben-Palette mit zwölf Hintergrund-/Textpaaren und mindestens 4,5:1 Textkontrast; Arche Nova verwendet weiterhin vier semantische Domainfarben.
- Spielertext und Farbauswahl folgen der themeunabhängigen Palette, Zooplan-Badges behalten eigenen Grafiktext.
- App-Version 0.8.5.1 / ApplicationVersion 11; keine Schemaänderung und keine Fotoerkennung.


## 0.8.5.0

- Zentrale semantische Themes Deep Forest, Königsblau und Dunkel mit persistenter Auswahl in den allgemeinen Einstellungen.
- Dynamische Farbressourcen, native Shell-Tab-Auswahl und Kontur, Historienchips und Monatsgruppen.
- App-Version 0.8.5.0 / ApplicationVersion 10; keine Schemaänderung.


## 0.8.4.0

- Completed-Partien mit Spiel-/Gruppenfiltern, aktuellen Namen und gespeicherten Platzierungen.
- Explizite Arche-Nova-Korrektur und bestätigtes Löschen abgeschlossener Partien; normale Scoring-Statusgrenze bleibt erhalten.
- Fachliche Festlegung zu Historie und späterer unabhängiger Statistik kanonisch dokumentiert.
- App-Version 0.8.4.0 / ApplicationVersion 9; keine Schemaänderung.


## 0.8.3.2

- Android Adaptive App Icon: Foreground füllt die Launcher-Maske stärker aus; der sichtbare Hintergrundring um das Logo wird deutlich reduziert.
- Arche-Nova-Ergebnis: Attraktivität, Artenschutz und besondere Karten verwenden auf spielerfarbig getönten Karten jetzt die Primärtextfarbe gemäß verbindlicher Kontrastregel.

## 0.8.3.1

- Buildfehler `CS1931` in `SqliteArkNovaRunningGameSessionQuery` behoben: lokale Ergebnisvariable und LINQ-Range-Variable verwenden wieder eindeutige Namen.
- Der dadurch verursachte Folgefehler `CS0006` für die fehlende `BoardgameTracker.Infrastructure.dll` entfällt nach erfolgreichem Infrastructure-Build.
- App-Version auf 0.8.3.1 / ApplicationVersion 7 erhöht.

## 0.8.3.0

- Domänenvokabular vollständig auf `Game`, `GameSession`, `Group` und `Player` vereinheitlicht.
- `Person` -> `Player`, `GroupPerson` -> `GroupPlayer`, `PersonId` -> `PlayerId`.
- `ArkNovaSessionPlayer` -> `ArkNovaGameSessionPlayer` und weitere verkürzte Session-Namen auf `GameSession` vereinheitlicht.
- Spielespezifischer Code in allen Projekten unter top-level `Games/<GameName>/` konsolidiert.
- Ark-Nova-Application- und UI-Code nach GameSession-Vertical-Slices strukturiert.
- Ark-Nova-EF-Konfigurationen, Seeder und SQLite-Implementierungen in das Ark-Nova-Infrastructure-Modul verschoben.
- Composition als verbindliches Modell für spielespezifische Entity-Erweiterungen dokumentiert; allgemeine Felder werden nicht dupliziert.
- Historische EF-Migrationen aus dem aktuellen Pre-Alpha-Quellmodell entfernt; Runtime bleibt bei `EnsureCreatedAsync()`. Nach diesem Schema-Refactoring ist ein lokaler DB-Reset erforderlich.
- Kanonische Dokumentstruktur bereinigt und Architektur-/Datenmodell-Dokumentation auf Current State konsolidiert.
- App-Version auf 0.8.3.0 / ApplicationVersion 6 erhöht.
- In der Erstellungsumgebung war kein `dotnet`-SDK verfügbar; Build/Test müssen lokal per Rebuild und Testlauf bestätigt werden.

## 0.8.2.5

- Android-Launcher-Icon endgültig auf den kanonischen Ressourcennamen `appicon` vereinheitlicht.
- Ursache des Roboter-Icons behoben: Der `MauiIcon`-Hintergrund hieß zuvor `appiconbg`; Androids Manifest muss aber exakt den aus dem `MauiIcon Include`-Dateinamen abgeleiteten Mipmap-Namen referenzieren.
- `MauiIcon Include` verwendet jetzt `Resources\\AppIcon\\appicon.png`, Foreground bleibt `appiconfg.png`; AndroidManifest referenziert passend `@mipmap/appicon` und `@mipmap/appicon_round`.
- `ForegroundScale=\"0.70\"` gesetzt, damit das Logo innerhalb der Android-Adaptive-Icon-Safe-Zone bleibt.
- App-Version auf 0.8.2.5 / ApplicationVersion 5 erhöht.

## 0.8.2.4

- Android-Buildfehler APT2260/APT2067 behoben: explizite `@mipmap/appicon`- und `@mipmap/appicon_round`-Referenzen aus `AndroidManifest.xml` entfernt.
- Das Launcher-Icon wird wieder ausschließlich über `MauiIcon` in der Projektdatei generiert und vom MAUI-Build eingebunden.
- App-Version auf 0.8.2.4 / ApplicationVersion 4 erhöht.

## 0.8.2.3

- Android-Launcher-Icon auf ein explizites MAUI Adaptive Icon umgestellt.
- Farbige Logo-Grafik wird als Foreground mit deckendem App-Hintergrund erzeugt.
- AndroidManifest referenziert `@mipmap/appicon` und `@mipmap/appicon_round` explizit.
- App-Version auf 0.8.2.3 / ApplicationVersion 3 erhöht.

## Historischer Dokumentationszwischenstand vor 0.8.2.3

- Update-Pakete mit Dateioperationen erhalten künftig einen generischen `APPLY_UPDATE.cmd`; Lösch-, Verschiebe- und Umbenennungsaktionen werden deklarativ aus der Operationsdatei gelesen.

- Kanonische Dokumentstruktur festgelegt.
- Historische Versionsanhänge aus Spezifikationen werden in dieses Changelog
  ausgelagert.
- Widersprüche zu Navigation, Zooplanschlüssel, besonderen Karten,
  Datenbankstrategie und Session-Parallelität aufgelöst.
- Fachliche Zielregel festgelegt: maximal eine offene Session je Spiel + Gruppe.
- Verbindliche Kontrastregel ergänzt: keine grauen fachlichen Inhalte auf
  spielerfarbigen Kacheln.
- Versions-/Lieferregel präzisiert.
- Bekannte Diskrepanz dokumentiert: `ApplicationDisplayVersion` im Code ist noch
  `0.8.1.6`, obwohl der funktionale Stand v0.8.2.1 ist.

## v0.8.2.1

- Spielkontext vor automatischer Gruppen-Navigation stabilisiert.
- Defaultspiel-Start, Spielauswahl und Session-Wiederaufnahme auf gemeinsamen
  Navigationspfad vereinheitlicht.
- Ergebnis-Spielerkacheln sollten wieder Spielerfarbtönung verwenden.
- Keine Datenbankschemaänderung.

Bekannter Nachtest: Ergebnisfarben greifen noch nicht in allen Fällen korrekt.

## v0.8.2.0

- Expliziter Spiel-/Navigationskontext eingeführt.
- Hauptnavigation: `Spiele · Gruppen · Historie · Einstellungen`.
- Gruppenübersicht immer im Kontext eines ausgewählten Spiels.
- Optionales Standardspiel.
- Letztes Spiel, letzter Hauptbereich und zuletzt betrachtete Session in MAUI
  Preferences.
- Wertungstexte/Orientierungsinformationen kontrastreicher vereinheitlicht.
- Deutsche UI-Bezeichnungen `Besondere Karten` und `Ruf`.
- Keine Datenbankschemaänderung.

## v0.8.1.9

- Gemeinsamer sichtbarer Disabled-Zustand für Primäraktionen.
- `Starten` bis zur gültigen Zooplanauswahl deaktiviert.
- `Wertung abschließen` bis zu vollständigen Pflichtwerten deaktiviert.
- Überlappende Zeilen in Wertungskacheln bereinigt.
- Fehlende Werte als `–`.
- Counter-Badge am besonderen-Karten-Icon entfernt.
- Partiedatum in Wertung ergänzt.
- Spiele-/Einstellungen-Erklärungen hinter Info-Icons verschoben.

## v0.8.1.8

- Neutrales besonderes-Karten-Icon statt fotografischem Kartenrücken.
- Zusätzliche Wertungszeile für Kartenanzahl und optional Ruf.
- Fehlende Rufdarstellung in der Übersicht ergänzt.
- Delta-Lieferregel in Setup dokumentiert.

## v0.8.1.7

- Bottom Tabs auf `Spiele · Gruppen · Historie · Einstellungen` erweitert.
- Gruppen-Tab als Root-Ziel.
- Busy-Schutz gegen Doppeltap auf Wertungsspieler.
- Besondere-Karten-Auswahl direkt nach Aktion schließen.
- Berechnete Punkte hinter Spielernamen.
- Partiedauer im Ergebnis.
- Historie-Tab als Platzhalter vorbereitet.

## v0.8.1.6

- Spezialkartentexte lokalisiert.
- Allgemeine Hauptnavigation eingeführt.
- Aktiv-Icon für Gruppen mit offener Session.
- Wertung verwerfen.
- Touch-/Command-Fehler bei Spielerwahl korrigiert.
- Zooplanname statt bloßer Nummer in Wertung.
- Besondere-Karten-Auswahl als größere Kacheln.
- Keine Datenbankschemaänderung.

## v0.8.1.5

- Vollständiger Arche-Nova-Wertungsslice.
- Offizielles 42-stufiges Artenschutz-Lookup.
- Neue/alte Punkteanzeige.
- Wertevalidierung für Attraktivität, Artenschutz, Ruf.
- Autosave während `Scoring`.
- `Completed` als Grenze für Historie/Statistik.
- Globale Arche-Nova-Preferences.
- Fünf konkrete besondere Karten mit Composite Key.
- Gleichstands-Screen mit Competition Ranking.
- Ergebnis bleibt bis `Fertig`.
- Kamera-Button vorbereitet.
- Tests für Wertungslookup/Punkte/Ranking.

## v0.8.1.3

- `ArkNovaZooPlan.Number` als natürlicher Primärschlüssel festgelegt.
- Künstliche Zooplan-ID entfernt.
- Spielidentität weiterhin `Game.Id` plus stabiler Key für speziell
  unterstützte Spiele.
- Entwicklungsdatenbank bei Schemawechsel neu erzeugen.

## v0.8.1.1

- Entwicklungsbetrieb auf `Database.EnsureCreatedAsync()` umgestellt.
- Idempotentes Seeding für Arche Nova und 14 Zoopläne.
- Shell-Navigationsparameter zentralisiert.

## v0.8.1

- Technischer Gesamt-Review.
- Architekturgrenzen bereinigt.
- `IQueryAttributable`/`ShellNavigationQueryParameters`.
- kompilierte XAML-Bindings mit `x:DataType`.
- zustandslose Persistenzports.
- xUnit v3 / Microsoft Testing Platform.
- Touchziele und Lokalisierung konsolidiert.

## v0.8.0

- Persistenter Arche-Nova-Start-Slice.
- `GameSession` + Spieler + Arche-Nova-Snapshots atomar gespeichert.
- Laufende Partie.
- Wiederaufnahme offener Partie.
- `Running -> Scoring`.
- autoritative Setup-Regeln aus UI in Core/Application verschoben.

## v0.7.x

- Arche-Nova-Partievorbereitung aufgebaut.
- Gruppen-Defaultfarben.
- Zooplan-Auslosung und Auswahl.
- Startspielerrotation aus letzter abgeschlossener Partie.
- kompakte Vorbereitungskacheln, Touch-/Busy-Regeln und Fehlerdarstellung
  iterativ bereinigt.

## v0.6.x

- Gruppen-Slice mit Vertical-Slice-Struktur.
- Gruppenerstellung, Bearbeitung, feste Mitgliedschaft und Reihenfolge.
- temporäre Gruppen.
- Archivierung/Reaktivierung.
- grundlegender UI-Design-Guide und Fehler-/Busy-Konzept.
