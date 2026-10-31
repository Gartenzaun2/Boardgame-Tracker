## calibration

| Method | TP / FP / FN | Precision / Recall / F1 | colored Top1 / Top2 / Top3 | null | phantom | output accuracy | coverage | A wrong / right | A right / wrong | Oracle |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| A_legacy | 94/25/25 | 79,0 %/79,0 %/0,790 | 94/104/107 | 17 | 21 | 74,0 % | 86,2 % | 0 | 0 | 94 |
| A_hardened | 94/25/25 | 79,0 %/79,0 %/0,790 | 94/104/107 | 17 | 21 | 74,0 % | 86,2 % | 0 | 0 | 94 |
| A_accepted | 61/0/58 | 100,0 %/51,3 %/0,678 | 61/61/61 | 62 | 0 | 100,0 % | 49,6 % | 0 | 33 | 94 |
| P0 | 112/137/7 | 45,0 %/94,1 %/0,609 | 94/96/96 | 19 | 6 | 85,5 % | 84,6 % | 6 | 6 | 100 |
| P1 | 112/124/7 | 47,5 %/94,1 %/0,631 | 94/96/96 | 19 | 6 | 85,5 % | 84,6 % | 6 | 6 | 100 |
| P2 | 112/137/7 | 45,0 %/94,1 %/0,609 | 101/101/102 | 13 | 11 | 83,5 % | 89,4 % | 9 | 2 | 103 |
| F1 | 88/6/31 | 93,6 %/73,9 %/0,826 | 88/88/88 | 29 | 0 | 93,6 % | 76,4 % | 0 | 6 | 94 |
| F2 | 88/5/31 | 94,6 %/73,9 %/0,830 | 88/88/88 | 30 | 0 | 94,6 % | 75,6 % | 2 | 8 | 96 |

| Method / track | TP / FP / FN | colored Top1 / total | null | output accuracy |
|---|---|---|---:|---:|
| A_legacy/attractiveness | 34/14/7 | 34/41 | 4 | 69,4 % |
| A_legacy/conservation | 24/10/17 | 24/41 | 9 | 70,6 % |
| A_legacy/reputation | 36/1/1 | 36/41 | 4 | 81,8 % |
| A_hardened/attractiveness | 34/14/7 | 34/41 | 4 | 69,4 % |
| A_hardened/conservation | 24/10/17 | 24/41 | 9 | 70,6 % |
| A_hardened/reputation | 36/1/1 | 36/41 | 4 | 81,8 % |
| A_accepted/attractiveness | 29/0/12 | 29/41 | 12 | 100,0 % |
| A_accepted/conservation | 0/0/41 | 0/41 | 41 | 0,0 % |
| A_accepted/reputation | 32/0/5 | 32/41 | 9 | 100,0 % |
| P0/attractiveness | 40/111/1 | 32/41 | 5 | 84,2 % |
| P0/conservation | 35/16/6 | 29/41 | 7 | 85,3 % |
| P0/reputation | 37/10/0 | 33/41 | 7 | 86,8 % |
| P1/attractiveness | 40/98/1 | 32/41 | 5 | 84,2 % |
| P1/conservation | 35/16/6 | 29/41 | 7 | 85,3 % |
| P1/reputation | 37/10/0 | 33/41 | 7 | 86,8 % |
| P2/attractiveness | 40/111/1 | 36/41 | 2 | 85,7 % |
| P2/conservation | 35/16/6 | 28/41 | 8 | 80,0 % |
| P2/reputation | 37/10/0 | 37/41 | 3 | 84,1 % |
| F1/attractiveness | 31/1/10 | 31/41 | 9 | 96,9 % |
| F1/conservation | 23/5/18 | 23/41 | 13 | 82,1 % |
| F1/reputation | 34/0/3 | 34/41 | 7 | 100,0 % |
| F2/attractiveness | 29/0/12 | 29/41 | 12 | 100,0 % |
| F2/conservation | 25/5/16 | 25/41 | 11 | 83,3 % |
| F2/reputation | 34/0/3 | 34/41 | 7 | 100,0 % |

