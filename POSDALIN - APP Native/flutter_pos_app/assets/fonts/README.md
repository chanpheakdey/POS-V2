# Khmer Font Installation

This directory contains the Noto Sans Khmer fonts required for proper Khmer script rendering.

## Required Files

- `NotoSansKhmer-Regular.ttf`
- `NotoSansKhmer-Bold.ttf`

## Download Instructions

### Option 1: Automated Download (Recommended)

Run the provided PowerShell script:

```powershell
powershell -ExecutionPolicy Bypass -File download_fonts.ps1
```

### Option 2: Manual Download

1. Visit: https://fonts.google.com/noto/specimen/Noto+Sans+Khmer
2. Click "Download family"
3. Extract the zip file
4. Copy the following files to this directory:
   - `NotoSansKhmer[wdth,wght].ttf` → rename to `NotoSansKhmer-Regular.ttf`
   - `NotoSansKhmer[wdth,wght].ttf` → duplicate as `NotoSansKhmer-Bold.ttf` (it's a variable font)

## Alternative: Direct Download Links

- Regular: https://github.com/google/fonts/raw/main/ofl/notosanskhmer/NotoSansKhmer%5Bwdth%2Cwght%5D.ttf

Note: The font is a variable font that includes all weights (100-900), so the same file works for both regular and bold variants.

## Verification

After adding the fonts, run:

```bash
flutter clean
flutter pub get
```

Then build the app to verify the fonts are properly included in the asset bundle.
