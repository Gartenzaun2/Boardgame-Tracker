## calibration

| Method | TP | FP | FN | Precision | Recall | F1 | colored Top1 / Top2 / Top3 | null | A wrong / readout right | Oracle | ms |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|
| C1 | 0 | 0 | 119 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 123 | 0 | 94 | 407,3 |
| C2a | 0 | 0 | 119 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 123 | 0 | 94 | 150,3 |
| C2b | 0 | 0 | 119 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 123 | 0 | 94 | 574,8 |
| C3 | 112 | 137 | 7 | 45,0 % | 94,1 % | 0,609 | 94 / 96 / 96 | 19 | 6 | 100 | 114,5 |

| Track C3 | TP | FP | FN | Precision | Recall | colored Top1 | null | A wrong / readout right |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| attractiveness | 40 | 111 | 1 | 26,5 % | 97,6 % | 32 | 5 | 1 |
| conservation | 35 | 16 | 6 | 68,6 % | 85,4 % | 29 | 7 | 5 |
| reputation | 37 | 10 | 0 | 78,7 % | 100,0 % | 33 | 7 | 0 |

| View C3 | TP | FP | FN | Precision | Recall | colored Top1 / total | null |
|---|---:|---:|---:|---:|---:|---|---:|
| unknown_v2 | 112 | 137 | 7 | 45,0 % | 94,1 % | 94/123 | 19 |

## old_holdout_reused

| Method | TP | FP | FN | Precision | Recall | F1 | colored Top1 / Top2 / Top3 | null | A wrong / readout right | Oracle | ms |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|
| C1 | 0 | 0 | 48 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 60 | 0 | 45 | 430,3 |
| C2a | 0 | 0 | 48 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 60 | 0 | 45 | 165,2 |
| C2b | 0 | 0 | 48 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 60 | 0 | 45 | 549,9 |
| C3 | 43 | 62 | 5 | 41,0 % | 89,6 % | 0,562 | 30 / 32 / 32 | 23 | 1 | 46 | 128,0 |

| Track C3 | TP | FP | FN | Precision | Recall | colored Top1 | null | A wrong / readout right |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| attractiveness | 15 | 44 | 1 | 25,4 % | 93,8 % | 13 | 4 | 1 |
| conservation | 9 | 9 | 3 | 50,0 % | 75,0 % | 6 | 11 | 0 |
| reputation | 19 | 9 | 1 | 67,9 % | 95,0 % | 11 | 8 | 0 |

| View C3 | TP | FP | FN | Precision | Recall | colored Top1 / total | null |
|---|---:|---:|---:|---:|---:|---|---:|
| unknown_v2 | 43 | 62 | 5 | 41,0 % | 89,6 % | 30/60 | 23 |

## v3_reused

| Method | TP | FP | FN | Precision | Recall | F1 | colored Top1 / Top2 / Top3 | null | A wrong / readout right | Oracle | ms |
|---|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|
| C1 | 0 | 0 | 148 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 156 | 0 | 103 | 399,2 |
| C2a | 0 | 0 | 148 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 156 | 0 | 103 | 152,7 |
| C2b | 0 | 0 | 148 | 0,0 % | 0,0 % | 0,000 | 0 / 0 / 0 | 156 | 0 | 103 | 584,1 |
| C3 | 116 | 64 | 32 | 64,4 % | 78,4 % | 0,707 | 100 / 100 / 100 | 34 | 12 | 115 | 118,1 |

| Track C3 | TP | FP | FN | Precision | Recall | colored Top1 | null | A wrong / readout right |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| attractiveness | 37 | 47 | 13 | 44,0 % | 74,0 % | 29 | 11 | 3 |
| conservation | 39 | 7 | 9 | 84,8 % | 81,2 % | 36 | 11 | 5 |
| reputation | 40 | 10 | 10 | 80,0 % | 80,0 % | 35 | 12 | 4 |

| View C3 | TP | FP | FN | Precision | Recall | colored Top1 / total | null |
|---|---:|---:|---:|---:|---:|---|---:|
| opposite_side | 19 | 12 | 5 | 61,3 % | 79,2 % | 15/24 | 6 |
| left | 19 | 14 | 5 | 57,6 % | 79,2 % | 19/24 | 1 |
| right | 19 | 21 | 5 | 47,5 % | 79,2 % | 18/24 | 1 |
| vertical_top_down | 42 | 7 | 10 | 85,7 % | 80,8 % | 33/60 | 22 |
| front | 7 | 6 | 5 | 53,8 % | 58,3 % | 6/12 | 2 |
| lower_side | 10 | 4 | 2 | 71,4 % | 83,3 % | 9/12 | 2 |
