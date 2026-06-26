# WebFlexVer2 AI 작업 컨텍스트

이 문서는 새 대화창에서 WebFlexVer2 UI 리뉴얼 작업을 이어가기 위한 요약 문서입니다.

새 대화에서는 먼저 이 파일을 읽고, 저장소 최신 코드를 확인한 뒤 작업합니다.

---

## 작업 방식

- 저장소는 GitHub 연결된 `sokumi/WebFlexVer2` 기준으로 최신 코드를 먼저 확인합니다.
- 사용자가 명시적으로 요청하지 않는 한 직접 저장소를 수정하지 않고, 적용 가능한 수정 전/후 코드 또는 전체 교체 코드를 제공합니다.
- 경로를 정확히 작성합니다.
- mock 데이터가 아니라 실제 DB / Controller / Entity / API 기준으로 작업합니다.
- 기존 구조를 모르면 추측하지 말고 저장소 파일을 확인한 뒤 답합니다.
- 설명보다 적용 가능한 코드 위주로 답합니다.
- TS/CSS 수정은 webpack watch로 반영됩니다.
- C# Controller 수정은 IIS Express 재시작이 필요합니다.

---

## 프로젝트 구조와 UI 방향

- ASP.NET Core MVC + Razor + TypeScript + Webpack 구조입니다.
- UI는 Tabler / Bootstrap 기반입니다.
- Grid는 Tabulator를 사용합니다.
- Chart는 ECharts를 사용합니다.
- Icon은 Lucide를 사용합니다.
- CSS는 공통 CSS 변수 기반 Light / Dark 테마를 사용합니다.
- 페이지별 TS/CSS는 아래에 둡니다.
  - `WebFlex.UI/Scripts/src/ts/views/...`
  - `WebFlex.UI/Scripts/src/css/views/...`
- 빌드 산출물은 `wwwroot/1.0.0` 아래에 생성됩니다.
- `_LayoutMain.cshtml`은 sidebar + topbar + page 구조입니다.
- `.wf-page` 안에서 각 페이지는 `<div class="wf-page-layout">` 또는 `<div class="wf-page-layout is-scroll">` 구조를 사용합니다.
- 데스크톱에서도 스크롤이 필요한 페이지는 `is-scroll`을 사용합니다.
- 반응형에서는 기본 페이지도 아래로 떨어지면서 스크롤 가능하게 합니다.

---

## 공통 알림

- 브라우저 기본 `alert` 대신 하단 고정 toast를 사용합니다.
- `notify.success/info/warning/error()` 형태로 사용합니다.
- toast host는 `_LayoutMain.cshtml`, `_LayoutLogin.cshtml`에 `#wfToastHost`로 들어갑니다.

---

## 공통 검색 패널

- 조회 조건이 필요한 페이지는 `.wf-search-panel`을 사용합니다.
- 접기/펼치기 버튼은 `data-wf-search-toggle`을 사용합니다.
- 접으면 아래 그리드가 남은 높이를 차지해야 합니다.
- 접기 시 `webflex:layoutChanged` 이벤트를 발생시키고, Tabulator/ECharts는 이때 redraw/resize합니다.

---

## 공통 그리드

`WebFlexGrid` 래퍼를 사용합니다.

현재 기능:

- `onRowClick`
- `onRowDoubleClick`
- `onSelectionChanged`
- `getData`
- `getSelectedData`
- `clearSelection`
- `setFilter`
- `clearFilter`
- `redraw`
- `refreshLayout`
- `showLoading`
- `hideLoading`

주의사항:

- Tabulator 이벤트는 options 안에 `rowClick`으로 넣지 말고 `table.on("rowClick", ...)` 방식으로 연결해야 정상 동작합니다.
- 앞으로 `booleanFormatter`, `badgeFormatter`, `actionButtonFormatter`, `onActionClick` 같은 공통 formatter도 추가 예정입니다.
- `WebFlexGrid`는 `Scripts/src/ts/components` 쪽으로 옮기는 방향입니다.

---

## 공통 컴포넌트

공용 컴포넌트 위치 예:

- `WebFlex.UI/Scripts/src/ts/components/webflexCheckTree.ts`
- `WebFlex.UI/Scripts/src/ts/components/webflexTagRegisterPopup.ts`

체크 트리:

