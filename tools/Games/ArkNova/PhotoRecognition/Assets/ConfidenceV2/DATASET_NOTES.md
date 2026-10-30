# Arche Nova confidence dataset v2

- Downscaled to 2000 px long edge, JPEG quality 88.
- Originals were 3024x4032 / 4032x3024 and are not included in this ZIP.
- IMG_4264 was mentioned but was not present in the uploaded archive.
- `viewIndex` is capture order within a scenario, not a calibrated camera-angle label.
- Single-color mapping inferred from visible track pieces:
  - IMG_4265–4268: black
  - IMG_4269–4272: yellow
  - IMG_4273–4275: red
  - IMG_4276–4277: blue
- Four-color sets:
  - IMG_4278–4281: normal set A
  - IMG_4282–4285: normal set B
  - IMG_4286–4289: hard set C
- For single-color photos, absent colors are explicitly `null` in the ground truth so false positives can be measured.
