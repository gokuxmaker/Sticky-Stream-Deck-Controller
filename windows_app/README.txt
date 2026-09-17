reTerminal Sticky - Stream Deck Host (Seeed Studio Edition)
===========================================================

Files in this folder:
- ReTerminalStreamDeck.exe  : Ready-to-run Windows application.
- streamdeck_config.json    : Button mappings and application preferences.
- StreamDeck_Bitmap.h       : Default 800x480 E-Paper UI bitmap header.
- custom_ui_800x480.png     : Active customized 800x480 E-Paper artwork.
- StreamDeckManager.cs      : C# source code.
- build.bat                 : One-click build script using stock Windows .NET Framework.

How to Use:
1. Connect your reTerminal Sticky to your PC via USB-C.
2. Double-click ReTerminalStreamDeck.exe.
3. The app will automatically connect to the device's COM port (115200 baud).
4. Tap any physical button on the reTerminal Sticky or configure keys in the UI.
5. To customize the display image, click '800x480 E-Paper Image', pick an image,
   and click 'Upload to reTerminal' for live USB sync!