![Nager.FileCompress](/docs/banner.png)

# Nager.FileCompress

`Nager.FileCompress` is a lightweight, efficient Windows Service designed to automatically scan directories for images and optimize their file size to save storage space. 

The service processes images safely: it preserves original file metadata (creation/modification dates and EXIF data) and only replaces the original file if a significant storage reduction is achieved.

---

## Features

* **Background Automation:** Runs as a native Windows Service with zero user interaction required.
* **Parallel Processing**: Configurable degree of parallelism to optimize performance on multi-core systems.
* **Smart Thresholding:** Only overwrites the original file if a **minimum of 20% space savings** is achieved.
* **Timestamp Preservation:** Keeps the exact same **Creation Date** and **Modification Date** as the original file.
* **Metadata Retention:** Uses **SixLabors.ImageSharp** for processing, ensuring that all embedded **EXIF data** remains intact.
* **Flexible Format Output:** Supports conversion between image formats (e.g., converting to webp or maintaining jpeg) and custom quality settings.
* **Safe Mode:** Includes an `AnalyzeOnly` flag to simulate optimization and preview potential space savings without modifying files.

---

## Command Line Options (CLI)

`Nager.FileCompressService.exe` supports command-line parameters to streamline service administration directly through the binary.

> **Note:** Administrative privileges (Run as Administrator) are required for installing and uninstalling the service.

| Parameter | Alias | Admin Required | Description |
| --- | --- | --- | --- |
| `/install` | – | **Yes** | Registers `Nager.FileCompress` as an automatic Windows Service and immediately starts it. |
| `/uninstall` | – | **Yes** | Gracefully stops the running `Nager.FileCompress` service and removes it from the system. |
| `/help` | `/?` | No | Displays available command-line arguments and help information. |

---

## Configuration

The service is configured via the `appsettings.json` file. You can define the target directory, file types to scan, and the operational mode.

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Quartz": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  },
  "FileProcessor": {
    "MaxDegreeOfParallelism": 4,
    "AnalyzeOnly": true,
    "SourceDirectory": "C:\\Temp\\TestImageData",
    "ImageOptimizer": {
      "FileExtensions": [
        ".jpg",
        ".jpeg"
      ],
      "Quality": 80,
      "OutputFormat": "jpeg",
      "KeepOriginal": true
    }
  }
}

```

### Configuration Options

| Key | Type | Description |
| --- | --- | --- |
| `FileProcessor.MaxDegreeOfParallelism` | `int` | Defines the maximum number of files processed in parallel. |
| `FileProcessor.AnalyzeOnly` | `bool` | If `true`, the service only calculates potential savings and logs them without modifying or deleting files. Set to `false` for active production optimization. |
| `FileProcessor.SourceDirectory` | `string` | The absolute path to the directory that should be scanned for images. |
| `FileProcessor.ImageOptimizer.FileExtensions` | `string[]` | An array of file extensions to include in the scan (e.g., `[".jpg", ".jpeg"]`). |
| `FileProcessor.ImageOptimizer.Quality` | `int` | The quality level for the optimized image (typically between `70` and `100`). |
| `FileProcessor.ImageOptimizer.OutputFormat` | `string` | The target format for the optimized image. Supported options include `jpeg` and `webp`. |
| `FileProcessor.ImageOptimizer.KeepOriginal` | `bool` | If `true`, the original file will be not removed after a successful optimization. |

---

## How it Works (Core Logic)

1. **Scan:** The service scans the `SourceDirectory` matching the `SearchPattern`.
2. **Process:** Images are loaded and optimized using ImageSharp.
3. **Evaluate:** The service calculates the compression ratio:

$$\text{Percent Saved} = 100 - \left( \frac{\text{Compressed Size}}{\text{Original Size}} \times 100 \right)$$


4. **Apply:** If $\text{Percent Saved} \ge 20\\%$, the original file is replaced (unless `AnalyzeOnly` is `true`).
5. **Sync Metadata:** The **Creation Time**, **Last Write Time** (Modification Date), and **EXIF data** are explicitly copied from the original file onto the optimized file.

---

## Installation

### 1. Install as a Windows Service

Open **Command Prompt (CMD) or PowerShell as Administrator** and use the native Windows Service Control (`sc.exe`) tool to register the service:

```cmd
sc.exe create "Nager.FileCompress" binpath= "C:\\Path\\To\\Your\\Published\\App\\Nager.FileCompressService.exe" start= auto

```

*(Note: The space after `binpath=` and `start=` is strictly required by the `sc` command).*

### 2. Start the Service

```cmd
sc.exe start "Nager.FileCompress"

```

### 3. Uninstalling the Service

If you need to remove the service in the future, stop it first, then delete it:

```cmd
sc.exe stop "Nager.FileCompress"
sc.exe delete "Nager.FileCompress"

```

---

## Prerequisites & Dependencies

* **.NET 10.0 Runtime**
