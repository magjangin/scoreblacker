# ScoreBlacker 개발 가이드

## 개발 환경 설정

### 필수 요구사항

1. **Visual Studio 2019 이상**
   - C# 7.3 이상 지원
   - .NET Framework 4.7.2

2. **MelonLoader**
   - ScoreBlacker의 기반 플랫폼
   - 게임 모드 로딩 및 Harmony 패칭 지원
   - [MelonLoader 공식 사이트](https://melonwiki.xyz/)

3. **Harmony 라이브러리**
   - MelonLoader에 포함됨
   - IL 수준의 메서드 패칭 지원

4. **Sixtar Gate STARTRAIL**
   - 대상 게임
   - 최신 버전으로 업데이트 필요

### 개발 환경 구성 단계

```
1. 프로젝트 복제/다운로드
2. scoreblacker.sln 열기 (Visual Studio)
3. 패키지 복원
4. 빌드 구성 설정 (Debug/Release)
5. 컴파일 및 DLL 생성
6. MelonLoader Mods 폴더에 배포
7. 게임 실행 및 테스트
```

## 빌드 프로세스

### 빌드 스크립트

#### build.bat (Windows)
```batch
@echo off
:: Release 빌드
msbuild scoreblacker.sln /p:Configuration=Release /p:Platform=AnyCPU

:: DLL 출력
echo Build completed. Check bin\Release\ for DLL
pause
```

#### clean_build.bat (Windows)
```batch
@echo off
:: 기존 빌드 결과 정리
rmdir /s /q bin obj

:: 클린 빌드 수행
msbuild scoreblacker.sln /p:Configuration=Release /p:Platform=AnyCPU /t:Clean,Build

echo Clean build completed
pause
```

### 빌드 절차

1. **개발 버전 빌드 (Debug)**
   ```
   Visual Studio에서 F5 누르기 (또는 Build > Build Solution)
   ```

2. **배포 버전 빌드 (Release)**
   ```
   Visual Studio에서 Configuration을 Release로 변경
   Build > Build Solution
   ```

3. **배포**
   ```
   bin\Release\scoreblacker.dll을 MelonLoader Mods 폴더로 복사
   ```

## 코드 구조 및 작성 규칙

### 명명 규칙

```csharp
// 클래스: PascalCase
public class ScorePatches { }

// 메서드: PascalCase
public static bool Prefix(...) { }

// 프로퍼티: PascalCase
public static float ScoreMultiplier { get; set; }

// 로컬 변수, 파라미터: camelCase
float adjustedScore = originalScore * multiplier;

// 상수: UPPER_SNAKE_CASE
private const float SCORE_INCREMENT = 0.01f;

// private 필드: _camelCase (또는 camelCase)
private float _lastManipulatedScore = 0f;
```

### 코드 스타일

#### 들여쓰기
```csharp
// 4 스페이스 또는 탭 사용
public class Example
{
    public void Method()
    {
        if (condition)
        {
            DoSomething();
        }
    }
}
```

#### 주석 작성
```csharp
// 단일 줄 주석
/// <summary>
/// 메서드/클래스에 대한 XML 문서화
/// </summary>
public void Method()
{
    // 코드 설명
    int value = 0;
}
```

#### 메서드 길이
- 권장: 50줄 이하
- 긴 메서드는 여러 메서드로 분리
- 각 메서드는 단일 책임

## Harmony 패칭 방법론

### 패치 유형

#### 1. Prefix (메서드 실행 전)
```csharp
public static bool Prefix(ref int score)
{
    // 메서드 실행 전 처리
    score = score * 2; // 스코어 2배
    
    return true;  // true: 원본 메서드 실행, false: 원본 메서드 건너뜀
}
```

**사용 사례**:
- 파라미터 전처리
- 조건부 실행 제어
- 조기 반환 (return false)

#### 2. Postfix (메서드 실행 후)
```csharp
public static void Postfix(ref int __result)
{
    // 메서드 실행 후 처리
    __result = __result + 100; // 결과값에 100 추가
}
```

**사용 사례**:
- 반환값 수정
- 부작용 처리
- 로깅/모니터링

#### 3. Transpiler (IL 코드 수정 - 고급)
```csharp
public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // IL 코드 수정 (고급 기술)
    // 거의 사용하지 않음
}
```

### 패치 적용 패턴

#### 패턴 1: 간단한 Prefix
```csharp
public static void Apply(Harmony harmony)
{
    var original = AccessTools.Method(typeof(TargetClass), "TargetMethod");
    var prefix = new HarmonyMethod(typeof(Patches), nameof(Prefix));
    
    harmony.Patch(original, prefix: prefix);
}

public static bool Prefix(TargetClass __instance, int value)
{
    MelonLogger.Msg($"TargetMethod called with value: {value}");
    return true;
}
```

#### 패턴 2: 복잡한 Prefix + Postfix
```csharp
public static void Apply(Harmony harmony)
{
    var original = AccessTools.Method(typeof(TargetClass), "TargetMethod");
    var prefix = new HarmonyMethod(typeof(Patches), nameof(Prefix));
    var postfix = new HarmonyMethod(typeof(Patches), nameof(Postfix));
    
    harmony.Patch(original, prefix: prefix, postfix: postfix);
}

public static bool Prefix(TargetClass __instance)
{
    __instance.SomeField = GetModifiedValue();
    return true;
}

public static void Postfix(ref int __result)
{
    __result = ApplyModification(__result);
}
```

#### 패턴 3: 동적 메서드 검색 및 패칭
```csharp
public static void Apply(Harmony harmony)
{
    var assembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
    
    var type = assembly?.GetType("Game.ScoreSystem");
    var methods = type?.GetMethods()
        .Where(m => m.Name.Contains("Score"))
        .ToArray();
    
    foreach (var method in methods ?? new MethodInfo[0])
    {
        var prefix = new HarmonyMethod(typeof(Patches), nameof(GenericPrefix));
        harmony.Patch(method, prefix: prefix);
    }
}

public static bool GenericPrefix(MethodBase __originalMethod)
{
    MelonLogger.Msg($"Patched: {__originalMethod.Name}");
    return true;
}
```

## 디버깅 기법

### 1. 콘솔 로깅
```csharp
MelonLogger.Msg("정보 메시지");
MelonLogger.Warning("경고 메시지");
MelonLogger.Error("에러 메시지");
```

### 2. 중단점 및 단계 실행
```
Visual Studio에서 F9로 중단점 설정
F10으로 단계 실행 (한 줄씩)
F11로 함수 내부 진입
```

### 3. 메모리 검사
```csharp
// 필드 값 확인
var value = __instance.GetType()
    .GetField("_fieldName", AccessTools.all)
    ?.GetValue(__instance);
    
MelonLogger.Msg($"Field value: {value}");
```

### 4. 상세 로깅
```csharp
public static bool Prefix(TargetClass __instance, int param)
{
    if (DebugMode)
    {
        MelonLogger.Msg($"Method called");
        MelonLogger.Msg($"  Instance: {__instance}");
        MelonLogger.Msg($"  Param: {param}");
        MelonLogger.Msg($"  Field: {__instance.SomeField}");
    }
    return true;
}
```

## 테스팅 전략

### 단위 테스트
- 개별 메서드 기능 검증
- Prefix/Postfix 로직 테스트

### 통합 테스트
```
로컬 모드에서:
1. 게임 시작
2. 곡 플레이
3. 점수 확인
4. 판정 확인
5. UI 업데이트 확인
```

### 회귀 테스트
```
매 버전 업데이트 후:
1. 기존 기능 정상 작동 확인
2. 성능 저하 여부 확인
3. 크래시 발생 여부 확인
```

## 배포 및 릴리스

### 버전 관리
```
MAJOR.MINOR.PATCH
1.2.3

- MAJOR: 큰 기능 추가/변경
- MINOR: 작은 기능 추가
- PATCH: 버그 픽스
```

### 릴리스 체크리스트
```
☐ 모든 기능 테스트 완료
☐ 버그 수정 확인
☐ 버전 번호 업데이트
☐ CHANGELOG.md 작성
☐ Release 빌드 수행
☐ DLL 파일 서명 (선택사항)
☐ 배포 및 공지
```

## 일반적인 문제 해결

### 패치가 적용되지 않음
```
1. 메서드명 확인 (AccessTools.Method에서 정확한 이름 사용)
2. 타입명 확인 (네임스페이스 포함 전체 경로)
3. 파라미터 시그니처 확인
4. 콘솔 로그에서 "Patching failed" 메시지 확인
```

### NullReferenceException
```
1. __instance 체크: if (__instance == null) return;
2. 필드 초기화 확인
3. Reflection 결과 null 체크
4. Safe navigation operator (?) 사용
```

### 성능 저하
```
1. Prefix/Postfix 로직 단순화
2. 불필요한 Reflection 제거
3. 로깅 수준 감소
4. 메서드 캐싱 사용
```

## 학습 자료

### 공식 문서
- [MelonLoader 문서](https://melonwiki.xyz/)
- [Harmony 가이드](https://harmony.pardeike.net/)
- [C# 공식 문서](https://docs.microsoft.com/ko-kr/dotnet/csharp/)

### 고급 주제
- IL (Intermediate Language) 이해
- Reflection 고급 기법
- 메모리 관리 및 최적화
