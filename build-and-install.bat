@echo off
chcp 65001 >nul
:: STS2 开局能量+1 Mod - Windows 一键构建安装脚本

set "MOD_NAME=StartingEnergyMod"
set "PROJECT_DIR=%~dp0"
set "GAME_DIR=E:\SteamLibrary\steamapps\common\Slay the Spire 2"
set "MODS_DIR=%GAME_DIR%\mods"

echo === STS2 %MOD_NAME% 构建脚本 ===
echo.

:: 检查 .NET SDK
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 找不到 dotnet 命令，请先安装 .NET 9 SDK
    echo 下载地址: https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

echo [1/5] 检查 .NET SDK... OK

:: 尝试自动检测游戏目录
if not exist "%GAME_DIR%\SlayTheSpire2.exe" (
    echo [提示] 默认游戏路径不存在，尝试其他路径...
    if exist "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.exe" (
        set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    ) else if exist "D:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.exe" (
        set "GAME_DIR=D:\SteamLibrary\steamapps\common\Slay the Spire 2"
    ) else (
        echo [错误] 找不到 Slay the Spire 2 安装目录
        echo 请手动修改本脚本中的 GAME_DIR 变量
        pause
        exit /b 1
    )
)

echo [2/5] 游戏目录: %GAME_DIR%

:: 创建 mods 目录
if not exist "%MODS_DIR%" (
    echo [3/5] 创建 mods 目录...
    mkdir "%MODS_DIR%"
) else (
    echo [3/5] mods 目录已存在
)

:: 构建项目
echo [4/5] 构建 Mod DLL...
cd /d "%PROJECT_DIR%"
dotnet build -c Release -p:GameDir="%GAME_DIR%"

if errorlevel 1 (
    echo [错误] 构建失败！
    pause
    exit /b 1
)

:: 复制文件到游戏目录
echo [5/5] 安装 Mod 到游戏...
copy /Y "%PROJECT_DIR%\bin\Release\net9.0\%MOD_NAME%.dll" "%MODS_DIR%\%MOD_NAME%.dll" >nul
copy /Y "%PROJECT_DIR%\%MOD_NAME%.json" "%MODS_DIR%\%MOD_NAME%.json" >nul

echo.
echo === 构建安装完成！===
echo Mod 文件已复制到:
echo   %MODS_DIR%\%MOD_NAME%.dll
echo   %MODS_DIR%\%MOD_NAME%.json
echo.
echo 现在启动游戏，在 Mod 菜单中启用 "%MOD_NAME%"
echo.
pause
