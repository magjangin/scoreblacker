using System;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace InventoryPopup
{
    public static class JudgeAnalyzer
    {
        public static void AnalyzeEJudgesUsage()
        {
            try
            {
                MelonLogger.Msg("\n=== EJudges 사용 분석 ===");
                
                var assemblyCSharp = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                
                if (assemblyCSharp == null)
                {
                    MelonLogger.Error("Assembly-CSharp.dll을 찾을 수 없습니다.");
                    return;
                }
                
                // EJudges enum 찾기
                var eJudgesType = assemblyCSharp.GetTypes()
                    .FirstOrDefault(t => t != null && t.IsEnum && t.Name == "EJudges");
                
                if (eJudgesType == null)
                {
                    MelonLogger.Error("EJudges enum을 찾을 수 없습니다.");
                    return;
                }
                
                MelonLogger.Msg($"✓ EJudges enum 발견: {eJudgesType.FullName}");
                
                // EJudges 값 출력
                var enumValues = Enum.GetValues(eJudgesType);
                MelonLogger.Msg("\nEJudges 값:");
                foreach (var value in enumValues)
                {
                    MelonLogger.Msg($"  - {value} = {Convert.ToInt32(value)}");
                }
                
                // EJudges를 파라미터로 받는 메서드 찾기
                MelonLogger.Msg("\n=== EJudges를 파라미터로 받는 메서드 ===");
                
                var allTypes = assemblyCSharp.GetTypes()
                    .Where(t => t != null && !t.IsAbstract && !t.IsInterface)
                    .ToArray();
                
                int methodCount = 0;
                foreach (var type in allTypes)
                {
                    try
                    {
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
                                                      BindingFlags.Instance | BindingFlags.Static | 
                                                      BindingFlags.DeclaredOnly)
                            .Where(m => !m.IsSpecialName && !m.IsConstructor)
                            .ToArray();
                        
                        foreach (var method in methods)
                        {
                            var parameters = method.GetParameters();
                            bool hasEJudgesParam = parameters.Any(p => p.ParameterType == eJudgesType);
                            
                            if (hasEJudgesParam)
                            {
                                methodCount++;
                                var accessModifier = method.IsPublic ? "public" : 
                                                    method.IsPrivate ? "private" : 
                                                    method.IsFamily ? "protected" : "internal";
                                var isStatic = method.IsStatic ? "static " : "";
                                var paramList = string.Join(", ", parameters.Select(p => 
                                    p.ParameterType == eJudgesType ? $"EJudges {p.Name}" : 
                                    $"{p.ParameterType.Name} {p.Name}"));
                                
                                MelonLogger.Msg($"\n  [{methodCount}] {type.Name}.{method.Name}");
                                MelonLogger.Msg($"      접근성: {accessModifier} {isStatic}");
                                MelonLogger.Msg($"      반환 타입: {method.ReturnType.Name}");
                                MelonLogger.Msg($"      파라미터: ({paramList})");
                                
                                // EJudges 파라미터의 이름 찾기
                                var eJudgesParams = parameters.Where(p => p.ParameterType == eJudgesType).ToArray();
                                foreach (var param in eJudgesParams)
                                {
                                    MelonLogger.Msg($"      ⭐ EJudges 파라미터: {param.Name} (위치: {param.Position})");
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                MelonLogger.Msg($"\n총 {methodCount}개의 메서드가 EJudges를 파라미터로 받습니다.");
                
                // EJudges를 반환하는 메서드 찾기
                MelonLogger.Msg("\n=== EJudges를 반환하는 메서드 ===");
                
                int returnMethodCount = 0;
                foreach (var type in allTypes)
                {
                    try
                    {
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
                                                      BindingFlags.Instance | BindingFlags.Static | 
                                                      BindingFlags.DeclaredOnly)
                            .Where(m => !m.IsSpecialName && !m.IsConstructor && m.ReturnType == eJudgesType)
                            .ToArray();
                        
                        foreach (var method in methods)
                        {
                            returnMethodCount++;
                            var accessModifier = method.IsPublic ? "public" : 
                                                method.IsPrivate ? "private" : 
                                                method.IsFamily ? "protected" : "internal";
                            var isStatic = method.IsStatic ? "static " : "";
                            var paramList = string.Join(", ", method.GetParameters().Select(p => 
                                $"{p.ParameterType.Name} {p.Name}"));
                            
                            MelonLogger.Msg($"\n  [{returnMethodCount}] {type.Name}.{method.Name}");
                            MelonLogger.Msg($"      접근성: {accessModifier} {isStatic}");
                            MelonLogger.Msg($"      파라미터: ({paramList})");
                            MelonLogger.Msg($"      ⭐ 반환 타입: EJudges");
                        }
                    }
                    catch { }
                }
                
                MelonLogger.Msg($"\n총 {returnMethodCount}개의 메서드가 EJudges를 반환합니다.");
                
                MelonLogger.Msg("\n=== EJudges 사용 분석 완료 ===\n");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"EJudges 분석 오류: {ex}");
            }
        }
    }
}







