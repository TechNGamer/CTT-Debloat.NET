$exeLoc = $HOME + "\AppData\Local\Temp\cttdebloatnet.exe"
$repo = (New-Object System.Net.WebClient).DownloadString('https://api.github.com/repos/TechNGamer/CTT-Debloat.NET/releases/latest') | ConvertFrom-Json -NoEnumerate

(New-Object System.Net.WebClient).DownloadFile($repo.assets.browser_download_url, $exeLoc)

Invoke-Expression $exeLoc