@ECHO OFF
CLS

REM This is an example script to show how to use the Help Library Manager
REM Launcher to remove an MS Help Viewer file.  You can use this as an example
REM for creating a script to run from your product's uninstaller.

REM NOTE: If not executed from within the same folder as the executable, a
REM full path is required on the executable.

HelpLibraryManagerLauncher.exe /product "{@CatalogProductId}" /version "{@CatalogVersion}" /locale {@Locale} /uninstall /silent /vendor "{@VendorName}" /mediaBookList "{@HelpTitle}" /productName "{@ProductTitle}"
