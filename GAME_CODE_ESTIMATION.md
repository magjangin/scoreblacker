# Sixtar Gate STARTRAIL 게임 코드 추정 문서

이 문서는 ScoreBlacker 모드의 리버스 엔지니어링 분석을 바탕으로 추정한 게임 코드 구조입니다.

## 목차

1. [핵심 타입 구조](#핵심-타입-구조)
2. [판정 시스템](#판정-시스템)
3. [스코어 시스템](#스코어-시스템)
4. [UI 시스템](#ui-시스템)
5. [메서드 시그니처](#메서드-시그니처)
6. [데이터 구조](#데이터-구조)

---

## 핵심 타입 구조

### 1. `RhythmGame.Play.RG_PS_Judgement`

게임의 판정 및 스코어 계산을 담당하는 핵심 클래스입니다.

#### 추정 클래스 구조

```csharp
namespace RhythmGame.Play
{
    public class RG_PS_Judgement
    {
        // 판정 카운터 (Perfect, Great, Good, Miss 등)
        private JudgeCounter JudgeCount { get; set; }
        
        // 판정 점수 (float)
        private float JudgeScore { get; set; }
        
        // 최종 점수 (int, UI에 표시되는 값)
        public int Score { get; }
        
        // 스타라이트 점수
        private float StarlightScore { get; set; }
        
        // 현재 콤보
        private int Combo { get; set; }
        
        // 스코어 추가 메서드
        public void AddStarlightScore(float score);
        
        // 판정 점수 계산 (private)
        private void CalculateJudgeScore(float delta);
        
        // 점수 업데이트 콜백 (private)
        private void OnScoreUpdated(int score);
        
        // 판정 액션 실행
        public void JudgeAction();
        
        // 판정 분기 처리
        public void JudgeDivergence();
        
        // 짧은 노트 판정 시도
        public void TryJudgeShortNote();
        
        // 콤보 추가 (private)
        private void AddCombo(int delta);
    }
}
```

#### 주요 역할

- **판정 처리**: 노트 판정 시 Perfect, Great, Good, Miss 등 판정 타입 결정
- **점수 계산**: 판정에 따른 점수 계산 및 누적
- **콤보 관리**: 연속 판정에 따른 콤보 증가/리셋
- **스코어 업데이트**: 점수 변경 시 UI에 알림

---

### 2. `JudgeCounter`

판정별 카운트를 저장하는 데이터 구조입니다.

#### 추정 구조

```csharp
public class JudgeCounter
{
    // 판정별 카운트 배열
    // 인덱스 0: Perfect
    // 인덱스 1: Great
    // 인덱스 2: Good
    // 인덱스 3: Miss
    // 인덱스 4: Fast (빠른 판정)
    // 인덱스 5: Slow (늦은 판정)
    private int[] judgeCount;
    
    // 또는 개별 필드로 구성될 수도 있음:
    // public int Perfect { get; set; }
    // public int Great { get; set; }
    // public int Good { get; set; }
    // public int Miss { get; set; }
    // public int Fast { get; set; }
    // public int Slow { get; set; }
}
```

---

## 판정 시스템

### 판정 타입 Enum

```csharp
namespace RhythmGame.Play
{
    public enum JudgeType
    {
        Perfect = 0,
        Great = 1,
        Good = 2,
        Miss = 3,
        Fast = 4,
        Slow = 5
    }
}
```

### 판정 처리 흐름

1. **노트 입력 감지** → `TryJudgeShortNote()` 또는 유사한 메서드 호출
2. **판정 계산** → `JudgeAction()` 또는 `JudgeDivergence()` 호출
3. **점수 계산** → `CalculateJudgeScore()` 호출
4. **스코어 업데이트** → `OnScoreUpdated()` 호출
5. **UI 업데이트** → UI 위젯들의 `OnGetScore()` 호출

---

## 스코어 시스템

### 점수 계산 방식

#### 1. 판정 점수 계산 (`CalculateJudgeScore`)

```csharp
private void CalculateJudgeScore(float delta)
{
    // delta: 이번 판정으로 추가될 점수
    // JudgeScore에 delta를 더함
    JudgeScore += delta;
    
    // 판정 타입에 따라 다른 점수 배율 적용 가능
    // Perfect: 1.0x
    // Great: 0.8x
    // Good: 0.6x
    // Miss: 0x
}
```

#### 2. 스타라이트 점수 추가 (`AddStarlightScore`)

```csharp
public void AddStarlightScore(float score)
{
    // 스타라이트 점수 추가
    StarlightScore += score;
    
    // 최종 점수에 반영될 수도 있음
    // Score = (int)(JudgeScore + StarlightScore);
}
```

#### 3. 점수 업데이트 (`OnScoreUpdated`)

```csharp
private void OnScoreUpdated(int score)
{
    // score: 새로운 최종 점수
    Score = score;
    
    // UI 위젯들에게 점수 변경 알림
    // PlayWidget.OnGetScore(score) 호출
    // RG_Scoreboard.InterpolateToScore(score) 호출
    // HighscoreMeter.OnGetScore(score) 호출
}
```

### 점수 필드 관계

```
JudgeScore (float) ──┐
                      ├──> Score (int) ──> UI 표시
StarlightScore (float) ┘
```

- `JudgeScore`: 판정에 따른 기본 점수 (float)
- `StarlightScore`: 추가 보너스 점수 (float)
- `Score`: 최종 점수 (int, UI에 표시)

---

## UI 시스템

### 1. `RhythmGame.Play.PlayWidget`

게임 플레이 중 점수를 표시하는 UI 위젯입니다.

#### 추정 구조

```csharp
namespace RhythmGame.Play
{
    public class PlayWidget : MonoBehaviour
    {
        // 점수 업데이트 콜백 (단일 파라미터)
        public void OnGetScore(int score)
        {
            // UI에 점수 표시
            // 예: scoreText.text = score.ToString();
        }
        
        // 점수 업데이트 콜백 (점수 + 목표 점수)
        public void OnGetScore(int score, int targetScore)
        {
            // UI에 점수 및 목표 점수 표시
            // 예: scoreText.text = $"{score} / {targetScore}";
        }
    }
}
```

### 2. `RG_Scoreboard`

스코어보드 UI 컴포넌트입니다.

#### 추정 구조

```csharp
public class RG_Scoreboard : MonoBehaviour
{
    // 점수를 부드럽게 보간하여 표시
    public void InterpolateToScore(int targetScore)
    {
        // 현재 표시 중인 점수에서 targetScore로 부드럽게 전환
        // 코루틴 또는 DOTween 등을 사용하여 애니메이션
    }
}
```

### 3. `RhythmGame.Play.HighscoreMeter`

하이스코어 미터 UI 컴포넌트입니다.

#### 추정 구조

```csharp
namespace RhythmGame.Play
{
    public class HighscoreMeter : MonoBehaviour
    {
        // 하이스코어 업데이트
        public void OnGetScore(int score)
        {
            // 하이스코어 미터 바 업데이트
            // 예: meter.fillAmount = (float)score / maxScore;
        }
    }
}
```

### 4. `RhythmGame.Play.RG_PS_Pause`

일시정지 UI를 관리하는 클래스입니다.

#### 추정 구조

```csharp
namespace RhythmGame.Play
{
    public class RG_PS_Pause : MonoBehaviour
    {
        // 일시정지 UI 표시
        public void Show(int curScore, float startFinishRatio)
        {
            // curScore: 현재 점수
            // startFinishRatio: 시작/종료 비율 (0.0 ~ 1.0)
            // 일시정지 메뉴 UI 활성화
        }
        
        // 일시정지 UI 시작 (애니메이션 등)
        public void Start()
        {
            // 일시정지 애니메이션 시작
        }
        
        // 일시정지 UI 닫기
        public void Close()
        {
            // 일시정지 메뉴 UI 비활성화
            // 게임 재개
        }
    }
}
```

---

## 메서드 시그니처

### RG_PS_Judgement 클래스

| 메서드 | 접근성 | 반환 타입 | 파라미터 | 설명 |
|--------|--------|-----------|----------|------|
| `AddStarlightScore` | public | void | `float score` | 스타라이트 점수 추가 |
| `CalculateJudgeScore` | private | void | `float delta` | 판정 점수 계산 |
| `OnScoreUpdated` | private | void | `int score` | 점수 업데이트 콜백 |
| `JudgeAction` | public | void | - | 판정 액션 실행 |
| `JudgeDivergence` | public | void | - | 판정 분기 처리 |
| `TryJudgeShortNote` | public | void | - | 짧은 노트 판정 시도 |
| `AddCombo` | private | void | `int delta` | 콤보 추가 |

### UI 위젯 클래스

| 클래스 | 메서드 | 접근성 | 반환 타입 | 파라미터 | 설명 |
|--------|--------|--------|-----------|----------|------|
| `PlayWidget` | `OnGetScore` | public | void | `int score` | 점수 업데이트 (단일) |
| `PlayWidget` | `OnGetScore` | public | void | `int score, int targetScore` | 점수 업데이트 (이중) |
| `RG_Scoreboard` | `InterpolateToScore` | public | void | `int score` | 점수 보간 애니메이션 |
| `HighscoreMeter` | `OnGetScore` | public | void | `int score` | 하이스코어 업데이트 |
| `RG_PS_Pause` | `Show` | public | void | `int curScore, float startFinishRatio` | 일시정지 UI 표시 |
| `RG_PS_Pause` | `Start` | public | void | - | 일시정지 UI 시작 |
| `RG_PS_Pause` | `Close` | public | void | - | 일시정지 UI 닫기 |

---

## 데이터 구조

### JudgeCounter 구조

```csharp
public class JudgeCounter
{
    // 판정별 카운트 배열
    // 인덱스 매핑:
    // [0] = Perfect
    // [1] = Great
    // [2] = Good
    // [3] = Miss
    // [4] = Fast
    // [5] = Slow
    private int[] judgeCount;
    
    // 또는 프로퍼티로:
    // public int Perfect { get; set; }
    // public int Great { get; set; }
    // public int Good { get; set; }
    // public int Miss { get; set; }
    // public int Fast { get; set; }
    // public int Slow { get; set; }
}
```

### RG_PS_Judgement 필드/프로퍼티

| 이름 | 타입 | 접근성 | 설명 |
|------|------|--------|------|
| `JudgeCount` | `JudgeCounter` | private | 판정 카운터 |
| `JudgeScore` | `float` | private | 판정 점수 |
| `Score` | `int` | public (getter only) | 최종 점수 |
| `StarlightScore` | `float` | private | 스타라이트 점수 |
| `Combo` | `int` | private | 현재 콤보 |

---

## 점수 계산 흐름도

```
노트 입력
    ↓
TryJudgeShortNote() / JudgeAction()
    ↓
판정 타입 결정 (Perfect/Great/Good/Miss)
    ↓
JudgeCounter 업데이트 (judgeCount[타입]++)
    ↓
CalculateJudgeScore(delta) 호출
    ↓
JudgeScore += delta
    ↓
OnScoreUpdated(Score) 호출
    ↓
Score = (int)JudgeScore (또는 Score = (int)(JudgeScore + StarlightScore))
    ↓
UI 업데이트:
  - PlayWidget.OnGetScore(Score)
  - RG_Scoreboard.InterpolateToScore(Score)
  - HighscoreMeter.OnGetScore(Score)
```

---

## 추정 근거

이 문서의 추정은 다음 근거를 바탕으로 작성되었습니다:

1. **리플렉션 분석**: `JudgementAnalyzer`, `ScoreTypeAnalyzer`에서 Assembly-CSharp.dll의 타입 구조 분석
2. **메서드 후킹**: Harmony 패치를 통해 실제 호출되는 메서드 시그니처 확인
3. **필드 접근**: 리플렉션을 통한 필드 읽기/쓰기로 데이터 구조 추정
4. **로깅 분석**: 디버그 모드에서 출력되는 로그를 통한 동작 흐름 파악

---

## 주의사항

- 이 문서는 **추정**이며, 실제 게임 코드와 다를 수 있습니다.
- 게임 업데이트 시 구조가 변경될 수 있습니다.
- 일부 메서드나 필드는 private일 수 있어 정확한 구조를 확인하기 어렵습니다.
- 실제 구현은 Unity의 코루틴, 이벤트 시스템 등을 사용할 수 있습니다.

---

## 참고 자료

- ScoreBlacker 모드 소스 코드
- MelonLoader 로그 출력
- Harmony 패치 분석 결과

