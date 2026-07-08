# Git Hook Management with `prek`

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

- [1. Installation Methods](#1-installation-methods)
  - [Option A: Using Homebrew (macOS / Linux)](#option-a-using-homebrew-macos--linux)
  - [Option B: Using `uv`](#option-b-using-uv)
  - [Option C: Using `pipx`](#option-c-using-pipx)
  - [Option D: Direct Binary](#option-d-direct-binary)
  - [Option E: Install with pip](#option-e-install-with-pip)
- [2. Basic CLI Usage](#2-basic-cli-usage)
- [3. The `prek` Priority & Execution System](#3-the-prek-priority--execution-system)
- [4. Tips for `prek`](#4-tips-for-prek)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

`prek` is a high-performance, ultra-fast Git hook manager written in Rust.
It serves as a drop-in, dependency-free replacement for the standard Python `pre-commit` framework.
By utilizing Rust's concurrency, sharing toolchains globally, and integrating with `uv` for environment management,
`prek` drastically reduces installation times and cache footprints while natively interpreting your
`.pre-commit-config.yaml` files.

---

## 1. Installation Methods

You can install `prek` using any of the following methods:

### Option A: Using Homebrew (macOS / Linux)

The most direct way to install `prek` is via Homebrew:

```shell
brew install prek
```

### Option B: Using `uv`

If you use `uv`, you can install it as a standalone tool:

```shell
uv tool install prek
```

### Option C: Using `pipx`

For an isolated Python-based binary environment installation:

```shell
pipx install prek
```

### Option D: Direct Binary

You can download pre-compiled execution assets directly from the prek GitHub Releases page.

### Option E: Install with pip

```shell
pip install prek
```

---

## 2. Basic CLI Usage

Manage your Git lifestyle checks using these essential commands:

* **Install Hooks**: Sets up the necessary Git hooks infrastructure inside your local workspace. Ensure you run this command directly from the root of your repository:
`prek install`
* **Run Hooks on Staged Files**: Automatically checks files currently staged in your index before committing.
`prek run`
* **Run Hooks on All Files**: Evaluates the full repository layout—ideal for initial setups or CI pipelines.
`prek run --all-files`
* **Run a Specific Hook**: Execute only one hook directly by appending its explicit identifier.
`prek run <hook_id> --all-files`
* **Uninstall Hooks**: Safely unregisters and removes the hooks path settings from your local Git workspace.
`prek uninstall`

---

## 3. The `prek` Priority & Execution System

`prek` optimizes how your pipeline resolves rules by establishing a clear hierarchy:

1. **CLI Runtime Flags**: Command arguments provided explicitly (like `--files` or `--directory`) instantly override underlying configuration rules.
2. **Built-in Native Overrides**: `prek` replaces common, resource-heavy Python hooks (like `trailing-whitespace`) with native Rust implementations. These run instantly with zero overhead, taking priority over traditional process invocation.
3. **Workspace-Aware Scheduling**: In complex monorepos, `prek` dynamically discovers multi-project folders. Independent folders at identical file depths are processed concurrently to finish tasks faster without overlapping file scopes.
4. **Strict File Extension Rules**: To keep validations accurate, file type discovery uses exact suffix matches. For instance, a file named `sample.pdf.txt` evaluates strictly as a text asset rather than tripping binary filters.

---

## 4. Tips for `prek`

* **Arrange Modifiers First**: Position hooks that actively alter files (such as code formatters and whitespace trimmers) above validation linters in your `.pre-commit-config.yaml`. This ensures checks validate finished formatting.
* **Leverage `--dry-run**`: Test pattern match filters or complex configuration additions cleanly with `prek run --dry-run` to trace file targeting without executing changes.
* **Cooling-off Periods for Safety**: Run `prek update --cooldown-days 7` to avoid downloading breaking changes instantly, keeping newly published upstream dependencies on hold for a week.
* **CI Build Verification**: In automated jobs, run `prek update --check` to flag mismatching pins or frozen tags instantly without altering workspace configurations during execution.
