@echo off

set SLC=bin\Release\SLC.exe
set Output=.\Protocol.cs
set NameSpace="Protocol"
set SDDL=phone.sddl
set Yield=bin\Release\CSharpYield
set Async=bin\Release\CSharpAsync

if not exist %SLC% (
    echo ERROR:: Build All Project In Release Mode First
    goto quit
)
if not exist %Yield%.dll (
    echo ERROR:: Build CSharpYield Project In Release Mode First
    goto quit
)
if not exist %Async%.dll (
    echo ERROR:: Build CSharpAsync Project In Release Mode First
    goto quit
)
if not exist %SDDL% (
    echo ERROR:: No SDDL File Named: %SDDL%
    goto quit
)

echo Build Target:
echo     1) Build Yield (Used to Client)
echo     2) Build Async (Used to Server)
choice /c:12 /m:"please select"

if %errorlevel%==1 %SLC% %SDDL% -o %Output% -t %Yield% -n %NameSpace%
if %errorlevel%==2 %SLC% %SDDL% -o %Output% -t %Async% -n %NameSpace%

echo.
echo Build Success: %Output%
echo.
goto quit

:quit
echo.
pause
