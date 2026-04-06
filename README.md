# 탱크 게임 과제 프로젝트 (상세 가이드)

본 문서는 프로젝트의 핵심 기능 구현 방식과 설정값들을 상세히 기술하여 개발 가이드라인을 제공합니다.

---

## 🏗️ 1. 환경 설정 및 렌더링 (Environment & Rendering)

### 1.1 모델 텍스쳐 적용 및 머티리얼 설정
*   **URP (Universal Render Pipeline)** 기반으로 모든 머티리얼을 설정합니다.
*   **텍스쳐 최적화**: 
    *   `Albedo`: 탱크 모델의 색상 및 디테일 표현.
    *   `Normal Map`: 표면의 굴곡을 표현하여 로우폴리곤 모델에서도 고품질 디테일 확보.
    *   `Metallic/Smoothness`: 금속 질감 및 반사율 조절 (탱크 몸체는 낮은 Smoothness, 금속 부위는 높은 Metallic 설정).
*   **Shader**: URP/Lit 셰이더를 기본으로 사용하며, 필요한 경우 투사체 등에 Simple Lit이나 Unlit을 적용합니다.

---

## 📏 2. 스케일 및 물리 설정 (Scaling & Physics)

### 2.1 1:1 리얼 스케일 조절
*   **기준**: Unity의 `1 Unit = 1 Meter`를 준수합니다.
*   **모델 임포트**: 외부 모델(Blender/FBX) 임포트 시 `Scale Factor`를 조절하여 실제 탱크 규격(예: 전장 6-9m)에 맞춥니다.
*   **맵 크기**: 탱크가 충분히 기동할 수 있는 광활한 평지 또는 지형(Terrain)을 1:1 비율로 배치합니다.

### 2.2 Collider 및 Rigidbody
*   **Mesh Collider** 보다는 성능 최적화를 위해 **Box/Capsule Collider**를 조합하여 탱크의 외형을 감쌉니다.
*   **Rigidbody**: 탱크의 무게(Mass, 약 30-50톤 상정)를 반영하고, 뒤집힘 방지를 위해 무게 중심(Center of Mass)을 낮게 설정합니다.

---

## 🕹️ 3. 조작 및 카메라 (Movement & Camera)

### 3.1 WASD 이동 로직
*   **입력 시스템**: `Input.GetAxis("Vertical")` 및 `"Horizontal"`을 사용하여 입력을 감지합니다.
*   **전진/후진**: `Rigidbody.AddRelativeForce` 또는 `position` 변화를 통해 구현하며, 지형과의 마찰력을 고려합니다.
*   **회전(조향)**: 제자리 회전(Pivot Turn) 및 주행 회전이 가능하도록 `Rigidbody.AddRelativeTorque` 또는 `Rotate` 함수를 사용합니다.

### 3.2 3인칭 카메라 (Third-Person View)
*   **Cinemachine**: `Cinemachine Virtual Camera`를 활용하여 탱크의 뒤쪽 상단에 배치합니다.
*   **Follow & LookAt**: 탱크의 특정 Transform(주로 포탑 뒤쪽)을 대상으로 지정하여 부드러운 추적을 구현합니다.
*   **Damping**: 카메라 이동 및 회전에 적절한 Damping 값을 부여하여 급격한 움직임에도 유연하게 반응하도록 설정합니다.

### 3.3 바퀴 및 궤도 애니메이션 (Optional)
*   **스크립팅**: 탱크의 현재 속도(velocity)와 방향에 따라 바퀴(Wheel) 오브젝트가 X축으로 회전하도록 구현합니다.
*   **UV 스크롤**: 궤도(Track)의 경우 텍스쳐 UV를 스크롤하여 움직이는 듯한 효과를 줍니다.

---

## ⚔️ 4. 전투 시스템 (Combat System)

### 4.1 투사체(Bullet) 발사 구현
*   **Prefab**: 총알 오브젝트를 프리팹으로 미리 제작합니다.
*   **Fire Point**: 포신(Barrel) 끝에 발사 위치를 담당하는 빈 게임 오브젝트(Empty GO)를 배치합니다.
*   **Logic**:
    1.  사용자 클릭 시 `Instantiate`를 통해 프리팹 생성.
    2.  `Rigidbody.AddForce(barrel.forward * firePower, ForceMode.Impulse)`를 통해 즉시 가속.
*   **최적화**: 일정 시간 후 또는 충돌 후 `Destroy` 되도록 설정하여 메모리를 관리합니다.

---

## 📅 향후 계획
*   발사 시각 효과(VFX) 및 사운드 추가
*   적 탱크 AI 및 전투 시스템 확장
*   UI/HUD (체력바, 조준선 등) 적용

---

## ⚠️ 주의 사항
*  맵은 텍스트 없음