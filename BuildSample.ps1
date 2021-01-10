try
{
    Push-Location .\build\Windows\
    $env:COREHOST_TRACE=0

    if($args.length -eq 1) {
        $assemblyName = split-path("../../samples/$($args[0])") -Leaf
        Start-Process .\CoreEngine.exe "Tools\Compiler ../../samples/$($args[0])/$assemblyName.csproj $($args[1])" -Wait -NoNewWindow
    } elseif($args.length -gt 0) {
        Start-Process .\CoreEngine.exe "Tools\Compiler ../../samples/$args/$args.csproj" -Wait -NoNewWindow
    } 
}

finally
{
    $env:COREHOST_TRACE=0
    Pop-Location
}
