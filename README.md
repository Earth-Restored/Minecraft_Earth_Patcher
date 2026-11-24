# Minecraft Earth Patcher

A cross-platform tool to patch the server endpoints in Minecraft Earth APK (Android) and IPA (iOS).

Available for **Windows**, **Linux**, **macOS**, and **Android**.

## Usage Instructions

1. Acquire a Minecraft Earth APK or IPA (e.g., dump it from your device).
2. Download and run the latest release of [Minecraft Earth Patcher](https://github.com/Earth-Restored/Minecraft_Earth_Patcher/releases).
3. Switch to the **Android** or **iOS** tab depending on your file.
4. Select your APK or IPA file.
5. *(Android only, optional)* Select a Minecraft Earth resource pack file.[^1]
6. Enter your server's locator and auth server address/hostname, including the port number:
    - For standard installations (`main`/`dev` branch), these will be the same (e.g., `your-ip:8080`).
    - For Aspire (experimental), check the "Patcher options" on the web portal's home page.
7. Click **Patch** and wait for the process to complete.
8. Installation:
    - If you are running the Android version of the patcher, tap **"Install APK"** to install it directly on your device.
    - Otherwise, click the **"Open output folder"** button, transfer the patched APK/IPA to your phone, and install it.
    - On iOS, the IPA has to be signed before you install it, see [Signing the IPA](#signing-the-ipa). Using AltStore is recommended.

[^1]: This allows using servers that only host partial resource packs, which is recommended for public servers to avoid copyright issues. On iOS, the IPA already bundles the full resource pack, so this is not required.

## Signing the IPA

The patcher doesn't sign the IPA. Xbox sign-in stores its tokens in two shared keychain groups, so the signed app's entitlements have to list both (`<TEAMID>` is your signing certificate's team ID):

- `<TEAMID>.com.microsoft.xal`
- `<TEAMID>.com.microsoft.adalcache`

Without them, sign-in fails before the app sends any request. To check a signed IPA:

```sh
unzip -q Solace-signed.ipa
codesign -d --entitlements - Payload/minecraftearthtf.app
```

## Credits

Patches are from the following repositories:

- [Project-Genoa/patches](https://github.com/Project-Genoa/patches)
- [Project-Earth-Team/Patches](https://github.com/Project-Earth-Team/Patches)
