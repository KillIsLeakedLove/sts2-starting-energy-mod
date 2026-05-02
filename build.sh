#!/bin/bash
# STS2 开局能量+1 Mod 构建脚本 (Linux/WSL)

set -e

# 颜色输出
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== STS2 开局能量+1 Mod 构建脚本 ===${NC}"

# 项目路径
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$PROJECT_DIR/build"
MODS_DIR="$BUILD_DIR/mods/StartingEnergyMod"

# 游戏路径（根据实际安装位置修改）
# 默认尝试常见路径
GAME_DIR=""
for path in \
    "/mnt/e/SteamLibrary/steamapps/common/Slay the Spire 2" \
    "/mnt/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2" \
    "/mnt/d/SteamLibrary/steamapps/common/Slay the Spire 2"; do
    if [ -d "$path" ]; then
        GAME_DIR="$path"
        break
    fi
done

if [ -z "$GAME_DIR" ]; then
    echo -e "${RED}错误：找不到 Slay the Spire 2 安装目录${NC}"
    echo "请手动设置 GAME_DIR 变量"
    exit 1
fi

echo -e "${GREEN}找到游戏目录: $GAME_DIR${NC}"

# 清理并创建构建目录
echo -e "${YELLOW}准备构建目录...${NC}"
rm -rf "$BUILD_DIR"
mkdir -p "$MODS_DIR"

# 构建 DLL
echo -e "${YELLOW}构建 Mod DLL...${NC}"
cd "$PROJECT_DIR"
dotnet build -c Release -p:GameDir="$GAME_DIR"

# 检查构建结果
DLL_PATH="$PROJECT_DIR/bin/Release/net9.0/StartingEnergyMod.dll"
if [ ! -f "$DLL_PATH" ]; then
    echo -e "${RED}错误：DLL 构建失败${NC}"
    exit 1
fi

# 复制文件到输出目录
echo -e "${YELLOW}复制文件到输出目录...${NC}"
cp "$DLL_PATH" "$MODS_DIR/StartingEnergyMod.dll"
cp "$PROJECT_DIR/StartingEnergyMod.json" "$MODS_DIR/StartingEnergyMod.json"

echo -e "${GREEN}构建成功！${NC}"
echo ""
echo -e "${YELLOW}安装方法：${NC}"
echo "1. 复制以下文件到游戏目录的 mods/ 文件夹："
echo "   $MODS_DIR/StartingEnergyMod.dll"
echo "   $MODS_DIR/StartingEnergyMod.json"
echo ""
echo "2. 或者运行以下命令自动安装："
echo "   cp \"$MODS_DIR\"/* \"$GAME_DIR/mods/\" 2>/dev/null || echo '请先创建 mods 目录'"
echo ""
echo -e "${GREEN}完成！${NC}"
