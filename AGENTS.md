# Repository Guidelines

## Auftrag und Scope

Nur den tatsächlich beauftragten Scope bearbeiten. Keine ungefragten Refactorings,
Audits, Optimierungen, Bereinigungen, Recherchen oder Zusatzprüfungen durchführen.

Bei fachlichen, architektonischen oder anderweitig relevanten Unklarheiten den
Entwickler fragen, statt Verhalten, Anforderungen oder Prioritäten zu erfinden.
Kleine eindeutige Implementierungsdetails innerhalb bestehender Regeln selbst entscheiden.

Bei Produkt-, UX-, Architektur- und Persistenzentscheidungen als konstruktiver
Diskussionspartner arbeiten: nicht reflexiv zustimmen, sondern relevante Gegenargumente,
Risiken, Alternativen und verfügbare Evidenz aktiv benennen und bei Bedarf gezielt
recherchieren. Nach einer bewussten Entscheidung des Entwicklers gilt diese als
gewünschter Sollzustand.

## Repository access boundary

Work only inside the current Boardgame-Tracker repository.

Do not inspect, read, search, modify, execute, or enumerate files or directories
outside the repository root unless the user explicitly requests access to a
specific external path.

Do not use broad filesystem searches that escape the repository root.

If a required dependency or file appears to exist outside the repository, stop
and report the missing external dependency instead of searching the system for it.

Normal external tooling required to build and test the repository is still allowed,
for example:
- installed .NET SDK/tooling;
- NuGet package restore;
- compiler and test runner executables;
- Git itself.

Using those tools does not authorize browsing or inspecting their installation
directories.

## Arbeitsweise

Die Aufgabe mit dem dafür am besten geeigneten verfügbaren Assistenten oder Werkzeug
bearbeiten. Nicht automatisch an Codex oder einen anderen Coding-Agenten delegieren.
Wenn Analyse, Review, Recherche, Architektur- oder Dokumentationsarbeit ohne lokalen
Ausführungsbedarf direkt besser erledigt werden kann, diese Arbeit selbst durchführen.

Reine Dokumentationsänderungen, die sich vollständig aus dem gepushten Repository-
Stand ableiten lassen, direkt bearbeiten und als aktualisierte Dateien bereitstellen;
dafür keinen zusätzlichen Coding-Agenten einschalten.

Codex bzw. lokale Coding-Werkzeuge bevorzugen, wenn nicht gepushte Arbeitsbaumdaten,
lokale Codeänderungen, Builds, Tests oder andere lokale Ausführung tatsächlich nötig
sind. Wenn ein anderer Arbeitsweg klar besser geeignet ist, den Entwickler kurz darauf
hinweisen.

Standardmäßig genau einen Agenten verwenden. Keine Subagenten, parallelen Agenten,
Multi-Agent-Orchestrierung oder Best-of-N-Läufe ohne ausdrücklichen Benutzerauftrag.

Bereits erarbeitete Ergebnisse wiederverwenden. Keine Arbeit zwischen Assistenten
verschieben, wenn dadurch dieselbe Analyse erneut durchgeführt werden müsste.

## Minimaler Kontext

Mit dem kleinsten Kontext arbeiten, der für die aktuelle Aufgabe erforderlich ist.

Zuerst nur direkt betroffene Dateien lesen. Weitere Dateien nur laden, wenn eine
konkrete Abhängigkeit, Unsicherheit, Fehlermeldung oder Entscheidung dies erfordert.
Große Dateien, Logs und Dokumente bevorzugt gezielt nach relevantem Abschnitt, Symbol
oder Zeilenbereich lesen statt vollständig.

Keine vorsorgliche Repository-Vollanalyse. Bereits gelesene unveränderte Inhalte
nicht ohne konkreten Grund erneut vollständig lesen.

Ohne konkreten Bedarf keine Git-Historie, `git blame`, alten Commits, Issues, Pull
Requests, Dependency-/SDK-/Runtime-Inventuren, Benchmarks, Profiling, generierten
Artefakte oder externe Webrecherche untersuchen.

Wenn ein Ansatz scheitert, zuerst Ursache und vorhandene Evidenz neu bewerten.
Den Untersuchungsraum nicht reflexartig durch weitere Dateien, Tools oder Recherche
vergrößern.

## Dokumentationsstruktur und Quellen der Wahrheit

`AGENTS.md` bleibt eine kompakte Arbeits- und Routingdatei. Fach-, Architektur-, UI-,
Code-, Persistenz- und spielspezifische Detailregeln gehören in ihre kanonischen
Dokumente und werden hier nicht dupliziert.

Allgemeine Dokumente unter `docs/` beschreiben ausschließlich spielübergreifende
Regeln. Spielspezifische Dokumentation liegt unter `docs/Games/<GameName>/`.
Ein spielspezifisches Dokument wird nur gelesen, wenn die Aufgabe dieses Spiel
oder genau dieses Spezialthema betrifft.

Bei Produkt-, Architektur-, Datenmodell-, UI-, Wireframe- oder Import/Export-Fragen
eines konkreten Spiels gilt zuerst die allgemeine kanonische Quelle und ergänzend
die gleichnamige Datei unter `docs/Games/<GameName>/`, sofern sie existiert.
Spezialisierte Dokumente wie Forschungs- oder Integrationsstatus nur bei Bedarf lesen.

`docs/README.md` nur bei Dokumentationsstruktur, Dokumentverschiebungen, neuen
Dokumenten oder unklarer Dokumentzuständigkeit lesen.

