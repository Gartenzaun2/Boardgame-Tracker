# Third-Party License Policy

## Ziel

Boardgame Tracker soll dauerhaft sowohl kommerziell als auch unentgeltlich vertrieben werden können und die Freiheit behalten, den eigenen Quellcode wahlweise proprietär/Closed Source oder ausdrücklich Open Source bereitzustellen.

Neue Drittanbieter-Bibliotheken, Frameworks, native Komponenten, Assets oder sonstige Abhängigkeiten dürfen deshalb nur verwendet werden, wenn ihre Lizenz mit diesen Zielen vereinbar ist.

## Grundregel

Neue Drittanbieter-Abhängigkeiten müssen eine Lizenz besitzen, die mindestens Folgendes erlaubt:

- kommerzielle Nutzung und Monetarisierung;
- kostenlose Nutzung und Distribution;
- Nutzung in einer proprietären / Closed-Source-Anwendung;
- Nutzung in einer Open-Source-Anwendung;
- Weitergabe in kompilierter bzw. binärer Form;
- Weitergabe des Quellcodes, sofern das Projekt dies ausdrücklich möchte;
- Modifikation;
- Distribution über App Stores und andere übliche Vertriebskanäle.

Die Lizenz darf insbesondere nicht verlangen, dass der gesamte eigene Boardgame-Tracker-Quellcode veröffentlicht, unter dieselbe Lizenz gestellt oder anderweitig in seiner künftigen Lizenzwahl eingeschränkt wird.

Übliche Pflichten wie Copyright-Hinweise, vollständige Lizenztexte, Attributionen oder NOTICE-Dateien sind zulässig und müssen korrekt erfüllt werden.

## Bevorzugte Lizenzen

Permissive Open-Source-Lizenzen sind grundsätzlich zu bevorzugen, insbesondere:

- Apache License 2.0;
- MIT License;
- BSD 2-Clause;
- BSD 3-Clause;
- ISC License;
- zlib License.

Andere permissive Open-Source-Lizenzen können ebenfalls verwendet werden, müssen aber vor Aufnahme anhand der tatsächlich verwendeten Version geprüft werden.

## Nicht ohne ausdrückliche Prüfung verwenden

Abhängigkeiten mit Copyleft-, File-Level-Copyleft- oder vergleichbaren Verpflichtungen dürfen nicht automatisch aufgenommen werden. Dazu gehören insbesondere:

- GPL;
- AGPL;
- LGPL;
- MPL;
- SSPL und andere Source-Available-Lizenzen;
- Dual-Licensing-Modelle mit unklaren Bedingungen für kommerzielle oder proprietäre Nutzung;
- sonstige Lizenzen, deren Pflichten von der Einbindungs-, Link- oder Distributionsform abhängen.

Diese Kategorien sind nicht pauschal als technisch oder rechtlich unbrauchbar einzustufen. Ihre Verwendung benötigt jedoch vor der Integration eine ausdrückliche Projektentscheidung und eine Prüfung der konkreten Lizenzfolgen für die tatsächliche Einbindungs- und Vertriebsform.

## Ohne ausdrückliche Projektentscheidung nicht zulässig

Keine neue Komponente aufnehmen, deren Lizenz beispielsweise:

- kommerzielle Nutzung oder Monetarisierung verbietet oder einschränkt;
- nur nicht-kommerzielle Nutzung gestattet;
- bestimmte Geschäftsfelder, Nutzergruppen oder allgemein zulässige Nutzungszwecke ausschließt;
- App-Store-Distribution verbietet oder wesentlich einschränkt;
- die Offenlegung oder Relizenzierung des gesamten Boardgame-Tracker-Quellcodes erzwingt;
- eine kostenpflichtige kommerzielle Lizenz voraussetzt, die nicht ausdrücklich beschlossen wurde;
- keine ausreichenden Rechte zur Weitergabe der Anwendung gewährt;
- keine ausreichenden Rechte zur gewünschten Modifikation oder Weitergabe des betroffenen Drittanbieter-Codes gewährt.

## Prüfung neuer Abhängigkeiten

Vor Aufnahme einer neuen Third-Party-Dependency mindestens prüfen und nachvollziehbar festhalten:

1. Name und tatsächlich verwendete Version;
2. maßgebliche Lizenz bzw. Lizenzen;
3. kommerzielle Nutzung und Monetarisierung erlaubt;
4. proprietäre/Closed-Source-Nutzung erlaubt;
5. Open-Source-Nutzung erlaubt;
6. Binärdistribution erlaubt;
7. Quellcodeweitergabe und Modifikation im gewünschten Umfang erlaubt;
8. Copyleft-, Source-Disclosure- oder Relizenzierungspflichten;
9. Attribution-, Copyright- und NOTICE-Pflichten;
10. besondere Patent-, Marken- oder Werbehinweise;
11. Kompatibilität mit Android-/iOS-App-Store-Distribution und dem geplanten Vertriebsmodell.

Die Prüfung bezieht sich auf die tatsächlich verwendete Version und den tatsächlich ausgelieferten Bestandteil, nicht nur auf allgemeine Angaben zum Upstream-Projekt.

Wenn die Lizenzlage nicht eindeutig ist, wird die Dependency nicht eingebunden, bis die Vereinbarkeit mit dieser Policy geklärt ist.

## Dokumentation und Auslieferung

Für tatsächlich ausgelieferte Drittanbieter-Komponenten müssen erforderliche Lizenztexte, Copyright-Hinweise, Attributionen und NOTICE-Inhalte erhalten bleiben und entsprechend der jeweiligen Lizenz mit der Anwendung bereitgestellt werden.

Soweit sinnvoll, werden Third-Party-Informationen zentral erfasst mit:

- Komponentenname;
- Version;
- Lizenzname;
- SPDX-Identifier, sofern eindeutig;
- Lizenztext;
- erforderlichen Attributionen bzw. NOTICE-Angaben.

Die In-App-Anzeige von Open-Source-/Third-Party-Lizenzen ersetzt keine darüber hinausgehenden Pflichten der jeweiligen Lizenz, ist aber der bevorzugte Ort für nutzerlesbare Lizenz- und Attributionshinweise.

## Eigener Anwendungscode

Die Lizenz einer eingebundenen Drittanbieter-Komponente ändert nicht automatisch die Lizenz des Boardgame Trackers. Die Auswahl von Dependencies soll gerade verhindern, dass die künftige Lizenzierungsfreiheit des eigenen Anwendungscodes unbeabsichtigt eingeschränkt wird.

Der eigene Anwendungscode soll die Option behalten, je nach späterer Produktentscheidung:

- proprietär / Closed Source;
- kostenlos;
- kommerziell;
- oder ausdrücklich unter einer Open-Source-Lizenz

bereitgestellt zu werden.

## Projektregel

Lizenzkompatibilität ist Teil der technischen Dependency-Auswahl und keine nachträgliche Release-Aufgabe.

Neue Drittanbieter-Abhängigkeiten nur aufnehmen, wenn ihre Lizenz geprüft und ihre Vereinbarkeit mit dieser Policy bestätigt wurde. Im Zweifel nicht integrieren und zuerst eine ausdrückliche Entscheidung einholen.
