# Repository Guidelines

## Projekt und Struktur
Boardgame Tracker ist eine bestehende mobile .NET-MAUI-App zur Erfassung und Auswertung von Brettspielpartien; Arche Nova ist das erste vollständig unterstützte Spiel. Ohne ausdrücklichen Auftrag weder neu aufbauen noch grundlegend umstrukturieren.

`BoardgameTracker.sln` bündelt vier Projekte unter `src/`; `tests/` enthält Core- und Application-Tests (xUnit v3/Microsoft Testing Platform). Ressourcen und Lokalisierung liegen unter `src/BoardgameTracker.App/Resources/`.

## Quellen der Wahrheit
Vor relevanten Änderungen die zuständigen Dokumente lesen:

| Entscheidung | Maßgebliche Datei |
|---|---|
| Produktverhalten und Anforderungen | `docs/PRODUCT_SPEC.md` |
| Architektur und Abhängigkeiten | `docs/ARCHITECTURE.md` |
| Domain und Persistenzmodell | `docs/DATA_MODEL.md` |
| UI-Regeln | `docs/UI_DESIGN_GUIDE.md` |
| Screens und Navigation | `docs/UI_WIREFRAME.md` |
| Umgebung, Build, Tests, Versionierung, Lieferung | `SETUP.md` |
| Projektüberblick | `README.md` |
| Historische Änderungen | `CHANGELOG.md` |

Die kanonischen Spezifikationen liegen ausschließlich unter `docs/`; keine gleichnamigen Kopien an anderen Stellen anlegen. Dokumentation nicht durch Annahmen ersetzen. Widersprüche zwischen Code und Dokumentation untersuchen; bei fachlich oder architektonisch relevanten Entscheidungen nachfragen, statt stillschweigend eine Seite zu wählen. Keine Anforderungen zum Füllen von Lücken erfinden.

## Architektur
Modularer Monolith mit pragmatischen Vertical Slices:

```text
App -> Application -> Core
Infrastructure -> Application + Core
```

- Core: Domainmodell, Invarianten, Berechnungen, reine Fachregeln.
- Application: Use Cases, Orchestrierung, Read Models, notwendige technische Ports.
- Infrastructure: EF Core, SQLite, DbContext, technische Implementierungen.
- App: MAUI, XAML, ViewModels, Navigation, Presentation, Lokalisierung, Composition Root.

Autoritative Fachregeln gehören nicht in ViewModels; Feature-ViewModels kennen keine konkreten EF-/SQLite-Implementierungen. Infrastructure nur im technischen Startup/Composition Root der App anbinden. Vertical Slices und Composition erhalten; Arche-Nova-Logik bleibt unter `Games/ArkNova/`. Allgemeine Gruppen-Commands, Read Models und Persistenzports dürfen keine spielspezifischen Typen oder festverdrahteten Farbregeln enthalten. Gemeinsame semantische `PlayerColor`-Werte sind erlaubt; jedes Spiel registriert seine erlaubte Teilmenge und einen eigenen Farb-Persistenzadapter. Gruppenformulare bleiben allgemein. Der Architekturtest für allgemeine Gruppenverträge muss erhalten bleiben. Domänenbegriffe `Game`, `GameSession`, `Group`, `Player` beibehalten.

Keine generischen Repositories, kein MediatR, keine unnötigen Manager-/Service-/Helper-/Utils-Abstraktionen und keine Interfaces ausschließlich für Mockbarkeit oder Symmetrie einführen.

Navigationsparameter ausschließlich über gemeinsame Konstanten aus `App/Navigation/NavigationParameters.cs` senden und in `ApplyQueryAttributes` auslesen; keine String-Literale für Schlüssel. Details: `docs/ARCHITECTURE.md`.

## Lesbarkeit und Typdeklarationen

Kleine, fachlich zugehörige Begleittypen dürfen in derselben Datei wie ihr Haupttyp stehen; keine Pflicht „ein Typ pro Datei“. Reine Implementierungsdetails möglichst privat verschachteln. Eigenständige Fachbegriffe, gemeinsame Verträge und Typen mit anderer Modul-/Schichtzugehörigkeit separat lassen. XAML-, DI- und EF-Verwendungen bei der Sichtbarkeit beachten.

Einfache oder kurze Typen explizit schreiben, insbesondere `string?`, `bool`, `int`, `Guid`, Enums und gut lesbare Modelltypen. `var` nur für komplexe Typen verwenden, wenn es die Lesbarkeit verbessert, z. B. lange verschachtelte generische `IEnumerable`-/Dictionary-Typen; anonyme Typen benötigen `var`. Ein offensichtlicher Initialisierer ist allein kein Grund für `var`. Beispiele: `string? gameKey = settings.SelectedGameKey;`, `Guid? groupId = selection;`. Keine pauschale nachträgliche Umstellung unberührten Codes.

