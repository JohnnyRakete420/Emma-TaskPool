<#
    Erzeugt aus einem publish-Ordner ein WiX-Fragment (.wxs), das jede Datei als
    eigene Component + File referenziert (Ersatz fuer das in WiX v5 entfallene
    heat.exe-Directory-Harvesting).

    Aufruf:
      .\Generate-FilesWxs.ps1 -PublishDir ..\publish\Emma.TrayApp `
                               -ComponentGroupId TrayAppFiles `
                               -OutFile TrayApp.Files.wxs
#>
param(
    [Parameter(Mandatory = $true)][string]$PublishDir,
    [Parameter(Mandatory = $true)][string]$ComponentGroupId,
    [Parameter(Mandatory = $true)][string]$OutFile,
    [string[]]$ExcludeFileNames = @()
)

function New-WixId {
    param([string]$Prefix, [string]$RelativePath)

    $sanitized = ($RelativePath -replace '[^A-Za-z0-9_\.]', '_')
    if ($sanitized.Length -gt 40) { $sanitized = $sanitized.Substring($sanitized.Length - 40) }

    $hashBytes = [System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($RelativePath))
    $hash = [System.BitConverter]::ToString($hashBytes).Replace('-', '').Substring(0, 8)

    return "${Prefix}_${sanitized}_${hash}"
}

$PublishDir = (Resolve-Path $PublishDir).Path
$files = Get-ChildItem -Path $PublishDir -Recurse -File | Where-Object { $ExcludeFileNames -notcontains $_.Name }

# Verzeichnisbaum aufbauen: relativer Ordnerpfad -> WiX-Directory-Id
$dirIds = @{ '' = 'INSTALLFOLDER' }

function Get-DirectoryId {
    param([string]$RelativeFolder)

    if ($dirIds.ContainsKey($RelativeFolder)) { return $dirIds[$RelativeFolder] }

    $parent = Split-Path $RelativeFolder -Parent
    if ($null -eq $parent) { $parent = '' }
    $parentId = Get-DirectoryId $parent

    $id = New-WixId -Prefix 'd' -RelativePath $RelativeFolder
    $dirIds[$RelativeFolder] = $id
    return $id
}

$componentXml = New-Object System.Collections.Generic.List[string]
$componentRefXml = New-Object System.Collections.Generic.List[string]

# Verzeichnisstruktur (nur Ordner, die tatsaechlich Dateien enthalten oder Elternordner davon sind)
$allFolders = $files | ForEach-Object { Split-Path $_.FullName.Substring($PublishDir.Length + 1) -Parent } | Sort-Object -Unique
foreach ($folder in $allFolders) {
    if ([string]::IsNullOrEmpty($folder)) { continue }

    $parts = $folder -split '[\\/]'
    $accum = ''
    foreach ($part in $parts) {
        $newAccum = if ($accum -eq '') { $part } else { "$accum\$part" }
        if (-not $dirIds.ContainsKey($newAccum)) {
            $parentId = Get-DirectoryId $accum
            $id = New-WixId -Prefix 'd' -RelativePath $newAccum
            $dirIds[$newAccum] = $id
        }
        $accum = $newAccum
    }
}

# Da WiX Directory-Elemente verschachtelt sein muessen, bauen wir stattdessen ueber
# DirectoryRef + separate <Directory Id Name Ref> - einfacher: wir nutzen flache
# DirectoryRef pro Ordner mit ParentRef ueber verschachteltes XML.
# -> Verschachtelten XML-Baum explizit aufbauen:
function Build-DirectoryTree {
    param([string]$RelativeFolder)

    $children = $allFolders | Where-Object {
        $_ -ne '' -and $_ -ne $RelativeFolder -and (Split-Path $_ -Parent) -eq $RelativeFolder
    }

    $sb = New-Object System.Text.StringBuilder
    foreach ($child in $children) {
        $id = $dirIds[$child]
        $name = Split-Path $child -Leaf
        [void]$sb.Append("<Directory Id=`"$id`" Name=`"$([System.Security.SecurityElement]::Escape($name))`">")
        [void]$sb.Append((Build-DirectoryTree -RelativeFolder $child))
        [void]$sb.Append("</Directory>")
    }
    return $sb.ToString()
}

$directoryTreeXml = Build-DirectoryTree -RelativeFolder ''

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($PublishDir.Length + 1)
    $relativeFolder = Split-Path $relativePath -Parent
    if ($null -eq $relativeFolder) { $relativeFolder = '' }
    $dirId = $dirIds[$relativeFolder]

    $compId = New-WixId -Prefix 'c' -RelativePath $relativePath
    $fileId = New-WixId -Prefix 'f' -RelativePath $relativePath
    $sourcePath = $file.FullName

    $componentXml.Add("    <Component Id=`"$compId`" Directory=`"$dirId`" Guid=`"*`">")
    $componentXml.Add("      <File Id=`"$fileId`" Source=`"$([System.Security.SecurityElement]::Escape($sourcePath))`" KeyPath=`"yes`" />")
    $componentXml.Add("    </Component>")
    $componentRefXml.Add("    <ComponentRef Id=`"$compId`" />")
}

$wxs = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <DirectoryRef Id="INSTALLFOLDER">
$directoryTreeXml
    </DirectoryRef>
  </Fragment>
  <Fragment>
$($componentXml -join "`n")
  </Fragment>
  <Fragment>
    <ComponentGroup Id="$ComponentGroupId">
$($componentRefXml -join "`n")
    </ComponentGroup>
  </Fragment>
</Wix>
"@

Set-Content -Path $OutFile -Value $wxs -Encoding UTF8
Write-Output "Erzeugt: $OutFile ($($files.Count) Dateien)"
