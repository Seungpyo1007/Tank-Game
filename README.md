# Tank-Game (3D Physics)

유니티(Unity) **Universal Render Pipeline (URP)** 기반의 워썬더 스타일 3D 탱크 시뮬레이션 프로토타입입니다.

---

## 주요 기능

### 차량 물리
- **휠 서스펜션 시뮬레이션**: 좌/우 트랙 각 3개 휠 (총 6개) raycast 기반 서스펜션
- **지면 정렬 힘**: 모든 구동/그립 힘을 지면 법선 평면에 투영 (경사면 자연 주행)
- **차동 트랙 조향**: 좌/우 트랙 독립 토크 → W/S 직진, A/D 제자리 회전, W+A 등 차동
- **안티롤 바**: 좌/우 휠 압축 차이로 횡전(roll) 흡수
- **공기 저항 + 핸드브레이크**: 고속 감속, Space 급정거
- **모듈 데미지**: 엔진/포/좌트랙/우트랙 HP 별도 — 엔진 파괴 시 이동 불가, 포 파괴 시 사격 불가, 트랙 파괴 시 편향

### 전투
- **포탑/포신 분리**: 마우스 추적 yaw + pitch (-5°~+18°)
- **AP / HE 두 탄종**: Tab으로 전환, 8초 장전
- **장갑 관통 시스템**: 포탑/차체 별 두께 + 경사각 보정 + 사거리에 따른 관통력
- **치명타**: 부위별 즉사 확률 (포탑/차체 후면/측면 다름) — CREW KILL, AMMO RACK, ENGINE DESTROYED
- **탄도학**: 중력 낙하 + 셸 자체 모델 + 트레일/광원
- **머즐 플래시**: 파티클 기반 (광원 + 코어 + 가스 제트 + 불꽃 + 연기 + 지면 먼지) + 카메라 셰이크

### AI
- **적 3대 매복 배치**: 도시 폴리곤 분석으로 엄폐 위치 자동 선정
- **LOS 체크 + 조준 노이즈**: 시야 막히면 안 쏨, 완벽 조준 X
- **선제공격 딜레이**: 스폰 후 6초 대기 (플레이어 첫 행동 시간 확보)
- **차동 추적 이동**: 가까우면 후진/멀면 전진, 경사 감지

### 카메라 / UI
- **트레일링 3인칭 카메라**: 부드러운 따라가기, FOV 속도감
- **5단계 저격 줌**: RMB 홀드 + 스크롤 휠 (×2.1 ~ ×24), 스코프 비네팅 + mil-dot 리티클
- **HUD**: HP 바, 장전 게이지, 탄종 선택, 속도계, 적 카운터, 명중 피드백
- **나침반 스트립**: 차체 헤딩 실시간 표시 (N/E/S/W + 도(°))
- **피격 방향 인디케이터**: 데미지 받은 방향 가리키는 빨간 호
- **포물선 드롭 인디케이터**: 스코프 모드에서 셸 낙하점 예측
- **결과 화면**: 처치 수, 명중률, 데미지, 시간 통계

### 메뉴 / 시스템
- **메인 메뉴**: Start / Quit
- **일시정지** (Esc): RESUME / RESTART / MAIN MENU / QUIT
- **사망 / 승리 배너**: 전투 통계 + PLAY AGAIN 버튼
- **URP 후처리**: Bloom, Vignette, Color Adjustments, Tonemapping

### 환경
- **Unity 공식 URP Terrain Demo** (4×4 = 16타일, 헤이트맵 + PBR 텍스처 레이어)
- **풀 디테일 시스템**: 탱크가 풀숲에 묻히지 않도록 밀도/거리 조정
- **GrassCrusher**: 탱크 주변 디테일 실시간 제거 (지나가면 풀 부서짐)

---

## 컨트롤

| 입력 | 동작 |
|---|---|
| W / S | 전진 / 후진 |
| A / D | 좌 / 우 회전 (제자리 또는 코너링) |
| Space | 핸드브레이크 |
| Mouse | 포탑 + 포신 조준 (커서 따라감) |
| Left Click | 발사 |
| Right Click (hold) | 저격 스코프 |
| Scroll (스코프 중) | 배율 변경 |
| Tab | AP / HE 탄종 전환 |
| Esc | 일시정지 |

---

## 기술 스택 및 환경

- **엔진**: Unity 6000.4.0f1
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **입력**: Unity Input System (new)
- **외부 에셋**:
  - [Kenney City Kit (Suburban)](https://kenney.nl/assets/city-kit-suburban) — CC0
  - [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) — CC0
  - Unity Terrain - URP Demo Scene (Unity Asset Store, 무료)

---

## 시작하기

1. **저장소 클론**
   ```bash
   git clone https://github.com/Seungpyo1007/Tank-Game.git
   ```
2. **Unity Hub로 열기**: Unity 6000.4.0f1 LTS 권장
3. **Asset Store에서 추가 임포트** (저장소에 포함되지 않은 대용량 데모):
   - "Unity Terrain - URP Demo Scene" 검색 → My Assets → Import
4. **임포트 후 메뉴 실행**: `TankGame > Load Demo Terrain` (지형 16타일 로드)
5. **씬 열기**: `Assets/Scenes/MenuScene.unity` 또는 `MainScene.unity`
6. **Play**

만약 머티리얼이 핑크/보라로 깨지면:
- `TankGame > Convert Standard Mats To URP`
- `TankGame > Fix Terrain Materials`
- `TankGame > Restore Demo Tree Textures`

탱크가 지형 밑에 박혀 있으면:
- `TankGame > Snap Tanks To Terrain`

---

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Tank/         차체, 포탑, 포신, 휠 서스펜션, 맵 생성기
│   ├── Combat/       셸, 장갑, 모듈, 체력, 머즐플래시, 게임통계
│   ├── AI/           적 탱크 AI
│   ├── Camera/       카메라 리그, 후처리, 카메라 셰이크
│   ├── UI/           HUD, 메뉴, 리드 인디케이터, 적 HP 바
│   └── Data/         ShellData (ScriptableObject)
├── Editor/           에디터 메뉴 (지형 로드, 머티리얼 변환 등)
├── Prefabs/          Bullet 프리팹
├── Materials/        URP 머티리얼 (지형, 트레이서, 파티클 등)
├── Kenney/           Kenney CC0 자산 (City Kit, Nature Kit)
├── Scenes/           MenuScene, MainScene
└── Tex/              텍스처
```

---

## 라이선스 및 크레딧

- **라이선스**: [MIT License](LICENSE)
- **개발자**: [Seungpyo1007](https://github.com/Seungpyo1007)
- **3D 자산**: Kenney.nl (CC0), Unity Technologies (Asset Store)
