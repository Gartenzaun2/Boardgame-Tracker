# C-Bounds-Fix: vollständiger Vergleich mit übernommenem A/B

A und B sind unverändert aus results.json übernommen, ausschließlich C wurde neu gemessen. Tripel = richtig/falsch/null; gleiche Nenner einschließlich Enthaltungen.

| Variante | Satz | n | Roh | Top1 roh | Nach E | High/Medium/Uncertain | Falsche High |
|---|---|---:|---|---|---|---|---:|
| A | calibration | 123 | 94/12/17 | 76,4 % | 87/9/27 | 38/23/62 | 0 |
| A | holdout | 60 | 45/8/7 | 75,0 % | 41/5/14 | 12/11/37 | 0 |
| A | v3 | 156 | 113/30/13 | 72,4 % | 109/28/19 | 48/32/76 | 8 |
| A | legacy6 | 72 | 60/11/1 | 83,3 % | 60/11/1 | 17/12/43 | 0 |
| B | calibration | 123 | 96/11/16 | 78,0 % | 91/10/22 | 41/21/61 | 0 |
| B | holdout | 60 | 45/6/9 | 75,0 % | 39/3/18 | 14/11/35 | 0 |
| B | v3 | 156 | 114/31/11 | 73,1 % | 108/29/19 | 47/33/76 | 10 |
| B | legacy6 | 72 | 59/12/1 | 81,9 % | 59/12/1 | 15/15/42 | 0 |
| C | calibration | 123 | 98/9/16 | 79,7 % | 89/8/26 | 40/22/61 | 0 |
| C | holdout | 60 | 43/8/9 | 71,7 % | 38/5/17 | 13/11/36 | 0 |
| C | v3 | 156 | 115/28/13 | 73,7 % | 110/26/20 | 49/31/76 | 10 |
| C | legacy6 | 72 | 60/11/1 | 83,3 % | 60/11/1 | 16/15/41 | 0 |

| Track/Farbe, über 411 vorhandene GT-Werte | A roh → E | B roh → E | C v2 roh → E |
|---|---|---|---|
| attractiveness | 100/25/12 → 96/22/19 | 99/26/12 → 96/23/18 | 100/23/14 → 96/21/20 |
| conservation | 96/24/17 → 89/22/26 | 102/21/14 → 93/20/24 | 104/19/14 → 93/18/26 |
| reputation | 116/12/9 → 112/9/16 | 113/13/11 → 108/11/18 | 112/14/11 → 108/11/18 |
| red | 87/13/2 → 87/12/3 | 84/16/2 → 84/15/3 | 83/17/2 → 83/16/3 |
| blue | 68/16/15 → 66/15/18 | 74/10/15 → 71/10/18 | 75/10/14 → 72/10/17 |
| yellow | 95/10/0 → 95/10/0 | 94/11/0 → 94/11/0 | 93/12/0 → 93/12/0 |
| black | 62/22/21 → 49/16/40 | 62/23/20 → 48/18/39 | 65/17/23 → 49/12/44 |

Alle Farb-/Track-/Confidence-Aufteilungen je Datensatz stehen zusätzlich in c-boundsfix-metrics.json, sämtliche Einzelentscheidungen und Registrierung je Foto in c-boundsfix-results.json.
