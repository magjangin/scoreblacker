using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(InventoryPopup.SceneDetector), "Judge Patcher", "1.0.0", "GitHub Copilot")]
[assembly: MelonGame("Lyrebird Studio", "Sixtar Gate STARTRAIL")]
[assembly: MelonColor(255, 192, 203, 255)] // Pink (R, G, B, A)

namespace InventoryPopup
{
    public class SceneDetector : MelonMod
    {
        private HarmonyLib.Harmony harmony;
        
        // 디버그 모드 설정
        public static bool DebugMode = false;
        
        // ESC 키 입력 추적
        private float lastEscPressTime = 0f;
        private const float escDoublePressWindow = 0.5f; // 0.5초 내에 두 번 누르면 Close
        private bool escPressedOnce = false;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Judge Patcher 초기화");
            
            // EJudges 사용 분석
            JudgeAnalyzer.AnalyzeEJudgesUsage();
            
            // Harmony 패치 적용
            harmony = new HarmonyLib.Harmony("JudgePatcher");
            ScorePatchManager.ApplyJudgePatches(harmony);
            MelonLogger.Msg("판정 패치 적용 완료");
        }
        
        public override void OnUpdate()
        {
            // ESC 키 입력 처리
            bool escPressed = PauseMethodHelper.CheckKeyDown(KeyCode.Escape);
            if (escPressed)
            {
                float currentTime = Time.time;
                
                if (escPressedOnce && (currentTime - lastEscPressTime) < escDoublePressWindow)
                {
                    // ESC 두 번 누름 - Close 호출
                    escPressedOnce = false;
                    PauseMethodHelper.CallPauseMethod("Close");
                    MelonLogger.Msg("[ESC 두 번] Close() 강제 호출");
                }
                else
                {
                    // ESC 한 번 누름 - Show와 Start 호출
                    escPressedOnce = true;
                    lastEscPressTime = currentTime;
                    
                    // Show와 Start 호출
                    bool showSuccess = PauseMethodHelper.CallPauseMethod("Show", 0, 0.01f);
                    if (showSuccess)
                    {
                        // Show 성공 후 인스턴스 재확인 (Show가 인스턴스를 활성화할 수 있음)
                        PauseMethodHelper.ClearCache();
                    }
                    PauseMethodHelper.CallPauseMethod("Start");
                    MelonLogger.Msg("[ESC 한 번] Show()와 Start() 강제 호출");
                }
            }
            
            // 1번 키 입력 처리
            bool onePressed = PauseMethodHelper.CheckKeyDown(KeyCode.Alpha1);
            if (onePressed)
            {
                // Show와 Start 강제 호출
                bool showSuccess = PauseMethodHelper.CallPauseMethod("Show", 0, 0.01f);
                if (showSuccess)
                {
                    // Show 성공 후 인스턴스 재확인
                    PauseMethodHelper.ClearCache();
                }
                PauseMethodHelper.CallPauseMethod("Start");
                MelonLogger.Msg("[1번 키] Show()와 Start() 강제 호출");
            }
        }

        public override void OnApplicationQuit()
        {
            try
            {
                harmony?.UnpatchSelf();
            }
            catch { }
        }
    }
}