Bekannte offene Soll-/Ist-Abweichungen, technische Schulden und konkrete Follow-ups
gehören ausschließlich nach `docs/TODO.md`. Soll-Dokumente bleiben frei von offenen
technischen Statusnotizen; Details zur Zuständigkeit stehen in `docs/README.md`.

| Thema | Kanonische Quelle |
|---|---|
| Produktverhalten und Anforderungen | `docs/PRODUCT_SPEC.md` |
| allgemeine Architektur, Schichten und Modulregeln | `docs/ARCHITECTURE.md` |
| Domain und Persistenzmodell | `docs/DATA_MODEL.md` |
| C#, Async, Naming und Kommentare | `docs/CODE_GUIDELINES.md` |
| allgemeine UI-/Interaktionsregeln | `docs/UI_DESIGN_GUIDE.md` |
| konkrete Screens und Navigation | `docs/UI_WIREFRAME.md` |
| Import/Export | `docs/IMPORT_EXPORT.md` |
| Drittanbieter und Lizenzanforderungen | `docs/THIRD_PARTY_LICENSE_POLICY.md` |
| Umgebung, Build, Tests, DB, Versionierung, Lieferung | `SETUP.md` |
| Projektüberblick | `README.md` |
| historische Änderungen | `CHANGELOG.md` |

Nicht alle Dokumente vorsorglich lesen. `README.md` und `CHANGELOG.md` sind keine
Standardlektüre für normale Implementierungsaufträge. Historische Quellen nur lesen,
wenn der Auftrag tatsächlich Historie benötigt.

Jede dauerhafte Detailregel besitzt genau eine kanonische Quelle. Bei relevantem
Widerspruch zwischen Code und kanonischer Dokumentation nicht stillschweigend eine
Seite wählen. Den Widerspruch klären lassen.

## Änderungen

Secrets, Zugangsdaten und private/persönliche Kennungen niemals in versionierte
Dateien, Patches, Commits oder bereitgestellte Diagnoseausgaben übernehmen.
Diagnose-/Logausgaben vor dem Bereitstellen redigieren; Details und
Redaktionsregeln stehen in `SETUP.md`.

Die kleinste sinnvolle Änderung durchführen. Funktionierenden, nicht betroffenen
Code nicht modernisieren, umbenennen, verschieben oder refactoren.

Auftragsfremde Auffälligkeiten höchstens kurz nennen; nicht automatisch beheben.

Tests niemals entfernen, abschwächen oder umgehen, nur um einen grünen Lauf zu
erreichen.

Fremde oder bereits vorhandene lokale Änderungen des Benutzers niemals zurücksetzen,
überschreiben oder ungefragt bereinigen.

## Verifikation

Nur so viel verifizieren, wie die konkrete Änderung sinnvoll erfordert.

Reine Markdown-Dokumentationsänderungen ohne Code-, Projekt-, Build- oder
Ressourcenänderung benötigen keinen Build und keine Tests. Inhalt/Konsistenz sowie
`git diff` und `git status` genügen.

Bei Code- oder Projektänderungen die kleinste sinnvolle Build-/Testmenge verwenden.
Die normale Obergrenze ohne besondere Freigabe ist:
1. ein Solution-Build;
2. Core-Tests einmal;
3. Application-Tests einmal.

Diese drei Schritte sind ein Maximum, kein Pflichtprogramm. Wenn weniger die
geänderte Eigenschaft ausreichend prüft, weniger ausführen.

Keine zusätzlichen Plattform-/Release-/Trimming-Matrizen, ABI-Prüfungen,
APK-/IPA-Analysen, Screenshots, manuellen UI-Prüfungen oder alternativen
Verifikationsverfahren ohne ausdrücklichen Auftrag.

Simulator-, Geräte- und manuelle UI-Läufe sind keine Standardverifikation. Wenn sie
für die konkrete Änderung als kleinste sinnvolle Runtime-Prüfung geeignet sind,
gelten Umfang und Abbruchkriterien aus `SETUP.md`. Unmittelbar vor der tatsächlichen
Ausführung eines Simulator-, Emulator- oder Gerätelaufs immer erneut die
ausdrückliche Bestätigung des Entwicklers einholen.

Schlägt eine notwendige Prüfung fehl, Ursache korrigieren und nur den betroffenen
notwendigen Schritt wiederholen. Bereits erfolgreiche unabhängige Prüfungen nicht
ohne konkreten Grund erneut ausführen.

Ist der Scope umgesetzt und die notwendige Verifikation erfolgreich, ist die Aufgabe
beendet. Keine zusätzlichen „zur Sicherheit“-Prüfungen starten.

## Git, Versionierung und Lieferung

Vor Änderungen `git status` prüfen; nach Änderungen `git diff` und `git status`.
Neue unversionierte Dateien zusätzlich direkt prüfen.

Agenten erstellen keine Commits, kein Staging und keinen Push. Keine Branches löschen
oder Git-History umschreiben, sofern der Benutzer dies nicht ausdrücklich neu anweist.

Für Versionswerte und `CHANGELOG.md` ausschließlich die für die konkrete Änderungsart
geltenden Regeln aus `SETUP.md` anwenden; deren Details hier nicht duplizieren.

Der Abschlussbericht bleibt kurz: Ergebnis, tatsächlich ausgeführte Verifikation,
offene relevante Punkte und Git-Status. Keine Tätigkeitschronik.
