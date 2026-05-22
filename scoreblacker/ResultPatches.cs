using MelonLoader;

namespace InventoryPopup
{
    public static class ResultPatches
    {
        public static bool BlockComparePlayResultHighScore()
        {
            MelonLogger.Msg("[결과 차단] 베스트/로컬 저장 갱신을 차단했습니다.");
            return false;
        }

        public static bool BlockPostRequestPlayResult()
        {
            MelonLogger.Msg("[결과 차단] 서버 랭킹 제출을 차단했습니다.");
            return false;
        }
    }
}
