# Simulator (2025)
## 🛠️ Project Setup

### 📦 Environment
- **Unity Version**: `2022.3.14f1`
- **Input System**: `Unity Input System Package (v1.6.1 or later)`
- **3D Template**: Core 3D Project

### 🎮 Hardware Used
- **Racing Wheel**: Thrustmaster T300RS
  - Steering axis: Stick X
  - Throttle: Slider (1 → -1)
  - Brake: Stick Y (-1 → 1)
- **Tested On**: Windows 11 PC

### 🧪 Features
- Basic driving simulation using a cube
- Physics-based movement with Rigidbody
- Steering, throttle, and brake mapped to racing wheel inputs
- Camera automatically follows the player cube
- Smooth deceleration and braking logic implemented
- Easily portable to other Unity scenes

### 📁 Key Files
- `Controls.inputactions`: Input action map
- `WheelCubeControl.cs`: Player cube movement and input handler
- `CameraFollow.cs`: Simple camera tracking script

### MEMO
unity 고치기 위해 무엇을 했는지…
- 다운된 driving 시나리오가 자율주행 모드인것을 알고 자율주행 관련 스크립트 및 컴포넌트 비활성화함
- unity랑 thrustmaster가 연결 잘 되는지 확인 위해 간단한 시나리오 만듬 (운전대로 cube 조작하기)
1. Input system 셋업
2. Action asset 생성 - steering, throttle, brake
3. 3D object 생성 (cube)
4. Control C# script 생성 (WheelCubeControl.cs)
5. Cube랑 script 연결
6. 카메라가 cube 따라 다니도록 C# script 추가 (CameraFollow.cs)
7. 차량운전과 비슷하도록 rigid body 추가하고 세부 설정 조정 mass drag angulardrag 등 (추가 튜닝 필요)
