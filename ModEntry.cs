using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using System.Reflection;

namespace StartingEnergyMod;

/// <summary>
/// Mod 主入口类
/// 继承 Node 以兼容 Godot 生命周期
/// 使用 [ModInitializer] 标记初始化方法
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "StartingEnergyMod";

    /// <summary>
    /// Mod 初始化方法，由 STS2 加载器自动调用
    /// </summary>
    public static void Initialize()
    {
        var harmony = new Harmony(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        GD.Print($"[{ModId}] 开局能量+1 Mod 已加载");
    }
}

/// <summary>
/// Harmony 补丁：修改玩家初始能量
/// 目标：战斗开始时将能量从默认的 3 增加到 4
/// </summary>
[HarmonyPatch]
public static class StartingEnergyPatch
{
    /// <summary>
    /// 动态查找 CombatState 中初始化战斗的方法
    /// </summary>
    static MethodBase? TargetMethod()
    {
        var combatStateType = AccessTools.TypeByName("CombatState");
        if (combatStateType == null)
        {
            GD.PrintErr($"[{MainFile.ModId}] 找不到 CombatState 类型");
            return null;
        }

        // 尝试常见的战斗初始化方法名
        string[] methodNames = { "StartCombat", "EnterCombat", "BeginCombat", "InitializeCombat" };
        foreach (var name in methodNames)
        {
            var method = AccessTools.Method(combatStateType, name);
            if (method != null)
            {
                GD.Print($"[{MainFile.ModId}] 找到目标方法: {method.Name}");
                return method;
            }
        }

        GD.PrintErr($"[{MainFile.ModId}] 警告：未能找到 CombatState 的战斗初始化方法");
        return null;
    }

    /// <summary>
    /// 后置补丁：战斗初始化完成后修改玩家能量
    /// </summary>
    static void Postfix(object __instance)
    {
        if (__instance == null) return;

        try
        {
            var combatStateType = __instance.GetType();

            // 获取 player 字段
            object? player = null;
            var playerField = AccessTools.Field(combatStateType, "player")
                ?? AccessTools.Field(combatStateType, "_player")
                ?? AccessTools.Field(combatStateType, "Player");

            if (playerField != null)
                player = playerField.GetValue(__instance);

            if (player == null)
            {
                // 尝试通过属性获取
                var playerProp = AccessTools.Property(combatStateType, "Player")
                    ?? AccessTools.Property(combatStateType, "player");
                if (playerProp != null)
                    player = playerProp.GetValue(__instance);
            }

            if (player != null)
            {
                ModifyPlayerEnergy(player);
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[{MainFile.ModId}] 能量修改失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 修改玩家能量：如果当前是默认值 3，则增加到 4
    /// </summary>
    private static void ModifyPlayerEnergy(object player)
    {
        var playerType = player.GetType();

        // 尝试 Energy 属性
        var energyProp = AccessTools.Property(playerType, "Energy")
            ?? AccessTools.Property(playerType, "energy")
            ?? AccessTools.Property(playerType, "CurrentEnergy")
            ?? AccessTools.Property(playerType, "currentEnergy");

        if (energyProp != null && energyProp.CanRead && energyProp.CanWrite)
        {
            var currentEnergy = (int)energyProp.GetValue(player)!;
            if (currentEnergy == 3)
            {
                energyProp.SetValue(player, 4);
                GD.Print($"[{MainFile.ModId}] 能量已修改: 3 -> 4");
            }
        }
        else
        {
            // 尝试直接修改字段
            var energyField = AccessTools.Field(playerType, "energy")
                ?? AccessTools.Field(playerType, "_energy")
                ?? AccessTools.Field(playerType, "Energy");

            if (energyField != null)
            {
                var currentEnergy = (int)energyField.GetValue(player)!;
                if (currentEnergy == 3)
                {
                    energyField.SetValue(player, 4);
                    GD.Print($"[{MainFile.ModId}] 能量字段已修改: 3 -> 4");
                }
            }
        }
    }
}

/// <summary>
/// 备用补丁：回合开始时增加能量（如果主补丁未生效）
/// </summary>
[HarmonyPatch]
public static class TurnStartEnergyPatch
{
    static MethodBase? TargetMethod()
    {
        var combatStateType = AccessTools.TypeByName("CombatState");
        if (combatStateType == null) return null;

        string[] methodNames = { "StartTurn", "BeginTurn", "OnTurnStart" };
        foreach (var name in methodNames)
        {
            var method = AccessTools.Method(combatStateType, name);
            if (method != null) return method;
        }

        return null;
    }

    static void Postfix(object __instance)
    {
        try
        {
            var combatStateType = __instance.GetType();

            // 检查是否是第一回合
            var turnField = AccessTools.Field(combatStateType, "turn")
                ?? AccessTools.Field(combatStateType, "_turn")
                ?? AccessTools.Field(combatStateType, "currentTurn");

            var turn = turnField != null ? (int)turnField.GetValue(__instance)! : 0;

            // 只在第一回合增加能量
            if (turn <= 1)
            {
                var playerField = AccessTools.Field(combatStateType, "player")
                    ?? AccessTools.Field(combatStateType, "_player");

                var player = playerField?.GetValue(__instance);
                if (player != null)
                {
                    var playerType = player.GetType();
                    var energyProp = AccessTools.Property(playerType, "Energy")
                        ?? AccessTools.Property(playerType, "energy")
                        ?? AccessTools.Property(playerType, "CurrentEnergy");

                    if (energyProp != null && energyProp.CanWrite)
                    {
                        var current = (int)energyProp.GetValue(player)!;
                        if (current == 3)
                        {
                            energyProp.SetValue(player, 4);
                            GD.Print($"[{MainFile.ModId}] 第一回合能量已修改: 3 -> 4");
                        }
                    }
                }
            }
        }
        catch { /* 静默处理 */ }
    }
}
