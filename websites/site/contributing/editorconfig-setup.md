---
uid: contributing/editorconfig-setup
---

# EditorConfig Setup

This project uses style rules setup in .editorconfig files. This allows us to manage style rules such as whitespace and indent style so they are consistent between different contributors and will create smaller diffs to review. To make pull requests go smoothly, it is important that each contributor ensure that these rules are respected. The best way is to use an editor that supports .editorconfig styles and to configure it to detect these files automatically.

### 🧠 Visual Studio
- Go to: **Tools > Options > Text Editor > [Language] > Code Style > General**
- Enable: **"Detect EditorConfig settings"**
- Optionally enable: **"Reformat on save"** (under Code Cleanup)

### 🧰 JetBrains Rider
- Go to: **Preferences > Editor > Code Style**
- Enable: **"Enable EditorConfig support"**
- Re-save the affected files to apply the formatting

### 💡 Visual Studio Code
- Install the **EditorConfig for VS Code** extension
- In your `settings.json`, enable:
  - `"files.insertFinalNewline": true`
  - `"files.trimTrailingWhitespace": true`

### 📝 Notepad++
- Go to: **Settings > Preferences > MISC**
  - Enable: **"Final line ending"**
- Go to: **Settings > Preferences > Language > Tab Settings**
  - Enable: **"Replace by space"** and configure indentation properly

### 🔧 Other Common Editors for .NET
- **JetBrains ReSharper** (for Visual Studio)
- **OmniSharp**-based editors (NeoVim, Sublime Text, etc.)
- **Vim** with [`editorconfig-vim`](https://github.com/editorconfig/editorconfig-vim)
- **Emacs** with [`editorconfig-emacs`](https://github.com/editorconfig/editorconfig-emacs)

### 🌍 Not Sure If Your Editor Supports It?
Check these official lists:
- Editors with built-in support: https://editorconfig.org/#pre-installed
- Editors with plugin support: https://editorconfig.org/#download

---

✅ After configuring your editor, **open each file with errors, make a whitespace-only change, and save**. This will apply your formatting settings and fix the rule violations.

## Pull Request Workflow

We highly recommend using an editor that supports .editorconfig and configure it to respect the formatting rules. However, when submitting pull request, there is an EditorConfig Rules Check workflow that will compare the styles in the PR against the .editorconfig and report any violations. This uses the [editorconfig-checker tool](https://github.com/editorconfig-checker/editorconfig-checker), which reports the file path and line number of each violation. If any of these checks fail, it will allow contributors to fix any style violations prior to requesting a PR review even if the editor used doesn't support .editorconfig.
