** 작업 시 필수로 기억해야 하는 부분
- CSS 클래스명: dvc1020-*를 대량 생성하지 말고, 기존처럼 wf-* 공통 클래스 중심 + 정말 필요한 페이지 전용 클래스만 최소화
- CSHTML 구조: 기존 레이아웃, 카드, 툴바, 검색 패널, 드로어/팝업 구조와 동일한 작성 방식
- TS 구조: Page 클래스, init(), 이벤트 바인딩, API 호출, notify, 렌더링 함수 분리 방식은 기존과 동일하게
- private, public 안 붙임. type 사용 하지 않고 any 사용, 무분별한 type 및 dto 생성하지 않음, 다른 곳에도 사용 될 것 같은 건 공통 내용으로 수정
- Controller 구조: WebFlexController 상속, Success/ErrorData, WebFlexModelMapper.PopulateDTOModel, ApplyModel, 트랜잭션/예외처리 패턴 동일하게
- 무분별한 type 및 dto 생성하지 않음, 필요한 경우에만 dto 생성, 다른 곳에도 사용 될 것 같은 건 공통 내용으로 수정, 쿼리에서 select 를 쓰는 경우는 특정 몇 개의 컬럼이 필요한 쿼리문에만 사용 이외에 전체 컬럼명을 모두 쓰는 select 문을 사용하지 않음
- 모델 매핑: request 필드를 하나씩 꺼내서 변수로 쓰는 방식이 아니라, 요청 전체를 모델에 먼저 담고 검증/보정/저장
- 기능 구현: 새 페이지 기능에 맞게 새로 작성
- 기존의 코드 스타일은 유지하고, 공통으로 사용하는 코드를 사용, 기존 모델 중심으로 사용하고 무분별한 dto 및 type 생성하지 않음

# WebFlexVer2 AI 작업 컨텍스트

이 문서는 새 대화창에서 WebFlexVer2 작업을 이어가기 위한 요약 문서입니다.

새 대화에서는 먼저 이 파일을 읽고, 저장소 최신 코드를 확인한 뒤 작업합니다.


OPC 기반 설비/태그를 등록하고, OPC UA 데이터를 수집해서 PostgreSQL/TimescaleDB에 저장하고, 웹에서 실시간 상태와 수집 데이터를 관리/조회하는 산업용 IoT 모니터링/수집 관리 시스템이에요.

프로젝트 역할

WebFlex.Shared
공통 엔티티/DTO/Enum 프로젝트입니다.
OpcDevice, OpcTag, OpcGroup, OpcCollectOption, CurrentValue, TimescaleValue 같은 DB 모델이 여기 있습니다. BaseEntity를 기준으로 ID, IsEnabled, CreatedAt, UpdatedAt를 공통으로 쓰고, KeyFieldColumn, ColumnStringLength, ColumnRequired, Column(Order) 같은 커스텀 속성으로 DB 매핑 스타일을 맞춥니다.
WebFlex.UI
ASP.NET Core MVC 웹 화면입니다.
Razor View + Controller + TypeScript + CSS 구조이고, 디바이스 관리, 태그 관리, OPC 수집 관리, 옵션 관리, Timescale 조회/설정, Windows Service 제어 화면을 담당합니다.
일반 설정 DB는 WebFlexDbContext, 시계열 조회 DB는 TsdReadDbContext로 분리되어 있습니다. 인증은 Cookie Auth이고, 기본적으로 모든 MVC에 AuthorizeFilter가 걸려 있습니다.
WebFlex.OpcCollector
실제 OPC 수집 Windows Service입니다.
OPC UA 세션/구독을 만들고, 태그 값을 메모리 딕셔너리에 최신값으로 유지한 뒤 1초 단위 스냅샷을 만들어 TimescaleDB에 저장합니다. API 서버 역할도 같이 해서 UI에서 JWT로 상태 조회/제어를 호출하는 구조입니다.

