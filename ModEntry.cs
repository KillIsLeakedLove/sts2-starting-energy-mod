using HarmonyLib;
using System.Reflection;

namespace StartingEnergyMod
{
    /// <summary>
    /// Mod 入口类
    /// 使用 [ModInitializer] 标记初始化方法，STS2 加载 Mod 时会自动调用
    /// </summary>
    public static class ModEntry
    {
        /// <summary>
        /// Mod 初始化方法
        /// 在这里应用 Harmony 补丁
        /// </summary>
        [ModInitializer]
        public static void Initialize()
        {
            // 创建 Harmony 实例，ID 使用 Mod 名称
            var harmony = new Harmony("StartingEnergyMod");
            
            // 应用所有标记了 [HarmonyPatch] 的补丁类
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            // 日志输出到游戏控制台（如果可用）
            System.Console.WriteLine("[StartingEnergyMod] 已加载：开局能量+1 Mod 初始化成功");
        }
    }

    /// <summary>
    /// Harmony 补丁：修改战斗状态中的初始能量
    /// 目标：在战斗开始时将玩家能量从默认的 3 增加到 4
    /// </summary>
    [HarmonyPatch]
    public static class StartingEnergyPatch
    {
        /// <summary>
        /// 动态目标方法选择器
        /// Harmony 使用此方法在运行时查找要补丁的目标方法
        /// 我们需要找到 CombatState 类中设置初始能量的方法
        /// </summary>
        /// <returns>要补丁的方法信息</returns>
        static MethodBase? TargetMethod()
        {
            // 尝试查找 CombatState 类型
            var combatStateType = AccessTools.TypeByName("CombatState");
            if (combatStateType == null)
            {
                System.Console.WriteLine("[StartingEnergyMod] 警告：找不到 CombatState 类型");
                return null;
            }

            // 策略1：尝试查找名为 "StartCombat" 或 "Initialize" 的方法
            // 这是战斗开始时的初始化方法，通常在这里设置初始能量
            var method = AccessTools.Method(combatStateType, "StartCombat");
            if (method != null)
            {
                System.Console.WriteLine($"[StartingEnergyMod] 找到目标方法：{method.Name}");
                return method;
            }

            // 策略2：尝试 "EnterCombat" 方法名
            method = AccessTools.Method(combatStateType, "EnterCombat");
            if (method != null)
            {
                System.Console.WriteLine($"[StartingEnergyMod] 找到目标方法：{method.Name}");
                return method;
            }

            // 策略3：尝试 "BeginCombat" 方法名
            method = AccessTools.Method(combatStateType, "BeginCombat");
            if (method != null)
            {
                System.Console.WriteLine($"[StartingEnergyMod] 找到目标方法：{method.Name}");
                return method;
            }

            // 策略4：查找所有方法，寻找包含 "energy" 或 "turn" 的方法
            var methods = combatStateType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
                                                      BindingFlags.Instance | BindingFlags.Static);
            foreach (var m in methods)
            {
                var name = m.Name.ToLowerInvariant();
                if (name.Contains("energy") || name.Contains("turn") || name.Contains("start"))
                {
                    System.Console.WriteLine($"[StartingEnergyMod] 候选方法：{m.Name}");
                }
            }

            // 策略5：尝试 "SetEnergy" 或 "ResetEnergy" 方法
            method = AccessTools.Method(combatStateType, "SetEnergy");
            if (method != null) return method;

            method = AccessTools.Method(combatStateType, "ResetEnergy");
            if (method != null) return method;

            // 如果找不到 CombatState 的方法，尝试查找 Player 或 AbstractCreature 中的能量相关方法
            var playerType = AccessTools.TypeByName("Player");
            if (playerType != null)
            {
                method = AccessTools.Method(playerType, "SetEnergy");
                if (method != null)
                {
                    System.Console.WriteLine($"[StartingEnergyMod] 找到 Player 目标方法：{method.Name}");
                    return method;
                }
            }

            var creatureType = AccessTools.TypeByName("AbstractCreature");
            if (creatureType != null)
            {
                var energyProp = AccessTools.Property(creatureType, "Energy");
                if (energyProp != null)
                {
                    System.Console.WriteLine($"[StartingEnergyMod] 找到 AbstractCreature.Energy 属性");
                }
            }

            System.Console.WriteLine("[StartingEnergyMod] 警告：未能自动找到目标方法，将尝试备用方案");
            return null;
        }

