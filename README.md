# Boardgame Tracker – Scaffold v0.6.3.1

v0.6.3.1 ist ein Compile-Bugfix für v0.6.3.

## Behobene Compilerfehler

- `AppResources.Retry` und `AppResources.TechnicalDetails` werden nicht
  mehr als nicht vorhandene stark typisierte Resource-Properties
  referenziert. Die beiden ausschließlich im Startup-Fehlerbildschirm
  verwendeten Texte werden vorerst direkt dort gesetzt.
- Der Navigation-Guard referenziert nicht mehr `CreateGroupButton` aus
  dem Code-behind. Das vorhandene Busy-Overlay blockiert weitere
  Eingaben bereits zuverlässig; dadurch ist keine direkte Abhängigkeit
  von einem generierten XAML-Feld nötig.

## Verhalten

Die in v0.6.3 eingeführten Funktionen bleiben erhalten:

- fataler DB-Startup-Fehler stoppt die eigentliche App,
- `Erneut versuchen`,
- Logging und DEBUG-Fehlerdetails,
- Schutz gegen Mehrfachtaps,
- Busy-Feedback,
- Empty-State für vorhandene Personen,
- klareres Pastellgelb.

## Datenbank

Keine Schemaänderung. **Keine Migration erforderlich.**

## Upgrade von v0.6.3

1.  v0.6.3.1 über v0.6.3 kopieren.
2.  `tools\upgrade-from-v063.cmd` ausführen.
3.  Clean Solution.
4.  Rebuild Solution.
