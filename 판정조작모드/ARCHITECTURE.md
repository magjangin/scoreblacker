# ScoreBlacker 아키텍처 가이드

## 프로젝트 아키텍처 개요

ScoreBlacker는 Harmony 패칭 시스템을 기반으로 한 모듈식 아키텍처로 설계되었습니다.

```
┌─────────────────────────────────────────────┐
│         SceneDetector (Entry Point)         │
│  - 모드 초기화 및 패치 관리                  │
│  - 입력 처리 및 이벤트 루프                  │
└──────────────────┬──────────────────────────┘
                   │
        ┌──────────┼──────────┬──────────┐
        │          │          │          │
        ▼          ▼          ▼          ▼
    ┌────────┐ ┌─────────┐ ┌────────┐ ┌──────────┐
    │ Score  │ │ Result  │ │ Judge  │ │ UI/Other │
    │Patches │ │ Patches │ │Patches │ │ Patches  │
    └────┬───┘ └────┬────┘ └────┬───┘ └────┬─────┘
         │          │           │          │
         └──────────┼───────────┼──────────┘
                    │           │
        ┌───────────┴───────────┴──┐
        │                          │
        ▼                          ▼
   ┌─────────┐              ┌─────────────┐
   │ Harmony │              │  Game Code  │
   │ Patches │              │ (Modified)  │
   └─────────┘              └─────────────┘
```

## 핵심 컴포넌트

### 1. SceneDetector (메인 모듈)

**역할**: 모드의 진입점 및 조율자 역할

```csharp
public class SceneDetector : MelonMod
{
    // 모드 초기화
    public override void OnInitializeMelon();
    
    // 프레임 업데이트 (입력 처리)
    public override void OnUpdate();
    
    // 씬 로드 이벤트 처리
    private void OnSceneLoad(int buildIndex, string sceneName);
}
```

**책임**:
- Harmony 패치 적용/관리
- 키보드 입력 처리 (ESC, 1번 키)
- 씬 로드 이벤트 감시
- 전역 설정 관리

### 2. ScorePatches (점수 조작 모듈)

**역할**: 게임의 스코어 계산 시스템을 후킹

```
게임 스코어 시스템
  ↓ [Prefix Hook]
ScorePatches.OnScoreUpdated()
  - 판정 변경 (모두 Perfect로)
  - 점수 제한 (0.01씩만 증가)
  - JudgeScore 필드 수정
  ↓ [Original 메서드 실행]
  ↓ [Postfix Hook]
UI 업데이트
```

**주요 메서드**:
- `AddStarlightScore`: 스타라이트 점수 추가 시 후킹
- `CalculateJudgeScore`: 판정 점수 계산 시 후킹
- `OnScoreUpdated`: 점수 업데이트 시 후킹 (핵심)
- 프로퍼티 getter 후킹: `Score`, `JudgeScore`

### 3. ResultPatches (결과 기록 차단 모듈)

**역할**: 결과 화면에서 로컬 기록 갱신과 서버 랭킹 제출을 차단

```
ManagerResult.ComparePlayResultHighScore()
  ↓ [Prefix Hook]
ResultPatches.BlockComparePlayResultHighScore()
  - highScore/fullCombo/rank 갱신 차단
  - SavePlayData 호출 차단
  ↓ [Original 메서드 실행 안 함]

ManagerResult.PostRequestPlayResult()
  ↓ [Prefix Hook]
ResultPatches.BlockPostRequestPlayResult()
  - LyrebirdServer.PostUserScore 호출 차단
  ↓ [Original 메서드 실행 안 함]
```

현재 정책은 조건 게이트 없이 모드가 활성화된 동안 결과 저장, 베스트 갱신, 서버 랭킹 제출을 항상 차단한다.

### 4. JudgePatches (판정 조작 모듈)

**역할**: 게임의 판정 시스템을 후킹하여 모든 판정을 Perfect로 변경

```
사용자 노트 입력
  ↓
게임 판정 시스템 (Great, Good, Miss 등 결정)
  ↓ [Postfix Hook]
JudgePatches.JudgeAction()
  - 판정을 Perfect로 강제 변경
  - 판정 로깅
  ↓
게임 스코어 시스템에 Perfect 판정 전달
```

**주요 메서드**:
- `JudgeAction`: 판정 실행 시 후킹
- `JudgeDivergence`: 판정 분기 처리 후킹
- `TryJudgeShortNote`: 짧은 노트 판정 후킹

### 5. 분석 유틸리티 클래스

게임 구조를 분석하고 이해하기 위한 도구들:

#### SceneAnalyzer
- "Score"가 포함된 오브젝트 검색
- 오브젝트 구조 매핑
- 컴포넌트 정보 추출

#### JudgementAnalyzer
- RG_PS_Judgement 타입 상세 분석
- 필드, 메서드, 이넘 정보 추출
- 판정 카운터 구조 분석