사이트 구조

Device
DVC1000: 디바이스 등록/관리
DVC1010: 디바이스 태그 관리
현재 메뉴 시드에는 DVC2000 그룹 관리가 잡혀 있음
Opc
OPC1000: OPC 수집 상태/디바이스별 구독 관리
OPC1020: Collector 수집 옵션
OPC1030: OPC Client/Subscription/MonitoredItem 옵션
OPC3000: OPC History 조회
OPC4000: TimescaleDB 설정
System
SVC1000: Windows Service 설치/시작/중지/재시작/삭제
Main/Index
실시간 currentvalue 리스트형 대시보드

코드 스타일

Controller는 보통 [Route(".../[action]")] + [HttpGet/HttpPost, ActionName("...")] 패턴입니다.
View 라우팅 Controller는 return View(MVCPath.Device.DVC1010)처럼 MVCPath 상수를 사용합니다.
API 응답은 { success, message, data } 형태로 통일하려는 방향입니다.
저장/수정 Controller는 WebFlexModelMapper.PopulateDTOModel<T>()로 request를 모델에 먼저 담고, 그 뒤 검증/보정/저장하는 스타일입니다.
저장성 로직은 BeginTransactionAsync() + try/catch + WebFlexMessageException 업무 예외 처리 패턴을 씁니다.
EF는 명시적 DbSet보다 _db.Set<OpcTag>(), _db.Set<OpcDevice>()를 많이 사용합니다.

프론트 스타일

Razor는 화면 뼈대만 만들고, 실제 데이터 조회/이벤트/렌더링은 페이지별 TS에서 처리합니다.
TS는 export default class Page { init(): void { ... } } 형태입니다.
jQuery + axios 계열 api.get/post, notify.success/error/warning/info, 공통 WebFlexGrid, WebFlexPopup, WebFlexCheckTree를 사용합니다.
CSS 클래스는 wf-* 공통 클래스 중심입니다. 페이지 전용 클래스도 wf-tag-*, wf-opc-*처럼 의미 기반으로 쓰고, dvc1020-* 같은 페이지번호 클래스 남발은 피하는 방향입니다.
UI는 Tabler/Bootstrap 기반, Grid는 Tabulator, Chart는 ECharts, Icon은 Lucide입니다.
CSS는 webflex-theme.css, webflex-layout.css, webflex-components.css 같은 공통 변수/컴포넌트 기반이고, 현재 테마는 에메랄드/그린 톤입니다.

수집 구조
현재 최신 코드 기준 수집 흐름은 이렇습니다.

OpcCollectTargetProvider
→ DB의 OpcDevice / OpcTag 수집 대상 조회
→ OpcUaRuntimeService
→ OPC UA Session / Subscription / MonitoredItem 생성
→ 값 변경 이벤트 수신
→ runtime.CurrentValues 딕셔너리에 최신값 유지
→ OpcRuntimeManager가 1초 경계마다 Snapshot 생성
→ TimescaleDbWriter.SaveSnapshotAsync()
→ COPY BINARY로 public.timescale 저장
→ 옵션이 켜져 있으면 public.currentvalue upsert

중요한 점은 최신 TimescaleDbWriter는 예전처럼 일반 큐를 계속 쌓는 구조가 아니라, DirectSnapshot 방식입니다. 다만 내부 저장은 _saveLock으로 한 번에 하나씩 처리하고, InsertHistoryAsync()에서 기본 chunk를 최대 500개로 잘라 COPY하고 있습니다.

DB 구조

일반 설정 DB: WebFlexDb
디바이스, 태그, 그룹, 옵션, 메뉴, 사용자/권한 등.
시계열 DB: WebFlexTsd
timescale, timescale_minute, currentvalue, currentvalue_minute.
현재 시계열 모델은 tag_id, group_id, value, status, cookie_value, source_timestamp, received_at 중심입니다. 예전 endpoint_url/node_id 중심에서 tag_id/group_id 중심으로 바뀐 상태입니다.

