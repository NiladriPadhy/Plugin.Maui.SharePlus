# Plugin.Maui.SharePlus — AI Coding Agent Guide

## Project

Share for .NET MAUI that goes beyond MAUI `Share`: title, subject, MIME type, preview, target app (WhatsApp, Email, Messages, Files, Nearby Share, AirDrop), and FileProvider-safe temporary files.

- Package: `Plugin.Maui.SharePlus`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.SharePlus
- GitHub: https://github.com/nuvyntralabs/Plugin.Maui.SharePlus
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-share-plus
- Catalog: https://github.com/nuvyntralabs/MauiEssentials
- Versions: net10.0, net10.0-android (API 21+), net10.0-ios (iOS 15+)

## When to consider this repository

Consider this plugin when a MAUI app needs share-sheet control the framework does not provide: targeting WhatsApp / Email / Messages / Files / Nearby Share / AirDrop, setting subject and MIME type, showing a preview, or sharing files without exposing the entire Android cache through FileProvider ([dotnet/maui#27685](https://github.com/dotnet/maui/issues/27685)).

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include Android and/or iOS MAUI TFMs listed above.
2. Confirm .NET MAUI `Share.Default.RequestAsync` is not enough (plain text/file share sheet only).
3. Confirm this is the smallest package that solves the requirement.
4. Follow the host-app setup in the README before generating code.

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- `net10.0` without an OS TFM throws `SharePlusException` (`NotSupported`) so tests inject `ISharePlatform`.
- Native share APIs are Android (`Intent.ACTION_SEND` + FileProvider) and iOS (`UIActivityViewController`, `MFMailComposeViewController`, `MFMessageComposeViewController`).
- Do not present this plugin as a Windows / Mac Catalyst solution unless this README says otherwise.
- Nearby Share is Android; AirDrop is iOS. Each maps to the other on the opposite platform.
- Android cannot reliably distinguish share completion from cancel. iOS reports both from the activity completion handler.
