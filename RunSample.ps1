try
{
    Push-Location .\build\Windows\
    $env:COREHOST_TRACE=0

    if($args.length -gt 0) {
        Start-Process .\CoreEngine.exe ..\..\samples\$args -Wait -NoNewWindow
    } else {
        Start-Process .\CoreEngine.exe -Wait -NoNewWindow
    }
}

finally
{
    $env:COREHOST_TRACE=0
    Pop-Location
}
