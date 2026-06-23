namespace WebFlex.Shared;

public enum VaribaleStatusType {
    /// <summary>
    /// good
    /// </summary>
    Good = 0,
    /// <summary>
    /// 베드
    /// </summary>
    Bad = 1,
    /// <summary>
    /// 네트워크 패킷 이상
    /// </summary>
    NetworkTransportError = 2,
    /// <summary>
    /// 읽기 타입 아웃
    /// </summary>
    TimeoutError = 3,
    /// <summary>
    /// 주소 정보 이상
    /// </summary>
    AddressError = 4,
    /// <summary>
    /// 읽기 메소드 이상
    /// </summary>
    MethodError = 5,
    /// <summary>
    /// 연산 작업 이상
    /// </summary>
    ConvertError = 6,
    /// <summary>
    /// 연산 작업 이상
    /// </summary>
    ExpressionError = 7,
    /// <summary>
    /// 상태없음
    /// </summary>
    UnKnow = 8,
    /// <summary>
    /// 커스텀1
    /// </summary>
    Warning = 9,
    /// <summary>
    /// 커스텀2
    /// </summary>
    Red = 10,
    /// <summary>
    /// 커스텀3
    /// </summary>
    Custome3 = 11,
    /// <summary>
    /// 커스텀4
    /// </summary>
    Custome4 = 12,
    /// <summary>
    /// 커스텀5
    /// </summary>
    Custome5 = 13
}