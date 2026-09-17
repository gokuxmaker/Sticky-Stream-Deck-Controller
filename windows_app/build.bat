@echo off
echo =====================================================================
echo  Compiling Seeed reTerminal Sticky Stream Deck Manager (.EXE)
echo =====================================================================

set "CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC_PATH%" set "CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC_PATH%" (
    echo [ERROR] Microsoft .NET Framework csc.exe compiler not found.
    exit /b 1
)

echo [1/2] Compiling ReTerminalStreamDeck.exe...
"%CSC_PATH%" /target:winexe /optimize+ /platform:anycpu /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll /out:ReTerminalStreamDeck.exe StreamDeckManager.cs

if %ERRORLEVEL% equ 0 (
    echo.
    echo =====================================================================
    echo  [SUCCESS] Build Complete!
    echo  Output: ReTerminalStreamDeck.exe
    echo =====================================================================
) else (
    echo.
    echo [ERROR] Build failed with error code %ERRORLEVEL%.
)