- `WebFlexCheckTree`는 공용 컴포넌트입니다.
- 상위 노드 선택 시 하위 노드 선택/해제되는 `cascadeCheck` 기본 동작이 필요합니다.
- 트리 그룹은 화살표 클릭 시 접기/펼치기 가능합니다.

태그 등록 팝업:

- `WebFlexTagRegisterPopup`은 공용 컴포넌트입니다.
- 중앙 팝업과 오른쪽 드로어 두 방식을 지원합니다.
- 두 방식 모두 기능은 같고 디자인만 다릅니다.
- `widthPercent`, `heightPercent` 옵션으로 화면 대비 가로/세로 크기를 설정할 수 있습니다.

Lucide 동적 아이콘:

- 동적 HTML의 Lucide 아이콘은 `window.lucide.createIcons()`를 호출해야 합니다.
- `app.ts`에서 `window.lucide`를 노출하도록 수정했습니다.

---

## 테마

- `webflex-theme.css`에서 CSS 변수로 테마를 관리합니다.
- 저장 기능 없이 주석 처리/해제 방식으로 라이트 테마를 변경합니다.
- 블루톤 / 에메랄드·그린톤 라이트 테마 블록을 만들어둔 상태입니다.
- 다크모드는 블루톤 다크 / 에메랄드 다크를 따로 둡니다.
- 에메랄드 다크는 배경까지 초록빛이면 너무 강해서, 배경/표면/텍스트는 블루 다크와 동일하고 포인트 색상만 에메랄드로 유지합니다.
- Tabler 버튼 색상 때문에 Bootstrap `--bs-*`만 바꾸면 안 되고 `--tblr-*` 변수도 같이 덮어야 합니다.

주요 변수:

- `--wf-primary`
- `--wf-primary-hover`
- `--wf-primary-active`
- `--wf-primary-soft`
- `--wf-success`
- `--wf-success-soft`
- `--wf-danger`
- `--wf-danger-soft`
- `--wf-warning`
- `--wf-warning-soft`
- `--wf-surface`
- `--wf-surface-soft`
- `--wf-bg`
- `--wf-border`
- `--wf-text`
- `--wf-text-muted`

---

## TST2000

디바이스 등록 테스트 페이지입니다.

경로:

- View: `WebFlex.UI/Views/Test/TST2000.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/test/tst2000.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/test/tst2000.css`
- API Controller: `WebFlex.UI/Controllers/Test/TestDeviceController.cs`

구조:

- 조회 조건 없이 구성합니다.
- 왼쪽: 디바이스 목록.
- 오른쪽: 디바이스 상세.
- 목록 row 클릭 시 오른쪽 상세 폼에 바인딩되고 수정 상태가 됩니다.
- 신규 버튼 클릭 시 폼 초기화되고 신규 상태가 됩니다.
- `WebFlexGrid`의 `onRowClick: row => this.selectRow(row)`를 사용합니다.
- 반응형에서 목록/상세가 세로로 떨어지도록 수정했습니다.

---

## TST2010

디바이스 태그 관리 테스트 페이지입니다.

경로:

- View: `WebFlex.UI/Views/Test/TST2010.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/test/tst2010.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/test/tst2010.css`

구조:

- 태그 등록 버튼 2개를 사용합니다.
  - 중앙 팝업
  - 오른쪽 드로어
- 두 팝업 모두 기능은 같고 디자인만 다릅니다.
- 공용 `WebFlexTagRegisterPopup` 컴포넌트를 참조해서 사용합니다.
- OPC 노드 트리에서 체크박스 클릭 가능해야 합니다.
- 상위 그룹 체크 시 하위 노드 전체 선택/해제됩니다.
- 그룹 화살표 클릭 시 접기/펼치기 됩니다.
- 팝업/드로어 크기는 `widthPercent`, `heightPercent` 옵션으로 설정합니다.
- 데이터는 실제 디바이스/API 기준입니다.

디바이스 연결 상태:

- 디바이스 선택 시 실제 OPC 연결 여부 확인 API를 호출합니다.
- 예: `/test/devicetag/check-connection?deviceId=...`
- 성공: 연결됨
- 실패: 연결 실패
- 미선택: 미선택
- CSS 상태:
  - 미선택: 회색
  - 연결: 초록
  - 실패: 빨강
- `wf-status-dot` 배경색은 CSS 변수를 사용합니다.

---

## SVC1000