한 줄로 정리하면, WebFlexVer2는 OPC 수집기를 Windows Service로 돌리고, MVC 웹에서 디바이스/태그/수집옵션/서비스/실시간 데이터를 관리하는 .NET 8 기반 OPC IoT 수집 플랫폼

---

## 작업 방식

- 저장소는 GitHub 연결된 `sokumi/WebFlexVer2` 기준으로 최신 코드를 먼저 확인합니다.
- 사용자가 명시적으로 요청하지 않는 한 직접 저장소를 수정하지 않고, 적용 가능한 수정 전/후 코드 또는 전체 교체 코드를 제공합니다.
- 커밋/푸시는 사용자가 명시적으로 요청했을 때만 수행합니다.
- 경로를 정확히 작성합니다.
- mock 데이터가 아니라 실제 DB / Controller / Entity / API 기준으로 작업합니다.
- 기존 구조를 모르면 추측하지 말고 저장소 파일을 확인한 뒤 답합니다.
- 설명보다 적용 가능한 코드 위주로 답합니다.
- TS/CSS 수정은 webpack watch로 반영됩니다.
- C# Controller 수정은 IIS Express 재시작이 필요합니다.
- 사용자는 수정 전/후 코드 또는 전체 교체 코드를 선호합니다.
- 한국어로 답변합니다.

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

## 최근 작업 인수인계: DVC1010 / 태그 등록 / Controller 패턴

다음 대화에서 우선 확인해야 할 최신 작업 흐름입니다.

### 관련 파일

- View: `WebFlex.UI/Views/Device/DVC1010.cshtml`
- Page TS: `WebFlex.UI/Scripts/src/ts/views/device/dvc1010.ts`
- Page CSS: `WebFlex.UI/Scripts/src/css/views/device/dvc1010.css`
- API Controller: `WebFlex.UI/Controllers/Device/DeviceTagController.cs`
- 공통 Controller: `WebFlex.UI/Common/WebFlexController.cs`
- 공통 모델 매퍼: `WebFlex.UI/Common/WebFlexModelMapper.cs`
- 공통 팝업 컴포넌트: `WebFlex.UI/Scripts/src/ts/components/webflexTagRegisterPopup.ts`
- 공통 체크 트리: `WebFlex.UI/Scripts/src/ts/components/webflexCheckTree.ts`
- 공통 예외: `WebFlex.Shared/Exceptions/WebFlexMessageException.cs`

### DVC1010 주요 설계 결정

- `webflexTagRegisterPopup.ts`는 공통 라이브러리 개념으로 유지합니다.
- 공통 컴포넌트는 `DVC1010`, `OpcTag`, `DeviceTagController`, DB 컬럼명 같은 페이지/모델 전용 개념을 몰라야 합니다.
- 공통 컴포넌트는 선택된 노드의 원본 필드와 팝업에서 편집한 필드만 반환합니다.
- `displayName -> tagName` 같은 저장 API DTO 변환은 `dvc1010.ts`에서 처리합니다.
- Controller에서는 저장 요청을 필드별로 하나씩 꺼내 변수에 담는 방식보다, 요청 JSON 전체를 먼저 모델에 매핑한 뒤 모델 값을 사용합니다.
- 예전 WebFlex 방식처럼 `PopulateDTOModel<T>(values)` 또는 `PopulateDTOModel<T>(JsonElement)`로 모델/리스트를 먼저 만들고 업무 로직을 처리합니다.
- `DeviceTagController.Save` 예시 흐름은 아래처럼 잡습니다.

