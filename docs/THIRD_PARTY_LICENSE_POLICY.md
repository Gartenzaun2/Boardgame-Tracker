# Boardgame Tracker – Third-Party License Policy

## 1. Zweck

Diese Policy definiert die Lizenzanforderungen für neue Drittanbieter-Bibliotheken,
Frameworks, native Komponenten, Assets und andere ausgelieferte Abhängigkeiten.

Boardgame Tracker soll die Freiheit behalten, künftig:

- kommerziell oder unentgeltlich vertrieben zu werden;
- proprietär / Closed Source zu bleiben;
- oder ausdrücklich Open Source veröffentlicht zu werden.

Eine Dependency darf diese Wahl nicht unbeabsichtigt einschränken.

## 2. Grundanforderungen

Neue Drittanbieter-Abhängigkeiten müssen für die tatsächlich verwendete Version und
Einbindungsform mindestens erlauben:

- kommerzielle Nutzung und Monetarisierung;
- unentgeltliche Nutzung und Distribution;
- Nutzung in proprietärer / Closed-Source-Software;
- Nutzung in Open-Source-Software;
- Binärdistribution;
- Modifikation;
- Quellcodeweitergabe, falls das Projekt dies später möchte;
- Distribution über App Stores und übliche Vertriebskanäle.

Übliche Pflichten wie Lizenztext, Copyright-Hinweis, Attribution oder NOTICE sind
zulässig, müssen aber vollständig erfüllt werden.

Eine Dependency darf ohne ausdrückliche Projektentscheidung insbesondere nicht
erzwingen, dass der gesamte Boardgame-Tracker-Quellcode veröffentlicht oder unter
eine bestimmte Lizenz gestellt wird.

## 3. Bevorzugte Lizenzen

Bevorzugt werden permissive Open-Source-Lizenzen, insbesondere:

```text
Apache-2.0
MIT
BSD-2-Clause
BSD-3-Clause
ISC
Zlib
```

Andere permissive Lizenzen sind ebenfalls möglich, müssen aber für die tatsächlich
verwendete Version geprüft werden.

## 4. Nur nach ausdrücklicher Prüfung

Copyleft-, File-Level-Copyleft-, Source-Available- oder vergleichbare Modelle werden
nicht automatisch integriert, insbesondere:

```text
GPL
AGPL
LGPL
MPL
SSPL
Dual Licensing mit relevanten Nutzungseinschränkungen
```

Diese Kategorien sind nicht pauschal unzulässig. Vor Integration muss jedoch
ausdrücklich geklärt werden, welche Folgen die konkrete Lizenz für die tatsächliche
Einbindungs-, Link- und Distributionsform hat.

Dasselbe gilt für jede andere Lizenz, deren Pflichten von der technischen Nutzung
oder dem Vertriebsmodell abhängen.

## 5. Ohne ausdrückliche Projektentscheidung nicht zulässig

Nicht integrieren, wenn die maßgebliche Lizenz beispielsweise:

- kommerzielle Nutzung oder Monetarisierung verbietet bzw. wesentlich beschränkt;
- nur nicht-kommerzielle Nutzung erlaubt;
- bestimmte Geschäftsfelder, Nutzergruppen oder allgemein zulässige Nutzungszwecke
  ausschließt;
- App-Store-Distribution verbietet oder wesentlich einschränkt;
- Veröffentlichung oder Relizenzierung des gesamten eigenen Anwendungscodes
  erzwingt;
- eine nicht beschlossene kostenpflichtige kommerzielle Lizenz voraussetzt;
- keine ausreichenden Rechte für Distribution oder notwendige Modifikation gewährt.

Ist die Lizenzlage nicht eindeutig, wird die Dependency nicht integriert, bis die
Vereinbarkeit mit dieser Policy geklärt ist.

## 6. Prüfung einer neuen Dependency

Vor Aufnahme mindestens prüfen und nachvollziehbar festhalten:

1. Name und tatsächlich verwendete Version;
2. maßgebliche Lizenz bzw. Lizenzen;
3. kommerzielle und proprietäre Nutzung;
4. Binärdistribution und gewünschte Modifikation/Quellcodeweitergabe;
5. Copyleft-, Source-Disclosure- oder Relizenzierungspflichten;
6. Copyright-, Attribution- und NOTICE-Pflichten;
7. relevante Patent-, Marken- oder Werbehinweise;
8. Vereinbarkeit mit Android-/iOS-App-Store-Distribution und dem geplanten
   Vertriebsmodell.

Maßgeblich sind die **tatsächlich verwendete Version und der tatsächlich
ausgelieferte Bestandteil**, nicht nur allgemeine Angaben zum Upstream-Projekt.

## 7. Umfang der Prüfung

Eine konkrete Dependency-Prüfung ist kein automatischer vollständiger
Repository-License-Audit.

Ohne ausdrücklichen Audit-Auftrag insbesondere nicht:

- den gesamten transitiven NuGet-Baum untersuchen;
- .NET-/Mono-Runtime oder SDK/NDK vollständig inventarisieren;
- eine projektweite Lizenz-/Provenance-Inventur erstellen;
- zusätzliche externe Quellen recherchieren, wenn die konkret ausgelieferten
  Lizenzinformationen die Frage bereits eindeutig beantworten.

Bei einer neuen Dependency endet die Prüfung, sobald ihre konkrete Einbindung und
die tatsächlich daraus entstehenden Distributionspflichten ausreichend geklärt und
erfüllt sind.

Bei nativen Bibliotheken werden die unmittelbar mit ausgelieferten bzw. statisch
eingebundenen Lizenz-/NOTICE-Pflichten berücksichtigt.

Ein vollständiger Dependency-License-Audit ist ein eigener Auftrag.

## 8. Dokumentation und Auslieferung

Für tatsächlich ausgelieferte Drittanbieter-Komponenten müssen alle erforderlichen:

- Lizenztexte;
- Copyright-Hinweise;
- Attributionen;
- NOTICE-Inhalte

erhalten und entsprechend der jeweiligen Lizenz bereitgestellt werden.

Soweit sinnvoll, werden dafür zentral festgehalten:

```text
Komponentenname
Version
Lizenzname
SPDX-Identifier, falls eindeutig
Lizenztext
erforderliche Attribution / NOTICE
```

Die In-App-Anzeige ist der bevorzugte Ort für nutzerlesbare Third-Party-Hinweise.
Sie ersetzt keine darüber hinausgehende Pflicht der jeweiligen Lizenz.

## 9. Projektregel

Lizenzkompatibilität ist Teil der **Dependency-Auswahl**, nicht erst eine
nachträgliche Release-Aufgabe.

Neue Drittanbieter-Abhängigkeiten nur integrieren, wenn ihre Lizenz geprüft und mit
dieser Policy vereinbar ist. Im Zweifel zuerst eine ausdrückliche Projektentscheidung
einholen.
