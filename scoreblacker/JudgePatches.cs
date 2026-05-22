using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace InventoryPopup
{
    // 판정 관련 패치 클래스
    [HarmonyPatch]
    public static class JudgePatches
    {
        // EJudges enum 타입 캐시
        private static Type eJudgesType = null;
        private static object bluestarValue = null;
        
        // EJudges 타입 및 BLUESTAR 값 가져오기
        private static void InitializeEJudges()
        {
            if (eJudgesType != null) return;
            
            try
            {
                var assemblyCSharp = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                
                if (assemblyCSharp != null)
                {
                    eJudgesType = assemblyCSharp.GetTypes()
                        .FirstOrDefault(t => t != null && t.IsEnum && t.Name == "EJudges");
                    
                    if (eJudgesType != null)
                    {
                        // BLUESTAR = 0 값 가져오기
                        bluestarValue = Enum.ToObject(eJudgesType, 0);
                        if (SceneDetector.DebugMode)
                        {
                            MelonLogger.Msg($"✓ EJudges 타입 초기화 완료: BLUESTAR = {bluestarValue}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Warning($"EJudges 초기화 오류: {ex.Message}");
                }
            }
        }
        
        // 공통 함수: JudgeCounter 필드 가져오기
        private static FieldInfo GetJudgeCounterField(Type instanceType)
        {
            string[] possibleFieldNames = { "<JudgeCount>k__BackingField", "JudgeCount", "judgeCount", "JudgeCounter" };
            
            foreach (var fieldName in possibleFieldNames)
            {
                var field = instanceType.GetField(fieldName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field;
            }
            
            // 타입 이름으로 찾기
            var allFields = instanceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return allFields.FirstOrDefault(f => 
                f.FieldType.Name == "JudgeCounter" || 
                f.FieldType.Name.IndexOf("JudgeCounter", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        // 공통 함수: judgeCount 배열 가져오기
        private static int[] GetJudgeCountArray(object __instance)
        {
            if (__instance == null) return null;
            
            try
            {
                var instanceType = __instance.GetType();
                var judgeCounterField = GetJudgeCounterField(instanceType);
                
                if (judgeCounterField == null) return null;
                
                var judgeCounter = judgeCounterField.GetValue(__instance);
                if (judgeCounter == null) return null;
                
                var counterType = judgeCounter.GetType();
                var judgeCountField = counterType.GetField("judgeCount", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (judgeCountField == null) return null;
                
                return judgeCountField.GetValue(judgeCounter) as int[];
            }
            catch
            {
                return null;
            }
        }
        
        // JudgeAction 후킹 - EJudges 파라미터를 BLUESTAR로 강제 변경
        public static bool JudgeActionPrefix(object __instance, ref object __0)
        {
            InitializeEJudges();
            
            // EJudges 파라미터를 BLUESTAR로 강제 변경
            if (eJudgesType != null && bluestarValue != null && __0 != null && __0.GetType() == eJudgesType)
            {
                var originalJudge = __0;
                __0 = bluestarValue;
                
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Msg($"[호출] JudgeAction: EJudges {originalJudge} → BLUESTAR");
                }
            }
            else if (SceneDetector.DebugMode)
            {
                MelonLogger.Msg("[호출] JudgeAction");
            }
            
            return true;
        }
        
        public static void JudgeActionPostfix(object __instance)
        {
            if (SceneDetector.DebugMode)
            {
                var judgeCountArray = GetJudgeCountArray(__instance);
                if (judgeCountArray != null)
                {
                    var judgeInfo = new System.Text.StringBuilder();
                    judgeInfo.Append("[판정 정보] ");
                    
                    string[] judgeNames = { "Perfect", "Great", "Good", "Miss", "Fast", "Slow" };
                    
                    for (int i = 0; i < judgeCountArray.Length; i++)
                    {
                        if (judgeCountArray[i] > 0)
                        {
                            string judgeName = i < judgeNames.Length ? judgeNames[i] : $"Index{i}";
                            judgeInfo.Append($"{judgeName}={judgeCountArray[i]} ");
                        }
                    }
                    
                    if (judgeCountArray.Any(v => v > 0))
                    {
                        MelonLogger.Msg(judgeInfo.ToString());
                    }
                }
                
                MelonLogger.Msg("[호출 완료] JudgeAction");
            }
        }
        
        // JudgeDivergence 후킹 - EJudges 파라미터를 BLUESTAR로 강제 변경
        public static bool JudgeDivergencePrefix(object __instance, ref object __0)
        {
            InitializeEJudges();
            
            // EJudges 파라미터를 BLUESTAR로 강제 변경
            if (eJudgesType != null && bluestarValue != null && __0 != null && __0.GetType() == eJudgesType)
            {
                var originalJudge = __0;
                __0 = bluestarValue;
                
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Msg($"[호출] JudgeDivergence: EJudges {originalJudge} → BLUESTAR");
                }
            }
            else if (SceneDetector.DebugMode)
            {
                MelonLogger.Msg("[호출] JudgeDivergence");
            }
            
            return true;
        }
        
        public static void JudgeDivergencePostfix(object __instance)
        {
            if (SceneDetector.DebugMode)
            {
                MelonLogger.Msg("[호출 완료] JudgeDivergence");
            }
        }
        
        // TryJudgeShortNote 후킹
        public static bool TryJudgeShortNotePrefix(object __instance)
        {
            if (SceneDetector.DebugMode)
            {
                MelonLogger.Msg("[호출] TryJudgeShortNote");
            }
            return true;
        }
        
        public static void TryJudgeShortNotePostfix(object __instance)
        {
            if (SceneDetector.DebugMode)
            {
                MelonLogger.Msg("[호출 완료] TryJudgeShortNote");
            }
        }
        
        // JudgeCounter.AddJudge 후킹 - EJudges 파라미터를 BLUESTAR로 강제 변경
        public static bool AddJudgePrefix(ref object __0)
        {
            InitializeEJudges();
            
            // EJudges 파라미터를 BLUESTAR로 강제 변경
            if (eJudgesType != null && bluestarValue != null && __0 != null && __0.GetType() == eJudgesType)
            {
                var originalJudge = __0;
                __0 = bluestarValue;
                
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Msg($"[호출] JudgeCounter.AddJudge: EJudges {originalJudge} → BLUESTAR");
                }
            }
            
            return true;
        }
        
        // JudgeTextViewer.OnGetJudge 후킹 - EJudges 파라미터를 BLUESTAR로 강제 변경
        public static bool OnGetJudgePrefix(ref object __0)
        {
            InitializeEJudges();
            
            // EJudges 파라미터를 BLUESTAR로 강제 변경
            if (eJudgesType != null && bluestarValue != null && __0 != null && __0.GetType() == eJudgesType)
            {
                var originalJudge = __0;
                __0 = bluestarValue;
                
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Msg($"[호출] JudgeTextViewer.OnGetJudge: EJudges {originalJudge} → BLUESTAR");
                }
            }
            
            return true;
        }
        
        // RedStarCounter.OnGetJudge 후킹 - EJudges 파라미터를 BLUESTAR로 강제 변경
        public static bool RedStarCounterOnGetJudgePrefix(ref object __0)
        {
            InitializeEJudges();
            
            // EJudges 파라미터를 BLUESTAR로 강제 변경
            if (eJudgesType != null && bluestarValue != null && __0 != null && __0.GetType() == eJudgesType)
            {
                var originalJudge = __0;
                __0 = bluestarValue;
                
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Msg($"[호출] RedStarCounter.OnGetJudge: EJudges {originalJudge} → BLUESTAR");
                }
            }
            
            return true;
        }
        
        // FastSlowMeter.OnGetJudge 후킹 - EJudges 파라미터를 BLUESTAR로 강제 변경
        public static bool FastSlowMeterOnGetJudgePrefix(ref object __0)
        {
            InitializeEJudges();
            
            // EJudges 파라미터를 BLUESTAR로 강제 변경
            if (eJudgesType != null && bluestarValue != null && __0 != null && __0.GetType() == eJudgesType)
            {
                var originalJudge = __0;
                __0 = bluestarValue;
                
                if (SceneDetector.DebugMode)
                {
                    MelonLogger.Msg($"[호출] FastSlowMeter.OnGetJudge: EJudges {originalJudge} → BLUESTAR");
                }
            }
            
            return true;
        }
    }
}
