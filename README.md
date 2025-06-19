# DJI RS Command Console

이 저장소는 DJI RS 시리즈 짐벌을 CAN 네트워크로 제어하기 위한 간단한 콘솔 프로그램 예제를 포함합니다. `GimbalLib` 라이브러리를 사용하여 짐벌에 명령을 전송합니다.

## 프로젝트 구성
- `References/GimbalLib` : 짐벌 제어용 라이브러리
- `CommanderConsole` : 콘솔 기반 테스트 프로그램

## 사용 방법
1. .NET 6 SDK를 설치합니다.
2. `CommanderConsole` 프로젝트를 빌드 후 실행합니다.

```
dotnet run --project CommanderConsole
```

### 콘솔 명령
- `connect [ip] [port]` : 짐벌에 연결합니다.
- `move yaw roll pitch time(ms)` : 지정한 각도로 이동합니다.
- `speed yaw roll pitch` : 각 축의 속도를 설정합니다.
- `getangle` : 현재 각도를 요청합니다.
- `recenter` : 짐벌을 리센터합니다.
- `version` : 장비 버전 정보를 조회합니다.
- `help` : 명령 목록을 표시합니다.
- `exit` : 프로그램을 종료합니다.

명령은 공백으로 구분하여 입력하며, 각 파라미터는 십진수 값으로 전달합니다.
