@echo off
setlocal
echo ========================================
echo ScoreBlacker 빌드 스크립트
echo ========================================
echo.

set "SCRIPT_DIR=%~dp0"
set "SLN_PATH=%SCRIPT_DIR%scoreblacker.sln"
set "OUTPUT_DLL=%SCRIPT_DIR%scoreblacker\bin\Debug\scoreblacker.dll"
set "GAME_ROOT=H:\Sixtar Gate STARTRAIL custom mode"
set "MODS_DIR=%GAME_ROOT%\Mods"
set "MSBUILD_PATH="

for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do (
    set "MSBUILD_PATH=%%i"
)

if not defined MSBUILD_PATH if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)
if not defined MSBUILD_PATH if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
)
if not defined MSBUILD_PATH if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)
if not defined MSBUILD_PATH if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)

if exist "%MSBUILD_PATH%" (
    echo MSBuild 경로: %MSBUILD_PATH%
) else (
    where dotnet >nul 2>nul
    if %ERRORLEVEL% NEQ 0 (
        echo MSBuild를 찾을 수 없습니다.
        echo Visual Studio 2022/Build Tools 또는 .NET SDK를 설치하세요.
        pause
        exit /b 1
    )
    echo MSBuild.exe를 찾지 못해 dotnet msbuild를 사용합니다.
)

echo.
echo 빌드 시작...
echo.

if exist "%MSBUILD_PATH%" (
    "%MSBUILD_PATH%" "%SLN_PATH%" /p:Configuration=Debug /p:Platform="Any CPU" /t:Build /v:minimal /nologo
) else (
    dotnet msbuild "%SLN_PATH%" /p:Configuration=Debug /p:Platform="Any CPU" /t:Build /v:minimal /nologo
)

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo 빌드 성공!
    echo ========================================
    echo 출력 파일: %OUTPUT_DLL%
    echo.
    echo ========================================
    echo mod 복사 단계
    echo ========================================
    if exist "%OUTPUT_DLL%" (
        if not exist "%MODS_DIR%" mkdir "%MODS_DIR%"
        copy /Y "%OUTPUT_DLL%" "%MODS_DIR%\scoreblacker.dll" >nul
        if %ERRORLEVEL% EQU 0 (
            echo 복사 완료: %MODS_DIR%\scoreblacker.dll
        ) else (
            echo 복사 실패: 파일 권한 또는 경로를 확인하세요.
        )
    ) else (
        echo 복사 실패: 출력 DLL을 찾을 수 없습니다.
    )
) else (
    echo.
    echo ========================================
    echo 빌드 실패!
    echo ========================================
)

echo.
pause
endlocal






