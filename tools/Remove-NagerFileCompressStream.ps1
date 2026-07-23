<#
.SYNOPSIS
    Removes specific Alternate Data Streams (ADS) from files in a designated directory.

.DESCRIPTION
    This script searches recursively through a specified target directory for files containing 
    the "nagerfilecompress" NTFS Alternate Data Stream (ADS) and removes the stream while 
    preserving the original host files.

.PARAMETER TargetPath
    Specifies the root directory path to scan and clean. Defaults to 'C:\MyFolder'.

.EXAMPLE
    .\Remove-NagerFileCompressStream.ps1 -TargetPath "D:\Data"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false, Position = 0, ValueFromPipeline = $true)]
    [string]$TargetPath = "C:\MyFolder"
)

$streamName = "nagerfilecompress"

if (-not (Test-Path -Path $TargetPath)) {
    Write-Error "Target path '$TargetPath' does not exist."
    exit 1
}

Write-Host "Scanning directory: $TargetPath for stream '$streamName'..." -ForegroundColor Cyan

$processedCount = 0

# Remove the "nagerfilecompress" stream from all files in the folder
Get-ChildItem -Path $TargetPath -Recurse -File | ForEach-Object {
    $filePath = $_.FullName
    if (Get-Item -Path $filePath -Stream $streamName -ErrorAction SilentlyContinue) {
        try {
            Remove-Item -Path $filePath -Stream $streamName -ErrorAction Stop
            Write-Host "Stream removed from: $filePath" -ForegroundColor Green
            $processedCount++
        }
        catch {
            Write-Warning "Failed to remove stream from: $filePath. Error: $_"
        }
    }
}

Write-Host "Cleanup completed. Removed '$streamName' stream from $processedCount file(s)." -ForegroundColor Yellow