```csharp
var saveModel = WebFlexModelMapper.PopulateDTOModel<DeviceTagSaveModel>(request);

if (string.IsNullOrWhiteSpace(saveModel.DEVICE_ID)) {
    return ErrorData("디바이스를 선택해 주세요.");
}

foreach (var tag in saveModel.NODES) {
    tag.DEVICE_ID = saveModel.DEVICE_ID;
    // 중복 체크, 신규 ID 채번, 기본값 세팅, 저장 처리
}
```

- `var deviceId = WebFlexModelMapper.GetString(request, "deviceId", "DEVICE_ID")`처럼 먼저 변수로 빼는 방식은 보조적으로만 사용합니다.
- 핵심 요청 모델은 반드시 `OpcTag` 또는 요청용 모델에 먼저 담는 방향입니다.
- `GetString`, `GetStringList`, `TryGetProperty`는 삭제/조회/특수 필드 보조 처리용으로만 사용합니다.

### DVC1010 태그 모델/화면 변경

- `OpcTag`에는 권한 컬럼 `PROTECT_TYPE`을 추가합니다.
- 권한 값은 3가지를 사용합니다.
  - `ReadOnly`: 읽기 전용
  - `ReadWrite`: 읽고 쓰기
  - `WriteOnly`: 쓰기 전용
- 기존 값 호환을 위해 `READ_ONLY`, `READ_WRITE`, `WRITE_ONLY`, 한국어 라벨도 normalize합니다.
- 데이터 타입 옵션은 다음 값을 사용합니다.
  - `bit`, `bool`, `uint8`, `int8`, `uint16`, `int16`, `bcd16`, `uint32`, `int32`, `float`, `bcd32`, `uint64`, `int64`, `double`, `ascii`, `utf8`, `datetime`, `timestamp(ms)`, `timestamp(s)`
- 등록 드로어/상세 폼에서는 데이터 타입과 권한을 수정할 수 있어야 합니다.
- 등록된 태그 그리드에서는 ID를 제외하고 나머지 필드를 수정 가능하게 합니다.
- 그리드 행 클릭 시 체크박스가 같이 선택되면 안 됩니다. 체크박스 클릭/변경 이벤트에서만 선택 상태를 변경합니다.
- 행 클릭 시 오른쪽 태그 상세 패널을 표시합니다.
- 아무 행도 선택하지 않았거나 상세가 닫힌 상태에서는 그리드가 가로 100%를 차지해야 합니다.
- 행을 클릭했을 때만 상세 패널 너비만큼 그리드가 줄어듭니다.
- 상세 패널 배치는 다음 순서를 따릅니다.
  - 설명
  - NodeId | 태그명
  - 데이터 타입 | 권한
  - 대시보드(bool) | 정렬
  - 수집 | DB 저장 | 사용
  - 테스트 입력 | 테스트 버튼
  - 상세설정 textarea

### OPC 노드 설명 조회

- Kepware에서 한글 설명은 일반 Attribute Description이 아니라 `_Description` Property 노드 값으로 들어오는 경우가 있습니다.
- `OpcBrowseService`에서 Variable 노드 조회 시 아래 순서로 설명을 찾습니다.
  - `Attributes.Description`
  - `HasProperty`의 `_Description`
  - `HasProperty`의 `Description`
  - 직접 NodeId 뒤에 `._Description`을 붙인 문자열 노드 값
- 찾은 값은 `DeviceNodeDto.Description`에 넣어 DVC1010 태그 등록 설명 기본값으로 사용합니다.

---

## 공통화 작업: Controller 구조

### 공통 Controller 파일

경로:

- `WebFlex.UI/Common/WebFlexController.cs`

공통 응답 메서드는 각 Controller에 반복해서 만들지 않고, 공통 base controller로 뺍니다.