| Method / color | Top1 / Top2 / Top3 / total | null | A wrong / right | Oracle |
|---|---|---:|---:|---:|
| A_legacy/red | 28/28/28/30 | 0 | 0 | 28 |
| A_legacy/blue | 15/19/21/27 | 9 | 0 | 15 |
| A_legacy/yellow | 30/30/30/33 | 0 | 0 | 30 |
| A_legacy/black | 21/27/28/33 | 8 | 0 | 21 |
| A_hardened/red | 28/28/28/30 | 0 | 0 | 28 |
| A_hardened/blue | 15/19/21/27 | 9 | 0 | 15 |
| A_hardened/yellow | 30/30/30/33 | 0 | 0 | 30 |
| A_hardened/black | 21/27/28/33 | 8 | 0 | 21 |
| A_accepted/red | 19/19/19/30 | 11 | 0 | 28 |
| A_accepted/blue | 14/14/14/27 | 13 | 0 | 15 |
| A_accepted/yellow | 22/22/22/33 | 11 | 0 | 30 |
| A_accepted/black | 6/6/6/33 | 27 | 0 | 21 |
| P0/red | 27/28/28/30 | 0 | 0 | 28 |
| P0/blue | 18/18/18/27 | 9 | 3 | 18 |
| P0/yellow | 28/29/29/33 | 0 | 0 | 30 |
| P0/black | 21/21/21/33 | 10 | 3 | 24 |
| P1/red | 27/28/28/30 | 0 | 0 | 28 |
| P1/blue | 18/18/18/27 | 9 | 3 | 18 |
| P1/yellow | 28/29/29/33 | 0 | 0 | 30 |
| P1/black | 21/21/21/33 | 10 | 3 | 24 |
| P2/red | 28/28/28/30 | 0 | 0 | 28 |
| P2/blue | 15/15/15/27 | 12 | 1 | 16 |
| P2/yellow | 29/29/29/33 | 0 | 0 | 30 |
| P2/black | 29/29/30/33 | 1 | 8 | 29 |
| F1/red | 27/27/27/30 | 1 | 0 | 28 |
| F1/blue | 15/15/15/27 | 12 | 0 | 15 |
| F1/yellow | 27/27/27/33 | 3 | 0 | 30 |
| F1/black | 19/19/19/33 | 13 | 0 | 21 |
| F2/red | 27/27/27/30 | 1 | 0 | 28 |
| F2/blue | 14/14/14/27 | 13 | 0 | 15 |
| F2/yellow | 27/27/27/33 | 3 | 0 | 30 |
| F2/black | 20/20/20/33 | 13 | 2 | 23 |

| Method / view | TP / FP / FN | colored Top1 / total | null |
|---|---|---|---:|
| A_legacy/unknown_v2 | 94/25/25 | 94/123 | 17 |
| A_hardened/unknown_v2 | 94/25/25 | 94/123 | 17 |
| A_accepted/unknown_v2 | 61/0/58 | 61/123 | 62 |
| P0/unknown_v2 | 112/137/7 | 94/123 | 19 |
| P1/unknown_v2 | 112/124/7 | 94/123 | 19 |
| P2/unknown_v2 | 112/137/7 | 101/123 | 13 |
| F1/unknown_v2 | 88/6/31 | 88/123 | 29 |
| F2/unknown_v2 | 88/5/31 | 88/123 | 30 |

## old_holdout_reused

