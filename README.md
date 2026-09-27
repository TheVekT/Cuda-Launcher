# Cuda Launcher

A customizable Minecraft launcher featuring smart drag-and-drop file management, Microsoft and offline accounts, switchable visual themes, and flexible instance isolation.

![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D7)
![Windows](https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white)
![Portable](https://img.shields.io/badge/Portable-2ea44f)
![Customizable](https://img.shields.io/badge/Customizable-orange)

---

## Preview

| Play View (Default Dark) | Instance Creation & Isolation |
| :---: | :---: |
| <img src="https://github.com/user-attachments/assets/943c8da1-5efa-4248-ba12-3298ca3ce7a5" alt="Play View" width="100%" /> | <img src="https://github.com/user-attachments/assets/f9f938c5-2919-4d21-bbfb-3a4fea1266b5" alt="Instance Creation" width="100%" /> |
| **Skin Wardrobe (Steampunk)** | **Smart Drag & Drop (Steampunk)** |
| <img src="https://github.com/user-attachments/assets/6643c20f-477b-4fee-bf88-c250e8387557" alt="Skin Wardrobe" width="100%" /> | <img src="https://github.com/user-attachments/assets/b9baf31a-f9c7-431d-80fd-a218b5358bec" alt="Drag and Drop" width="100%" /> |

---

## Features

### Instance Isolation Modes

When creating an instance, you can choose how its files are organized:

- **Global (Shared)**: Uses the default `%AppData%\.minecraft` directory.
- **Full Isolation**: Creates a completely separate directory for the instance.
- **Partial Isolation**: Provides a dedicated, isolated mods folder while sharing the common `.minecraft` directory for worlds, configs, and options.

For technical details on how directories and symlinks are mapped, refer to the [Instance Isolation Guide](https://github.com/TheVekT/Cuda-Launcher/blob/main/docs/Instance-Isolation.md).

### Smart Drag and Drop

Drop files directly onto the launcher window to import them into your currently selected instance. Mods (`.jar`) are routed into the instance's mods folder, resource packs and shader packs go into their designated folders, world saves are placed into `saves`, and theme archives or translation files are applied directly to the launcher.

### Accounts and Skin Wardrobe

The launcher supports both official Microsoft accounts (via an interactive login window) and offline/local accounts.

An integrated skin wardrobe allows managing local skin presets for both Classic (Steve) and Slim (Alex) player models, complete with an interactive 3D preview.

### Visual Themes

The interface supports runtime theme switching. It includes Default Dark, Default Light, Steampunk, and Cyberpunk themes, with support for custom community theme archives.

### Auto-Updates

A built-in updater checks for new releases on startup and handles downloads and installation automatically.

---

## System Requirements

- **Operating System**: Windows 10 or Windows 11.
- **File System**: NTFS formatted drive (required for symbolic links in Partial Isolation mode).

All builds are self-contained and do not require pre-installing any external runtimes.

---

## Installation

- **Portable**: Download and unpack `Cuda-Launcher-portable.zip` into any folder, then run `Cuda Launcher.exe`.
- **Installer**: Download `Cuda-Launcher-setup.exe` and follow the setup wizard.

---

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