```csharp
using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Common;

public abstract class WebFlexController : Controller {
    protected IActionResult Success(string message = "처리되었습니다.", object? data = null) {
        return Json(new {
            success = true,
            message,
            data
        });
    }

    protected IActionResult ErrorData(string message, object? data = null) {
        return Json(new {
            success = false,
            message,
            data
        });
    }

    protected static string GetErrorMessage(Exception ex) {
        return ex.InnerException?.Message ?? ex.Message;
    }
}

public abstract class WebFlexApiController : ControllerBase {
    protected IActionResult Success(string message = "처리되었습니다.", object? data = null) {
        return Ok(new {
            success = true,
            message,
            data
        });
    }

    protected IActionResult ErrorData(string message, object? data = null) {
        return Ok(new {
            success = false,
            message,
            data
        });
    }

    protected static string GetErrorMessage(Exception ex) {
        return ex.InnerException?.Message ?? ex.Message;
    }
}
```

### 사용 규칙

- Razor View를 반환하거나 MVC Controller 성격이면 `WebFlexController`를 상속합니다.

```csharp
public class DeviceTagController : WebFlexController {
}
```

- 순수 API Controller 성격이면 `WebFlexApiController`를 상속합니다.

```csharp
public class CurrentValueController : WebFlexApiController {
}
```

- 각 Controller 안에 아래 메서드를 다시 만들지 않습니다.
  - `Success`
  - `ErrorData`
  - `GetErrorMessage`
- 예외 응답은 아래 형태를 사용합니다.

```csharp
} catch (Exception ex) {
    return ErrorData(GetErrorMessage(ex));
}
```

- 트랜잭션이 있는 액션은 아래 흐름을 유지합니다.

```csharp
await using var tran = await _db.Database.BeginTransactionAsync();

try {
    // 업무 처리
    await _db.SaveChangesAsync();
    await tran.CommitAsync();
    return Success("저장되었습니다.");
} catch (WebFlexMessageException ex) {
    await tran.RollbackAsync();
    return ErrorData(ex.Message);
} catch (Exception ex) {
    await tran.RollbackAsync();
    return ErrorData(GetErrorMessage(ex));
}
```

---

## 공통화 작업: WebFlexModelMapper

### 공통 매퍼 파일

경로:

- `WebFlex.UI/Common/WebFlexModelMapper.cs`

### 목적

- 예전 WebFlex의 `PopulateDTOModel<T>(values)` 흐름을 WebFlexVer2에서도 사용할 수 있게 합니다.
- Controller에서 JSON 필드를 하나씩 꺼내 변수에 담는 것이 아니라, JSON 문자열 또는 `JsonElement`를 모델에 먼저 매핑합니다.
- 이미 조회한 기존 Entity에 요청 값을 덮어쓰는 기능도 지원합니다.
- 리스트 JSON도 `List<T>` 모델로 변환합니다.

### 기본 사용 예시

문자열 JSON을 모델로 변환:

```csharp
var model = WebFlexModelMapper.PopulateModel<OpcTag>(values);
```

문자열 JSON을 리스트 모델로 변환:

```csharp
var models = WebFlexModelMapper.PopulateDTOModel<List<OpcTag>>(values);
```

`JsonElement`를 요청 모델로 변환:

```csharp
var saveModel = WebFlexModelMapper.PopulateDTOModel<DeviceTagSaveModel>(request);
```

기존 DB Entity에 요청값 덮어쓰기:

```csharp
var tag = await _db.Set<OpcTag>().FirstAsync(x => x.ID == id);
WebFlexModelMapper.ApplyModel(tag, request, NormalizeOpcTag);
```

### 이름 매핑 규칙

- 이름 비교 시 `_`, `-`를 제거하고 소문자로 비교합니다.
- 아래 매핑이 자동으로 맞아야 합니다.
  - `nodeId -> NODE_ID`
  - `tagName -> TAG_NAME`
  - `dataType -> DATA_TYPE`
  - `protectType -> PROTECT_TYPE`
  - `isCollectEnabled -> IS_COLLECTENABLED`
  - `saveToDatabase -> SAVE_TO_DATABASE`
  - `showOnDashboard -> SHOW_ON_DASHBOARD`
  - `samplingIntervalMs -> SAMPLINGINTERVALMS`
  - `sortOrder -> SORT_ORDER`
  - `isEnabled -> IsEnabled`
  - `expression` 또는 `expressions -> EXPRESSIONS`
  - `displayName -> TAG_NAME`
  - `tagId -> ID`