| Method | TP / FP / FN | Precision / Recall / F1 | colored Top1 / Top2 / Top3 | null | phantom | output accuracy | coverage | A wrong / right | A right / wrong | Oracle |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| A_legacy | 42/16/6 | 72,4 %/87,5 %/0,792 | 45/47/48 | 7 | 10 | 71,4 % | 88,3 % | 0 | 0 | 45 |
| A_hardened | 42/16/6 | 72,4 %/87,5 %/0,792 | 45/47/48 | 7 | 10 | 71,4 % | 88,3 % | 0 | 0 | 45 |
| A_accepted | 23/0/25 | 100,0 %/47,9 %/0,648 | 23/23/23 | 37 | 0 | 100,0 % | 38,3 % | 0 | 22 | 45 |
| P0 | 43/62/5 | 41,0 %/89,6 %/0,562 | 30/32/32 | 23 | 4 | 73,2 % | 61,7 % | 1 | 16 | 46 |
| P1 | 43/48/5 | 47,3 %/89,6 %/0,619 | 30/32/32 | 23 | 3 | 75,0 % | 61,7 % | 1 | 16 | 46 |
| P2 | 43/62/5 | 41,0 %/89,6 %/0,562 | 44/46/46 | 9 | 6 | 77,2 % | 85,0 % | 2 | 3 | 47 |
| F1 | 33/1/15 | 97,1 %/68,8 %/0,805 | 32/32/32 | 27 | 1 | 94,1 % | 55,0 % | 0 | 13 | 45 |
| F2 | 30/1/18 | 96,8 %/62,5 %/0,759 | 30/30/30 | 29 | 0 | 96,8 % | 51,7 % | 0 | 15 | 45 |

| Method / track | TP / FP / FN | colored Top1 / total | null | output accuracy |
|---|---|---|---:|---:|
| A_legacy/attractiveness | 15/8/1 | 14/20 | 2 | 60,9 % |
| A_legacy/conservation | 9/4/3 | 14/20 | 4 | 77,8 % |
| A_legacy/reputation | 18/4/2 | 17/20 | 1 | 77,3 % |
| A_hardened/attractiveness | 15/8/1 | 14/20 | 2 | 60,9 % |
| A_hardened/conservation | 9/4/3 | 14/20 | 4 | 77,8 % |
| A_hardened/reputation | 18/4/2 | 17/20 | 1 | 77,3 % |
| A_accepted/attractiveness | 10/0/6 | 10/20 | 10 | 100,0 % |
| A_accepted/conservation | 0/0/12 | 0/20 | 20 | 0,0 % |
| A_accepted/reputation | 13/0/7 | 13/20 | 7 | 100,0 % |
| P0/attractiveness | 15/44/1 | 13/20 | 4 | 68,4 % |
| P0/conservation | 9/9/3 | 6/20 | 11 | 66,7 % |
| P0/reputation | 19/9/1 | 11/20 | 8 | 84,6 % |
| P1/attractiveness | 15/30/1 | 13/20 | 4 | 72,2 % |
| P1/conservation | 9/9/3 | 6/20 | 11 | 66,7 % |
| P1/reputation | 19/9/1 | 11/20 | 8 | 84,6 % |
| P2/attractiveness | 15/44/1 | 14/20 | 3 | 73,7 % |
| P2/conservation | 9/9/3 | 14/20 | 4 | 82,4 % |
| P2/reputation | 19/9/1 | 16/20 | 2 | 76,2 % |
| F1/attractiveness | 14/0/2 | 13/20 | 7 | 92,9 % |
| F1/conservation | 6/1/6 | 6/20 | 13 | 85,7 % |
| F1/reputation | 13/0/7 | 13/20 | 7 | 100,0 % |
| F2/attractiveness | 11/0/5 | 11/20 | 9 | 100,0 % |
| F2/conservation | 6/1/6 | 6/20 | 13 | 85,7 % |
| F2/reputation | 13/0/7 | 13/20 | 7 | 100,0 % |