#### ScoreTypeAnalyzer
- 스코어 관련 모든 타입 검색
- 타입 계층 구조 분석
- 메서드/필드 시그니처 분석

## 데이터 흐름

### 점수 조작 프로세스

```
1. 게임에서 노트 판정 발생
        ↓
2. JudgePatches.JudgeAction() [Postfix]
   → 판정을 Perfect로 변경
   → 판정 정보 로깅
        ↓
3. 게임 스코어 시스템 실행
   (수정된 판정으로 점수 계산)
        ↓
4. ScorePatches.OnScoreUpdated() [Prefix]
   → JudgeCounter 배열 조작
     (Perfect가 아닌 것들을 0으로, Perfect에 모두 더함)
   → 원본 점수에서 JudgeScore 읽기
   → 점수 증가량 제한 (0.01씩만)
   → LastManipulatedScore 업데이트
        ↓
5. 원본 메서드 실행
   (조작된 데이터로 UI 업데이트)
```

### 결과 기록 차단 프로세스

```
1. 결과 화면 ManagerResult.Start() 실행
        ↓
2. ComparePlayResultHighScore(playResult) 호출
        ↓
3. ResultPatches.BlockComparePlayResultHighScore() [Prefix]
   → 베스트/로컬 저장 갱신 차단
        ↓
4. PostRequestPlayResult() 호출
        ↓
5. ResultPatches.BlockPostRequestPlayResult() [Prefix]
   → 서버 랭킹 제출 차단
```

## 확장 포인트 (Extension Points)

### 새로운 패치 추가

```csharp
// 1. 새로운 패치 클래스 생성
public class NewPatches
{
    public static void Apply(Harmony harmony)
    {
        // Harmony 패치 정의
        var method = AccessTools.Method(typeof(TargetClass), "TargetMethod");
        var prefix = new HarmonyMethod(typeof(NewPatches), nameof(Prefix));
        harmony.Patch(method, prefix: prefix);
    }
    
    public static bool Prefix(/* parameters */)
    {
        // 처리 로직
        return true; // 원본 메서드 실행
    }
}

// 2. SceneDetector에서 호출
private void ApplyPatches(Harmony harmony)
{
    NewPatches.Apply(harmony);
}
```

### 새로운 분석 도구 추가

```csharp
public class NewAnalyzer
{
    public static void Analyze()
    {
        // 분석 로직
        MelonLogger.Msg("Analysis Result...");
    }
}

// OnUpdate에서 특정 키 입력 시 호출
if (Input.GetKeyDown(KeyCode.F9))
{
    NewAnalyzer.Analyze();
}
```

## 패치 적용 순서

1. **ScorePatches 적용** (점수 조작)
2. **ResultPatches 적용** (결과 저장/랭킹 차단)
3. **JudgePatches 적용** (판정 변경)
4. **기타 패치 적용** (UI, 위젯 등)

이 순서는 중요합니다. 판정이 먼저 변경되어야 점수 계산이 올바르게 작동합니다.

## 성능 고려사항

### 최적화된 부분
- 메서드 검색은 초기화 시에만 수행 (O(n) → O(1) for hooks)
- Prefix/Postfix는 최소한의 로직만 수행
- 배열 조작 시 직접 접근 사용

### 성능 영향
- Harmony 패칭: ~1-2ms per hook call (미미함)
- 분석 도구: 초기 로드 시에만 실행
- 디버그 로깅: 활성화 시 약간의 오버헤드

## 디버깅 가이드

### 문제 진단 절차

1. **DebugMode 활성화**
   ```csharp
   public static bool DebugMode = true;
   ```

2. **콘솔 로그 확인**
   - 패치 적용 여부
   - 메서드 후킹 성공 여부
   - 각 단계의 처리 결과

3. **분석 도구 실행**
   ```
   - ESC: 게임 상태 스냅샷
   - F1: Scene 분석
   - F2: Judge 시스템 분석
   ```

4. **코드 추적**
   - OnScoreUpdated 실행 여부
   - JudgeCounter 배열 상태
   - 점수 값 변화

## 보안 및 안정성

### 안전한 메모리 접근
- Reflection 사용 시 null 체크
- Try-catch 로 예외 처리
- 잘못된 오프셋 접근 방지

### 게임 안정성
- Prefix에서 게임 상태 과도하게 수정하지 않음
- Original 메서드 실행 허용
- 예외 발생 시 로깅 후 진행

## 마이그레이션 가이드

### 게임 버전 업데이트 시

1. **메서드 시그니처 확인**
   - 타겟 메서드가 여전히 존재하는지 확인
   - 파라미터 변경 여부 확인

2. **분석 도구 실행**
   ```
   F2: 새로운 메서드 구조 분석
   ```

3. **패치 업데이트**
   - 변경된 메서드에 대한 패치 수정
   - 새로운 메서드 추가

4. **테스트**
   ```
   로컬 모드에서 기능 테스트
   ```