### 타입 변환 규칙

- 문자열, bool, 숫자형, decimal, double, float, DateTime, Guid, enum을 처리합니다.
- 문자열 JSON도 그대로 모델에 담겨야 합니다.
- bool은 `true/false`, `1/0`, `Y/N`, `YES/NO` 형태를 허용합니다.
- 리스트 속성은 JSON 배열이면 `List<T>`로 변환합니다.
- `PopulateDTOModel<T>` 내부에서 제네릭 제약 오류가 나지 않도록, 제약 없는 `T`에는 `ApplyModel<T>`가 아니라 `ApplyModelByType(object model, Type modelType, JsonElement element)`를 사용합니다.

### Save 액션 모델 매핑 규칙

DVC1010 태그 저장처럼 요청 payload가 `{ deviceId, nodes }` 구조이면 전용 요청 모델을 만들고 전체 request를 먼저 담습니다.

```csharp
private class DeviceTagSaveModel : OpcTag {
    public List<OpcTag> NODES { get; set; } = new();
}
```

```csharp
[HttpPost, ActionName("save")]
public async Task<IActionResult> Save([FromBody] JsonElement request) {
    var saveModel = WebFlexModelMapper.PopulateDTOModel<DeviceTagSaveModel>(request);

    if (string.IsNullOrWhiteSpace(saveModel.DEVICE_ID)) {
        return ErrorData("디바이스를 선택해 주세요.");
    }

    if (saveModel.NODES.Count == 0) {
        return ErrorData("저장할 노드를 선택해 주세요.");
    }

    foreach (var tag in saveModel.NODES) {
        tag.DEVICE_ID = saveModel.DEVICE_ID;
        // 중복 체크, 신규 ID 채번, 기본값 세팅, 저장 처리
    }
}
```

중요: `request`에서 `deviceId`, `nodes`를 각각 꺼내서 변수로 분리하는 방식보다, 요청 전체를 모델에 먼저 담는 방식을 우선합니다.

---

## 공통 팝업 컴포넌트 방향

`WebFlexTagRegisterPopup`은 중앙 팝업과 오른쪽 드로어 양쪽에서 동일하게 동작해야 합니다.

공통 컴포넌트 타입은 페이지 전용 DTO가 아니라 범용 노드 타입이어야 합니다.

- `WebFlexTagRegisterNode`
  - `[key: string]: unknown` 허용
  - `nodeId`
  - `parentNodeId`
  - `displayName`
  - `nodeClass`
  - `dataType`
  - `description`
  - `children`
- `WebFlexTagRegisterSaveNode`
  - `[key: string]: unknown` 허용
  - `nodeId`
  - `parentNodeId`
  - `displayName`
  - `originalDisplayName`
  - `nodeClass`
  - `dataType`
  - `description`
  - `isCollectEnabled`
  - `isEnabled`
  - `protectType`

공통 컴포넌트 옵션은 필요한 경우 아래처럼 확장 가능합니다.

- `isSelectable?: (node) => boolean`
- `getNodeId?: (node) => string`
- `getNodeText?: (node) => string`
- `getNodeTooltip?: (node) => string`

기본값은 OPC 태그 등록 흐름에 맞춰도 되지만, 특정 API 저장 DTO 이름을 컴포넌트 내부에 넣으면 안 됩니다.

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
- 체크박스 선택 컬럼이 있는 그리드는 체크박스 클릭 이벤트에서 `stopPropagation()` 처리하여 행 클릭 이벤트와 분리합니다.
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
- 드롭다운은 옵션 내용이 잘리지 않도록 min-width/auto width를 고려합니다.
- 저장 API DTO 이름을 컴포넌트 내부에 넣지 않습니다.
- 페이지별 저장 변환은 각 page TS에서 처리합니다.

