using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace InventoryPopup
{
    public static class ScorePatchManager
    {
        public static void ApplyJudgePatches(HarmonyLib.Harmony harmony)
        {
            try
            {
                var assemblyCSharp = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                
                if (assemblyCSharp == null)
                {
                    MelonLogger.Error("Assembly-CSharp.dll을 찾을 수 없습니다.");
                    return;
                }
                
                // EJudges enum 타입 찾기
                var eJudgesType = assemblyCSharp.GetTypes()
                    .FirstOrDefault(t => t != null && t.IsEnum && t.Name == "EJudges");
                
                if (eJudgesType == null)
                {
                    MelonLogger.Error("EJudges enum을 찾을 수 없습니다.");
                    return;
                }
                
                var judgementType = assemblyCSharp.GetType("RhythmGame.Play.RG_PS_Judgement");
                if (judgementType == null)
                {
                    MelonLogger.Error("RG_PS_Judgement 타입을 찾을 수 없습니다.");
                    return;
                }
                
                // JudgeAction 후킹 (EJudges 파라미터 조작)
                var judgeActionMethods = judgementType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.Name == "JudgeAction")
                    .ToArray();
                var judgeActionMethod = judgeActionMethods.FirstOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    return parameters.Length >= 1 && parameters[0].ParameterType == eJudgesType;
                });
                if (judgeActionMethod != null)
                {
                    harmony.Patch(judgeActionMethod, 
                        prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.JudgeActionPrefix)),
                        postfix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.JudgeActionPostfix)));
                    MelonLogger.Msg("✓ JudgeAction(EJudges) 후킹 완료");
                }
                
                // JudgeDivergence 후킹 (EJudges 파라미터 조작)
                var judgeDivergenceMethods = judgementType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.Name == "JudgeDivergence")
                    .ToArray();
                var judgeDivergenceMethod = judgeDivergenceMethods.FirstOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    return parameters.Length >= 1 && parameters[0].ParameterType == eJudgesType;
                });
                if (judgeDivergenceMethod != null)
                {
                    harmony.Patch(judgeDivergenceMethod, 
                        prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.JudgeDivergencePrefix)),
                        postfix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.JudgeDivergencePostfix)));
                    MelonLogger.Msg("✓ JudgeDivergence(EJudges) 후킹 완료");
                }
                
                // TryJudgeShortNote 후킹
                var tryJudgeShortNoteMethods = judgementType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.Name == "TryJudgeShortNote")
                    .ToArray();
                var tryJudgeShortNoteMethod = tryJudgeShortNoteMethods.FirstOrDefault();
                if (tryJudgeShortNoteMethod != null)
                {
                    harmony.Patch(tryJudgeShortNoteMethod, 
                        prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.TryJudgeShortNotePrefix)),
                        postfix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.TryJudgeShortNotePostfix)));
                    MelonLogger.Msg("✓ TryJudgeShortNote 후킹 완료");
                }
                
                // JudgeCounter.AddJudge 후킹
                var judgeCounterType = assemblyCSharp.GetType("JudgeCounter");
                if (judgeCounterType != null)
                {
                    var addJudgeMethods = judgeCounterType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.Name == "AddJudge")
                        .ToArray();
                    var addJudgeMethod = addJudgeMethods.FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length >= 1 && parameters[0].ParameterType == eJudgesType;
                    });
                    if (addJudgeMethod != null)
                    {
                        harmony.Patch(addJudgeMethod, 
                            prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.AddJudgePrefix)));
                        MelonLogger.Msg("✓ JudgeCounter.AddJudge(EJudges) 후킹 완료");
                    }
                }
                
                // JudgeTextViewer.OnGetJudge 후킹
                var judgeTextViewerType = assemblyCSharp.GetType("JudgeTextViewer");
                if (judgeTextViewerType != null)
                {
                    var onGetJudgeMethods = judgeTextViewerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.Name == "OnGetJudge")
                        .ToArray();
                    var onGetJudgeMethod = onGetJudgeMethods.FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length >= 1 && parameters[0].ParameterType == eJudgesType;
                    });
                    if (onGetJudgeMethod != null)
                    {
                        harmony.Patch(onGetJudgeMethod, 
                            prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.OnGetJudgePrefix)));
                        MelonLogger.Msg("✓ JudgeTextViewer.OnGetJudge(EJudges) 후킹 완료");
                    }
                }
                
                // RedStarCounter.OnGetJudge 후킹
                var redStarCounterType = assemblyCSharp.GetType("RedStarCounter");
                if (redStarCounterType != null)
                {
                    var onGetJudgeMethods = redStarCounterType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.Name == "OnGetJudge")
                        .ToArray();
                    var onGetJudgeMethod = onGetJudgeMethods.FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length >= 1 && parameters[0].ParameterType == eJudgesType;
                    });
                    if (onGetJudgeMethod != null)
                    {
                        harmony.Patch(onGetJudgeMethod, 
                            prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.RedStarCounterOnGetJudgePrefix)));
                        MelonLogger.Msg("✓ RedStarCounter.OnGetJudge(EJudges) 후킹 완료");
                    }
                }
                
                // FastSlowMeter.OnGetJudge 후킹
                var fastSlowMeterType = assemblyCSharp.GetType("FastSlowMeter");
                if (fastSlowMeterType != null)
                {
                    var onGetJudgeMethods = fastSlowMeterType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.Name == "OnGetJudge")
                        .ToArray();
                    var onGetJudgeMethod = onGetJudgeMethods.FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length >= 1 && parameters[0].ParameterType == eJudgesType;
                    });
                    if (onGetJudgeMethod != null)
                    {
                        harmony.Patch(onGetJudgeMethod, 
                            prefix: new HarmonyLib.HarmonyMethod(typeof(JudgePatches), nameof(JudgePatches.FastSlowMeterOnGetJudgePrefix)));
                        MelonLogger.Msg("✓ FastSlowMeter.OnGetJudge(EJudges) 후킹 완료");
                    }
                }
                
                ApplyResultPatches(harmony, assemblyCSharp);
                MelonLogger.Msg("판정 패치 적용 완료");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"판정 패치 적용 오류: {ex}");
            }
        }

        private static void ApplyResultPatches(HarmonyLib.Harmony harmony, Assembly assemblyCSharp)
        {
            try
            {
                var managerResultType = assemblyCSharp.GetType("RhythmGame.Result.ManagerResult");
                if (managerResultType == null)
                {
                    MelonLogger.Warning("ManagerResult 타입을 찾을 수 없어 결과 저장 차단 패치를 건너뜁니다.");
                    return;
                }

                var compareHighScoreMethod = managerResultType.GetMethod("ComparePlayResultHighScore",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (compareHighScoreMethod != null)
                {
                    harmony.Patch(compareHighScoreMethod,
                        prefix: new HarmonyLib.HarmonyMethod(typeof(ResultPatches), nameof(ResultPatches.BlockComparePlayResultHighScore)));
                    MelonLogger.Msg("✓ ManagerResult.ComparePlayResultHighScore 차단 패치 완료");
                }
                else
                {
                    MelonLogger.Warning("ComparePlayResultHighScore 메서드를 찾을 수 없습니다.");
                }

                var postPlayResultMethod = managerResultType.GetMethod("PostRequestPlayResult",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (postPlayResultMethod != null)
                {
                    harmony.Patch(postPlayResultMethod,
                        prefix: new HarmonyLib.HarmonyMethod(typeof(ResultPatches), nameof(ResultPatches.BlockPostRequestPlayResult)));
                    MelonLogger.Msg("✓ ManagerResult.PostRequestPlayResult 차단 패치 완료");
                }
                else
                {
                    MelonLogger.Warning("PostRequestPlayResult 메서드를 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"결과 저장 차단 패치 적용 오류: {ex}");
            }
        }
    }
}
