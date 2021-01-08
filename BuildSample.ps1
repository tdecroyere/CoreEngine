try
{
    Push-Location .\build\Windows\
    $env:COREHOST_TRACE=0

    if($args.length -gt 1) {
        Start-Process .\CoreEngine.exe "Compiler\Compiler ../../samples/$($args[0])/$($args[0]).csproj $($args[1])" -Wait -NoNewWindow
    } elseif($args.length -gt 0) {
        Start-Process .\CoreEngine.exe "Compiler\Compiler ../../samples/$args/$args.csproj" -Wait -NoNewWindow
    } 
}

finally
{
    $env:COREHOST_TRACE=0
    Pop-Location
}