Lucide 동적 아이콘:

- 동적 HTML의 Lucide 아이콘은 `window.lucide.createIcons()`를 호출해야 합니다.
- `app.ts`에서 `window.lucide`를 노출하도록 수정했습니다.

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
- 공통 응답은 `WebFlexController`, `WebFlexApiController`의 `Success`, `ErrorData`를 사용합니다.
- Controller마다 `Success`, `ErrorData`를 private 메서드로 반복 작성하지 않습니다.
- 저장성 API는 가능한 경우 `BeginTransactionAsync`, 명시적 `try/catch`, `RollbackAsync`, `CommitAsync` 흐름이 액션 내부에 보이게 작성합니다.
- 예상 가능한 예외는 `WebFlexMessageException`으로 처리합니다.
- 예기치 않은 예외는 `GetErrorMessage(ex)`로 실패 응답을 반환합니다.
- 모델 저장/수정 요청은 request 값을 필드별로 먼저 꺼내기보다 `WebFlexModelMapper`로 모델에 먼저 담고, 이후 검증/보정/저장합니다.

---

## 주요 페이지 요약

### TST2000

- 디바이스 등록 테스트 페이지입니다.
- View: `WebFlex.UI/Views/Test/TST2000.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/test/tst2000.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/test/tst2000.css`
- API Controller: `WebFlex.UI/Controllers/Test/TestDeviceController.cs`
- 왼쪽 목록, 오른쪽 상세 구조입니다.
- 목록 row 클릭 시 오른쪽 상세 폼에 바인딩되고 수정 상태가 됩니다.

### TST2010

- 디바이스 태그 관리 테스트 페이지입니다.
- View: `WebFlex.UI/Views/Test/TST2010.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/test/tst2010.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/test/tst2010.css`
- 중앙 팝업 / 오른쪽 드로어 모두 공용 `WebFlexTagRegisterPopup` 컴포넌트를 사용합니다.
- OPC 노드 트리에서 상위 그룹 체크 시 하위 노드 전체 선택/해제됩니다.

### SVC1000

- Windows Service 관리 페이지입니다.
- View: `WebFlex.UI/Views/System/SVC1000.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/system/svc1000.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/system/svc1000.css`
- 상태 조회, 서비스 등록, 시작, 중지, 재시작, 삭제 기능을 유지합니다.
- ZIP 배포 함수 `deployZip()`는 TS에 남아 있으나 View에는 현재 노출하지 않습니다.

### OPC1000

- OPC 수집 관리 페이지입니다.
- View: `WebFlex.UI/Views/Opc/OPC1000.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/opc/opc1000.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/opc/opc1000.css`
- 기존 디바이스 select 방식은 제거하고 모든 디바이스를 카드 형태로 표시합니다.
- Write Queue는 사용하지 않으므로 카드와 관련 TS 코드를 삭제했습니다.
- 동적 카드 HTML의 Lucide 아이콘은 `window.lucide.createIcons()` 호출이 필요합니다.

### Main / Index

- 메인 인덱스는 실시간 CurrentValue 리스트형 대시보드입니다.
- View: `WebFlex.UI/Views/Main/Index.cshtml`
- TS: `WebFlex.UI/Scripts/src/ts/views/main/index.ts`
- CSS: `WebFlex.UI/Scripts/src/css/views/main/index.css`
- `/api/currentvalue/page?skip=&take=&groupId=&keyword=`를 사용합니다.
- `/api/currentvalue/stream`으로 SSE 업데이트를 받습니다.
- 검색은 엔터 없이 input debounce로 자동 검색합니다.
- 그룹 선택 영역은 접기/펼치기 가능합니다.

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