| Method / color | Top1 / Top2 / Top3 / total | null | A wrong / right | Oracle |
|---|---|---:|---:|---:|
| A_legacy/red | 12/12/12/15 | 2 | 0 | 12 |
| A_legacy/blue | 9/11/12/15 | 4 | 0 | 9 |
| A_legacy/yellow | 14/14/14/15 | 0 | 0 | 14 |
| A_legacy/black | 10/10/10/15 | 1 | 0 | 10 |
| A_hardened/red | 12/12/12/15 | 2 | 0 | 12 |
| A_hardened/blue | 9/11/12/15 | 4 | 0 | 9 |
| A_hardened/yellow | 14/14/14/15 | 0 | 0 | 14 |
| A_hardened/black | 10/10/10/15 | 1 | 0 | 10 |
| A_accepted/red | 7/7/7/15 | 8 | 0 | 12 |
| A_accepted/blue | 6/6/6/15 | 9 | 0 | 9 |
| A_accepted/yellow | 10/10/10/15 | 5 | 0 | 14 |
| A_accepted/black | 0/0/0/15 | 15 | 0 | 10 |
| P0/red | 11/12/12/15 | 3 | 0 | 12 |
| P0/blue | 8/8/8/15 | 6 | 1 | 10 |
| P0/yellow | 9/10/10/15 | 4 | 0 | 14 |
| P0/black | 2/2/2/15 | 10 | 0 | 10 |
| P1/red | 11/12/12/15 | 3 | 0 | 12 |
| P1/blue | 8/8/8/15 | 6 | 1 | 10 |
| P1/yellow | 9/10/10/15 | 4 | 0 | 14 |
| P1/black | 2/2/2/15 | 10 | 0 | 10 |
| P2/red | 12/12/12/15 | 3 | 0 | 12 |
| P2/blue | 9/10/10/15 | 4 | 1 | 10 |
| P2/yellow | 12/13/13/15 | 1 | 0 | 14 |
| P2/black | 11/11/11/15 | 1 | 1 | 11 |
| F1/red | 12/12/12/15 | 3 | 0 | 12 |
| F1/blue | 8/8/8/15 | 7 | 0 | 9 |
| F1/yellow | 10/10/10/15 | 4 | 0 | 14 |
| F1/black | 2/2/2/15 | 13 | 0 | 10 |
| F2/red | 11/11/11/15 | 4 | 0 | 12 |
| F2/blue | 7/7/7/15 | 8 | 0 | 9 |
| F2/yellow | 10/10/10/15 | 4 | 0 | 14 |
| F2/black | 2/2/2/15 | 13 | 0 | 10 |

| Method / view | TP / FP / FN | colored Top1 / total | null |
|---|---|---|---:|
| A_legacy/unknown_v2 | 42/16/6 | 45/60 | 7 |
| A_hardened/unknown_v2 | 42/16/6 | 45/60 | 7 |
| A_accepted/unknown_v2 | 23/0/25 | 23/60 | 37 |
| P0/unknown_v2 | 43/62/5 | 30/60 | 23 |
| P1/unknown_v2 | 43/48/5 | 30/60 | 23 |
| P2/unknown_v2 | 43/62/5 | 44/60 | 9 |
| F1/unknown_v2 | 33/1/15 | 32/60 | 27 |
| F2/unknown_v2 | 30/1/18 | 30/60 | 29 |

## v3_reused

| Method | TP / FP / FN | Precision / Recall / F1 | colored Top1 / Top2 / Top3 | null | phantom | output accuracy | coverage | A wrong / right | A right / wrong | Oracle |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| A_legacy | 101/25/47 | 80,2 %/68,2 %/0,737 | 103/115/116 | 24 | 0 | 78,0 % | 84,6 % | 0 | 10 | 113 |
| A_hardened | 107/26/41 | 80,5 %/72,3 %/0,762 | 113/125/126 | 13 | 0 | 79,0 % | 91,7 % | 0 | 0 | 113 |
| A_accepted | 65/14/83 | 82,3 %/43,9 %/0,573 | 66/66/66 | 76 | 0 | 82,5 % | 51,3 % | 0 | 47 | 113 |
| P0 | 116/64/32 | 64,4 %/78,4 %/0,707 | 100/100/100 | 34 | 0 | 82,0 % | 78,2 % | 7 | 20 | 120 |
| P1 | 116/56/32 | 67,4 %/78,4 %/0,725 | 100/100/100 | 35 | 0 | 82,6 % | 77,6 % | 7 | 20 | 120 |
| P2 | 116/64/32 | 64,4 %/78,4 %/0,707 | 114/115/115 | 14 | 0 | 80,3 % | 91,0 % | 13 | 12 | 126 |
| F1 | 99/18/49 | 84,6 %/66,9 %/0,747 | 101/101/101 | 37 | 0 | 84,9 % | 76,3 % | 0 | 12 | 113 |
| F2 | 102/18/46 | 85,0 %/68,9 %/0,761 | 104/104/104 | 34 | 0 | 85,2 % | 78,2 % | 3 | 12 | 116 |