Windows Service 관리 페이지입니다.

경로:

- View: `WebFlex.UI/Views/System/SVC1000.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/system/svc1000.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/system/svc1000.css`

디자인 방향:

- 서비스 카드 형태입니다.
- 상태에 따라 상단 accent 색상이 변경됩니다.
  - 실행 중: 성공색
  - 중지/미등록: 회색
  - 오류: 위험색
- PID/메모리 사용량은 화면에는 표시하되 아직 기능 추가 전이라 `-`로 고정합니다.
- 로그는 접기/펼치기 가능합니다.

기존 기능 유지:

- 상태 조회
- 서비스 등록
- 시작
- 중지
- 재시작
- 삭제

ZIP 배포:

- ZIP 배포 함수 `deployZip()`는 TS에 남아 있으나 View에는 현재 노출하지 않습니다.
- Collector ZIP 업로드 → 서비스 중지 → 배포 → 재시작 흐름으로 추정됩니다.
- 운영 배포용이라 나중에 별도 접이식 배포 관리 영역으로 분리하는 것이 좋습니다.

---

## OPC1000

OPC 수집 관리 페이지입니다.

경로:

- View: `WebFlex.UI/Views/Opc/OPC1000.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/opc/opc1000.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/opc/opc1000.css`

구조:

- 기존 디바이스 select 방식은 제거했습니다.
- 페이지를 열면 모든 디바이스를 카드 형태로 표시합니다.
- 최대 2개 디바이스를 표시합니다.

카드 정보:

- 디바이스명
- 코드
- 타입
- endpoint
- 구독 태그
- 현재값 row 수
- CPU/메모리는 API 없으면 `-`
- 구독중지/구독시작 버튼
- DB 저장 상태 배지

상단 요약 카드:

- 전체 디바이스
- 구독 태그
- Snapshot Rows
- DB Inserted
- Collector 버전

Write Queue:

- Write Queue는 사용하지 않으므로 카드와 관련 TS 코드를 삭제했습니다.

구독중지 개수:

- 디바이스 구독중지 시 전체 디바이스 카드 하단에 `N개 구독중지`가 표시되게 합니다.
- 구독중지 판정:
  - `runtimeStatus.subscriptionStopped === true`
  - 또는 `summary.subscriptionStatus === "Stopped"`
  - 또는 `summary.subscriptionStatus === "SubscriptionStopped"`
  - 또는 `summary.subscriptionStatus === "중지"`

Lucide 아이콘 문제:

- 동적 카드 HTML의 Lucide `cpu`, `link`, `timer`, `clock`, `save` 등 아이콘은 `window.lucide.createIcons()` 호출이 필요합니다.
- 아이콘이 테마 전환 직후만 보이고 3초 refresh 후 사라졌던 원인은 다음과 같습니다.
  - `app.ts`는 `lucide`를 import만 하고 `window.lucide`에 등록하지 않았습니다.
  - `opc1000.ts`는 `window.lucide.createIcons()`를 호출하고 있었습니다.
- 해결:
  - `app.ts`에서 `exposeWebFlexIcons()` 추가.
  - `window.lucide = { createIcons: () => createIcons({ icons }) }`
  - `DOMContentLoaded`에서 `exposeWebFlexIcons()`를 먼저 호출합니다.

---

## Main / Index

메인 인덱스는 실시간 CurrentValue 리스트형 대시보드입니다.

경로:

- View: `WebFlex.UI/Views/Main/Index.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/main/index.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/main/index.css`

주의:

- 최신 코드에서 script 경로는 `~/1.0.0/js/views/main/index.js`입니다.
- CSS도 `Scripts/src/css/views/main/index.css` 기준으로 작업합니다.

구조:

- 기존처럼 notify/SSE로 업데이트되는 데이터를 계속 받아서 화면 업데이트합니다.
- 페이징 UI는 만들지 않습니다.
- 서버 사이드 페이지 조회는 사용하지만 UI는 무한 스크롤입니다.
- `/api/currentvalue/page?skip=&take=&groupId=&keyword=` 사용.
- 처음 120개 로드.
- 스크롤 하단 근처 도달 시 다음 120개 로드.

SSE:

