using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace InventoryPopup
{
    public static class PauseMethodHelper
    {
        // RG_PS_Pause 인스턴스 캐시
        private static object cachedPauseInstance = null;
        private static Type pauseType = null;
        
        // Input 클래스 캐시
        private static Type inputType = null;
        private static MethodInfo getKeyDownMethod = null;
        
        // 키 입력 감지 (리플렉션 사용)
        public static bool CheckKeyDown(KeyCode keyCode)
        {
            try
            {
                if (inputType == null)
                {
                    inputType = typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.Input");
                    if (inputType == null)
                    {
                        // 다른 어셈블리에서 찾기
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            inputType = assembly.GetType("UnityEngine.Input");
                            if (inputType != null) break;
                        }
                    }
                    
                    if (inputType != null)
                    {
                        getKeyDownMethod = inputType.GetMethod("GetKeyDown", 
                            BindingFlags.Public | BindingFlags.Static, 
                            null, 
                            new Type[] { typeof(KeyCode) }, 
                            null);
                    }
                }
                
                if (getKeyDownMethod != null)
                {
                    var result = getKeyDownMethod.Invoke(null, new object[] { keyCode });
                    return result is bool boolResult && boolResult;
                }
            }
            catch (Exception ex)
            {
                // 첫 번째 호출 실패 시 한 번만 로그
                if (inputType == null)
                {
                    MelonLogger.Warning($"[ESC] Input.GetKeyDown 호출 오류: {ex.Message}");
                }
            }
            
            return false;
        }
        
        // RG_PS_Pause 메서드 강제 호출
        public static bool CallPauseMethod(string methodName, params object[] parameters)
        {
            try
            {
                // 타입 찾기
                if (pauseType == null)
                {
                    try
                    {
                        var assemblyCSharp = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
                        
                        if (assemblyCSharp == null)
                        {
                            MelonLogger.Warning("[ESC] Assembly-CSharp.dll을 찾을 수 없습니다.");
                            return false;
                        }
                        
                        pauseType = assemblyCSharp.GetType("RhythmGame.Play.RG_PS_Pause");
                        if (pauseType == null)
                        {
                            MelonLogger.Warning("[ESC] RG_PS_Pause 타입을 찾을 수 없습니다.");
                            return false;
                        }
                    }
                    catch (Exception typeEx)
                    {
                        MelonLogger.Warning($"[ESC] 타입 찾기 실패: {typeEx.Message}");
                        return false;
                    }
                }
                
                // 인스턴스 찾기
                if (cachedPauseInstance == null)
                {
                    try
                    {
                        // Resources.FindObjectsOfTypeAll 사용 (비활성화된 오브젝트도 포함)
                        MethodInfo findMethod = typeof(Resources).GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(m => m.Name == "FindObjectsOfTypeAll" && m.IsGenericMethodDefinition);
                        
                        if (findMethod != null)
                        {
                            try
                            {
                                var genericMethod = findMethod.MakeGenericMethod(pauseType);
                                var allObjects = genericMethod.Invoke(null, null) as UnityEngine.Object[];
                                if (allObjects != null && allObjects.Length > 0)
                                {
                                    cachedPauseInstance = allObjects[0];
                                }
                                else
                                {
                                    MelonLogger.Warning("[ESC] RG_PS_Pause 인스턴스를 찾을 수 없습니다.");
                                    return false;
                                }
                            }
                            catch (Exception invokeEx)
                            {
                                MelonLogger.Warning($"[ESC] 인스턴스 찾기 실패: {invokeEx.Message}");
                                return false;
                            }
                        }
                        else
                        {
                            MelonLogger.Warning("[ESC] FindObjectsOfTypeAll 메서드를 찾을 수 없습니다.");
                            return false;
                        }
                    }
                    catch (Exception instanceEx)
                    {
                        MelonLogger.Warning($"[ESC] 인스턴스 찾기 실패: {instanceEx.Message}");
                        return false;
                    }
                }
                
                // 메서드 찾기 및 호출
                MethodInfo method = null;
                
                // 파라미터 타입 배열 생성
                Type[] paramTypes = parameters.Length > 0
                    ? parameters.Select(p => p?.GetType() ?? typeof(object)).ToArray()
                    : Type.EmptyTypes;
                
                // 모든 오버로드 가져오기
                var allMethods = pauseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m != null && m.Name == methodName)
                    .ToArray();
                
                if (allMethods.Length == 0)
                {
                    MelonLogger.Warning($"[ESC] {methodName} 메서드를 찾을 수 없습니다.");
                    return false;
                }
                
                // 파라미터 개수와 타입이 일치하는 메서드 찾기
                var matchingMethods = allMethods.Where(m =>
                {
                    if (m == null) return false;
                    var methodParams = m.GetParameters();
                    if (methodParams.Length != paramTypes.Length)
                        return false;
                    
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        if (methodParams[i].ParameterType != paramTypes[i])
                        {
                            // 타입 호환성 체크
                            if (methodParams[i].ParameterType == typeof(float) && paramTypes[i] == typeof(double))
                                continue;
                            if (methodParams[i].ParameterType == typeof(double) && paramTypes[i] == typeof(float))
                                continue;
                            if (methodParams[i].ParameterType == typeof(int) && paramTypes[i] == typeof(long))
                                continue;
                            if (methodParams[i].ParameterType == typeof(long) && paramTypes[i] == typeof(int))
                                continue;
                            
                            return false;
                        }
                    }
                    return true;
                }).ToArray();
                
                if (matchingMethods.Length > 0)
                {
                    method = matchingMethods[0];
                }
                else
                {
                    // 파라미터 개수만 맞는 메서드 시도
                    var candidates = allMethods.Where(m => m != null && m.GetParameters().Length == parameters.Length).ToArray();
                    if (candidates.Length > 0)
                    {
                        method = candidates[0];
                    }
                }
                
                if (method == null)
                {
                    MelonLogger.Warning($"[ESC] {methodName} 메서드를 찾을 수 없습니다. (파라미터: {parameters.Length}개)");
                    return false;
                }
                
                // 인스턴스 유효성 재확인
                if (cachedPauseInstance == null)
                {
                    MelonLogger.Warning($"[ESC] {methodName} 호출 실패: 인스턴스가 null입니다.");
                    cachedPauseInstance = null;
                    return false;
                }
                
                // Unity 오브젝트인 경우 유효성 확인
                if (cachedPauseInstance is UnityEngine.Object unityObj)
                {
                    if (unityObj == null)
                    {
                        MelonLogger.Warning($"[ESC] {methodName} 호출 실패: Unity 오브젝트가 파괴되었습니다.");
                        cachedPauseInstance = null;
                        return false;
                    }
                }
                
                // 메서드 호출
                try
                {
                    object[] invokeParams = parameters.Length > 0 ? parameters : new object[0];
                    method.Invoke(cachedPauseInstance, invokeParams);
                    MelonLogger.Msg($"[ESC] {methodName}({string.Join(", ", parameters)}) 호출 완료");
                    return true;
                }
                catch (Exception invokeEx)
                {
                    MelonLogger.Warning($"[ESC] {methodName} 호출 실패: {invokeEx.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[ESC] {methodName} 호출 오류: {ex.Message}");
                cachedPauseInstance = null;
                return false;
            }
        }
        
        // 캐시 초기화 (인스턴스 재검색을 위해)
        public static void ClearCache()
        {
            cachedPauseInstance = null;
        }
    }
}