| Method / track | TP / FP / FN | colored Top1 / total | null | output accuracy |
|---|---|---|---:|---:|
| A_legacy/attractiveness | 29/12/21 | 30/52 | 9 | 69,8 % |
| A_legacy/conservation | 35/8/13 | 37/52 | 7 | 82,2 % |
| A_legacy/reputation | 37/5/13 | 36/52 | 8 | 81,8 % |
| A_hardened/attractiveness | 31/13/19 | 32/52 | 6 | 69,6 % |
| A_hardened/conservation | 37/8/11 | 41/52 | 3 | 83,7 % |
| A_hardened/reputation | 39/5/11 | 40/52 | 4 | 83,3 % |
| A_accepted/attractiveness | 28/9/22 | 28/52 | 15 | 75,7 % |
| A_accepted/conservation | 0/0/48 | 0/52 | 52 | 0,0 % |
| A_accepted/reputation | 37/5/13 | 38/52 | 9 | 88,4 % |
| P0/attractiveness | 37/47/13 | 29/52 | 11 | 70,7 % |
| P0/conservation | 39/7/9 | 36/52 | 11 | 87,8 % |
| P0/reputation | 40/10/10 | 35/52 | 12 | 87,5 % |
| P1/attractiveness | 37/39/13 | 29/52 | 12 | 72,5 % |
| P1/conservation | 39/7/9 | 36/52 | 11 | 87,8 % |
| P1/reputation | 40/10/10 | 35/52 | 12 | 87,5 % |
| P2/attractiveness | 37/47/13 | 32/52 | 5 | 68,1 % |
| P2/conservation | 39/7/9 | 42/52 | 4 | 87,5 % |
| P2/reputation | 40/10/10 | 40/52 | 5 | 85,1 % |
| F1/attractiveness | 29/9/21 | 29/52 | 14 | 76,3 % |
| F1/conservation | 32/4/16 | 32/52 | 16 | 88,9 % |
| F1/reputation | 38/5/12 | 40/52 | 7 | 88,9 % |
| F2/attractiveness | 29/9/21 | 29/52 | 14 | 76,3 % |
| F2/conservation | 35/4/13 | 35/52 | 13 | 89,7 % |
| F2/reputation | 38/5/12 | 40/52 | 7 | 88,9 % |

| Method / color | Top1 / Top2 / Top3 / total | null | A wrong / right | Oracle |
|---|---|---:|---:|---:|
| A_legacy/red | 30/31/31/39 | 3 | 0 | 33 |
| A_legacy/blue | 27/30/30/39 | 3 | 0 | 29 |
| A_legacy/yellow | 30/31/31/39 | 3 | 0 | 33 |
| A_legacy/black | 16/23/24/39 | 15 | 0 | 18 |
| A_hardened/red | 33/34/34/39 | 0 | 0 | 33 |
| A_hardened/blue | 29/32/32/39 | 1 | 0 | 29 |
| A_hardened/yellow | 33/34/34/39 | 0 | 0 | 33 |
| A_hardened/black | 18/25/26/39 | 12 | 0 | 18 |
| A_accepted/red | 19/19/19/39 | 15 | 0 | 33 |
| A_accepted/blue | 17/17/17/39 | 18 | 0 | 29 |
| A_accepted/yellow | 21/21/21/39 | 13 | 0 | 33 |
| A_accepted/black | 9/9/9/39 | 30 | 0 | 18 |
| P0/red | 31/31/31/39 | 3 | 1 | 34 |
| P0/blue | 27/27/27/39 | 6 | 3 | 32 |
| P0/yellow | 25/25/25/39 | 7 | 0 | 33 |
| P0/black | 17/17/17/39 | 18 | 3 | 21 |
| P1/red | 31/31/31/39 | 3 | 1 | 34 |
| P1/blue | 27/27/27/39 | 7 | 3 | 32 |
| P1/yellow | 25/25/25/39 | 7 | 0 | 33 |
| P1/black | 17/17/17/39 | 18 | 3 | 21 |
| P2/red | 31/32/32/39 | 1 | 1 | 34 |
| P2/blue | 28/28/28/39 | 6 | 3 | 32 |
| P2/yellow | 28/28/28/39 | 3 | 0 | 33 |
| P2/black | 27/27/27/39 | 4 | 9 | 27 |
| F1/red | 32/32/32/39 | 2 | 0 | 33 |
| F1/blue | 26/26/26/39 | 9 | 0 | 29 |
| F1/yellow | 28/28/28/39 | 6 | 0 | 33 |
| F1/black | 15/15/15/39 | 20 | 0 | 18 |
| F2/red | 32/32/32/39 | 2 | 0 | 33 |
| F2/blue | 28/28/28/39 | 7 | 2 | 31 |
| F2/yellow | 28/28/28/39 | 6 | 0 | 33 |
| F2/black | 16/16/16/39 | 19 | 1 | 19 |

