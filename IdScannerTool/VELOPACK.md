# IdScannerTool Velopack

## 전제
- 업데이트 배포원은 GitHub Releases 입니다.
- 클라이언트 앱은 인증 토큰 없이 업데이트를 확인합니다.
- 따라서 `UPDATE_URL`이 가리키는 GitHub 저장소의 Releases 는 공개 접근이 가능해야 합니다.

## Config.ini
`[GENERAL]` 섹션에서 아래 값을 사용합니다.

```ini
UPDATE_URL=https://github.com/noeyhusChoi/moenybox_devicecontroller
UPDATE_DISABLE=0
UPDATE_CHANNEL=win
```

- `UPDATE_URL`: GitHub 저장소 URL
- `UPDATE_DISABLE`: `1`이면 업데이트 비활성화
- `UPDATE_CHANNEL`: Velopack 채널명. 현재 `win`

## 로컬 패키징

패키지 생성만:

```powershell
pwsh .\tools\pack-idscannertool-velopack.ps1 -Version 1.0.0
```

GitHub Releases 업로드까지:

```powershell
$env:VELOPACK_RELEASE_TOKEN = "<github token>"
pwsh .\tools\pack-idscannertool-velopack.ps1 -Version 1.0.0 -Upload -PublishRelease
```

산출물 위치:
- `artifacts\publish\IdScannerTool`
- `artifacts\velopack`

## GitHub Actions

워크플로 파일:
- [idscannertool-release.yml](c:\Users\niaci\RiderProjects\moenybox_devicecontroller\.github\workflows\idscannertool-release.yml)

트리거:
- 수동 실행
- `idscannertool-v*` 태그 push

필요 설정:
- secret: `VELOPACK_RELEASE_TOKEN`
  - 다른 저장소에 업로드할 때 필요
  - 같은 저장소에 업로드하면 `GITHUB_TOKEN` fallback 가능
- variable: `VELOPACK_REPO_URL`
  - 미설정 시 `https://github.com/noeyhusChoi/moenybox_devicecontroller` 사용

## 릴리즈 순서
1. 버전 결정
2. `tools/pack-idscannertool-velopack.ps1`로 로컬 검증
3. `workflow_dispatch` 또는 태그 push 실행
4. 설치 파일과 `RELEASES`, `releases.win.json` 업로드 확인
5. 설치본에서 업데이트 버튼으로 확인
