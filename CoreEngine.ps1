Push-Location .\build\Windows\
$env:COREHOST_TRACE=0
Start-Process .\CoreEngine.exe $args[0] -Wait -NoNewWindow
$env:COREHOST_TRACE=0
Pop-Location