| Method / view | TP / FP / FN | colored Top1 / total | null |
|---|---|---|---:|
| A_legacy/opposite_side | 14/5/10 | 14/24 | 5 |
| A_legacy/left | 20/4/4 | 20/24 | 0 |
| A_legacy/right | 17/5/7 | 16/24 | 0 |
| A_legacy/vertical_top_down | 33/6/19 | 36/60 | 17 |
| A_legacy/front | 7/5/5 | 7/12 | 0 |
| A_legacy/lower_side | 10/0/2 | 10/12 | 2 |
| A_hardened/opposite_side | 14/5/10 | 14/24 | 5 |
| A_hardened/left | 20/4/4 | 20/24 | 0 |
| A_hardened/right | 17/5/7 | 16/24 | 0 |
| A_hardened/vertical_top_down | 39/7/13 | 46/60 | 6 |
| A_hardened/front | 7/5/5 | 7/12 | 0 |
| A_hardened/lower_side | 10/0/2 | 10/12 | 2 |
| A_accepted/opposite_side | 9/2/15 | 9/24 | 13 |
| A_accepted/left | 13/3/11 | 13/24 | 8 |
| A_accepted/right | 10/3/14 | 10/24 | 11 |
| A_accepted/vertical_top_down | 23/3/29 | 24/60 | 33 |
| A_accepted/front | 4/3/8 | 4/12 | 5 |
| A_accepted/lower_side | 6/0/6 | 6/12 | 6 |
| P0/opposite_side | 19/12/5 | 15/24 | 6 |
| P0/left | 19/14/5 | 19/24 | 1 |
| P0/right | 19/21/5 | 18/24 | 1 |
| P0/vertical_top_down | 42/7/10 | 33/60 | 22 |
| P0/front | 7/6/5 | 6/12 | 2 |
| P0/lower_side | 10/4/2 | 9/12 | 2 |
| P1/opposite_side | 19/11/5 | 15/24 | 6 |
| P1/left | 19/12/5 | 19/24 | 1 |
| P1/right | 19/18/5 | 18/24 | 1 |
| P1/vertical_top_down | 42/6/10 | 33/60 | 23 |
| P1/front | 7/6/5 | 6/12 | 2 |
| P1/lower_side | 10/3/2 | 9/12 | 2 |
| P2/opposite_side | 19/12/5 | 17/24 | 3 |
| P2/left | 19/14/5 | 19/24 | 0 |
| P2/right | 19/21/5 | 18/24 | 0 |
| P2/vertical_top_down | 42/7/10 | 43/60 | 9 |
| P2/front | 7/6/5 | 7/12 | 1 |
| P2/lower_side | 10/4/2 | 10/12 | 1 |
| F1/opposite_side | 13/2/11 | 13/24 | 9 |
| F1/left | 20/4/4 | 20/24 | 0 |
| F1/right | 16/4/8 | 16/24 | 4 |
| F1/vertical_top_down | 36/4/16 | 38/60 | 18 |
| F1/front | 6/4/6 | 6/12 | 2 |
| F1/lower_side | 8/0/4 | 8/12 | 4 |
| F2/opposite_side | 15/2/9 | 15/24 | 7 |
| F2/left | 20/4/4 | 20/24 | 0 |
| F2/right | 16/4/8 | 16/24 | 4 |
| F2/vertical_top_down | 36/4/16 | 38/60 | 18 |
| F2/front | 6/4/6 | 6/12 | 2 |
| F2/lower_side | 9/0/3 | 9/12 | 3 |
