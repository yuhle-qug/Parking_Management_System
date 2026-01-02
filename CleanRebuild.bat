@echo off
REM Complete cleanup and rebuild script
echo Cleaning up all bin and obj folders...
for /d /r . %%d in (bin,obj) do @if exist "%%d" (rd /s /q "%%d" && echo Deleted %%d)

echo.
echo Cleaning up .vs folders...
for /d /r . %%d in (.vs) do @if exist "%%d" (rd /s /q "%%d" && echo Deleted %%d)

echo.
echo Running dotnet clean...
dotnet clean

echo.
echo Running dotnet restore...
dotnet restore

echo.
echo Running dotnet build...
dotnet build

echo.
echo Build complete!
pause