Bei `if` mit genau einer einfachen Anweisung keine geschweiften Klammern verwenden; die Anweisung steht eingerückt auf einer eigenen Zeile (z. B. `if (isBusy)` und darunter `return;`). Ausnahme: Bei verschachtelten `if`-Anweisungen Klammern für eine eindeutige Block-/`else`-Zuordnung verwenden. Mehrere Anweisungen verwenden ebenfalls einen mehrzeiligen Klammerblock. Die öffnende Klammer darf auf derselben Zeile wie der zugehörige Ausdruck stehen, z. B. `if (condition) {` oder `finally {`; der Inhalt steht auf eigenen Zeilen. Keine Anweisung direkt hinter der Bedingung auf derselben Zeile und kein vollständiger `finally`-Block auf einer Zeile.

Eine Anweisung pro Zeile; komplexe Abläufe durch Absätze, benannte Zwischenschritte und bei Bedarf private Methoden gliedern. Projekt-Typnamen über `using` einbinden, sofern kein Namenskonflikt besteht. Keine festen Zeilengrenzen oder zusätzlichen Wrapper allein zur Aufteilung einführen.

## Vorgehen und Entscheidungsgrenze
Vor jeder Änderung betroffene Dateien und relevante Dokumentation lesen, die Implementierung verstehen und Auswirkungen auf Tests, Persistenz, UI und Dokumentation prüfen. Danach die kleinste sinnvolle Änderung durchführen; funktionierenden Code nicht unnötig neu schreiben.

Fehler, Ungereimtheiten und Verbesserungsmöglichkeiten außerhalb des aktuellen Auftrags nur benennen und mit dem Benutzer besprechen; nicht automatisch korrigieren oder als beiläufige Bereinigung aufnehmen. Solche Änderungen erst nach ausdrücklicher Beauftragung umsetzen. Ist eine auftragsfremde Korrektur Voraussetzung für die Aufgabe, die Abhängigkeit erklären und vor der Umsetzung abstimmen.

Keine Architektur-, Persistenz- oder Paketänderungen außerhalb des Auftrags; neue NuGet-Pakete nur bei klarem Bedarf. Kleine technische Entscheidungen innerhalb bestehender Spezifikation selbstständig treffen. Vor Änderungen an Architekturgrundsätzen, grundlegendem Datenmodell, Persistenzstrategie/Migration, wesentlichen Benutzerabläufen, größeren Abhängigkeiten oder bestehenden Anforderungen nachfragen; ebenso bei nicht spezifiziertem Fachverhalten.

Neue/geänderte Fachlogik geeignet testen. Tests niemals für einen grünen Lauf entfernen, abschwächen oder umgehen. Sichtbare Texte über bestehende Lokalisierung führen; UI-Änderungen anhand beider UI-Dokumente prüfen.

## Verifikation und Lieferung
Relevante Tests, bei Bedarf die breitere Suite und soweit lokal möglich den Build ausführen. Beispiele ab Solution-Root:

```powershell
dotnet build BoardgameTracker.sln
dotnet test --project tests/BoardgameTracker.Core.Tests
dotnet test --project tests/BoardgameTracker.Application.Tests
```

Build-/Restore-Voraussetzungen und relevante Geräte-Smoke-Tests gemäß `SETUP.md` beachten. `git diff` auf unbeabsichtigte Änderungen prüfen, einschließlich neuer Dateien. Fehlgeschlagene/nicht ausführbare Prüfungen ausdrücklich nennen; nur tatsächlich ausgeführte Verifikation als erfolgreich melden.

Pre-Alpha verwendet `EnsureCreatedAsync`; die dokumentierte Ausnahme ist der atomare, verlustfreie SQLite-Upgrade der Arche-Nova-Spezialkarten. Keine pauschalen DB-Resets für dieses Update. Versions- und Lieferregeln aus `SETUP.md` einhalten; keine Lieferarchive ohne ausdrücklichen Auftrag erzeugen.

## Dokumentation und Git
Vor Änderungen `git status` prüfen. Bereits vorhandene, nicht zum aktuellen Auftrag gehörende Änderungen des Benutzers niemals zurücksetzen, überschreiben oder verwerfen. Nach Abschluss erneut `git status` und `git diff` prüfen. Neue, noch unversionierte Dateien zusätzlich direkt prüfen, da `git diff` sie nicht anzeigt.

Dokumentation nur bei geändertem gültigem Projektstand aktualisieren; Versionshistorie ausschließlich in `CHANGELOG.md` pflegen. Lokale Dateien im Rahmen ausdrücklich erteilter Implementierungsaufträge bearbeiten; vorhandene fremde Änderungen bewahren.

Commit und Push ausschließlich auf ausdrückliche Anweisung, vorher Änderungen und ausgeführte Tests zusammenfassen. Ohne ausdrücklichen Auftrag keine Branches löschen oder History umschreiben.

## Abschlussbericht
Kurz nennen: Änderungen und wichtige Dateien, tatsächlich ausgeführte Tests/Builds samt Ergebnis, offene Probleme/Entscheidungen, Dokumentationsanpassungen und Bereitschaft für Commit/Push. Bereitschaft ist keine Erlaubnis zur Ausführung.
