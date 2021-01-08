try
{
    Push-Location .\build\Windows\
    $env:COREHOST_TRACE=0

    if($args) {
        Start-Process .\CoreEngine.exe $args -Wait -NoNewWindow
    } else {
        Start-Process .\CoreEngine.exe -Wait -NoNewWindow
    }
}

finally
{
    $env:COREHOST_TRACE=0
    Pop-Location
}
