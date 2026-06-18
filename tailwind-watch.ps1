param(
    [string]$ProjectDir = (Get-Location).Path,
    [string]$TailwindExe = ".\tailwindcss.exe",
    [string]$InputCss = "./wwwroot/css/tailwind.css",
    [string]$OutputCss = "./wwwroot/css/site.css",
    [int]$DebounceMs = 300,
    [switch]$Quiet
)

$InputPath = Join-Path $ProjectDir $InputCss
$OutputPath = Join-Path $ProjectDir $OutputCss
$TailwindPath = Join-Path $ProjectDir $TailwindExe

$Watcher = [System.IO.FileSystemWatcher]::new()
$Watcher.Path = $ProjectDir
$Watcher.IncludeSubdirectories = $true
$Watcher.NotifyFilter = [System.IO.NotifyFilters]::LastWrite -bor [System.IO.NotifyFilters]::FileName
$Watcher.Filter = "*.*"

$AllowedExtensions = @('.css', '.cshtml', '.js', '.html', '.razor')
$WatchDirs = @('Views', 'Pages', 'wwwroot')

function Should-Watch($FullPath) {
    $Ext = [System.IO.Path]::GetExtension($FullPath)
    if ($Ext -notin $AllowedExtensions) { return $false }
    foreach ($Dir in $WatchDirs) {
        $Check = Join-Path $ProjectDir $Dir
        if ($FullPath.StartsWith($Check, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

$PendingChange = $false
$LastChange = [DateTime]::MinValue
$SyncRoot = [hashtable]::new()

$Action = {
    $Path = $Event.SourceEventArgs.FullPath
    if (-not (Should-Watch $Path)) { return }
    $SyncRoot.ActualChange = [DateTime]::UtcNow
}

$Handlers = @()
$Handlers += Register-ObjectEvent -InputObject $Watcher -EventName Changed -Action $Action -SourceIdentifier "TW_Changed"
$Handlers += Register-ObjectEvent -InputObject $Watcher -EventName Created -Action $Action -SourceIdentifier "TW_Created"
$Handlers += Register-ObjectEvent -InputObject $Watcher -EventName Renamed -Action $Action -SourceIdentifier "TW_Renamed"
$Watcher.EnableRaisingEvents = $true

function Invoke-Tailwind {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Compilando Tailwind CSS..."
    $proc = Start-Process -FilePath $TailwindPath -ArgumentList "-i `"$InputCss`" -o `"$OutputCss`"" -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -eq 0) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] OK - site.css actualizado" -ForegroundColor Green
    } else {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR (código: $($proc.ExitCode))" -ForegroundColor Red
    }
}

Write-Host "=== TailwindCSS Watch (PowerShell nativo) ===" -ForegroundColor Cyan
Write-Host "Observando: Views/, Pages/, wwwroot/ (*.css, *.cshtml, *.js, *.html, *.razor)" -ForegroundColor Cyan
Write-Host "Input:  $InputCss" -ForegroundColor Cyan
Write-Host "Output: $OutputCss" -ForegroundColor Cyan
Write-Host "Presiona Ctrl+C para detener.`n" -ForegroundColor Yellow

Invoke-Tailwind

try {
    while ($true) {
        $now = [DateTime]::UtcNow
        $actual = $SyncRoot.ActualChange
        if ($actual -and ($now - $actual).TotalMilliseconds -le $DebounceMs) {
            Start-Sleep -Milliseconds 50
            $now = [DateTime]::UtcNow
            if (($now - $actual).TotalMilliseconds -ge $DebounceMs) {
                $SyncRoot.ActualChange = $null
                Invoke-Tailwind
            }
        } else {
            Start-Sleep -Milliseconds 100
        }
    }
}
finally {
    $Watcher.EnableRaisingEvents = $false
    $Watcher.Dispose()
    foreach ($h in $Handlers) {
        Unregister-Event -SourceIdentifier $h.Name -ErrorAction SilentlyContinue
    }
    Remove-Event -ErrorAction SilentlyContinue
    Write-Host "`nWatcher detenido." -ForegroundColor Yellow
}
