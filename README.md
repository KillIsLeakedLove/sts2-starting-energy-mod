# Slay the Spire 2 开局能量+1 Mod

## 功能
加载此 Mod 后，所有角色的开局初始能量从 3 点增加到 4 点。

## 安装方法

### 方法1：手动安装
1. 下载本项目的 `build/mods/StartingEnergyMod/` 文件夹内容
2. 复制到游戏目录的 `mods/` 文件夹下：
   ```
   <游戏根目录>\mods\
     StartingEnergyMod.json
     StartingEnergyMod.dll
   ```
3. 启动游戏，在 Mod 菜单中启用

### 方法2：构建安装
1. 确保已安装 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. 克隆本仓库
3. 运行构建脚本：
   - Windows: `powershell -ExecutionPolicy Bypass -File build.ps1`
   - Linux/WSL: `bash build.sh`
4. 构建输出在 `build/mods/StartingEnergyMod/`，复制到游戏 `mods/` 目录

## 游戏目录位置
默认 Steam 安装路径：
- `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\`
- 或 `E:\SteamLibrary\steamapps\common\Slay the Spire 2\`

## 技术说明
- 使用 Harmony 补丁修改 `CombatState` 类中设置初始能量的逻辑
- 在战斗开始时检测当前能量，如果是默认值 3 则增加到 4
- 兼容所有角色（Ironclad、Silent、Defect、Watcher 等）

## 卸载
删除游戏目录 `mods\StartingEnergyMod.json` 和 `mods\StartingEnergyMod.dll`

## 开源协议
MIT License