        /// <summary>
        /// 后置补丁：在目标方法执行后修改能量
        /// </summary>
        /// <param name="__instance">目标方法的实例（CombatState 实例）</param>
        static void Postfix(object __instance)
        {
            if (__instance == null) return;

            try
            {
                // 获取 CombatState 类型
                var combatStateType = __instance.GetType();
                
                // 尝试获取 Player 字段/属性
                var playerField = AccessTools.Field(combatStateType, "player");
                var playerProp = AccessTools.Property(combatStateType, "Player");
                
                object? player = null;
                if (playerField != null)
                    player = playerField.GetValue(__instance);
                else if (playerProp != null)
                    player = playerProp.GetValue(__instance);

                if (player == null)
                {
                    // 尝试其他字段名
                    playerField = AccessTools.Field(combatStateType, "_player");
                    if (playerField != null)
                        player = playerField.GetValue(__instance);
                }

                if (player != null)
                {
                    // 修改玩家能量
                    ModifyPlayerEnergy(player);
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[StartingEnergyMod] 错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 修改玩家能量值
        /// </summary>
        /// <param name="player">玩家对象实例</param>
        private static void ModifyPlayerEnergy(object player)
        {
            var playerType = player.GetType();
            
            // 尝试获取 Energy 属性
            var energyProp = AccessTools.Property(playerType, "Energy");
            if (energyProp == null)
                energyProp = AccessTools.Property(playerType, "energy");
            
            // 尝试获取 CurrentEnergy 属性
            if (energyProp == null)
                energyProp = AccessTools.Property(playerType, "CurrentEnergy");
            if (energyProp == null)
                energyProp = AccessTools.Property(playerType, "currentEnergy");

            if (energyProp != null && energyProp.CanRead && energyProp.CanWrite)
            {
                var currentEnergy = (int)energyProp.GetValue(player)!;
                // 如果当前能量是默认值 3，增加到 4
                if (currentEnergy == 3)
                {
                    energyProp.SetValue(player, 4);
                    System.Console.WriteLine($"[StartingEnergyMod] 能量已修改：3 -> 4");
                }
            }
            else
            {
                // 尝试直接修改字段
                var energyField = AccessTools.Field(playerType, "energy");
                if (energyField == null)
                    energyField = AccessTools.Field(playerType, "_energy");
                if (energyField == null)
                    energyField = AccessTools.Field(playerType, "Energy");

                if (energyField != null)
                {
                    var currentEnergy = (int)energyField.GetValue(player)!;
                    if (currentEnergy == 3)
                    {
                        energyField.SetValue(player, 4);
                        System.Console.WriteLine($"[StartingEnergyMod] 能量字段已修改：3 -> 4");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 备用补丁方案：直接修改回合开始时的能量
    /// 如果上面的 CombatState 补丁不生效，尝试此方案
    /// </summary>
    [HarmonyPatch]
    public static class TurnStartEnergyPatch
    {
        static MethodBase? TargetMethod()
        {
            // 尝试查找回合开始相关的方法
            var combatStateType = AccessTools.TypeByName("CombatState");
            if (combatStateType == null) return null;

            // 尝试 "StartTurn" 方法
            var method = AccessTools.Method(combatStateType, "StartTurn");
            if (method != null)
            {
                System.Console.WriteLine($"[StartingEnergyMod] 找到回合开始方法：{method.Name}");
                return method;
            }

            method = AccessTools.Method(combatStateType, "BeginTurn");
            if (method != null) return method;

            method = AccessTools.Method(combatStateType, "OnTurnStart");
            if (method != null) return method;

            return null;
        }

        static void Postfix(object __instance)
        {
            // 只在第一回合增加能量
            try
            {
                var combatStateType = __instance.GetType();
                
                // 检查是否是第一回合
                var turnField = AccessTools.Field(combatStateType, "turn");
                if (turnField == null)
                    turnField = AccessTools.Field(combatStateType, "_turn");
                if (turnField == null)
                    turnField = AccessTools.Field(combatStateType, "currentTurn");

                var turn = turnField != null ? (int)turnField.GetValue(__instance)! : 0;
                
                // 如果是第一回合（turn == 1 或 0，取决于游戏实现）
                if (turn <= 1)
                {
                    // 获取玩家并修改能量
                    var playerField = AccessTools.Field(combatStateType, "player");
                    if (playerField == null)
                        playerField = AccessTools.Field(combatStateType, "_player");
                    
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
                                System.Console.WriteLine($"[StartingEnergyMod] 第一回合能量已修改：3 -> 4");
                            }
                        }
                    }
                }
            }
            catch { /* 静默处理错误 */ }
        }
    }
}