- `/api/currentvalue/stream`
- `currentvalue` 이벤트 수신.
- 이미 로드된 행만 즉시 업데이트합니다.
- 아직 로드하지 않은 행은 순서/skip 꼬임 방지를 위해 즉시 삽입하지 않습니다.
- 나중에 스크롤로 로드할 때 DB 최신값이 반영됩니다.
- 업데이트된 값은 `value-flash` 애니메이션으로 변경 표시합니다.

검색:

- 검색 중에도 SSE 업데이트는 멈추지 않습니다.
- 검색 조건에 맞는 로드된 행만 실시간 업데이트합니다.
- 검색 조건에서 벗어난 이미 로드된 행은 제거하는 방향으로 수정할 수 있습니다.
- 검색은 엔터 없이 input debounce로 자동 검색합니다.
  - `searchTimer`
  - `input` 이벤트에서 `requestSearch()`
  - 350ms 후 keyword 적용 + reload
  - Enter는 `applySearchImmediately()`로 즉시 검색

그룹 선택:

- 상단 그룹 선택 카드.
- 그룹 chip으로 필터.
- 전체 그룹 / 그룹별 count / badCount 표시.
- 그룹 선택 영역은 접기/펼치기 가능.
- 관련 요소:
  - 버튼: `#btnToggleGroupPanel`
  - panel: `#currentGroupPanel`
  - body: `#groupFilterBody`

현재 원하는 배치:

- 그룹 선택 카드가 위에 있고 접기/펼치기 가능.
- 실시간 태그 리스트 헤더는 가능하면 한 줄입니다.
- `wf-current-status-row` 4개 태그는 `실시간 태그 리스트 1~120` 바로 오른쪽에 위치합니다.
- 한 줄 예:
  - `실시간 태그 리스트 1 ~ 120 전체 1008 로드 120 수신 1513 연결됨    새로고침 검색창`
- 새로고침/검색창은 같은 헤더 라인의 오른쪽에 위치합니다.
- 화면이 좁을 때만 자연스럽게 줄바꿈합니다.

최근 완료된 수정:

- `Index.cshtml` / `index.css`를 전체 교체하는 방향으로 정리했습니다.
- 이후 엔터 없이 검색되도록 TS를 수정했습니다.
  - `renderTimer` 아래 `searchTimer: number | null = null;` 추가.
  - `#txtKeyword` 이벤트를 `input` debounce + Enter 즉시 검색으로 변경.
  - `requestSearch()`, `applySearchImmediately()` 함수 추가.

---

## API / DB

- `WebFlexDbContext`에는 `DbSet` 속성이 명시적으로 없고 `_db.Set<OpcDevice>()`, `_db.Set<OpcTag>()`를 사용합니다.
- `OpcDevice` 엔티티:
  - `WebFlex.Shared/Entities/Opc/OpcDevice.cs`
- `OpcTag` 엔티티:
  - `WebFlex.Shared/Entities/Opc/OpcTag.cs`
- 디바이스 목록은 실제 DB의 `OpcDevice`에서 조회합니다.
- 태그 수는 `OpcTag`에서 `DEVICE_ID` 기준 count합니다.

CurrentValue:

- Entity: `WebFlex.Shared/Entities/Timeseries/CurrentValue.cs`
- `TsdReadDbContext`에 매핑되어 있습니다.

CurrentValue API가 없거나 병합 필요하면 아래 기준을 사용합니다.

- Controller: `WebFlex.UI/Controllers/Api/CurrentValueController.cs`
- `/api/currentvalue/page`
- `/api/currentvalue/groups`
- `/api/currentvalue/stream`

기존 notify service:

- `WebFlex.UI/Services/CurrentValue/CurrentValueNotifyService.cs`

---

## Controller 패턴

- View 라우팅 Controller와 API Controller를 분리합니다.
- 테스트 페이지:
  - `TestController`는 View 라우팅만 담당합니다.
  - 테스트 페이지별 API는 별도 Controller로 분리합니다.
  - 예: `TestDeviceController`는 `/test/device/[action]`.
- API 응답은 `{ success, message, data }` 형태로 통일합니다.

---

## 사용자 선호

- 최신 코드를 반드시 확인하고 작업합니다.
- 임의 추측 금지.
- 사용자가 명시적으로 요청하지 않는 한 직접 push/수정 금지.
- 수정 전/후 코드 또는 전체 교체 코드 제공.
- 경로 정확히.
- mock 데이터 금지.
- 설명은 짧게, 적용 가능한 코드 위주.
- 한국어 답변.
